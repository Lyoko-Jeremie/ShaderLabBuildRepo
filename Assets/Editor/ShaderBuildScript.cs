using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;
using System.Collections.Generic;

/// <summary>
/// Unity Editor build script for building ShaderLab fragments as AssetBundles.
/// Each shader in Assets/Shaders is compiled into its own independently usable
/// AssetBundle named after the shader file (lower-case, no extension).
///
/// Include files (.cginc / .hlsl / .glsl) are compile-time only: Unity resolves
/// them when building the bundle and bakes the resulting bytecode into the shader
/// asset, so no include file needs to be present at runtime.  Each produced
/// AssetBundle is therefore fully self-contained with no external dependencies.
///
/// Invoked by the CI workflow via -buildMethod ShaderBuildScript.Build.
/// </summary>
public class ShaderBuildScript
{
    private const string ShadersPath = "Assets/Shaders";
    private const string OutputDirectory = "build";

    /// <summary>
    /// Entry point called by the CI workflow (-buildMethod ShaderBuildScript.Build).
    /// Builds one self-contained AssetBundle per shader into the build/ directory.
    /// </summary>
    [MenuItem("Build/Export Shader AssetBundles")]
    public static void Build()
    {
        Debug.Log("[ShaderBuildScript] Starting per-shader AssetBundle build...");

        // Collect .shader assets.
        string[] shaderAssets = AssetDatabase
            .FindAssets("t:Shader", new[] { ShadersPath })
            .Select(AssetDatabase.GUIDToAssetPath)
            .ToArray();

        if (shaderAssets.Length == 0)
        {
            Debug.LogError("[ShaderBuildScript] No shader assets found in " + ShadersPath);
            EditorApplication.Exit(1);
            return;
        }

        // Track every asset that receives a bundle assignment so we can clear
        // the metadata afterwards and avoid persisting changes in the project.
        var assignedAssets = new HashSet<string>();

        // Assign each shader to its own named bundle.
        // Include files (.cginc/.hlsl/.glsl) are intentionally excluded: Unity
        // resolves them at bundle-build time and bakes the compiled bytecode into
        // the shader asset, so they are not required at runtime.  Omitting them
        // keeps every bundle truly independent with no cross-bundle references.
        foreach (string shaderAsset in shaderAssets)
        {
            string bundleName = Path.GetFileNameWithoutExtension(shaderAsset).ToLowerInvariant();
            Debug.Log($"[ShaderBuildScript] Bundle '{bundleName}' <- {shaderAsset}");

            AssetImporter importer = AssetImporter.GetAtPath(shaderAsset);
            if (importer != null)
            {
                importer.SetAssetBundleNameAndVariant(bundleName, string.Empty);
                assignedAssets.Add(shaderAsset);
            }
        }

        Directory.CreateDirectory(OutputDirectory);

        AssetBundleManifest manifest;
        try
        {
            manifest = BuildPipeline.BuildAssetBundles(
                OutputDirectory,
                BuildAssetBundleOptions.None,
                BuildTarget.StandaloneWindows64
            );
        }
        finally
        {
            // Clear bundle name assignments to avoid persisting metadata changes.
            foreach (string asset in assignedAssets)
            {
                AssetImporter importer = AssetImporter.GetAtPath(asset);
                if (importer != null)
                    importer.SetAssetBundleNameAndVariant(string.Empty, string.Empty);
            }
        }

        if (manifest == null)
        {
            Debug.LogError("[ShaderBuildScript] BuildAssetBundles failed.");
            EditorApplication.Exit(1);
            return;
        }

        string[] builtBundles = manifest.GetAllAssetBundles();
        Debug.Log($"[ShaderBuildScript] Build complete. {builtBundles.Length} bundle(s) written to: {OutputDirectory}/");
        foreach (string b in builtBundles)
            Debug.Log($"[ShaderBuildScript]   {b}");

        EditorApplication.Exit(0);
    }
}

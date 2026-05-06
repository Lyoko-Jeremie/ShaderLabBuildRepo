using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;
using System.Collections.Generic;

/// <summary>
/// Unity Editor build script for building ShaderLab fragments as a single AssetBundle.
/// All shaders in Assets/Shaders are compiled into one AssetBundle named "shaders.assetbundle".
///
/// Include files (.cginc / .hlsl / .glsl) are compile-time only: Unity resolves
/// them when building the bundle and bakes the resulting bytecode into the shader
/// asset, so no include file needs to be present at runtime.  The produced
/// AssetBundle is fully self-contained with no external dependencies.
///
/// Invoked by the CI workflow via -buildMethod ShaderBuildScript.Build.
/// </summary>
public class ShaderBuildScript
{
    private const string ShadersPath = "Assets/Shaders";
    private const string OutputDirectory = "build";
    private const string BundleName = "shaders.assetbundle";

    /// <summary>
    /// Returns the platform-specific output subdirectory derived from the active
    /// build target that was set by the CI runner before invoking this method.
    /// Example: "build/StandaloneWindows64" or "build/Android".
    /// </summary>
    private static string GetPlatformOutputDirectory()
    {
        string platformName = EditorUserBuildSettings.activeBuildTarget.ToString();
        return Path.Combine(OutputDirectory, platformName);
    }

    /// <summary>
    /// Entry point called by the CI workflow (-buildMethod ShaderBuildScript.Build).
    /// Builds one self-contained AssetBundle containing all shaders into a
    /// platform-specific subdirectory: build/<TargetPlatform>/.
    /// </summary>
    [MenuItem("Build/Export Shader AssetBundles")]
    public static void Build()
    {
        BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
        string platformOutputDir = GetPlatformOutputDirectory();
        Debug.Log($"[ShaderBuildScript] Starting single AssetBundle build for {target} -> {platformOutputDir}/");

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

        // Assign all shaders to the single shared bundle.
        // Include files (.cginc/.hlsl/.glsl) are intentionally excluded: Unity
        // resolves them at bundle-build time and bakes the compiled bytecode into
        // the shader asset, so they are not required at runtime.
        foreach (string shaderAsset in shaderAssets)
        {
            Debug.Log($"[ShaderBuildScript] Bundle '{BundleName}' <- {shaderAsset}");

            AssetImporter importer = AssetImporter.GetAtPath(shaderAsset);
            if (importer != null)
            {
                importer.SetAssetBundleNameAndVariant(BundleName, string.Empty);
                assignedAssets.Add(shaderAsset);
            }
        }

        Directory.CreateDirectory(platformOutputDir);

        AssetBundleManifest manifest;
        try
        {
            // ChunkBasedCompression (LZ4) is the Unity-recommended format for
            // AssetBundle.LoadFromFile.  The default (None = LZMA) can cause
            // LoadFromFile to return null when the bundle is loaded by a runtime
            // that differs even slightly from the exact build environment, and it
            // requires full decompression on load.  LZ4 supports random-access
            // reading and has far better cross-version compatibility.
            manifest = BuildPipeline.BuildAssetBundles(
                platformOutputDir,
                BuildAssetBundleOptions.ChunkBasedCompression,
                target
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
        Debug.Log($"[ShaderBuildScript] Build complete. {builtBundles.Length} bundle(s) written to: {platformOutputDir}/");
        foreach (string b in builtBundles)
            Debug.Log($"[ShaderBuildScript]   {b}");

        EditorApplication.Exit(0);
    }
}

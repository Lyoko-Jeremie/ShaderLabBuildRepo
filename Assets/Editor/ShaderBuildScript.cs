using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;

/// <summary>
/// Unity Editor build script for building ShaderLab fragments as an AssetBundle.
/// Invoked by the CI workflow via -buildMethod ShaderBuildScript.Build.
/// </summary>
public class ShaderBuildScript
{
    private const string ShadersPath = "Assets/Shaders";
    private const string OutputDirectory = "build";
    private const string BundleName = "shaderlib";

    /// <summary>
    /// Entry point called by the CI workflow (-buildMethod ShaderBuildScript.Build).
    /// Assigns all shaders and HLSL/CG include files from Assets/Shaders to an
    /// AssetBundle and builds it into the build/ directory.
    /// </summary>
    [MenuItem("Build/Export Shader AssetBundle")]
    public static void Build()
    {
        Debug.Log("[ShaderBuildScript] Starting ShaderLab AssetBundle build...");

        // Collect .shader assets
        string[] shaderAssets = AssetDatabase
            .FindAssets("t:Shader", new[] { ShadersPath })
            .Select(AssetDatabase.GUIDToAssetPath)
            .ToArray();

        // Collect HLSL / CG include files (stored as DefaultAsset by Unity)
        string[] includeAssets = AssetDatabase
            .FindAssets("t:DefaultAsset", new[] { ShadersPath })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => p.EndsWith(".cginc") || p.EndsWith(".hlsl") || p.EndsWith(".glsl"))
            .ToArray();

        string[] allAssets = shaderAssets.Concat(includeAssets).ToArray();

        if (allAssets.Length == 0)
        {
            Debug.LogError("[ShaderBuildScript] No shader assets found in " + ShadersPath);
            EditorApplication.Exit(1);
            return;
        }

        Debug.Log($"[ShaderBuildScript] Assigning {allAssets.Length} asset(s) to bundle '{BundleName}':");
        foreach (string asset in allAssets)
        {
            Debug.Log($"[ShaderBuildScript]   {asset}");
            AssetImporter importer = AssetImporter.GetAtPath(asset);
            if (importer != null)
                importer.SetAssetBundleNameAndVariant(BundleName, string.Empty);
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
            // Clear bundle name assignments to avoid persisting metadata changes in the project.
            foreach (string asset in allAssets)
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

        Debug.Log($"[ShaderBuildScript] Build complete. AssetBundles written to: {OutputDirectory}/");
        EditorApplication.Exit(0);
    }
}

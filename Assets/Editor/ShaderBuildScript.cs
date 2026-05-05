using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;

/// <summary>
/// Unity Editor build script for exporting ShaderLab fragments as a Unity package.
/// Invoked by the CI workflow via -buildMethod ShaderBuildScript.Build.
/// </summary>
public class ShaderBuildScript
{
    private const string ShadersPath = "Assets/Shaders";
    private const string OutputDirectory = "build";
    private const string PackageName = "ShaderLib.unitypackage";

    /// <summary>
    /// Entry point called by the CI workflow (-buildMethod ShaderBuildScript.Build).
    /// Exports all shaders and HLSL/CG include files from Assets/Shaders into a
    /// distributable Unity package located at build/ShaderLib.unitypackage.
    /// </summary>
    [MenuItem("Build/Export Shader Package")]
    public static void Build()
    {
        Debug.Log("[ShaderBuildScript] Starting ShaderLab build...");

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

        Debug.Log($"[ShaderBuildScript] Exporting {allAssets.Length} asset(s):");
        foreach (string asset in allAssets)
            Debug.Log($"[ShaderBuildScript]   {asset}");

        Directory.CreateDirectory(OutputDirectory);
        string packagePath = Path.Combine(OutputDirectory, PackageName);

        AssetDatabase.ExportPackage(
            allAssets,
            packagePath,
            ExportPackageOptions.Default | ExportPackageOptions.IncludeDependencies
        );

        Debug.Log($"[ShaderBuildScript] Build complete. Package written to: {packagePath}");
        EditorApplication.Exit(0);
    }
}

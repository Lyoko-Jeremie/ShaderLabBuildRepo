#ifndef EXAMPLE_INCLUDE_CGINC
#define EXAMPLE_INCLUDE_CGINC

// ---------------------------------------------------------------------------
// Utility helpers shared across ShaderLab fragments in this repository.
// Include this file with:
//   #include "CGInclude/ExampleInclude.cginc"
// ---------------------------------------------------------------------------

/// Applies additive fog on top of a colour sample.
/// Compatible with Unity's built-in UNITY_TRANSFER_FOG / UNITY_FOG_COORDS macros.
#define ApplyFog(color, i) \
    ({ fixed4 _fogColor = (color); UNITY_APPLY_FOG(i.fogCoord, _fogColor); _fogColor; })

/// Linearises a depth buffer value to a [0, 1] range in view space.
/// @param rawDepth   Raw depth value from the depth buffer.
/// @param nearPlane  Camera near-clip distance.
/// @param farPlane   Camera far-clip distance.
inline float LinearizeDepth(float rawDepth, float nearPlane, float farPlane)
{
    return (2.0 * nearPlane) / (farPlane + nearPlane - rawDepth * (farPlane - nearPlane));
}

/// Converts a world-space normal to view space.
/// @param worldNormal  Normalised normal in world space.
inline float3 WorldToViewNormal(float3 worldNormal)
{
    return normalize(mul((float3x3)UNITY_MATRIX_V, worldNormal));
}

/// Remaps a value from one range [inMin, inMax] to another [outMin, outMax].
inline float Remap(float value, float inMin, float inMax, float outMin, float outMax)
{
    return outMin + (value - inMin) * (outMax - outMin) / (inMax - inMin);
}

#endif // EXAMPLE_INCLUDE_CGINC

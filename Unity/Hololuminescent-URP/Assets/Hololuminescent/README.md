# Planar Reflection for Unity URP

A high-performance planar reflection system for Unity's Universal Render Pipeline with proper oblique projection and culling.

## Features

- **URP Integration**: Full compatibility with Universal Render Pipeline
- **Oblique Projection**: Proper near-plane clipping for accurate reflections
- **Optimized Rendering**: Configurable texture resolution and layer masking
- **Real-time Updates**: Automatic reflection updates for dynamic scenes
- **Fresnel Effect**: Physically-based reflection falloff
- **Normal Distortion**: Surface wave/ripple effects for water
- **Multi-Camera Support**: Works with game and scene view cameras

## Setup Instructions

### 1. Material Setup

1. Create a new material in Unity
2. Assign the `Custom/URP/PlanarReflection` shader
3. Configure material properties:
   - **Base Color**: Underlying surface color
   - **Reflection Strength**: Overall reflection intensity (0-1)
   - **Smoothness**: Surface smoothness for PBR lighting
   - **Fresnel Power**: Controls reflection falloff at grazing angles
   - **Distortion Strength**: Wave distortion amount (for water surfaces)
   - **Normal Map**: Optional normal map for surface detail

### 2. GameObject Setup

1. Create a plane or quad mesh for your reflective surface
2. Apply the material from step 1
3. Add the `PlanarReflection` component to the GameObject
4. Configure component settings:

#### Reflection Settings
- **Reflection Layers**: Which layers should be visible in reflection
- **Texture Size**: Reflection render texture resolution (power of 2)
  - 256: Low quality, high performance
  - 512: Medium quality (recommended)
  - 1024: High quality
  - 2048: Ultra quality, performance intensive
- **Clip Plane Offset**: Adjusts near plane to prevent artifacts (0.01-0.1)

#### Rendering Settings
- **Use Occlusion Culling**: Enable for better performance
- **Far Clip Plane**: Maximum reflection distance
- **Clear Color**: Background color for reflection camera

## Performance Optimization

### Layer Masking
Exclude non-essential layers from reflection:
```csharp
// In Inspector: Uncheck UI, particles, effects layers
reflectionLayers = LayerMask.GetMask("Default", "Environment", "Characters");
```

### Resolution Scaling
Adjust texture size based on surface size:
- Small puddles: 256x256
- Medium mirrors/water: 512x512
- Large water bodies: 1024x1024
- Hero water surface: 2048x2048

### LOD Considerations
The reflection camera respects your LOD settings, so distant objects will use lower detail meshes automatically.

## Technical Details

### Reflection Matrix Calculation
The script calculates a reflection matrix based on the plane's position and normal:
- Mirrors the camera position across the plane
- Inverts the view frustum
- Applies oblique near-plane clipping to prevent artifacts

### Oblique Projection
Uses oblique projection matrix to clip geometry below the reflection plane without rendering unnecessary objects.

### Culling Inversion
Inverts face culling during reflection rendering to maintain correct face visibility after mirroring.

## Common Use Cases

### Water Surface
```csharp
// Recommended settings for water:
textureSize = 512;
clipPlaneOffset = 0.07f;
optimizeForWaterSurface = true;
farClipPlane = 500f;

// Material settings:
Reflection Strength = 0.5-0.8
Fresnel Power = 2-3
Distortion Strength = 0.01-0.03
```

### Mirror Surface
```csharp
// Recommended settings for mirrors:
textureSize = 1024;
clipPlaneOffset = 0.01f;
optimizeForWaterSurface = false;
farClipPlane = 1000f;

// Material settings:
Reflection Strength = 0.9-1.0
Fresnel Power = 1.0
Distortion Strength = 0.0
```

### Polished Floor
```csharp
// Recommended settings for floors:
textureSize = 512;
clipPlaneOffset = 0.05f;
farClipPlane = 200f;

// Material settings:
Reflection Strength = 0.3-0.5
Fresnel Power = 3-5
Distortion Strength = 0.0
```

## Troubleshooting

### Artifacts at Surface Edge
- Increase `clipPlaneOffset` (try 0.07-0.1)
- Ensure plane mesh is perfectly flat

### Low Performance
- Reduce `textureSize`
- Limit `reflectionLayers` to essential objects
- Reduce `farClipPlane`
- Disable shadows on reflection camera (already done)

### Reflection Doesn't Update
- Ensure the component is enabled
- Check that the material uses the correct shader
- Verify reflection layers include visible objects

### Recursive Rendering
- The script includes protection against recursive rendering
- If issues occur, check for multiple reflection surfaces viewing each other

## Advanced Customization

### Custom Shader Properties
You can extend the shader to add:
- Refraction effects
- Underwater fog
- Caustics
- Color tinting
- Depth-based transparency

### Custom Filtering
Modify the `CopyCameraProperties` method to customize camera settings:
```csharp
// Example: Force specific clear flags
dest.clearFlags = CameraClearFlags.SolidColor;
dest.backgroundColor = Color.blue;
```

## Requirements

- Unity 2021.3 or newer
- Universal Render Pipeline 12.0 or newer
- Shader Model 4.5+

## Notes

- The reflection camera is created at runtime and hidden in hierarchy
- Render textures are automatically cleaned up on disable/destroy
- Works in both game view and scene view
- Compatible with post-processing stack

# SDF Cube Mask - HDRP Post Process Effect

A simple HDRP post-processing effect that renders the scene normally inside a cube volume, and fades to white (or any color) outside.

## Setup

### 1. Add Files to Project
Copy the files to your Unity project:
```
Assets/
├── SDFCubeMask/
│   ├── SDFCubeMaskVolume.cs
│   ├── SDFCubeMask.shader
│   ├── SDFCubeMaskGizmo.cs
│   └── Editor/
│       └── SDFCubeMaskGizmoEditor.cs
```

### 2. Register the Custom Post Process

In your **HDRP Global Settings** (or per-quality HDRP Asset):

1. Go to `Edit > Project Settings > Graphics > HDRP Global Settings`
2. Scroll to **Custom Post Process Orders**
3. Under **After Post Process**, click **+** and add `SDFCubeMaskVolume`

### 3. Add to Volume Profile

1. Select your Global Volume (or create one: `GameObject > Volume > Global Volume`)
2. In the Volume component, click **Add Override**
3. Select `Post-processing > SDF Cube Mask`
4. Enable the effect and configure:
   - **Cube Center**: World position of the cube center
   - **Cube Size**: Dimensions of the cube (X, Y, Z)
   - **Edge Fade**: How soft the transition is (in world units)
   - **Outside Color**: Color outside the cube (default: white)

### 4. Add the Gizmo (Optional but Recommended)

1. Create an empty GameObject in your scene
2. Add the `SDFCubeMaskGizmo` component
3. Position it where you want the cube center
4. Adjust `Cube Size` and `Edge Fade` - they auto-sync to the Volume

## Usage Tips

- The gizmo shows two wireframes: cyan for the main cube, yellow for the fade boundary
- Use the cube handles in Scene view to resize interactively
- Set `Edge Fade` to 0 for a hard edge
- The sky is always rendered as the outside color

## Parameters

| Parameter | Description |
|-----------|-------------|
| Enabled | Toggle the effect on/off |
| Cube Center | World-space center of the visible region |
| Cube Size | Full dimensions of the cube (not half-extents) |
| Edge Fade | Distance over which the scene fades to outside color |
| Outside Color | What to show outside the cube (default white) |

## How It Works

1. Reconstructs world position from depth buffer
2. Calculates signed distance to box using standard SDF
3. Creates a smooth mask based on distance and fade parameter
4. Lerps between scene color and outside color based on mask

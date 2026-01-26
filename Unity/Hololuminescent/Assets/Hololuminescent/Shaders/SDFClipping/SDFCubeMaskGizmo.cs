using UnityEngine;

[ExecuteAlways]
public class SDFCubeMaskGizmo : MonoBehaviour
{
    [Header("Cube Settings")]
    public Vector3 cubeSize = Vector3.one * 10f;
    
    [Header("Gizmo Appearance")]
    public Color wireColor = Color.cyan;
    public Color faceColor = new Color(0f, 1f, 1f, 0.1f);
    
    [Header("Edge Fade Preview")]
    public float edgeFade = 0.5f;
    public Color fadeColor = new Color(1f, 1f, 0f, 0.05f);
    
    [Header("Sync with Volume")]
    public bool autoSyncToVolume = true;

    void OnDrawGizmos()
    {
        DrawCubeGizmo(false);
    }

    void OnDrawGizmosSelected()
    {
        DrawCubeGizmo(true);
    }

    void DrawCubeGizmo(bool selected)
    {
        Vector3 center = transform.position;
        
        // Draw transparent faces
        Gizmos.color = selected ? faceColor * 2f : faceColor;
        Gizmos.DrawCube(center, cubeSize);
        
        // Draw wire edges
        Gizmos.color = selected ? wireColor : wireColor * 0.7f;
        Gizmos.DrawWireCube(center, cubeSize);
        
        // Draw fade region (outer boundary)
        if (edgeFade > 0.001f)
        {
            Gizmos.color = fadeColor;
            Vector3 fadeSize = cubeSize + Vector3.one * edgeFade * 2f;
            Gizmos.DrawCube(center, fadeSize);
            
            Gizmos.color = selected ? Color.yellow : Color.yellow * 0.5f;
            Gizmos.DrawWireCube(center, fadeSize);
        }
    }

    void Update()
    {
        if (autoSyncToVolume)
        {
            SyncToVolume();
        }
    }

    void SyncToVolume()
    {
        #if UNITY_EDITOR
        // Find the volume in the scene and sync values
        var volumes = FindObjectsByType<UnityEngine.Rendering.Volume>(FindObjectsSortMode.None);
        foreach (var volume in volumes)
        {
            if (volume.profile != null && 
                volume.profile.TryGet<SDFCubeMaskVolume>(out var maskVolume))
            {
                maskVolume.cubeCenter.value = transform.position;
                maskVolume.cubeSize.value = cubeSize;
                maskVolume.edgeFade.value = edgeFade;
            }
        }
        #endif
    }
}

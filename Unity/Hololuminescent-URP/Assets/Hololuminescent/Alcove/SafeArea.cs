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

    private void OnDrawGizmos()
    {
        DrawCubeGizmo(false);
    }

    private void OnDrawGizmosSelected()
    {
        DrawCubeGizmo(true);
    }

    private void DrawCubeGizmo(bool selected)
    {
        var center = transform.position;

        // Transparent faces
        Gizmos.color = selected ? faceColor * 2f : faceColor;
        Gizmos.DrawCube(center, cubeSize);

        // Wire edges
        Gizmos.color = selected ? wireColor : wireColor * 0.7f;
        Gizmos.DrawWireCube(center, cubeSize);

        // Fade region
        if (edgeFade > 0.001f)
        {
            var fadeSize = cubeSize + Vector3.one * (edgeFade * 2f);

            Gizmos.color = fadeColor;
            Gizmos.DrawCube(center, fadeSize);

            Gizmos.color = selected ? Color.yellow : Color.yellow * 0.5f;
            Gizmos.DrawWireCube(center, fadeSize);
        }
    }
}

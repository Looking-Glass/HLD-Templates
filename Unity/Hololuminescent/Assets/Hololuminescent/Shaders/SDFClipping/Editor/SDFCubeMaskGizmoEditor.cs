using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SDFCubeMaskGizmo))]
public class SDFCubeMaskGizmoEditor : Editor
{
    void OnSceneGUI()
    {
        SDFCubeMaskGizmo gizmo = (SDFCubeMaskGizmo)target;
        
        // Draw handles for resizing the cube
        EditorGUI.BeginChangeCheck();
        
        Vector3 center = gizmo.transform.position;
        Vector3 size = gizmo.cubeSize;
        
        // Use box bounds handle for intuitive editing
        Handles.color = gizmo.wireColor;
        
        // Draw face handles
        Vector3 newSize = size;
        
        // X axis handles
        Vector3 xPosHandle = center + Vector3.right * size.x * 0.5f;
        Vector3 xNegHandle = center - Vector3.right * size.x * 0.5f;
        
        float handleSize = HandleUtility.GetHandleSize(center) * 0.1f;
        
        EditorGUI.BeginChangeCheck();
        Vector3 newXPos = Handles.Slider(xPosHandle, Vector3.right, handleSize, Handles.CubeHandleCap, 0.01f);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(gizmo, "Resize SDF Cube");
            newSize.x = (newXPos.x - center.x) * 2f;
        }
        
        EditorGUI.BeginChangeCheck();
        Vector3 newXNeg = Handles.Slider(xNegHandle, -Vector3.right, handleSize, Handles.CubeHandleCap, 0.01f);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(gizmo, "Resize SDF Cube");
            newSize.x = (center.x - newXNeg.x) * 2f;
        }
        
        // Y axis handles
        Vector3 yPosHandle = center + Vector3.up * size.y * 0.5f;
        Vector3 yNegHandle = center - Vector3.up * size.y * 0.5f;
        
        EditorGUI.BeginChangeCheck();
        Vector3 newYPos = Handles.Slider(yPosHandle, Vector3.up, handleSize, Handles.CubeHandleCap, 0.01f);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(gizmo, "Resize SDF Cube");
            newSize.y = (newYPos.y - center.y) * 2f;
        }
        
        EditorGUI.BeginChangeCheck();
        Vector3 newYNeg = Handles.Slider(yNegHandle, -Vector3.up, handleSize, Handles.CubeHandleCap, 0.01f);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(gizmo, "Resize SDF Cube");
            newSize.y = (center.y - newYNeg.y) * 2f;
        }
        
        // Z axis handles
        Vector3 zPosHandle = center + Vector3.forward * size.z * 0.5f;
        Vector3 zNegHandle = center - Vector3.forward * size.z * 0.5f;
        
        EditorGUI.BeginChangeCheck();
        Vector3 newZPos = Handles.Slider(zPosHandle, Vector3.forward, handleSize, Handles.CubeHandleCap, 0.01f);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(gizmo, "Resize SDF Cube");
            newSize.z = (newZPos.z - center.z) * 2f;
        }
        
        EditorGUI.BeginChangeCheck();
        Vector3 newZNeg = Handles.Slider(zNegHandle, -Vector3.forward, handleSize, Handles.CubeHandleCap, 0.01f);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(gizmo, "Resize SDF Cube");
            newSize.z = (center.z - newZNeg.z) * 2f;
        }
        
        // Clamp to positive values
        newSize = Vector3.Max(newSize, Vector3.one * 0.1f);
        
        if (gizmo.cubeSize != newSize)
        {
            gizmo.cubeSize = newSize;
            EditorUtility.SetDirty(gizmo);
        }
        
        // Draw edge fade distance as dashed lines at corners
        if (gizmo.edgeFade > 0.001f)
        {
            Handles.color = Color.yellow * 0.8f;
            Vector3 corner = center + size * 0.5f;
            Vector3 fadeCorner = corner + Vector3.one * gizmo.edgeFade;
            Handles.DrawDottedLine(corner, fadeCorner, 2f);
        }
    }
}

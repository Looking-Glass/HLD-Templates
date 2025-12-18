using UnityEngine;

[ExecuteInEditMode]
public class PrefabColorChanger : MonoBehaviour
{
    public Color baseColor = Color.white;

    private Renderer[] renderers;

    private void OnEnable()
    {
        renderers = GetComponentsInChildren<Renderer>();
    }

    private void OnValidate()
    {
        ApplyColor(baseColor);
    }

    private void ApplyColor(Color color)
    {
        foreach (var renderer in GetComponentsInChildren<Renderer>())
        {
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);

            if (renderer.sharedMaterial.HasProperty("_BaseColor"))
            {
                block.SetColor("_BaseColor", color);
            }

            if (renderer.sharedMaterial.HasProperty("_UnlitColor"))
            {
                block.SetColor("_UnlitColor", color);
            }

            renderer.SetPropertyBlock(block);
        }
    }

    [ContextMenu("Reset to Original")]
    public void ResetToOriginal()
    {
        foreach (var renderer in GetComponentsInChildren<Renderer>())
        {
            renderer.SetPropertyBlock(null);
        }

        baseColor = Color.white;
    }
}
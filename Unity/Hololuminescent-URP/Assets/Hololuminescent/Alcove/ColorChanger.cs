using UnityEngine;

[ExecuteAlways]
public class PrefabColorChanger : MonoBehaviour
{
    public Color baseColor = Color.white;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int UnlitColorId = Shader.PropertyToID("_UnlitColor");

    private Renderer[] _renderers;
    private MaterialPropertyBlock _block;

    private void OnEnable()
    {
        CacheRenderers();
        ApplyColor();
    }

    private void OnValidate()
    {
        if (!isActiveAndEnabled) return;
        ApplyColor();
    }

    private void CacheRenderers()
    {
        _renderers = GetComponentsInChildren<Renderer>();
        _block ??= new MaterialPropertyBlock();
    }

    private void ApplyColor()
    {
        if (_renderers == null || _block == null) CacheRenderers();

        foreach (var rend in _renderers)
        {
            if (rend == null || rend.sharedMaterial == null) continue;

            rend.GetPropertyBlock(_block);

            if (rend.sharedMaterial.HasProperty(BaseColorId))
                _block.SetColor(BaseColorId, baseColor);

            if (rend.sharedMaterial.HasProperty(UnlitColorId))
                _block.SetColor(UnlitColorId, baseColor);

            rend.SetPropertyBlock(_block);
        }
    }

    [ContextMenu("Reset to Original")]
    public void ResetToOriginal()
    {
        if (_renderers == null) CacheRenderers();

        foreach (var rend in _renderers)
        {
            if (rend != null) rend.SetPropertyBlock(null);
        }

        baseColor = Color.white;
    }

    [ContextMenu("Refresh Renderers")]
    public void RefreshRenderers()
    {
        CacheRenderers();
        ApplyColor();
    }
}
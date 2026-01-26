using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using System;

[Serializable, VolumeComponentMenu("Post-processing/SDF Cube Mask")]
public sealed class SDFCubeMaskVolume : CustomPostProcessVolumeComponent, IPostProcessComponent
{
    public BoolParameter enabled = new BoolParameter(false);
    public Vector3Parameter cubeCenter = new Vector3Parameter(Vector3.zero);
    public Vector3Parameter cubeSize = new Vector3Parameter(Vector3.one * 10f);
    public ClampedFloatParameter edgeFade = new ClampedFloatParameter(0.5f, 0f, 5f);
    public ColorParameter outsideColor = new ColorParameter(Color.white);

    Material m_Material;

    public bool IsActive() => enabled.value && m_Material != null;

    public override CustomPostProcessInjectionPoint injectionPoint => CustomPostProcessInjectionPoint.AfterPostProcess;

    public override void Setup()
    {
        if (Shader.Find("Hidden/PostProcess/SDFCubeMask") != null)
            m_Material = new Material(Shader.Find("Hidden/PostProcess/SDFCubeMask"));
    }

    public override void Render(CommandBuffer cmd, HDCamera camera, RTHandle source, RTHandle destination)
    {
        if (m_Material == null)
            return;

        m_Material.SetVector("_CubeCenter", cubeCenter.value);
        m_Material.SetVector("_CubeSize", cubeSize.value);
        m_Material.SetFloat("_EdgeFade", edgeFade.value);
        m_Material.SetColor("_OutsideColor", outsideColor.value);
        m_Material.SetTexture("_InputTexture", source);

        HDUtils.DrawFullScreen(cmd, m_Material, destination);
    }

    public override void Cleanup()
    {
        CoreUtils.Destroy(m_Material);
    }
}

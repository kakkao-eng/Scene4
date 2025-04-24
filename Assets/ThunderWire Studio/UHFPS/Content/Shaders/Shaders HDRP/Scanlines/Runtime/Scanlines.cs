using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UHFPS.Rendering
{
    [Serializable, VolumeComponentMenu("ThunderWire/Scanlines")]
    public sealed class Scanlines : CustomPostProcessVolumeComponent, IPostProcessComponent
    {
        public ClampedFloatParameter ScanlinesStrength = new(0f, 0f, 2f);
        public ClampedFloatParameter ScanlinesSharpness = new(1.5f, 0f, 5f);
        public ClampedFloatParameter ScanlinesScroll = new(2f, 0f, 5f);
        public FloatParameter ScanlinesFrequency = new(5);

        public FloatParameter GlitchIntensity = new(0);
        public FloatParameter GlitchFrequency = new(0);

        Material m_Material;

        public bool IsActive() => m_Material != null && ScanlinesStrength.value > 0;

        public override CustomPostProcessInjectionPoint injectionPoint => CustomPostProcessInjectionPoint.AfterPostProcess;

        public override void Setup()
        {
            m_Material = CoreUtils.CreateEngineMaterial("Shader Graphs/ScanlinesBlit");
        }

        public override void Render(CommandBuffer cmd, HDCamera camera, RTHandle source, RTHandle destination)
        {
            if (!IsActive())
                return;

            m_Material.SetFloat("_ScanlinesStrength", ScanlinesStrength.value);
            m_Material.SetFloat("_ScanlinesSharpness", ScanlinesSharpness.value);
            m_Material.SetFloat("_ScanlinesScroll", ScanlinesScroll.value);
            m_Material.SetFloat("_ScanlinesFrequency", ScanlinesFrequency.value);
            m_Material.SetFloat("_GlitchIntensity", GlitchIntensity.value);
            m_Material.SetFloat("_GlitchFrequency", GlitchFrequency.value);

            HDUtils.DrawFullScreen(cmd, m_Material, destination);
        }

        public override void Cleanup()
        {
            CoreUtils.Destroy(m_Material);
        }
    }
}
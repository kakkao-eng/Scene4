using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UHFPS.Rendering
{
    [Serializable, VolumeComponentMenu("ThunderWire/Eye Blink")]
    public sealed class EyeBlink : CustomPostProcessVolumeComponent, IPostProcessComponent
    {
        public ClampedFloatParameter Blink = new(0f, 0f, 1f);
        public ClampedFloatParameter VignetteOuterRing = new(0.4f, 0f, 1f);
        public ClampedFloatParameter VignetteInnerRing = new(0.5f, 0f, 1f);
        public ClampedFloatParameter VignetteAspectRatio = new(1f, 0f, 1f);

        Material m_Material;

        public bool IsActive() => m_Material != null && Blink.value > 0f;

        public override CustomPostProcessInjectionPoint injectionPoint => CustomPostProcessInjectionPoint.AfterPostProcess;

        public override void Setup()
        {
            m_Material = CoreUtils.CreateEngineMaterial("UHFPS/EyeBlink");
        }

        public override void Render(CommandBuffer cmd, HDCamera camera, RTHandle source, RTHandle destination)
        {
            if (!IsActive())
                return;

            m_Material.SetFloat("_Blink", Blink.value);
            m_Material.SetFloat("_VignetteOuterRing", VignetteOuterRing.value);
            m_Material.SetFloat("_VignetteInnerRing", VignetteInnerRing.value);
            m_Material.SetFloat("_VignetteAspectRatio", VignetteAspectRatio.value);

            m_Material.SetTexture("_InputTexture", source);
            HDUtils.DrawFullScreen(cmd, m_Material, destination);
        }

        public override void Cleanup()
        {
            CoreUtils.Destroy(m_Material);
        }
    }
}
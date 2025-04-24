using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UHFPS.Rendering
{
    [Serializable, VolumeComponentMenu("ThunderWire/Fear Tentacles")]
    public sealed class FearTentacles : CustomPostProcessVolumeComponent, IPostProcessComponent
    {
        public ClampedFloatParameter EffectFade = new(0, 0f, 1f);
        public ClampedFloatParameter TentaclesPosition = new(0, -0.2f, 0.2f);
        public ClampedFloatParameter LayerPosition = new(0, -2f, 2f);
        public ClampedFloatParameter VignetteStrength = new(0, 0f, 1f);
        public ClampedFloatParameter TentaclesSpeed = new(1f, 0.1f, 3f);
        public ClampedIntParameter Tentacles = new(20, 10, 50);
        public BoolParameter TopLayer = new(false);

        Material m_Material;
        float effectTime;

        public bool IsActive() => m_Material != null && EffectFade.value > 0f;

        public override CustomPostProcessInjectionPoint injectionPoint => CustomPostProcessInjectionPoint.AfterPostProcess;

        public override void Setup()
        {
            m_Material = CoreUtils.CreateEngineMaterial("UHFPS/FearTentacles");
        }

        public override void Render(CommandBuffer cmd, HDCamera camera, RTHandle source, RTHandle destination)
        {
            if (!IsActive())
                return;

            m_Material.SetFloat("_EffectTime", effectTime);
            m_Material.SetFloat("_EffectFade", EffectFade.value);
            m_Material.SetFloat("_TentaclesPosition", TentaclesPosition.value);
            m_Material.SetFloat("_LayerPosition", LayerPosition.value);
            m_Material.SetFloat("_VignetteStrength", VignetteStrength.value);
            m_Material.SetFloat("_NumOfTentacles", Tentacles.value);
            m_Material.SetInteger("_ShowLayer", TopLayer.value ? 1 : 0);
            effectTime += Time.deltaTime * TentaclesSpeed.value;

            m_Material.SetTexture("_InputTexture", source);
            HDUtils.DrawFullScreen(cmd, m_Material, destination);
        }

        public override void Cleanup()
        {
            CoreUtils.Destroy(m_Material);
        }
    }
}
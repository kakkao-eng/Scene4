using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UHFPS.Rendering
{
    [Serializable, VolumeComponentMenu("ThunderWire/Blood Distortion")]
    public sealed class BloodDisortion : CustomPostProcessVolumeComponent, IPostProcessComponent
    {
        private static readonly int k_BlendColor = Shader.PropertyToID("_BlendColor");
        private static readonly int k_OverlayColor = Shader.PropertyToID("_OverlayColor");
        private static readonly int k_BlendTexture = Shader.PropertyToID("_BlendTex");
        private static readonly int k_BumpTexture = Shader.PropertyToID("_BumpMap");
        private static readonly int k_BloodAmount = Shader.PropertyToID("_BloodAmount");
        private static readonly int k_BlendAmount = Shader.PropertyToID("_BlendAmount");
        private static readonly int k_EdgeSharpness = Shader.PropertyToID("_EdgeSharpness");
        private static readonly int k_Distortion = Shader.PropertyToID("_Distortion");

        public ColorParameter BlendColor = new(Color.white);
        public ColorParameter OverlayColor = new(Color.white);
        public Texture2DParameter BlendTexture = new(null);
        public Texture2DParameter BumpMap = new(null);

        public ClampedFloatParameter BloodAmount = new(0f, 0f, 1f);
        public NoInterpClampedFloatParameter MinBloodAmount = new(0f, 0f, 1f);
        public NoInterpClampedFloatParameter MaxBloodAmount = new(1f, 0f, 1f);
        public NoInterpClampedFloatParameter EdgeSharpness = new(0.5f, 0f, 1f);
        public NoInterpClampedFloatParameter Distortion = new(0.5f, 0f, 1f);

        Material m_Material;

        public bool IsActive() => m_Material != null 
            && BlendTexture.value != null 
            && BumpMap.value != null 
            && BloodAmount.value > 0;

        public override CustomPostProcessInjectionPoint injectionPoint => CustomPostProcessInjectionPoint.AfterPostProcess;

        public override void Setup()
        {
            m_Material = CoreUtils.CreateEngineMaterial("UHFPS/BloodDisortion");
        }

        public override void Render(CommandBuffer cmd, HDCamera camera, RTHandle source, RTHandle destination)
        {
            if (!IsActive())
                return;

            m_Material.SetColor(k_BlendColor, BlendColor.value);
            m_Material.SetColor(k_OverlayColor, OverlayColor.value);

            m_Material.SetTexture(k_BlendTexture, BlendTexture.value);
            m_Material.SetTexture(k_BlendTexture, BlendTexture.value);
            m_Material.SetTexture(k_BumpTexture, BumpMap.value);
            m_Material.SetFloat(k_EdgeSharpness, EdgeSharpness.value);
            m_Material.SetFloat(k_Distortion, Distortion.value);

            float minBlood = MinBloodAmount.value;
            float maxBlood = MaxBloodAmount.value;

            float bloodAmount = BloodAmount.value;
            m_Material.SetFloat(k_BloodAmount, bloodAmount);

            float blendAmount = Mathf.Clamp01(bloodAmount * (maxBlood - minBlood) + minBlood);
            m_Material.SetFloat(k_BlendAmount, blendAmount);

            m_Material.SetTexture("_InputTexture", source);
            HDUtils.DrawFullScreen(cmd, m_Material, destination);
        }

        public override void Cleanup()
        {
            CoreUtils.Destroy(m_Material);
        }
    }
}
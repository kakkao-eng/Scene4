using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UHFPS.Rendering
{
    [Serializable, VolumeComponentMenu("ThunderWire/Raindrop")]
    public sealed class Raindrop : CustomPostProcessVolumeComponent, IPostProcessComponent
    {
        public ClampedFloatParameter Raining = new(0f, 0f, 1f);
        public TextureParameter DropletsMask = new(null);
        public Vector2Parameter Tiling = new(Vector2.one);
        public ClampedFloatParameter Distortion = new(0.5f, 0f, 1f);
        public FloatParameter DistortionScale = new(10f);
        public ClampedFloatParameter TilingScale = new(1f, 0.1f, 2f);
        public ClampedFloatParameter GlobalRotation = new(0f, -180f, 180f);
        public ClampedFloatParameter DropletsGravity = new(0f, 0f, 1f);
        public ClampedFloatParameter DropletsSpeed = new(1f, 0f, 2f);
        public ClampedFloatParameter DropletsStrength = new(1f, 0f, 1f);

        Material m_Material;

        public bool IsActive() => m_Material != null 
            && DropletsMask != null
            && Raining.value > 0f;

        public override CustomPostProcessInjectionPoint injectionPoint => CustomPostProcessInjectionPoint.AfterPostProcess;

        public override void Setup()
        {
            m_Material = CoreUtils.CreateEngineMaterial("UHFPS/Raindrop");
        }

        public override void Render(CommandBuffer cmd, HDCamera camera, RTHandle source, RTHandle destination)
        {
            if (!IsActive())
                return;

            Vector2 tiling = Tiling.value;
            float tilingScale = TilingScale.value;
            tiling *= tilingScale;

            m_Material.SetFloat("_Raining", Raining.value);
            m_Material.SetTexture("_DropletsMask", DropletsMask.value);
            m_Material.SetVector("_Tiling", tiling);
            m_Material.SetFloat("_Distortion", Distortion.value);
            m_Material.SetFloat("_DistortionScale", DistortionScale.value);
            m_Material.SetFloat("_GlobalRotation", GlobalRotation.value);
            m_Material.SetFloat("_DropletsGravity", DropletsGravity.value);
            m_Material.SetFloat("_DropletsSpeed", DropletsSpeed.value);
            m_Material.SetFloat("_DropletsStrength", DropletsStrength.value);

            m_Material.SetTexture("_InputTexture", source);
            HDUtils.DrawFullScreen(cmd, m_Material, destination);
        }

        public override void Cleanup()
        {
            CoreUtils.Destroy(m_Material);
        }
    }
}
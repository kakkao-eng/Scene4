using UnityEngine;
using ThunderWire.Attributes;
using Newtonsoft.Json.Linq;
using UHFPS.Tools;

namespace UHFPS.Runtime
{
    [InspectorHeader("Simple Light")]
    public class SimpleLight : MonoBehaviour, ISaveable
    {
        public Light Light;

        [Header("Settings")]
        public bool UseEmission = true;
        public bool LightState = false;

        [Header("Emission")]
        public RendererMaterial LightRenderer;
        public string EmissionKeyword = "_EmissiveColor";
        public float EmissionIntensity = 2f;

        private void Awake()
        {
            if (LightState && !SaveGameManager.GameWillLoad) 
                SetLightState(true);
        }

        public void SetLightState(bool state)
        {
            if (state)
            {
                if (Light) Light.enabled = true;
                if (UseEmission) LightRenderer.ClonedMaterial.EnableKeyword(EmissionKeyword);
            }
            else
            {
                if (Light) Light.enabled = false;
                if (UseEmission) LightRenderer.ClonedMaterial.DisableKeyword(EmissionKeyword);
            }

            if (Light) Light.enabled = state;
            if (UseEmission) LightRenderer.ClonedMaterial.SetEmission(state, EmissionIntensity, EmissionKeyword);
            LightState = state;
        }

        public StorableCollection OnSave()
        {
            return new StorableCollection()
            {
                { "lightState", LightState }
            };
        }

        public void OnLoad(JToken data)
        {
            bool lightState = (bool)data["lightState"];
            SetLightState(lightState);
        }
    }
}
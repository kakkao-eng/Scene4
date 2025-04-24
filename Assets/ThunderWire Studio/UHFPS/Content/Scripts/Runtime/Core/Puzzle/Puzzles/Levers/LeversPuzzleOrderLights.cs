using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json.Linq;
using UHFPS.Tools;

namespace UHFPS.Runtime
{
    public class LeversPuzzleOrderLights : MonoBehaviour, ISaveable
    {
        [Serializable]
        public sealed class OrderLight
        {
            public Light Light;
            public RendererMaterial LightMaterial;
            [HideInInspector]
            public bool IsEnabled;
        }

        public LeversPuzzle LeversPuzzle;
        public List<OrderLight> OrderLights = new();
        public string EmissionKeyword = "_EmissiveColor";
        public float EmissionIntensity = 1f;
        public int OrderIndex = 0;

        public void OnSetLever()
        {
            if (OrderIndex < LeversPuzzle.Levers.Count)
                SetLightState(OrderLights[OrderIndex++], true);
        }

        public void ResetLights()
        {
            foreach (var item in OrderLights)
            {
                SetLightState(item, false);
            }

            OrderIndex = 0;
        }

        private void SetLightState(OrderLight light, bool state)
        {
            if(light.Light != null)
                light.Light.enabled = state;

            light.LightMaterial.ClonedMaterial.SetEmission(state, EmissionIntensity, EmissionKeyword);
            light.IsEnabled = state;
        }

        public StorableCollection OnSave()
        {
            StorableCollection storableCollection = new StorableCollection();

            for (int i = 0; i < OrderLights.Count; i++)
            {
                string name = "light_" + i;
                storableCollection.Add(name, OrderLights[i].IsEnabled);
            }

            storableCollection.Add("orderIndex", OrderIndex);
            return storableCollection;
        }

        public void OnLoad(JToken data)
        {
            for (int i = 0; i < OrderLights.Count; i++)
            {
                string name = "light_" + i;
                bool lightState = (bool)data[name];
                SetLightState(OrderLights[i], lightState);
            }

            OrderIndex = (int)data["orderIndex"];
        }
    }
}
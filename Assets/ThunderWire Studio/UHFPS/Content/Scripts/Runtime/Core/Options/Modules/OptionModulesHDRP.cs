using System;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json.Linq;
using UnityEngine.Rendering.HighDefinition;
using static UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData;

namespace UHFPS.Runtime
{
    [Serializable]
    // 0 - None, 1 - FXAA, 2 - TAA, 3 - SMAA
    public class OptionAntialiasingMode : OptionModule
    {
        public override string ContextName => "HDRP/Antialiasing Mode";

        public GString[] AntialiasingModes =
        {
            new("*None", ""),
            new("*FXAA", ""),
            new("*TAA", ""),
            new("*SMAA", ""),
        };

        public override void OnApplyOption()
        {
            int antialiasing = (int)Value;
            if (IsChanged)
            {
                AntialiasingMode mode = (AntialiasingMode)antialiasing;
                Options.CameraData.antialiasing = mode;
            }

            Options.SerializableData[Name] = new(antialiasing);
        }

        public override void OnLoadOption(bool fromFile)
        {
            if (fromFile && CheckOption(JTokenType.Integer, out int antialiasing))
            {
                Options.CameraData.antialiasing = (AntialiasingMode)antialiasing;
                Behaviour.SetOptionValue(antialiasing);
                return;
            }

            AntialiasingMode mode = Options.CameraData.antialiasing;
            Behaviour.SetOptionValue((int)mode);
        }

        public override void OnBuildOption(OptionBehaviour behaviour)
        {
            behaviour.SetOptionData(new StorableCollection() { { "options", AntialiasingModes } });
        }
    }

    [Serializable]
    // 0 - Low, 1 - Medium, 2 - High
    // Only TAA, SMAA
    public class OptionAntialiasingQuality : OptionModule
    {
        public override string ContextName => "HDRP/Antialiasing Quality";

        public GString[] QualityLevels =
{
            new("*Low", ""),
            new("*Medium", ""),
            new("*High", ""),
        };

        public override void OnApplyOption()
        {
            int quality = (int)Value;
            if (IsChanged && quality >= 0)
            {
                if (Options.CameraData.antialiasing == AntialiasingMode.TemporalAntialiasing)
                {
                    Options.CameraData.TAAQuality = (TAAQualityLevel)quality;
                }
                else if (Options.CameraData.antialiasing == AntialiasingMode.SubpixelMorphologicalAntiAliasing)
                {
                    Options.CameraData.SMAAQuality = (SMAAQualityLevel)quality;
                }
                else if (Options.CameraData.antialiasing == AntialiasingMode.FastApproximateAntialiasing)
                {
                    quality = -1;
                }
            }

            Options.SerializableData[Name] = new(quality);
        }

        public override void OnLoadOption(bool fromFile)
        {
            if (fromFile && CheckOption(JTokenType.Integer, out int value))
            {
                if (value >= 0)
                {
                    if (Options.CameraData.antialiasing == AntialiasingMode.TemporalAntialiasing)
                        Options.CameraData.TAAQuality = (TAAQualityLevel)value;
                    else if (Options.CameraData.antialiasing == AntialiasingMode.SubpixelMorphologicalAntiAliasing)
                        Options.CameraData.SMAAQuality = (SMAAQualityLevel)value;
                    Behaviour.SetOptionValue(value);
                }
                return;
            }

            int quality = 0;
            if (Options.CameraData.antialiasing == AntialiasingMode.TemporalAntialiasing)
                quality = (int)Options.CameraData.TAAQuality;
            else if (Options.CameraData.antialiasing == AntialiasingMode.SubpixelMorphologicalAntiAliasing)
                quality = (int)Options.CameraData.SMAAQuality;

            Behaviour.SetOptionValue(quality);
        }

        public override void OnBuildOption(OptionBehaviour behaviour)
        {
            behaviour.SetOptionData(new StorableCollection() { { "options", QualityLevels } });
        }
    }

    [Serializable]
    // 0 - 0m (Disabled), 1 - 25m (Very Low), 2 - 40m (Low), 3 - 55m (Medium), 4 - 70m (High), 5 - 85m (Very High), 6 - 100m (Max)
    public class OptionShadowDistance : OptionModule
    {
        public override string ContextName => "HDRP/ShadowDistance";

        public NameValue<float>[] ShadowDistances =
        {
            new("*Disabled", 0f),
            new("*Very Low", 25f),
            new("*Low", 40f),
            new("*Medium", 55f),
            new("*High", 70f),
            new("*Very High", 85f),
            new("*Max", 100f)
        };

        public override void OnApplyOption()
        {
            int value = (int)Value;
            if (IsChanged && value >= 0 && Options.GlobalVolume != null && Options.GlobalVolume.profile.TryGet(out HDShadowSettings shadows))
            {
                float distance = ShadowDistances[value].Value;
                shadows.maxShadowDistance.value = distance;
            }

            Options.SerializableData[Name] = new(value);
        }

        public override void OnLoadOption(bool fromFile)
        {
            HDShadowSettings shadows = null;
            if (Options.GlobalVolume != null) Options.GlobalVolume.profile.TryGet(out shadows);

            if (fromFile && CheckOption(JTokenType.Integer, out int value))
            {
                if (shadows != null)
                {
                    value = Mathf.Clamp(value, 0, 6);
                    float distance = ShadowDistances[value].Value;
                    shadows.maxShadowDistance.value = distance;
                }

                Behaviour.SetOptionValue(value);
                return;
            }

            if (shadows != null)
            {
                float shadowDistance = shadows.maxShadowDistance.value;
                int distance = ClosestIndex(ShadowDistances, (int)shadowDistance);
                Behaviour.SetOptionValue(distance);
            }
        }

        public override void OnBuildOption(OptionBehaviour behaviour)
        {
            GString[] gStrings = ShadowDistances.Select(x => x.Name).ToArray();
            behaviour.SetOptionData(new StorableCollection() { { "options", gStrings } });
        }

        private int ClosestIndex(NameValue<float>[] array, float targetValue)
        {
            int closestIndex = -1;
            float smallestDifference = float.MaxValue;

            for (int i = 0; i < array.Length; i++)
            {
                float difference = Mathf.Abs(array[i].Value - targetValue);
                if (difference < smallestDifference)
                {
                    smallestDifference = difference;
                    closestIndex = i;
                }
            }

            return closestIndex;
        }
    }

    [Serializable]
    // Budget: 0 - 0.166, 1 - 0.333, 2 - 0.666
    public class OptionFogQuality : OptionModule
    {
        public override string ContextName => "HDRP/Fog Quality";

        public NameValue<float>[] FogBudgets =
{
            new("*Low", 0.166f),
            new("*Medium", 0.333f),
            new("*High", 0.666f)
        };

        public override void OnApplyOption()
        {
            int value = (int)Value;
            if (IsChanged && value >= 0 && Options.GlobalVolume != null && Options.GlobalVolume.profile.TryGet(out Fog fog))
            {
                float budget = FogBudgets[value].Value;
                fog.volumetricFogBudget = budget;
            }

            Options.SerializableData[Name] = new(value);
        }

        public override void OnLoadOption(bool fromFile)
        {
            Fog fog = null;
            if (Options.GlobalVolume != null) Options.GlobalVolume.profile.TryGet(out fog);

            if (fromFile && CheckOption(JTokenType.Integer, out int value))
            {
                if (fog != null)
                {
                    value = Mathf.Clamp(value, 0, 6);
                    float budget = FogBudgets[value].Value;
                    fog.volumetricFogBudget = budget;
                }

                Behaviour.SetOptionValue(value);
                return;
            }

            if (fog != null)
            {
                float budget = fog.volumetricFogBudget;
                int budgetInt = FogBudgets.ToList().FindIndex(x => x.Value == budget);
                Behaviour.SetOptionValue(budgetInt);
            }
        }

        public override void OnBuildOption(OptionBehaviour behaviour)
        {
            GString[] gStrings = FogBudgets.Select(x => x.Name).ToArray();
            behaviour.SetOptionData(new StorableCollection() { { "options", gStrings } });
        }
    }
}
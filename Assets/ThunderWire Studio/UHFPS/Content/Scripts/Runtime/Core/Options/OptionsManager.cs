using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UHFPS.Scriptable;
using UHFPS.Tools;
using UHFPS.Input;
using ThunderWire.Attributes;

namespace UHFPS.Runtime
{
    [Docs("https://docs.twgamesdev.com/uhfps/guides/options-manager")]
    public class OptionsManager : Singleton<OptionsManager>
    {
        public const char NAME_SEPARATOR = '.';
        public const string EXTENSION = ".json";

        public struct OptionLinkRef
        {
            public OptionLink OptionLink;
            public OptionItem OptionItem;
        }

        [Serializable]
        public struct OptionReference
        {
            public string Name;
            public string GUID;

            public OptionReference(string name, string guid)
            {
                Name = name;
                GUID = guid;
            }
        }

        [Serializable]
        public sealed class OptionItem
        {
            public GString Title;
            public OptionReference OptionReference;
            public OptionBehaviour OptionBehaviour;
        }

        [Serializable]
        public sealed class OptionLink
        {
            public Transform SectionParent;
            public OptionReference SectionReference;
            public List<OptionItem> OptionItems;
        }

        private SerializationAsset SerializationAsset => SerializationUtillity.SerializationAsset;
        private string OptionsFilename => SerializationAsset.OptionsFilename + EXTENSION;

        private string OptionsPath
        {
            get
            {
                string configPath = SerializationAsset.GetConfigPath();
                if (!Directory.Exists(configPath))
                    Directory.CreateDirectory(configPath);

                return configPath + "/" + OptionsFilename;
            }
        }

        public static bool ResolutionLoaded { get; private set; }
        public Dictionary<string, BehaviorSubject<object>> OptionSubjects { get; set; } = new();
        public Dictionary<string, JValue> SerializableData { get; set; } = new();

        public List<Resolution> Resolutions => resolutions.ToList();
        public List<DisplayInfo> DisplayInfos => displayInfos;

        public ObservableValue<DisplayInfo> CurrentDisplay { get; set; } = new();
        public ObservableValue<FullScreenMode> CurrentFullscreen { get; set; } = new();
        public ObservableValue<Resolution> CurrentResolution { get; set; } = new();

        public HDAdditionalCameraData CameraData
        {
            get
            {
                if (cameraData == null)
                {
                    if (PlayerPresenceManager.HasReference)
                    {
                        Camera playerCamera = PlayerPresenceManager.Instance.PlayerCamera;
                        cameraData = playerCamera.GetComponent<HDAdditionalCameraData>();
                    }
                    else
                    {
                        cameraData = Camera.main.GetComponent<HDAdditionalCameraData>();
                    }
                }

                return cameraData;
            }
        }

        private readonly CompositeDisposable disposables = new();

        public List<OptionLink> OptionLinks = new();
        public OptionsAsset OptionsAsset;
        public Volume GlobalVolume;

        public bool ApplyAndSaveInputs = true;
        public bool ShowDebug = true;

        private Resolution[] resolutions;
        private List<DisplayInfo> displayInfos;

        private OptionsAsset instance;
        private HDAdditionalCameraData cameraData;

        private void OnDestroy()
        {
            Destroy(instance);
            disposables.Dispose();
        }

        private void Awake()
        {
            instance = Instantiate(OptionsAsset);

            // initialize options instances
            for (int i = 0; i < instance.Sections.Count; i++)
            {
                var section = instance.Sections[i];
                for (int j = 0; section.Items.Count > j; j++)
                {
                    var module = section.Items[j];
                    var optionItem = OptionLinks[i].OptionItems[j];

                    module.Options = this;
                    module.Behaviour = optionItem.OptionBehaviour;

                    // register custom option observers
                    if(module is OptionCustomBoolean customBoolean)
                    {
                        OptionSubjects[module.Name] = new BehaviorSubject<object>(customBoolean.DefaultValue);
                    }
                    else if(module is OptionCustomInteger customInteger)
                    {
                        OptionSubjects[module.Name] = new BehaviorSubject<object>(customInteger.DefaultValue);
                    }
                    else if (module is OptionCustomFloat customFloat)
                    {
                        OptionSubjects[module.Name] = new BehaviorSubject<object>(customFloat.DefaultValue);
                    }
                }
            }
        }

        private void Start()
        {
            // get screen info
            resolutions = Screen.resolutions
                .GroupBy(res => new { res.width, res.height })
                .Select(g => g.OrderByDescending(res => res.refreshRateRatio.value).First())
                .Reverse().ToArray();

            displayInfos = new();
            Screen.GetDisplayLayout(displayInfos);

            // build runtime and load options
            BuildOptionsRuntime();
            LoadOptions();
        }

        private void BuildOptionsRuntime()
        {
            foreach (var section in instance.Sections)
            {
                foreach (var item in section.Items)
                {
                    item.OnBuildOptionRuntime();
                }
            }
        }

        private async void LoadOptions()
        {
            bool fromFile = File.Exists(OptionsPath);

            if (fromFile)
            {
                await DeserializeOptions();
                if (ShowDebug) Debug.Log("[OptionsManager] The options have been loaded.");
            }

            foreach (var section in instance.Sections)
            {
                foreach (var option in section.Items)
                {
                    option.OnLoadOption(fromFile);
                }
            }

            if (!ResolutionLoaded)
            {
                ApplyResolution();
                ResolutionLoaded = true;
            }
        }

        private void ApplyResolution()
        {
            int screenWidth = CurrentResolution.Value.width;
            int screenHeight = CurrentResolution.Value.height;
            var fullscreen = CurrentFullscreen.Value;

            if (CurrentResolution.IsChanged && CurrentFullscreen.IsChanged)
            {
                Screen.SetResolution(screenWidth, screenHeight, fullscreen);
            }
            else if (CurrentResolution.IsChanged)
            {
                Screen.SetResolution(screenWidth, screenHeight, Screen.fullScreenMode);
            }
            else if (CurrentFullscreen.IsChanged)
            {
                Screen.fullScreenMode = fullscreen;
            }
        }

        private void ClearOptions()
        {
            if (OptionLinks.Count > 0)
            {
                foreach (var section in OptionLinks)
                {
                    foreach (var item in section.OptionItems)
                    {
                        if (item.OptionBehaviour != null)
                        {
                            DestroyImmediate(item.OptionBehaviour.gameObject);
                            item.OptionBehaviour = null;
                        }
                    }
                }
            }
        }

        public static void ObserveOption(string name, Action<object> onChange)
        {
            if (Instance.OptionSubjects.TryGetValue(name, out var subject))
                subject.Subscribe(onChange).AddTo(Instance.disposables);
        }

        public void RefreshOptions()
        {
            Dictionary<string, Transform> _cache = new();
            foreach (var link in OptionLinks)
            {
                _cache.Add(link.SectionReference.GUID, link.SectionParent);
            }

            ClearOptions();
            OptionLinks = new();

            foreach (var section in OptionsAsset.Sections)
            {
                _cache.TryGetValue(section.Section.GUID, out Transform parent);
                OptionLinks.Add(new()
                {
                    SectionParent = parent,
                    SectionReference = new(section.Section.Name, section.Section.GUID),
                    OptionItems = section.Items.Select(x => new OptionItem()
                    {
                        Title = x.Title,
                        OptionReference = new(x.Name, x.GUID)
                    }).ToList()
                });
            }

            _cache.Clear();
        }

        public void BuildOptions()
        {
            ClearOptions();

            for (int i = 0; i < OptionsAsset.Sections.Count; i++)
            {
                var section = OptionsAsset.Sections[i];
                for (int j = 0; section.Items.Count > j; j++)
                {
                    var item = section.Items[j];
                    if (item.Prefab == null)
                        continue;

                    var optionLink = OptionLinks[i];
                    var optionItem = optionLink.OptionItems[j];

                    GameObject option = Instantiate(item.Prefab);
                    option.name = item.Name;

                    if (option.TryGetComponent(out OptionBehaviour behaviour))
                    {
                        optionItem.OptionBehaviour = behaviour;
                        item.OnBuildOption(behaviour);
                        option.transform.SetParent(optionLink.SectionParent);

                        if(behaviour.Title != null && optionItem.Title != null)
                        {
                            if (behaviour.Title.TryGetComponent(out GLocText gloc))
                                gloc.GlocKey.GlocText = optionItem.Title;
                        }

                        behaviour.Title.text = optionItem.OptionReference.Name;
                    }
                }
            }
        }

        public async void ApplyOptions()
        {
            foreach (var section in instance.Sections)
            {
                foreach (var option in section.Items)
                {
                    option.OnApplyOption();
                    option.Behaviour.IsChanged = false;
                }
            }

            ApplyResolution();
            await SerializeOptions();

            if (ApplyAndSaveInputs) InputManager.ApplyInputRebindOverrides();
            if (ShowDebug) Debug.Log($"[OptionsManager] The option values have been saved to '{OptionsFilename}'.");
        }

        public void DiscardChanges()
        {
            bool anyDiscard = false;
            foreach (var section in instance.Sections)
            {
                foreach (var option in section.Items)
                {
                    if (!option.Behaviour.IsChanged)
                        continue;

                    option.OnLoadOption(true);
                    anyDiscard = true;
                }
            }

            if (ApplyAndSaveInputs) InputManager.ResetInputsToDefaults();
            if (ShowDebug && anyDiscard) Debug.Log("[OptionsManager] Options Discarded");
        }

        private async Task SerializeOptions()
        {
            string json = JsonConvert.SerializeObject(SerializableData, Formatting.Indented, new JsonSerializerSettings()
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            });

            await File.WriteAllTextAsync(OptionsPath, json);
        }

        private async Task DeserializeOptions()
        {
            string json = await File.ReadAllTextAsync(OptionsPath);
            SerializableData = JsonConvert.DeserializeObject<Dictionary<string, JValue>>(json);
        }
    }
}
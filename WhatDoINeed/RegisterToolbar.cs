using KSP_Log;
using System;
using ToolbarControl_NS;
using UnityEngine;

namespace WhatDoINeed
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class RegisterToolbar : MonoBehaviour
    {
        internal static Log Log = null;

        static internal bool initted = false;

        public static Repository repository;

        internal static int numMissingExperiments = 0;
        internal static int numFillableContracts = 0;
        internal static int numUnfillableContracts = 0;
        internal static int numSelectedcontracts = 0;

        internal Texture2D minus = null, plus = null;

        internal static GUIContent minusIcon;
        internal static GUIContent plusIcon;

        internal static void DumpPartInfoList()
        {
            Log.Info("vvvvvvvvvvvvvvvv PartInfoList vvvvvvvvvvvvvvvv");
            foreach (var part in Repository.partInfoList)
            {
                Log.Info("Key: " + part.Key + ", partName: " + part.Value.partName + ", index: " + part.Value.index);
            }
            Log.Info("^^^^^^^^^^^^^^^^ PartInfoList ^^^^^^^^^^^^^^^^");
        }


        void Awake()
        {
            if (Log == null)
#if DEBUG
                Log = new Log("WhatDoINeed", Log.LEVEL.INFO);
            //Log = new Log("WhatDoINeed", Log.LEVEL.ERROR);
#else
                Log = new Log("WhatDoINeed", Log.LEVEL.ERROR);
#endif

            //DontDestroyOnLoad(this);
            Settings.Instance = new Settings();

            foreach (var n in Enum.GetNames(typeof(PartCategories)))
                Repository.partCategories[n] = new PartCategoryWrapper(n);

            // The following are the known module categories, after an exhaustive search of github
            Repository.contractObjectives["Antenna"] = 0;   // ModuleDataTransmitter, ModuleDataTransmitterFeedable
            Repository.contractObjectives["Generator"] = 0; // ModuleDeployableSolarPanel, ModuleGenerator
            Repository.contractObjectives["Dock"] = 0;      // ModuleDockingNode, FlexoTube
            Repository.contractObjectives["Grapple"] = 0;   // ModuleGrappleNode
            Repository.contractObjectives["Wheel"] = 0;     //ModuleWheelBase, KSPWheelMotor

            Repository.contractObjectives["Laboratory"] = 0;     //Kerbalism
            Repository.contractObjectives["Harvester"] = 0;     //Kerbalism
            Repository.contractObjectives["Greenhouse"] = 0;     //Kerbalism


            repository = new Repository();

        }

        void Start()
        {
            ToolbarControl.RegisterMod(WhatDoINeed.MODID, WhatDoINeed.MODNAME);
        }

        internal static void InitializePartsAvailability()
        {
            Log.Info("InitializePartsAvailability");
            foreach (var p in Repository.partInfoList.Values)
            {
                AvailablePart part = PartLoader.LoadedPartsList[p.index];

                p.isAvailable = ResearchAndDevelopment.PartModelPurchased(part) || ResearchAndDevelopment.IsExperimentalPart(part);
                Log.Info("part: " + p.partName + ", purchased: " + ResearchAndDevelopment.PartModelPurchased(part) +
                    ", IsExperimental: " + ResearchAndDevelopment.IsExperimentalPart(part) + ", isAvailable: " + p.isAvailable);
                p.isOnShip = false;
                foreach (var n in Enum.GetNames(typeof(PartCategories)))
                    Repository.partCategories[n] = new PartCategoryWrapper(n);
            }
        }


        internal static string processedModuleName(string moduleName)
        {
            // Following is because for some reason, there is no ModuleTrackBodies found, but Track Bodies is, for TarsierSpaceTechnology
            // 
            if (moduleName == "ModuleTrackBodies")
                return "Track Bodies";
            if (moduleName == "ModuleTripLogger")
                return null;
            return moduleName;
        }

        internal static void SetPartOnShip(Part part)
        {
            if (!Repository.partInfoList.ContainsKey(part.name))
            {
                //Log.Error("********************** vvvvvvvvvvvvvvvv *******************");
                Log.Error("SetPartOnShip, part not found: " + part.name);
                //foreach (var p in Repository.partInfoList)
                //    Log.Error("p: " + p.Key);
                //Log.Error("********************** ^^^^^^^^^^^^^^^^ *******************");
                return;
            }
            Repository.partInfoList[part.name].isOnShip = true;
            Repository.partInfoList[part.name].numAvailable++;
            if (!Repository.partCategories.ContainsKey(part.partInfo.category.ToString()))
            {
                Log.Error("SetPartOnShip, partCatagories missing key: " + part.partInfo.category.ToString());
                return;
            }
            Repository.partCategories[part.partInfo.category.ToString()].numAvailable++;


            Repository.contractObjectives["Antenna"] += part.HasValidContractObjective("Antenna") ? 1 : 0;
            Repository.contractObjectives["Generator"] += part.HasValidContractObjective("Generator") ? 1 : 0;
            Repository.contractObjectives["Dock"] += part.HasValidContractObjective("Dock") ? 1 : 0;

            Repository.contractObjectives["Grapple"] += part.HasValidContractObjective("Grapple") ? 1 : 0;
            Repository.contractObjectives["Wheel"] += part.HasValidContractObjective("Wheel") ? 1 : 0;

            Repository.contractObjectives["Laboratory"] += part.HasValidContractObjective("Laboratory") ? 1 : 0;
            Repository.contractObjectives["Harvester"] += part.HasValidContractObjective("Harvester") ? 1 : 0;
            Repository.contractObjectives["Greenhouse"] += part.HasValidContractObjective("Greenhouse") ? 1 : 0;


            for (int i = 0; i < part.Modules.Count; i++)
            {
                var m = part.Modules[i];
                var mname = processedModuleName(m.moduleName);
                if (mname != null)
                {
                    if (!Repository.moduleInformation.ContainsKey(mname))
                        Repository.moduleInformation[mname] = new ModuleInformation(mname);
                    Repository.moduleInformation[m.moduleName].numAvailable++;
                }

                switch (m.moduleName)
                {
                    case "ModuleEngines":
                        {
                            var module = m as ModuleEngines;
                            Repository.engineTypes[(int)module.engineType]++;
                            break;
                        }
                    case "ModuleEnginesFX":
                        {
                            var module = m as ModuleEnginesFX;
                            Repository.engineTypes[(int)module.engineType]++;
                            break;
                        }
                }
            }
        }

        internal static bool IsPartOnShip(string partName)
        {
            return Repository.partInfoList[partName].isOnShip;
        }

        internal static AvailablePart IsPartAvailable(string partName, bool ignoreShip = false)
        {
            if (Repository.partInfoList.ContainsKey(partName) && (Repository.partInfoList[partName].isAvailable || ignoreShip))
            {
                return PartLoader.LoadedPartsList[Repository.partInfoList[partName].index];
            }
            return null;
        }

        internal static bool CheckForModuleLaunchClamp(ConfigNode[] modules)
        {

            for (int j = 0; j < modules.Length; j++)
            {
                string name = null;
                modules[j].TryGetValue("name", ref name);
                if (name != null)
                    Log.Info("moduleName: " + name);
                if (name != null && name.Contains("LaunchClamp"))
                {
                    Log.Info("LaunchClamp found, bypassing ");
                    return true;      
                }
            }
            return false;
        }

        /// <summary>
        /// Create a dictionary of the loaded parts
        /// </summary>
        internal void LoadPartInfo()
        {
            //Log.Info("LoadPartInfo 1");
            ConfigNode[] modules;
            for (int i = 0; i < PartLoader.LoadedPartsList.Count; i++)
            {
                //Log.Info("LoadPartInfo, i: " + i + ", PartLoader.LoadedPartsList[i].name: " + PartLoader.LoadedPartsList[i].name);
                if (!PartLoader.LoadedPartsList[i].name.Contains("flag") &&
                    !PartLoader.LoadedPartsList[i].name.Contains("kerbal")) // &&
                                                                            //!PartLoader.LoadedPartsList[i].name.Contains("launchClamp1"))
                {
                    if (Repository.partInfoList.ContainsKey(PartLoader.LoadedPartsList[i].name))
                    {
                        Log.Error("Duplicate part name found: " + PartLoader.LoadedPartsList[i].name + ", ignoring and continuing");
                        continue;
                    }
                    else
                    {
                        // Launch clamps contain generators, but shouldn't be included in the check for a generator
                        // since it stays on the launch pad.  This sections looks at all the modules' names
                        // first searching for a module named LaunchClamp.  
                        // Needs to be done before any changes done to the repository
                        modules = PartLoader.LoadedPartsList[i].partConfig.GetNodes();
                        if (CheckForModuleLaunchClamp(modules))
                            continue;
                        Repository.partInfoList.Add(PartLoader.LoadedPartsList[i].name, new PartInformation(PartLoader.LoadedPartsList[i].name, i));
                    }
                    // Now continue with normal checks
                    for (int j = 0; j < modules.Length; j++)
                    {
                        string name = null;
                        modules[j].TryGetValue("name", ref name);

                        if (name != null)
                        {
                            if (!Repository.moduleInformation.ContainsKey(name))
                            {
                                // Following is because for some reason, there is no ModuleTrackBodies found, but Track Bodies is, for TarsierSpaceTechnology
                                // 
                                var mname = processedModuleName(name);
                                if (mname != null)
                                    Repository.moduleInformation.Add(name, new ModuleInformation(mname));
                            }
                            switch (name)
                            {
                                case "ModuleScienceExperiment":     // All of these use the experimentID to define the experiment
                                case "ModuleGroundExperiment":
                                case "DMAnomalyScanner":
                                case "DMAsteroidScanner":
                                case "DMBathymetry":
                                case "DMBioDrill":
                                case "DMModuleScienceAnimate":
                                case "DMModuleScienceAnimateGeneric":
                                case "DMReconScope":
                                case "DMRoverGooMat":
                                case "DMSIGINT":
                                case "DMSeismicHammer":
                                case "DMSeismicSensor":
                                case "DMSoilMoisture":
                                case "DMSolarCollector":
                                case "DMUniversalStorageScience":
                                case "DMUniversalStorageSoilMoisture":
                                case "DMUniversalStorageSolarCollector":
                                case "DMXRayDiffract":
                                case "KEESExperiment":
                                case "Magnetometer Scan":
                                case "ModuleScienceAvailabilityIndicator":
                                case "ScienceExperiment":
                                case "StationExperiment":
                                case "TSTChemCam":
                                case "USAdvancedScience":
                                case "USSimpleScience":
                                    {
                                        string experimentID = null;
                                        modules[j].TryGetValue("experimentID", ref experimentID);
                                        if (experimentID == null)
                                            modules[j].TryGetValue("experimentId", ref experimentID); // Needed for ModuleGroundExperiment
                                        if (experimentID != null)
                                        {
                                            if (!Repository.allExperimentParts.ContainsKey(experimentID))
                                                Repository.allExperimentParts.Add(experimentID, new ExperimentParts(experimentID));
                                            Repository.allExperimentParts[experimentID].parts.Add(PartLoader.LoadedPartsList[i].name);
                                        }
                                    }
                                    break;
                                case "SCANsat":
                                    {
                                        string experimentType = null;

                                        modules[j].TryGetValue("sensorType", ref experimentType); // SCANsat
                                        short s;
                                        if (short.TryParse(experimentType, out s))
                                        {
                                            SCANsatSCANtype scantype = (SCANsatSCANtype)s;
                                            foreach (SCANsatSCANtype sst in Enum.GetValues(typeof(SCANsatSCANtype)))
                                            {
                                                var a = scantype & sst;
                                                if (a != 0)
                                                {
                                                    string experimentID = a.ToString();
                                                    if (experimentID != null)
                                                    {
                                                        if (!Repository.allExperimentParts.ContainsKey(experimentID))
                                                            Repository.allExperimentParts.Add(experimentID, new ExperimentParts(experimentID));
                                                        Repository.allExperimentParts[experimentID].parts.Add(PartLoader.LoadedPartsList[i].name);

                                                    }
                                                }
                                            }
                                        }
                                    }
                                    break;
#if false

                                string experiment = null;
                                modules[j].TryGetValue("experiment", ref experiment);
                                if (experiment != null)
                                {
                                    if (!allExperiments.ContainsKey(experiment))
                                        allExperiments.Add(experiment, new ExperimentParts(experiment));
                                    allExperiments[experiment].parts.Add(PartLoader.LoadedPartsList[i].name);
                                }
                                break;
#endif
                            }
                        }
                    }
                }
            }
        }

        void OnGUI()
        {
            if (!initted)
            {
                initted = true;

                LoadPartInfo();
                Settings.Instance.kspWindow = new GUIStyle(GUI.skin.window); // GUIStyle(HighLogic.Skin.window);

                Settings.Instance.kspWindow.onNormal.textColor =
                    Settings.Instance.kspWindow.onHover.textColor =
                    Settings.Instance.kspWindow.onFocused.textColor =
                    Settings.Instance.kspWindow.onActive.textColor =
                    Settings.Instance.kspWindow.focused.textColor =
                    Settings.Instance.kspWindow.hover.textColor =
                    Settings.Instance.kspWindow.active.textColor =
                    Settings.Instance.kspWindow.normal.textColor = HighLogic.Skin.window.active.textColor;

                WhatDoINeed.SetAlphaFor(1, 0, Settings.Instance.kspWindow, HighLogic.Skin.window.active.background, Settings.Instance.kspWindow.active.textColor);
                WhatDoINeed.FadeStatus = WhatDoINeed.Fade.increasing;

                Settings.Instance.largeDisplayFont = new GUIStyle(GUI.skin.scrollView); // 
                Settings.Instance.largeDisplayFont.normal.textColor = Color.yellow;
                Settings.Instance.largeDisplayFont.wordWrap = true;

                Settings.Instance.displayFont = new GUIStyle(GUI.skin.scrollView); // GUIStyle(GUI.skin.label);
                Settings.Instance.displayFont.normal.textColor = Color.yellow;
                Settings.Instance.displayFont.wordWrap = true;

                Settings.Instance.displayFontGreen = new GUIStyle(GUI.skin.scrollView); // GUIStyle(GUI.skin.label);
                Settings.Instance.displayFontGreen.normal.textColor = Color.green;
                Settings.Instance.displayFontGreen.wordWrap = true;

                Settings.Instance.displayFontCyan = new GUIStyle(GUI.skin.scrollView); // GUIStyle(GUI.skin.label);
                Settings.Instance.displayFontCyan.normal.textColor = Color.cyan;
                Settings.Instance.displayFontCyan.wordWrap = true;


                Settings.Instance.displayFontRed = new GUIStyle(GUI.skin.scrollView); // GUIStyle(GUI.skin.label);
                Settings.Instance.displayFontRed.normal.textColor = Color.red;
                Settings.Instance.displayFontRed.wordWrap = true;

                Settings.Instance.displayFontOrange = new GUIStyle(GUI.skin.scrollView); // GUIStyle(GUI.skin.label);

                Settings.Instance.displayFontOrange.normal.textColor = new Color(1.0f, 0.64f, 0f); ;

                Settings.Instance.settingsDisplayFont = new GUIStyle(GUI.skin.label);
                Settings.Instance.settingsDisplayFont.normal.textColor = Color.white;

                Settings.Instance.settingsToggleDisplayFont = new GUIStyle(GUI.skin.toggle);
                Settings.Instance.settingsToggleDisplayFont.normal.textColor = Color.white;

                Settings.Instance.settingsFontSizeDisplayFont = new GUIStyle(GUI.skin.label);
                Settings.Instance.settingsFontSizeDisplayFont.normal.textColor = Color.white;

                Settings.Instance.buttonFont = new GUIStyle(GUI.skin.button);
                Settings.Instance.buttonFont.padding = new RectOffset();


                Settings.Instance.launchCheckDisplayFont = new GUIStyle(GUI.skin.scrollView);
                Settings.Instance.launchCheckDisplayFont.normal.textColor = Color.white;

                Settings.Instance.launchCheckDisplayFontRed = new GUIStyle(GUI.skin.scrollView);
                Settings.Instance.launchCheckDisplayFontRed.normal.textColor = Color.red;

                Settings.Instance.textAreaFont = new GUIStyle(GUI.skin.scrollView); // GUIStyle(GUI.skin.textArea);
                Settings.Instance.textAreaFont.normal.textColor = Color.white;
                Settings.Instance.textAreaFont.wordWrap = true;

                Settings.Instance.textAreaSmallFont = new GUIStyle(GUI.skin.scrollView); // GUIStyle(GUI.skin.textArea);
                Settings.Instance.textAreaSmallFont.wordWrap = true;

                Settings.Instance.textAreaWordWrap = new GUIStyle(GUI.skin.scrollView); // GUIStyle(GUI.skin.textArea);
                Settings.Instance.textAreaWordWrap.wordWrap = true;

                Settings.Instance.myStyle = new GUIStyle();
                Settings.Instance.styleOff = new Texture2D(2, 2);
                Settings.Instance.styleOn = new Texture2D(2, 2);

                Settings.Instance.resizeButton = GetToggleButtonStyle("resize", 20, 20, true);

                Settings.Instance.textFieldStyleRed = new GUIStyle(GUI.skin.scrollView) // GUIStyle(GUI.skin.textField)
                {
                    focused = { textColor = Color.red },
                    hover = { textColor = Color.red },
                    normal = { textColor = Color.red },
                    alignment = TextAnchor.MiddleLeft,
                };
                Settings.Instance.textFieldStyleNormal = new GUIStyle(GUI.skin.scrollView) // GUIStyle(GUI.skin.textField)
                {
                    alignment = TextAnchor.MiddleLeft,
                };
                Settings.Instance.scrollViewStyle = new GUIStyle(GUI.skin.scrollView);

                WhatDoINeed.SetFontSizes(Settings.Instance.fontSize, Settings.Instance.bold);

                ToolbarControl.LoadImageFromFile(ref minus, KSPUtil.ApplicationRootPath + "GameData/WhatDoINeed/PluginData/textures/icons8-minus-24");
                ToolbarControl.LoadImageFromFile(ref plus, KSPUtil.ApplicationRootPath + "GameData/WhatDoINeed/PluginData/textures/icons8-plus-math-24");

                minusIcon = new GUIContent(minus, "-");
                plusIcon = new GUIContent(plus, "+");

            }
        }

        public GUIStyle GetToggleButtonStyle(string styleName, int width, int height, bool hover)
        {
            ToolbarControl.LoadImageFromFile(ref Settings.Instance.styleOff, "GameData/WhatDoINeed/PluginData/textures/" + styleName + "_off");
            ToolbarControl.LoadImageFromFile(ref Settings.Instance.styleOn, "GameData/WhatDoINeed/PluginData/textures/" + styleName + "_on");

            Settings.Instance.myStyle.name = styleName + "Button";
            Settings.Instance.myStyle.padding = new RectOffset() { left = 0, right = 0, top = 0, bottom = 0 };
            Settings.Instance.myStyle.border = new RectOffset() { left = 0, right = 0, top = 0, bottom = 0 };
            Settings.Instance.myStyle.margin = new RectOffset() { left = 0, right = 0, top = 2, bottom = 2 };
            Settings.Instance.myStyle.normal.background = Settings.Instance.styleOff;
            Settings.Instance.myStyle.onNormal.background = Settings.Instance.styleOn;
            if (hover)
            {
                Settings.Instance.myStyle.hover.background = Settings.Instance.styleOn;
            }
            Settings.Instance.myStyle.active.background = Settings.Instance.styleOn;
            Settings.Instance.myStyle.fixedWidth = width;
            Settings.Instance.myStyle.fixedHeight = height;
            return Settings.Instance.myStyle;
        }


    }
}
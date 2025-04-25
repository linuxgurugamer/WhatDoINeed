using ClickThroughFix;
using ContractParser;
using KSP.UI.Screens;
using SpaceTuxUtility;
using System;
using System.Collections.Generic;
using System.Linq;
using ToolbarControl_NS;
using UnityEngine;
using static WhatDoINeed.RegisterToolbar;
using static WhatDoINeed.Utility;

namespace WhatDoINeed
{
    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public partial class WhatDoINeed : MonoBehaviour
    {
        internal  static ToolbarControl toolbarControl;
        //internal static ToolbarControl Toolbar { get { return toolbarControl; } }

        internal static WhatDoINeed Instance;
        bool hide = false;
        void OnHideUI() { hide = true; }
        void OnShowUI() { hide = false; }
        bool Hide { get { return hide; } }

        bool visible = false;

        bool IsVisible { get { return visible; } }


        //bool selectVisible = false;
        int winId, selWinId, manualContractWinId;
        public static double quickHideEnd = 0;

        internal const string MODID = "WhatDoINeed";
        internal const string MODNAME = "What Do I Need?";

        bool resizingWindow = false;
        public bool resizingSelWindow = false;

        //Vector2 scrollPos;

        bool kisAvailable = false;
        bool scansatAvailable = false;


        internal int btnId;

        internal bool winOpen { get { return visible; } }
        // public Repository repository;


        AvailablePart GetPartByIndex(string index)
        {
            return IsPartAvailable(index, true);
        }


        public void Start()
        {
            Log.Info("WhatDoINeed.Start 1");
            if (!HighLogic.LoadedSceneIsEditor || !(HighLogic.CurrentGame.Mode == Game.Modes.CAREER || HighLogic.CurrentGame.Mode == Game.Modes.SCIENCE_SANDBOX))
            {
                Log.Info("Sandbox mode, exiting");
                if (toolbarControl!=null)
                    toolbarControl.enabled = false;
                Destroy(this);
                return;
            }
            Log.Info("WhatDoINeed.Start 2");
            if (toolbarControl != null)
                toolbarControl.enabled = true;
            Instance = this;
            scansatAvailable = HasMod.hasMod("SCANsat");
            kisAvailable = KISWrapper.Initialize();

            lastAlpha = -1;
            Settings.Instance.LoadData();

            Repository.Contracts.Clear();
            ScanContracts();
            SetUpExperimentParts();
            if (scansatAvailable)
                SetupSCANsatStrings();
            InitializePartsAvailability();
            InitializeModuleTitles();

            GameEvents.onHideUI.Add(OnHideUI);
            GameEvents.onShowUI.Add(OnShowUI);
            GameEvents.onEditorShipModified.Add(onEditorShipModified);
            GameEvents.onGameSceneLoadRequested.Add(onGameSceneLoadRequested);

            EditorLogic.fetch.newBtn.onClick.AddListener(() => { ScanShip(); });

            winId = WindowHelper.NextWindowId("WhatDoINeed");
            selWinId = WindowHelper.NextWindowId("CCD_Select");
            manualContractWinId = WindowHelper.NextWindowId("ManualContractEntry");

            if (toolbarControl == null)
            {
                toolbarControl = gameObject.AddComponent<ToolbarControl>();
                toolbarControl.AddToAllToolbars(GUIToolbarToggle, GUIToolbarToggle,
                     ApplicationLauncher.AppScenes.SPH | ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.TRACKSTATION | ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.SPACECENTER | ApplicationLauncher.AppScenes.MAPVIEW,
                     MODID,
                     "CCD_Btn",
                     "WhatDoINeed/PluginData/textures/WhatDoINeed-38.png",
                     "WhatDoINeed/PluginData/textures/WhatDoINeed-24.png",
                    MODNAME);
            }

            ButtonManager.BtnManager.InitializeListener(EditorLogic.fetch.launchBtn, EditorLogic.fetch.launchVessel, "What Do I Need?");
            btnId = ButtonManager.BtnManager.AddListener(EditorLogic.fetch.launchBtn, OnLaunchButtonInput, "What Do I Need?", "What Do I Need?");

            Utility.SetWinPos(ref Settings.Instance.winPos, Settings.Instance.winPos.width, Settings.Instance.winPos.height);

            if (!Settings.Instance.helpWindowShown)
            {
                ShowHelpWindow();
                Settings.Instance.helpWindowShown = true;
                Settings.Instance.SaveData();
            }
            CheckVisibleStatus();
        }

        void CheckVisibleStatus()
        {
            if (Settings.Instance.reopenIfLastOpen)
            {
                //toolbarControl.buttonActive =
                Log.Info("reopenIfLastOpen: " + Settings.Instance.reopenIfLastOpen + ", lastVisibleStatus: " + Settings.Instance.lastContractWindowVisibleStatus +
", SettingsWindow: " + Settings.Instance.lastSettingsWindowVisibleStatus + ", SelectWindow: " + Settings.Instance.lastSelectWindowVisibleStatus +
", Parts Window: " + Settings.Instance.lastPartsWindowVisibleStatus);
                visible = Settings.Instance.lastContractWindowVisibleStatus;

                if (Settings.Instance.lastSettingsWindowVisibleStatus)
                    SettingsWindow.GetInstance();
                if (Settings.Instance.lastSelectWindowVisibleStatus)
                    SelectWindow.GetInstance();
                if (Settings.Instance.lastPartsWindowVisibleStatus)
                    NeededPartsWindow.GetInstance();
            }

        }

        internal class SCANsatDefs
        {
            internal string typeText;
            internal SCANsatSCANtype typeValue;

            internal SCANsatDefs(SCANsatSCANtype scansatDefvalue)
            {
                this.typeText = ((SCANsatSCANtype)scansatDefvalue).ToString();
                this.typeValue = scansatDefvalue;
            }
        }

        internal static int CountBits(int i)
        {
            int count;

            for (count = 0; i != 0; ++count)
                i &= (i - 1);

            return count;
        }


        static List<SCANsatDefs> scanSatDefs = null;
        void SetupSCANsatStrings()
        {
            if (scanSatDefs == null)
            {
                scanSatDefs = new List<SCANsatDefs>();
                foreach (SCANsatSCANtype foo in Enum.GetValues(typeof(SCANsatSCANtype)))
                {
                    scanSatDefs.Add(new SCANsatDefs(foo));
                }
            }
        }


        void OnDestroy()
        {
            GameEvents.onHideUI.Remove(OnHideUI);
            GameEvents.onShowUI.Remove(OnShowUI);
            GameEvents.onEditorShipModified.Remove(onEditorShipModified);

            GameEvents.onGameSceneLoadRequested.Remove(onGameSceneLoadRequested);
        }


        void onEditorShipModified(ShipConstruct sc)
        {
            ScanShip();
        }

        void onGameSceneLoadRequested(GameScenes scene)
        {
            if (scene == GameScenes.EDITOR)
                CheckVisibleStatus();
            else
                visible = false;
        }

        int lastNumParts = -1;
        void Update()
        {
            if (Input.GetMouseButtonUp(0) || Input.GetMouseButtonUp(1) || Input.GetMouseButtonUp(2) || lastNumParts != EditorLogic.fetch.ship.Count)
            {
                //Log.Info("Update, Input.GetMouseButtonUp(0)");
                ScanShip();
                lastNumParts = EditorLogic.fetch.ship.Count;
            }

        }
        void FixedUpdate()
        {
            if (FadeStatus != Fade.none && fadeStatus != Fade.faded)
                DoFade();
            else
                if (Time.realtimeSinceStartup > quickHideEnd && FadeStatus != Fade.none)
                FadeStatus = Fade.increasing;

        }

        /// <summary>
        /// Examines a part looking to classify it for future use
        /// </summary>
        /// <param name="part"></param>
        void ScanPart(Part part)
        {

            repository.shipInfo.AddPart(part.name);

#if false
            if (part.isAntenna(out ModuleDeployableAntenna antenna))
                repository.shipInfo.AddModuleType("Antenna");
            if (part.name.ToLower().Contains("battery"))
                repository.shipInfo.AddModuleType("Battery");
            if (part.dockingPorts.Count > 0)
                repository.shipInfo.AddModuleType("Dock");
#endif

            if (part.HasValidContractObjective("Antenna"))
                repository.shipInfo.AddModuleType("Antenna");
            if (part.HasValidContractObjective("Generator"))
                repository.shipInfo.AddModuleType("Generator");
            if (part.HasValidContractObjective("Grapple"))
                repository.shipInfo.AddModuleType("Grapple");
            if (part.HasValidContractObjective("Wheel"))
                repository.shipInfo.AddModuleType("Wheel");
            if (part.HasValidContractObjective("Laboratory"))
                repository.shipInfo.AddModuleType("Laboratory");
            if (part.HasValidContractObjective("Harvester"))
                repository.shipInfo.AddModuleType("Harvester");
            if (part.HasValidContractObjective("Greenhouse"))
                repository.shipInfo.AddModuleType("Greenhouse");

            // zzz

            for (int i1 = 0; i1 < part.Modules.Count; i1++)
            {
                var module = part.Modules[i1];
                if (module == null)
                {
                    continue;
                }
                repository.shipInfo.AddModule(module.moduleName);

                if (module is ModuleAlternator || module is ModuleDeployableSolarPanel)
                    repository.shipInfo.AddModuleType("Generator");
                if (module is ModuleGrappleNode)
                    repository.shipInfo.AddModuleType("Grapple");
                if (module is ModuleWheelBase || module.name == "KSPWheelBase")
                    repository.shipInfo.AddModuleType("Wheel");

                // To determine ModuleType:
                //Antenna     Check for AntennaType, should be RELAY or TRANSMIT
                //Battery     Has "battery" in name
                //Dock        has ModuleDockingNode
                //Generator   has ModuleAlternator or ModuleDeployableSolarPanel
                //Grapple     has ModuleGrappleNode
                //Wheel       has ModuleWheelBase or KSPWheelBase


            }
        }

        void ScanShip()
        {
            if (EditorLogic.fetch == null)
            {
                Log.Error("ScanShip, EditorLogic.fetch is null");
                return;
            }
            if (EditorLogic.fetch.ship == null)
            {
                Log.Error("ScanShip, EditorLogic.fetch.ship is null");
                return;
            }

            repository.shipInfo.Reinitialize();
            Repository.ClearNumAvailable();
            experimentStatus.Clear();

            Log.Info("ScanShip");
            for (int i = 0; i < EditorLogic.fetch.ship.parts.Count; i++)
            {
                Part part = EditorLogic.fetch.ship.parts[i];
                if (part != null)
                {
                    ScanPart(part);
                    SetPartOnShip(part);
                }
            }

            for (int i = 0; i < EditorLogic.fetch.ship.parts.Count; i++)
            {
                var p = EditorLogic.fetch.ship.parts[i];
                if (p == null)
                {
                    continue;
                }
                if (p.Modules == null)
                {
                    continue;
                }
                for (int i1 = 0; i1 < p.Modules.Count; i1++)
                {
                    var module = p.Modules[i1];
                    if (module == null)
                    {
                        continue;
                    }
                    if (module is ModuleInventoryPart)
                    {
                        if (((ModuleInventoryPart)module).storedParts == null)
                        {
                            continue;
                        }
                        foreach (StoredPart storedPart in ((ModuleInventoryPart)module).storedParts.Values)
                        {
                            AvailablePart spart = PartLoader.getPartInfoByName(storedPart.partName);
                            ScanPart(spart.partPrefab);
                            SetPartOnShip(spart.partPrefab);
                        }
                    }
                }

            }

            // Scan all parts stored in Kerbal Inventory System inventories
            if (kisAvailable)
                ScanShipKISInventory();

            PreprocessContracts(4);

#if !DEBUG
            if (Settings.Instance.debugMode)
#endif
            DumpAllScannedData();
        }

        void DumpAllScannedData()
        {
            Log.Info("======================================================================");
            Log.Info("======================================================================");
            //DumpPartInfoList();
            Log.Info("vvvvvvvvvvvv repository.allExperimentParts Dump vvvvvvvvvvvvv");
            foreach (var e in Repository.allExperimentParts)
            {
                Log.Info("Experiment: " + e.Value.experimentName);
                string p = "";
                foreach (var part in e.Value.parts)
                {
                    p += part + ", ";
                }
                Log.Info("    Parts: " + p);
            }
            Log.Info("^^^^^^^^^^^^ repository.allExperimentParts Dump ^^^^^^^^^^^^^\n");

            Log.Info("vvvvvvvvvvvv Repository Dump vvvvvvvvvvvvv");
            Log.Info("*** Contracts ***");
            foreach (var c in Repository.Contracts.Values)
            {
                Log.Info("Contract: " + c.Log());
                foreach (var eg in c.ExperimentGroups)
                {
                    Log.Info("    Experiment group name: " + eg.Key);
                    foreach (Experiment e in eg.Value)
                        Log.Info("        " + e.Log());
                }
                Log.Info("    Num of Neededparts: " + c.NeededParts.Count);
                foreach (var p in c.NeededParts)
                {
                    foreach (AvailPartWrapper l in p.Value)
                    {
                        Log.Info("        NeededParts Key: " + p.Key + ", " + l.partTitle);

                    }
                }
                //Log.Info("    PartGroupKeys: " + c.LogPartGroups());
                if (c.PartGroups.Count > 0)
                {
                    foreach (var pg in c.PartGroups)
                        Log.Info("        PartGroupKey: " + pg.Value.partGroupKey + ", numAvailable: " + pg.Value.numAvailable);
                }

                foreach (Param p in c.Params)
                {
                    Log.Info("    Params: " + p.Log());
                    if (p.RequestedParts != null)
                        Log.Info("        RequestedParts: " + p.LogReqParts());
                    if (p.subjectId != null)
                        Log.Info("        SubjectID: " + p.subjectId);
                    if (p.CheckModules.Count > 0)
                    {
                        foreach (var cm in p.CheckModules)
                            Log.Info("        ModuleTypes: " + cm.ModuleTypes + ", Description: " + cm.Description);
                    }
                    if (p.PartNames.Count > 0)
                        Log.Info("        PartNames: " + p.LogPartNames());
                    if (p.ModuleNames.Count > 0)
                        Log.Info("        ModuleNames: " + p.LogModuleNames());
                    if (p.Filters.Count > 0)
                    {
                        Log.Info("        Filters:");
                        foreach (var f in p.Filters)
                            Log.Info("            " + f.Log());
                    }
                }
                Log.Info("    AvailableParts");
                foreach (var pg in c.NeededParts)
                {
                    Log.Info("      PartGroup: " + pg.Key);
                    foreach (var e in pg.Value)
                    {
                        Log.Info("          AvailablePart: " + e.Log());
                        Log.Info("             Experiments: " + e.LogExperiments());
                    }
                }
            }
            Log.Info("--------");

            Log.Info("^^^^^^^^^^^^ Repository Dump ^^^^^^^^^^^^^\n");

            Log.Info("vvvvvvvvvvvv RegisterToolbar.Repository.moduleInformation Dump vvvvvvvvvvvvv");
            Log.Info("Repository.moduleInformation.Count: " + Repository.moduleInformation.Count);
            foreach (var mi in Repository.moduleInformation)
            {
                Log.Info("Key: " + mi.Key + ", moduleName: " + mi.Value.moduleName + ", numAvailable: " + mi.Value.numAvailable);
            }
            Log.Info("^^^^^^^^^^^^ Repository.moduleInformation.Repository.moduleInformation Dump ^^^^^^^^^^^^^\n");


#if true
            Log.Info("=====================================");
            Log.Info("Data Dump, ActiveContracts");
            Log.Info("=====================================");
            foreach (var contract in Settings.Instance.activeContracts)
            {
                Log.Info("GUID: " + contract.Key.ToString());
            }
#endif
            Log.Info("=====================================");
            Log.Info("Data Dump, activeLocalContracts");
            Log.Info("=====================================");
            foreach (var guid in activeLocalContracts)
                Log.Info("GUID: " + guid);
            Log.Info("======================================================================");
            Log.Info("======================================================================");

        }



        void ScanShipKISInventory()
        {
            //Log.Info("ScanShipKISInventory");
            for (int i0 = 0; i0 < EditorLogic.fetch.ship.Parts.Count; i0++)
            {
                Part p = EditorLogic.fetch.ship.Parts[i0];
                var availableItems = KISWrapper.GetInventories(p).SelectMany(i => i.items); //.ToArray();
                //Log.Info("part: " + p.name + ", # inv items: " + availableItems.Count());
                foreach (KeyValuePair<int, KISWrapper.KIS_Item> i in availableItems)
                {
                    var kisInvPart = i.Value.partNode.GetValue("name");
                    var part = PartLoader.getPartInfoByName(kisInvPart);
                    ScanPart(part.partPrefab);
                    SetPartOnShip(part.partPrefab);

                    //Log.Info("ScanShipKISInventory kisInvPart: " + kisInvPart);
                }
            }
        }


        List<Guid> activeLocalContracts = new List<Guid>();
        /// <summary>
        /// Create list of all experiments, and which parts support each experiment
        /// </summary>
        void SetUpExperimentParts()
        {
            //Log.Info("SetUpExperimentParts");
            // First get list of all experiments in the active contracts

            activeLocalContracts.Clear();
            ConfigNode configNode = new ConfigNode();
            HighLogic.CurrentGame.Save(configNode);
            ConfigNode gameNode = configNode.GetNode("GAME");
            ConfigNode[] scenarios = gameNode.GetNodes("SCENARIO");

            //Log.Info("=====================================");
            //Log.Info("Scanning contracts in Scenario ContractSystem node");
            //Log.Info("=====================================");

            if (scenarios != null && scenarios.Length > 0)
            {
                for (int i = 0; i < scenarios.Length; i++)
                {
                    var scenario = scenarios[i];
                    string name = scenario.GetValue("name");
                    if (name == "ContractSystem")
                    {
                        ConfigNode[] contracts = scenario.GetNode("CONTRACTS").GetNodes("CONTRACT");
                        //GUILayout.Label("contracts.Count: " + contracts.Length, CapComSkins.headerText, GUILayout.Width(100));
                        for (int j = 0; j < contracts.Length; j++)
                        {
                            ConfigNode contract = contracts[j];
                            string state = contract.GetValue("state");
                            Guid contractGuid = new Guid(contract.GetValue("guid"));


                            ConfigNode[] param_s = contract.GetNodes("PARAM");
                            string type = contract.GetValue("type");

                            string dataName = contract.GetValue("dataName");
                            if (state == "Active")
                            {
                                var aContracts = contractParser.getActiveContracts;
                                contractContainer c = aContracts.FirstOrDefault(b => b.ID == contractGuid);
                                if (c == null)
                                    Log.Error("Unable to find contract in contractParser: " + contractGuid);

                                var contractTypeTmp = c.Root.GetType().ToString().Split('.');
                                string contractType = contractTypeTmp[contractTypeTmp.Length - 1];
                                if (contractType != "PlantFlag" && contractType != "ExplorationContract")
                                {
                                    bool validContract = false;
                                    for (int k = 0; k < param_s.Length; k++)
                                    {
                                        if (state != "Complete" && state != "Cancelled")
                                        {
                                            ProcessConfigNode(k, contract, state, contractGuid, type, param_s[k], ref validContract);
                                        }
                                    }
                                    if (validContract)
                                        activeLocalContracts.Add(contractGuid);
                                    else
                                    if (!validContract && Repository.Contracts.ContainsKey(contractGuid))
                                        Repository.Contracts.Remove(contractGuid);

                                }
                            }
                        }
                    }
                }
            }

            //Log.Info("=====================================");
            //Log.Info("Scanning Parts, looking for experiments");
            //Log.Info("=====================================");


            // Now go through all the parts, looking for experiments
            for (int i = 0; i < PartLoader.LoadedPartsList.Count; i++)
            {
                AvailablePart p = PartLoader.LoadedPartsList[i];
                if (p.partConfig != null) // && !p.name.StartsWith("kerbal"))
                {

                    if (p.category != PartCategories.none)
                    {
                        ConfigNode[] modules = p.partConfig.GetNodes("MODULE");
                        Log.Info("SetupExperimentParts, modules: " + modules.Length);
                        if (modules != null && modules.Length > 0)
                        {
                            for (int j = 0; j < modules.Length; j++)
                            {
                                ConfigNode experiment = modules[j];
                                string name = experiment.GetValue("name");
                                string experimentID = null;
                                experimentID = experiment.GetValue("experimentID");
#if false
                                if (name == "ModuleResourceScanner")
                                {
                                    if (!scansatAvailable)
                                    {
                                    }
                                    else
                                    {

                                    }
                                }
#endif
                                if (scansatAvailable && name == "ModuleSCANresourceScanner")
                                {
                                    experimentID = DoSCANSatResourceScanner(experiment, p);

                                }
                                if (name == "DMModuleScienceAnimate")
                                {
                                    experimentID = experiment.GetValue("experimentID");

                                }
                                if (scansatAvailable && name == "SCANsat")
                                {
                                    experimentID = DoSCANSatModule(experiment, p);
                                }
                                else
                                {
                                    //Log.Info("part: " + p.name + ", moduleName: " + name + ", experimentID: " + experimentID + ", experimentType: " + experimentType);
                                    if (experimentID != null && experimentID != "Nothing")
                                    {
                                    }
                                }
                            }
                        }
                    }
                }
            }

            //Log.Info("=====================================");
            //Log.Info("Scanning ship, looking for parts with experiments");
            //Log.Info("=====================================");

            ScanShip();
        }

        public List<string> moduleTypes = new List<string> { "Antenna", "Generator", "Dock", "Grapple", "Wheel", "Laboratory", "Harvester", "Greenhouse" };

        // need to merge the one below with SetUpExperimentParts
        void InitializeModuleTitles()
        {

            Log.Info("InitializeModuleTitles 1");
            if (!Repository.moduleInformation.ContainsKey("Antenna"))
            {
                Repository.moduleInformation.Add("Antenna", new ModuleInformation("Antenna"));
                Repository.moduleInformation.Add("Generator", new ModuleInformation("Generator"));
                Repository.moduleInformation.Add("Dock", new ModuleInformation("Dock"));
                Repository.moduleInformation.Add("Grapple", new ModuleInformation("Grapple"));
                Repository.moduleInformation.Add("Wheel", new ModuleInformation("Wheel"));

                Repository.moduleInformation.Add("Laboratory", new ModuleInformation("Laboratory"));
                Repository.moduleInformation.Add("Harvester", new ModuleInformation("Harvester"));
                Repository.moduleInformation.Add("Greenhouse", new ModuleInformation("Greenhouse"));

                if (PartLoader.LoadedPartsList == null)
                    Log.Error("PartLoader.LoadedPartsList is null");

                for (int i = 0; i < PartLoader.LoadedPartsList.Count; i++)
                {
                    AvailablePart ap = PartLoader.LoadedPartsList[i];

                    if (ap.partConfig != null && ap.category != PartCategories.none)
                    {
                        ConfigNode[] nodes = ap.partConfig.GetNodes("MODULE");
                        if (RegisterToolbar.CheckForModuleLaunchClamp(nodes))
                            continue;

                        //Log.Info("p.partConfig.name: " + ap.partConfig.name + ", numNodes: " + nodes.Length);
                        for (int j = 0; j < nodes.Length; j++)
                        {
                            ConfigNode node = nodes[j];
                            string moduleName = node.GetValue("name");
                            if (moduleName == null)
                            {
                                Log.Info("Module name is null");
                            }
                            if (!Repository.moduleInformation.ContainsKey(moduleName))
                                Repository.moduleInformation.Add(moduleName, new ModuleInformation(moduleName));

                            // The module categories are coded this way because the only way to get it from the module itself
                            // would be to instantiate each part and then query the part.  Especially for games with thousands ofpart
                            // that would take too long
                            // The limitation of doing it this way is that any new mods which have new modules won't be
                            // seen.
                            switch (moduleName)
                            {
                                case "ModuleAntennaFeed":                       // Near Future Exploration
                                case "ModuleDeployableAntenna":
                                case "ModuleRTAntenna":                         // RemoteTech
                                case "Antenna":                                 // Kerbalism
                                case "ModuleSEPStation":                        // Surface Experiment Pack, old mod not being updated
                                    Repository.moduleInformation["Antenna"].partsWithModule.Add(ap);
                                    break;

                                case "ModuleDeployableSolarPanel":
                                case "ModuleGenerator":
                                case "FissionGenerator":                        // Near Future Electrical
                                case "ModuleRadioisotopeGenerator":             // Near Future Electrical
                                case "ModuleSystemHeatFissionEngine":           // SystemHeat
                                case "ModuleSystemHeatFissionReactor":          // SystemHeat
                                case "ModuleSystemHeatFissionEngineOld ":       // SystemHeat
                                case "KerbalismSentinel":                       // Kerbalism
                                case "SSTUSolarPanelStatic":                    // SSTU
                                case "SSTUModularPart":                         // SSTU
                                case "SSTUSolarPanelDeployable":                // SSTU
                                    Repository.moduleInformation["Generator"].partsWithModule.Add(ap);
                                    break;

                                case "ModuleDockingNode":
                                case "FlexoTube":
                                    Repository.moduleInformation["Dock"].partsWithModule.Add(ap);
                                    break;

                                case "ModuleGrappleNode":
                                    Repository.moduleInformation["Grapple"].partsWithModule.Add(ap);
                                    break;

                                case "ModuleWheelBase":
                                case "KSPWheelMotor":                           // KSPwheel
                                    Repository.moduleInformation["Wheel"].partsWithModule.Add(ap);
                                    break;

                                case "Laboratory":                              // Kerbalism
                                    Repository.moduleInformation["Laboratory"].partsWithModule.Add(ap);
                                    break;

                                case "Harvester":                               // Kerbalism
                                    Repository.moduleInformation["Harvester"].partsWithModule.Add(ap);
                                    break;
                                case "Greenhouse":                               // Kerbalism
                                    Repository.moduleInformation["Greenhouse"].partsWithModule.Add(ap);
                                    break;
                            }
                        }
                    }
                }
            }
#if DEBUG
            foreach (var s in moduleTypes)
            {
                Log.Info("Module Type: " + s + ", number of parts: " + Repository.moduleInformation[s].partsWithModule.Count);
                foreach (var p in Repository.moduleInformation[s].partsWithModule)
                    Log.Info("        Part: " + p.name);
            }
#endif
        }

        /// <summary>
        /// Needs to be optimized 
        /// </summary>
        /// <param name="partModule"></param>
        /// <returns></returns>
        string GetPartModuleTitle(string filterPartModule)
        {
            //if (Repository.moduleInformation.Count == 0)
            //    InitializeModuleTitles();

            if (Repository.moduleInformation.ContainsKey(filterPartModule))
                return Repository.moduleInformation[filterPartModule].moduleName;
            return null;
        }

        void ProcessConfigNode(int cnt, ConfigNode contract, string state, Guid contractGuid, string type, ConfigNode param, ref bool validContract)
        {
            string param_name = param.GetValue("name");
            string param_type = param.GetValue("type");

            //string param_state = param.GetValue("state");
            //Log.Info("ProcessConfigNode, state: " + state + ", contractGuid: " + contractGuid + ", type: " + type + ", param_name: " + param_name);

            //if (param_state == "Incomplete")
            {
                string param_part = null;
                string experiment = null;
                string experimentDetail = null;
                string scansatExpID = null;
                short scansatExpIDShort = -1;
                string scanName = "";
                string kerbalName = null;

                param.TryGetValue("experiment", ref experiment);
                param.TryGetValue("KerbalName", ref kerbalName);

                Param parameter = null;

                bool newVesselParamFound = false;
                Log.Info("ProcessConfigNode, contractGuid: " + contractGuid + ", name: " + param_name + ", type: " + param_type);

                if (experiment == null)
                {
                    switch (param_name)
                    {
                        case "NewVessel":
                            newVesselParamFound = true;
                            break;
                        case "SCANsatCoverage": // For SCANsat
                            {
                                newVesselParamFound = true;
                                param.TryGetValue("scanType", ref scansatExpID);
                                if (short.TryParse(scansatExpID, out scansatExpIDShort))
                                {
                                    //Log.Info("SCANsatCoverage scanType: " + scansatExpID + " not a number, trying alternate method");

                                    // for some reason, the short.TryParse is failing sometimes on a legitimate number, but the Convert works

                                    try
                                    {
                                        scansatExpIDShort = Convert.ToInt16(scansatExpID);
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Error("SCANsatCoverage scanType: " + scansatExpID + " not a number");
                                        Log.Error("Convert error: " + ex.Message);
                                    }
                                }
                                param.TryGetValue("scanName", ref scanName);
                                experiment = "SCANsat"; // + experiment;
                                experimentDetail = experiment;

                                CEP_Key_Tuple ckt;
                                //ContractExperimentPart cep;

                                parameter = new Param(param_name, cnt, param_name);
                                parameter.scanName = scanName;
                                parameter.scanType = (SCANsatSCANtype)scansatExpIDShort;

                                //Repository.Contracts[contractGuid].AddParam(parameter);

                                short a = Convert.ToInt16(scansatExpID);
                                ckt = new CEP_Key_Tuple(a.ToString(), contractGuid, true);
                                //cep = new ContractExperimentPart(ckt, scansatExpID);

                                repository.AddExperimentToContract(contractGuid, ckt.Key(), new Experiment(1, ckt, scansatExpID));

                                experiment = "";
                                break;
                            }
                        // Handle the OrbitalScience here
                        case "StnSciParameter":
                            {
                                newVesselParamFound = true;
                                param.TryGetValue("experimentType", ref experiment);  // Now check for Station Science experiments
                                if (experiment != null)
                                {

                                    //experimentDetail = experiment.Remove(0, 16);
                                    //string a = experimentDetail[0].ToString().ToLower();
                                    //experimentDetail = a + experimentDetail.Remove(0, 1);
                                    experimentDetail = experiment;
                                    experiment = experiment.Substring(0, 16); // should be StnSciExperiment

                                }
                                parameter = new Param(param_name, cnt, param_name);
                                parameter.experimentType = experiment;
                                Repository.Contracts[contractGuid].AddParam(parameter);

                                break;
                            }

#if true
                        // Not sure if following will show up as s unique contract in the contract system, need to test
                        // TODO
                        case "USAdvancedScience":  //Universal Storage    (probably not Station Science experiments)
                            {
                                newVesselParamFound = true;
                                Log.Info("USAdvancedScience experiment found");
                                param.TryGetValue("experimentID", ref experiment);  // More Station Science experiments
                                experimentDetail = experiment;
                                break;
                            }
#endif
                        case "ConstructionParameter":
                        case "RepairPartParameter":
                        case "PartTest":
                            {
                                newVesselParamFound = true;

                                if (param_name == "RepairPartParameter" || param_name == "ConstructionParameter")
                                    param_part = param.GetValue("partName");
                                else
                                    param_part = param.GetValue("part");

                                //parameter = new Param(param_name, param_name);
                                //parameter.AddPartName(param_part);
                                //Repository.Contracts[contractGuid].AddParam(parameter);

                                CEP_Key_Tuple ckt = new CEP_Key_Tuple(param_name, contractGuid, param_part);
                                Log.Info("PartTest, param_part: " + param_part);
                                var p = GetPartByIndex(param_part);
                                if (p != null)
                                    Repository.Contracts[contractGuid].AddNeededPart(1, p.name, new AvailPartWrapper(p));
                                else
                                    Log.Error("Part not found by index");

                                repository.AddExperimentToContract(contractGuid, ckt.Key(), new Experiment(2, ckt));
                                //ZZZ

                                break;
                            }

                        case "VesselSystemsParameter":
                            // checkModuleTypes = Antenna | Generator | Dock
                            // checkModuleDescriptions = has an antenna| has a docking port | can generate power
                            {
                                newVesselParamFound = true;
                                string checkModuleTypes = null;
                                string checkModuleDescriptions = null;

                                param.TryGetValue("checkModuleTypes", ref checkModuleTypes);
                                param.TryGetValue("checkModuleDescriptions", ref checkModuleDescriptions);

                                var moduleTypes = checkModuleTypes.Split('|');
                                var moduleDescriptions = checkModuleDescriptions.Split('|');
                                if (moduleTypes.Length != moduleDescriptions.Length)
                                    Log.Error("moduleTypes count doesn't match moduleDescriptions.");
                                //parameter = new Param(param_name, cnt, type);
                                parameter = new Param(param_name, cnt, param_name);
                                for (int i1 = 0; i1 < moduleTypes.Length; i1++)
                                {
                                    parameter.CheckModules.Add(new CheckModule(moduleTypes[i1], moduleDescriptions[i1]));
                                }
                                Repository.Contracts[contractGuid].AddParam(parameter);
                            }
                            break;

                        case "PartRequestParameter":
                            // partNames = cupola|sspx-cupola-125-1|sspx-cupola-1875-1|sspx-cupola-375-1|sspx-observation-25-1|sspx-dome-5-1
                            // moduleNames = ModuleScienceLab
                            string partNames = null;
                            string moduleNames = null;
                            newVesselParamFound = true;

                            parameter = new Param(param_name, cnt, param_name);

                            param.TryGetValue("partNames", ref partNames);
                            if (partNames != null)
                            {
                                parameter.AddPartNames(partNames.Split('|').ToList());

                            }
                            param.TryGetValue("moduleNames", ref moduleNames);
                            if (moduleNames != null)
                            {
                                parameter.ModuleNames = moduleNames.Split('|').ToList();
                            }
                            Repository.Contracts[contractGuid].AddParam(parameter);

                            break;

                        case "DMLongOrbitParameter": // This is for the DMagic science stuff
                            {
                                newVesselParamFound = true;
                                var partRequestParams = param.GetNodes("PARAM");

                                for (int l = 0; l < partRequestParams.Length; l++)
                                {
                                    string pname = null;
                                    partRequestParams[l].TryGetValue("name", ref pname);
                                    if (pname == "DMPartRequestParameter")
                                    {
                                        string Requested_Parts = null;
                                        partRequestParams[l].TryGetValue("Requested_Parts", ref Requested_Parts);
                                        List<string> rp = Requested_Parts.Split('|').ToList();
                                        for (int i = 0; i < rp.Count; i++)
                                        {
                                            var r = rp[i];
                                            parameter = new Param(param_name, cnt * 100 + i, type);
                                            parameter.AddRequestedParts(r.Split(',').ToList());
                                            Repository.Contracts[contractGuid].AddParam(parameter);
                                        }
                                    }
                                }
                                experiment = null;

                                break;
                            }

                        case "PartValidation": // For REPOSoftTech/ResearchBodies
                            {
                                Log.Info("PartValidation");
                                newVesselParamFound = true;
                                parameter = new Param(param_name, cnt, param_name);

                                var partValidationFilterParams = param.GetNodes("FILTER");
                                Log.Info("PartValidation, number of FILTERs: " + partValidationFilterParams.Length);

                                // First check for needed part modules
                                for (int m = 0; m < partValidationFilterParams.Length; m++)
                                {
                                    string filterPartModule = null;
                                    string filterCategory = null;
                                    string filterType = null;

                                    ConfigNode prp = partValidationFilterParams[m];

                                    prp.TryGetValue("partModule", ref filterPartModule);
                                    prp.TryGetValue("type", ref filterType);
                                    if (filterPartModule != null)
                                    {
                                        string filterPartModuleTitle = GetPartModuleTitle(filterPartModule);

                                        Log.Info("FILTER partModule, reqPartModule: " + filterPartModule);

                                        if (filterPartModuleTitle != null)
                                            parameter.Filters.Add(new Filter("partModule", filterPartModule, filterPartModuleTitle));

                                    }
                                    else
                                    {
                                        prp.TryGetValue("category", ref filterCategory);
                                        if (filterCategory != null)
                                        {
                                            Log.Info("FILTER, filterCategory: " + filterCategory);
                                            parameter.Filters.Add(new Filter("category", filterCategory, ""));
                                        }
                                        else
                                        {
                                            Log.Info("FILTER, type: " + filterType);
                                            switch (filterType)
                                            {
                                                case "NONE":
                                                    {
                                                        var module = prp.GetNode("MODULE");
                                                        if (module != null)
                                                        {
                                                            string engineType = null;
                                                            string aname = null;
                                                            module.TryGetValue("engineType", ref engineType);
                                                            if (engineType != null)
                                                            {
                                                                parameter.Filters.Add(new Filter("NONE", "engineType", engineType));
                                                            }
                                                            else
                                                            {
                                                                module.TryGetValue("name", ref aname);
                                                                if (aname != null)
                                                                    parameter.Filters.Add(new Filter("NONE", "moduleName", aname));
                                                            }
                                                        }
                                                    }
                                                    break;
                                                case "FILTER":
                                                    var parts = prp.GetValues("part");
                                                    if (parts != null)
                                                    {
                                                        foreach (var part in parts)
                                                            parameter.Filters.Add(new Filter("FILTER", "part", part));
                                                    }
                                                    var partModules = prp.GetValues("partModule");
                                                    foreach (var pm in partModules)
                                                    {
                                                        // string filterPartModuleTitle = GetPartModuleTitle(filterPartModule) ;
                                                        parameter.Filters.Add(new Filter("FILTER", "partModule", pm));
                                                    }
                                                    break;
                                            }
                                        }
                                    }
                                    if (!Repository.Contracts.ContainsKey(contractGuid))
                                        Log.Error("contractGuid not found in Contracts");
                                    Repository.Contracts[contractGuid].AddParam(parameter);
                                    //Log.Info("After repository");

#if false
	FILTER
	{
		type = FILTER
		partModule = RoverScience
	}
				
	FILTER
	{
		type = NONE
		MODULE
		{
			EngineType = SolidBooster
		}
	}
    FILTER
	{
		type = NONE
		MODULE
		{
			name = ModuleEnginesFX
		}
	}
    FILTER
	{
		type = FILTER
		part = sensorAtmosphere
		part = GooExperiment
		part = Magnetometer
		part = sensorAccelerometer
		part = sensorBarometer
		part = sensorGravimeter
		part = sensorThermometer
	}
				
#endif
                                    Log.Info("PartValidation 2");

                                    //else
                                    //Log.Info("SetUpExperimentPart 3");
                                    //Log.Info("SetUpExperimentPart 4");
                                    Log.Info("PartValidation 3");

                                }
                                break;
                            }
                        case "CollectDeployedScience":
                            {
                                newVesselParamFound = true;
                                string subjectId = null;
                                param.TryGetValue("subjectId", ref subjectId);

                                if (subjectId != null)
                                {
                                    parameter = new Param(param_name, cnt, param_name);
                                    parameter.subjectId = subjectId;
                                    Repository.Contracts[contractGuid].AddParam(parameter);

                                }
                                break;
                            }
                    }
                }
                if (newVesselParamFound)
                    validContract = true;

            }
        }


        void ScanContracts()
        {
            //Log.Info("ScanContracts");
            var aContracts = contractParser.getActiveContracts;

            int cnt = 0;
            for (int i = 0; i < aContracts.Count; i++)
            {
                contractContainer contract = aContracts[i];
                var contractTypeTmp = contract.Root.GetType().ToString().Split('.');
                string contractType = contractTypeTmp[contractTypeTmp.Length - 1];
                Log.Info("contractType: " + contractType);

                if (contractType != "PlantFlag" && contractType != "ExplorationContract")
                {
                    if (!Settings.Instance.activeContracts.ContainsKey(contract.ID))
                    {
                        Settings.Instance.activeContracts[contract.ID] = false;
                        //Log.Info("Contract Added: " + contract.ID);
                    }

                    repository.AddContract(new ContractWrapper(contract, contract.ID, contractType, contractType));
                    if (Settings.Instance.activeContracts.ContainsKey(contract.ID) &&
                   Settings.Instance.activeContracts[contract.ID])
                        cnt++;
                }
            }

            Log.Info("initialShowAll: " + Settings.Instance.initialShowAll + ", cnt: " + cnt);
            if (Settings.Instance.initialShowAll && cnt == 0)
            {
                foreach (var contract in Repository.Contracts)
                    contract.Value.selected = true;
            }
            else
            {
                //foreach (var a in Settings.Instance.activeContracts)
                //    Log.Info("ScanContracts, activeContracts from settings: " + a.Key + ", value: " + a.Value);
                foreach (var contract in Repository.Contracts)
                {
                    //Log.Info("ScanContracts, contract.Key: " + contract.Key);
                    if (Settings.Instance.activeContracts.ContainsKey(contract.Key) &&
                        Settings.Instance.activeContracts[contract.Key])
                        contract.Value.selected = true;
                }
            }
        }

        string DoSCANSatResourceScanner(ConfigNode experiment, AvailablePart p)
        {
            // TODO
            string experimentID = "Nothing";
            return experimentID;
        }


        /// <summary>
        /// checks the config of the part module against the activeContracts
        /// </summary>
        /// <param name="experimentConfigNode"></param>
        /// <param name="availPart"></param>
        /// <returns></returns>
        string DoSCANSatModule(ConfigNode experimentConfigNode, AvailablePart availPart)
        {
            //return "";
            //Log.Info("===============");
            //Log.Info("DoSCANSatModule 1, part: " + availPart.name);

            string experimentID = "Nothing";
            return experimentID;
        }

        Rect lastWinPosSize = new Rect();


        private void GUIToolbarToggle()
        {
            GUIToggle();
            if (!visible)
                HideAllWindows();
            else
                CheckVisibleStatus();
        }

        private void GUIToggle()
        {
            visible = !visible;
            Settings.Instance.lastContractWindowVisibleStatus = IsVisible;
            //Settings.Instance.lastSettingsWindowVisibleStatus = SettingsWindow.IsVisible;
            //Settings.Instance.lastSelectWindowVisibleStatus = SelectWindow.IsVisible;
            //Settings.Instance.lastPartsWindowVisibleStatus = PartsWindow.IsVisible;
            Settings.Instance.SaveData();
        }

        private void HideAllWindows()
        {
            Log.Info("HideAllWindows");
            if (SettingsWindow.Exists && SettingsWindow.IsVisible)
                SettingsWindow.GetInstance().DestroyWin(true);

            if (SelectWindow.Exists && SelectWindow.IsVisible)
                SelectWindow.GetInstance().DestroyWin(true);

            if (NeededPartsWindow.Exists && NeededPartsWindow.IsVisible)
                NeededPartsWindow.GetInstance().DestroyWin(true);
        }

        public void OnGUI()
        {
            if (!(HighLogic.CurrentGame.Mode == Game.Modes.CAREER || HighLogic.CurrentGame.Mode == Game.Modes.SCIENCE_SANDBOX))
            {
                return;
            }

            if (!Hide && visible && Settings.Instance != null)
            {
                Rect tmpPos;
                SetAlpha(Settings.Instance.Alpha);
                SetFontAlpha(ref Settings.Instance.kspWindow);
                if (Settings.Instance.enableClickThrough || Time.realtimeSinceStartup <= quickHideEnd)
                    tmpPos = GUILayout.Window(winId, Settings.Instance.winPos, ContractWindowDisplay, "What Do I Need? - Active Contract" + ((Settings.Instance.activeContracts.Count != 1) ? "s" : ""), Settings.Instance.kspWindow);
                else
                    tmpPos = ClickThruBlocker.GUILayoutWindow(winId, Settings.Instance.winPos, ContractWindowDisplay, "What Do I Need? - Active Contract" + ((Settings.Instance.activeContracts.Count != 1) ? "s" : ""), Settings.Instance.kspWindow);
                if (!Settings.Instance.lockPos)
                    Settings.Instance.winPos = tmpPos;

                if (Settings.Instance.winPos != lastWinPosSize)
                {
                    lastWinPosSize = Settings.Instance.winPos = tmpPos;
                    Settings.Instance.SaveData();
                }
            }
        }

        private string getPartTitle(string name)
        {
            if (!string.IsNullOrEmpty(name))
            {
                AvailablePart partInfoByName = PartLoader.getPartInfoByName(name.Replace('_', '.'));
                //Log.Info("WhatDoINeed.getPartTitle, partInfoByName: " + partInfoByName.name);
                if (partInfoByName != null && ResearchAndDevelopment.PartModelPurchased(partInfoByName))
                {
                    return CleanPartTitle(partInfoByName.title);
                }
            }


            return "";
        }

        void ShowHelpWindow()
        {
            GameObject myplayer = new GameObject("HelpWindowClass");

            var w = myplayer.AddComponent<HelpWindowClass>();
        }

        Vector2 contractPos;
        Dictionary<string, bool> openClosed = new Dictionary<string, bool>();



        static internal void SetFontSizes(float fontSize, bool bold)
        {
            //Log.Info("SetFontSizes, fontSize: " + fontSize + ", font: " + Settings.Instance.settingsDisplayFont.font.name);

            Settings.Instance.largeDisplayFont.fontSize = (int)fontSize + 2;
            Settings.Instance.largeDisplayFont.fontStyle = FontStyle.Bold; // bold ? FontStyle.Bold : FontStyle.Normal;
            //Settings.Instance.largeDisplayFont.normal.textColor = Color.yellow;
            Settings.Instance.largeDisplayFont.border = new RectOffset();
            Settings.Instance.largeDisplayFont.padding = new RectOffset();
            Settings.Instance.largeDisplayFont.alignment = TextAnchor.LowerLeft;
#if true
            Settings.Instance.displayFont.fontSize = (int)fontSize;
            Settings.Instance.displayFont.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            //Settings.Instance.displayFont.normal.textColor = Color.yellow;
            Settings.Instance.displayFont.border = new RectOffset();
            Settings.Instance.displayFont.padding = new RectOffset();

            Settings.Instance.displayFontGreen.fontSize = (int)fontSize;
            Settings.Instance.displayFontGreen.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            //Settings.Instance.displayFontGreen.normal.textColor = Color.green;
            Settings.Instance.displayFontGreen.border = new RectOffset();
            Settings.Instance.displayFontGreen.padding = new RectOffset();

            Settings.Instance.displayFontCyan.fontSize = (int)fontSize;
            Settings.Instance.displayFontCyan.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            //Settings.Instance.displayFontCyan.normal.textColor = Color.cyan;
            Settings.Instance.displayFontCyan.border = new RectOffset();
            Settings.Instance.displayFontCyan.padding = new RectOffset();

            Settings.Instance.displayFontRed.fontSize = (int)fontSize;
            Settings.Instance.displayFontRed.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            //Settings.Instance.displayFontCyan.normal.textColor = Color.Red;
            Settings.Instance.displayFontRed.border = new RectOffset();
            Settings.Instance.displayFontRed.padding = new RectOffset();


#endif

            Settings.Instance.settingsDisplayFont.fontSize = (int)fontSize;
            Settings.Instance.settingsToggleDisplayFont.fontSize = (int)fontSize;

            //Settings.Instance.settingsFontSizeDisplayFont.fontSize = (int)fontSize;


            Settings.Instance.launchCheckDisplayFont.fontSize = (int)fontSize;
            Settings.Instance.launchCheckDisplayFont.fontStyle = FontStyle.Normal;

            Settings.Instance.launchCheckDisplayFont.border = new RectOffset();
            Settings.Instance.launchCheckDisplayFont.padding = new RectOffset();

            Settings.Instance.launchCheckDisplayFontRed.fontSize = (int)fontSize;
            Settings.Instance.launchCheckDisplayFontRed.border = new RectOffset();
            Settings.Instance.launchCheckDisplayFontRed.padding = new RectOffset();
            Settings.Instance.launchCheckDisplayFontRed.fontStyle = FontStyle.Bold;



            //Settings.Instance.displayFont.normal.background,

            //Settings.Instance.scrollViewStyle = Settings.Instance.displayFont;
            //Settings.Instance.displayFont.padding = new RectOffset();

            Settings.Instance.textAreaFont.fontSize = (int)fontSize;
            Settings.Instance.textAreaFont.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            //Settings.Instance.textAreaFont.normal.textColor = Color.white;

            Settings.Instance.textAreaSmallFont.fontSize = (int)fontSize - 2;
            Settings.Instance.textAreaSmallFont.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            Settings.Instance.textAreaSmallFont.richText = true;
            //Settings.Instance.textAreaSmallFont.normal.textColor = Color.white;
        }

#if true
        static Texture2D SetImageAlpha(Texture2D img, byte Alpha)
        {
            Texture2D copyTexture = GUISkinCopy.CopyTexture2D(Settings.Instance.displayFont.normal.background);

            var pixels = copyTexture.GetPixels32();
            for (int i = 0; i < pixels.Length; ++i)
                pixels[i].a = (byte)Alpha;


            copyTexture.SetPixels32(pixels);
            copyTexture.Apply();
            return copyTexture;
        }
#endif

        static float lastAlpha = -1;

        internal enum Fade { decreasing, increasing, faded, none };
        static Fade fadeStatus = Fade.none;
        internal static Fade FadeStatus
        {
            get { return fadeStatus; }
            set
            {
                fadeStatus = value;
                switch (FadeStatus)
                {
                    case Fade.decreasing:
                        alphaFade = 1; break;
                    case Fade.increasing:
                        alphaFade = 0; break;
                }
            }
        }
        static float alphaFade = -1;
        static float buttonAlpha = 1;
        const float fadeStep = 0.02f;

        internal void DoFade()
        {
            switch (FadeStatus)
            {
                case Fade.decreasing:
                    {
                        alphaFade -= fadeStep;
                        SetAllAlpha();
                        if (alphaFade <= 0)
                            fadeStatus = Fade.faded;
                        Color temp = GUI.color;
                        temp.a = alphaFade;
                        GUI.color = temp;
                        buttonAlpha = alphaFade;
                    }
                    break;
                case Fade.increasing:
                    {
                        alphaFade += fadeStep;
                        SetAllAlpha();
                        if (alphaFade >= 1)
                        {
                            fadeStatus = Fade.none;
                        }
                        Color temp = GUI.color;
                        temp.a = alphaFade;
                        GUI.color = temp;
                        buttonAlpha = alphaFade;
                    }
                    break;
            }
        }

        public void SetButtonAlpha()
        {
            Color temp = GUI.color;
            temp.a = buttonAlpha;
            GUI.color = temp;
        }

        void SetAllAlpha()
        {
            alphaFade = Mathf.Clamp(alphaFade, 0f, 1f);
            float realAlpha = alphaFade * lastAlpha;


            SetAlphaFor(1, realAlpha, Settings.Instance.kspWindow, HighLogic.Skin.window.active.background, Settings.Instance.kspWindow.active.textColor);

            SetFontAlpha(ref Settings.Instance.largeDisplayFont);
            SetFontAlpha(ref Settings.Instance.displayFont);
            SetFontAlpha(ref Settings.Instance.displayFontCyan);
            SetFontAlpha(ref Settings.Instance.displayFontRed);
            SetFontAlpha(ref Settings.Instance.displayFontOrange);
            SetFontAlpha(ref Settings.Instance.displayFontGreen);
            SetFontAlpha(ref Settings.Instance.textAreaFont);
            SetFontAlpha(ref Settings.Instance.textAreaSmallFont);
            SetFontAlpha(ref Settings.Instance.settingsDisplayFont);
            SetFontAlpha(ref Settings.Instance.settingsToggleDisplayFont);
            SetFontAlpha(ref Settings.Instance.settingsFontSizeDisplayFont);


            SetFontAlpha(ref Settings.Instance.buttonFont);
            SetFontAlpha(ref Settings.Instance.resizeButton);

        }

        public void SetFontAlpha(ref GUIStyle font)
        {
            Color c = font.normal.textColor;
            c.a = alphaFade;
            font.normal.textColor = c;
            font.hover.textColor = c;
            font.active.textColor = c;
            font.focused.textColor = c;

        }

        internal static void SetAlpha(float Alpha)
        {
            GUIStyle workingWindow;
            if (Alpha == lastAlpha)
                return;
            lastAlpha = Alpha;
            if (Settings.Instance.kspWindow.active.background == null)
            {
                //Log.Info("SetAlpha, Settings.Instance.kspWindow.active.background is null");
                //Settings.Instance.kspWindow.active.background = GUISkinCopy.CopyTexture2D(GUI.skin.window.active.background);
                Settings.Instance.kspWindow.active.background = GUISkinCopy.CopyTexture2D(HighLogic.Skin.window.active.background);
            }

            workingWindow = Settings.Instance.kspWindow;

            SetAlphaFor(1, Alpha, Settings.Instance.kspWindow, HighLogic.Skin.window.active.background, workingWindow.active.textColor);
            //SetAlphaFor(2, Alpha, Settings.Instance.textAreaFont, GUI.skin.textArea.normal.background, Settings.Instance.textAreaFont.normal.textColor);
            //SetAlphaFor(3, Alpha, Settings.Instance.textAreaSmallFont, GUI.skin.textArea.normal.background, Settings.Instance.textAreaSmallFont.normal.textColor);
            //SetAlphaFor(4, Alpha, Settings.Instance.displayFont, GUI.skin.textArea.normal.background, Settings.Instance.displayFont.normal.textColor);
            //SetAlphaFor(5, Alpha, Settings.Instance.scrollViewStyle, GUI.skin.scrollView.normal.background, workingWindow.active.textColor);
        }

        public static void SetAlphaFor(int id, float Alpha, GUIStyle style, Texture2D backgroundTexture, Color color)
        {
            if (backgroundTexture == null)
            {
                Log.Error("SetAlphaFor, Null backgroundTexture, id: " + id);
                return;
            }
            Texture2D copyTexture = GUISkinCopy.CopyTexture2D(backgroundTexture);

            var pixels = copyTexture.GetPixels32();
            for (int i = 0; i < pixels.Length; ++i)
                pixels[i].a = (byte)Alpha;


            copyTexture.SetPixels32(pixels);
            copyTexture.Apply();

            style.active.background =
                style.normal.background =
                style.hover.background =
                style.onNormal.background =
                style.onHover.background =
                style.onActive.background =
                style.focused.background =
                style.onFocused.background =
                style.onNormal.background =
                style.normal.background = copyTexture;

            style.active.textColor =
                style.normal.textColor =
                style.hover.textColor =
                style.onNormal.textColor =
                style.onHover.textColor =
                style.onActive.textColor =
                style.focused.textColor =
                style.onFocused.textColor =
                style.onNormal.textColor =
                style.normal.textColor = color;
        }



    }
}
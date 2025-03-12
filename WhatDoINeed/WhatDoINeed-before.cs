#define SCANSAT


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


namespace WhatDoINeed
{
    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public partial class WhatDoINeed : MonoBehaviour
    {
        private static ToolbarControl toolbarControl;
        //internal static ToolbarControl Toolbar { get { return toolbarControl; } }

        bool hide = false;
        void OnHideUI() { hide = true; }
        void OnShowUI() { hide = false; }
        bool Hide { get { return hide; } }

        bool visible = false;
        bool selectVisible = false;
        bool settingsVisible = false;
        int winId, selWinId, manualContractWinId;
        double quickHideEnd = 0;

        public const float SEL_WINDOW_WIDTH = 400;
        public const float SEL_WINDOW_HEIGHT = 300;

        public const float WIN_WIDTH = 400;
        public const float WIN_HEIGHT = 600;

        internal const string MODID = "WhatDoINeed";
        internal const string MODNAME = "What Do I Need?";

        Rect selWinPos = new Rect(Screen.width / 2 - SEL_WINDOW_WIDTH / 2, Screen.height / 2 - SEL_WINDOW_HEIGHT / 2, SEL_WINDOW_WIDTH, SEL_WINDOW_HEIGHT);

        int numDisplayedContracts = 0;
        int newNumDisplayedContracts = 0;
        string contractText = "Contracts";

        bool resizingWindow = false;

        Vector2 scrollPos;

        bool kisAvailable = false;
        bool scansatAvailable = false;

        //const string htmlRed = "<color=#ff0000>";
        const string htmlRed = "<color=#ff3c3c>";
        //const string htmlRed = "<color=#fff12a>";  // Light yellow (copied from colors found in Mission Control)
        const string htmlGreen = "<color=#00ff00>";
        //const string htmlGreen = "<color=#8cf893>"; // Light green (copied from colors found in Mission Control)
        const string htmlPaleblue = "<color=#acfcff>";

        const string htmlOrange = "<color=#ffa500>";
        const string htmlRedOrange = "<color=#ff4d00>";
        const string htmlYellow = "<color=#ffff00>";
        const string htmlMagenta = "<color=#ff00ff>";
        const string htmlCyan = "<color=#00FFFF>";

        int btnId;

        Repository repository;


        AvailablePart GetPartByIndex(string index)
        {
            return IsPartAvailable(index);
        }


        public void Start()
        {
            if (!HighLogic.LoadedSceneIsEditor || !(HighLogic.CurrentGame.Mode == Game.Modes.CAREER || HighLogic.CurrentGame.Mode == Game.Modes.SCIENCE_SANDBOX))
            {
                Log.Info("Sandbox mode, exiting");
                return;
            }

#if SCANSAT
            scansatAvailable = HasMod.hasMod("SCANsat");
#endif
            kisAvailable = KISWrapper.Initialize();

            lastAlpha = -1;

            repository = new Repository();

            ScanContracts();
            SetUpExperimentParts();
#if SCANSAT
            if (scansatAvailable)
                SetupSCANsatStrings();
#endif
            InitializePartsAvailability();

            GameEvents.onHideUI.Add(OnHideUI);
            GameEvents.onShowUI.Add(OnShowUI);


            GameEvents.onEditorPodSelected.Add(EditorSelectedPickedDeleted);
            GameEvents.onEditorPodPicked.Add(EditorSelectedPickedDeleted);
            GameEvents.onEditorPodDeleted.Add(onEditorPodDeleted);
            GameEvents.onEditorPartEvent.Add(onEditorPartEvent);

            GameEvents.onGameSceneLoadRequested.Add(onGameSceneLoadRequested);

            winId = WindowHelper.NextWindowId("WhatDoINeed");
            selWinId = WindowHelper.NextWindowId("CCD_Select");
            manualContractWinId = WindowHelper.NextWindowId("ManualContractEntry");

            if (toolbarControl == null)
            {
                toolbarControl = gameObject.AddComponent<ToolbarControl>();
                toolbarControl.AddToAllToolbars(GUIToggle, GUIToggle,
                     ApplicationLauncher.AppScenes.SPH | ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.TRACKSTATION | ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.SPACECENTER | ApplicationLauncher.AppScenes.MAPVIEW,
                     MODID,
                     "CCD_Btn",
                     "WhatDoINeed/PluginData/textures/WhatDoINeed-38.png",
                     "WhatDoINeed/PluginData/textures/WhatDoINeed-24.png",
                    MODNAME);
            }

            ButtonManager.BtnManager.InitializeListener(EditorLogic.fetch.launchBtn, EditorLogic.fetch.launchVessel, "What Do I Need?");
            btnId = ButtonManager.BtnManager.AddListener(EditorLogic.fetch.launchBtn, OnLaunchButtonInput, "What Do I Need?", "What Do I Need?");

            SetWinPos();
            if (!Settings.Instance.helpWindowShown)
            {
                ShowHelpWindow();
                Settings.Instance.helpWindowShown = true;
                //Settings.Instance.SaveData();
            }
            if (Settings.Instance.reopenIfLastOpen)
            {
                //toolbarControl.buttonActive =
                visible = Settings.Instance.lastVisibleStatus;
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

            GameEvents.onEditorPartEvent.Remove(onEditorPartEvent);
            GameEvents.onEditorPodSelected.Remove(EditorSelectedPickedDeleted);
            GameEvents.onEditorPodPicked.Remove(EditorSelectedPickedDeleted);
            GameEvents.onEditorPodDeleted.Remove(onEditorPodDeleted);

            GameEvents.onGameSceneLoadRequested.Remove(onGameSceneLoadRequested);
        }

        const int WIDTH = 300;
        const int HEIGHT = 200;

        public void OnLaunchButtonInput()
        {
            if (Settings.Instance.checkForMissingBeforeLaunch)
            {
                int numSelectedcontracts = 0;
                int numMissingExperiments = 0;
                foreach (var contract in Settings.Instance.activeContracts)
                {
                    if (contract.Value.selected || !Settings.Instance.onlyCheckSelectedContracts)
                    {
                        numSelectedcontracts++;
                        foreach (var expPart in experimentParts.Values)
                        {
                            if (expPart.contractGuid == contract.Value.contractContainer.ID)
                            {
                                if (expPart.parts.Count != 0)
                                {
                                    bool partFound = false;
                                    for (int i = 0; i < expPart.parts.Count; i++)
                                    {
                                        var part = expPart.parts[i];
                                        if (part.part.category != PartCategories.none && part.partTitle != "")
                                        {
                                            if (part.numAvailable > 0)
                                            {
                                                partFound = true;
                                                break;
                                            }
                                        }
                                    }
                                    if (!partFound)
                                        numMissingExperiments++;
                                }
                            }
                        }
                    }
                }
                if (numMissingExperiments > 0)
                {
                    // You have active contracts that requires some parts that your vessel currently does not have.Are you sure you want to launch?
#if true
                    PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f),
                       new Vector2(0.5f, 0.5f),
                       new MultiOptionDialog("Fill It Up",
                           "You have active contracts that requires some parts\n" +
                           "that your vessel currently does not have.\n\n" +
                           "There " + ((numSelectedcontracts == 1) ? "is " : "are ") + numSelectedcontracts + " selected contract" + ((numSelectedcontracts == 1) ? "\n" : "s\n") +
                           htmlRed + "There " + ((numMissingExperiments == 1) ? "is " : "are ") + numMissingExperiments + " missing experiment" + ((numMissingExperiments == 1) ? "\n\n" : "s\n\n") + "</color>" +
                           "Are you sure you want to launch?\n\n" +
                           "Please select your option from the choices below",
                           "Unfullfillable Contracts",
                           HighLogic.UISkin,
                           new Rect(0.5f, 0.5f, WIDTH, HEIGHT),
                           new DialogGUIFlexibleSpace(),
                           new DialogGUIVerticalLayout(
                               new DialogGUIFlexibleSpace(),

                               new DialogGUIHorizontalLayout(
                                   new DialogGUIFlexibleSpace(),
                                   new DialogGUIButton("OK to launch",
                                       delegate
                                       {
                                           //ResetDelegates();
                                           //Log.Info("OnLaunchButtonInput 1");
                                           //defaultLaunchDelegate();
                                           ButtonManager.BtnManager.InvokeNextDelegate(btnId, "What-Do-I-Need-next");

                                       }, 240.0f, 30.0f, true),
                                    new DialogGUIFlexibleSpace()
                                ),

                                new DialogGUIFlexibleSpace(),

                                new DialogGUIHorizontalLayout(
                                   new DialogGUIFlexibleSpace(),
                                   new DialogGUIButton("Cancel", () => { }, 240.0f, 30.0f, true),
                                   new DialogGUIFlexibleSpace()
                                   )
                               )
                           ),
                            false,
                            HighLogic.UISkin);
                }
                else
                {
                    //Log.Info("OnLaunchButtonInput 4");
                    ButtonManager.BtnManager.InvokeNextDelegate(btnId, "What-Do-I-Need-next");
#endif
                }
            }
            else
            {
                //Log.Info("OnLaunchButtonInput 3");
                ButtonManager.BtnManager.InvokeNextDelegate(btnId, "What-Do-I-Need-next");
            }
        }

        void onGameSceneLoadRequested(GameScenes scene)
        {
            visible = false;
        }
        void EditorSelectedPickedDeleted(Part p)
        {
            ScanShip();
        }
        void onEditorPodDeleted()
        {
            ScanShip();
        }

        void Update()
        {
            if (Input.GetMouseButtonUp(0) || Input.GetMouseButtonUp(1) || Input.GetMouseButtonUp(2))
            {
                //Log.Info("Update, Input.GetMouseButtonUp(0)");
                ScanShip();
            }
        }
        void onEditorPartEvent(ConstructionEventType constrE, Part part)
        {
            ScanShip();
        }


        bool shipScanned = false; // Used to show the displayed contracts in the log file only 1 time instead of repeating in the log file


        /// <summary>
        /// Examines a part looking to classify it for future use
        /// </summary>
        /// <param name="part"></param>
        void ScanPart(Part part)
        {
            repository.shipInfo.AddPart(part.name);

            if (part.isAntenna(out ModuleDeployableAntenna antenna))
                repository.shipInfo.AddModuleType("Antenna");
            if (part.name.ToLower().Contains("battery"))
                repository.shipInfo.AddModuleType("Battery");
            if (part.dockingPorts.Count > 0)
                repository.shipInfo.AddModuleType("Dock");

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
            //Log.Info("ScanShip");
            shipScanned = true;

            repository.shipInfo.Reinitialize();
            for (int i = 0; i < EditorLogic.fetch.ship.parts.Count; i++)
            {
                Part part = EditorLogic.fetch.ship.parts[i];
                if (part == null)
                {
                    continue;
                }
                ScanPart(part);
                SetPartOnShip(part);
            }



            // First set all experiments to false, then scan
            // the vessel's parts
            if (experimentParts == null)
            {
                Log.Error("ScanShip: experimentParts is null");
                return;
            }
            foreach (var ep in experimentParts)
            {
                ep.Value.numExpAvail = 0;

                for (int i = 0; i < ep.Value.parts.Count; i++)
                {
                    var p = ep.Value.parts[i];
                    p.numAvailable = 0;
                }
            }
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
            for (int i = 0; i < EditorLogic.fetch.ship.parts.Count; i++)
            {
                var p = EditorLogic.fetch.ship.parts[i];
                if (p == null)
                {
                    continue;
                }
                foreach (var ep in experimentParts)
                {
                    for (int j = 0; j < ep.Value.parts.Count; j++)
                    {
                        var ap = ep.Value.parts[j];
                        if (p.name == ap.part.name)
                        {
                            ep.Value.numExpAvail++;
                            ap.numAvailable++;
                        }
                    }
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
                            foreach (var ep in experimentParts)
                            {
                                for (int i2 = 0; i2 < ep.Value.parts.Count; i2++)
                                {
                                    var ap = ep.Value.parts[i2];
                                    if (ap.part.name == storedPart.partName)
                                    {
                                        ep.Value.numExpAvail++;
                                        ap.numAvailable++;
                                        //Log.Info("ScanShip, found part: " + p.name);
                                    }
                                }
                            }
                        }
                    }
                }

            }

            // Scan all parts stored in Kerbal Inventory System inventories
            if (kisAvailable)
                ScanShipKISInventory();

#if !DEBUG
            if (Settings.Instance.debugMode)
#endif
            DumpAllScannedData();
        }

        void DumpAllScannedData()
        {
            Log.Info("======================================================================");
            Log.Info("======================================================================");
            DumpPartInfoList();
            Log.Info("vvvvvvvvvvvv allExperimentParts Dump vvvvvvvvvvvvv");
            foreach (var e in allExperimentParts)
            {
                Log.Info("Experiment: " + e.Value.experimentName);
                string p = "";
                foreach (var part in e.Value.parts)
                {
                    p += part + ", ";
                }
                Log.Info("    Parts: " + p);
            }
            Log.Info("^^^^^^^^^^^^ allExperimentParts Dump ^^^^^^^^^^^^^\n");

            Log.Info("vvvvvvvvvvvv Repository Dump vvvvvvvvvvvvv");
            Log.Info("  Contracts");
            foreach (var c in repository.Contracts.Values)
            {
                Log.Info("Contract: " + c.Log());
                foreach (var eg in c.ExperimentGroups)
                {
                    Log.Info("    Experiment group name: " + eg.Key);
                    foreach (Experiment e in eg.Value)
                        Log.Info("        " + e.Log());
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
                    if (p.Vessels.Count > 0)
                        Log.Info("        RequestedVessels: " + p.LogVessels());
                    if (p.subjectId != null)
                        Log.Info("        SubjectID: " + p.subjectId);
                    if (p.CheckModules.Count > 0)
                    {
                        foreach (var cm in p.CheckModules)
                            Log.Info("        ModuleTypes: " + cm.ModuleTypes + ", Description: " + cm.Description + ", numAvailable: " + cm.numAvailable);
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
                Log.Info("AvailableParts");
                foreach (var pg in c.NeededParts)
                {
                    Log.Info("  PartGroup: " + pg.Key);
                    foreach (var e in pg.Value)
                    {
                        Log.Info("      AvailablePart: " + e.Log());
                        Log.Info("         Experiments: " + e.LogExperiments());
                    }
                }
            }
            Log.Info("--------");

            Log.Info("^^^^^^^^^^^^ Repository Dump ^^^^^^^^^^^^^\n");


            Log.Info("=====================================");
            Log.Info("Data Dump, contracts with all experiments listed");
            Log.Info("=====================================");
            foreach (var contract in Settings.Instance.activeContracts)
            {
                Log.Info("GUID: " + contract.Key.ToString() + ", Title: " + contract.Value.contractContainer.Title + ", # Experiments: " + contract.Value.experiments.Count);
                string exp = "";
                foreach (var e in contract.Value.experiments)
                {
                    exp += e.ToString() + ", ";
                }
                Log.Info("     Experiments: " + exp);
            }

            Log.Info("=====================================");
            Log.Info("Data Dump, Experiment Parts (parts with experiments needed for active contracts)");
            Log.Info("=====================================");
            foreach (var ep in experimentParts)
            {
                ContractExperimentPart expPart = ep.Value;
                if (expPart.scanSatExperiment)
                    Log.Info("Key: " + ep.Key + ", SCANSat part: ep.ExperimentID: " + expPart.experimentID + ", scanType: " +
                        expPart.scanType.ToString() + ", scantype: " + (int)expPart.scanType + ", experimentTitle: " +
                        expPart.experimentTitle + ", contractGuid: " + expPart.contractGuid + ", numExpAvail: " + expPart.numExpAvail +
                        ", # parts: " + expPart.parts.Count);
                else
                    Log.Info("Key: " + ep.Key + ", ep.ExperimentID: " + expPart.experimentID + ", experimentTitle: " +
                        expPart.experimentTitle + ", contractGuid: " + expPart.contractGuid + ", numExpAvail: " + expPart.numExpAvail +
                        ", # parts: " + expPart.parts.Count);

                foreach (AvailPartWrapper part in expPart.parts)
                {
                    if (part.scanSatPart)
                        Log.Info("  part.partTitle: " + part.partTitle + ", numAvailable: " + part.numAvailable + ", scanType: " + part.scanType + ", scanType(int): " + (int)part.scanType +
                            ", scanSatPart: " + part.scanSatPart);
                    else
                        Log.Info("  part.partTitle: " + part.partTitle + ", numAvailable: " + part.numAvailable);
                }
            }


            Log.Info("=====================================");
            Log.Info("Data Dump, ActiveContracts");
            Log.Info("=====================================");
            foreach (var contract in Settings.Instance.activeContracts)
            {
                Log.Info("GUID: " + contract.Key.ToString() + ", Title: " + contract.Value.contractContainer.Title);
            }
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

                    //Log.Info("ScanShipKISInventory kisInvPart: " + kisInvPart);

                    foreach (var ep in experimentParts)
                    {
                        for (int j = 0; j < ep.Value.parts.Count; j++)
                        {
                            var ap = ep.Value.parts[j];
                            if (kisInvPart == ap.part.name)
                            {
                                ep.Value.numExpAvail++;
                                ap.numAvailable++;
                            }
                        }
                    }
                }
            }
        }



        Dictionary<string, ContractExperimentPart> experimentParts = new Dictionary<string, ContractExperimentPart>();
        //MultiKeyMultiDataDictionary<string, string, ContractExperimentPart, AvailPartWrapper> experimentPartsExpanded = new MultiKeyMultiDataDictionary<string, string, ContractExperimentPart, AvailPartWrapper>();
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

                                    activeLocalContracts.Add(contractGuid);
                                    for (int k = 0; k < param_s.Length; k++)
                                    {
                                        if (state != "Complete" && state != "Cancelled")
                                            ProcessConfigNode(contract, state, contractGuid, type, param_s[k]);
                                    }
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
                                        for (int k = 0; k < activeLocalContracts.Count; k++)
                                        {
                                            var activeContract = activeLocalContracts[k];

                                            var cet = new CEP_Key_Tuple(experimentID, activeContract).Key();
#if DEBUG
                                            //if (show)
                                            //    Log.Info("part: " + p.name + ", title: " + p.title + ", experiment module name: " + name + ", expid: " + experimentID + ", key: " + cet);
#endif
                                            if (experimentParts.ContainsKey(cet))
                                            {
#if DEBUG
                                                //    if (show)
                                                //        Log.Info("part: " + p.name + ", title: " + p.title + ", experiment module name: " + name + ", expid: " + experimentID + ", contract: " + activeContract);
#endif
                                                experimentParts[cet].parts.Add(new AvailPartWrapper(p));

                                                //experimentPartsExpanded.Add(cet, p.name, new AvailPartWrapper(p));


                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
#if false
            Log.Info("=====================================");
            Log.Info("Data Dump, experimentParts");
            Log.Info("=====================================");

            foreach (var epall in experimentParts)
            {
                string parts = "";
                foreach (var p in epall.Value.parts)
                    parts += p.part.name + ", ";
                if (!epall.Value.scanSatPart)
                    Log.Info("Key: " + epall.Key + ",    Parts: " + parts);
                else
                    Log.Info("Key: " + epall.Key + ",    Parts: " + parts + ", scanType: " + (int)epall.Value.scanType);
            }
#endif
            //Log.Info("=====================================");
            //Log.Info("Scanning ship, looking for parts with experiments");
            //Log.Info("=====================================");

            ScanShip();
        }

        // Dictionary<string, string>  moduleTitles = new Dictionary<string, string>();

        void InitializeModuleTitles()
        {
            foreach (AvailablePart ap in PartLoader.LoadedPartsList)
                foreach (AvailablePart.ModuleInfo mi in ap.moduleInfos)
                {
                    if (!moduleTitles.ContainsKey(mi.moduleName))
                        moduleTitles.Add(mi.moduleName, mi.moduleDisplayName);
                }
        }

        /// <summary>
        /// Needs to be optimized 
        /// </summary>
        /// <param name="partModule"></param>
        /// <returns></returns>
        string GetPartModuleTitle(string filterPartModule)
        {
            if (moduleTitles.Count == 0)
                InitializeModuleTitles();

            // Following is because for some reason, there is no ModuleTrackBodies found, but Track Bodies is, for TarsierSpaceTechnology
            // 
            if (filterPartModule == "ModuleTrackBodies")
            {
                filterPartModule = "Track Bodies";
            }
            return moduleTitles[filterPartModule];
        }

        void ProcessConfigNode(ConfigNode contract, string state, Guid contractGuid, string type, ConfigNode param)
        {
            string param_name = param.GetValue("name");
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

                if (experiment == null)
                {
                    Log.Info("param_Name: " + param_name + ", type: " + type);
                    switch (param_name)
                    {
#if SCANSAT
                        case "SCANsatCoverage": // For SCANsat
                            {
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

                                parameter = new Param(param_name, param_name);
                                parameter.scanName = scanName;
                                parameter.scanType = (SCANsatSCANtype)scansatExpIDShort;

                                //repository.Contracts[contractGuid].AddParam(parameter);

                                short a = Convert.ToInt16(scansatExpID);
                                ckt = new CEP_Key_Tuple(a.ToString(), contractGuid, true);
                                //cep = new ContractExperimentPart(ckt, scansatExpID);

                                repository.AddExperimentToContract(contractGuid, ckt.Key(), new Experiment(1, ckt, scansatExpID));

                                experiment = "";
                                break;
                            }
#endif
                        // Handle the OrbitalScience here
                        case "StnSciParameter":
                            {
                                param.TryGetValue("experimentType", ref experiment);  // Now check for Station Science experiments
                                if (experiment != null)
                                {

                                    //experimentDetail = experiment.Remove(0, 16);
                                    //string a = experimentDetail[0].ToString().ToLower();
                                    //experimentDetail = a + experimentDetail.Remove(0, 1);
                                    experimentDetail = experiment;
                                    experiment = experiment.Substring(0, 16); // should be StnSciExperiment

                                }
                                parameter = new Param(param_name, param_name);
                                parameter.experimentType = experiment;
                                repository.Contracts[contractGuid].AddParam(parameter);

                                break;
                            }

#if false
                                                // Not sure if following will show up as s unique contract in the contract system, need to test
                                                case "USAdvancedScience":  //Universal Storage    (probably not Station Science experiments)
                                                    {
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
                                if (param_name == "RepairPartParameter" || param_name == "ConstructionParameter")
                                    param_part = param.GetValue("partName");
                                else
                                    param_part = param.GetValue("part");

                                //parameter = new Param(param_name, param_name);
                                //parameter.AddPartName(param_part);
                                //repository.Contracts[contractGuid].AddParam(parameter);

                                CEP_Key_Tuple ckt = new CEP_Key_Tuple(param_name, contractGuid, param_part);
                                var p = GetPartByIndex(param_part);
                                if (p != null)
                                    repository.Contracts[contractGuid].AddNeededPart(1, p.name, new AvailPartWrapper2(p));

                                repository.AddExperimentToContract(contractGuid, ckt.Key(), new Experiment(2, ckt));
                                // zzz ScanLoadedParts();

                                if (!experimentParts.ContainsKey(ckt.Key()))
                                {
#if false
                                                            Log.Info("Contract guid: " + contractGuid + ", experiment: " + param_name + ", part: " + param_part + ", key: " + ckt.Key());
#endif
                                    experimentParts.Add(ckt.Key(), new ContractExperimentPart(ckt));

                                    for (int l = 0; l < PartLoader.LoadedPartsList.Count; l++)
                                    {
                                        /*AvailablePart*/ p = PartLoader.LoadedPartsList[l];
                                        if (p.name == param_part)
                                        {
                                            experimentParts[ckt.Key()].parts.Add(new AvailPartWrapper(p));
                                            //experimentPartsExpanded.Add(ckt.Key(), p.name, new AvailPartWrapper(p));

                                            //if (!Settings.Instance.activeContracts.ContainsKey(contractGuid))
                                            if (param_part == null)
                                                Log.Error("Error 1, param_part is null");
                                            Settings.Instance.activeContracts[contractGuid].experiments.Add(param_name, param_part);
                                            break;
                                        }
                                    }
                                }
                                break;
                            }

                        case "VesselSystemsParameter":
                            // checkModuleTypes = Antenna | Generator | Dock
                            // checkModuleDescriptions = has an antenna| has a docking port | can generate power
                            {
                                string checkModuleTypes = null;
                                string checkModuleDescriptions = null;

                                param.TryGetValue("checkModuleTypes", ref checkModuleTypes);
                                param.TryGetValue("checkModuleDescriptions", ref checkModuleDescriptions);

                                var moduleTypes = checkModuleTypes.Split('|');
                                var moduleDescriptions = checkModuleDescriptions.Split('|');
                                if (moduleTypes.Length != moduleDescriptions.Length)
                                    Log.Error("moduleTypes count doesn't match moduleDescriptions.");
                                parameter = new Param(param_name, type);
                                for (int i1 = 0; i1 < moduleTypes.Length; i1++)
                                {
                                    parameter.CheckModules.Add(new CheckModule(moduleTypes[i1], moduleDescriptions[i1]));
                                }
                                repository.Contracts[contractGuid].AddParam(parameter);
                            }
                            break;

                        case "PartRequestParameter":
                            // partNames = cupola|sspx-cupola-125-1|sspx-cupola-1875-1|sspx-cupola-375-1|sspx-observation-25-1|sspx-dome-5-1
                            // moduleNames = ModuleScienceLab
                            string partNames = null;
                            string moduleNames = null;

                            parameter = new Param(param_name, param_name);

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
                            repository.Contracts[contractGuid].AddParam(parameter);

                            break;

                        case "DMLongOrbitParameter": // This is for the DMagic science stuff
                            {

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
                                        foreach (var r in rp)
                                        {
                                            parameter = new Param(param_name, type);
                                            parameter.AddRequestedParts(r.Split(',').ToList());
                                            repository.Contracts[contractGuid].AddParam(parameter);
                                        }
                                    }
                                }


                                for (int l = 0; l < partRequestParams.Length; l++)
                                {
                                    var prp = partRequestParams[l];
                                    string requestedParts = prp.GetValue("Requested_Parts");
                                    if (requestedParts != null)
                                    {
                                        //List<String> 
                                        List<string> requestedPartsList = requestedParts.Split(',', '|').ToList<string>();
                                        for (int m = 0; m < requestedPartsList.Count; m++)
                                        {
                                            string part = requestedPartsList[m];
                                            CEP_Key_Tuple ckt = new CEP_Key_Tuple(param_name, contractGuid, part);
                                            if (!experimentParts.ContainsKey(ckt.Key()))
                                            {
#if false
                                                                        Log.Info("DMPartRequestParameter, Contract guid: " + contractGuid + ", experiment: " + part + ", part: " + part + ", key: " + ckt.Key());
#endif
                                                for (int n = 0; n < PartLoader.LoadedPartsList.Count; n++)
                                                {
                                                    AvailablePart p = PartLoader.LoadedPartsList[n];
                                                    if (p.name == part && p.category != PartCategories.none)
                                                    {
                                                        experimentParts.Add(ckt.Key(), new ContractExperimentPart(ckt));
                                                        experimentParts[ckt.Key()].parts.Add(new AvailPartWrapper(p));

                                                        //experimentPartsExpanded.Add(ckt.Key(), p.name, new AvailPartWrapper(p));


                                                        //if (!Settings.Instance.activeContracts.ContainsKey(contractGuid))
                                                        if (experiment == null)
                                                        {
                                                            //Log.Info("DMPartRequestParameter, Contract guid: " + contractGuid + ", experiment: " + part + ", part: " + part + ", key: " + ckt.Key());
                                                            //Log.Error("Error 2, experiment is null");
                                                        }
                                                        else
                                                            Settings.Instance.activeContracts[contractGuid].experiments.Add(experiment, experiment);

                                                        break;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                break;
                            }

                        case "PartValidation": // For REPOSoftTech/ResearchBodies
                            {
                                //Log.Info("PartValidation");
                                parameter = new Param(param_name, param_name);

                                var partValidationFilterParams = param.GetNodes("FILTER");
                                //Log.Info("PartValidation, number of FILTERs: " + partValidationFilterParams.Length);

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
                                        //Log.Info("FILTER partModule, reqPartModule: " + filterPartModule);
                                        parameter.Filters.Add(new Filter("partModule", filterPartModule, filterPartModuleTitle));

                                    }
                                    else
                                    {
                                        prp.TryGetValue("category", ref filterCategory);
                                        if (filterCategory != null)
                                        {
                                            //Log.Info("FILTER, filterCategory: " + filterCategory);
                                            parameter.Filters.Add(new Filter("category", filterCategory, ""));
                                        }
                                        else
                                        {
                                            //Log.Info("FILTER, type: " + filterType);
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
                                                                    parameter.Filters.Add(new Filter("NONE", "name", aname));
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
                                    repository.Contracts[contractGuid].AddParam(parameter);
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
                                    var reqPartModules = prp.GetValues("partModule");
                                    //Log.Info("PartValidation 1");
                                    for (int i1 = 0; i1 < reqPartModules.Length; i1++)
                                    {
                                        string requestedPartModule = reqPartModules[i1];
                                        if (requestedPartModule != null)
                                        {
                                            //Log.Info("partModule: " + requestedPartModule + ", i1: " + i1 + ", param_name: " + param_name);
                                            CEP_Key_Tuple ckt = new CEP_Key_Tuple(param_name, contractGuid, requestedPartModule);

                                            if (!experimentParts.ContainsKey(ckt.Key()))
                                            {
                                                for (int n = 0; n < PartLoader.LoadedPartsList.Count; n++)
                                                {
                                                    AvailablePart p = PartLoader.LoadedPartsList[n];
                                                    //Log.Info("PartValidation 1.2 p: " + p.name);
                                                    if (p.category != PartCategories.none)
                                                    {
                                                        var mNodesList = p.partConfig.GetNodes("MODULE");
                                                        for (int o = 0; o < mNodesList.Length; o++)
                                                        {
                                                            var mNode = mNodesList[o];
                                                            string moduleName = mNode.GetValue("name");

                                                            if (moduleName == requestedPartModule)
                                                            {
                                                                //Log.Info("p.name: " + p.name);
                                                                //repository.AddPart(p.name, new AvailPartWrapper2(GetPartByIndex(p.name)));

                                                                //repository.AddExperimentToContract(contractGuid, ckt.Key(), new Experiment(3, ckt));

#if false
                                                                repository.AssignExperimentToPart(p.name, p.name, param_name);
                                                                repository.AddExperimentToContract(contractGuid, ckt.Key(), new Experiment(ckt));
#endif

                                                                if (!experimentParts.ContainsKey(ckt.Key()))
                                                                    experimentParts.Add(ckt.Key(), new ContractExperimentPart(ckt));
                                                                experimentParts[ckt.Key()].parts.Add(new AvailPartWrapper(p));

                                                                if (experiment == null)
                                                                    Log.Error("Error 3, experiment is null");
                                                                else
                                                                {
                                                                    experiment = "PartValidation";
                                                                    if (!Settings.Instance.activeContracts[contractGuid].experiments.ContainsKey(experiment))
                                                                        Settings.Instance.activeContracts[contractGuid].experiments.Add(experiment, experiment);
                                                                }
                                                                experiment = null;
#if false
                                                                                    if (moduleName.Substring(0, 2) == "dm")
                                                                                        Log.Info("Part: " + p.name + " has module: " + requestedPartModule + ", key: " + ckt.Key());
#endif
                                                                break;
                                                            }
                                                        }
                                                    }
                                                }
                                                //Log.Info("SetUpExperimentPart 2");
                                            }
                                        }
                                    }
                                    Log.Info("PartValidation 2");

                                    //else
                                    //Log.Info("SetUpExperimentPart 3");

                                    // Now check for required parts
                                    {
                                        var requestedParts = prp.GetValues("part");
                                        for (int o = 0; o < requestedParts.Length; o++)
                                        {
                                            string requestedPart = requestedParts[o];
                                            if (requestedPart != null)
                                            {
                                                CEP_Key_Tuple ckt = new CEP_Key_Tuple(param_name, contractGuid, requestedPart);

                                                {
                                                    //Log.Info("requestedPart: " + requestedPart);
                                                    AvailablePart p = GetPartByIndex(requestedPart);
                                                    if (p != null)
                                                        repository.Contracts[contractGuid].AddNeededPart(2, requestedPart, new AvailPartWrapper2(p));

                                                    //repository.AssignExperimentToPart(requestedPart, requestedPart, param_name);

                                                    repository.AddExperimentToContract(contractGuid, ckt.Key(), new Experiment(4, ckt));

#if true
                                                    //Log.Info("Contract guid: " + contractGuid + ", experiment: " + "PartValidation" + ", requestedPart: " + requestedPart + ", key: " + ckt.Key());
#endif
                                                    for (int n = 0; n < PartLoader.LoadedPartsList.Count; n++)
                                                    {
                                                        /*AvailablePart */ p = PartLoader.LoadedPartsList[n];
                                                        if (p.name == requestedPart)
                                                        {
                                                            if (!experimentParts.ContainsKey(ckt.Key()))
                                                                experimentParts.Add(ckt.Key(), new ContractExperimentPart(ckt));
                                                            experimentParts[ckt.Key()].parts.Add(new AvailPartWrapper(p));

                                                            //experimentPartsExpanded.Add(ckt.Key(), p.name, new AvailPartWrapper(p));

                                                            experiment = "PartValidation";
                                                            if (!Settings.Instance.activeContracts[contractGuid].experiments.ContainsKey(experiment))
                                                                Settings.Instance.activeContracts[contractGuid].experiments.Add(experiment, experiment);
                                                            experiment = null;

                                                            break;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    //Log.Info("SetUpExperimentPart 4");
                                    Log.Info("PartValidation 3");

                                }
                                break;
                            }
                        case "CollectDeployedScience":
                            {
                                {
                                    string subjectId = null;
                                    param.TryGetValue("subjectId", ref subjectId);

                                    if (subjectId != null)
                                    {
                                        parameter = new Param(param_name, param_name);
                                        parameter.subjectId = subjectId;
                                        repository.Contracts[contractGuid].AddParam(parameter);

                                    }
                                }
                                break;
                            }
                    }
                }
                if (experiment != null)
                {
                    CEP_Key_Tuple ckt;
                    ContractExperimentPart cep;
                    // string scansatExp = null;
                    if (scansatAvailable && scansatExpID != null)
                    {
                        short a = Convert.ToInt16(scansatExpID);
                        //scansatExp = ("SCANsat" + (SCANsatSCANtype)a).ToString();

                        ckt = new CEP_Key_Tuple(a.ToString(), contractGuid, true);

                        cep = new ContractExperimentPart(ckt, scansatExpID);
                    }
                    else
                    {
                        ckt = new CEP_Key_Tuple(experiment, contractGuid);
                        cep = new ContractExperimentPart(ckt);
                    }

                    if (!experimentParts.ContainsKey(ckt.Key()))
                    {
                        if (scansatAvailable && scansatExpID != null)
                        {
#if DEBUG
                            //Log.Info("Contract guid: " + contractGuid + ", experiment: " + experiment + ", key: " + ckt.Key() + ", scansatExpID: " + scansatExpID);
#endif
                            experimentParts.Add(ckt.Key(), cep);
                        }
                        else
                        {
#if DEBUG
                            //Log.Info("Contract guid: " + contractGuid + ", experiment: " + experiment + ", key: " + ckt.Key());
#endif
                            experimentParts.Add(ckt.Key(), cep);
                        }
                        //Log.Info("Adding experiment to Settings.Instance.activecontracts, guid: " + contractGuid);

                        if (experiment == null)
                            Log.Error("Error 4, experiment is null");

                        //repository.AddExperimentToContract(contractGuid, ckt.Key(), new Experiment(ckt));

                        if (scansatExpID != null)
                        {

                            //Log.Info("Adding experiment to activeContracts, experiment: " + ckt.fullExpID() /* scansatExp */ + ", scansatExpID: " + scansatExpID);
                            Settings.Instance.activeContracts[contractGuid].experiments.Add(ckt.fullExpID() /* scansatExp */ , scansatExpID);
                        }
                        else
                        {
                            //Log.Info("Adding experiment to activeContracts, experiment: " + experiment);
                            if (!Settings.Instance.activeContracts[contractGuid].experiments.ContainsKey(experiment))
                                Settings.Instance.activeContracts[contractGuid].experiments.Add(experiment, experiment);
                        }
                    }

                }
            }
        }


        void ScanContracts()
        {
            //return;
            //Log.Info("ScanContracts");
            //Settings.Instance.activeContracts.Clear();
            var aContracts = contractParser.getActiveContracts;
            for (int i = 0; i < aContracts.Count; i++)
            {
                contractContainer contract = aContracts[i];
                var contractTypeTmp = contract.Root.GetType().ToString().Split('.');
                string contractType = contractTypeTmp[contractTypeTmp.Length - 1];
                //Log.Info("contractType: " + contractType);
                if (contractType != "PlantFlag" && contractType != "ExplorationContract")
                {
                    if (!Settings.Instance.activeContracts.ContainsKey(contract.ID))
                    {
                        Settings.Instance.activeContracts.Add(contract.ID, new Contract(contract));
                        //Log.Info("Contract Added: " + contract.ID);
                    }

                    repository.AddContract(new ContractWrapper(contract, contract.ID, contractType, contractType));
                }
            }
            //Log.Info("Active Contracts: " + Settings.Instance.activeContracts.Count);
            int cnt = 0;
            foreach (var contract in Settings.Instance.activeContracts)
            {
                //Log.Info("ScanContracts, Settings.Instance.activeContract: " + contract.Key);
                if (contract.Value.selected)
                    cnt++;
            }

            if (Settings.Instance.initialShowAll && cnt == 0) //Settings.Instance.activeContracts.Count == 0)
            {
                foreach (var contract in Settings.Instance.activeContracts)
                    contract.Value.selected = true;
                foreach (var contract in repository.Contracts)
                    contract.Value.selected = true;
            }
            else
            {
                foreach (var contract in Settings.Instance.activeContracts)
                    if (repository.Contracts.ContainsKey(contract.Key))
                        repository.Contracts[contract.Key].selected = contract.Value.selected;
            }


        }

        string DoSCANSatResourceScanner(ConfigNode experiment, AvailablePart p)
        {
            //Log.Info("DoSCANSatResourceScanner 1, part: " + p.name + ", Settings.Instance.activeContracts.Count: " + Settings.Instance.activeContracts.Count);
            string experimentID = "Nothing";

            string experimentType = experiment.GetValue("sensorType"); // SCANsat
            short s;
            if (short.TryParse(experimentType, out s))
            {
                SCANsatSCANtype scantype = (SCANsatSCANtype)s;

                experimentID = scantype.ToString();
                //Log.Info("DoSCANSatResourceScanner 2, part: " + p.name + ", ScannerType: " + s);

                //foreach (ContractExperimentPart ep in experimentParts.Values)
                {
                    //var a = ep.scanType & scantype;
                    //Log.Info("DoSCANSatResourceScanner 3, ep: " + ep.experimentID + ", scantype: " + ep.scanType + ", scantype: " + scantype + ", a: " + a);
                    //if (a != 0)
                    {
                        foreach (var activeContract in Settings.Instance.activeContracts)
                        {
                            //Log.Info("DoSCANSatResourceScanner 4, activeContract: " + activeContract.Key + ", numExperiments: " + activeContract.Value.experiments.Count);
                            foreach (var e in activeContract.Value.experiments)
                            {
                                if (short.TryParse(e.Value, out short expScanType))
                                {
                                    var a = expScanType & s;
                                    //Log.Info("DoSCANSatResourceScanner 5, expScanType: " + expScanType + ", scantype: " + scantype + ", scantype: " + scantype + ", a: " + a);
                                    if (a != 0)
                                    {
                                        var cet = new CEP_Key_Tuple(e.Key, activeContract.Key);
#if DEBUG
                                        //Log.Info("DoSCANSatResourceScanner 6, part: " + p.name + ", title: " + p.title + ", experiment module name: " + name + ", expid: " + experimentID + ", key: " + cet);
#endif
                                        if (!experimentParts.ContainsKey(cet.Key()))
                                        {
                                            //Log.Info("Adding to experimentParts, key: " + cet.Key() + ", contract: " + activeContract);
                                            experimentParts.Add(cet.Key(), new ContractExperimentPart(cet));
                                        }
                                        {
#if DEBUG
                                            //Log.Info("DoSCANSatResourceScanner, part: " + p.name + ", title: " + p.title + ", experiment module name: " + name + ", expid: " + experimentID + ", contract: " + activeContract);
#endif
                                            var experimentPart = experimentParts[cet.Key()];
                                            experimentPart.parts.Add(new AvailPartWrapper(p));

                                            //experimentPartsExpanded.Add(cet.Key(), p.name, new AvailPartWrapper(p));


                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            experimentID = "Nothing";
            //if (experimentID != "Nothing")
            //    Log.Info("part: " + p.name + ", ModuleResourceScanner,  experimentType: " + experimentID);
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
            string sensorTypeStr = experimentConfigNode.GetValue("sensorType"); // SCANsat
            short sensorType;
            if (short.TryParse(sensorTypeStr, out sensorType))
            {
                // Need to do a bitwise AND to see if the part has the required experiment

                SCANsatSCANtype scantype = (SCANsatSCANtype)sensorType;
                //Log.Info("DoSCANsatModule, part: " + p.name + ", experiment: " + experiment.name + ", type: " + s +
                //    ", type bitCount: " + CountBits(s));
                experimentID = scantype.ToString();

                foreach (var ep in Settings.Instance.activeContracts)
                {

                    var activeContract = ep.Key;
                    foreach (string expStr in ep.Value.experiments.Values)
                    {
                        if (short.TryParse(expStr, out short expsensorType))
                        {
                            var availableExperiment = expsensorType & (short)scantype;

                            if (availableExperiment != 0)
                            {
                                //Log.Info("DoSCANSatModule 2, expStr: " + expStr + ", expsensorType: " + (int)expsensorType + ", scantype: " + (int)scantype + ", sensorTypeStr: " + sensorTypeStr + ", a: " + (int)availableExperiment + ", contract: " + activeContract);
#if false
                            Log.Info("part: " + p.name +", title: " + p.title +  ", experiment module name: " + name + ", expid: " + experimentID + ", ep.scanType: " + (int)ep.scanType);
                            Log.Info("Bitwise comparision: " + (int)a);
#endif
                                //CEP_Key_Tuple cet = new CEP_Key_Tuple("SCANsat+" + availableExperiment.ToString(), activeContract);
                                CEP_Key_Tuple cet = new CEP_Key_Tuple("SCANsat", activeContract, ((SCANsatSCANtype)availableExperiment).ToString());
                                if (!experimentParts.ContainsKey(cet.Key()))
                                {
                                    //Log.Info("DoSCANSatModule 3, adding: " + cet.Key());
                                    experimentParts.Add(cet.Key(), new ContractExperimentPart(cet));
                                }

#if DEBUG
                                // Log.Info("DoSCANSatModule 4, part: " + availPart.name + ", title: " + availPart.title + ", experiment module name: SCANsat" + ", Key: " + cet.Key());
#endif
                                experimentParts[cet.Key()].parts.Add(new AvailPartWrapper(availPart, (SCANsatSCANtype)availableExperiment));

                                //experimentPartsExpanded.Add(cet.Key(), availPart.name, new AvailPartWrapper(availPart));


                                //break;
                                //Log.Info("DoSCANSatModule 5");
                            }
                        }
                    }
                }
#if false
#if false
                    Log.Info("part: " + availPart.name + ", experiment module name: " + name + ", expid: " + experimentID + ", key: " + cet);
#endif

                    foreach (SCANsatSCANtype st in Enum.GetValues(typeof(SCANsatSCANtype)))
                    {
                        if (CountBits((int)sensorType) != 1)
                            continue;
                        if ((st & scantype) == SCANsatSCANtype.Nothing)
                            continue;
                        Log.Info("part: " + availPart.name + ", experiment module name: " + name + ", expid: " + experimentID + ", sensorType: " + st.ToString());


#if false
                        if (experimentParts.ContainsKey(cet))
                    {
#if DEBUG
                        Log.Info("part: " + availPart.name + ", experiment module name: " + name + ", expid: " + experimentID + ", contract: " + activeContract);
#endif
                        var experimentPart = experimentParts[cet];
                        experimentPart.parts.Add(new AvailPartWrapper(p));
                    }
#endif
                    }
#endif

            }
            //Log.Info("DoSCANSatModule, experimentID: " + experimentID);
            //Log.Info("===============");
            experimentID = "Nothing";
            return experimentID;
        }

        public void SetWinPos()
        {

            //if (Settings.Instance.editorWinPos.width == 0)
            //    Settings.Instance.editorWinPos = new Rect(Settings.Instance.winPos);
            //else
            //    Settings.Instance.winPos = new Rect(Settings.Instance.editorWinPos);
            Settings.Instance.winPos.width = Mathf.Clamp(Settings.Instance.winPos.width, Settings.WINDOW_WIDTH, Screen.width);
            Settings.Instance.winPos.width = Math.Min(Settings.Instance.winPos.width, Settings.WINDOW_WIDTH);
            Settings.Instance.winPos.x = Math.Min(Settings.Instance.winPos.x, Screen.width - Settings.Instance.winPos.width);
            Settings.Instance.lastVisibleStatus = visible;

            //Settings.Instance.SaveData();
        }


        private void GUIToggle()
        {
            visible = !visible;
            Settings.Instance.lastVisibleStatus = visible;
        }

        public void OnGUI()
        {
            if (!(HighLogic.CurrentGame.Mode == Game.Modes.CAREER || HighLogic.CurrentGame.Mode == Game.Modes.SCIENCE_SANDBOX))
            {
                return;
            }

            if (!Hide && visible && Settings.Instance != null && Time.realtimeSinceStartup > quickHideEnd)
            {
                Rect tmpPos;
                //GUI.skin = HighLogic.Skin;
                SetAlpha(Settings.Instance.Alpha);


                {
                    //if (!selectVisible)
                    {
                        if (Settings.Instance.enableClickThrough && !settingsVisible)
                            tmpPos = GUILayout.Window(winId, Settings.Instance.winPos, ContractWindowDisplay, "What Do I Need? - Active " + contractText, Settings.Instance.kspWindow);
                        else
                            tmpPos = ClickThruBlocker.GUILayoutWindow(winId, Settings.Instance.winPos, ContractWindowDisplay, "What Do I Need? - Active " + contractText + " & Settings", Settings.Instance.kspWindow);
                        if (!Settings.Instance.lockPos)
                            Settings.Instance.winPos = tmpPos;
                    }
                    if (selectVisible)
                    {
                        selWinPos = ClickThruBlocker.GUILayoutWindow(selWinId, selWinPos, SelectContractWindowDisplay, "What Do I Need? - Contract Selection", Settings.Instance.kspWindow);
                    }
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
                    return partInfoByName.title;
                }
            }


            return "";
        }

        void ShowHelpWindow()
        {
            GameObject myplayer = new GameObject("HelpWindowClass");

            var w = myplayer.AddComponent<HelpWindowClass>();
            //Log.Info("Added HelpWindowClass");
        }

        Vector2 contractPos;
        Dictionary<string, bool> openClosed = new Dictionary<string, bool>();


        private void resizeWindow()
        {
            if (Input.GetMouseButtonUp(0))
            {
                resizingWindow = false;
            }

            if (resizingWindow)
            {
                Settings.Instance.winPos.width = Input.mousePosition.x - Settings.Instance.winPos.x + 10;
                Settings.Instance.winPos.width = Mathf.Clamp(Settings.Instance.winPos.width, Settings.WINDOW_WIDTH, Screen.width);
                Settings.Instance.winPos.height = (Screen.height - Input.mousePosition.y) - Settings.Instance.winPos.y + 10;
                Settings.Instance.winPos.height = Mathf.Clamp(Settings.Instance.winPos.height, Settings.WINDOW_HEIGHT, Screen.height);
            }
        }

        static internal void SetFontSizes(float fontSize, bool bold)
        {

            Settings.Instance.largeDisplayFont.fontSize = (int)fontSize + 2;
            Settings.Instance.largeDisplayFont.fontStyle = FontStyle.Bold; // bold ? FontStyle.Bold : FontStyle.Normal;
            Settings.Instance.largeDisplayFont.normal.textColor = Color.yellow;
            Settings.Instance.largeDisplayFont.border = new RectOffset();
            Settings.Instance.largeDisplayFont.padding = new RectOffset();
            Settings.Instance.largeDisplayFont.alignment = TextAnchor.LowerLeft;

            Settings.Instance.displayFont.fontSize = (int)fontSize;
            Settings.Instance.displayFont.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            Settings.Instance.displayFont.normal.textColor = Color.yellow;
            Settings.Instance.displayFont.border = new RectOffset();
            Settings.Instance.displayFont.padding = new RectOffset();

            //Settings.Instance.displayFont.normal.background,

#if false
            Texture2D sortBackground = new Texture2D(1, 1);
            sortBackground.SetPixel(1, 1, XKCDColors.OffWhite);
            sortBackground.Apply();

            Settings.Instance.displayFont.onFocused.background =
            Settings.Instance.displayFont.onNormal.background =
            Settings.Instance.displayFont.onHover.background =
            Settings.Instance.displayFont.active.background =
            Settings.Instance.displayFont.focused.background =
            Settings.Instance.displayFont.hover.background =
            Settings.Instance.displayFont.normal.background = sortBackground; // SetImageAlpha(Settings.Instance.displayFont.normal.background, 1);
#endif
            //Settings.Instance.scrollViewStyle = Settings.Instance.displayFont;
            //Settings.Instance.displayFont.padding = new RectOffset();

            Settings.Instance.textAreaFont.fontSize = (int)fontSize;
            Settings.Instance.textAreaFont.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            Settings.Instance.textAreaFont.normal.textColor = Color.white;

            Settings.Instance.textAreaSmallFont.fontSize = (int)fontSize - 2;
            Settings.Instance.textAreaSmallFont.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            Settings.Instance.textAreaSmallFont.richText = true;
            Settings.Instance.textAreaSmallFont.normal.textColor = Color.white;
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
        internal static void SetAlpha(float Alpha)
        {
            GUIStyle workingWindow;
            if (Alpha == lastAlpha)
                return;
            lastAlpha = Alpha;
            if (Settings.Instance.kspWindow.active.background == null)
            {
                Log.Info("SetAlpha, Settings.Instance.kspWindow.active.background is null");
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

        static void SetAlphaFor(int id, float Alpha, GUIStyle style, Texture2D backgroundTexture, Color color)
        {
            Log.Info("SetAlphafor, id: " + id);
            if (backgroundTexture == null)
            {
                Log.Info("SetAlphaFor, Null backgroundTexture, id: " + id);
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


        void SelectContractWindowDisplay(int id)
        {
            Guid? keyToRemove = null;
            GUIStyle toggleStyle = new GUIStyle(GUI.skin.toggle);
            toggleStyle.alignment = TextAnchor.UpperLeft;
            using (new GUILayout.VerticalScope())
            {
                GUILayout.Label("Click on contract to enable/disable it", Settings.Instance.largeDisplayFont);
                GUILayout.Space(15);
                scrollPos = GUILayout.BeginScrollView(scrollPos, Settings.Instance.scrollViewStyle);

                foreach (var a in Settings.Instance.activeContracts)
                {
                    using (new GUILayout.HorizontalScope())
                    {
                        //a.Value.selected = GUILayout.Toggle(a.Value.selected, "", toggleStyle);
                        //GUILayout.Label(a.Value.contractContainer.Title, Settings.Instance.largeDisplayFont);
                        if (GUILayout.Button((a.Value.selected ? htmlGreen : htmlRed) + a.Value.contractContainer.Title + "</color>", Settings.Instance.largeDisplayFont))
                            a.Value.selected = !a.Value.selected;
                        GUILayout.FlexibleSpace();
                    }
                }
                GUILayout.EndScrollView();
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Select All", GUILayout.Width(90)))
                    {
                        foreach (var a in Settings.Instance.activeContracts)
                            a.Value.selected = true;
                    }
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Clear All", GUILayout.Width(90)))
                    {
                        foreach (var a in Settings.Instance.activeContracts)
                            a.Value.selected = false;
                    }
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Close", GUILayout.Width(90)))
                    {
                        selectVisible = false;
                        Settings.Instance.ResetWinPos();
                    }
                    GUILayout.FlexibleSpace();
                }
            }
            if (keyToRemove != null)
                Settings.Instance.activeContracts.Remove((Guid)keyToRemove);
            GUI.DragWindow();
        }
    }
}
#define SCANSAT


using System;
using System.Collections.Generic;
using UnityEngine;
using KSP.UI.Screens;
using ClickThroughFix;
using ToolbarControl_NS;
using SpaceTuxUtility;
using ContractParser;
using System.Linq;

using static WhatDoINeed.RegisterToolbar;
using Contracts.Predicates;
using System.Security.Cryptography;
using static KSP.UI.UIRectScaler;
using System.Runtime.InteropServices;


namespace WhatDoINeed
{
    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public class WhatDoINeed : MonoBehaviour
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
        string contractText = "Contracts";

        bool resizingWindow = false;

        Vector2 scrollPos;

        bool kisAvailable = false;
        bool scansatAvailable = false;

        //const string htmlRed = "<color=#ff0000>";
        const string htmlRed = "<color=#fff12a>";  // Light yellow (copied from colors found in Mission Control)
        //const string htmlGreen = "<color=#00ff00>";
        const string htmlGreen = "<color=#8cf893>"; // Light green (copied from colors found in Mission Control)
        const string htmlPaleblue = "<color=#acfcff>";
        int btnId;



        public void Start()
        {
            if (!HighLogic.LoadedSceneIsEditor || !(HighLogic.CurrentGame.Mode == Game.Modes.CAREER || HighLogic.CurrentGame.Mode == Game.Modes.SCIENCE_SANDBOX))
            {
                Log.Info("Sandbox mode, exiting");
                return;
            }
            //kisAvailable = HasMod.hasMod("KIS");
#if SCANSAT
            scansatAvailable = HasMod.hasMod("SCANsat");
#endif
            kisAvailable = KISWrapper.Initialize();

            lastAlpha = -1;
            Settings.Instance.LoadData();

            ScanContracts();
            SetUpExperimentParts();
#if SCANSAT
            if (scansatAvailable)
                SetupSCANsatStrings();
#endif
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
                Settings.Instance.SaveData();
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
                                           Log.Info("OnLaunchButtonInput 1");
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
                    Log.Info("OnLaunchButtonInput 4");
                    ButtonManager.BtnManager.InvokeNextDelegate(btnId, "What-Do-I-Need-next");
#endif
                }
            }
            else
            {
                Log.Info("OnLaunchButtonInput 3");
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
                Log.Info("Update, Input.GetMouseButtonUp(0)");
                ScanShip();
            }
        }
        void onEditorPartEvent(ConstructionEventType constrE, Part part)
        {
            ScanShip();
        }

        void ScanShip()
        {
            Log.Info("ScanShip");
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
                    for (int j = 0; j < ep.Value.parts.Count - 1; j++)
                    {
                        var ap = ep.Value.parts[j];
                        if (p.name == ap.part.name)
                        {
                            ep.Value.numExpAvail++;
                            ap.numAvailable++;
                            //Log.Info("ScanShip, found part: " + p.name);
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
#if DEBUG
            DumpAllScannedData();
#endif
        }

        void DumpAllScannedData()
        {
            Log.Info("======================================================================");
            Log.Info("======================================================================");

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
                if (expPart.scanSatPart)
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
            Log.Info("ScanShipKISInventory");
            for (int i0 = 0; i0 < EditorLogic.fetch.ship.Parts.Count; i0++)
            {
                Part p = EditorLogic.fetch.ship.Parts[i0];
                var availableItems = KISWrapper.GetInventories(p).SelectMany(i => i.items); //.ToArray();
                Log.Info("part: " + p.name + ", # inv items: " + availableItems.Count());
                foreach (KeyValuePair<int, KISWrapper.KIS_Item> i in availableItems)
                {
                    var kisInvPart = i.Value.partNode.GetValue("name");
                    Log.Info("ScanShipKISInventory kisInvPart: " + kisInvPart);

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
        List<Guid> activeLocalContracts = new List<Guid>();
        /// <summary>
        /// Create list of all experiments, and which parts support each experiment
        /// </summary>
        void SetUpExperimentParts()
        {
            Log.Info("SetUpExperimentParts");
            // First get list of all experiments in the active contracts

            activeLocalContracts.Clear();
            ConfigNode configNode = new ConfigNode();
            HighLogic.CurrentGame.Save(configNode);
            ConfigNode gameNode = configNode.GetNode("GAME");
            ConfigNode[] scenarios = gameNode.GetNodes("SCENARIO");

            Log.Info("=====================================");
            Log.Info("Scanning contracts in Scenario ContractSystem node");
            Log.Info("=====================================");

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
                            var contract = contracts[j];
                            string state = contract.GetValue("state");
                            Guid contractGuid = new Guid(contract.GetValue("guid"));
                            ConfigNode[] param_s = contract.GetNodes("PARAM");
                            string dataName = contract.GetValue("dataName");
                            if (state == "Active")
                            {
                                activeLocalContracts.Add(contractGuid);
                                for (int k = 0; k < param_s.Length; k++)
                                {
                                    var param = param_s[k];
                                    string param_name = param.GetValue("name");
                                    string param_state = param.GetValue("state");
                                    string param_part;
                                    string experiment = null;
                                    string experimentDetail = null;
                                    string scansatExpID = null;
                                    short scansatExpIDShort = -1;

                                    if (param_state == "Incomplete")
                                    {
                                        param.TryGetValue("experiment", ref experiment);
                                        if (experiment == null)
                                        {
                                            switch (param_name)
                                            {
#if SCANSAT

                                                case "SCANsatCoverage": // For SCANsat
                                                    {
                                                        //param.TryGetValue("scanName", ref scansatExpID);
                                                        param.TryGetValue("scanType", ref scansatExpID);
                                                        if (short.TryParse(scansatExpID, out scansatExpIDShort))
                                                        {
                                                            Log.Error("SCANsatCoverage scanType: " + scansatExpID + " not a number");
                                                        }

                                                        experiment = "SCANsat"; // + experiment;
                                                        experimentDetail = experiment;
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
                                                        break;
                                                    }

                                                // Not sure if following will show up as s unique contract in the contract system, need to test
                                                case "USAdvancedScience":  //Universal Storage    (probably not Station Science experiments)
                                                    {
                                                        Log.Info("USAdvancedScience experiment found");
                                                        param.TryGetValue("experimentID", ref experiment);  // More Station Science experiments
                                                        experimentDetail = experiment;
                                                        break;
                                                    }

                                                case "ConstructionParameter":
                                                case "RepairPartParameter":
                                                case "PartTest":
                                                    {
                                                        if (param_name == "RepairPartParameter" || param_name == "ConstructionParameter")
                                                            param_part = param.GetValue("partName");
                                                        else
                                                            param_part = param.GetValue("part");

                                                        CEP_Key_Tuple ckt = new CEP_Key_Tuple(param_name, contractGuid, param_part);
                                                        if (!experimentParts.ContainsKey(ckt.Key()))
                                                        {
#if false
                                                            Log.Info("Contract guid: " + contractGuid + ", experiment: " + param_name + ", part: " + param_part + ", key: " + ckt.Key());
#endif
                                                            experimentParts.Add(ckt.Key(), new ContractExperimentPart(ckt));

                                                            for (int l = 0; l < PartLoader.LoadedPartsList.Count; l++)
                                                            {
                                                                AvailablePart p = PartLoader.LoadedPartsList[l];
                                                                if (p.name == param_part)
                                                                {
                                                                    experimentParts[ckt.Key()].parts.Add(new AvailPartWrapper(p));
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

                                                case "DMLongOrbitParameter": // This is for the DMagic science stuff
                                                    {
#if false
                                                        Log.Info("PARAM found: DMLongOrbitParameter");
#endif
                                                        var partRequestParams = param.GetNodes("PARAM");
                                                        for (int l = 0; l < partRequestParams.Length; l++)
                                                        {
                                                            var prp = partRequestParams[l];
                                                            string requestedParts = prp.GetValue("Requested_Parts");
                                                            if (requestedParts != null)
                                                            {
                                                                List<string> requestedPartsList = requestedParts.Split(',').ToList<string>();
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

                                                                                //if (!Settings.Instance.activeContracts.ContainsKey(contractGuid))
                                                                                if (experiment == null)
                                                                                {
                                                                                    Log.Info("DMPartRequestParameter, Contract guid: " + contractGuid + ", experiment: " + part + ", part: " + part + ", key: " + ckt.Key());
                                                                                    Log.Error("Error 2, experiment is null");
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
                                                        Log.Info("PARAM found: PartValidation");
                                                        var partValidationFilterParams = param.GetNodes("FILTER");
                                                        for (int m = 0; m < partValidationFilterParams.Length; m++)
                                                        {
                                                            var prp = partValidationFilterParams[m];
                                                            string requestedPartModule = prp.GetValue("partModule");
                                                            if (requestedPartModule != null)
                                                            {
                                                                CEP_Key_Tuple ckt = new CEP_Key_Tuple(param_name, contractGuid, requestedPartModule);
                                                                if (!experimentParts.ContainsKey(ckt.Key()))
                                                                {
#if true
                                                                    Log.Info("Contract guid: " + contractGuid + ", experiment: " + "PartValidation" + ", module: " + requestedPartModule + ", key: " + ckt.Key());
#endif
                                                                    for (int n = 0; n < PartLoader.LoadedPartsList.Count; n++)
                                                                    {
                                                                        AvailablePart p = PartLoader.LoadedPartsList[n];
                                                                        if (p.category != PartCategories.none)
                                                                        {
                                                                            var mNodesList = p.partConfig.GetNodes("MODULE");
                                                                            for (int o = 0; o < mNodesList.Length; o++)
                                                                            {
                                                                                var mNode = mNodesList[o];
                                                                                string moduleName = mNode.GetValue("name");

                                                                                if (moduleName == requestedPartModule)
                                                                                {
                                                                                    if (!experimentParts.ContainsKey(ckt.Key()))
                                                                                        experimentParts.Add(ckt.Key(), new ContractExperimentPart(ckt));
                                                                                    experimentParts[ckt.Key()].parts.Add(new AvailPartWrapper(p));
                                                                                    //if (!Settings.Instance.activeContracts.ContainsKey(contractGuid))
                                                                                    if (experiment == null)
                                                                                        Log.Error("Error 3, experiment is null");
                                                                                    experiment = "PartValidation";
                                                                                    if (!Settings.Instance.activeContracts[contractGuid].experiments.ContainsKey(experiment))
                                                                                        Settings.Instance.activeContracts[contractGuid].experiments.Add(experiment, experiment);
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
                                                                }
                                                            }
                                                        }
                                                        break;
                                                    }
                                            }
                                        }
                                        if (experiment != null)
                                        {
                                            CEP_Key_Tuple ckt = new CEP_Key_Tuple(experiment, contractGuid);
                                            if (!experimentParts.ContainsKey(ckt.Key()))
                                            {
                                                if (scansatAvailable && scansatExpID != null)
                                                {
                                                    ckt = new CEP_Key_Tuple(experiment, contractGuid, ((SCANsatSCANtype)scansatExpIDShort).ToString()); //  scansatExpID);
#if DEBUG
                                                    Log.Info("Contract guid: " + contractGuid + ", experiment: " + experiment + ", key: " + ckt.Key() + ", scansatExpID: " + scansatExpID);
#endif
                                                    experimentParts.Add(ckt.Key(), new ContractExperimentPart(ckt, scansatExpID));
                                                }
                                                else
                                                {
#if DEBUG
                                                    Log.Info("Contract guid: " + contractGuid + ", experiment: " + experiment + ", key: " + ckt.Key());
#endif
                                                    experimentParts.Add(ckt.Key(), new ContractExperimentPart(ckt));
                                                }
                                                Log.Info("Adding experiment to Settings.Instance.activecontracts, guid: " + contractGuid);

                                                if (experiment == null)
                                                    Log.Error("Error 4, experiment is null");
                                                if (scansatExpID != null)
                                                {
                                                    Log.Info("Adding experiment to activeContracts, experiment: " + experiment + ", scansatExpID: " + scansatExpID);
                                                    Settings.Instance.activeContracts[contractGuid].experiments.Add(experiment, scansatExpID);
                                                }
                                                else
                                                {
                                                    Log.Info("Adding experiment to activeContracts, experiment: " + experiment);
                                                    if (!Settings.Instance.activeContracts[contractGuid].experiments.ContainsKey(experiment))
                                                        Settings.Instance.activeContracts[contractGuid].experiments.Add(experiment, experiment);
                                                }
                                            }

                                        }
                                    }
                                }
                            }
                        }
                    }



                }
            }

            Log.Info("=====================================");
            Log.Info("Scanning Parts, looking for experiments");
            Log.Info("=====================================");


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
                                bool show = false;
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
                                    show = true;
                                    experimentID = DoSCANSatResourceScanner(experiment, p);

                                }
                                if (name == "DMModuleScienceAnimate")
                                {
                                    experimentID = experiment.GetValue("experimentID");

                                }
                                if (scansatAvailable && name == "SCANsat")
                                {
                                    show = true;

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
                                            if (show)
                                                Log.Info("part: " + p.name + ", title: " + p.title + ", experiment module name: " + name + ", expid: " + experimentID + ", key: " + cet);
#endif
                                            if (experimentParts.ContainsKey(cet))
                                            {
#if DEBUG
                                                if (show)
                                                    Log.Info("part: " + p.name + ", title: " + p.title + ", experiment module name: " + name + ", expid: " + experimentID + ", contract: " + activeContract);
#endif
                                                var experimentPart = experimentParts[cet];
                                                experimentPart.parts.Add(new AvailPartWrapper(p));
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
            Log.Info("=====================================");
            Log.Info("Scanning ship, looking for parts with experiments");
            Log.Info("=====================================");

            ScanShip();
        }

        void ScanContracts()
        {
            Log.Info("ScanContracts");
            var aContracts = contractParser.getActiveContracts;
            for (int i = 0; i < aContracts.Count; i++)
            {
                var contract = aContracts[i];
                if (!Settings.Instance.activeContracts.ContainsKey(contract.ID))
                {
                    Settings.Instance.activeContracts.Add(contract.ID, new Contract(contract));
                    Log.Info("Contract: " + contract.ID);
                }
            }
            Log.Info("Active Contracts: " + Settings.Instance.activeContracts.Count);
            int cnt = 0;
            foreach (var contract in Settings.Instance.activeContracts)
            {
                Log.Info("ScanContracts, Settings.Instance.activeContract: " + contract.Key);
                if (contract.Value.selected)
                    cnt++;
            }

            if (Settings.Instance.initialShowAll && cnt == 0) //Settings.Instance.activeContracts.Count == 0)
            {
                foreach (var contract in Settings.Instance.activeContracts)
                    contract.Value.selected = true;
            }


        }

        string DoSCANSatResourceScanner(ConfigNode experiment, AvailablePart p)
        {
            Log.Info("DoSCANSatResourceScanner 1, part: " + p.name + ", Settings.Instance.activeContracts.Count: " + Settings.Instance.activeContracts.Count);
            string experimentID = "Nothing";

            string experimentType = experiment.GetValue("sensorType"); // SCANsat
            short s;
            if (short.TryParse(experimentType, out s))
            {
                SCANsatSCANtype scantype = (SCANsatSCANtype)s;

                experimentID = scantype.ToString();
                Log.Info("DoSCANSatResourceScanner 2, part: " + p.name + ", ScannerType: " + s);

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
                                            Log.Info("Adding to experimentParts, key: " + cet.Key() + ", contract: " + activeContract);
                                            experimentParts.Add(cet.Key(), new ContractExperimentPart(cet));
                                        }
                                        {
#if DEBUG
                                            //Log.Info("DoSCANSatResourceScanner, part: " + p.name + ", title: " + p.title + ", experiment module name: " + name + ", expid: " + experimentID + ", contract: " + activeContract);
#endif
                                            var experimentPart = experimentParts[cet.Key()];
                                            experimentPart.parts.Add(new AvailPartWrapper(p));
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            experimentID = "Nothing";
            if (experimentID != "Nothing")
                Log.Info("part: " + p.name + ", ModuleResourceScanner,  experimentType: " + experimentID);
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
            Log.Info("===============");
            Log.Info("DoSCANSatModule 1, part: " + availPart.name);

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
                                Log.Info("DoSCANSatModule 2, expStr: " + expStr + ", expsensorType: " + (int)expsensorType + ", scantype: " + (int)scantype + ", sensorTypeStr: " + sensorTypeStr + ", a: " + (int)availableExperiment + ", contract: " + activeContract);
#if false
                            Log.Info("part: " + p.name +", title: " + p.title +  ", experiment module name: " + name + ", expid: " + experimentID + ", ep.scanType: " + (int)ep.scanType);
                            Log.Info("Bitwise comparision: " + (int)a);
#endif
                                //CEP_Key_Tuple cet = new CEP_Key_Tuple("SCANsat+" + availableExperiment.ToString(), activeContract);
                                CEP_Key_Tuple cet = new CEP_Key_Tuple("SCANsat", activeContract, ((SCANsatSCANtype)availableExperiment).ToString());
                                if (!experimentParts.ContainsKey(cet.Key()))
                                {
                                    Log.Info("DoSCANSatModule 3, adding: " + cet.Key());
                                    experimentParts.Add(cet.Key(), new ContractExperimentPart(cet));
                                }

#if DEBUG
                                Log.Info("DoSCANSatModule 4, part: " + availPart.name + ", title: " + availPart.title + ", experiment module name: SCANsat" + ", Key: " + cet.Key());
#endif
                                experimentParts[cet.Key()].parts.Add(new AvailPartWrapper(availPart, (SCANsatSCANtype)availableExperiment));
                                //break;
                                Log.Info("DoSCANSatModule 5");
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
            Log.Info("DoSCANSatModule, experimentID: " + experimentID);
            Log.Info("===============");
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

            Settings.Instance.SaveData();
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
                    if (!selectVisible)
                    {
                        if (Settings.Instance.enableClickThrough && !settingsVisible)
                            tmpPos = GUILayout.Window(winId, Settings.Instance.winPos, ContractWindowDisplay, "What Do I Need? - Active " + contractText, Settings.Instance.kspWindow);
                        else
                            tmpPos = ClickThruBlocker.GUILayoutWindow(winId, Settings.Instance.winPos, ContractWindowDisplay, "What Do I Need? - Active " + contractText + " & Settings", Settings.Instance.kspWindow);
                        if (!Settings.Instance.lockPos)
                            Settings.Instance.winPos = tmpPos;
                    }
                    else
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
            Log.Info("Added HelpWindowClass");
        }

        Vector2 contractPos;
        Dictionary<string, bool> openClosed = new Dictionary<string, bool>();

        void ContractWindowDisplay(int id)
        {
            numDisplayedContracts = 0;

            if (Settings.Instance.activeContracts != null && !settingsVisible)
            {
                contractPos = GUILayout.BeginScrollView(contractPos, Settings.Instance.scrollViewStyle, GUILayout.MaxHeight(Screen.height - 20));
                foreach (var contract in Settings.Instance.activeContracts)
                {
                    if (contract.Value.selected)
                    {
                        numDisplayedContracts++;

                        string contractId = contract.Key.ToString();
                        bool requirementsOpen = false;
                        if (!openClosed.TryGetValue(contractId, out requirementsOpen))
                        {
                            openClosed.Add(contractId, requirementsOpen);
                            requirementsOpen = openClosed[contractId] = true;
                        }
                        GUILayout.Space(10);
                        using (new GUILayout.HorizontalScope())
                        {
                            if (GUILayout.Button(requirementsOpen ? "-" : "+", GUILayout.Width(20)))
                            {
                                requirementsOpen = !requirementsOpen;
                                openClosed[contractId] = requirementsOpen;
                            }
                            GUILayout.Label(contract.Value.contractContainer.Title, Settings.Instance.largeDisplayFont);
                            //GUILayout.TextField("Guid: " + contract.Value.contractContainer.ID, Settings.Instance.displayFont);
                        }

                        if (Settings.Instance.showBriefing && openClosed[contractId])
                        {
                            using (new GUILayout.HorizontalScope())
                            {
                                GUILayout.Space(30);
                                GUILayout.TextArea(contract.Value.contractContainer.Briefing, Settings.Instance.textAreaFont);
                            }
                        }
                        if (requirementsOpen)
                        {
                            bool partsFound = false;
                            foreach (var expPartAll in experimentParts)
                            {
                                var expPart = expPartAll.Value;
                                if (expPart.contractGuid == contract.Value.contractContainer.ID)
                                {
                                    using (new GUILayout.HorizontalScope())
                                    {
                                        GUILayout.Space(30);
                                        if (expPart.scanSatPart)
                                            GUILayout.Label(htmlPaleblue + "Experiment:  " + expPart.scanType.ToString() + "</color>", Settings.Instance.displayFont);
                                        else
                                            GUILayout.Label(htmlPaleblue + "Experiment:  " + expPart.experimentTitle + "</color>", Settings.Instance.displayFont);
                                    }
#if true
                                    if (expPart.parts.Count == 0)
                                    {
                                        using (new GUILayout.HorizontalScope())
                                        {
                                            GUILayout.Space(30);
                                            GUILayout.Label(htmlPaleblue + "No Parts Needed" + "</color>", Settings.Instance.displayFont);

                                        }
                                    }
                                    else
#endif
                                    {
#if false
                                        using (new GUILayout.HorizontalScope())
                                        {
                                            GUILayout.Space(30);
                                            GUILayout.Label(htmlPaleblue + "expPart count:" + expPart.parts.Count + "</color>", Settings.Instance.displayFont);
                                        }
#endif
                                        partsFound = true;
                                        using (new GUILayout.HorizontalScope())
                                        {
                                            GUILayout.Space(30);
                                            GUILayout.Label(htmlPaleblue + "Fulfilling Parts:" + "</color>", Settings.Instance.displayFont);
                                        }
                                        for (int i = 0; i < expPart.parts.Count; i++)
                                        {
                                            AvailPartWrapper part = expPart.parts[i];

                                            if (part.part.category != PartCategories.none && part.partTitle != "")
                                            {
                                                using (new GUILayout.HorizontalScope())
                                                {
                                                    GUILayout.Space(40);
                                                    if (part.numAvailable == 0)
                                                        GUILayout.Label(htmlRed + part.partTitle + "</color>", Settings.Instance.displayFont);
                                                    else
                                                        GUILayout.Label(htmlGreen + part.partTitle + " (" + part.numAvailable + ")"
                                                            + "</color>", Settings.Instance.displayFont);
                                                }
                                            }
                                        }
                                    }
                                }

                            }


                            if (!partsFound)
                            {
                                using (new GUILayout.HorizontalScope())
                                {
                                    GUILayout.Space(30);
                                    GUILayout.Label(htmlPaleblue + "No Parts Needed" + "</color>", Settings.Instance.displayFont);
                                }
                            }
                        }
                    }
                }
                GUILayout.EndScrollView();
            }
            contractText = (numDisplayedContracts != 1) ? "Contracts" : "Contract";

            if (settingsVisible)
            {
                bool oBold = Settings.Instance.bold;
                var oAlpha = Settings.Instance.Alpha;
                GUILayout.Space(30);
                using (new GUILayout.HorizontalScope())
                {
                    // This stupidity is due to a bug in the KSP skin
                    Settings.Instance.showBriefing = GUILayout.Toggle(Settings.Instance.showBriefing, "");
                    GUILayout.Label("Display Briefing");
                    GUILayout.FlexibleSpace();
                    Settings.Instance.bold = GUILayout.Toggle(Settings.Instance.bold, "");
                    GUILayout.Label("Bold");
                    GUILayout.FlexibleSpace();
                    Settings.Instance.lockPos = GUILayout.Toggle(Settings.Instance.lockPos, "");
                    GUILayout.Label("Lock Position");
                    GUILayout.FlexibleSpace();
                    Settings.Instance.hideButtons = GUILayout.Toggle(Settings.Instance.hideButtons, "");
                    GUILayout.Label("Hide Buttons");
                }
                using (new GUILayout.HorizontalScope())
                {
                    Settings.Instance.enableClickThrough = GUILayout.Toggle(Settings.Instance.enableClickThrough, "");
                    GUILayout.Label("Allow click-through");
                    GUILayout.FlexibleSpace();
                    Settings.Instance.initialShowAll = GUILayout.Toggle(Settings.Instance.initialShowAll, "");
                    GUILayout.Label("Show all active contracts upon entry");
                    GUILayout.FlexibleSpace();
                }
#if true
                using (new GUILayout.HorizontalScope())
                {
                    Settings.Instance.reopenIfLastOpen = GUILayout.Toggle(Settings.Instance.reopenIfLastOpen, "");
                    GUILayout.Label("Reopen when entering editor");
                    GUILayout.FlexibleSpace();
                }
#endif
                using (new GUILayout.HorizontalScope())
                {
                    Settings.Instance.checkForMissingBeforeLaunch = GUILayout.Toggle(Settings.Instance.checkForMissingBeforeLaunch, "");
                    GUILayout.Label("Check For Missing Experiments Before Launch");
                    GUILayout.FlexibleSpace();

                    Settings.Instance.onlyCheckSelectedContracts = GUILayout.Toggle(Settings.Instance.onlyCheckSelectedContracts, "");
                    GUILayout.Label("Only Check Selected Contracts");
                    GUILayout.FlexibleSpace();
                }

                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("Transparency:", GUILayout.Width(130));
                    Settings.Instance.Alpha = GUILayout.HorizontalSlider(Settings.Instance.Alpha, 0f, 255f, GUILayout.Width(130));
                    GUILayout.FlexibleSpace();
                    GUILayout.Label("Hide Time (" + Settings.Instance.HideTime.ToString("F0") + "s):");
                    Settings.Instance.HideTime = GUILayout.HorizontalSlider(Settings.Instance.HideTime, 1f, 30, GUILayout.Width(130));
                    GUILayout.FlexibleSpace();
                }
                if (oAlpha != Settings.Instance.Alpha)
                {
                    SetAlpha(Settings.Instance.Alpha);
                }
                var oFontSize = Settings.Instance.fontSize;
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("Font Size:", GUILayout.Width(130));
                    Settings.Instance.fontSize = GUILayout.HorizontalSlider(Settings.Instance.fontSize, 8f, 30f, GUILayout.Width(130));
                    GUILayout.FlexibleSpace();
                }
                if (oFontSize != Settings.Instance.fontSize || oBold != Settings.Instance.bold)
                    SetFontSizes(Settings.Instance.fontSize, Settings.Instance.bold);




            }

            if (!Settings.Instance.hideButtons || settingsVisible || numDisplayedContracts == 0)
            {
                GUILayout.FlexibleSpace();
                using (new GUILayout.HorizontalScope())
                {
                    if (!settingsVisible)
                    {
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("Contract Selection", GUILayout.Width(150)))
                        {
                            selectVisible = true;

                            if (Settings.Instance.activeContracts == null)
                            {
                                Log.Error("activeContracts is null");
                                Settings.Instance.activeContracts = new Dictionary<Guid, Contract>();
                            }
                        }
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("Close", GUILayout.Width(90)))
                        {
                            GUIToggle();
                        }
                    }
                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button(settingsVisible ? "Close Settings" : "Settings", GUILayout.Width(110)))
                    {
                        settingsVisible = !settingsVisible;
                        if (!settingsVisible)
                        {
                            Settings.Instance.SaveData();
                            //Settings.Instance.failToWrite = false;
                        }
                    }
                    GUILayout.FlexibleSpace();
                    //GUILayout.Space(30);
                }
            }
            if (GUI.Button(new Rect(4, 2, 24, 24), "B"))
            {
                Settings.Instance.hideButtons = !Settings.Instance.hideButtons;
            }
            if (GUI.Button(new Rect(26, 2, 24, 24), "L"))
            {
                Settings.Instance.lockPos = !Settings.Instance.lockPos;
            }
            if (GUI.Button(new Rect(52, 2, 24, 24), "S"))
            {
                settingsVisible = !settingsVisible;
            }

            if (GUI.Button(new Rect(Settings.Instance.winPos.width - 24 - 2, 2, 24, 24), "X"))
            {
                GUIToggle();
            }
            if (GUI.Button(new Rect(Settings.Instance.winPos.width - 48 - 4, 2, 24, 24), "H"))
            {
                quickHideEnd = Time.realtimeSinceStartup + Settings.Instance.HideTime;
            }
            if (GUI.Button(new Rect(Settings.Instance.winPos.width - 72 - 6, 2, 24, 24), "?"))
            {
                ShowHelpWindow();
            }

            if (!Settings.Instance.lockPos)
            {
                if (GUI.RepeatButton(new Rect(Settings.Instance.winPos.width - 23f, Settings.Instance.winPos.height - 24f, 24, 24), "", Settings.Instance.resizeButton))
                {
                    resizingWindow = true;
                }
            }
            resizeWindow();
            GUI.DragWindow();
        }

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
                GUILayout.Label("Click on contract to enable", Settings.Instance.largeDisplayFont);
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
                    if (GUILayout.Button("Accept", GUILayout.Width(90)))
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
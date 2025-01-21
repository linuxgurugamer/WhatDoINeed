using System;
using System.Collections.Generic;
using UnityEngine;
using KSP.UI.Screens;
using ClickThroughFix;
using ToolbarControl_NS;
using SpaceTuxUtility;
using ContractParser;


using static WhatDoINeed.RegisterToolbar;

// Transparency for unity skin ???

namespace WhatDoINeed
{
    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public class Display : MonoBehaviour
    {
        private static ToolbarControl toolbarControl;
        internal static ToolbarControl Toolbar { get { return toolbarControl; } }

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

        public void Start()
        {
            if (!HighLogic.LoadedSceneIsEditor || !(HighLogic.CurrentGame.Mode == Game.Modes.CAREER || HighLogic.CurrentGame.Mode == Game.Modes.SCIENCE_SANDBOX))
            {
                Log.Info("Sandbox mode, exiting");
                return;
            }
            lastAlpha = -1;
            SetUpExperimentParts();
            GameEvents.onHideUI.Add(OnHideUI);
            GameEvents.onShowUI.Add(OnShowUI);

            GameEvents.onEditorPodSelected.Add(onEditorPodSelected);
            GameEvents.onEditorPodPicked.Add(onEditorPodSelected);
            GameEvents.onEditorPodDeleted.Add(onEditorPodDeleted);


            GameEvents.onEditorPartEvent.Add(onEditorPartEvent);


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
            //InvokeRepeating("SlowUpdate", 5, 5);
            SetWinPos();
            if (!Settings.Instance.helpWindowShown)
            {
                ShowHelpWindow();
                Settings.Instance.helpWindowShown = true;
                Settings.Instance.SaveData();
            }
        }

        void OnDestroy()
        {
            GameEvents.onHideUI.Remove(OnHideUI);
            GameEvents.onShowUI.Remove(OnShowUI);
            GameEvents.onEditorPartEvent.Remove(onEditorPartEvent);
            GameEvents.onEditorPodSelected.Remove(onEditorPodSelected);
            GameEvents.onEditorPodPicked.Remove(onEditorPodSelected);


        }

        void onEditorPodSelected(Part p)
        {
            ScanShip();
        }
        void onEditorPodDeleted()
        {
            ScanShip();
        }
        void onEditorPartEvent(ConstructionEventType constrE, Part part)
        {
            ScanShip();
        }

        void ScanShip()
        {
            //Log.Info("ScanShip");
            // First set all experiments to false, then scan
            // the vessel's parts
            foreach (var ep in experimentParts)
            {
                ep.Value.numExpAvail = 0;
                foreach (var p in ep.Value.parts)
                    p.numAvailable = 0;
            }
            foreach (Part p in EditorLogic.fetch.ship.Parts)
            {
                foreach (var ep in experimentParts)
                {
                    foreach (var ap in ep.Value.parts)
                    {
                        if (p.name == ap.part.name)
                        {
                            ep.Value.numExpAvail++;
                            ap.numAvailable++;
                            Log.Info("ScanShip, found part: " + p.name);
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
            if (scenarios != null && scenarios.Length > 0)
                foreach (var scenario in scenarios)
                {
                    string name = scenario.GetValue("name");
                    if (name == "ContractSystem")
                    {
                        ConfigNode[] contracts = scenario.GetNode("CONTRACTS").GetNodes("CONTRACT");
                        //GUILayout.Label("contracts.Count: " + contracts.Length, CapComSkins.headerText, GUILayout.Width(100));
                        foreach (var contract in contracts)
                        {
                            string state = contract.GetValue("state");
                            Guid contractGuid = new Guid(contract.GetValue("guid"));
                            ConfigNode[] param_s = contract.GetNodes("PARAM");
                            string dataName = contract.GetValue("dataName");
                            if (state == "Active")
                            {
                                activeLocalContracts.Add(contractGuid);
                                foreach (var param in param_s)
                                {
                                    string param_name = param.GetValue("name");
                                    string param_state = param.GetValue("state");
                                    string param_part;
                                    string experiment = null;
                                    if (param_state == "Incomplete")
                                    {
                                        param.TryGetValue("experiment", ref experiment);
                                        if (experiment == null)
                                        {
                                            // Handle the OrbitalScience here
                                            if (param_name == "StnSciParameter")
                                            {
                                                param.TryGetValue("experimentType", ref experiment);  // Now check for Station Science experiments
                                                if (experiment != null)
                                                {
                                                    experiment = experiment.Remove(0, 16);
                                                    string a = experiment[0].ToString().ToLower();
                                                    experiment = a + experiment.Remove(0, 1);

                                                }
                                            }
                                            if (param_name == "SCANsatCoverage")
                                            {
                                                param.TryGetValue("scanName", ref experiment);  // Now check for Station Science experiments
                                            }
                                            if (param_name == "USAdvancedScience")
                                            {
                                                param.TryGetValue("experimentID", ref experiment);  // Now check for Station Science experiments
                                            }
                                            if (param_name == "RepairPartParameter"  || param_name == "PartTest")
                                            {
                                                if (param_name == "RepairPartParameter")
                                                param_part = param.GetValue("partName");
                                                else
                                                    param_part = param.GetValue("part");

                                                CEP_Key_Tuple ckt = new CEP_Key_Tuple(param_name, contractGuid, param_part);
                                                if (!experimentParts.ContainsKey(ckt.Key()))
                                                {
#if DEBUG
                                                    Log.Info("Contract guid: " + contractGuid + ", experiment: " + param_name + ", part: " + param_part + ", key: " + ckt.Key());
#endif
                                                    experimentParts.Add(ckt.Key(), new ContractExperimentPart(ckt));

                                                    foreach (AvailablePart p in PartLoader.LoadedPartsList)
                                                    {
                                                        if (p.name == param_part)
                                                        {
                                                             experimentParts[ckt.Key()].parts.Add(new AvailPartWrapper(p)); 
                                                            break;
                                                        }
                                                    }
                                                  

                                                }

                                            }

                                        }
                                        if (experiment != null)
                                        {
                                            CEP_Key_Tuple ckt = new CEP_Key_Tuple(experiment, contractGuid);
                                            if (!experimentParts.ContainsKey(ckt.Key()))
                                            {
#if DEBUG
                                                Log.Info("Contract guid: " + contractGuid + ", experiment: " + experiment + ", key: " + ckt.Key());
#endif

                                                experimentParts.Add(ckt.Key(), new ContractExperimentPart(ckt));
                                             
                                            }
                                        }

                                    }
                                }
                            }
                        }
                    }
                }

            // Now go through all the parts, looking for experiments
            List<AvailablePart> loadedParts = new List<AvailablePart>();
            if (PartLoader.Instance != null)
                loadedParts.AddRange(PartLoader.LoadedPartsList);
            foreach (AvailablePart p in loadedParts)
            {
                if (p.partConfig != null) // && !p.name.StartsWith("kerbal"))
                {

                    if (p.category != PartCategories.none)
                    {
                        ConfigNode[] modules = p.partConfig.GetNodes("MODULE");
                        if (modules != null && modules.Length > 0)
                        {
                            foreach (var experiment in modules)
                            {
                                string name = experiment.GetValue("name");
                                string experimentID = experiment.GetValue("experimentID");
                                if (experimentID != null)
                                {
                                    foreach (var activeContract in activeLocalContracts)
                                    {
                                        var cet = new CEP_Key_Tuple(experimentID, activeContract).Key();
#if DEBUG
                                        Log.Info("part: " + p.name + ", experiment module name: " + name + ", expid: " + experimentID + ", key: " + cet);
#endif
                                        if (experimentParts.ContainsKey(cet))
                                        {
#if DEBUG
                                            Log.Info("part: " + p.name + ", experiment module name: " + name + ", expid: " + experimentID + ", contract: " + activeContract);
#endif
                                            var experimentPart = experimentParts[cet];
                                            experimentPart.parts.Add(new AvailPartWrapper(p));
                                        }
                                    }
                                }
                                else
                                {
                                }
                            }
                        }
                    }
                }
            }
#if DEBUG
            foreach (var epall in experimentParts)
            {
                string parts = "";
                foreach (var p in epall.Value.parts)
                    parts += p.part.name + ", ";
                Log.Info("Key: " + epall.Key + ",    Parts: " + parts);
            }
#endif

            ScanShip();
            var aContracts = contractParser.getActiveContracts;
            foreach (var a in aContracts)
            {
                if (!Settings.Instance.activeContracts.ContainsKey(a.ID))
                    Settings.Instance.activeContracts.Add(a.ID, new Contract(a));
            }
            if (Settings.Instance.initialShowAll)
            {
                foreach (var a in Settings.Instance.activeContracts)
                    a.Value.selected = true;
            }

        }

        public void SetWinPos()
        {
            Settings.Instance.SaveData();

            //if (Settings.Instance.editorWinPos.width == 0)
            //    Settings.Instance.editorWinPos = new Rect(Settings.Instance.winPos);
            //else
            //    Settings.Instance.winPos = new Rect(Settings.Instance.editorWinPos);
            Settings.Instance.winPos.width = Mathf.Clamp(Settings.Instance.winPos.width, Settings.WINDOW_WIDTH, Screen.width);
            Settings.Instance.winPos.width = Math.Min(Settings.Instance.winPos.width, Settings.WINDOW_WIDTH);
            Settings.Instance.winPos.x = Math.Min(Settings.Instance.winPos.x, Screen.width - Settings.Instance.winPos.width);
        }


        private void GUIToggle()
        {
            visible = !visible;
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
                        selWinPos = ClickThruBlocker.GUILayoutWindow(selWinId, selWinPos, SelectContractWindowDisplay, "What Do I Need? - Contract Selection");
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

#if false
        private List<string> getPartTitlesFromModules(List<string> names)
        {
            List<string> list = new List<string>();
            for (int i = 0; i < names.Count; i++)
            {
                string text = names[i];
                if (string.IsNullOrEmpty(text))
                {
                    continue;
                }

                for (int num = PartLoader.LoadedPartsList.Count - 1; num >= 0; num--)
                {
                    AvailablePart availablePart = PartLoader.LoadedPartsList[num];
                    if (availablePart != null && ResearchAndDevelopment.PartModelPurchased(availablePart) && !(availablePart.partPrefab == null))
                    {
                        try
                        {
                            int hashCode = text.GetHashCode();
                            for (int num2 = availablePart.partPrefab.Modules.Count - 1; num2 >= 0; num2--)
                            {
                                PartModule partModule = availablePart.partPrefab.Modules[num2];
                                if (!(partModule == null) && partModule.ModuleAttributes != null && partModule.ClassID == hashCode)
                                {
                                    list.Add(availablePart.title);
                                    break;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError("[Contract Parser] Custom Notes: Error Parsing Part: [" + availablePart.name + "] For Module: [" + text + "]\n" + ex.ToString());
                        }
                    }
                }
            }

            return list;
        }
#endif

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

            if (Settings.Instance.activeContracts != null)
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
                            foreach (var f in experimentParts.Values)
                            {
                                if (f.contractGuid == contract.Value.contractContainer.ID)
                                {
                                    using (new GUILayout.HorizontalScope())
                                    {
                                        GUILayout.Space(30);
                                        GUILayout.Label("<color=#acfcff>" + "Experiment:  " + f.experimentTitle + "</color>", Settings.Instance.displayFont);
                                    }
                                    using (new GUILayout.HorizontalScope())
                                    {
                                        GUILayout.Space(30);
                                        GUILayout.Label("<color=#acfcff>" + "Fulfilling Parts:" + "</color>", Settings.Instance.displayFont);
                                    }
                                    foreach (var part in f.parts)
                                    {

                                        using (new GUILayout.HorizontalScope())
                                        {
                                            GUILayout.Space(40);
                                            if (part.numAvailable == 0)
                                                GUILayout.Label("<color=#ff0000>" + part.partTitle + "</color>", Settings.Instance.displayFont);
                                            else
                                                GUILayout.Label("<color=#00ff00>" + part.partTitle + " (" + part.numAvailable + ")"
                                                    + "</color>", Settings.Instance.displayFont);
                                        }
                                    }
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

                using (new GUILayout.HorizontalScope())
                {
                    // This stupidity is due to a bug in the KSP skin
                    Settings.Instance.showBriefing = GUILayout.Toggle(Settings.Instance.showBriefing, "");
                    GUILayout.Label("Display Briefing");
                    Settings.Instance.bold = GUILayout.Toggle(Settings.Instance.bold, "");
                    GUILayout.FlexibleSpace();
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
#if false
                using (new GUILayout.HorizontalScope())
                {
                    Settings.Instance.saveToFile = GUILayout.Toggle(Settings.Instance.saveToFile, "");
                    GUILayout.Label("Save to file");
                    if (Settings.Instance.saveToFile)
                    {
                        bool exists = false;
                        if (Settings.Instance.fileName.Length > 0)
                            exists = Directory.Exists(Path.GetDirectoryName(Settings.Instance.fileName)) || Path.GetDirectoryName(Settings.Instance.fileName) == "";
                        GUILayout.Space(20);
                        Settings.Instance.fileName = GUILayout.TextField(Settings.Instance.fileName,
                           exists ? Settings.Instance.textFieldStyleNormal : Settings.Instance.textFieldStyleRed,
                           GUILayout.MinWidth(60), GUILayout.ExpandWidth(true));
                    }
                }
#endif

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
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Select", GUILayout.Width(90)))
                    {
                        selectVisible = true;

                        if (Settings.Instance.activeContracts == null)
                            Settings.Instance.activeContracts = new Dictionary<Guid, Contract>();
#if false
                        var aContracts = contractParser.getActiveContracts;
                        foreach (var a in aContracts)
                        {
                            if (!Settings.Instance.activeContracts.ContainsKey(a.ID))
                                Settings.Instance.activeContracts.Add(a.ID, new Contract(a));
                        }
                        if (Settings.Instance.initialShowAll)
                        {
                            foreach (var a in Settings.Instance.activeContracts)
                                a.Value.selected = true;
                        }
#endif
                    }
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Close", GUILayout.Width(90)))
                    {
                        GUIToggle();
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
            using (new GUILayout.VerticalScope())
            {
                scrollPos = GUILayout.BeginScrollView(scrollPos, Settings.Instance.scrollViewStyle);
                foreach (var a in Settings.Instance.activeContracts)
                {
                    using (new GUILayout.HorizontalScope())
                    {
                        a.Value.selected = GUILayout.Toggle(a.Value.selected, "");

                        if (GUILayout.Button(a.Value.contractContainer.Title, Settings.Instance.displayFont))
                            a.Value.selected = !a.Value.selected;
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
#if false
                        WriteContractsToFile();
#endif
                        Settings.Instance.ResetWinPos();
                    }
                    GUILayout.FlexibleSpace();
                }
            }
            if (keyToRemove != null)
                Settings.Instance.activeContracts.Remove((Guid)keyToRemove);
            GUI.DragWindow();
        }

#if false
        void WriteContractsToFile()
        {
            if (!Settings.Instance.saveToFile)
                return;
            bool exists = Directory.Exists(Path.GetDirectoryName(Settings.Instance.fileName)) || Path.GetDirectoryName(Settings.Instance.fileName) == "";
            StringBuilder str = new StringBuilder();
            if (exists)
            {
                if (Settings.Instance.activeContracts != null)
                {
                    foreach (var a in Settings.Instance.activeContracts)
                    {
                        if (a.Value.selected)
                        {
                            str.AppendLine( a.Value.contractContainer.Title);
                            str.AppendLine( a.Value.contractContainer.Briefing);
                            str.AppendLine();
                        }
                    }
                    try
                    {
                        File.WriteAllText(Settings.Instance.fileName, str.ToString());
                    }
                    catch //(Exception ex)
                    {
                        if (!Settings.Instance.failToWrite)
                            ScreenMessages.PostScreenMessage("Unable to write contracts to file: " + Settings.Instance.fileName, 10f);
                        Settings.Instance.failToWrite = true;
                    }
                }
            }
        }
#endif

    }
}
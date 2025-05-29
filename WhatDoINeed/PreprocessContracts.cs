#if  !DEBUG
#define NO_DISPLAY_DEBUG
#endif
using System;
using System.Collections.Generic;
using UnityEngine;
using static WhatDoINeed.RegisterToolbar;
using static WhatDoINeed.Utility;

namespace WhatDoINeed
{

    public partial class WhatDoINeed
    {
        internal const string checkMark = "\u2714";
        internal const string smallBullet = "\u2022";
        internal const string largeBullet = "\u25cf";

        //const string htmlRed = "<color=#ff0000>";
        //public const string htmlRed = "<color=#ff3c3c>";
        public const string htmlWhite = "<color=#ffffff>";
        //const string htmlRed = "<color=#fff12a>";  // Light yellow (copied from colors found in Mission Control)
        //public const string htmlGreen = "<color=#00ff00>";
        //const string htmlGreen = "<color=#8cf893>"; // Light green (copied from colors found in Mission Control)
        const string htmlPaleblue = "<color=#acfcff>";

        //const string htmlOrange = "<color=#ffa500>";
        //const string htmlRedOrange = "<color=#ff4d00>";
        const string htmlYellow = "<color=#ffff00>";
        const string htmlMagenta = "<color=#ff00ff>";
        //internal const string htmlCyan = "<color=#00FFFF>";

        List<DisplayLine> displayLines = new List<DisplayLine>();
        Dictionary<string, int> experimentStatus = new Dictionary<string, int>();

        string GetParamDescr(Param param)
        {
            switch (param.Key)
            {
#if false
                case "CollectScienceCustom": // BlueDog Bureau
                    Log.Info("GetParamDescr, CollectScienceCustom, experiment: " + param.experiment);
                    return param.experiment;
                    switch (param.experiment)
                    {
                        case "bd_cosmicRay":

                        case "bd_mapping":

                        case "bd_surveillance":
                            break;
                    }
                    break;
#endif
                case "SCANsatCoverage": // For SCANsat
                    switch (param.scanName)
                    {
                        case "AltimetryHiRes": return "Altimetry High Resolution";
                        case "AltimetryLoRes": return "Altimetry Low Resolution";
                        case "ResourceHiRes": return "Resource High Resolution";
                        case "ResourceLoRes": return "Resource Low Resolution";
                        case "VisualHiRes": return "Visual High Resolution";
                        case "VisualLoRes": return "Visual Low Resolution";
                    }
                    return param.scanName;

                case "StnSciParameter":
                    break;
                case "ConstructionParameter":
                    break;
                case "RepairPartParameter":
                    break;
                case "PartTest":
                    break;
                case "VesselSystemsParameter":
                    switch (param.Value)
                    {
                        case "StationContract":
                            return "Station Contract";
                    }

                    break;
                case "PartRequestParameter":
                    break;
                case "DMLongOrbitParameter": // This is for the DMagic science stuff
                    switch (param.Value)
                    {
                        case "DMMagneticSurveyContract":
                            return "Magnetic Survey Contract";
                    }
                    return param.Value;
                case "PartValidation": // For REPOSoftTech/ResearchBodies
                    break;
                case "CollectDeployedScience":
                    return "Collected Deployed Science";

            }
            return param.Value;
        }

        void DisplayPart(string expId, AvailPartWrapper availPartWrapper)
        {
            DisplayPart(expId, availPartWrapper.NameID);
        }

        void DisplayUnfilledPart(string debug, string str, bool isPart, AvailablePart partName = null)
        {
#if NO_DISPLAY_DEBUG
            debug = "";
#endif
            numMissingExperiments++;
            displayLines.Add(new DisplayLine(debug + str, 80, isPart, partName, Settings.Instance.displayFontCyan));
        }

        const bool ShowCheckmark = true;
        const bool NoCheckmark = false;
        void DisplayFilledPart(string debug, string str, int value, bool isPart, AvailablePart partName = null)
        {
#if NO_DISPLAY_DEBUG
            debug = "";
#endif
            //displayLines.Add(new DisplayLine(checkMark + " " + debug + " " + str + " (" + value + ")", 40, isPart, ShowCheckmark, partName, Settings.Instance.displayFontCyan));
            displayLines.Add(new DisplayLine(debug + str + " (" + value + ")", 80, isPart, ShowCheckmark, partName, Settings.Instance.displayFontCyan));
        }


        void DisplayUnfilledModule(string debug, string str, bool isCheckmark, bool filled = false)
        {
#if NO_DISPLAY_DEBUG
            debug = "";
#endif
            numMissingExperiments++;

            displayLines.Add(new DisplayLine(debug + " Module: " + str, 100, isCheckmark, Settings.Instance.displayFontCyan, new GUIContent("Parts"), 60, str));
        }

        void DisplayFilledModule(string debug, string str, int value)
        {
            //DisplayUnfilledModule(debug, checkMark + " Module: " + debug + " " + str + " (" + value + ")", ShowCheckmark, true);
            DisplayUnfilledModule(debug, "Module: " + debug + " " + str + " (" + value + ")", ShowCheckmark, true);
        }


        void DisplayModuleButton(string debug, string str, int value, bool showCheckmark, KeyValuePair<Guid, ContractWrapper> contract, string contractId, bool filled, ref CheckModule cm)
        {
#if NO_DISPLAY_DEBUG
            debug = "";
#endif
            DisplayModuleButton(10, 27, (cm.expanded ? minusIcon : plusIcon), 20, debug + " Module: " + debug + " " + str, showCheckmark, contract, contractId, ref cm);

        }

        void DisplayPart(string expId, string part)
        {
            AvailablePart ap = IsPartAvailable(part);
            if (ap != null)
            {
                if (Repository.partInfoList[part].numAvailable == 0)
                    DisplayUnfilledPart("1", "Part: " + CleanPartTitle(ap.title), true, ap);
                else
                {
                    DisplayFilledPart("2", "Part: " + CleanPartTitle(ap.title), Repository.partInfoList[part].numAvailable, true, ap);
                    Log.Info("DisplayPart 2, found: " + part + ", expId: " + expId);
                    experimentStatus[expId]++;
                }
            }
        }

        int DisplayPart(string expId, bool x, string part)
        {
            AvailablePart ap = IsPartAvailable(part);
            if (ap != null)
            {
                if (Repository.partInfoList[part].numAvailable == 0)
                {
                    DisplayUnfilledPart("3", "Part: " + CleanPartTitle(ap.title), true, ap);
                    return 0;
                }
                else
                {
                    DisplayFilledPart("401", "Part: " + CleanPartTitle(ap.title), Repository.partInfoList[part].numAvailable, true, ap);
                    return 1;
                }
            }
            return 0;
        }

        int DisplayPartGroup(string debug, ContractWrapper contract, string key, string partGroupName)
        {
            int cnt = 0;
#if NO_DISPLAY_DEBUG
            debug = "";
#endif
            if (experimentStatus[key] == 0)
            {
                displayLines.Add(new DisplayLine(debug + "Requirement: " +partGroupName, 30, false, null, Settings.Instance.displayFontRed));
                numUnfillableContracts++;
                Log.Info("DisplayPartGroup, numUnfillableContracts: " + numUnfillableContracts + ", partGroupName: " + partGroupName);
            }
            else
            {
                displayLines.Add(new DisplayLine(debug + "Requirement: " + partGroupName, 30, false, null, Settings.Instance.displayFontGreen));
                numFillableContracts++;
                Log.Info("DisplayPartGroup, numFillableContracts: " + numFillableContracts + ", partGroupName: " + partGroupName + ", experimentStatus[experimentID]: " + experimentStatus[key]);
            }


            if (contract.NeededParts.ContainsKey(key))
            {
                foreach (AvailPartWrapper apw in contract.NeededParts[key])
                {
                    cnt += DisplayPart(key, false, apw.NameID);
                }
            }
            else
            {
                Log.Error("PartGroup: " + key + " not found in NeededParts");
            }
            return cnt;
        }
        void DisplayModule(string expId, CheckModule cm)
        {
            if (Repository.contractObjectives[cm.ModuleTypes] > 0)
            {
                DisplayFilledPart("5", "Module: " + cm.ModuleTypes, Repository.contractObjectives[cm.ModuleTypes], false);
                Log.Info("DisplayModule 5, found: " + cm.ModuleTypes + ", expId: " + expId);
                experimentStatus[expId]++;
            }
            else
                DisplayUnfilledPart("6", "Module: " + cm.ModuleTypes, false);


        }

        bool CheckPartsForModule(string module)
        {
            if (!Repository.moduleInformation.ContainsKey(module))
                return false;
            for (int i = 0; i < Repository.moduleInformation[module].partsWithModule.Count; i++)
            {
                AvailablePart ap = Repository.moduleInformation[module].partsWithModule[i];
                if (ResearchAndDevelopment.PartModelPurchased(ap) || ResearchAndDevelopment.IsExperimentalPart(ap))
                {
                    string title = Utility.CleanPartTitle(ap.title);
                    string str = title;
                    //bool isCheckmark = false;
                    if (Repository.partInfoList.ContainsKey(ap.name))
                    {
                        if (Repository.partInfoList[ap.name].numAvailable > 0)
                            return true;
                    }
                }
            }
            return false;
        }

        void DisplayPartsForModule(string module, bool expanded = true)
        {
            if (!Repository.moduleInformation.ContainsKey(module))
                return;
            //Log.Info("DisplayPartsForModule, module: " + module + ", parts.Count: " + Repository.moduleInformation[module].partsWithModule.Count);
            if (expanded)
            {
                for (int i = 0; i < Repository.moduleInformation[module].partsWithModule.Count; i++)
                {
                    AvailablePart ap = Repository.moduleInformation[module].partsWithModule[i];
                    if (ResearchAndDevelopment.PartModelPurchased(ap) || ResearchAndDevelopment.IsExperimentalPart(ap))
                    {
                        string title = Utility.CleanPartTitle(ap.title);
                        string str = title;
                        bool isCheckmark = false;
                        if (!Repository.partInfoList.ContainsKey(ap.name))
                        {
                            Log.Info("DisplayPartsForModule, ap.name: " + ap.name + " not in partInfoList");
                        }
                        else
                        {
                            if (Repository.partInfoList[ap.name].numAvailable > 0)
                                isCheckmark = true;
                        }
                        displayLines.Add(new DisplayLine(title, 80, true, isCheckmark, ap, Settings.Instance.displayFontCyan));
                    }
                }
            }
        }

        int DisplayModule(ref CheckModule cm, KeyValuePair<Guid, ContractWrapper> contract, string contractId)
        {
            Log.Info("DisplayModule, cm.moduleTypes: " + cm.ModuleTypes);
            bool showCheckmark = CheckPartsForModule(cm.ModuleTypes);
            if (Repository.contractObjectives[cm.ModuleTypes] > 0)
            {

                //DisplayFilledModule("7", cm.ModuleTypes, Repository.contractObjectives[cm.ModuleTypes]);
                DisplayModuleButton("71", cm.ModuleTypes, Repository.contractObjectives[cm.ModuleTypes], showCheckmark, contract, contractId, false, ref cm);
                DisplayPartsForModule(cm.ModuleTypes, cm.expanded);

                return 1;
            }
            else
            {
                //DisplayUnfilledModule("8", cm.ModuleTypes);
                DisplayModuleButton("81", cm.ModuleTypes, Repository.contractObjectives[cm.ModuleTypes], showCheckmark, contract, contractId, false, ref cm);
                DisplayPartsForModule(cm.ModuleTypes, cm.expanded);
                return 0;
            }
        }

        int DisplayModule(string module)
        {
            // TODO need to get module info
            if (Repository.moduleInformation[module].numAvailable == 0)
            {
                DisplayUnfilledModule("9", module, NoCheckmark);
                DisplayPartsForModule(module);
                return 0;
            }
            else
            {
                DisplayFilledModule("10", module, Repository.moduleInformation[module].numAvailable);
                DisplayPartsForModule(module);
                return 1;
            }
        }


        int DisplayCategory(string category)
        {
            if (Enum.TryParse<PartCategories>(category, out PartCategories value))
            {
                if (Repository.partCategories[category].numAvailable == 0)
                    DisplayUnfilledPart("11", "Category: " + category, false);
                else
                {
                    DisplayFilledPart("12", "Category: " + category, Repository.partCategories[category].numAvailable, false);
                    return 1;
                }
            }
            else
            {
                DisplayUnfilledPart("13", "Category not found: " + category, false);
            }
            return 0;
        }

#if false
        void DisplayLabel(string str)
        {
            displayLines.Add(new DisplayLine(htmlCyan + str + "</color>", 30, Settings.Instance.displayFont));
        }
#endif

        void DisplayExperiment(string debug, string experimentID, string str)
        {
#if NO_DISPLAY_DEBUG
            debug = "";
#endif
            if (experimentStatus.ContainsKey(experimentID))
            {
                if (experimentStatus[experimentID] == 0)
                {
                    displayLines.Add(new DisplayLine(debug + str, 30, false, null, Settings.Instance.displayFontRed));
                    if (experimentID.Contains("VesselSystemsParameter"))
                        displayLines.Add(new DisplayLine("(all needed)", 60, false, null, Settings.Instance.displayFontOrange));
                    numUnfillableContracts++;
                    Log.Info("DisplayExperiment, numUnfillableContracts: " + numUnfillableContracts + ", str: " + str);
                }
                else
                {
                    displayLines.Add(new DisplayLine(debug + str, 30, false, null, Settings.Instance.displayFontGreen));
                    numFillableContracts++;
                    Log.Info("DisplayExperiment, numFillableContracts: " + numFillableContracts + ", str: " + str + ", experimentStatus[experimentID]: " + experimentStatus[experimentID]);
                }

            }
            else
            {
                experimentStatus[experimentID] = 0;
                Log.Info("DisplayExperiment, experimentID: " + experimentID + " not in experimentStatus[]");
            }
        }

        int DisplayParamInfo(string expId, Param param)
        {
            Log.Info("DisplayParamInfo, param: " + param.Log());
            if (Repository.allExperimentParts.ContainsKey(param.subjectId))
            {
                foreach (var a in Repository.allExperimentParts[param.subjectId].parts)
                {
                    Log.Info("DisplayParamInfo, part: " + a + ", isOnShip: " + Repository.partInfoList[a].isOnShip + ", numAvailable: " + Repository.partInfoList[a].numAvailable);
                    if (Repository.partInfoList[a].isOnShip)
                    {
                        DisplayFilledPart("15", ResearchAndDevelopment.GetExperiment(param.subjectId).experimentTitle, 1, false);
                        return 1;
                    }
                }

                DisplayUnfilledPart("14", ResearchAndDevelopment.GetExperiment(param.subjectId).experimentTitle, false);
            }
            return 0;
        }


        int DisplayEngineType(string type)
        {

            if (Enum.TryParse<EngineType>(type, out EngineType value))
            {
                if (Repository.engineTypes[(int)value] == 0)
                    DisplayUnfilledPart("15", "Engine Type: " + type, false);
                else
                {
                    DisplayFilledPart("16", "Engine Type: " + type, Repository.engineTypes[(int)value], false);
                    return 1;
                }
            }
            else
                DisplayUnfilledPart("17", "Engine Type not found: " + type, false);
            return 0;
        }

        int DisplayFilter(Filter filter)
        {
            string partModule = null;
            string part = null;
            string category = null;

            switch (filter.category)
            {
                case "partModule":
                    partModule = filter.value;
                    return DisplayModule(partModule);
                case "category":
                    category = filter.value;
                    return DisplayCategory(category);
                case "NONE":
                    switch (filter.value)
                    {
                        case "engineType":
                            return DisplayEngineType(filter.type);
                        case "moduleName":
                            partModule = filter.type;
                            return DisplayModule(partModule);
                    }
                    break;
                case "FILTER":
                    switch (filter.value)
                    {
                        case "part":
                            part = filter.type;
                            return DisplayPart("Filter", false, part);
                        case "partModule":
                            partModule = filter.type;
                            return DisplayModule(partModule);
                    }
                    break;
            }
            return 0;
        }

        void DisplayButton(int space, GUIContent buttonStr, int width, string str, KeyValuePair<Guid, ContractWrapper> contract, string contractId, ref bool requirementsOpen)
        {
            displayLines.Add(new DisplayLine(space, buttonStr, width, str, contract, contractId, requirementsOpen, Settings.Instance.largeDisplayFont));
        }

        void DisplayModuleButton(int space, int indent, GUIContent buttonStr, int width, string str, bool showCheckmark, KeyValuePair<Guid, ContractWrapper> contract, string contractId, ref CheckModule cm)
        {
            displayLines.Add(new DisplayLine(space, indent, buttonStr, width, str, showCheckmark, contract, contractId, cm.expanded, Settings.Instance.displayFontCyan, ref cm));
        }

        void DisplayTextArea(int space, string briefing)
        {
            displayLines.Add(new DisplayLine(30, briefing, Settings.Instance.textAreaFont, true));
        }



        void ShowProcessedContracts()
        {
            if (Settings.Instance == null)
            {
                Log.Error("ShowProcessedContracts, Settings.Instance is null");
                return;
            }
            if (Settings.Instance.scrollViewStyle == null)
            {
                Log.Error("Settings.Instance.scrollViewStyle is null");
                return;
            }
            contractPos = GUILayout.BeginScrollView(contractPos, Settings.Instance.scrollViewStyle, GUILayout.MaxHeight(Screen.height - 20));
            for (int i = 0; i < displayLines.Count; i++)
            {
                DisplayLine line = displayLines[i];
                switch (line.lineType)
                {
                    case DisplayLineType.normal:
                        {
                            using (new GUILayout.HorizontalScope())
                            {
                                GUILayout.Space(line.space);
                                if (line.showCheckmark)
                                {
                                    GUILayout.Label(new GUIContent(checkMark, ""), line.font, GUILayout.Width(20));
                                    GUILayout.Space(10);
                                }
                                else
                                    GUILayout.Space(30);
                                if (line.isPart && line.partName != null && Settings.Instance.allowPartSpawning)
                                {
                                    if (GUILayout.Button(line.line, line.font))
                                    {
                                        if (EditorLogic.SelectedPart != null)
                                            EditorLogic.DeletePart(EditorLogic.SelectedPart);
                                        EditorLogic.fetch.SpawnPart(line.partName);
                                        if (Settings.Instance.HideWindowsWhenSpawning)
                                        {
                                            quickHideEnd = Time.realtimeSinceStartup + Settings.Instance.SpawnHideTime;
                                            FadeStatus = Fade.decreasing;
                                        }
                                    }
                                }
                                else
                                    GUILayout.Label(line.line, line.font);
                            }
                        }
                        break;
                    case DisplayLineType.button:
                        {
                            GUILayout.Space(line.buttonSpacer);
                            using (new GUILayout.HorizontalScope())
                            {
                                if (GUILayout.Button(line.buttonStr, line.buttonFont, GUILayout.Width(line.buttonWidth)))
                                {
                                    line.requirementsOpen = !line.requirementsOpen;
                                    openClosed[line.contractId] = line.requirementsOpen;
                                    Log.Info("DisplayLineType.button, line.requirementsOpen: " + line.requirementsOpen);

                                }
                                GUILayout.Label(line.buttonTitle, line.buttonFont);

                            }
                        }
                        break;

                    case DisplayLineType.moduleToggleButton:
                        {
                            GUILayout.Space(line.buttonSpacer);
                            using (new GUILayout.HorizontalScope())
                            {
                                GUILayout.Space(30);
                                if (line.indent > 0)
                                    GUILayout.Space((int)line.indent);
                                if (GUILayout.Button(line.buttonStr, line.buttonFont, GUILayout.Width(line.buttonWidth)))
                                {
                                    line.checkModule.expanded = !line.checkModule.expanded;
                                    Log.Info("DisplayLineType.moduleToggleButton, line.checkModule.expanded: " + line.checkModule.expanded);
                                    //PartsWindow.GetInstance(line.buttonTitle);
                                }
#if true
                                if (line.showCheckmark)
                                {
                                    GUILayout.Label(new GUIContent(checkMark, ""), line.buttonFont, GUILayout.Width(20));
                                    //GUILayout.Space(10);
                                }
                                else
                                    GUILayout.Space(20);
#endif
                                GUILayout.Label(line.buttonTitle, line.buttonFont);
                                GUILayout.Space(20);
                                if (GUILayout.Button("Parts", Settings.Instance.buttonFont, GUILayout.Width(60 /* line.buttonWidth */)))
                                {
                                    Log.Info("buttonData: " + line.checkModule.ModuleTypes.ToString());
                                    NeededPartsWindow.GetInstance(line.checkModule.ModuleTypes.ToString());
                                }
                                GUILayout.FlexibleSpace();
                            }
                        }
                        break;

                    case DisplayLineType.windowbutton:
                        using (new GUILayout.HorizontalScope())
                        {
                            GUILayout.Space(line.space);
                            GUILayout.Label(line.line, line.font);
                            GUILayout.FlexibleSpace();
                        }

                        break;


                    case DisplayLineType.textarea:
                        {
                            using (new GUILayout.HorizontalScope())
                            {
                                GUILayout.Space(line.textAreaSpace);
                                GUILayout.TextArea(line.textArea, line.textareaFont);

                            }
                        }
                        break;
                }
            }
            GUILayout.EndScrollView();
        }



        public void PreprocessContracts(int from)
        {
            Log.Info("PreprocessContracts 1, from: " + from + ", numMissingExperiments: " + numMissingExperiments +
                ", numFillableContracts: " + numFillableContracts + ", numUnfillableContracts: " + numUnfillableContracts);
            experimentStatus.Clear();

            //
            // The entire process is done two times because the contracts need to be shown before the parts and modules
            // But the part and modules aren't seen until after the first pass
            //
            for (int z = 0; z < 2; z++)
            {
                numMissingExperiments = 0;
                numFillableContracts = 0;
                numUnfillableContracts = 0;
                displayLines.Clear();

                if (Repository.Contracts != null)
                {
                    foreach (KeyValuePair<Guid, ContractWrapper> contract in Repository.Contracts)
                    {
                        if (contract.Value.selected)
                        {
                            //newNumDisplayedContracts++;
                            var contractId = "repository-" + contract.Key.ToString();
                            bool requirementsOpen = false;
                            if (!openClosed.TryGetValue(contractId, out requirementsOpen))
                            {
                                openClosed.Add(contractId, requirementsOpen);
                                requirementsOpen = openClosed[contractId] = true;
                            }

                            DisplayButton(10, requirementsOpen ? minusIcon : plusIcon, 20, contract.Value.contractContainer.Title, contract, contractId, ref requirementsOpen);

                            if (Settings.Instance.showBriefing && openClosed[contractId])
                            {
                                DisplayTextArea(30, contract.Value.contractContainer.Briefing);
                            }
                            if (requirementsOpen)
                            {
                                var experiments = repository.GetExperimentsInContract(contract.Key);
                                Log.Info("requirementsOpen, experiments.count: " + experiments.Count);

                                for (int e = 0; e < experiments.Count; e++)
                                {
                                    Experiment experiment = experiments[e];

                                    string experimentID = (experiment.scanSatExperiment) ? experiment.scanType.ToString() : experiment.ExperimentID;

                                    if (experiment.scanSatExperiment)
                                        DisplayExperiment("1", experiment.ExperimentID, "Experiment:  " + experiment.scanType.ToString());
                                    else
                                        DisplayExperiment("2", experiment.ExperimentID, "Experiment:  " + experiment.experimentTitle);

                                    Log.Info("experimentID: " + experiment.ExperimentID);
                                    switch (experimentID)
                                    {
                                        case "PartTest":
                                        case "ConstructionParameter":
                                            {
                                                experimentStatus[experiment.ExperimentID] = 0;
                                                foreach (List<AvailPartWrapper> partsList in contract.Value.NeededParts.Values)
                                                {
                                                    for (int pl = 0; pl < partsList.Count; pl++)
                                                    {
                                                        var part = partsList[pl];
                                                        DisplayPart(experiment.ExperimentID, part);
                                                    }
                                                }
                                            }
                                            break;
                                        default:
                                            {
                                                if (Repository.allExperimentParts.ContainsKey(experimentID))
                                                {
                                                    foreach (var part in Repository.allExperimentParts[experimentID].parts)
                                                        DisplayPart(experimentID, part);
                                                }
                                                else
                                                {
                                                    DisplayUnfilledPart("19", " " + experimentID + " missing", false);

                                                }
                                                break;
                                            }
                                    }
                                    var availParts = repository.GetPartsForExperimentInContract(contract.Key, experiment.ExperimentID);
                                    if (availParts.Count > 0)
                                    {
                                        //DisplayLabel("1 Fulfilling Parts:");
                                        for (int i = 0; i < availParts.Count; i++)
                                        {
                                            var ap = availParts[i];
                                            DisplayPart(experiment.ExperimentID, ap);
                                        }
                                    }
                                }

                                if (contract.Value.PartGroups.Count > 0)
                                {
                                    //DisplayLabel("2 Fulfilling Parts:");

                                    foreach (var pg in contract.Value.PartGroups)
                                    {
                                        if (!experimentStatus.ContainsKey(pg.Key))
                                            experimentStatus[pg.Key] = 0;
                                        experimentStatus[pg.Key] = DisplayPartGroup("19", contract.Value, pg.Key, pg.Value.partGroupName);
                                    }
                                }

                                foreach (Param param in contract.Value.Params)
                                {
                                    string s = GetParamDescr(param);
                                    if (param.scanType != SCANsatSCANtype.Nothing)
                                        DisplayExperiment("3", param.Key, "Experiment:  " + param.scanType.ToString());
                                    else
                                        DisplayExperiment("4", param.Key, "Experiment:  " + s);

                                    //DisplayLabel("3 Fulfilling Parts and Modules:");

                                    int cnt = 0;
                                    bool avail = false;
                                    int totalNeeded = (param.RequestedParts.Count > 0 ? 1 : 0) +
                                        (param.PartNames.Count > 0 ? 1 : 0) +
                                        param.CheckModules.Count +
                                        contract.Value.NeededParts.Count +
                                        param.Filters.Count +
                                        (param.subjectId != null ? 1 : 0);

                                    for (int p = 0; p < param.RequestedParts.Count; p++)
                                    {
                                        var part = param.RequestedParts[p];
                                        cnt += DisplayPart(param.Key, false, part);
                                    }

                                    for (int p = 0; p < param.PartNames.Count; p++)
                                    {
                                        var str = param.PartNames[p];
                                        avail = (DisplayPart(param.Key, false, str) > 0);
                                    }
                                    cnt += (avail ? 1 : 0);
                                    for (int cmi = 0; cmi < param.CheckModules.Count; cmi++)
                                    {
                                        CheckModule checkModule = param.CheckModules[cmi];
                                        contractId = "repository-" + contract.Key.ToString();
                                        cnt += DisplayModule(ref checkModule, contract, contractId);
                                    }
                                    if (param.subjectId != null)
                                    {
                                        cnt += DisplayParamInfo(param.Key, param);
                                    }

                                    //for (int fi = 0; fi < param.Filters.Count; fi++)
                                    //{
                                    //    var filter = param.Filters[fi];
                                    //}
                                    if (param.Filters.Count > 0)
                                    {
                                        for (int fi = 0; fi < param.Filters.Count; fi++)
                                        {
                                            var filter = param.Filters[fi];

                                            cnt += DisplayFilter(filter);
                                            if (filter.category == "part")
                                            {
                                                cnt += DisplayPart(param.Key, false, filter.type);
                                            }
                                            if (filter.category == "partModule")
                                            {
                                                cnt += DisplayPart(param.Key, false, filter.type);
                                            }
                                        }
                                    }

                                    if (cnt == totalNeeded)
                                    {
                                        experimentStatus[param.Key] = 1;
                                        Log.Info("After all checks, cnt == totalNeeded, param.Key: " + param.Key);

                                    }
                                    //Log.Info("Experiment: " + s + ", totalNeeded: " + totalNeeded + ", cnt: " + cnt + ", contractID: " + contractId);
                                    //Log.Info("RequestedParts.Count: " + param.RequestedParts.Count +
                                    //    ", PartNames.Count: " + param.PartNames.Count +
                                    //    ", CheckModules.Count: " + param.CheckModules.Count +
                                    //    ", Filters.Count: " + param.Filters.Count +
                                    //    ", param.subjectId != null: " +
                                    //    (param.subjectId != null).ToString());
                                }
                            }
                        }
                    }
                }
            }

#if DEBUG
            Log.Info("vvvvvvvvvvvv experimentStatus vvvvvvvvvvvvvvv");
            foreach (var e in experimentStatus)
            {
                Log.Info("experimentStatus, key: " + e.Key + ", value: " + e.Value);
            }
            Log.Info("^^^^^^^^^^^^ experimentStatus ^^^^^^^^^^^^^^^");
#endif
            Log.Info("PreprocessContracts 2, from: " + from + ", numMissingExperiments: " + numMissingExperiments +
                ", numFillableContracts: " + numFillableContracts + ", numUnfillableContracts: " + numUnfillableContracts);
        }

    }
}

using SpaceTuxUtility;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

//using ContractParser;

namespace WhatDoINeed
{
    public class Settings
    {
        public const float WINDOW_WIDTH = 400;
        public const float WINDOW_HEIGHT = 300;

        public const float SEL_WINDOW_WIDTH = 400;
        public const float SEL_WINDOW_HEIGHT = 300;

        public const float SETTINGS_WINDOW_WIDTH = 300;
        public const float SETTINGS_WINDOW_HEIGHT = 300;


        public static Settings Instance;
        internal GUIStyle largeDisplayFont, displayFont, displayFontGreen, textAreaFont, textAreaSmallFont;
        internal GUIStyle settingsFontSizeDisplayFont, settingsDisplayFont, settingsToggleDisplayFont;
        internal GUIStyle launchCheckDisplayFont, launchCheckDisplayFontRed, displayFontCyan, displayFontRed, displayFontOrange, buttonFont;

        internal GUIStyle kspWindow;

        internal GUIStyle myStyle;
        internal Texture2D styleOff;
        internal Texture2D styleOn;
        internal GUIStyle resizeButton;
        internal GUIStyle scrollViewStyle;

        internal GUIStyle textFieldStyleRed;
        internal GUIStyle textFieldStyleNormal;
        internal GUIStyle textAreaWordWrap;

        // Following are saved in a file
        internal bool helpWindowShown = false;
        internal const float DEFAULT_FONT_SIZE = 12f;
        internal float fontSize = DEFAULT_FONT_SIZE;
        internal bool bold = false;
        internal bool initialShowAll = true;
        internal bool showBriefing = false;
        internal float Alpha = 255;
        internal float HideTime = 15;
        internal bool lockPos = false;
        internal bool enableClickThrough = true;
        internal bool reopenIfLastOpen = true;
        internal bool lastContractWindowVisibleStatus = false;
        internal bool lastSettingsWindowVisibleStatus = false;
        internal bool lastSelectWindowVisibleStatus = false;
        internal bool lastPartsWindowVisibleStatus = false;


        internal bool allowPartSpawning = true;
        //internal bool ShowModName = true;
        internal bool HideWindowsWhenSpawning = true;
        internal float SpawnHideTime = 5;

        internal bool checkForMissingBeforeLaunch = true;
        internal bool onlyCheckSelectedContracts = true;
        internal bool debugMode = false;

        internal Rect winPos = new Rect((Screen.width - WINDOW_WIDTH) * 0.5f, (Screen.height - WINDOW_HEIGHT) * 0.5f, WINDOW_WIDTH, WINDOW_HEIGHT);

        internal Rect selWinPos = new Rect(Screen.width / 2 - SEL_WINDOW_WIDTH / 2, Screen.height / 2 - SEL_WINDOW_HEIGHT / 2, SEL_WINDOW_WIDTH, SEL_WINDOW_HEIGHT);

        internal Rect settingsWinPos = new Rect((Screen.width - SETTINGS_WINDOW_WIDTH) * 0.5f, (Screen.height - SETTINGS_WINDOW_HEIGHT) * 0.5f, SETTINGS_WINDOW_WIDTH, SETTINGS_WINDOW_HEIGHT);

        internal Rect partsWinPos = new Rect(Screen.width / 2 - SEL_WINDOW_WIDTH / 2, Screen.height / 2 - SETTINGS_WINDOW_HEIGHT / 2, SETTINGS_WINDOW_WIDTH, SETTINGS_WINDOW_HEIGHT);

        internal Dictionary<Guid, bool> activeContracts = new Dictionary<Guid, bool>();

        internal static string CFG_PATH { get { return KSPUtil.ApplicationRootPath + "GameData/WhatDoINeed/PluginData/"; } }
        static readonly string CFG_FILE = CFG_PATH + "displayInfo.cfg";

        internal static readonly string DISPLAYINFO_NODENAME = "DISPLAYINFO";
        internal static readonly string CONTRACT_NODENAME = "CONTRACT";

        public Settings()
        {
            //Log.Info("settings.Settings");
            Instance = this;
        }

        public void ResetWinPos()
        {
            //Log.Info("settings.ResetWinPos");
            //if (HighLogic.LoadedScene != GameScenes.EDITOR)
            //    winPos = new Rect();
            //editorWinPos = new Rect();
        }


        public void SaveData(ConfigNode configFileNode)
        {
            ConfigNode contracts = new ConfigNode("CONTRACTS");
            configFileNode.AddNode(contracts);
            foreach (var contract in Repository.Contracts.Values)
            {
                if (contract.selected)
                {
                    contracts.AddValue("contractGuid", contract.Id);
                }
            }
        }

        public void SaveData()
        {
            //Log.Info("settings.SaveData");
            var configFile = new ConfigNode();
            var configFileNode = new ConfigNode(DISPLAYINFO_NODENAME);


            configFileNode.AddValue("helpWindowShown", helpWindowShown);
            configFileNode.AddValue("fontSize", fontSize);
            configFileNode.AddValue("bold", bold);
            configFileNode.AddValue("showBriefing", showBriefing);
            configFileNode.AddValue("initialShowAll", initialShowAll);

            configFileNode.AddValue("Alpha", Alpha);
            configFileNode.AddValue("HideTime", HideTime);
            configFileNode.AddValue("lockPos", lockPos);
            configFileNode.AddValue("enableClickThrough", enableClickThrough);

            configFileNode.AddValue("reopenIfLastOpen", reopenIfLastOpen);
            configFileNode.AddValue("lastVisibleStatus", lastContractWindowVisibleStatus);
            configFileNode.AddValue("lastSettingsWindowVisibleStatus", lastSettingsWindowVisibleStatus);
            configFileNode.AddValue("lastSelectWindowVisibleStatus", lastSelectWindowVisibleStatus);
            configFileNode.AddValue("lastPartsWindowVisibleStatus", lastPartsWindowVisibleStatus);



            configFileNode.AddValue("allowPartSpawning", allowPartSpawning);
            configFileNode.AddValue("HideWindowsWhenSpawning", HideWindowsWhenSpawning);
            configFileNode.AddValue("SpawnHideTime", SpawnHideTime);


            //configFileNode.AddValue("ShowModName", ShowModName);


            configFileNode.AddValue("checkForMissingBeforeLaunch", checkForMissingBeforeLaunch);
            configFileNode.AddValue("onlyCheckSelectedContracts", onlyCheckSelectedContracts);

            configFileNode.AddValue("x", winPos.x);
            configFileNode.AddValue("y", winPos.y);
            configFileNode.AddValue("width", winPos.width);
            configFileNode.AddValue("height", winPos.height);

            configFileNode.AddValue("selWinPos-x", selWinPos.x);
            configFileNode.AddValue("selWinPos-y", selWinPos.y);
            configFileNode.AddValue("selWinPos-width", selWinPos.width);
            configFileNode.AddValue("selWinPos-height", selWinPos.height);

            configFileNode.AddValue("settings-x", settingsWinPos.x);
            configFileNode.AddValue("settings-y", settingsWinPos.y);
            configFileNode.AddValue("settings-width", settingsWinPos.width);
            configFileNode.AddValue("settings-height", settingsWinPos.height);

            configFileNode.AddValue("parts-x", partsWinPos.x);
            configFileNode.AddValue("parts-y", partsWinPos.y);
            configFileNode.AddValue("parts-width", partsWinPos.width);
            configFileNode.AddValue("parts-height", partsWinPos.height);



            configFile.AddNode(configFileNode);

            configFile.Save(CFG_FILE);
        }

        // Following used by Scenario
        public void LoadData(ConfigNode configFileNode)
        {
            if (configFileNode != null)
            {
                ConfigNode contracts = configFileNode.GetNode("CONTRACTS");
                if (contracts != null)
                {
                    var guids = contracts.GetValuesList("contractGuid");
                    activeContracts.Clear();
                    foreach (var guid in guids)
                    {
                        Guid contractGuid = new Guid(guid);
                        activeContracts[contractGuid] = true;
                        //RegisterToolbar.Log.Info("settings.LoadData, activeContracts, contractGuid: " + contractGuid);
                    }
                }

            }
        }
        public void LoadData()
        {
            //Log.Info("settings.LoadData");
            winPos = new Rect(Screen.width - WINDOW_WIDTH, Screen.height / 8, WINDOW_WIDTH, WINDOW_HEIGHT);
            if (File.Exists(CFG_FILE))
            {
                var configFile = ConfigNode.Load(CFG_FILE);
                if (configFile != null)
                {
                    var configFileNode = configFile.GetNode(DISPLAYINFO_NODENAME);
                    if (configFileNode != null)
                    {

                        helpWindowShown = configFileNode.SafeLoad("helpWindowShown", helpWindowShown);
                        fontSize = configFileNode.SafeLoad("fontSize", fontSize);
                        WhatDoINeed.SetFontSizes(fontSize, false);
                        bold = configFileNode.SafeLoad("bold", bold);
                        showBriefing = configFileNode.SafeLoad("showBriefing", showBriefing);
                        initialShowAll = configFileNode.SafeLoad("initialShowAll", initialShowAll);

                        Alpha = configFileNode.SafeLoad("Alpha", Alpha);
                        HideTime = configFileNode.SafeLoad("HideTime", HideTime);
                        lockPos = configFileNode.SafeLoad("lockPos", lockPos);
                        enableClickThrough = configFileNode.SafeLoad("enableClickThrough", enableClickThrough);

                        reopenIfLastOpen = configFileNode.SafeLoad("reopenIfLastOpen", reopenIfLastOpen);
                        lastContractWindowVisibleStatus = configFileNode.SafeLoad("lastVisibleStatus", lastContractWindowVisibleStatus);

                        lastSettingsWindowVisibleStatus = configFileNode.SafeLoad("lastSettingsWindowVisibleStatus", lastSettingsWindowVisibleStatus);
                        lastSelectWindowVisibleStatus = configFileNode.SafeLoad("lastSelectWindowVisibleStatus", lastSelectWindowVisibleStatus);
                        lastPartsWindowVisibleStatus = configFileNode.SafeLoad("lastPartsWindowVisibleStatus", lastPartsWindowVisibleStatus);

                        allowPartSpawning = configFileNode.SafeLoad("allowPartSpawning", allowPartSpawning);
                        HideWindowsWhenSpawning = configFileNode.SafeLoad("HideWindowsWhenSpawning", HideWindowsWhenSpawning);
                        SpawnHideTime = configFileNode.SafeLoad("SpawnHideTime", SpawnHideTime);

                        //ShowModName = configFileNode.SafeLoad("ShowModName", ShowModName);

                        checkForMissingBeforeLaunch = configFileNode.SafeLoad("checkForMissingBeforeLaunch", checkForMissingBeforeLaunch);
                        onlyCheckSelectedContracts = configFileNode.SafeLoad("onlyCheckSelectedContracts", onlyCheckSelectedContracts);

                        winPos.x = configFileNode.SafeLoad("x", (Screen.width - WINDOW_WIDTH) * 0.5f);
                        winPos.y = configFileNode.SafeLoad("y", (Screen.height - WINDOW_HEIGHT) * 0.5f);
                        winPos.width = configFileNode.SafeLoad("width", WINDOW_WIDTH);
                        winPos.height = configFileNode.SafeLoad("height", WINDOW_HEIGHT);

                        selWinPos.x = configFileNode.SafeLoad("selWinPos-x", (Screen.width - WINDOW_WIDTH) * 0.5f);
                        selWinPos.y = configFileNode.SafeLoad("selWinPos-y", (Screen.height - WINDOW_HEIGHT) * 0.5f);
                        selWinPos.width = configFileNode.SafeLoad("selWinPos-width", WINDOW_WIDTH);
                        selWinPos.height = configFileNode.SafeLoad("selWinPos-height", WINDOW_HEIGHT);

                        settingsWinPos.x = configFileNode.SafeLoad("settings-x", (Screen.width - WINDOW_WIDTH) * 0.5f);
                        settingsWinPos.y = configFileNode.SafeLoad("settings-y", (Screen.height - WINDOW_HEIGHT) * 0.5f);
                        settingsWinPos.width = configFileNode.SafeLoad("settings-width", SETTINGS_WINDOW_WIDTH);
                        settingsWinPos.height = configFileNode.SafeLoad("settings-height", SETTINGS_WINDOW_HEIGHT);

                        partsWinPos.x = configFileNode.SafeLoad("parts-x", (Screen.width - WINDOW_WIDTH) * 0.5f);
                        partsWinPos.y = configFileNode.SafeLoad("parts-y", (Screen.height - WINDOW_HEIGHT) * 0.5f);
                        partsWinPos.width = configFileNode.SafeLoad("parts-width", WINDOW_WIDTH);
                        partsWinPos.height = configFileNode.SafeLoad("parts-height", WINDOW_HEIGHT);

                    }

                }
            }
        }
    }
}
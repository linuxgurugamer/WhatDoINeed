using System;
using System.IO;
using System.Collections.Generic;

using UnityEngine;
using SpaceTuxUtility;
using static WhatDoINeed.RegisterToolbar;
using ContractParser;

//using ContractParser;

namespace WhatDoINeed
{
    public class Settings
    {
        public const float WINDOW_WIDTH = 500;
        public const float WINDOW_HEIGHT = 100;

        public static Settings Instance;
        internal GUIStyle largeDisplayFont, displayFont, textAreaFont, textAreaSmallFont; //, labelFont;
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
        internal float fontSize = 12f;
        internal bool bold = false;
        internal bool initialShowAll = true;
        internal bool showBriefing = false;
        internal float Alpha = 255;
        internal float HideTime = 15;
        internal bool lockPos = false;
        internal bool hideButtons = false;
        internal bool enableClickThrough = true;
        internal bool reopenIfLastOpen = true;
        internal bool lastVisibleStatus = false;

        internal Rect winPos = new Rect(Screen.width / 2 - WINDOW_WIDTH / 2, Screen.height / 2 - WINDOW_HEIGHT / 2, WINDOW_WIDTH, WINDOW_HEIGHT);
        //internal Rect editorWinPos;

        internal Dictionary<Guid, Contract> activeContracts;

        internal static string CFG_PATH { get { return KSPUtil.ApplicationRootPath + "GameData/WhatDoINeed/PluginData/"; } }
        static readonly string CFG_FILE = CFG_PATH + "displayInfo.cfg";

        internal static readonly string DISPLAYINFO_NODENAME = "DISPLAYINFO";
        internal static readonly string CONTRACT_NODENAME = "CONTRACT";

        public Settings()
        {
            Log.Info("settings.Settings");
            Instance = this;
            activeContracts = new Dictionary<Guid, Contract>();
        }

        public void ResetWinPos()
        {
            Log.Info("settings.ResetWinPos");
            //if (HighLogic.LoadedScene != GameScenes.EDITOR)
            //    winPos = new Rect();
            //editorWinPos = new Rect();
        }
        public void SaveData()
        {
            Log.Info("settings.SaveData");
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
            configFileNode.AddValue("hideButtons", hideButtons);
            configFileNode.AddValue("enableClickThrough", enableClickThrough);

            configFileNode.AddValue("reopenIfLastOpen", reopenIfLastOpen);
            configFileNode.AddValue("lastVisibleStatus", lastVisibleStatus);
            

            configFileNode.AddValue("x", winPos.x);
            configFileNode.AddValue("y", winPos.y);
            configFileNode.AddValue("width", winPos.width);
            configFileNode.AddValue("height", winPos.height);

            configFile.AddNode(configFileNode);

            ConfigNode contracts = new ConfigNode("CONTRACTS");
            configFileNode.AddNode(contracts);
            Log.Info("Writing contracts to file");
            foreach (var a in Settings.Instance.activeContracts)
            {
                if (a.Value.selected)
                {
                    contracts.AddValue("contractGuid", a.Key);
                    Log.Info("contractGuid: " + a.Key);
                }
            }

            configFile.Save(CFG_FILE);
        }

        public void LoadData()
        {
            Log.Info("settings.LoadData");
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
                        bold = configFileNode.SafeLoad("bold", bold);
                        showBriefing = configFileNode.SafeLoad("showBriefing", showBriefing);
                        initialShowAll = configFileNode.SafeLoad("initialShowAll", initialShowAll);

                        Alpha = configFileNode.SafeLoad("Alpha", Alpha);
                        HideTime = configFileNode.SafeLoad("HideTime", HideTime);
                        lockPos = configFileNode.SafeLoad("lockPos", lockPos);
                        hideButtons = configFileNode.SafeLoad("hideButtons", hideButtons);
                        enableClickThrough = configFileNode.SafeLoad("enableClickThrough", enableClickThrough);

                        reopenIfLastOpen = configFileNode.SafeLoad("reopenIfLastOpen", reopenIfLastOpen);
                        lastVisibleStatus = configFileNode.SafeLoad("lastVisibleStatus", lastVisibleStatus);
                        
                        winPos.x = configFileNode.SafeLoad("x", winPos.x);
                        winPos.y = configFileNode.SafeLoad("y", winPos.y);
                        winPos.width = configFileNode.SafeLoad("width", winPos.width);
                        winPos.height = configFileNode.SafeLoad("height", winPos.height);

                        ConfigNode contracts = configFileNode.GetNode("CONTRACTS");
                        if (contracts != null)
                        {
                            Log.Info("CONTRACTS found");
                            var guids = contracts.GetValuesList("contractGuid");
                            Log.Info("contractGuids found: " + guids.Count);
                            Instance.activeContracts.Clear();
                            //var aContracts = contractParser.getActiveContracts;
                            foreach (var guid in guids)
                            {
                                Log.Info("CONTRACTS, contractGuid: " + guid);
                                Guid contractGuid = new Guid(guid);
                                foreach (var a in contractParser.getActiveContracts)
                                {
                                    if (a.ID == contractGuid)
                                    {
                                        Log.Info("contractGuid found in contractParser.getActiveContracts");
                                        Instance.activeContracts.Add(contractGuid, new Contract(a));
                                        activeContracts[contractGuid].selected = true;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                winPos = new Rect(Screen.width - WhatDoINeed.WIN_WIDTH, Screen.height / 8, WhatDoINeed.WIN_WIDTH, WhatDoINeed.WIN_HEIGHT);
            }
        }
    }
}
using System;
using System.IO;
using System.Collections.Generic;

using UnityEngine;
using SpaceTuxUtility;
using static WhatDoINeed.RegisterToolbar;

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
        internal bool failToWrite = false;

        // Following are saved in a file
        internal float fontSize = 12f;
        internal bool bold = false;
        internal bool initialShowAll = true;
        internal bool showBriefing = false;
        internal float Alpha = 255;
        internal float HideTime = 15;
        internal bool lockPos = false;
        internal bool hideButtons = false;
        internal bool enableClickThrough = true;
        internal string fileName = "";
        internal bool saveToFile = false;
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
            configFileNode.AddValue("fontSize", fontSize);
            configFileNode.AddValue("bold", bold);
            configFileNode.AddValue("showBriefing", showBriefing);
            configFileNode.AddValue("initialShowAll", initialShowAll);
            
            configFileNode.AddValue("Alpha", Alpha);
            configFileNode.AddValue("HideTime", HideTime);
            configFileNode.AddValue("lockPos", lockPos);
            configFileNode.AddValue("hideButtons", hideButtons);
            configFileNode.AddValue("enableClickThrough", enableClickThrough);


            if (fileName != null)
                configFileNode.AddValue("fileName", fileName);
            configFileNode.AddValue("saveToFile", saveToFile);

            configFileNode.AddValue("x", winPos.x);
            configFileNode.AddValue("y", winPos.y);
            configFileNode.AddValue("width", winPos.width);
            configFileNode.AddValue("height", winPos.height);

            //configFileNode.AddValue("x", editorWinPos.x);
            //configFileNode.AddValue("y", editorWinPos.y);
            //configFileNode.AddValue("width", editorWinPos.width);
            //configFileNode.AddValue("height", editorWinPos.height);

            configFile.AddNode(configFileNode);


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
                        fontSize = configFileNode.SafeLoad("fontSize", fontSize);
                        bold = configFileNode.SafeLoad("bold", bold);
                        showBriefing = configFileNode.SafeLoad("showBriefing", showBriefing);
                        initialShowAll = configFileNode.SafeLoad("initialShowAll", initialShowAll);
                        
                        Alpha = configFileNode.SafeLoad("Alpha", Alpha);
                        HideTime = configFileNode.SafeLoad("HideTime", HideTime);
                        lockPos = configFileNode.SafeLoad("lockPos", lockPos);
                        hideButtons = configFileNode.SafeLoad("hideButtons", hideButtons);
                        enableClickThrough = configFileNode.SafeLoad("enableClickThrough", enableClickThrough);


                        fileName = configFileNode.SafeLoad("fileName", fileName);
                        saveToFile = configFileNode.SafeLoad("saveToFile", saveToFile);

                        winPos.x = configFileNode.SafeLoad("x", winPos.x);
                        winPos.y = configFileNode.SafeLoad("y", winPos.y);
                        winPos.width = configFileNode.SafeLoad("width", winPos.width);
                        winPos.height = configFileNode.SafeLoad("height", winPos.height);

                    }
                }
            } else
            {
                winPos = new Rect(Screen.width - Display.WIN_WIDTH,  Screen.height / 8 , Display.WIN_WIDTH, Display.WIN_HEIGHT);
            }
        }
    }
}
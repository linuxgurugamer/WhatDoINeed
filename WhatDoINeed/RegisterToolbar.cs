using UnityEngine;
using ToolbarControl_NS;
using KSP_Log;

namespace WhatDoINeed
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class RegisterToolbar : MonoBehaviour
    {
        internal static Log Log = null;

        static internal bool initted = false;
        void Awake()
        {
            if (Log == null)
#if DEBUG
                //Log = new Log("WhatDoINeed", Log.LEVEL.INFO);
            Log = new Log("WhatDoINeed", Log.LEVEL.ERROR);
#else
                Log = new Log("WhatDoINeed", Log.LEVEL.ERROR);
#endif

            //DontDestroyOnLoad(this);
            Settings.Instance = new Settings();
        }

        void Start()
        {
            ToolbarControl.RegisterMod(WhatDoINeed.MODID, WhatDoINeed.MODNAME);
#if false
            Settings.Instance.kspWindow = new GUIStyle(HighLogic.Skin.window);
            Settings.Instance.kspWindow.active.background = GUISkinCopy.CopyTexture2D(HighLogic.Skin.window.active.background);

            Settings.Instance.kspWindow = new GUIStyle(GUI.skin.window); // GUIStyle(HighLogic.Skin.window);
            Settings.Instance.kspWindow.active.background = GUISkinCopy.CopyTexture2D(GUI.skin.window.active.background);
#endif
        }

        void OnGUI()
        {
            if (!initted)
            {
                initted = true;

                Log.Info("RegisterToolbar, OnGUI initialization");
                Settings.Instance.kspWindow = new GUIStyle(GUI.skin.window); // GUIStyle(HighLogic.Skin.window);

                Settings.Instance.kspWindow.active.textColor = HighLogic.Skin.window.active.textColor;
                //Settings.Instance.textAreaFont.normal.textColor = HighLogic.Skin.window.normal.textColor;



                //Settings.Instance.kspWindow.active.background = GUISkinCopy.CopyTexture2D(HighLogic.Skin.window.active.background);
                //Settings.Instance.kspWindow.normal.background = GUISkinCopy.CopyTexture2D(HighLogic.Skin.window.normal.background);

                //Settings.Instance.kspWindow = new GUIStyle(GUI.skin.window); // GUIStyle(HighLogic.Skin.window);
                //Settings.Instance.kspWindow.active.background = GUISkinCopy.CopyTexture2D(GUI.skin.window.active.background);

                Settings.Instance.largeDisplayFont = new GUIStyle(GUI.skin.scrollView); // 
                Settings.Instance.largeDisplayFont.normal.textColor = Color.yellow;

                Settings.Instance.displayFont = new GUIStyle(GUI.skin.scrollView); // GUIStyle(GUI.skin.label);
                Settings.Instance.displayFont.normal.textColor = Color.yellow;
                //Settings.Instance.labelFont = new GUIStyle(GUI.skin.scrollView); // GUIStyle(GUI.skin.label);

                Settings.Instance.textAreaFont = new GUIStyle(GUI.skin.scrollView); // GUIStyle(GUI.skin.textArea);
                Settings.Instance.textAreaSmallFont = new GUIStyle(GUI.skin.scrollView); // GUIStyle(GUI.skin.textArea);

                Settings.Instance.textAreaWordWrap = new GUIStyle(GUI.skin.scrollView); // GUIStyle(GUI.skin.textArea);
                Settings.Instance.textAreaWordWrap.wordWrap = true;

                Settings.Instance.myStyle = new GUIStyle();
                Settings.Instance.styleOff = new Texture2D(2, 2);
                Settings.Instance.styleOn = new Texture2D(2, 2);

                Settings.Instance.resizeButton = GetToggleButtonStyle("resize", 20, 20, true);

                Settings.Instance.textFieldStyleRed = new GUIStyle(GUI.skin.scrollView) // GUIStyle(GUI.skin.textField)
                {
                    focused = { textColor = Color.red },
                    hover= { textColor = Color.red },
                    normal = { textColor = Color.red },
                    alignment = TextAnchor.MiddleLeft,
                };
                Settings.Instance.textFieldStyleNormal = new GUIStyle(GUI.skin.scrollView) // GUIStyle(GUI.skin.textField)
                {
                    alignment = TextAnchor.MiddleLeft,
                };
                Settings.Instance.scrollViewStyle = new GUIStyle(GUI.skin.scrollView);

                WhatDoINeed.SetFontSizes(Settings.Instance.fontSize, Settings.Instance.bold);
                //Display.SetAlpha(Settings.Instance.Alpha);
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
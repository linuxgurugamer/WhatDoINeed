
using ClickThroughFix;
using SpaceTuxUtility;
using UnityEngine;
using static WhatDoINeed.WhatDoINeed;

namespace WhatDoINeed
{

    public class SettingsWindow : MonoBehaviour
    {
        private static GameObject go;
        private static SettingsWindow instance;
        private int settingsWinId;

        Rect lastWinPosSize = new Rect();
        internal static bool IsVisible { get { return Settings.Instance.lastSettingsWindowVisibleStatus; } }
        internal static bool Exists {  get {  return instance != null; } }  
        internal static SettingsWindow GetInstance()
        {
            if (go == null)
            {
                go = new GameObject("SettingsWindow");
                instance = go.AddComponent<SettingsWindow>();
            }
            Settings.Instance.lastSettingsWindowVisibleStatus = true;
            return instance;
        }
        internal void DestroyWin(bool closeAll = false)
        {
            if (!closeAll)
                Settings.Instance.lastSettingsWindowVisibleStatus = false;
            Destroy(this);
        }
        void OnDestroy()
        {
            GameEvents.onGameSceneLoadRequested.Remove(onGameSceneLoadRequested);
            instance = null;
            go = null;
        }

        void Awake()
        {
            instance = this;
            settingsWinId = WindowHelper.NextWindowId("WDIN-SettingsWindow");

        }

        void Start()
        {
            GameEvents.onGameSceneLoadRequested.Add(onGameSceneLoadRequested);
            lastFontSizeSliderValue = oFontSize = Settings.Instance.fontSize;

            Settings.Instance.settingsFontSizeDisplayFont.fontSize = (int)Settings.Instance.fontSize;
        }

        void onGameSceneLoadRequested(GameScenes scene)
        {
            if (scene != GameScenes.EDITOR)
                DestroyWin();
        }

        void FixedUpdate()
        {
            if (WhatDoINeed.Instance != null && oFontDisplaySize != null)
                WhatDoINeed.Instance.SetFontAlpha(ref oFontDisplaySize);
        }
        void OnGUI()
        {
            if (!Settings.Instance.lastSelectWindowVisibleStatus && !Settings.Instance.lastPartsWindowVisibleStatus && !WhatDoINeed.Instance.winOpen)
                DestroyWin();
            Rect tmpPos;
            if (!oFontInitted)
            {
                oFontDisplaySize = new GUIStyle(Settings.Instance.settingsDisplayFont);
                oFontInitted = true;
            }
            SetAlpha(Settings.Instance.Alpha);

            tmpPos = ClickThruBlocker.GUILayoutWindow(settingsWinId, Settings.Instance.settingsWinPos, ShowSettingsWindow, "What Do I Need? Settings", Settings.Instance.kspWindow);
            if (!Settings.Instance.lockPos)
                Settings.Instance.settingsWinPos = tmpPos;

            if (Settings.Instance.settingsWinPos != lastWinPosSize)
            {
                lastWinPosSize = Settings.Instance.settingsWinPos = tmpPos;
                Settings.Instance.SaveData();
            }
        }

        void SectionLabel(string str)
        {

            GUILayout.Label("___________ " + str + " ___________", Settings.Instance.settingsDisplayFont);

        }
        const float sliderWidth = 200; // * Settings.Instance.fontSize / Settings.DEFAULT_FONT_SIZE;
        GUIStyle oFontDisplaySize = null;
        bool oFontInitted = false;
        const int verticalSpacer = 5;
        float oFontSize, lastFontSizeSliderValue;

        void ShowSettingsWindow(int id)
        {
            WhatDoINeed.Instance.SetButtonAlpha();

            var oAlpha = Settings.Instance.Alpha;

            GUILayout.Space(10);

            oFontSize = Settings.Instance.fontSize;
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("Font Size (" + (int)Settings.Instance.fontSize + "): ", Settings.Instance.settingsFontSizeDisplayFont);
                Settings.Instance.fontSize = GUILayout.HorizontalSlider(Settings.Instance.fontSize, 8f, 30f, GUILayout.Width(sliderWidth));
                GUILayout.FlexibleSpace();
            }
            SetFontSizes(Settings.Instance.fontSize, Settings.Instance.bold);

            GUILayout.Space(verticalSpacer);
            Settings.Instance.showBriefing = GUILayout.Toggle(Settings.Instance.showBriefing, "Display Briefing", Settings.Instance.settingsToggleDisplayFont);
            GUILayout.Space(verticalSpacer);
            Settings.Instance.bold = GUILayout.Toggle(Settings.Instance.bold, "Bold", Settings.Instance.settingsToggleDisplayFont);
            GUILayout.Space(verticalSpacer);
            Settings.Instance.lockPos = GUILayout.Toggle(Settings.Instance.lockPos, "Lock Position and size", Settings.Instance.settingsToggleDisplayFont);
            GUILayout.Space(verticalSpacer);
            Settings.Instance.enableClickThrough = GUILayout.Toggle(Settings.Instance.enableClickThrough, "Allow click-through", Settings.Instance.settingsToggleDisplayFont);
            GUILayout.Space(verticalSpacer);
            Settings.Instance.initialShowAll = GUILayout.Toggle(Settings.Instance.initialShowAll, "Show all active contracts upon entry", Settings.Instance.settingsToggleDisplayFont);
            GUILayout.Space(verticalSpacer);
            Settings.Instance.reopenIfLastOpen = GUILayout.Toggle(Settings.Instance.reopenIfLastOpen, "Restore last open state when entering editor", Settings.Instance.settingsToggleDisplayFont);
            GUILayout.Space(verticalSpacer);
            SectionLabel("Part Spawning");
            Settings.Instance.allowPartSpawning = GUILayout.Toggle(Settings.Instance.allowPartSpawning, "Allow Part Spawning by clicking on part names", Settings.Instance.settingsToggleDisplayFont);
            if (Settings.Instance.allowPartSpawning)
            {
                Settings.Instance.HideWindowsWhenSpawning = GUILayout.Toggle(Settings.Instance.HideWindowsWhenSpawning, "Hide windows when spawning a part", Settings.Instance.settingsToggleDisplayFont);
                GUILayout.Space(verticalSpacer);
            }
            GUILayout.Space(verticalSpacer);

            SectionLabel("Pre-Launch Checks");
            Settings.Instance.checkForMissingBeforeLaunch = GUILayout.Toggle(Settings.Instance.checkForMissingBeforeLaunch, "Check For Missing Experiments Before Launch", Settings.Instance.settingsToggleDisplayFont);
            GUILayout.Space(verticalSpacer);
            Settings.Instance.onlyCheckSelectedContracts = GUILayout.Toggle(Settings.Instance.onlyCheckSelectedContracts, "Only Check Selected Contracts", Settings.Instance.settingsToggleDisplayFont);
            GUILayout.Label("________________________________", Settings.Instance.settingsDisplayFont);

            GUILayout.Space(verticalSpacer);
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("Spawn Hide Time (" + Settings.Instance.SpawnHideTime.ToString("F1") + "s): ", Settings.Instance.settingsDisplayFont);
                Settings.Instance.SpawnHideTime = GUILayout.HorizontalSlider(Settings.Instance.SpawnHideTime, 0f, 15, GUILayout.Width(sliderWidth));
                GUILayout.FlexibleSpace();
            }
            GUILayout.Space(verticalSpacer);



            //using (new GUILayout.HorizontalScope())
            //{
            //     Settings.Instance.ShowModName = GUILayout.Toggle(Settings.Instance.ShowModName, "Show Mod Names with part");
            //}
            GUILayout.Space(verticalSpacer);
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("Transparency:", Settings.Instance.settingsDisplayFont);
                Settings.Instance.Alpha = GUILayout.HorizontalSlider(Settings.Instance.Alpha, 0f, 255f, GUILayout.Width(sliderWidth));
                GUILayout.FlexibleSpace();
            }
            GUILayout.Space(verticalSpacer);
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("Hide Time (" + Settings.Instance.HideTime.ToString("F1") + "s): ", Settings.Instance.settingsDisplayFont);
                Settings.Instance.HideTime = GUILayout.HorizontalSlider(Settings.Instance.HideTime, 1f, 30, GUILayout.Width(sliderWidth));
                GUILayout.FlexibleSpace();
            }
            if (oAlpha != Settings.Instance.Alpha)
            {
                SetAlpha(Settings.Instance.Alpha);
            }
            GUILayout.Space(verticalSpacer);
            Settings.Instance.debugMode = GUILayout.Toggle(Settings.Instance.debugMode, "Debug mode", Settings.Instance.settingsToggleDisplayFont);

            GUILayout.FlexibleSpace();
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Close Settings", Settings.Instance.buttonFont, GUILayout.Width(110)))
                {
                    Settings.Instance.SaveData();
                    //go = null;
                    DestroyWin();
                }
                GUILayout.FlexibleSpace();
            }
            if (!Settings.Instance.lockPos)
            {
                if (GUI.RepeatButton(new Rect(Settings.Instance.settingsWinPos.width - 23f, Settings.Instance.settingsWinPos.height - 24f, 24, 24), "", Settings.Instance.resizeButton))
                {
                    resizingWindow = true;
                }
                Utility.ResizeWindow(ref resizingWindow, ref Settings.Instance.settingsWinPos, Settings.SETTINGS_WINDOW_WIDTH, Settings.SETTINGS_WINDOW_HEIGHT);
                if (!Settings.Instance.lockPos)
                    GUI.DragWindow();
            }
        }
        bool resizingWindow = false;

    }
}

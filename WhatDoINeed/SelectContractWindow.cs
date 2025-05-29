using ClickThroughFix;
using SpaceTuxUtility;
using UnityEngine;
using UnityEngine.UI;
using static WhatDoINeed.RegisterToolbar;
using static WhatDoINeed.WhatDoINeed;

namespace WhatDoINeed
{
    public class SelectWindow : MonoBehaviour
    {
        private static GameObject go;
        private static SelectWindow instance;
        Rect lastWinPosSize = new Rect();
        int selWinId;
        private ScrollRect myScrollRect;


        internal static bool IsVisible { get { return Settings.Instance.lastSelectWindowVisibleStatus; } }

        internal static bool Exists { get { return instance != null; } }

        internal static SelectWindow GetInstance()
        {
            if (go == null)
            {
                go = new GameObject("SelectWindow");
                instance = go.AddComponent<SelectWindow>();
            }
            Settings.Instance.lastSelectWindowVisibleStatus = true;
            return instance;
        }

        internal void DestroyWin(bool closeAll = false)
        {
            if (!closeAll)
                Settings.Instance.lastSelectWindowVisibleStatus = false;

            Destroy(this);
        }
        void OnDestroy()
        {
            Log.Info("SelectContract.Destroy, OnDestroy");

            GameEvents.onGameSceneLoadRequested.Remove(onGameSceneLoadRequested);
            go = null;
            instance = null;
        }

        void Awake()
        {
            instance = this;
        }
        void Start()
        {
            selWinId = WindowHelper.NextWindowId("WDIN-SelectWindow");
            GameEvents.onGameSceneLoadRequested.Add(onGameSceneLoadRequested);

            myScrollRect = GetComponent<ScrollRect>();
            if (myScrollRect != null)
            {
                Log.Info("SelectContractWindow, ScrollRect found in child objects!");
            }

        }

        void onGameSceneLoadRequested(GameScenes scene)
        {
            if (scene != GameScenes.EDITOR)
                DestroyWin(true);
        }


        void OnGUI()
        {
            Rect tmpPos;
            SetAlpha(Settings.Instance.Alpha);

            WhatDoINeed.Instance.SetFontAlpha(ref Settings.Instance.kspWindow);

            if (Settings.Instance.enableClickThrough && Time.realtimeSinceStartup <= quickHideEnd)
                tmpPos = GUILayout.Window(selWinId, Settings.Instance.selWinPos, SelectContractWindowDisplay, "What Do I Need? - Contract Selection", Settings.Instance.kspWindow);
            else
                tmpPos = ClickThruBlocker.GUILayoutWindow(selWinId, Settings.Instance.selWinPos, SelectContractWindowDisplay, "What Do I Need? - Contract Selection", Settings.Instance.kspWindow);

            if (!Settings.Instance.lockPos)
                Settings.Instance.selWinPos = tmpPos;

            if (Settings.Instance.selWinPos != lastWinPosSize)
            {
                lastWinPosSize = Settings.Instance.settingsWinPos;
                Settings.Instance.SaveData();
            }
        }

        Vector2 scrollPos;

        bool scrollBarsVisible = false;


        void SelectContractWindowDisplay(int id)
        {
            float contentHeight = 0;
            Log.Info("SelectContractWindowDisplay, numContracts: " + Repository.Contracts.Count);
            WhatDoINeed.Instance.SetButtonAlpha();

            GUIStyle toggleStyle = new GUIStyle(GUI.skin.toggle);
            toggleStyle.alignment = TextAnchor.UpperLeft;
            using (new GUILayout.VerticalScope())
            {
                const string firstLabel = "Click on contract to enable/disable it";
                GUILayout.Label(firstLabel, Settings.Instance.largeDisplayFont);
                GUILayout.Space(15);
                contentHeight = 15f + Settings.Instance.largeDisplayFont.CalcHeight(new GUIContent(firstLabel), 1000);

                scrollPos = GUILayout.BeginScrollView(scrollPos, Settings.Instance.scrollViewStyle, GUILayout.Height(Settings.Instance.selWinPos.height - 100));

                foreach (var contract in Repository.Contracts)
                {
                    using (new GUILayout.HorizontalScope())
                    {
                        string str = Utility.GetIndentedText(contract.Value.contractContainer.Title, Settings.Instance.selWinPos.width - 40 - (scrollBarsVisible ? 30 : 0), Settings.Instance.displayFont, "      ");
                        if (GUILayout.Button(largeBullet + " " + str, (contract.Value.selected ? Settings.Instance.displayFontGreen : Settings.Instance.displayFont)))
                            contract.Value.selected = !contract.Value.selected;
                        GUILayout.FlexibleSpace();
                        contentHeight += Settings.Instance.displayFontGreen.CalcHeight(new GUIContent(str), 1000) + 10;
                    }
                    GUILayout.Space(10);
                }
                GUILayout.EndScrollView();
                scrollBarsVisible = (contentHeight > Settings.Instance.selWinPos.height - 100);
                GUILayout.FlexibleSpace();
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Select All", Settings.Instance.buttonFont, GUILayout.Width(90)))
                    {
                        foreach (var a in Repository.Contracts)
                            a.Value.selected = true;
                    }
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Clear All", Settings.Instance.buttonFont, GUILayout.Width(90)))
                    {
                        foreach (var a in Repository.Contracts)
                            a.Value.selected = false;
                    }
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Close", Settings.Instance.buttonFont, GUILayout.Width(90)))
                    {
                        DestroyWin();
                    }
                    GUILayout.FlexibleSpace();
                }
            }
            WhatDoINeed.Instance.PreprocessContracts(3);

            if (!Settings.Instance.lockPos)
            {
                if (GUI.RepeatButton(new Rect(Settings.Instance.selWinPos.width - 23f, Settings.Instance.selWinPos.height - 24f, 24, 24), "", Settings.Instance.resizeButton))
                {
                    WhatDoINeed.Instance.resizingSelWindow = true;
                }
                Utility.ResizeWindow(ref WhatDoINeed.Instance.resizingSelWindow, ref Settings.Instance.selWinPos, Settings.SEL_WINDOW_WIDTH, Settings.SEL_WINDOW_HEIGHT);
                GUI.DragWindow();
            }

        }
    }
}

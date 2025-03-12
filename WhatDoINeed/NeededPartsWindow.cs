using ClickThroughFix;
using LibNoise.Models;
using SpaceTuxUtility;
using UnityEngine;
using UnityEngine.UI;
using static WhatDoINeed.RegisterToolbar;
using static WhatDoINeed.WhatDoINeed;

namespace WhatDoINeed
{
    internal class NeededPartsWindow : MonoBehaviour
    {
        private static GameObject go;
        private static NeededPartsWindow instance;
        private int settingsWinId;

        Rect lastWinPosSize = new Rect();
        string module;
        bool resizingWindow = false;
        bool showMounted = false;
        private ScrollRect myScrollRect;

        internal static bool IsVisible { get { return Settings.Instance.lastPartsWindowVisibleStatus; } }
        internal static bool Exists { get { return instance != null; } }

        internal static NeededPartsWindow GetInstance(string module)
        {
            if (go == null)
            {
                go = new GameObject("PartsWindow");
                instance = go.AddComponent<NeededPartsWindow>();
            }
            NeededPartsWindow.instance.module = module;
            Settings.Instance.lastPartsWindowVisibleStatus = true;
            Log.Info("NeededPartsWindow, module: " + module);
            return instance;
        }
        internal static NeededPartsWindow GetInstance()
        {
            return instance;
        }

        void Awake()
        {
            instance = this;
            settingsWinId = WindowHelper.NextWindowId("WDIN-PartsWindow");

        }

        internal void DestroyWin(bool closeAll = false)
        {
            if (!closeAll)
                Settings.Instance.lastPartsWindowVisibleStatus = false;

            Destroy(this);
        }

        void OnDestroy()
        {
            GameEvents.onGameSceneLoadRequested.Remove(onGameSceneLoadRequested);
            instance = null;
            go = null;
        }

        void Start()
        {
            GameEvents.onGameSceneLoadRequested.Add(onGameSceneLoadRequested);
        }
        void onGameSceneLoadRequested(GameScenes scene)
        {
            if (scene != GameScenes.EDITOR)
                DestroyWin();
        }
        void OnGUI()
        {
            Rect tmpPos;

            SetAlpha(Settings.Instance.Alpha);
            WhatDoINeed.Instance.SetFontAlpha(ref Settings.Instance.kspWindow);

            if (Time.realtimeSinceStartup > quickHideEnd || !Settings.Instance.enableClickThrough)
                tmpPos = GUILayout.Window(settingsWinId, Settings.Instance.partsWinPos, ShowPartsWindow, "What Do I Need? - " + module + " Needed Parts List", Settings.Instance.kspWindow);
            else
                tmpPos = ClickThruBlocker.GUILayoutWindow(settingsWinId, Settings.Instance.partsWinPos, ShowPartsWindow, "What Do I Need? - " + module + " Needed Parts List", Settings.Instance.kspWindow);

            if (!Settings.Instance.lockPos)
                Settings.Instance.partsWinPos = tmpPos;

            if (Settings.Instance.partsWinPos != lastWinPosSize)
            {
                lastWinPosSize = Settings.Instance.partsWinPos = tmpPos;
                Settings.Instance.SaveData();
            }
        }

        Vector2 contractPos;
        bool scrollBarsVisible = false;

        void ShowPartsWindow(int id)
        {
            float contentHeight = 0f;

            WhatDoINeed.Instance.SetButtonAlpha();

            GUILayout.Space(10);
            contractPos = GUILayout.BeginScrollView(contractPos, Settings.Instance.scrollViewStyle, GUILayout.Height(Settings.Instance.partsWinPos.height - 80) );
            myScrollRect = GetComponentInChildren<ScrollRect>();
            if (myScrollRect != null)
            {
                Log.Info("SelectContractWindow, ScrollRect found in objects!");
            }
            else
            {
                myScrollRect = GetComponentInChildren<ScrollRect>();
                if (myScrollRect != null)
                    Log.Info("SelectContractWindow, ScrollRect found in child objects!");

            }

            if (Repository.moduleInformation.ContainsKey(module))
            {
                for (int i = 0; i < Repository.moduleInformation[module].partsWithModule.Count; i++)
                {
                    AvailablePart ap = Repository.moduleInformation[module].partsWithModule[i];
                    if (ResearchAndDevelopment.PartModelPurchased(ap) || ResearchAndDevelopment.IsExperimentalPart(ap))
                    {
                        using (new GUILayout.HorizontalScope())
                        {
                            string str;
                            string title = Utility.CleanPartTitle(ap.title);
                            if (!Repository.partInfoList.ContainsKey(ap.name))
                            {
                                Log.Error("ShowPartsWindows, partInfoList missing: " + ap.name);
                            }
                            else
                            {
                                if (Repository.partInfoList[ap.name].numAvailable == 0)
                                {
                                    str = "  " + title;
                                    GUILayout.Space(30);
                                    str = title;
                                }
                                else
                                {
                                    GUILayout.Label(new GUIContent(checkMark, ""), Settings.Instance.displayFontCyan, GUILayout.Width(20));
                                    GUILayout.Space(10);
                                    str = checkMark + " " + title;
                                    str = title;
                                }
                                if (!showMounted || Repository.partInfoList[ap.name].numAvailable > 0)
                                {
                                    str = Utility.GetIndentedText(str, Settings.Instance.partsWinPos.width - 20 - (scrollBarsVisible ? 30 : 0), Settings.Instance.displayFont, "             ");
                                    if (Settings.Instance.allowPartSpawning)
                                    {
                                        if (GUILayout.Button(str, Settings.Instance.displayFontCyan))
                                        {
                                            if (EditorLogic.SelectedPart != null)
                                                EditorLogic.DeletePart(EditorLogic.SelectedPart);

                                            EditorLogic.fetch.SpawnPart(ap);
                                            if (Settings.Instance.HideWindowsWhenSpawning)
                                            {
                                                quickHideEnd = Time.realtimeSinceStartup + Settings.Instance.SpawnHideTime;
                                                FadeStatus = Fade.decreasing;
                                            }
                                        }
                                    }
                                    else
                                        GUILayout.Label(str, Settings.Instance.displayFont);

                                    contentHeight += Settings.Instance.displayFontGreen.CalcHeight(new GUIContent(str), 1000) + 2;
                                    GUILayout.Space(2);

                                }
                            }
                        }
                    }
                }
            }
            GUILayout.EndScrollView();
            scrollBarsVisible = (contentHeight > Settings.Instance.partsWinPos.height - 100);
            GUILayout.FlexibleSpace();

            using (new GUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                if (GUILayout.Button(showMounted ? "Show All Parts" : "Show Mounted Parts", Settings.Instance.buttonFont, GUILayout.Width(140)))
                {
                    showMounted = !showMounted;
                }
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Close", Settings.Instance.buttonFont, GUILayout.Width(140)))
                {
                    Settings.Instance.SaveData();
                    DestroyWin();
                }
                GUILayout.FlexibleSpace();

            }
            if (!Settings.Instance.lockPos)
            {
                if (GUI.RepeatButton(new Rect(Settings.Instance.partsWinPos.width - 23f, Settings.Instance.partsWinPos.height - 24f, 24, 24), "", Settings.Instance.resizeButton))
                {
                    resizingWindow = true;
                }
                Utility.ResizeWindow(ref resizingWindow, ref Settings.Instance.partsWinPos, Settings.SETTINGS_WINDOW_WIDTH, Settings.SETTINGS_WINDOW_HEIGHT);
                if (!Settings.Instance.lockPos)
                    GUI.DragWindow();
            }

        }

    }
}

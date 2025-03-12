using ClickThroughFix;
using SpaceTuxUtility;
using System;
using UnityEngine;
using static WhatDoINeed.RegisterToolbar;


namespace WhatDoINeed
{

    public partial class WhatDoINeed
    {
        public void OnLaunchButtonInput()
        {
            if (Settings.Instance.checkForMissingBeforeLaunch)
            {
                numSelectedcontracts = 0;
                foreach (var contract in Repository.Contracts)
                {
                    if (contract.Value.selected || !Settings.Instance.onlyCheckSelectedContracts)
                    {
                        numSelectedcontracts++;
                    }
                }
                //Log.Info("OnLaunchButtonInput 1, numSelectedContracts: " + numSelectedcontracts + ", numUnfillableContracts: " + numUnfillableContracts + ", numFillableContracts: " + numFillableContracts);

                PreprocessContracts(1);
                Log.Info("OnLaunchButtonInput 2, numSelectedContracts: " + numSelectedcontracts + ", numUnfillableContracts: " + numUnfillableContracts + ", numFillableContracts: " + numFillableContracts);

                if (numSelectedcontracts > 0 && numUnfillableContracts > 0)
                {
                    //Log.Info("OnLaunchButtonInput, before GetInstance(), numSelectedContracts: " + numSelectedcontracts + ", numUnfillableContracts: " + numUnfillableContracts + ", numFillablecontracts: " + numFillableContracts);
                    LaunchButtonInput.GetInstance();
                }
                else
                {
                    //Log.Info("OnLaunchButtonInput 4");
                    ButtonManager.BtnManager.InvokeNextDelegate(btnId, "What-Do-I-Need-next");
                }
            }
            else
            {
                //Log.Info("OnLaunchButtonInput 3");
                ButtonManager.BtnManager.InvokeNextDelegate(btnId, "What-Do-I-Need-next");
            }

        }

        public class LaunchButtonInput : MonoBehaviour
        {
            private static GameObject go;
            private static LaunchButtonInput instance;
            int launchWinId;

            public const float SEL_WINDOW_WIDTH = 400;
            public const float SEL_WINDOW_HEIGHT = 300;

            Rect launchWinPos = new Rect(Screen.width / 2 - SEL_WINDOW_WIDTH / 2, Screen.height / 2 - SEL_WINDOW_HEIGHT / 2, SEL_WINDOW_WIDTH, SEL_WINDOW_HEIGHT);


            internal static LaunchButtonInput GetInstance()
            {
                if (go == null)
                {
                    go = new GameObject("LaunchButtonInput");
                    instance = go.AddComponent<LaunchButtonInput>();
                }
                return instance;
            }

            void OnDestroy()
            {
                GameEvents.onGameSceneLoadRequested.Remove(onGameSceneLoadRequested);

                go = null;
            }

            void Awake()
            {
                instance = this;
            }

            void Start()
            {
                launchWinId = WindowHelper.NextWindowId("WDIN-LaunchButtonInput");
                GameEvents.onGameSceneLoadRequested.Add(onGameSceneLoadRequested);
                Log.Info("OnLaunchButtonInput 4, LaunchButtonInput.Start, numSelectedContracts: " + numSelectedcontracts + ", numUnfillableContracts: " + numUnfillableContracts + ", numFillablecontracts: " + numFillableContracts);
                WhatDoINeed.Instance.PreprocessContracts(2);
                Log.Info("OnLaunchButtonInput 5, LaunchButtonInput.Start, numSelectedContracts: " + numSelectedcontracts + ", numUnfillableContracts: " + numUnfillableContracts + ", numFillablecontracts: " + numFillableContracts);

            }
            void onGameSceneLoadRequested(GameScenes scene)
            {
                if (scene != GameScenes.EDITOR)
                    Destroy(this);
            }

            void OnGUI()
            {
                SetAlpha(Settings.Instance.Alpha);

                GUI.skin = HighLogic.Skin;
                launchWinPos = ClickThruBlocker.GUILayoutWindow(launchWinId, launchWinPos, LaunchInputWindow, "What Do I Need? - Launch Check", Settings.Instance.kspWindow);
            }

            string IsArePlural(int x, bool plural = false)
            {
                if (!plural)
                    return ((x == 1) ? "is " : "are ");
                return (x == 1) ? "" : "s";
            }

            string IsArePlural(int x, string str, string str2 = " ")
            {
                return "There " + IsArePlural(x) + x + str + IsArePlural(x, true) + str2;

            }

            void LaunchInputWindow(int id)
            {
                GUI.BringWindowToFront(id);

                using (new GUILayout.VerticalScope())
                {
                    GUILayout.Space(20);
                    using (new GUILayout.HorizontalScope())
                        GUILayout.Label("You have active contracts that have experiments and/or requirements that can't be run due to missing parts.", Settings.Instance.launchCheckDisplayFont);
                    GUILayout.Space(20);

                    using (new GUILayout.HorizontalScope())
                        GUILayout.Label(IsArePlural(numSelectedcontracts, " selected contract") + ".", Settings.Instance.launchCheckDisplayFont);
                    using (new GUILayout.HorizontalScope())
                        GUILayout.Label(IsArePlural(numFillableContracts, " contract", " that can be fulfilled") + ".", Settings.Instance.launchCheckDisplayFont);
                    GUILayout.Space(10);
                    using (new GUILayout.HorizontalScope())
                        GUILayout.Label(IsArePlural(numUnfillableContracts, " contract", " that cannot be fulfilled") + "!" , Settings.Instance.launchCheckDisplayFontRed);
                    GUILayout.Space(20);
                    Settings.Instance.launchCheckDisplayFont.fontStyle = FontStyle.Normal;

                    using (new GUILayout.HorizontalScope())
                        GUILayout.Label("Are you sure you want to launch?", Settings.Instance.launchCheckDisplayFont);
                    GUILayout.Label("");
                    GUILayout.FlexibleSpace();
                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("Launch", Settings.Instance.buttonFont, GUILayout.Width(90)))
                        {
                            ButtonManager.BtnManager.InvokeNextDelegate(WhatDoINeed.Instance.btnId, "What-Do-I-Need-next");
                            Destroy(this);
                        }
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("Cancel", Settings.Instance.buttonFont, GUILayout.Width(90)))
                        {
                            Destroy(this);
                        }
                        GUILayout.FlexibleSpace();
                    }

                }
            }

        }
    }
}

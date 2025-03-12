using System;
using System.Collections.Generic;
using UnityEngine;
using static WhatDoINeed.RegisterToolbar;

namespace WhatDoINeed
{
    public partial class WhatDoINeed
    {

        void ExpandCollapseAll(bool value)
        {
            foreach (KeyValuePair<Guid, ContractWrapper> contract in Repository.Contracts)
            {
                var contractId = "repository-" + contract.Key.ToString();
                if (openClosed.ContainsKey(contractId))
                {
                    openClosed[contractId] = value;
                    Log.Info("ExpandCollapseAll, contract: " + contractId + " set to " + value);
                }
            }
        }

        void ToggleBriefing()
        {
            Settings.Instance.showBriefing = !Settings.Instance.showBriefing;
        }

        /// <summary>
        /// Display the contracts and requirements
        /// </summary>
        /// <param name="id"></param>
        void ContractWindowDisplay(int id)
        {
            SetButtonAlpha();

            ShowProcessedContracts();

            GUILayout.FlexibleSpace();
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Expand All", Settings.Instance.buttonFont, GUILayout.Width(120)))
                {
                    ExpandCollapseAll(true);
                }
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Collapse All", Settings.Instance.buttonFont, GUILayout.Width(120)))
                {
                    ExpandCollapseAll(false);
                }
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(Settings.Instance.showBriefing ? "Hide Briefing" : "Show Briefing", Settings.Instance.buttonFont, GUILayout.Width(120)))
                {
                    ToggleBriefing();
                }

                GUILayout.FlexibleSpace();
                GUILayout.Space(13);
            }
            using (new GUILayout.HorizontalScope())
            {
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button((SelectWindow.Exists && SelectWindow.IsVisible) ? "Close Contract Selection" : "Contract Selection", Settings.Instance.buttonFont, GUILayout.Width(180)))
                    {
                        if (SelectWindow.Exists && SelectWindow.IsVisible)
                        {
                            SelectWindow.GetInstance().DestroyWin();
                        }
                        else
                        {
                            if (!SelectWindow.Exists)
                                SelectWindow.GetInstance();
                        }
                    }
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Close", Settings.Instance.buttonFont, GUILayout.Width(90)))
                    {
                        GUIToggle();
                    }
                }
                GUILayout.FlexibleSpace();

                if (GUILayout.Button((SettingsWindow.Exists && SettingsWindow.IsVisible) ? "Close Settings" : "Settings", Settings.Instance.buttonFont, GUILayout.Width(110)) ||
                    GUI.Button(new Rect(52, 2, 24, 24), "S", Settings.Instance.buttonFont))
                {
                    if (SettingsWindow.Exists && SettingsWindow.IsVisible)
                    {
                        SettingsWindow.GetInstance().DestroyWin();

                    }
                    else
                    {
                        if (!SettingsWindow.Exists)
                            SettingsWindow.GetInstance();
                    }
                }
                GUILayout.FlexibleSpace();
                GUILayout.Space(20);
            }
            if (GUI.Button(new Rect(26, 2, 24, 24), "L", Settings.Instance.buttonFont))
            {
                Settings.Instance.lockPos = !Settings.Instance.lockPos;
            }

            if (GUI.Button(new Rect(Settings.Instance.winPos.width - 24 - 2, 2, 24, 24), "X", Settings.Instance.buttonFont))
            {
                GUIToggle();
            }
            if (GUI.Button(new Rect(Settings.Instance.winPos.width - 48 - 4, 2, 24, 24), "H", Settings.Instance.buttonFont))
            {
                quickHideEnd = Time.realtimeSinceStartup + Settings.Instance.HideTime;
            }
            if (GUI.Button(new Rect(Settings.Instance.winPos.width - 72 - 6, 2, 24, 24), "?", Settings.Instance.buttonFont))
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
            Utility.ResizeWindow(ref resizingWindow, ref Settings.Instance.winPos, Settings.WINDOW_WIDTH, Settings.WINDOW_HEIGHT);
            if (!Settings.Instance.lockPos)
                GUI.DragWindow();
        }
    }
}

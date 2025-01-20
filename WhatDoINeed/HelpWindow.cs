
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using ClickThroughFix;
using ToolbarControl_NS;

using static WhatDoINeed.RegisterToolbar;

namespace WhatDoINeed
{
    //[KSPAddon(KSPAddon.Startup.SpaceCentre, true)]
    public class HelpWindowClass : MonoBehaviour
    {
        internal static GUIStyle windowStyle = null;

        Rect helpWindow;
        int introWindowId;
        int MAIN_WIDTH = Screen.height * 3 / 4;
        int MAIN_HEIGHT = 400;
        internal static int automoved = 0;

        GUIStyle areaStyle;

        private void Awake()
        {
            introWindowId = GUIUtility.GetControlID(FocusType.Passive);
            DontDestroyOnLoad(this);
        }

        private void Start()
        {
            MAIN_WIDTH = Screen.width / 2;
            MAIN_HEIGHT = Screen.height * 3 / 4;

            helpWindow = new Rect((Screen.width - MAIN_WIDTH) / 2, (Screen.height - MAIN_HEIGHT) / 2, MAIN_WIDTH, MAIN_HEIGHT);
            areaStyle = new GUIStyle(HighLogic.Skin.textArea);
            areaStyle.richText = true;
        }

        public void OnGUI()
        {
            if (windowStyle == null)
            {
                GUI.color = Color.grey;
                windowStyle = new GUIStyle(HighLogic.Skin.window);
                windowStyle.active.background = windowStyle.normal.background;
                Texture2D tex = windowStyle.normal.background;
                var pixels = tex.GetPixels32();

                for (int i = 0; i < pixels.Length; ++i)
                    pixels[i].a = 255;

                tex.SetPixels32(pixels); tex.Apply();
                windowStyle.active.background =
                windowStyle.focused.background =
                windowStyle.normal.background = tex;
            }

            helpWindow = ClickThruBlocker.GUILayoutWindow(introWindowId, helpWindow, HelpWindow, "'What Do I Need?'  Help Window", windowStyle);
            if (automoved < 2)
            {
                helpWindow.x = (Screen.width - helpWindow.width) / 2;
                helpWindow.y = (Screen.height - helpWindow.height) / 2;

                automoved++;
            }
        }

        const string IntroPath = "WhatDoINeed/PluginData/Help.txt";


        private bool Load(string fileName)
        {
            // Handle any problems that might arise when reading the text
            try
            {
                string line;
                // Create a new StreamReader, tell it which file to read and what encoding the file
                // was saved as
                StreamReader theReader = new StreamReader(fileName, Encoding.Default);
                // Immediately clean up the reader after this block of code is done.
                // You generally use the "using" statement for potentially memory-intensive objects
                // instead of relying on garbage collection.
                // (Do not confuse this with the using directive for namespace at the 
                // beginning of a class!)
                using (theReader)
                {
                    line = theReader.ReadLine();
                    if (line != null)
                    {
                        // While there's lines left in the text file, do this:
                        do
                        {
                            ProcessLine(line);
                            line = theReader.ReadLine();
                        }
                        while (line != null);
                    }
                    // Done reading, close the reader and return true to broadcast success    
                    theReader.Close();
                    return true;
                }

            }
            // If anything broke in the try block, we throw an exception with information
            // on what didn't work
            catch (Exception e)
            {
                Log.Error("Load error: " + e.Message);
                return false;
            }
        }


        List<string> lines = new List<string>();
        List<Texture2D> images = new List<Texture2D>();
        bool loaded = false;
        void ProcessLine(string line)
        {
            if (line.Length >= 7 && line.Substring(0, 7) == "<IMAGE=")
            {
                string s = line.Substring(7, line.Length - 8);

                Texture2D image = new Texture2D(2, 2, TextureFormat.ARGB32, false);

                if (ToolbarControl.LoadImageFromFile(ref image, KSPUtil.ApplicationRootPath + "GameData/" + s))
                {
                    images.Add(image);
                    lines.Add(line);
                }
            }
            else
            {
                lines.Add(line);
            }
        }

        Vector2 scrollPos = new Vector2();

        void LoadAndDisplay(string f)
        {
            if (!loaded)
            {
                Load(f);
                loaded = true;
            }
            int imgcnt = 0;
            string l = "";

            if (GUI.Button(new Rect(helpWindow.width - 24 - 2, 2, 24, 24), "X"))
            {
                Destroy(this);
            }

            scrollPos = GUILayout.BeginScrollView(scrollPos, Settings.Instance.scrollViewStyle);

            foreach (var line in lines)
            {
                if (line.Length >= 7 && line.Substring(0, 7) == "<IMAGE=")
                {
                    if (l.Length > 0)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(l);
                        GUILayout.EndHorizontal();
                        l = "";
                    }
                    if (imgcnt <= images.Count)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();
                        GUILayout.Box(images[imgcnt], GUILayout.Width(images[imgcnt].width + 10), GUILayout.Height(images[imgcnt].height + 10));
                        GUILayout.FlexibleSpace();
                        imgcnt++;
                        GUILayout.EndHorizontal();
                    }
                }
                else
                {
                    if (l == "")
                        l = line;
                    else
                        l += "\n" + line;
                }
            }
            if (l.Length > 0)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(l);
                GUILayout.EndHorizontal();
                l = "";
            }
            GUILayout.EndScrollView();
        }

        void HelpWindow(int id)
        {
            LoadAndDisplay(KSPUtil.ApplicationRootPath + "GameData/" + IntroPath);


            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("OK", GUILayout.Width(120)))
            {
                Destroy(this);
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUI.DragWindow();
        }
    }
}

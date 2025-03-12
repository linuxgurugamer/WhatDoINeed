using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static WhatDoINeed.RegisterToolbar;

namespace WhatDoINeed
{
    internal class Utility
    {
 
        // Splits the text into lines, inserting an indent for lines after the first.
        internal static string GetIndentedText(string text, float maxWidth, GUIStyle style, string indent)
        {
            string[] words = text.Split(' ');
            string result = "";
            string currentLine = "";
            int i;
            for ( i = 0; i < text.Length; i++)
                if (text[i] != ' ')
                    break;
            if (i>0)
            currentLine = text.Substring(0, i);
            //bool firstLine = true;

            foreach (string word in words)
            {
                // Test if adding the next word would exceed the max width.
                string testLine = (currentLine.Length == 0) ? word : currentLine + " " + word;
                float size = style.CalcSize(new GUIContent(testLine)).x;
                if (size >= maxWidth && currentLine.Length > 0)
                {
                    // Add the current line to the result.
                    result += currentLine + "\n";
                    // For subsequent lines, add the indent.
                    currentLine = indent + word;
                    //firstLine = false;
                }
                else
                {
                    currentLine = testLine;
                }
            }
            result += currentLine; // append the final line
            return result;
        }

        public static  void ResizeWindow(ref bool resizingWindow, ref Rect winPos, float WIDTH, float HEIGHT)
        {
            if (Input.GetMouseButtonUp(0))
            {
                resizingWindow = false;
            }

            if (resizingWindow)
            {
                winPos.width = Input.mousePosition.x - winPos.x + 10;
                winPos.width = Mathf.Clamp(winPos.width, WIDTH, Screen.width);
                winPos.height = (Screen.height - Input.mousePosition.y) - winPos.y + 10;
                winPos.height = Mathf.Clamp(winPos.height, HEIGHT, Screen.height);
                //SetWinPos(ref winPos, WIDTH, HEIGHT);
            }
        }

        internal  static void SetWinPos(ref Rect winPos, float WIDTH, float HEIGHT)
        {
            winPos.width = Mathf.Clamp(winPos.width, WIDTH, Screen.width);
            //winPos.width = Math.Min(winPos.width, WIDTH);
            winPos.x = Math.Min(winPos.x, Screen.width - winPos.width);
            
            //winPos.height = Mathf.Clamp(winPos.height, HEIGHT, Screen.height);
            //winPos.height = Math.Min(winPos.height, HEIGHT);
            //winPos.y = Math.Min(winPos.y, Screen.height - winPos.y);

            Settings.Instance.SaveData();
        }

        internal static string CleanPartTitle(string title)
        {
            int idx = title.IndexOf("<color=");
            if (idx < 0)
                return title;
            return title.Substring(0, idx);
        }

    }

}

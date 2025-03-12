using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace WhatDoINeed
{
    public partial class WhatDoINeed
    {

        internal enum DisplayLineType { normal, button, textarea, windowbutton, moduleToggleButton};
        internal class DisplayLine
        {
            internal DisplayLineType lineType;

            internal int space;
            internal string line;
            internal GUIStyle font;

            internal int buttonSpacer;
            internal int indent = 0;
            internal GUIContent buttonStr;
            internal  string buttonData;
            internal int buttonWidth;
            internal string buttonTitle;
            internal KeyValuePair<Guid, ContractWrapper> contract;
            internal string contractId;
            internal bool requirementsOpen;
            internal GUIStyle buttonFont;

            internal int textAreaSpace;
            internal string textArea;
            internal GUIStyle textareaFont;
            internal CheckModule checkModule;
            internal bool showCheckmark = false;

            internal bool isPart = false;
            internal AvailablePart partName = null;

            internal DisplayLine(string line, int space, bool isPart, AvailablePart partname = null, GUIStyle font = null)
            {
                lineType = DisplayLineType.normal;
                this.line = line;
                this.space = space;
                this.isPart = isPart;
                this.partName = partname;
                this.font = font;
            }

            internal DisplayLine(string line, int space, bool isPart, bool showCheckMark, AvailablePart partname = null, GUIStyle font = null)
            {
                lineType = DisplayLineType.normal;
                this.line = line;
                this.space = space;
                this.isPart = isPart;
                this.partName = partname;
                this.font = font;
                this.showCheckmark = showCheckMark;
            }

            internal DisplayLine(string line, int space, bool showCheckMark,  GUIStyle font , GUIContent buttonStr, int buttonWidth, string buttonData)
            {
                lineType = DisplayLineType.windowbutton;
                this.line = line;
                this.space = space;
                this.font = font;
                this.buttonStr = buttonStr;
                this.buttonWidth = buttonWidth;
                this.buttonData = buttonData;
                this.showCheckmark = showCheckMark;
            }

            internal DisplayLine(int buttonSpacer, GUIContent buttonStr, int buttonWidth, string buttonTitle, KeyValuePair<Guid, ContractWrapper> contract, string contractId, bool requirementsOpen, GUIStyle buttonFont)
            {
                lineType = DisplayLineType.button;
                this.buttonSpacer = buttonSpacer;
                this.buttonStr = buttonStr;
                this.buttonWidth = buttonWidth;
                this.buttonTitle = buttonTitle;
                this.contract = contract;
                this.contractId = contractId;
                this.requirementsOpen = requirementsOpen;
                this.buttonFont = buttonFont;
            }

            internal DisplayLine(int buttonSpacer, GUIContent buttonStr, int buttonWidth, string buttonTitle, KeyValuePair<Guid, ContractWrapper> contract, string contractId, bool requirementsOpen, GUIStyle buttonFont, ref CheckModule cm)
            {
                this.checkModule = cm;
                lineType = DisplayLineType.moduleToggleButton;
                this.buttonSpacer = buttonSpacer;
                this.buttonStr = buttonStr;
                this.buttonWidth = buttonWidth;
                this.buttonTitle = buttonTitle;
                this.contract = contract;
                this.contractId = contractId;
                this.requirementsOpen = requirementsOpen;
                this.buttonFont = buttonFont;
            }

            internal DisplayLine(int buttonSpacer, int indent, GUIContent buttonStr, int buttonWidth, string buttonTitle, bool showCheckMark, KeyValuePair<Guid, ContractWrapper> contract, string contractId, bool requirementsOpen, GUIStyle buttonFont, ref CheckModule cm)
            {
                this.checkModule = cm;
                lineType = DisplayLineType.moduleToggleButton;
                this.buttonSpacer = buttonSpacer;
                this.indent = indent; 
                this.buttonStr = buttonStr;
                this.buttonWidth = buttonWidth;
                this.buttonTitle = buttonTitle;
                this.contract = contract;
                this.contractId = contractId;
                this.requirementsOpen = requirementsOpen;
                this.buttonFont = buttonFont;
                this.showCheckmark = showCheckMark;
            }

            internal DisplayLine(int space, string briefing, GUIStyle textAreaFont, bool isTextArea) // the bool here is just to have this instantiator different than the first
            {
                lineType = DisplayLineType.textarea;
                this.textAreaSpace = space;
                this.textArea = briefing;
                this.textareaFont = textAreaFont;
            }

        }


    }
}

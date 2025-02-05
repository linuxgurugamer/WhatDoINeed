using System;
using System.Collections.Generic;
using ContractParser;
//using KSP_Log;

using static WhatDoINeed.RegisterToolbar;

namespace WhatDoINeed
{
    public class Contract
    {
        internal bool selected;
        internal bool active;
        internal Dictionary<string, string> experiments;

        internal contractContainer contractContainer;

        internal Contract(contractContainer cc)
        {
            selected = false;
            active = true;
            contractContainer = cc;
            experiments = new Dictionary<string, string>();
        }
    }

    public class CEP_Key_Tuple
    {
        public string expID;
        public Guid contractGuid;
        public string part;

        public string Key()
        {
            if (part == "")
                return expID + "+" + contractGuid.ToString();
            else
                return expID + "+" + part + "+" + contractGuid.ToString();
        }

        public CEP_Key_Tuple(string experimentID, Guid contractGid)
        {
            this.expID = experimentID;
            this.contractGuid = contractGid;
            part = "";
        }

        public CEP_Key_Tuple(string experimentID, Guid contractGid, string part)
        {
            this.expID = experimentID;
            this.contractGuid = contractGid;
            this.part = part;
        }

    }

    public enum GUICircleSelection
    {
        ACTIVE,
        DSN,
        RELAY,
        DSN_AND_RELAY,
        NONE
    }

    internal class AvailPartWrapper
    {
        internal AvailablePart part;
        internal string partTitle;
        internal int numAvailable;
        internal SCANsatSCANtype scanType;
        internal bool scanSatPart = false;

        public AvailPartWrapper(AvailablePart part)
        {
            this.part = part;
            this.partTitle = getPartTitle(part.name);
            numAvailable = 0;
        }

        public AvailPartWrapper(AvailablePart part, SCANsatSCANtype scanType)
        {
            this.part = part;
            this.partTitle = getPartTitle(part.name);
            numAvailable = 0;
            this.scanType = scanType;
            scanSatPart = true;
        }

        private string getPartTitle(string name)
        {
            if (!string.IsNullOrEmpty(name))
            {
                AvailablePart partInfoByName = PartLoader.getPartInfoByName(name.Replace('_', '.'));
                if (partInfoByName != null && ResearchAndDevelopment.PartModelPurchased(partInfoByName))
                {
                    return partInfoByName.title;
                }
            }

            return "";
        }
    }

    public class ContractExperimentPart
    {
        internal string experimentID;
        internal SCANsatSCANtype scanType;
        internal bool scanSatPart = false;
        internal string experimentTitle;
        internal Guid contractGuid;
        internal List<AvailPartWrapper> parts;
        internal int numExpAvail = 0;

        public ContractExperimentPart(CEP_Key_Tuple tuple)
        {
            parts = new List<AvailPartWrapper>();
            //parts.Add(part);
            this.experimentID = tuple.expID;
            this.contractGuid = tuple.contractGuid;
            experimentTitle = getExpTitle(this.experimentID);
        }

        public ContractExperimentPart(CEP_Key_Tuple tuple, string scansatExpID)
        {
            parts = new List<AvailPartWrapper>();
            //parts.Add(part);
            this.experimentID = tuple.expID;
            this.contractGuid = tuple.contractGuid;
            experimentTitle = getExpTitle(scansatExpID);
            string s;
            Log.Info("expID: " + tuple.expID);
            if (tuple.expID.Length >= 9 && tuple.expID.Substring(0, 7) == "SCANsat")
                s = tuple.expID.Substring(8);
            else
                s = tuple.expID; //.Substring(8);

            scanType = (SCANsatSCANtype)(short.Parse(scansatExpID));
            scanSatPart = true;
        }



        private string getExpTitle(string name)
        {
            if (name == null)
                return "";
            ScienceExperiment se = ResearchAndDevelopment.GetExperiment(name);
            if (se != null) return se.experimentTitle;
            return name;
        }


    }
}

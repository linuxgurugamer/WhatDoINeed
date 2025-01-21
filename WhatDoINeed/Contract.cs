using System;
using System.Collections.Generic;
using ContractParser;

namespace WhatDoINeed
{
    public class Contract
    {
        internal bool selected;
        internal bool active;

        internal contractContainer contractContainer;

        internal Contract(contractContainer cc)
        {
            selected = false;
            active = true;
            contractContainer = cc;
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

    public class AvailPartWrapper
    {
        public AvailablePart part;
        public string partTitle;
        public int numAvailable;

        public AvailPartWrapper(AvailablePart part)
        {
            this.part = part;
            this.partTitle = getPartTitle(part.name);
            numAvailable = 0;
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

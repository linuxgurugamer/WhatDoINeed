using System.Collections.Generic;
using static WhatDoINeed.Utility;

namespace WhatDoINeed
{
    /// <summary>
    /// Wrapper around a part with some needed information
    /// </summary>
    public class AvailPartWrapper
    {
        public string NameID { get; set; }
        public List<string> Experiments { get; set; } = new List<string>(); // List of experiments this part is assigned to

        internal AvailablePart part;
        internal string partTitle;
        internal int numAvailable;
        internal SCANsatSCANtype scanType;
        internal bool scanSatPart = false;
        internal AntennaType antennaType;
        public string LogExperiments()
        {
            string s = "";
            foreach (var e in Experiments)
                s += e + ", ";
            return s;
        }
        public string Log()
        {
            return "NameID: " + NameID + ", part.title: " + CleanPartTitle(part.title) + ", scanType: " + scanType.ToString() +
                ", scanSatPart: " + scanSatPart + ", numAvailable: " + numAvailable;
        }
        /// <summary>
        /// Instantiator
        /// </summary>
        /// <param name="part"></param>
        /// <param name="scanType"></param>
        public AvailPartWrapper(AvailablePart part, SCANsatSCANtype scanType = SCANsatSCANtype.Nothing)
        {
            NameID = part.name;
            this.part = part;
            this.partTitle = GetPartTitle(part.name);
            numAvailable = 0;
            if (scanType != SCANsatSCANtype.Nothing)
            {
                this.scanType = scanType;
                scanSatPart = true;
            }
        }
        public AvailPartWrapper(AvailablePart part, string antennaType)
        {
            NameID = part.name;
            this.part = part;
            this.partTitle = GetPartTitle(part.name);
            numAvailable = 0;
            switch (antennaType)
            {
                case "RELAY": this.antennaType = AntennaType.RELAY; break;
                case "DIRECT": this.antennaType = AntennaType.DIRECT; break;
                default: return;
            }
        }
        

        /// <summary>
        /// Returns part title
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string GetPartTitle(string name)
        {
            if (!string.IsNullOrEmpty(name))
            {
                AvailablePart partInfoByName = PartLoader.getPartInfoByName(name.Replace('_', '.'));
                if (partInfoByName != null && ResearchAndDevelopment.PartModelPurchased(partInfoByName))
                {
                    return CleanPartTitle(partInfoByName.title);
                }
            }

            return "";
        }
    }
}

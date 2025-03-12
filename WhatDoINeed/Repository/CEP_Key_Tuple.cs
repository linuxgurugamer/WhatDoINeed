using System;
using System.Collections.Generic;
using ContractParser;
//using KSP_Log;

using static WhatDoINeed.RegisterToolbar;

namespace WhatDoINeed
{
    /// <summary>
    /// Needed for the repository code
    /// </summary>
    public class CEP_Key_Tuple
    {
        public string expID;
        public Guid contractGuid;
        public string part;
        bool scansat;

        public string fullExpID()
        {
            return (scansat ? "SCANsat" : "") + expID;
        }
        public string Key()
        {
            if (part == "")
                return (scansat ? "SCANsat" : "") + expID + "+" + contractGuid.ToString();
            else
                return (scansat ? "SCANsat" : "") + expID + "+" + part + "+" + contractGuid.ToString();
        }

        public CEP_Key_Tuple(string experimentID, Guid contractGid, bool scansat = false)
        {
            this.expID = experimentID;
            this.contractGuid = contractGid;
            part = "";
            this.scansat = scansat;
        }

        public CEP_Key_Tuple(string experimentID, Guid contractGid, string part, bool scansat = false)
        {
            this.expID = experimentID;
            this.contractGuid = contractGid;
            this.part = part;
            this.scansat = scansat;
        }

    }
}

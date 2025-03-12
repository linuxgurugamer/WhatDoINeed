using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WhatDoINeed
{
    /// <summary>
    /// Wrapper around an experiment with some needed information
    /// </summary>
    public class Experiment
    {
        public string ExperimentID { get; set; }

        internal SCANsatSCANtype scanType;
        internal bool scanSatExperiment = false;
        internal string experimentTitle;
        internal Guid contractGuid;
        public string Name { get; set; }

        public string Log()
        {
            return "ExperimentID: " + ExperimentID +
                   ", scanType: " + scanType +
                   ", experimentTitle: " + experimentTitle +
                   ", contractGuid: " + contractGuid +
                   ", Name: " + Name;
        }
        public string ContractExperiment()
        {
            return contractGuid.ToString() + "+" + ExperimentID;
        }

        /// <summary>
        /// Instantiator
        /// </summary>
        /// <param name="tuple"></param>
        /// <param name="scansatExpID"></param>
        public Experiment(int from, CEP_Key_Tuple tuple, string scansatExpID = null)
        {
            if (scansatExpID == null)
            {
                this.ExperimentID = tuple.expID;
                this.contractGuid = tuple.contractGuid;
                experimentTitle = getExpTitle(this.ExperimentID);

            }
            else
            {
                this.ExperimentID = scansatExpID;
                this.contractGuid = tuple.contractGuid;
                short a = Convert.ToInt16(scansatExpID);

                experimentTitle = getExpTitle(("SCANsat" + (SCANsatSCANtype)a).ToString());
                string s;
                if (tuple.expID.Length >= 9 && tuple.expID.Substring(0, 7) == "SCANsat")
                    s = tuple.expID.Substring(8);
                else
                    s = tuple.expID;
                scanType = (SCANsatSCANtype)(short.Parse(scansatExpID));
                scanSatExperiment = true;
            }
            //RegisterToolbar.Log.Info("Experiment, from: " +from + ", "+ Log());
        }

        /// <summary>
        /// Returns the title for an experiment
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
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

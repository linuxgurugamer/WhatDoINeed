using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WhatDoINeed
{
    /// <summary>
    /// Represents a wrapper for a part group, containing the part group key and the number available.
    /// </summary>
    public class PartGroupWrapper
    {
        public string partGroupKey { get; set; }
        public int numAvailable { get; set; } = 0;

        /// <summary>
        /// Instantiator for PartGroupWrapper.
        /// </summary>
        /// <param name="partGroupKey">The key identifying the part group.</param>
        /// <param name="numAvailable">The number of parts available in the group (default is 0).</param>
        public PartGroupWrapper(string partGroupKey, int numAvailable = 0)
        {
            this.partGroupKey = partGroupKey;
            this.numAvailable = numAvailable;
        }

        /// <summary>
        /// Returns a formatted log line for this PartGroupWrapper.
        /// </summary>
        public string Log()
        {
            return "partGroupKey: " + partGroupKey + ", numAvailable: " + numAvailable;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WhatDoINeed
{
    /// <summary>
    /// Represents a check module with a type, description, and tracks the number available.
    /// </summary>
    public class CheckModule
    {
        public string ModuleTypes { get; set; }
        public string Description { get; set; }
        public bool expanded { get; set; }

        /// <summary>
        /// Alternate instantiator for CheckModule.
        /// Initializes ModuleTypes and Description.
        /// </summary>
        /// <param name="types">The type of the check module.</param>
        /// <param name="description">The description of the check module.</param>
        public CheckModule(string types, string description)
        {
            ModuleTypes = types;
            Description = description;
            expanded = false;
        }
        public string Log()
        {
            return "ModuleTypes: " + ModuleTypes +
                   ", Description: " + Description;
        }
    }
}

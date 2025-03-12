using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WhatDoINeed
{
    /// <summary>
    /// Represents ship part modules with dictionaries for parts, modules, and module types.
    /// </summary>
    public class ShipPartModules
    {
        // Dictionaries storing the formatted value as both key and value.
        public Dictionary<string, string> parts { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> modules { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> moduleTypes { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Reinitializes all dictionaries to new empty dictionaries.
        /// </summary>
        public void Reinitialize()
        {
            parts = new Dictionary<string, string>();
            modules = new Dictionary<string, string>();
            moduleTypes = new Dictionary<string, string>();
        }

        /// <summary>
        /// Adds a part value to the parts dictionary after replacing underscores with periods.
        /// </summary>
        /// <param name="value">The part string value.</param>
        public void AddPart(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                string formattedValue = value.Replace('_', '.');
                if (!parts.ContainsKey(formattedValue))
                {
                    parts.Add(formattedValue, formattedValue);
                }
            }
        }

        /// <summary>
        /// Adds a module value to the modules dictionary after replacing underscores with periods.
        /// </summary>
        /// <param name="value">The module string value.</param>
        public void AddModule(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                string formattedValue = value.Replace('_', '.');
                if (!modules.ContainsKey(formattedValue))
                {
                    modules.Add(formattedValue, formattedValue);
                }
            }
        }

        /// <summary>
        /// Adds a module type value to the moduleTypes dictionary after replacing underscores with periods.
        /// </summary>
        /// <param name="value">The module type string value.</param>
        public void AddModuleType(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                string formattedValue = value.Replace('_', '.');
                if (!moduleTypes.ContainsKey(formattedValue))
                {
                    moduleTypes.Add(formattedValue, formattedValue);
                }
            }
        }

        /// <summary>
        /// Checks if a part value exists in the parts dictionary.
        /// </summary>
        /// <param name="value">The part string value to check.</param>
        /// <returns>True if the formatted part value exists; otherwise, false.</returns>
        public bool ContainsPart(string value)
        {
            if (string.IsNullOrEmpty(value))
                return false;
            string formattedValue = value.Replace('_', '.');
            return parts.ContainsKey(formattedValue);
        }

        /// <summary>
        /// Checks if a module value exists in the modules dictionary.
        /// </summary>
        /// <param name="value">The module string value to check.</param>
        /// <returns>True if the formatted module value exists; otherwise, false.</returns>
        public bool ContainsModule(string value)
        {
            if (string.IsNullOrEmpty(value))
                return false;
            string formattedValue = value.Replace('_', '.');
            return modules.ContainsKey(formattedValue);
        }

        /// <summary>
        /// Checks if a module type value exists in the moduleTypes dictionary.
        /// </summary>
        /// <param name="value">The module type string value to check.</param>
        /// <returns>True if the formatted module type exists; otherwise, false.</returns>
        public bool ContainsModuleType(string value)
        {
            if (string.IsNullOrEmpty(value))
                return false;
            string formattedValue = value.Replace('_', '.');
            return moduleTypes.ContainsKey(formattedValue);
        }

        /// <summary>
        /// Returns a formatted log line for ShipPartModules.
        /// </summary>
        public string Log()
        {
            string partsLog = string.Join(", ", parts.Values);
            string modulesLog = string.Join(", ", modules.Values);
            string moduleTypesLog = string.Join(", ", moduleTypes.Values);
            return "Parts: [" + partsLog + "], Modules: [" + modulesLog + "], ModuleTypes: [" + moduleTypesLog + "]";
        }
    }
}

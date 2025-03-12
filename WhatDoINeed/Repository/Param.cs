using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WhatDoINeed
{
    /// <summary>
    /// Parameter class with additional fields and a KerbalName property
    /// </summary>
    public class Param
    {
        public string Key { get; set; }
        public string Value { get; set; }

        public string KerbalName { get; set; }
        public string scanName { get; set; }                            // SCANsatCoverage
        public SCANsatSCANtype scanType { get; set; } = SCANsatSCANtype.Nothing;                       // SCANsatCoverage
        public string experimentType { get; set; }                      // StnSciParameter
        public string subjectId { get; set; }                           // Deployed science
        public List<Filter> Filters { get; set; } = new List<Filter>(); // PartValidation

        // RequestedParts and PartNames (strings are formatted: underscores replaced with periods)
        public List<string> RequestedParts { get; set; } = new List<string>();

        public List<string> PartNames { get; set; } = new List<string>();


        // List of vessel IDs
        //public List<string> Vessels { get; set; } = new List<string>();

        // List of check modules (each with a type and description)
        public List<CheckModule> CheckModules { get; set; } = new List<CheckModule>();

        // List of module names
        public List<string> ModuleNames { get; set; } = new List<string>();


        /// <summary>
        /// Instantiator for Param.
        /// Initializes the Key and Value properties.
        /// </summary>
        /// <param name="key">The parameter key.</param>
        /// <param name="value">The parameter value.</param>
        public Param(string key, int cnt, string value)
        {
            Key = key + cnt;
            Value = value;
        }

        /// <summary>
        /// Adds a single part name to the PartNames list after replacing underscores with periods.
        /// </summary>
        /// <param name="value">The part name string to format and add.</param>
        public void AddPartName(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                string formattedValue = value.Replace('_', '.');
                PartNames.Add(formattedValue);
            }
        }

        /// <summary>
        /// Adds a list of part names to the PartNames list (each formatted with underscores replaced by periods).
        /// </summary>
        /// <param name="values">The collection of part name strings.</param>
        public void AddPartNames(IEnumerable<string> values)
        {
            if (values != null)
            {
                foreach (var value in values)
                {
                    if (!string.IsNullOrEmpty(value))
                    {
                        string formattedValue = value.Replace('_', '.');
                        PartNames.Add(formattedValue);
                    }
                }
            }
        }

        /// <summary>
        /// Adds a single requested part to the RequestedParts list after formatting.
        /// </summary>
        /// <param name="value">The requested part string to format and add.</param>
        public void AddRequestedPart(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                string formattedValue = value.Replace('_', '.');
                RequestedParts.Add(formattedValue);
            }
        }

        /// <summary>
        /// Adds a list of requested parts to the RequestedParts list (each formatted with underscores replaced by periods).
        /// </summary>
        /// <param name="values">The collection of requested part strings.</param>
        public void AddRequestedParts(IEnumerable<string> values)
        {
            if (values != null)
            {
                foreach (var value in values)
                {
                    if (!string.IsNullOrEmpty(value))
                    {
                        string formattedValue = value.Replace('_', '.');
                        RequestedParts.Add(formattedValue);
                    }
                }
            }
        }


        public string Log()
        {
            string l = "Key: " + Key + ", Value: " + Value;
            if (KerbalName != null)
                l += ", KerbalName: " + KerbalName;
            if (scanName != null)
                l += ", scanName: " + scanName;
            if (scanType != SCANsatSCANtype.Nothing)
                l += ", scanType: " + scanType;
            if (experimentType != null)
                l += ", experimentType: " + experimentType;
            if (subjectId != null)
                l += ", subjectId: " + subjectId;

            return l;
        }
        public string LogReqParts()
        {
            string p = "";
            foreach (string part in RequestedParts)
                p += part + ", ";
            return p;
        }

        public string LogChkModules()
        {
            string s = "";
            foreach (var c in CheckModules)
                s += "Type: " + c.ModuleTypes + ", Description: " + c.Description;
            return s;
        }
#if false
        public string LogVessels()
        {
            string v = "";
            foreach (var vessels in Vessels)
                v += vessels + ", ";
            return v;
        }
#endif

        public string LogModuleNames()
        {
            string s = "";
            foreach (var c in ModuleNames)
                s += c + ", ";
            return s;
        }
        public string LogPartNames()
        {
            string s = "";
            foreach (var part in PartNames)
                s += part + ", ";
            return s;
        }

        /// <summary>
        /// Adds a Filter to the Filters list.
        /// </summary>
        /// <param name="filter">The Filter to add.</param>
        public void AddFilter(Filter filter)
        {
            if (filter != null)
            {
                Filters.Add(filter);
            }
        }

        /// <summary>
        /// Removes a Filter from the Filters list.
        /// </summary>
        /// <param name="filter">The Filter to remove.</param>
        /// <returns>True if the filter was removed, otherwise false.</returns>
        public bool RemoveFilter(Filter filter)
        {
            return Filters.Remove(filter);
        }

        /// <summary>
        /// Retrieves all filters matching a given category.
        /// </summary>
        /// <param name="category">The category to search for.</param>
        /// <returns>A list of Filters that match the category.</returns>
        public List<Filter> GetFiltersByCategory(string category)
        {
            return Filters.Where(f => f.category == category).ToList();
        }
    }
}

using ContractParser;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WhatDoINeed
{
    /// <summary>
    /// Wrapper around a contract with some needed information.
    /// Contains experiment groups, a dictionary of part groups, a dictionary of needed parts,
    /// and parameters.
    /// </summary>
    public class ContractWrapper
    {
        public Guid Id { get; set; }
        public string SortOrder { get; set; } // Field to determine sorting order

        internal string contractType; // New internal field for contract contractType

        internal bool selected;
        internal bool active;

        internal contractContainer contractContainer;

        // Groups of experiments assigned to the contract
        public Dictionary<string, List<Experiment>> ExperimentGroups { get; set; } = new Dictionary<string, List<Experiment>>(); // Groups of experiments

        // List of part group keys assigned to the contract
        public Dictionary<string, PartGroupWrapper> PartGroups { get; set; } = new Dictionary<string, PartGroupWrapper>();

        // Needed parts for this contract (moved from Repository)
        public Dictionary<string, List<AvailPartWrapper>> NeededParts { get; set; } = new Dictionary<string, List<AvailPartWrapper>>();

        // Parameters for the contract
        public List<Param> Params { get; set; } = new List<Param>();


        /// <summary>
        /// Instantiator
        /// </summary>
        /// <param name="cc"></param>
        internal ContractWrapper(contractContainer cc, Guid id, string sortOrder, string contractType)
        {
            //RegisterToolbar.Log.Info("ContractWrapper, guid: " + id + ", contractType: " +  contractType);
            this.Id = cc.ID;
            selected = false;
            active = true;
            contractContainer = cc;
            SortOrder = sortOrder;
            this.contractType = contractType;
        }

        /// <summary>
        /// Adds a single parameter to the contract.
        /// </summary>
        /// <param name="param">The parameter to add.</param>
        public void AddParam(Param param)
        {
            //RegisterToolbar.Log.Info("AddParam, contract: " + contractContainer.ID + ", param: " + param.Log());
            //foreach (var f in param.Filters)
            //    RegisterToolbar.Log.Info("Param filter: " + f.Log());
            Params.Add(param);
        }


        /// <summary>
        /// Adds a PartGroupWrapper to the contract.
        /// </summary>
        /// <param name="partGroupKey">The key of the part group to add.</param>
        /// <param name="numAvailable">The number available (optional, default is 0).</param>
        public void AddPartGroup(string partGroupKey, int numAvailable = 0)
        {
            if (PartGroups.ContainsKey(partGroupKey))
            {
                throw new Exception("This part group has already been added to the contract.");
            }
            PartGroups[partGroupKey] = new PartGroupWrapper(partGroupKey, numAvailable);
        }

        /// <summary>
        /// Updates the numAvailable field in the specified PartGroupWrapper.
        /// </summary>
        /// <param name="partGroupKey">The key of the part group to update.</param>
        /// <param name="newNumAvailable">The new number available value.</param>
        public void UpdatePartGroupAvailability(string partGroupKey, int newNumAvailable)
        {
            if (!PartGroups.ContainsKey(partGroupKey))
            {
                throw new Exception("Part group not found in the contract.");
            }
            PartGroups[partGroupKey].numAvailable = newNumAvailable;
        }

        /// <summary>
        /// Returns the numAvailable value from a specified part group.
        /// </summary>
        /// <param name="partGroupKey">The key of the part group.</param>
        /// <returns>The number available in the part group.</returns>
        public int GetPartGroupAvailability(string partGroupKey)
        {
            if (!PartGroups.TryGetValue(partGroupKey, out var partGroup))
            {
                throw new Exception("Part group not found in the contract.");
            }
            return partGroup.numAvailable;
        }

        /// <summary>
        /// Adds needed parts to the contract's NeededParts dictionary.
        /// </summary>
        public void AddNeededParts(string groupKey, IEnumerable<AvailPartWrapper> parts)
        {
            if (NeededParts.ContainsKey(groupKey))
                throw new Exception($"A needed part group with the key '{groupKey}' already exists in the contract.");
            NeededParts[groupKey] = parts.ToList();
        }

        /// <summary>
        /// Adds a single needed part to the contract's NeededParts dictionary.
        /// </summary>
        public void AddNeededPart(int from, string groupKey, AvailPartWrapper part)
        {
            if (part.NameID == null)
            {
                RegisterToolbar.Log.Error("AddNeededPart, part.nameID is null");
                return;
            }
            if (!NeededParts.ContainsKey(groupKey))
                NeededParts[groupKey] = new List<AvailPartWrapper>();
            NeededParts[groupKey].Add(part);
        }

        /// <summary>
        /// Returns a formatted log line for this ContractWrapper.
        /// </summary>
        public string Log()
        {
            //string experimentGroupsLog = string.Join(" | ", ExperimentGroups.Select(kvp =>
            //    "GroupKey: " + kvp.Key + " -> [" + string.Join(", ", kvp.Value.Select(exp => exp.Log())) + "]"));
            //string partGroupsLog = string.Join(" | ", PartGroups.Select(kvp => kvp.Value.Log()));
            //string paramsLog = string.Join(" | ", Params.Select(p => p.Log()));

            return "Id: " + Id +
                   ", SortOrder: " + SortOrder +
                   ", contractType: " + contractType;
            // ", ExperimentGroups: {" + experimentGroupsLog + "}" +
            // ", PartGroups: {" + partGroupsLog + "}" +
            // ", Params: {" + paramsLog + "}";
        }
    }
}

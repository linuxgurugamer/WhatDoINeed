using System;
using System.Collections.Generic;
using System.Linq;

namespace WhatDoINeed
{



    public class PartInformation
    {
        internal string partName;
        internal int index;
        internal bool isAvailable;
        internal bool isOnShip;
        internal int numAvailable = 0;

        internal PartInformation(string partName, int index, bool isAvailable = false, bool isOnShip = false)
        {
            this.partName = partName;
            this.index = index;
            this.isAvailable = isAvailable;
            this.isOnShip = isOnShip;
        }
    }
    public class ModuleInformation
    {
        internal string moduleName;
        internal int numAvailable = 0;
        internal List<AvailablePart> partsWithModule = new List<AvailablePart>();

        internal ModuleInformation(string moduleName)
        {
            this.moduleName = moduleName;
        }
    }

    public class PartCategoryWrapper
    {
        internal string category;
        internal int numAvailable = 0;

        internal PartCategoryWrapper(string category)
        {
            this.category = category;
        }
    }


    public class ExperimentParts
    {
        internal string experimentName;
        internal List<string> parts = new List<string>();

        internal ExperimentParts(string experimentName)
        {
            this.experimentName = experimentName;
        }
    }






    /// <summary>
    /// The Repository class serves as a centralized manager for contracts, part groups, experiments, and parameters, 
    /// providing methods to add, assign, and retrieve this interrelated data.
    /// </summary>
    public class Repository
    {
        public static Dictionary<Guid, ContractWrapper> Contracts { get; set; } = new Dictionary<Guid, ContractWrapper>();
        public ShipPartModules shipInfo { get; set; } = new ShipPartModules();
        public static Dictionary<string, ExperimentParts> allExperimentParts = new Dictionary<string, ExperimentParts>();
        public static Dictionary<string, PartInformation> partInfoList = new Dictionary<string, PartInformation>();
        public static Dictionary<string, ModuleInformation> moduleInformation = new Dictionary<string, ModuleInformation>();
        public static int[] engineTypes = new int[Enum.GetNames(typeof(EngineType)).Length];
        public static Dictionary<string, PartCategoryWrapper> partCategories = new Dictionary<string, PartCategoryWrapper>();
        public static Dictionary<string, int> contractObjectives = new Dictionary<string, int>();

        internal static void ClearNumAvailable()
        {
            foreach (var p in Repository.partInfoList)
                p.Value.numAvailable = 0;
            foreach (var m in Repository.moduleInformation)
                m.Value.numAvailable = 0;

            Array.Clear(Repository.engineTypes, 0, Repository.engineTypes.Length);
            foreach (var n in Enum.GetNames(typeof(PartCategories)))
                Repository.partCategories[n].numAvailable = 0;

            Repository.contractObjectives["Antenna"] = 0;
            Repository.contractObjectives["Generator"] = 0;
            Repository.contractObjectives["Dock"] = 0;

            Repository.contractObjectives["Grapple"] = 0;
            Repository.contractObjectives["Wheel"] = 0;
            Repository.contractObjectives["Laboratory"] = 0;
            Repository.contractObjectives["Harvester"] = 0;
            Repository.contractObjectives["Greenhouse"] = 0;

            RegisterToolbar.numMissingExperiments = 0;
            foreach (var p in Repository.partInfoList.Values)
            {
                p.isOnShip = false;
                p.numAvailable = 0;
            }
        }


        // Add a new contract
        public void AddContract(ContractWrapper contract)
        {
            if (Contracts.ContainsKey(contract.Id))
                throw new Exception("A contract with the same ID already exists.");
            Contracts[contract.Id] = contract;
            //RegisterToolbar.Log.Info("AddContract, contract ID: " + contract.Id);
        }

#if false
        /// <summary>
        /// Add a group of experiments to a contract
        /// </summary>
        /// <param name="contractId"></param>
        /// <param name="groupKey"></param>
        /// <param name="experiments"></param>
        /// <exception cref="Exception"></exception>
        public void AddExperimentGroupToContract(Guid contractId, string groupKey, IEnumerable<Experiment> experiments)
        {
            if (!Contracts.TryGetValue(contractId, out var contract))
                throw new Exception("Contract not found.");

            if (contract.ExperimentGroups.ContainsKey(groupKey))
                throw new Exception($"An experiment group with the key '{groupKey}' already exists.");

            contract.ExperimentGroups[groupKey] = experiments.ToList();
        }
#endif

        /// <summary>
        /// Add a single experiment to a contract (adds to an existing group or creates a new one)
        /// </summary>
        /// <param name="contractId"></param>
        /// <param name="groupKey"></param>
        /// <param name="experiment"></param>
        /// <exception cref="Exception"></exception>
        public void AddExperimentToContract(Guid contractId, string groupKey, Experiment experiment)
        {
            if (!Contracts.TryGetValue(contractId, out var contract))
                throw new Exception("Contract not found.");

            if (!contract.ExperimentGroups.ContainsKey(groupKey))
            {
                contract.ExperimentGroups[groupKey] = new List<Experiment>();
            }

            // Technically, the "found" isn't needed, I could just add a return instead of found = true, but this
            // keeps the flexibility of being able to add something at the end
            bool found = false;
            foreach (var ex in contract.ExperimentGroups[groupKey])
            {
                if (ex.ExperimentID == experiment.ExperimentID)
                {
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                contract.ExperimentGroups[groupKey].Add(experiment);
            }
        }


        /// <summary>
        /// Add a group of parts to the repository
        /// </summary>
        /// <param name="groupKey"></param>
        /// <param name="parts"></param>
        /// <exception cref="Exception"></exception>
        public void AddNeededPartsToContract(Guid contractId, string groupKey, IEnumerable<AvailPartWrapper> parts)
        {
            if (!Contracts.TryGetValue(contractId, out var contract))
                throw new Exception("Contract not found.");
            if (contract.NeededParts.ContainsKey(groupKey))
                throw new Exception($"A needed part group with the key '{groupKey}' already exists in the contract.");
            contract.NeededParts[groupKey] = parts.ToList();
        }

        /// <summary>
        /// Add a single part (creates a group with one part)
        /// </summary>
        /// <param name="groupKey"></param>
        /// <param name="part"></param>
        public void AddNeededPartToContract(Guid contractId, string groupKey, AvailPartWrapper part)
        {
            if (!Contracts.TryGetValue(contractId, out var contract))
                throw new Exception("Contract not found.");
            if (part.NameID == null)
            {
                RegisterToolbar.Log.Error("AddNeededPartToContract, part.nameID is null");
                return;
            }
            if (!contract.NeededParts.ContainsKey(groupKey))
                contract.NeededParts[groupKey] = new List<AvailPartWrapper>();
            contract.NeededParts[groupKey].Add(part);
        }

        /// <summary>
        /// Assign a single experiment to a part
        /// </summary>
        /// <param name="partGroupKey"></param>
        /// <param name="partNameID"></param>
        /// <param name="experimentID"></param>
        /// <exception cref="Exception"></exception>
        public void AssignExperimentToPartInContract(Guid contractId, string partGroupKey, string partNameID, string experimentID)
        {
            if (!Contracts.TryGetValue(contractId, out var contract))
                throw new Exception("Contract not found.");
            if (!contract.NeededParts.ContainsKey(partGroupKey))
                throw new Exception("Needed part group not found in the contract.");
            var part = contract.NeededParts[partGroupKey].FirstOrDefault(p => p.NameID == partNameID);
            if (part == null)
                throw new Exception("Part not found in the specified needed part group.");
            if (!part.Experiments.Contains(experimentID))
            {
                part.Experiments.Add(experimentID);
            }
        }

        /// <summary>
        /// Assign a list of experiments to a part
        /// </summary>
        /// <param name="partGroupKey"></param>
        /// <param name="partNameID"></param>
        /// <param name="experimentIDs"></param>
        /// <exception cref="Exception"></exception>
        public void AssignExperimentsToPartInContract(Guid contractId, string partGroupKey, string partNameID, IEnumerable<string> experimentIDs)
        {
            if (!Contracts.TryGetValue(contractId, out var contract))
                throw new Exception("Contract not found.");
            if (!contract.NeededParts.ContainsKey(partGroupKey))
                throw new Exception("Needed part group not found in the contract.");
            var part = contract.NeededParts[partGroupKey].FirstOrDefault(p => p.NameID == partNameID);
            if (part == null)
                throw new Exception("Part not found in the specified needed part group.");
            foreach (var experimentID in experimentIDs)
            {
                if (!part.Experiments.Contains(experimentID))
                {
                    part.Experiments.Add(experimentID);
                }
            }
        }


        /// <summary>
        /// Adds a group of parts (by its key) to a contract.
        /// </summary>
        public void AddPartGroupToContract(Guid contractId, string partGroupKey)
        {
            if (!Contracts.TryGetValue(contractId, out var contract))
                throw new Exception("Contract not found.");
            if (!contract.NeededParts.ContainsKey(partGroupKey))
                throw new Exception("Needed part group not found in the contract.");
            if (contract.PartGroups.ContainsKey(partGroupKey))
                throw new Exception("This part group has already been added to the contract.");

            // Create and add a new PartGroupWrapper
            contract.PartGroups[partGroupKey] = new PartGroupWrapper(partGroupKey);
        }

        /// <summary>
        /// Retrieve all parts assigned to a contract via part groups
        /// </summary>
        /// <param name="contractId"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>

        public List<AvailPartWrapper> GetPartsForContractFromNeededParts(Guid contractId)
        {
            if (!Contracts.TryGetValue(contractId, out var contract))
                throw new Exception("Contract not found.");
            var parts = new List<AvailPartWrapper>();
            foreach (var groupKey in contract.NeededParts.Keys)
            {
                parts.AddRange(contract.NeededParts[groupKey]);
            }
            return parts;
        }

        /// <summary>
        /// Get all parts for a contract
        /// </summary>
        /// <param name="contractId"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public List<AvailPartWrapper> GetPartsForContract(Guid contractId)
        {
            if (!Contracts.TryGetValue(contractId, out var contract))
                throw new Exception("Contract not found.");
            var experimentIDs = contract.ExperimentGroups.Values
                .SelectMany(group => group.Select(e => e.ExperimentID))
                .ToHashSet();
            return contract.NeededParts.Values
                .SelectMany(group => group)
                .Where(p => p.Experiments.Any(expID => experimentIDs.Contains(expID)))
                .ToList();
        }

        /// <summary>
        /// Get parts for a specific experiment in a contract
        /// </summary>
        /// <param name="contractId"></param>
        /// <param name="experimentId"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public List<AvailPartWrapper> GetPartsForExperimentInContract(Guid contractId, string experimentID)
        {
            if (!Contracts.TryGetValue(contractId, out var contract))
                throw new Exception("Contract not found.");

            if (!contract.ExperimentGroups.Values.SelectMany(group => group).Any(e => e.ExperimentID == experimentID))
                throw new Exception("Experiment not found in the specified contract.");
            return contract.NeededParts.Values
                .SelectMany(group => group)
                .Where(p => p.Experiments.Contains(experimentID))
                .ToList();
        }

        /// <summary>
        /// Get list of experiments in a contract
        /// </summary>
        /// <param name="contractId"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public List<Experiment> GetExperimentsInContract(Guid contractId)
        {
            if (!Contracts.TryGetValue(contractId, out var contract))
                throw new Exception("Contract not found.");

            return contract.ExperimentGroups.Values.SelectMany(group => group).ToList();
        }

        /// <summary>
        /// Get sorted list of all contracts
        /// </summary>
        /// <returns></returns>
        public List<ContractWrapper> GetContracts(bool sorted = false)
        {
            if (sorted)
                return Contracts.Values
                    .OrderBy(c => c.SortOrder, StringComparer.Ordinal)
                    .ThenBy(c => c.Id)
                       .ToList();
            else
                return Contracts.Values.ToList();
        }
        public string Log()
        {
            // Log for needed parts is taken from each contract's NeededParts dictionary.
            string contractsLog = string.Join(" | ", Contracts.Select(c => c.Value.Log()));
            return "Repository:" +
                   "\nContracts: {" + contractsLog + "}" +
                   "\nShip Info: " + shipInfo.Log();
        }
    }
}
using KIS;
using KISAPIv1;

using static WhatDoINeed.RegisterToolbar;

namespace Workshop.W_KIS
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;

   

	using UnityEngine;

	public class KIS_Shared
	{


        private static KISAPIv1.PartUtilsImpl _partUtilsImpl;


        public static float GetPartVolume(AvailablePart part)
		{
            return (float)_partUtilsImpl.GetPartVolume(part);
		}

		internal static void Initialize() //Assembly kisAssembly)
		{
            _partUtilsImpl = new PartUtilsImpl();
        }
	}

	public class ModuleKISItem
	{
        KIS.ModuleKISItem _moduleKISItem;

		public float volumeOverride
		{
			get
			{
                if (_moduleKISItem == null)
                    return 0;
                return _moduleKISItem.volumeOverride;
			}
		}

		public ModuleKISItem(object obj)
		{
            _moduleKISItem = (KIS.ModuleKISItem)obj;
		}

		internal static void Initialize() //Assembly kisAssembly)
		{
		}
	}

	public class ModuleKISInventory
	{
		public enum InventoryType { Container, Pod, Eva }

        KIS.ModuleKISInventory _moduleKISInventory;


		public ModuleKISInventory(object obj)
		{
            _moduleKISInventory = (KIS.ModuleKISInventory)obj;
		}

		public InventoryType invType
		{
			get
			{
                return (InventoryType)_moduleKISInventory.invType;
			}
		}

		public int podSeat
		{
			get
			{
                return _moduleKISInventory.podSeat;
			}
		}

		public float maxVolume
		{
			get
			{
                return _moduleKISInventory.maxVolume;
			}
		}

		public bool showGui
		{
			get
			{
                return _moduleKISInventory.showGui;
            }
        }

		public Part part
		{
			get
			{
                return _moduleKISInventory.part;
			}
		}


        public Dictionary<int, W_KIS_Item> items
        {
            get
            {
                var dict = new Dictionary<int, W_KIS_Item>();

                var inventoryItems = (IDictionary)_moduleKISInventory.items;

                foreach (DictionaryEntry entry in inventoryItems)
				{
					dict.Add((int)entry.Key, new W_KIS_Item(entry.Value));
				}

                return dict;
            }
        }


		public float GetContentVolume()
		{
            return (float)_moduleKISInventory.totalContentsVolume;
		}

		public bool isFull()
		{
            return _moduleKISInventory.isFull();
		}

		public W_KIS_Item AddItem(Part partPrefab)
		{
            var obj = _moduleKISInventory.AddItem(partPrefab, 1, -1);
            
			return new W_KIS_Item(obj);
		}

		internal static void Initialize() //Assembly kisAssembly)
        {

        }
    }

	public class W_KIS_Item
	{
		public struct ResourceInfo
		{
            private readonly ProtoPartResourceSnapshot _resourceInfo;

			public string resourceName
			{
				get
				{
                    return _resourceInfo.resourceName;
				}
			}

			public double amount
			{
				get
				{
                    return _resourceInfo.amount;
				}
			}

			public double maxAmount
			{
				get
				{
                    return _resourceInfo.maxAmount;
				}
			}

			public ResourceInfo(object obj)
			{
                _resourceInfo = (ProtoPartResourceSnapshot)obj;
			}

			public static void Initialize() // Assembly kisAssembly)
            {

            }
        }

        KIS.KIS_Item _kis_item;

		public W_KIS_Item(object obj)
		{
            _kis_item = (KIS.KIS_Item)obj;

        }

		public KIS_IconViewer Icon
		{
			get
			{
                if (_kis_item.icon == null)
                    return null;
                return new KIS_IconViewer(_kis_item.icon);
			}
		}

		public AvailablePart availablePart
		{
			get
			{
                return _kis_item.availablePart;
			}
		}

		public int quantity
		{
            get { return _kis_item.quantity; }
        }

        public void UpdateResource(string name, double amount, bool isAmountRelative = false)
        {
            _kis_item.UpdateResource(name, amount, isAmountRelative);
        }

        public ConfigNode partNode
        {
            get { return _kis_item.partNode;  }
        }

        public void Delete()
        {
            _kis_item.Delete();
        }
        public bool stackable
		{
            get { return _kis_item.stackable; }
		}

		public ProtoPartResourceSnapshot[] GetResources()
		{
            return KISAPI.PartNodeUtils.GetResources(_kis_item.partNode);
		}

		public void SetResource(string name, int amount)
		{
            KISAPI.PartNodeUtils.UpdateResource(_kis_item.partNode, name, amount); 
		}

		public void EnableIcon(int resolution)
		{
            _kis_item.EnableIcon(resolution);
		}

		public void DisableIcon()
		{
            _kis_item.DisableIcon();
		}

		public void StackRemove(int quantity)
		{
            _kis_item.StackRemove(quantity);
        }

		internal static void Initialize() // Assembly kisAssembly)
        {
        }
    }

	public class KIS_IconViewer
	{
        KIS.KIS_IconViewer _iconViewer;

        public Texture texture
		{
			get
			{
                return _iconViewer.texture;
			}
		}

		public void Dispose()
		{
            _iconViewer.Dispose();
		}

        public KIS_IconViewer(object obj)
		{
            _iconViewer = (KIS.KIS_IconViewer)obj;
		}

        public KIS_IconViewer(Part p, int resolution) 
        {
            _iconViewer = new KIS.KIS_IconViewer(p, resolution);
        }


        internal static void Initialize() // Assembly kisAssembly)
        {
		}
	}

	public class KISWrapper
	{
		public static bool Initialize()
		{
			var kisAssembly = AssemblyLoader.loadedAssemblies.FirstOrDefault(a => a.assembly.GetName().Name.Equals("KIS", StringComparison.InvariantCultureIgnoreCase));

             if (kisAssembly == null)
			{
                    return false;
			}
            KIS_Shared.Initialize(); //  kisAssembly.assembly);
            ModuleKISInventory.Initialize(); // kisAssembly.assembly);
            ModuleKISItem.Initialize(); // kisAssembly.assembly);
            W_KIS_Item.Initialize(); // kisAssembly.assembly);
            KIS_IconViewer.Initialize(); // kisAssembly.assembly);
            W_KIS_Item.ResourceInfo.Initialize(); // kisAssembly.assembly);
            return true;
		}

		public static List<ModuleKISInventory> GetInventories(Vessel vessel)
		{
			var inventories = new List<ModuleKISInventory>();
			foreach (var part in vessel.parts)
			{
				foreach (PartModule module in part.Modules)
				{
					if (module.moduleName == "ModuleKISInventory")
					{
                        if (module is KIS.ModuleKISInventory) {
                            inventories.Add(new ModuleKISInventory(module));
                        } else {
                            Log.Error(module.name + " is not KIS.ModuleKISInventory, but " + module.ClassName + " instead. Skipping");
                        }
                    }
				}
			}
			return inventories;
		}

        public static List<ModuleKISInventory> GetInventories(Part part)
        {
            var inventories = new List<ModuleKISInventory>();
            
            foreach (PartModule module in part.Modules)
            {
                if (module.moduleName == "ModuleKISInventory")
                {
                    if (module is KIS.ModuleKISInventory) {
                        inventories.Add(new ModuleKISInventory(module));
                    } else {
                        Log.Error(module.name + " is not KIS.ModuleKISInventory, but " + module.ClassName + " instead. Skipping");
                    }
                }
            }
            
            return inventories;
        }

        public static ModuleKISItem GetKisItem(Part part)
		{
			ModuleKISItem item = null;
			foreach (PartModule module in part.Modules)
			{
				if (module.moduleName == "ModuleKISItem")
				{
					item = new ModuleKISItem(module);
				}
			}
			return item;
		}
	}
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using static WhatDoINeed.RegisterToolbar;
using System.Reflection;

namespace WhatDoINeed
{
#if false
    public class KIS_Shared
	{
        private static KISAPIv1.PartUtilsImpl _partUtilsImpl;

		internal static void Initialize() //Assembly kisAssembly)
		{
            _partUtilsImpl = new PartUtilsImpl();
        }
	}
#endif

	public class ModuleKISItem
	{

		internal static void Initialize() //Assembly kisAssembly)
		{
		}
	}

	public class ModuleKISInventory
	{
		public enum InventoryType { Container, Pod, Eva }




		internal static void Initialize() //Assembly kisAssembly)
        {

        }
    }

	public class W_KIS_Item
	{
		public struct ResourceInfo
		{
#if false
            private readonly ProtoPartResourceSnapshot _resourceInfo;

			public string resourceName
			{
				get
				{
                    return _resourceInfo.resourceName;
				}
			}
#endif
			public static void Initialize() // Assembly kisAssembly)
            {

            }
        }

		internal static void Initialize() // Assembly kisAssembly)
        {
        }
    }

	public class KISWrapper
	{
		public static bool Initialize()
		{
            AssemblyLoader.LoadedAssembly KISasm = null;

            var kisAssembly = AssemblyLoader.loadedAssemblies.FirstOrDefault(a => a.assembly.GetName().Name.Equals("KIS", StringComparison.InvariantCultureIgnoreCase));

             if (kisAssembly == null)
			{
                    return false;
			}
            foreach (var la in AssemblyLoader.loadedAssemblies)
            {
                if (la.assembly.GetName().Name.Equals("KIS", StringComparison.InvariantCultureIgnoreCase))
                {
                    KISasm = la;
                }
            }
            if (KISasm != null)
            {
                Debug.Log($"[KISWrapper] found KIS {KISasm}");
                ModuleKISInventory.Initialize(KISasm.assembly);
                KIS_Item.Initialize(KISasm.assembly);
            }

            //KIS_Shared.Initialize(); //  kisAssembly.assembly);
            W_KIS_Item.Initialize(); // kisAssembly.assembly);
            W_KIS_Item.ResourceInfo.Initialize(); // kisAssembly.assembly);
            return true;
		}


        public static List<ModuleKISInventory> GetInventories(Part part)
        {
            var inventories = new List<ModuleKISInventory>();
            
            for (int i = 0;i < part.Modules.Count; i++)
            { 
                var module = part.Modules[i];
                if (module.moduleName == "ModuleKISInventory")
                {
                    //if (module is ModuleKISInventory) {
                        inventories.Add(new ModuleKISInventory(module));
                    //} else {
                    //    Log.Error(module.name + " is not KIS.ModuleKISInventory, but " + module.ClassName + " instead. Skipping");
                    //}
                }
            }
            
            return inventories;
        }

        public class KIS_Item
        {
            static Type KIS_Item_class;
            static FieldInfo kis_partNode;
            static PropertyInfo kis_itemResourceMass;

            object obj;

            public ConfigNode partNode { get { return (ConfigNode)kis_partNode.GetValue(obj); } }
            public double itemResourceMass { get { return (double)kis_itemResourceMass.GetValue(obj, null); } }

            public struct ResourceInfo
            {
                public string resourceName { get; private set; }
                public double maxAmount { get; private set; }
                public double amount { get; private set; }

                public ResourceInfo(ConfigNode node)
                {
                    double val;
                    resourceName = node.GetValue("name");
                    double.TryParse(node.GetValue("maxAmount"), out val);
                    maxAmount = val;
                    double.TryParse(node.GetValue("amount"), out val);
                    amount = val;
                }
            }

            public KIS_Item(object obj)
            {
                this.obj = obj;
            }

            public List<ResourceInfo> GetResources()
            {
                var resources = new List<ResourceInfo>();
                var kis_resources = partNode.GetNodes("RESOURCE");
                for (int i = 0;i < kis_resources.Length; i++)
                { 
                    var res = kis_resources[i];
                    resources.Add(new ResourceInfo(res));
                }
                return resources;
            }

            internal static void Initialize(Assembly KISasm)
            {
                KIS_Item_class = KISasm.GetTypes().Where(t => t.Name.Equals("KIS_Item")).FirstOrDefault();
                kis_partNode = KIS_Item_class.GetField("partNode");
                kis_itemResourceMass = KIS_Item_class.GetProperty("itemResourceMass");
            }
        }
        public class ModuleKISInventory
        {
            static Type ModuleKISInventory_class;
            static FieldInfo kis_items;

            object obj;

            public Dictionary<int, KIS_Item> items
            {
                get
                {
                    var dict = new Dictionary<int, KIS_Item>();
                    var items = (IDictionary)kis_items.GetValue(obj);
                    foreach (DictionaryEntry de in items)
                    {
                        dict.Add((int)de.Key, new KIS_Item(de.Value));
                    }
                    return dict;
                }
            }

            public Part part
            {
                get
                {
                    return (obj as PartModule).part;
                }
            }

            public ModuleKISInventory(object obj)
            {
                this.obj = obj;
            }

            internal static bool Initialize(Assembly KISasm)
            {
                var types = KISasm.GetTypes();

                for (int i = 0; i < types.Length; i++)
                { 
                    var t = types[i];
                    if (t.Name == "ModuleKISInventory")
                    {
                        ModuleKISInventory_class = t;
                        kis_items = ModuleKISInventory_class.GetField("items");
                        Debug.Log($"[KISWrapper] ModuleKISInventory {kis_items}");
                        return true;
                    }
                }
                return false;
            }
        }
    }
}

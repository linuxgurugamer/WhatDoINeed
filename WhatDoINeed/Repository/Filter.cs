using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WhatDoINeed
{
    /// <summary>
    /// Represents a filter with a category, a value, and a type.
    /// </summary>
    public class Filter
    {
        public string category { get; set; }
        public string value { get; set; }
        public string type { get; set; }


        /// <summary>
        /// Instantiator for Filter.
        /// Initializes the category, value, and type properties.
        /// </summary>
        /// <param name="category">The filter category.</param>
        /// <param name="value">The filter value.</param>
        /// <param name="type">The filter type.</param>
        public Filter(string category, string value, string type)
        {
            //RegisterToolbar.Log.Info("Filter, category: " + category + ", value: " + value + ", type: " + type);
            this.category = category;
            this.value = value;
            this.type = type;
        }
        public string Log()
        {
            return "category: " + category + ", value: " + value + ", type: " + type;
        }
    }
}

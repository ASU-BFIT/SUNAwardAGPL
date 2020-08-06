using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BTS.Common.Mvc.Grid.Infrastructure
{
    /// <summary>
    /// Infrastructure class to hold metadata when exporting grids
    /// </summary>
    public class GridExport
    {
        /// <summary>
        /// Column name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Column display name
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Constructs a new GridExport
        /// </summary>
        /// <param name="name"></param>
        public GridExport(string name)
        {
            Name = name;
            DisplayName = name;
        }

        /// <summary>
        /// Constructs a new GridExport
        /// </summary>
        /// <param name="name"></param>
        /// <param name="displayName"></param>
        public GridExport(string name, string displayName)
        {
            Name = name;
            DisplayName = displayName;
        }

        /// <summary>
        /// For compatibility with MemberInfo.GetDisplayName() (part of ReflectionExtensions in BTS.Common)
        /// </summary>
        /// <returns></returns>
        public string GetDisplayName()
        {
            return DisplayName;
        }

        /// <summary>
        /// Retrieves the value of this column
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public object GetValue(object obj)
        {
            var dict = (Dictionary<string, string>)obj;
            return dict[Name];
        }
    }
}
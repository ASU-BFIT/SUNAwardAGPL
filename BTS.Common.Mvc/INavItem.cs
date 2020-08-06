using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace BTS.Common.Mvc
{
    /// <summary>
    /// Represents an item in the top nav
    /// </summary>
    public interface INavItem : IHtmlString
    {
        /// <summary>
        /// If false, this nav item is not visible (likely due to
        /// failing permissions checks).
        /// </summary>
        bool IsVisible { get; }

        /// <summary>
        /// Convert this nav item into its JSON representation
        /// </summary>
        /// <returns></returns>
        object ToJson(UrlHelper url);
    }
}
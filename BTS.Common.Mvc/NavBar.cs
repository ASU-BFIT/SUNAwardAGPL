using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using Newtonsoft.Json;

namespace BTS.Common.Mvc
{
    /// <summary>
    /// Top nav bar
    /// </summary>
    public class NavBar : IHtmlString
    {
        /// <summary>
        /// Items in the navbar
        /// </summary>
        public IReadOnlyList<INavItem> Items { get; private set; }

        /// <summary>
        /// If the Home icon should link elsewhere, setting this defines that link.
        /// If unset, it links to the root of the application (HomeController.Index)
        /// </summary>
        public string HomeLink { get; set; }

        private readonly HtmlHelper Helper;

        /// <summary>
        /// Generates a new navigation bar
        /// </summary>
        /// <param name="helper"></param>
        /// <param name="navItems">Items to show in the bar</param>
        public NavBar(HtmlHelper helper, params INavItem[] navItems)
        {
            Helper = helper;
            Items = navItems.Where(i => i.IsVisible).ToList();
        }

        /// <summary>
        /// Defines the location that the Home icon links to. 
        /// </summary>
        /// <param name="url">
        /// URL to link to. Use Url.Action() to link to another action in the application,
        /// or you can specify a fully-qualified URL instead.
        /// </param>
        /// <returns>The NavBar instance, for chaining fluent methods.</returns>
        public NavBar WithHomeLink(string url)
        {
            HomeLink = url;

            return this;
        }

        /// <summary>
        /// Renders navbar to HTML
        /// </summary>
        /// <returns></returns>
        public string ToHtmlString()
        {
            if (Items.Count == 0)
            {
                return String.Empty;
            }

            return Helper.Partial("~/Views/Common/_NavBar.cshtml", this).ToHtmlString();
        }

        /// <summary>
        /// Renders navbar to JSON (for rolling into shared ASU header's hamburger menu)
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public string ToJsonString(UrlHelper url)
        {
            // this is only called if ToHtmlString above rendered our partial view
            // so we don't need to check if Items.Count is 0 here.
            var jsonObjList = new List<object>()
            {
                new { title = "Home", path = url.Action("Index", "Home") }
            };

            jsonObjList.AddRange(Items.Select(i => i.ToJson(url)));

            return JsonConvert.SerializeObject(jsonObjList);
        }
    }
}
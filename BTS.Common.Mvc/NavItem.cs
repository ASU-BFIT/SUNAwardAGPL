using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Text;

namespace BTS.Common.Mvc
{
    /// <summary>
    /// Dropdown in a nav bar
    /// </summary>
    public class NavItem : INavItem
    {
        /// <summary>
        /// Links in the dropdown
        /// </summary>
        public IReadOnlyList<NavLink> Items { get; private set; }
        /// <summary>
        /// If dropdown is rendered
        /// </summary>
        public bool IsVisible => Items.Count > 0;
        private readonly HtmlHelper Helper;
        private readonly string HeaderText;
        private readonly string ControllerName;

        /// <summary>
        /// Constructs a new dropdown
        /// </summary>
        /// <param name="helper"></param>
        /// <param name="headerText"></param>
        /// <param name="controllerName"></param>
        /// <param name="navLinks"></param>
        public NavItem(HtmlHelper helper, string headerText, string controllerName, params NavLink[] navLinks)
        {
            Helper = helper;
            Items = navLinks.Where(l => l.IsVisible).ToList();
            HeaderText = headerText;
            ControllerName = controllerName;
        }

        /// <summary>
        /// Renders dropdown to HTML
        /// </summary>
        /// <returns></returns>
        public string ToHtmlString()
        {
            if (!IsVisible)
            {
                return String.Empty;
            }

            bool active = Helper.ViewContext.RouteData.Values["Controller"].ToString() == ControllerName;

            var outerLi = new TagBuilder("li");
            outerLi.AddCssClass("dropdown");
            if (active)
            {
                outerLi.AddCssClass("active");
            }

            var dropdownToggle = new TagBuilder("a");
            dropdownToggle.MergeAttributes(new Dictionary<string, string>
            {
                { "href", "#" },
                { "class", "dropdown-toggle" },
                { "data-toggle", "dropdown" },
                { "role", "button" },
                { "aria-haspopup", "true" },
                { "aria-expanded", "false" }
            });
            dropdownToggle.SetInnerText(HeaderText);

            var innerMenu = new TagBuilder("ul");
            innerMenu.AddCssClass("dropdown-menu");
            innerMenu.InnerHtml = String.Empty;
            foreach (var item in Items)
            {
                innerMenu.InnerHtml += item.ToHtmlString(true);
            }

            outerLi.InnerHtml = dropdownToggle.ToString() + innerMenu.ToString();

            return outerLi.ToString();
        }

        /// <summary>
        /// Renders dropdown to JSON
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public object ToJson(UrlHelper url)
        {
            return new { title = HeaderText, path = String.Empty, children = Items.Select(i => i.ToJson(url)).ToList() };
        }
    }
}

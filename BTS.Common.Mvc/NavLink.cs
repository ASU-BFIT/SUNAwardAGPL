using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Web.Mvc.Html;

namespace BTS.Common.Mvc
{
    /// <summary>
    /// Link in a navbar
    /// </summary>
    public class NavLink : INavItem
    {
        /// <summary>
        /// If link is rendered
        /// </summary>
        public bool IsVisible { get; private set; }
        private readonly HtmlHelper Helper;
        private readonly string LinkText;
        private readonly string ActionName;
        private readonly string ControllerName;

        /// <summary>
        /// Constructs a new NavLink
        /// </summary>
        /// <param name="helper"></param>
        /// <param name="linkText"></param>
        /// <param name="actionName"></param>
        /// <param name="controllerName"></param>
        /// <param name="isVisible"></param>
        public NavLink(HtmlHelper helper, string linkText, string actionName, string controllerName, bool isVisible)
        {
            Helper = helper;
            LinkText = linkText;
            ActionName = actionName;
            ControllerName = controllerName;
            IsVisible = isVisible;
        }

        /// <summary>
        /// Renders link to HTML
        /// </summary>
        /// <returns></returns>
        public string ToHtmlString()
        {
            return ToHtmlString(false);
        }

        /// <summary>
        /// Renders link to HTML
        /// </summary>
        /// <param name="forDropdown">Whether or not this link is inside of a dropdown list</param>
        /// <returns></returns>
        public string ToHtmlString(bool forDropdown)
        {
            if (!IsVisible)
            {
                return String.Empty;
            }

            var builder = new TagBuilder("li");

            if (forDropdown)
            {
                builder.AddCssClass("dropdown-item");
            }

            if (Helper.ViewContext.RouteData.Values["Controller"].ToString() == ControllerName
                && Helper.ViewContext.RouteData.Values["Action"].ToString() == ActionName)
            {
                builder.AddCssClass("active");
            }

            builder.InnerHtml = Helper.ActionLink(LinkText, ActionName, ControllerName).ToString();
            return builder.ToString();
        }

        /// <summary>
        /// Renders link to JSON
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public object ToJson(UrlHelper url)
        {
            return new { title = LinkText, path = url.Action(ActionName, ControllerName) };
        }
    }
}

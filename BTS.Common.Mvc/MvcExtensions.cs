using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Web.Configuration;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using System.Web.Optimization;
using System.IO;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Net;
using System.Diagnostics;
using System.Web;
using System.Text;

using BTS.Common.Web;
using System.Diagnostics.CodeAnalysis;

namespace BTS.Common.Mvc
{
    /// <summary>
    /// Extension methods for MVC
    /// </summary>
    public static class MvcExtensions
    {
        /// <summary>
        /// Init ASU header/footer cache on application pool startup (for faster first page load)
        /// </summary>
        static MvcExtensions()
        {
            // note: we want to no-op if this isn't being called from a web context
            if (Process.GetCurrentProcess().ProcessName == "w3wp")
            {
                FetchAsuElements();
            }
        }

        private static string _asuHead = null;
        private static string _asuGtm = null;
        private static string _asuHeader = null;
        private static string _asuFooter = null;

        private static void FetchAsuElements()
        {
            var headerSection = WebConfigurationManager.GetWebApplicationSection("asuHeader") as AsuHeaderSection ?? new AsuHeaderSection();
            string cacheDir = headerSection.CacheDirectory;

            if (String.IsNullOrEmpty(cacheDir))
            {
                cacheDir = @"C:\inetpub\temp";
            }

            string fullCacheDir = Path.Combine(cacheDir, "asuTheme" + headerSection.Version);
            int needRecache = 4;

            needRecache -= ReadAsuElementFromFile(Path.Combine(fullCacheDir, "head.txt"), ref _asuHead);
            needRecache -= ReadAsuElementFromFile(Path.Combine(fullCacheDir, "gtm.txt"), ref _asuGtm);
            needRecache -= ReadAsuElementFromFile(Path.Combine(fullCacheDir, "header.txt"), ref _asuHeader);
            needRecache -= ReadAsuElementFromFile(Path.Combine(fullCacheDir, "footer.txt"), ref _asuFooter);

            if (needRecache > 0)
            {
                CacheAsuElements();

                try
                {
                    if (!Directory.Exists(fullCacheDir))
                    {
                        Directory.CreateDirectory(fullCacheDir);
                    }

                    WriteAsuElementToFile(Path.Combine(fullCacheDir, "head.txt"), ref _asuHead);
                    WriteAsuElementToFile(Path.Combine(fullCacheDir, "gtm.txt"), ref _asuGtm);
                    WriteAsuElementToFile(Path.Combine(fullCacheDir, "header.txt"), ref _asuHeader);
                    WriteAsuElementToFile(Path.Combine(fullCacheDir, "footer.txt"), ref _asuFooter);
                }
                catch (UnauthorizedAccessException)
                {
                    // no permission to write here, so don't write a cache
                    // we should log this somehow as well (in AppInsights whenever that gets set up)
                    // since this is part of initialization, this doesn't log to the error log very well
                    // (the error message is suppressed and replaced with something completely unhelpful)
                }
            }
        }

        /// <summary>
        /// Reads a cached file. Returns 1 on success and 0 on failure (failure indicates a
        /// requirement to recache the file).
        /// </summary>
        /// <param name="path"></param>
        /// <param name="storage"></param>
        /// <returns></returns>
        private static int ReadAsuElementFromFile(string path, ref string storage)
        {
            if (!File.Exists(path))
            {
                return 0;
            }

            using (var f = File.OpenText(path))
            {
                var cacheExp = Convert.ToDateTime(f.ReadLine());
                var cacheHash = f.ReadLine();

                if (cacheExp < DateTime.Now)
                {
                    storage = null;
                    return 0;
                }

                storage = f.ReadToEnd();

                var sbytes = Encoding.UTF8.GetBytes(storage);
                var shash = new StringBuilder();
                using (var crypt = new SHA256Managed())
                {
                    var bhash = crypt.ComputeHash(sbytes);
                    foreach (var b in bhash)
                    {
                        shash.AppendFormat("{0:x2}", b);
                    }
                }

                if (cacheHash != shash.ToString())
                {
                    storage = null;
                    return 0;
                }

                return 1;
            }
        }

        private static void WriteAsuElementToFile(string path, ref string content)
        {
            var sbytes = Encoding.UTF8.GetBytes(content);
            var shash = new StringBuilder();
            using (var crypt = new SHA256Managed())
            {
                var bhash = crypt.ComputeHash(sbytes);
                foreach (var b in bhash)
                {
                    shash.AppendFormat("{0:x2}", b);
                }
            }

            var date = DateTime.Now.AddDays(60).ToString();

            using (var f = new StreamWriter(path, false, Encoding.UTF8))
            {
                f.WriteLine(date);
                f.WriteLine(shash);
                f.Write(content);
            }
        }

        private static void CacheAsuElements()
        {
            string baseUrl;

            var section = WebConfigurationManager.GetWebApplicationSection("asuHeader") as AsuHeaderSection ?? new AsuHeaderSection();

            if (String.IsNullOrEmpty(section.Version))
            {
                throw new InvalidOperationException("Invalid version setting for asuHeader in Web.config.");
            }

            switch (section.LoadFrom)
            {
                case AsuHeaderSection.FetchMode.AFS:
                    baseUrl = @"\\afs\asu.edu\www\asuthemes\";
                    break;
                case AsuHeaderSection.FetchMode.HTTPS:
                    baseUrl = "https://www.asu.edu/asuthemes/";
                    break;
                default:
                    throw new InvalidOperationException("Invalid loadFrom setting for asuHeader in Web.config.");
            }

            baseUrl += section.Version + "/";

            using (var client = new WebClient())
            {
                using (var reader = new StreamReader(client.OpenRead(baseUrl + "heads/default.shtml")))
                {
                    _asuHead = reader.ReadToEnd();
                }

                using (var reader = new StreamReader(client.OpenRead(baseUrl + "includes/gtm.shtml")))
                {
                    _asuGtm = reader.ReadToEnd();
                }

                using (var reader = new StreamReader(client.OpenRead(baseUrl + "headers/default.shtml")))
                {
                    _asuHeader = reader.ReadToEnd();
                }

                using (var reader = new StreamReader(client.OpenRead(baseUrl + "includes/footer.shtml")))
                {
                    _asuFooter = reader.ReadToEnd();
                }
            }
        }

        /// <summary>
        /// Adds the ASU header. This is called by _Layout and does not need to be called unless not using _Layout.
        /// </summary>
        /// <param name="helper"></param>
        /// <param name="what"></param>
        /// <returns></returns>
        [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Provides a more accessible API if we expose this as an extension method on HtmlHelper")]
        public static MvcHtmlString AsuElement(this HtmlHelper helper, AsuSharedElement what)
        {
            if (_asuHead == null)
            {
                CacheAsuElements();
            }

            switch (what)
            {
                case AsuSharedElement.Head:
                    return new MvcHtmlString(_asuHead);
                case AsuSharedElement.GoogleTagManager:
                    return new MvcHtmlString(_asuGtm);
                case AsuSharedElement.Header:
                    return new MvcHtmlString(_asuHeader);
                case AsuSharedElement.Footer:
                    return new MvcHtmlString(_asuFooter);
            }

            return MvcHtmlString.Empty;
        }

        /// <summary>
        /// Retrieves the current user
        /// </summary>
        /// <param name="helper"></param>
        /// <returns></returns>
        public static ClaimsPrincipal GetUser(this HtmlHelper helper)
        {
            return helper.ViewContext.HttpContext.User as ClaimsPrincipal;
        }

        /// <summary>
        /// Registers shared filters that should be on every application
        /// </summary>
        /// <param name="filters"></param>
        public static void RegisterCommonFilters(this GlobalFilterCollection filters)
        {
            filters.Add(new RequestUrlHeaderAttribute());
            filters.Add(new AntiForgeryTokenHeaderAttribute());
            FilterProviders.Providers.Add(new ValidateAntiForgeryFilter());
        }

        /// <summary>
        /// Gets an HTML helper inside of a controller
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="controller"></param>
        /// <param name="writer"></param>
        /// <returns></returns>
        public static HtmlHelper<T> GetHtmlHelper<T>(this ControllerBase controller, TextWriter writer = null)
        {
            return new HtmlHelper<T>(
                new ViewContext(
                    controller.ControllerContext,
                    new WebFormView(controller.ControllerContext, "BTS.Common"),
                    new ViewDataDictionary<T>(),
                    controller.TempData,
                    writer ?? TextWriter.Null
                ),
                new ViewPage<T>()
            );
        }

        /// <summary>
        /// Cast the HtmlHelper to a different type, using the passed-in model as the new view data dictionary
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="helper"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        public static HtmlHelper<T> As<T>(this HtmlHelper helper, T model)
        {
            // NOTE: DO NOT MAKE ADDITIONAL OVERLOADS OF THIS
            // (it will break GridExtensions)
            var dict = new ViewDataDictionary<T>(model);

            return new HtmlHelper<T>(
                new ViewContext(
                    helper.ViewContext.Controller.ControllerContext,
                    helper.ViewContext.View,
                    dict,
                    helper.ViewContext.TempData,
                    helper.ViewContext.Writer
                ),
                new ViewPage<T>()
                {
                    ViewData = dict
                }
            );
        }

        /// <summary>
        /// Render a button
        /// </summary>
        /// <param name="helper"></param>
        /// <param name="type"></param>
        /// <param name="buttonText"></param>
        /// <param name="classNames"></param>
        /// <returns></returns>
        public static Button Button(this HtmlHelper helper, ButtonType type, string buttonText = null, string classNames = null)
        {
            return new Button(helper, type, buttonText, classNames);
        }

        /// <summary>
        /// Creates a new group for buttons that floats them to the right, should be called as a using() block
        /// </summary>
        /// <param name="helper"></param>
        /// <returns></returns>
        public static SubmitGroup SubmitGroup(this HtmlHelper helper)
        {
            return new SubmitGroup(helper);
        }

        /// <summary>
        /// Creates a new input field for a form.
        /// </summary>
        /// <typeparam name="TModel">Model type</typeparam>
        /// <typeparam name="TProperty">Property type</typeparam>
        /// <param name="helper"></param>
        /// <param name="field">Field to generate a form input for</param>
        /// <returns></returns>
        public static InputField<TModel, TProperty> FieldFor<TModel, TProperty>(this HtmlHelper<TModel> helper, Expression<Func<TModel, TProperty>> field)
        {
            var propInfo = field.GetPropertyInfo();

            if (propInfo == null)
            {
                throw new ArgumentException("field must be a lambda expression returning a public property");
            }

            // set defaults, fluent methods can be used to further configure them
            return new InputField<TModel, TProperty>(field, helper);
        }

        /// <summary>
        /// In case of multiple field groups on a page, this resets the group so they don't have weird alignment issues
        /// </summary>
        /// <param name="helper"></param>
        public static void ResetFields(this HtmlHelper helper)
        {
            helper.ViewData[InputField.RENDERED_COLS_MD] = 0;
            helper.ViewData[InputField.RENDERED_COLS_SM] = 0;
        }

        /// <summary>
        /// Render a checkbox for a nullable bool
        /// </summary>
        /// <typeparam name="TModel"></typeparam>
        /// <param name="htmlHelper"></param>
        /// <param name="expression"></param>
        /// <param name="htmlAttributes"></param>
        /// <returns></returns>
        public static MvcHtmlString CheckBoxFor<TModel>(this HtmlHelper<TModel> htmlHelper, Expression<Func<TModel, bool?>> expression, IDictionary<string, object> htmlAttributes)
        {
            if (expression == null)
            {
                throw new ArgumentNullException("expression");
            }

            ModelMetadata metadata = ModelMetadata.FromLambdaExpression(expression, htmlHelper.ViewData);
            bool? isChecked = null;
            if (metadata.Model != null)
            {
                if (Boolean.TryParse(metadata.Model.ToString(), out bool modelChecked))
                {
                    isChecked = modelChecked;
                }
            }

            return htmlHelper.CheckBox(ExpressionHelper.GetExpressionText(expression), isChecked.GetValueOrDefault(false), htmlAttributes);
        }


        /// <summary>
        /// Creates a new group for accordions where only one should be toggled on at any point in time, should be called as a using() block
        /// </summary>
        /// <param name="helper"></param>
        /// <returns></returns>
        public static AccordionGroup AccordionGroup(this HtmlHelper helper)
        {
            return new AccordionGroup(helper);
        }

        /// <summary>
        /// Creates a new accordion with static content, should be called as a using() block
        /// </summary>
        /// <param name="helper"></param>
        /// <param name="title">Accordion title</param>
        /// <param name="expanded">Whether or not this accordion starts expanded by default (only one Accordion of an AccordionGroup can be expanded by default)</param>
        /// <returns></returns>
        public static Accordion Accordion(this HtmlHelper helper, string title, bool expanded = false)
        {
            return new Accordion(helper, title, expanded);
        }

        /// <summary>
        /// Creates a new accordion with ajaxed-in content, should be called normally, e.g. @Html.Accordion(...)
        /// </summary>
        /// <param name="helper"></param>
        /// <param name="ajaxTarget">Controller/Action to load via ajax</param>
        /// <param name="title">Accordion title</param>
        /// <param name="expanded">Whether or not this accordion starts expanded by default (only one Accordion of an AccordionGroup can be expanded by default)</param>
        /// <returns></returns>
        public static MvcHtmlString Accordion(this HtmlHelper helper, string ajaxTarget, string title, bool expanded = false)
        {
            var acc = new Accordion(helper, title, expanded, ajaxTarget);
            acc.Dispose();
            return MvcHtmlString.Empty;
        }

        /// <summary>
        /// Does some chaining magic, can't remember what
        /// </summary>
        /// <typeparam name="TModel"></typeparam>
        /// <typeparam name="TProperty"></typeparam>
        /// <param name="helper"></param>
        /// <param name="expr"></param>
        /// <returns></returns>
        public static IChainInclude<TModel> ChainSource<TModel, TProperty>(this HtmlHelper<TModel> helper, Expression<Func<TModel, TProperty>> expr)
        {
            return new ChainInclude<TModel, TProperty>(helper, expr);
        }

        /// <summary>
        /// Generates a navigation bar. The home icon/link is inserted automatically. Using this helper is preferred over rendering the
        /// navbar yourself, as this integrates with the ASU global header to roll the nav into the hamburger menu when the screen size
        /// gets too small.
        /// </summary>
        /// <param name="helper"></param>
        /// <param name="navItems">The list of navigation items; use Html.NavLink() or Html.NavItem() to generate these.</param>
        /// <returns></returns>
        public static NavBar NavBar(this HtmlHelper helper, params INavItem[] navItems)
        {
            return new NavBar(helper, navItems);
        }

        /// <summary>
        /// Generates a navigation link (a element wrapped in an li), restricted by security.
        /// </summary>
        /// <param name="helper"></param>
        /// <param name="linkText">Text inside of link</param>
        /// <param name="actionName">Action for link to call</param>
        /// <param name="controllerName">Controller for link to call</param>
        /// <param name="flags">Security level to restrict link visibility to</param>
        /// <param name="permissions">Security permissions to restrict link visibility to</param>
        /// <returns>HTML containing the li and a element for the link, or an empty string if the user does not have permission</returns>
        public static NavLink NavLink(this HtmlHelper helper, string linkText, string actionName, string controllerName, SecurityFlags flags, params Enum[] permissions)
        {
            bool visible = true;

            if (!helper.GetUser().IsAllowed(flags, permissions))
            {
                visible = false;
            }

            return NavLinkInternal(helper, linkText, actionName, controllerName, visible);
        }

        /// <summary>
        /// Generates a navigation link (a element wrapped in an li), restricted by security.
        /// </summary>
        /// <param name="helper"></param>
        /// <param name="linkText">Text inside of link</param>
        /// <param name="actionName">Action for link to call</param>
        /// <param name="controllerName">Controller for link to call</param>
        /// <param name="roles">Roles to restrict link visibility to</param>
        /// <returns>HTML containing the li and a element for the link, or an empty string if the user does not have permission</returns>
        public static NavLink NavLink(this HtmlHelper helper, string linkText, string actionName, string controllerName, params string[] roles)
        {
            bool visible = true;

            if (roles.Length > 0 && !helper.GetUser().IsAllowed(roles))
            {
                visible = false;
            }

            return NavLinkInternal(helper, linkText, actionName, controllerName, visible);
        }

        private static NavLink NavLinkInternal(HtmlHelper helper, string linkText, string actionName, string controllerName, bool visible)
        {
            return new NavLink(helper, linkText, actionName, controllerName, visible);
        }

        /// <summary>
        /// Generates a navigation dropdown containing the specified items.
        /// </summary>
        /// <param name="helper"></param>
        /// <param name="headerText">Text for the dropdown header</param>
        /// <param name="controllerName">If the current controller is equal to this, this item is marked as active</param>
        /// <param name="dropDownItems">List of dropdown items</param>
        /// <returns>HTML containing the dropdown li, or an empty string if there are no dropdown items</returns>
        public static NavItem NavItem(this HtmlHelper helper, string headerText, string controllerName, params NavLink[] dropDownItems)
        {
            return new NavItem(helper, headerText, controllerName, dropDownItems);
        }

        /// <summary>
        /// Generates the tab bar for the page. This should only be called from full views, and only once per view.
        /// </summary>
        /// <param name="helper"></param>
        /// <param name="tabs">Tabs to place in the tab bar, use HtmlHelper.Tab() to populate this</param>
        /// <returns></returns>
        public static MvcHtmlString TabBar(this HtmlHelper helper, params MvcHtmlString[] tabs)
        {
            var items = tabs.Where(t => t != MvcHtmlString.Empty).ToList();

            if (items.Count == 0)
            {
                // user does not have any tabs on this page, redirect them to an access denied page instead
                helper.ViewContext.HttpContext.Response.Redirect("~/Error/NoPermission");
            }

            return helper.Partial("~/Views/Common/_TabBar.cshtml", tabs);
        }

        /// <summary>
        /// Generates a tab in the tab bar
        /// </summary>
        /// <param name="helper"></param>
        /// <param name="linkText">Tab name</param>
        /// <param name="actionName">Action to be loaded when tab is clicked, must return a Partial View</param>
        /// <param name="controllerName">Controller to be loaded when tab is clicked</param>
        /// <param name="flags">Security level to restrict tab visibility</param>
        /// <param name="permissions">Security permissions to restrict tab visibility</param>
        /// <returns></returns>
        public static MvcHtmlString Tab(this HtmlHelper helper, string linkText, string actionName, string controllerName, SecurityFlags flags, params Enum[] permissions)
        {
            if (!helper.GetUser().IsAllowed(flags, permissions))
            {
                return MvcHtmlString.Empty;
            }

            return TabInternal(helper, linkText, actionName, controllerName);
        }

        /// <summary>
        /// Generates a tab in the tab bar
        /// </summary>
        /// <param name="helper"></param>
        /// <param name="linkText">Tab name</param>
        /// <param name="actionName">Action to be loaded when tab is clicked, must return a Partial View</param>
        /// <param name="controllerName">Controller to be loaded when tab is clicked</param>
        /// <param name="roles">Security roles to restrict tab visibility</param>
        /// <returns></returns>
        public static MvcHtmlString Tab(this HtmlHelper helper, string linkText, string actionName, string controllerName, params string[] roles)
        {
            if (roles.Length > 0 && !helper.GetUser().IsAllowed(roles))
            {
                return MvcHtmlString.Empty;
            }

            return TabInternal(helper, linkText, actionName, controllerName);
        }

        private static MvcHtmlString TabInternal(HtmlHelper helper, string linkText, string actionName, string controllerName)
        {
            if (!helper.ViewData.ContainsKey("_tabIndex"))
            {
                helper.ViewData["_tabIndex"] = 0;
            }

            var urlHelper = new UrlHelper(helper.ViewContext.RequestContext);
            var a = new TagBuilder("a");
            a.SetInnerText(linkText);

            a.MergeAttributes(new Dictionary<string, object>
            {
                { "data-toggle", "tab" },
                { "id", String.Format("tab-{0}", helper.ViewData["_tabIndex"]) },
                { "class", "nav-link" },
                { "href", String.Format("#tabsection-{0}", helper.ViewData["_tabIndex"]) },
                { "data-ajax-url", urlHelper.Action(actionName, controllerName) },
                { "role", "tab" },
                { "aria-controls", String.Format("tabsection-{0}", helper.ViewData["_tabIndex"]) },
                { "aria-selected", "false" }
            });

            helper.ViewData["_tabIndex"] = (int)helper.ViewData["_tabIndex"] + 1;

            return new MvcHtmlString(String.Format(@"<li class=""nav-item tab static-tab"">{0}</li>", a.ToString()));
        }

        /// <summary>
        /// Adds a button which generates dynamic tabs
        /// </summary>
        /// <param name="helper"></param>
        /// <param name="buttonText">Button text</param>
        /// <param name="tabName">Tab name (shown in bar)</param>
        /// <param name="actionName">Action to be loaded, must return a Partial View</param>
        /// <param name="controllerName">Controller to be loaded</param>
        /// <param name="buttonType">Type of button, impacts visual styling</param>
        /// <param name="behavior">Behavior of new tabs, either an existing tab can be switched to if possible or a new tab can always be opened</param>
        /// <param name="flags">Security level to restrict button visibility</param>
        /// <param name="permissions">Security permissions to restrict button visibility</param>
        /// <returns></returns>
        public static MvcHtmlString AddTabButton(this HtmlHelper helper, string buttonText, string tabName, string actionName, string controllerName, ButtonType buttonType, AddTabBehavior behavior, SecurityFlags flags, params Enum[] permissions)
        {
            if (!helper.GetUser().IsAllowed(flags, permissions))
            {
                return MvcHtmlString.Empty;
            }

            return AddTabButtonInternal(helper, buttonText, tabName, actionName, controllerName, buttonType, behavior);
        }

        /// <summary>
        /// Adds a button which generates dynamic tabs
        /// </summary>
        /// <param name="helper"></param>
        /// <param name="buttonText">Button text</param>
        /// <param name="tabName">Tab name (shown in bar)</param>
        /// <param name="actionName">Action to be loaded, must return a Partial View</param>
        /// <param name="controllerName">Controller to be loaded</param>
        /// <param name="buttonType">Type of button, impacts visual styling</param>
        /// <param name="behavior">Behavior of new tabs, either an existing tab can be switched to if possible or a new tab can always be opened</param>
        /// <param name="roles">Security roles to restrict button visibility</param>
        /// <returns></returns>
        public static MvcHtmlString AddTabButton(this HtmlHelper helper, string buttonText, string tabName, string actionName, string controllerName, ButtonType buttonType, AddTabBehavior behavior, params string[] roles)
        {
            if (roles.Length > 0 && !helper.GetUser().IsAllowed(roles))
            {
                return MvcHtmlString.Empty;
            }

            return AddTabButtonInternal(helper, buttonText, tabName, actionName, controllerName, buttonType, behavior);
        }

        private static MvcHtmlString AddTabButtonInternal(HtmlHelper helper, string buttonText, string tabName, string actionName, string controllerName, ButtonType buttonType, AddTabBehavior behavior)
        {
            string btnClass = Mvc.Button.GetButtonClass(buttonType);
            string addTabType;

            switch (behavior)
            {
                case AddTabBehavior.AlwaysAdd:
                    addTabType = "always";
                    break;
                default: // AddTabBehavior.SwitchExisting
                    addTabType = "switch";
                    break;
            }

            return new MvcHtmlString(String.Format(@"<button class=""btn {0}"" type=""button"" data-addtab=""{1}"" data-addtab-action=""{2}/{3}"" data-addtab-name=""{4}"">{5}</button>",
                btnClass, addTabType, helper.AttributeEncode(controllerName), helper.AttributeEncode(actionName), helper.AttributeEncode(tabName), helper.Encode(buttonText)));
        }

        /// <summary>
        /// Adds page-specific css. This is called by _Layout and does not need to be called unless not using _Layout.
        /// </summary>
        /// <param name="helper"></param>
        [Obsolete("Use RenderPageStyles instead, as that does not interfere with bundling")]
        public static void AddPageStyles(this HtmlHelper helper)
        {
            string controller = helper.ViewContext.RouteData.GetRequiredString("controller");
            string action = helper.ViewContext.RouteData.GetRequiredString("action");

            BundleTable.Bundles.GetBundleFor("~/Content/css").Include(String.Format("~/Content/ViewStyles/{0}/{1}.css", controller, action));
        }

        /// <summary>
        /// Adds page-specific css. This is called by _Layout and does not need to be called unless not using _Layout.
        /// </summary>
        /// <param name="helper"></param>
        public static IHtmlString RenderPageStyles(this HtmlHelper helper)
        {
            var controller = helper.ViewContext.RouteData.GetRequiredString("controller");
            var action = helper.ViewContext.RouteData.GetRequiredString("action");
            var virtualPath = String.Format("~/Content/ViewStyles/{0}/{1}.css", controller, action);
            var realPath = helper.ViewContext.HttpContext.Request.MapPath(virtualPath);

            if (!File.Exists(realPath))
            {
                return MvcHtmlString.Empty;
            }

            return Scripts.Render(virtualPath);
        }

        /// <summary>
        /// Adds page-specific javascript. This is called by _Layout and does not need to be called unless not using _Layout.
        /// </summary>
        /// <param name="helper"></param>
        [Obsolete("Use RenderPageScripts instead, as that does not interfere with bundling")]
        public static void AddPageScripts(this HtmlHelper helper)
        {
            string controller = helper.ViewContext.RouteData.GetRequiredString("controller");
            string action = helper.ViewContext.RouteData.GetRequiredString("action");

            BundleTable.Bundles.GetBundleFor("~/bundles/Site").Include(String.Format("~/Scripts/ViewScripts/{0}/{1}.js", controller, action));
        }

        /// <summary>
        /// Adds page-specific javascript. This is called by _Layout and does not need to be called unless not using _Layout.
        /// </summary>
        /// <param name="helper"></param>
        public static IHtmlString RenderPageScripts(this HtmlHelper helper)
        {
            var controller = helper.ViewContext.RouteData.GetRequiredString("controller");
            var action = helper.ViewContext.RouteData.GetRequiredString("action");
            var virtualPath = String.Format("~/Scripts/ViewScripts/{0}/{1}.js", controller, action);
            var realPath = helper.ViewContext.HttpContext.Request.MapPath(virtualPath);

            if (!File.Exists(realPath))
            {
                return MvcHtmlString.Empty;
            }

            return Scripts.Render(virtualPath);
        }

        /// <summary>
        /// Creates a new standard form, should be called as a using() block. Use this instead of calling BeginForm() directly.
        /// </summary>
        /// <param name="helper"></param>
        /// <param name="actionName">Action to submit the form to (default is current page's action)</param>
        /// <param name="controllerName">Controller to submit the form to (default is current page's controller)</param>
        /// <param name="horizontal">Whether or not to make the form a horizontal form</param>
        /// <returns></returns>
        public static MvcForm StandardForm(this HtmlHelper helper, string actionName = null, string controllerName = null, bool horizontal = false)
        {
            if (actionName == null)
            {
                actionName = (string)helper.ViewContext.RouteData.Values["action"];
                controllerName = (string)helper.ViewContext.RouteData.Values["controller"];
            }
            else if (controllerName == null)
            {
                controllerName = (string)helper.ViewContext.RouteData.Values["controller"];
            }

            var htmlAttrs = new Dictionary<string, object>();

            if (horizontal)
            {
                helper.ViewData[InputField.IS_HORIZ_FORM] = true;
                htmlAttrs["class"] = "form-horizontal";
            }
            else
            {
                helper.ViewData[InputField.IS_HORIZ_FORM] = false;
            }

            helper.ViewData[InputField.RENDERED_COLS_SM] = 0;
            helper.ViewData[InputField.RENDERED_COLS_MD] = 0;

            var form = helper.BeginForm(actionName, controllerName, FormMethod.Post, htmlAttrs);
            // write the antiforgerytoken field
            helper.ViewContext.Writer.Write(helper.AntiForgeryToken());

            return form;
        }

        /// <summary>
        /// Creates a new ajax-submitted form, should be called as a using() block
        /// </summary>
        /// <param name="helper"></param>
        /// <param name="actionName">Action to submit the form to (default is current page's action)</param>
        /// <param name="controllerName">Controller to submit the form to (default is current page's controller)</param>
        /// <param name="horizontal">Whether or not to make the form a horizontal form</param>
        /// <returns></returns>
        public static MvcForm AjaxForm(this HtmlHelper helper, string actionName = null, string controllerName = null, bool horizontal = false)
        {
            if (actionName == null)
            {
                actionName = (string)helper.ViewContext.RouteData.Values["action"];
                controllerName = (string)helper.ViewContext.RouteData.Values["controller"];
            }
            else if (controllerName == null)
            {
                controllerName = (string)helper.ViewContext.RouteData.Values["controller"];
            }

            var ajaxAttrs = new Dictionary<string, object>()
            {
                { "data-ajaxform", "true" }
            };

            if (horizontal)
            {
                helper.ViewData[InputField.IS_HORIZ_FORM] = true;
                ajaxAttrs["class"] = "form-horizontal";
            }
            else
            {
                helper.ViewData[InputField.IS_HORIZ_FORM] = false;
            }

            helper.ViewData[InputField.RENDERED_COLS_SM] = 0;
            helper.ViewData[InputField.RENDERED_COLS_MD] = 0;

            var form = helper.BeginForm(actionName, controllerName, FormMethod.Post, ajaxAttrs);

            return form;
        }

        /// <summary>
        /// Generates a header for a modal window
        /// </summary>
        /// <param name="helper"></param>
        /// <param name="headerText">Text to show in the header</param>
        /// <returns></returns>
        [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Provides a more accessible API if we expose this as an extension method on HtmlHelper")]
        public static MvcHtmlString ModalHeader(this HtmlHelper helper, string headerText)
        {
            return new MvcHtmlString(String.Format(@"
<div class=""modal-header"">
    <button type=""button"" class=""close"" data-dismiss=""modal"" aria-label=""Close"">
        <span aria-hidden=""true"" class=""fa fa-close""></span>
    </button>
    <h4 class=""modal-title"">{0}</h4>
</div>", HttpUtility.HtmlEncode(headerText)));
        }

        /// <summary>
        /// Generates the body of a modal window (wrap in using block)
        /// </summary>
        /// <param name="helper"></param>
        /// <returns></returns>
        public static DisposableWrapper ModalBody(this HtmlHelper helper)
        {
            return new DisposableWrapper(
                () => helper.ViewContext.Writer.WriteLine(@"<div class=""modal-body""><div class=""container-fluid"">"),
                () => helper.ViewContext.Writer.WriteLine(@"</div></div>")
            );
        }

        /// <summary>
        /// Generates the footer of a modal window (wrap in using block)
        /// </summary>
        /// <param name="helper"></param>
        /// <returns></returns>
        public static DisposableWrapper ModalFooter(this HtmlHelper helper)
        {
            return new DisposableWrapper(
                () => helper.ViewContext.Writer.WriteLine(@"<div class=""modal-footer"">"),
                () => helper.ViewContext.Writer.WriteLine(@"</div>")
            );
        }
    }
}

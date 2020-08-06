using System.Web.Optimization;
using System.Web.Mvc;
using BTS.Common.Mvc;

[assembly: WebActivatorEx.PostApplicationStartMethod(typeof($rootnamespace$.App_Start.BTSCommonBundleConfig), "RegisterBundles")]

namespace $rootnamespace$.App_Start
{
    public class BTSCommonBundleConfig
    {
        public static void RegisterBundles()
        {
            // allows loading embedded resources via filesystem, e.g. PartialView("~/Common/_TabBar")
            EmbeddedPathProvider.Register("Common");

            // When <compilation debug="true" />, MVC will render the full readable version. When set to <compilation debug="false" />, the minified version will be rendered automatically
            BundleTable.Bundles.Add(new StyleBundle("~/Content/Common").Include(
                "~/Common/Content/spinner.css",
                "~/Common/Content/Common.css"));
            BundleTable.Bundles.Add(new ScriptBundle("~/bundles/Common").Include(
                "~/Common/Scripts/Common.js",
                "~/Common/Scripts/Chain.js",
                "~/Common/Scripts/Grid.js",
                "~/Common/Scripts/Modal.js",
                "~/Common/Scripts/Tab.js"));
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace SUNAward
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                name: "AwardPreview",
                url: "Award/Preview",
                defaults: new { controller = "Award", action = "Preview" }
            );

            routes.MapRoute(
                name: "AwardIndex",
                url: "Award/{id}",
                defaults: new { controller = "Award", action = "Index", id = UrlParameter.Optional }
            );

            // legacy award endpoint, allow old links in email to still work
            routes.MapRoute(
                name: "LegacyAward",
                url: "SUNAward.aspx",
                defaults: new { controller = "Award", action = "Index" }
            );

            // legacy form endpoint
            routes.MapRoute(
                name: "LegacyForm",
                url: "AwardForm.aspx",
                defaults: new { controller = "Home", action = "RedirectToIndex" }
            );

            // if someone hits refresh on the angular preview page, redirect them back to the main form
            routes.MapRoute(
                name: "AngularPreview",
                url: "preview",
                defaults: new { controller = "Home", action = "RedirectToIndex" }
            );

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}

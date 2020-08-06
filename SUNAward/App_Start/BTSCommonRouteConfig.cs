using System.Web.Routing;
using System.Web.Mvc;
using BTS.Common.Mvc;

[assembly: WebActivatorEx.PostApplicationStartMethod(typeof(SUNAward.App_Start.BTSCommonRouteConfig), "RegisterRoutes")]

namespace SUNAward.App_Start
{
    public class BTSCommonRouteConfig
    {
        public static void RegisterRoutes()
        {
            // allows loading embedded resources via web requests
            RouteTable.Routes.MapRoute(
                name: "Common",
                url: "Common/{*path}",
                defaults: new { controller = "Resource", action = "Resource" },
                namespaces: new string[] { "BTS.Common.Mvc.Controllers" }
            );

            RouteTable.Routes.MapRoute(
                name: "Session",
                url: "Session/{action}",
                defaults: new { controller = "Session", action = "Index" },
                namespaces: new string[] { "BTS.Common.Mvc.Controllers" }
            );

            RouteTable.Routes.MapRoute(
                name: "Grid",
                url: "Grid/{action}/{id}",
                defaults: new { controller = "Grid", action = "Render", id = UrlParameter.Optional },
                namespaces: new string[] { "BTS.Common.Mvc.Controllers" }
            );

            // ensure default route is last
            var defaultRoute = RouteTable.Routes["Default"];
            if (defaultRoute != null)
            {
                RouteTable.Routes.Remove(defaultRoute);
                RouteTable.Routes.Add(defaultRoute);
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Security.Principal;
using System.Web.Mvc;
using System.Web.Caching;

using BTS.Common.Mvc.Grid;

namespace BTS.Common.Mvc.Controllers
{
    /// <summary>
    /// Shared controller to render grids
    /// </summary>
    public class GridController : Controller
    {
        private static string GetCacheKey(string name, IPrincipal user)
        {
            return String.Format("gridcache__u_{0}__g_{1}", user.Identity.Name, name);
        }

        /// <summary>
        /// Retrieves a cached grid
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static IGrid GetFromCache(string name)
        {
            var context = System.Web.HttpContext.Current;
            var key = GetCacheKey(name, context.User);

            return context.Cache[key] as IGrid;
        }

        /// <summary>
        /// Adds a grid to the cache
        /// </summary>
        /// <param name="name"></param>
        /// <param name="grid"></param>
        public static void AddToCache(string name, IGrid grid)
        {
            if (String.IsNullOrEmpty(name))
            {
                throw new ArgumentException("name cannot be null or empty", "name");
            }

            if (grid == null)
            {
                throw new ArgumentNullException("grid");
            }

            var context = System.Web.HttpContext.Current;
            var key = GetCacheKey(name, context.User);
            context.Cache.Insert(key, grid, null, Cache.NoAbsoluteExpiration, TimeSpan.FromHours(4));
        }

        /// <summary>
        /// Renders a grid from cache
        /// </summary>
        /// <param name="id"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public PartialViewResult Render(string id, IGridOptions options)
        {
            // get grid from cache based on id (the string grid name)
            var cachedGrid = GetFromCache(id);

            if (cachedGrid == null)
            {
                throw new InvalidOperationException("Grid is not in cache");
            }

            return this.RenderGrid(cachedGrid, options);
        }
    }
}
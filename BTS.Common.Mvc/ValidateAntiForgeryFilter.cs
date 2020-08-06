using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace BTS.Common.Mvc
{
    /// <summary>
    /// Global CSRF check for actions which require security
    /// </summary>
    public class ValidateAntiForgeryFilter : IFilterProvider
    {
        /// <summary>
        /// If an action requires security, hook up a CSRF check
        /// </summary>
        /// <param name="controllerContext"></param>
        /// <param name="actionDescriptor"></param>
        /// <returns></returns>
        public IEnumerable<Filter> GetFilters(ControllerContext controllerContext, ActionDescriptor actionDescriptor)
        {
            List<Filter> result = new List<Filter>();

            // If a POST action requires security, it also requires a CSRF token
            // A SkipValidationAttribute can override this behavior if need be, as long as it's skipping AntiForgery validation
            if (String.Equals(controllerContext.HttpContext.Request.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase)
                && actionDescriptor.GetCustomAttributes(typeof(AuthorizeAttribute), true).Any())
            {
                result.Add(new Filter(new ValidateAntiForgeryTokenAttribute(), FilterScope.Action, null));
            }

            if (String.Equals(controllerContext.HttpContext.Request.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase)
                && actionDescriptor.ControllerDescriptor.GetCustomAttributes(typeof(AuthorizeAttribute), true).Any())
            {
                result.Add(new Filter(new ValidateAntiForgeryTokenAttribute(), FilterScope.Controller, null));
            }

            return result;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace BTS.Common.Mvc
{
    /// <summary>
    /// Applied as a global filter in order to support the ajaxReady event in javascript.
    /// If a request is redirected via HTTP 302 or somesuch, the XmlHttpRequest object has no way of determining the final URI.
    /// Adding a response header stating the final requested resource fixes this issue nicely.
    /// </summary>
    public class RequestUrlHeaderAttribute : ActionFilterAttribute
    {
        /// <summary>
        /// Add response header
        /// </summary>
        /// <param name="filterContext"></param>
        public override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            filterContext.HttpContext.Response.Headers.Add("X-Request-Url", filterContext.HttpContext.Request.Url.AbsolutePath);
        }
    }
}

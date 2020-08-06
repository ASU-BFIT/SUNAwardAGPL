using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Helpers;
using System.Web.Mvc;
using System.Reflection;

namespace BTS.Common.Mvc
{
    /// <summary>
    /// Applied as a global filter to support submitting AJAX requests. Since each call to HtmlHelper.AntiForgeryToken() resets the valid token,
    /// we instead need to maintain a global token that is applicable to all forms and is guaranteed to be refreshed each time an ajax call completes.
    /// This filter combined with clientside javascript accomplishes that by reading the new header and then submitting it with each POST request.
    /// </summary>
    public class AntiForgeryTokenHeaderAttribute : ActionFilterAttribute
    {
        /// <summary>
        /// Add CSRF token to the response header
        /// </summary>
        /// <param name="filterContext"></param>
        public override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            // don't generate a token on anon requests
            // sometimes the user identity is still stubbed. I've traced this to mean we're either logging the user out (DeferredSessionStore.DoGetSession
            // is clearing the user), but requests to /Common resources seem to also keep the user stubbed.
            var identity = filterContext.HttpContext.User.Identity;
            if (!identity.IsAuthenticated || identity.AuthenticationType == "DeferredStub")
            {
                return;
            }

            // we want to get at the AntiForgeryWorker, however this is a private member of AntiForgery
            object worker = typeof(AntiForgery).GetField("_worker", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
            // now we want to call GetFormInputElement to get the form token with side effects (e.g. setting request headers and cookies)
            TagBuilder tag = (TagBuilder)worker.GetType().GetMethod("GetFormInputElement").Invoke(worker, new object[] { filterContext.HttpContext });

            filterContext.HttpContext.Response.Headers.Add("X-AntiForgeryToken", tag.Attributes["value"]);
        }
    }
}

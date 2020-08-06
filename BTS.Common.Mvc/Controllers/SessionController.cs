using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Xml;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.Owin;
using Microsoft.Owin.Security;

using BTS.Common.CAS;

using CasConstants = BTS.Common.CAS.Constants;

namespace BTS.Common.Mvc.Controllers
{
    /// <summary>
    /// Shared controller to handle sign-in/sign-out
    /// </summary>
    [AllowAnonymous]
    public class SessionController : Controller
    {
        /// <summary>
        /// Path to application-specific login page
        /// </summary>
        internal static PathString AppLoginPath { get; set; }

        /// <summary>
        /// No-op
        /// </summary>
        /// <returns></returns>
        public ActionResult Index()
        {
            return new EmptyResult();
        }

        /// <summary>
        /// Attempt CAS authentication
        /// </summary>
        /// <param name="returnUrl">URL to redirect user to after login is complete</param>
        /// <returns></returns>
        [HttpGet]
        public ActionResult Login(string returnUrl)
        {
            return new ChallengeResult(CasConstants.CAS, returnUrl);
        }

        /// <summary>
        /// No-op; this method is never directly called as our OWIN middleware
        /// hijacks it before we get to this point. It exists just to prevent errors from cropping
        /// up client-side in the event that OWIN was not set up correctly.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ActionResult Validate()
        {
            return new EmptyResult();
        }

        /// <summary>
        /// Handles Single Sign-Out requests by clearing our session.
        /// This is called by CAS via AJAX so the user never sees the response; as such,
        /// we don't bother sending one (CAS just cares that we get a 200 OK).
        /// </summary>
        /// <param name="logoutRequest"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Validate(string logoutRequest)
        {
            string ticket = null;
            var authManager = HttpContext.GetOwinContext().Authentication;
            var casId = authManager.User.Identities.SingleOrDefault(o => o.AuthenticationType == CasConstants.CAS);

            if (casId == null)
            {
                // no longer signed in; session may have expired by itself on our side
                return new EmptyResult();
            }

            using (var reader = XmlReader.Create(new MemoryStream(Encoding.UTF8.GetBytes(logoutRequest))))
            {
                // we may want to do further validation here, such as verifying
                // this came from the registered CAS server's IP, or verifying that
                // the IssueInstant is within allowable skew. However, since this is guarding
                // a logout, the worst security issue that can occur is forcing a minor DOS in the sense
                // that the user will renew their SSO session next time they load a page
                // (meaning longer pageload times). In all, not a huge deal.
                reader.MoveToContent();
                if (reader.ReadToDescendant("samlp:SessionIndex"))
                {
                    ticket = reader.ReadElementContentAsString();
                }
            }

            if (!String.IsNullOrEmpty(ticket) && casId.FindFirst(CasConstants.SERVICE_TICKET).Value == ticket)
            {
                Session.Clear();
                Session.Abandon();
                authManager.SignOut();
            }

            return new EmptyResult();
        }

        /// <summary>
        /// Indicates that a client should re-authenticate, likely due to an expired session.
        /// On success, a message is displayed to alert the user to close this tab and re-try their previous action.
        /// This is typically not necessary unless using AJAX in an application (as we cannot re-auth as part of an AJAX request).
        /// </summary>
        /// <returns></returns>
        [Authorize]
        public ActionResult Reauth()
        {
            return View();
        }

        private class ChallengeResult : HttpUnauthorizedResult
        {
            public string LoginProvider { get; set; }
            public string RedirectUri { get; set; }

            public ChallengeResult(string provider, string redirectUri)
            {
                LoginProvider = provider;
                RedirectUri = redirectUri;
            }

            public override void ExecuteResult(ControllerContext context)
            {
                var properties = new AuthenticationProperties()
                {
                    RedirectUri = RedirectUri
                };

                context.HttpContext.GetOwinContext().Authentication.Challenge(properties, LoginProvider);
            }
        }
    }
}
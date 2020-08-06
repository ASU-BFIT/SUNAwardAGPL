using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Security.Claims;

using Owin;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Infrastructure;
using Microsoft.Owin.Security.DataHandler;
using Microsoft.Owin.Security.DataProtection;
using Microsoft.Owin.Security.Cookies;

using BTS.Common.CAS;
using System.Diagnostics.CodeAnalysis;

namespace BTS.Common.Web
{
    /// <summary>
    /// A session store using ASP.NET sessions which allows for setting up a session stub
    /// early in the authentication process, and then filling in the remaining session details
    /// once ASP.NET sessions are fully initialized. This session store is not fully compatible
    /// with the &lt;authorize&gt; section in Web.config -- restricting authorization to * or ?
    /// is supported, but specifying individual groups is not. Application code should do the group
    /// filtering instead.
    /// </summary>
    public class DeferredSessionStore : IAuthenticationSessionStore
    {
        private List<(DeferredAction action, CasAuthenticationSession session)> DeferredSessions { get; set; }
        private TicketDataFormat Format { get; set; }

        /// <summary>
        /// How long each session lasts before it expires, in minutes.
        /// Sliding expiration is used, so a session could last longer than this.
        /// </summary>
        public int Expiration { get; set; }

        /// <summary>
        /// Constructs a new DeferredSessionStore
        /// </summary>
        /// <param name="app"></param>
        /// <param name="expiration"></param>
        public DeferredSessionStore(IAppBuilder app, int expiration)
        {
            DeferredSessions = new List<(DeferredAction action, CasAuthenticationSession session)>();
            Format = new TicketDataFormat(app.CreateDataProtector("BTS.Common.Web", "AuthenticationTicket"));
            Expiration = expiration;
        }

        /// <summary>
        /// Destroys a session from the session store
        /// </summary>
        /// <param name="key"></param>
        public void Remove(string key)
        {
            DeferredSessions.Add((DeferredAction.Remove,
                new CasAuthenticationSession() { SessionId = key }));
        }

        /// <summary>
        /// Asynchronously destroys a session from the session store
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public async Task RemoveAsync(string key)
        {
            await Task.Run(() => Remove(key));
        }

        /// <summary>
        /// Extends the expiration time of a session in the session store
        /// </summary>
        /// <param name="key"></param>
        /// <param name="ticket"></param>
        [SuppressMessage("Style", "CA1801:Review unused parameters",
            Justification = "key parameter is required for IAuthenticationSessionStore implementation")]
        public void Renew(string key, AuthenticationTicket ticket)
        {
            if (ticket == null)
            {
                throw new ArgumentNullException(nameof(ticket));
            }

            DeferredSessions.Add((DeferredAction.Renew,
                new CasAuthenticationSession() { SessionId = ticket.Properties.Dictionary[Constants.SERVICE_TICKET] }));
        }

        /// <summary>
        /// Asynchronously extends the expiration time of a session in the session store
        /// </summary>
        /// <param name="key"></param>
        /// <param name="ticket"></param>
        /// <returns></returns>
        public async Task RenewAsync(string key, AuthenticationTicket ticket)
        {
            await Task.Run(() => Renew(key, ticket));
        }

        /// <summary>
        /// Fetches a session from the session store
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public AuthenticationTicket Retrieve(string key)
        {
            var session = new CasAuthenticationSession()
            {
                Data = null,
                Expires = DateTime.Now.AddMinutes(60),
                SessionId = key
            };

            DeferredSessions.Add((DeferredAction.Get, session));

            return new AuthenticationTicket(GetStubIdentity(key), new AuthenticationProperties());
        }

        /// <summary>
        /// Asynchronously fetches a session from the session store
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public async Task<AuthenticationTicket> RetrieveAsync(string key)
        {
            return await Task.Run(() => Retrieve(key));
        }

        /// <summary>
        /// Writes session data to the session store
        /// </summary>
        /// <param name="ticket"></param>
        /// <returns></returns>
        public string Store(AuthenticationTicket ticket)
        {
            if (ticket == null)
            {
                throw new ArgumentNullException(nameof(ticket));
            }

            var session = new CasAuthenticationSession()
            {
                SessionId = ticket.Properties.Dictionary[Constants.SERVICE_TICKET],
                Expires = DateTime.Now.AddMinutes(Expiration)
            };

            ticket.Properties.ExpiresUtc = session.Expires;
            session.Data = Format.Protect(ticket);

            DeferredSessions.Add((DeferredAction.Store, session));

            return session.SessionId;
        }

        /// <summary>
        /// Asynchronously writes session data to the session store
        /// </summary>
        /// <param name="ticket"></param>
        /// <returns></returns>
        public async Task<string> StoreAsync(AuthenticationTicket ticket)
        {
            return await Task.Run(() => Store(ticket));
        }

        private static string GetSessionKey(string sid)
        {
            return $"DeferredSession-{sid}";
        }

        private static ClaimsIdentity GetStubIdentity(string key)
        {
            var id = new ClaimsIdentity("DeferredStub");
            id.AddClaim(new Claim(ClaimTypes.NameIdentifier, key));
            id.AddClaim(new Claim(ClaimTypes.Name, key));
            id.AddClaim(new Claim(Constants.IDENTITY_CLAIM, "DeferredSession"));

            return id;
        }

        private static ClaimsIdentity GetLoggedOutIdentity()
        {
            var id = new ClaimsIdentity();
            id.AddClaim(new Claim(ClaimTypes.NameIdentifier, "Anonymous"));
            id.AddClaim(new Claim(ClaimTypes.Name, "Anonymous"));
            id.AddClaim(new Claim(Constants.IDENTITY_CLAIM, "DeferredSession"));

            return id;
        }

        private void DoGetSession(CasAuthenticationSession session, HttpContext context)
        {
            var owinContext = context.GetOwinContext();
            var helper = new SecurityHelper(owinContext);
            var stubUser = owinContext.Request.User;

            // don't have a user stub but rather some other user object?
            // if so, some other authentication was used, so don't do anything to the user object
            if (stubUser?.Identity?.AuthenticationType != "DeferredStub")
            {
                return;
            }

            // validate that we're unstubbing the correct user
            // note that if we don't validate, we log the user out
            if (stubUser.Identity.Name != session.SessionId)
            {
                Logout(context, owinContext);
                return;
            }

            // grab out protected ticket from session store
            var ciphertext = (string)context.Session[GetSessionKey(session.SessionId)];
            if (String.IsNullOrEmpty(ciphertext))
            {
                Logout(context, owinContext);
                return;
            }

            var ticket = Format.Unprotect(ciphertext);

            // check expiration
            if (ticket.Properties.ExpiresUtc.HasValue && ticket.Properties.ExpiresUtc.Value < DateTime.UtcNow)
            {
                Logout(context, owinContext);
                return;
            }

            // everything is good, initialize the user
            owinContext.Request.User = null;
            helper.AddUserIdentity(ticket.Identity);

            if (ticket.Properties.Dictionary.TryGetValue(Constants.IDENTITY_PROP, out string otherIds)
                && !String.IsNullOrEmpty(otherIds))
            {
                var list = IdentityList.Deserialize(otherIds);

                if (list != null)
                {
                    foreach (var id in list.Items)
                    {
                        helper.AddUserIdentity(id);
                    }
                }
            }

            // fix up our HttpContext user
            context.User = owinContext.Request.User;
            Thread.CurrentPrincipal = owinContext.Request.User;
        }

        private static void DoRemoveSession(CasAuthenticationSession session, HttpContext context)
        {
            context.Session.Remove(GetSessionKey(session.SessionId));
        }

        private void DoRenewSession(CasAuthenticationSession session, HttpContext context)
        {
            var ciphertext = (string)context.Session[GetSessionKey(session.SessionId)];
            if (String.IsNullOrEmpty(ciphertext))
            {
                return;
            }

            var ticket = Format.Unprotect(ciphertext);
            ticket.Properties.ExpiresUtc = session.Expires;
            context.Session[GetSessionKey(session.SessionId)] = Format.Protect(ticket);
        }

        private static void DoStoreSession(CasAuthenticationSession session, HttpContext context)
        {
            context.Session[GetSessionKey(session.SessionId)] = session.Data;
        }

        /// <summary>
        /// Executes all deferred actions. This should only be called
        /// after ASP.NET sessions are initialized. In the event that we attempt
        /// to retrieve an invalid session, this will log the user out.
        /// </summary>
        /// <param name="context">HttpContext for the current request.</param>
        public void RunDeferredActions(HttpContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            // it's a for loop because we may add things to the end of the list while iterating
            // (such as when we cause a logout above)
            for (var i = 0; i < DeferredSessions.Count; i++)
            {
                var (action, session) = DeferredSessions[i];

                switch (action)
                {
                    case DeferredAction.Get:
                        DoGetSession(session, context);
                        break;
                    case DeferredAction.Remove:
                        DoRemoveSession(session, context);
                        break;
                    case DeferredAction.Renew:
                        DoRenewSession(session, context);
                        break;
                    case DeferredAction.Store:
                        DoStoreSession(session, context);
                        break;
                }
            }

            DeferredSessions.Clear();
        }

        private static void Logout(HttpContext context, IOwinContext owinContext)
        {
            var anonId = new ClaimsPrincipal(GetLoggedOutIdentity());

            context.Session.Clear();
            context.Session.Abandon();
            owinContext.Authentication.SignOut();
            // don't smash the owin user, as we need it intact to correctly clear the sign-in cookie
            context.User = anonId;
            Thread.CurrentPrincipal = anonId;
        }

        private enum DeferredAction
        {
            Get,
            Remove,
            Renew,
            Store
        }
    }
}

using System;
using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using BTS.Common.AD;

namespace BTS.Common.CAS
{
    /// <summary>
    /// CAS-specific authentication options
    /// </summary>
    public class CasAuthenticationOptions : AuthenticationOptions
    {
        /// <summary>
        /// Set up default options
        /// </summary>
        public CasAuthenticationOptions()
            : base(Constants.CAS)
        {
            var providerProperties = new Dictionary<string, object>
            {
                { "SupportedProtocolVersions", new int[] { 1, 2, 3 } },
                { "Gateway", true },
                { "Renew", true },
                { "Proxy", true },
                { "Attributes", true },
                { "CustomAttributes", false }
            };

            AuthenticationMode = AuthenticationMode.Passive;
            Description = new AuthenticationDescription(providerProperties)
            {
                AuthenticationType = Constants.CAS,
                Caption = "CAS"
            };
        }
        /// <summary>
        /// Version of the CAS protocol to use (1, 2, or 3).
        /// Using either 2 or 3 is recommended, default 2.
        /// </summary>
        public int CasVersion { get; set; } = 2;

        /// <summary>
        /// URL to CAS instance.
        /// Default https://weblogin.asu.edu/cas/
        /// </summary>
        public Uri CasUrlBase { get; set; } = new Uri("https://weblogin.asu.edu/cas/");

        /// <summary>
        /// Location to redirect client to after authentication.
        /// This path is used internally, and application code on this path is never called.
        /// It is usually not necessary to change this from the default. If you wish to run
        /// application code after authentication, set LoginPath instead.
        /// </summary>
        public PathString CallbackPath { get; set; } = new PathString("/Session/Validate");

        /// <summary>
        /// Location to redirect client to after authentication. The client is first
        /// redirected to CallbackPath in order to set up the user principal, and then is redirected
        /// here afterwards if this is set. This path will get a ReturnUrl query parameter so that
        /// it can further redirect the user to their original destination. If left default, the client
        /// will be redirected directly to the location specified in ReturnUrl.
        /// </summary>
        public PathString LoginPath { get; set; }

        /// <summary>
        /// Location to redirect client to in the event that they have signed onto CAS
        /// but do not have the correct role permissions to view the page. If null,
        /// a 403 will be issued if the user does not have permissions.
        /// </summary>
        public PathString NoPermsPath { get; set; } = new PathString("/Error/NoPermission");

        /// <summary>
        /// If true, requires that the user re-enters their credentials
        /// instead of relying on existing SSO sessions.
        /// Default is false.
        /// </summary>
        public bool Renew { get; set; }

        /// <summary>
        /// If true, will not prompt the user for credentials even if they
        /// do not have an SSO session. The CallbackPath will be called
        /// without any ticket parameter.
        /// Default is false.
        /// </summary>
        public bool Gateway { get; set; }

        /// <summary>
        /// If true, indicates that this application should act as a proxy server
        /// and request a Proxy Granting Ticket to authenticate other apps that
        /// this application proxies to. To get a Session Ticket for a proxied
        /// application, call GetProxySession() on the authentication context.
        /// Default is false.
        /// </summary>
        public bool ProxyServer { get; set; }

        /// <summary>
        /// Path used to retrive the proxy granting ticket.
        /// This path should exist and allow unauthenticated access, but return no data.
        /// </summary>
        public PathString ProxyCallback { get; set; } = new PathString("/Session/Proxy");

        /// <summary>
        /// If true, indicates the application receives a proxy ticket instead
        /// of a service ticket (e.g. we're receiving auth info from a proxy
        /// rather than CAS itself). This lets us know to hit the proxy validation
        /// endpoints rather than the regular service ticket validation endpoints.
        /// If true, TrustedProxies MUST be set and all proxies giving us the ticket
        /// MUST be present in the list. Can be set to true even if not using a proxy.
        /// Default is false.
        /// </summary>
        public bool ProxyClient { get; set; }

        /// <summary>
        /// Trusted proxy servers. If we are acting as a proxy client, we verify
        /// all parent proxies appear in this list. If they do not, we refuse to
        /// authenticate the request.
        /// </summary>
        public HashSet<string> TrustedProxies { get; } = new HashSet<string>();

        /// <summary>
        /// Protocol version 3 only:
        /// What HTTP method should be used when redirecting the client to
        /// the CallbackPath.
        /// Default is null, meaning this parameter is excluded from the request
        /// (which then causes CAS to default to GET)
        /// </summary>
        public string Method { get; set; }

        /// <summary>
        /// AuthenticationType of the middleware used to actually handle the sign in.
        /// The default is the default signinas type set for the application.
        /// </summary>
        public string SignInAsAuthenticationType { get; set; }

        /// <summary>
        /// Underlying session store used to persist our session data. This should be
        /// the same object as the session store passed to the cookie middleware,
        /// or null if not using the cookie middleware.
        /// Required for proper Single Log Out (SLO) functionality.
        /// </summary>
        public IAuthenticationSessionStore SessionStore { get; set; }

        /// <summary>
        /// Callback called when an authentication occurs, to allow for additional
        /// things to happen on successful authentication (such as retrieving group
        /// information from Active Directory). By default this loads AD info.
        /// </summary>
        public Action<CasAuthenticationContext> OnAuthenticated { get; set; }
            = ctx => ctx.OtherIdentities.Add(ActiveDirectory.GetUserInfo(ctx.CasIdentity.Name, loadGroupNames: true));

        /// <summary>
        /// Callback right before the ClaimsIdentity is returned to the cookie handler
        /// (or whatever the default sign in handler is) in order to add additional claims
        /// to the cookie. Can be used for setting security permissions, for example.
        /// </summary>
        public Action<CasAuthenticationContext, ClaimsIdentity> OnMakeClaims { get; set; }

        /// <summary>
        /// Callback when logging a user out, to clear any application-specific stuff
        /// tied to the user's CAS session.
        /// </summary>
        public Action<CasAuthenticationContext> OnSignOut { get; set; }

        /// <summary>
        /// Location that a client visits to impersonate someone else. If we receive a request
        /// on this path, we fire CanImpersonate to let the application determine if the current
        /// user is allowed to impersonate other users. If that returns a yes result, we then
        /// log in as the impersonated user, and subsequently call OnImpersonate to alert the
        /// application that an impersonation happened. OnMakeClaims is called twice, once for
        /// the real client and once for the impersonated client.
        /// </summary>
        public PathString ImpersonationPath { get; set; } = new PathString("/Session/Impersonate");

        /// <summary>
        /// Callback to determine whether or not the given user is allowed to impersonate other
        /// users. If not set, impersonation is not allowed.
        /// </summary>
        public Func<CasAuthenticationContext, bool> CanImpersonate { get; set; }

        /// <summary>
        /// Callback to alert the application that a user was impersonated. The first identity is
        /// the user performing the impersonation and the second identity is the user being impersonated.
        /// </summary>
        public Action<CasAuthenticationContext, ClaimsIdentity, ClaimsIdentity> OnImpersonation { get; set; }
    }
}

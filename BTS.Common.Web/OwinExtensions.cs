using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security;
using System.Security.Claims;
using System.Web;
using System.Web.SessionState;
using Microsoft.AspNet.Identity;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Extensions;
using Microsoft.Owin.Host.SystemWeb;
using Owin;

using BTS.Common.CAS;

namespace BTS.Common.Web
{
    /// <summary>
    /// Extensions for OWIN middleware.
    /// </summary>
    public static class OwinExtensions
    {
        /// <summary>
        /// <para>
        /// Register this application to use CAS authentication with session cookies.
        /// To keep cookie size minimal, a generic session storage is used to store actual session details
        /// and handle things such as single sign-out.
        /// </para>
        /// <para>
        /// This method does not configure any sort of security. Use app.UseRoleBasedSecurity() or app.UseBitfieldSecurity()
        /// as appropriate. If you are not using shared security management, app.UseCustomSecurity() allows you to register
        /// a callback in your Startup.cs file to set up security details on successful authentication.
        /// </para>
        /// </summary>
        /// <param name="app"></param>
        /// <param name="appName">Application name, used in session cookie. Should not contain any spaces.</param>
        /// <param name="sessionStore">Instance of the application-specific session storage to use.</param>
        /// <returns></returns>
        public static IAppBuilder UseCasAuthWithSessionStore(this IAppBuilder app, string appName, ICasSessionStore sessionStore)
        {
            return UseCasAuthWithSessionStore(app, appName, sessionStore, PathString.Empty, PathString.Empty, null);
        }

        /// <summary>
        /// <para>
        /// Register this application to use CAS authentication with session cookies.
        /// To keep cookie size minimal, the ASP.NET session storage is used to store actual session details
        /// and handle things such as single sign-out.
        /// </para>
        /// <para>
        /// This method does not configure any sort of security. Use app.UseRoleBasedSecurity() or app.UseBitfieldSecurity()
        /// as appropriate. If you are not using shared security management, app.UseCustomSecurity() allows you to register
        /// a callback in your Startup.cs file to set up security details on successful authentication.
        /// </para>
        /// <para>
        /// You should attempt to call UseCasAuthWithSessionStore instead if possible; ASP.NET sessions should only be used
        /// if such use is unavoidable (such as when porting an existing application that already uses session data heavily)
        /// </para>
        /// </summary>
        /// <param name="app"></param>
        /// <param name="appName">Application name, used in session cookie. Should not contain any spaces.</param>
        /// <returns></returns>
        public static IAppBuilder UseCasAuthWithSessionCookies(this IAppBuilder app, string appName)
        {
            return UseCasAuthWithSessionCookies(app, appName, PathString.Empty, PathString.Empty, null);
        }

        /// <summary>
        /// <para>
        /// Register this application to use CAS authentication with session cookies.
        /// To keep cookie size minimal, a generic session storage is used to store actual session details
        /// and handle things such as single sign-out.
        /// </para>
        /// <para>
        /// This method does not configure any sort of security. Use app.UseRoleBasedSecurity() or app.UseBitfieldSecurity()
        /// as appropriate. If you are not using shared security management, app.UseCustomSecurity() allows you to register
        /// a callback in your Startup.cs file to set up security details on successful authentication.
        /// </para>
        /// </summary>
        /// <param name="app"></param>
        /// <param name="appName">Application name, used in session cookie. Should not contain any spaces.</param>
        /// <param name="sessionStore">Instance of the application-specific session storage to use.</param>
        /// <param name="loginPath">Path to application-specific login code (setting roles, determining access, etc.)</param>
        /// <returns></returns>
        public static IAppBuilder UseCasAuthWithSessionStore(this IAppBuilder app, string appName, ICasSessionStore sessionStore, PathString loginPath)
        {
            return UseCasAuthWithSessionStore(app, appName, sessionStore, loginPath, PathString.Empty, null);
        }

        /// <summary>
        /// <para>
        /// Register this application to use CAS authentication with session cookies.
        /// To keep cookie size minimal, the ASP.NET session storage is used to store actual session details
        /// and handle things such as single sign-out.
        /// </para>
        /// <para>
        /// This method does not configure any sort of security. Use app.UseRoleBasedSecurity() or app.UseBitfieldSecurity()
        /// as appropriate. If you are not using shared security management, app.UseCustomSecurity() allows you to register
        /// a callback in your Startup.cs file to set up security details on successful authentication.
        /// </para>
        /// <para>
        /// You should attempt to call UseCasAuthWithSessionStore instead if possible; ASP.NET sessions should only be used
        /// if such use is unavoidable (such as when porting an existing application that already uses session data heavily)
        /// </para>
        /// </summary>
        /// <param name="app"></param>
        /// <param name="appName">Application name, used in session cookie. Should not contain any spaces.</param>
        /// <param name="loginPath">Path to application-specific login code (setting roles, determining access, etc.)</param>
        /// <returns></returns>
        public static IAppBuilder UseCasAuthWithSessionCookies(this IAppBuilder app, string appName, PathString loginPath)
        {
            return UseCasAuthWithSessionCookies(app, appName, loginPath, PathString.Empty, null);
        }

        /// <summary>
        /// <para>
        /// Register this application to use CAS authentication with session cookies.
        /// To keep cookie size minimal, a generic session storage is used to store actual session details
        /// and handle things such as single sign-out.
        /// </para>
        /// <para>
        /// This method does not configure any sort of security. Use app.UseRoleBasedSecurity() or app.UseBitfieldSecurity()
        /// as appropriate. If you are not using shared security management, app.UseCustomSecurity() allows you to register
        /// a callback in your Startup.cs file to set up security details on successful authentication.
        /// </para>
        /// </summary>
        /// <param name="app"></param>
        /// <param name="appName">Application name, used in session cookie. Should not contain any spaces.</param>
        /// <param name="sessionStore">Instance of the application-specific session storage to use.</param>
        /// <param name="loginPath">Path to application-specific login code (setting roles, determining access, etc.)</param>
        /// <param name="noPermsPath">Path to page where user is redirected if we get an authentication loop (indicating they are logged in but still unable to access a particular page)</param>
        /// <returns></returns>
        public static IAppBuilder UseCasAuthWithSessionStore(this IAppBuilder app, string appName, ICasSessionStore sessionStore, PathString loginPath, PathString noPermsPath)
        {
            return UseCasAuthWithSessionStore(app, appName, sessionStore, loginPath, noPermsPath, null);
        }

        /// <summary>
        /// <para>
        /// Register this application to use CAS authentication with session cookies.
        /// To keep cookie size minimal, the ASP.NET session storage is used to store actual session details
        /// and handle things such as single sign-out.
        /// </para>
        /// <para>
        /// This method does not configure any sort of security. Use app.UseRoleBasedSecurity() or app.UseBitfieldSecurity()
        /// as appropriate. If you are not using shared security management, app.UseCustomSecurity() allows you to register
        /// a callback in your Startup.cs file to set up security details on successful authentication.
        /// </para>
        /// <para>
        /// You should attempt to call UseCasAuthWithSessionStore instead if possible; ASP.NET sessions should only be used
        /// if such use is unavoidable (such as when porting an existing application that already uses session data heavily)
        /// </para>
        /// </summary>
        /// <param name="app"></param>
        /// <param name="appName">Application name, used in session cookie. Should not contain any spaces.</param>
        /// <param name="loginPath">Path to application-specific login code (setting roles, determining access, etc.)</param>
        /// <param name="noPermsPath">Path to page where user is redirected if we get an authentication loop (indicating they are logged in but still unable to access a particular page)</param>
        /// <returns></returns>
        public static IAppBuilder UseCasAuthWithSessionCookies(this IAppBuilder app, string appName, PathString loginPath, PathString noPermsPath)
        {
            return UseCasAuthWithSessionCookies(app, appName, loginPath, noPermsPath, null);
        }

        /// <summary>
        /// <para>
        /// Register this application to use CAS authentication with session cookies.
        /// To keep cookie size minimal, a generic session storage is used to store actual session details
        /// and handle things such as single sign-out.
        /// </para>
        /// <para>
        /// This method does not configure any sort of security. Use app.UseRoleBasedSecurity() or app.UseBitfieldSecurity()
        /// as appropriate. If you are not using shared security management, app.UseCustomSecurity() allows you to register
        /// a callback in your Startup.cs file to set up security details on successful authentication.
        /// </para>
        /// </summary>
        /// <param name="app"></param>
        /// <param name="appName">Application name, used in session cookie. Should not contain any spaces.</param>
        /// <param name="sessionStore">Instance of the application-specific session storage to use.</param>
        /// <param name="loginPath">Path to application-specific login code (setting roles, determining access, etc.)</param>
        /// <param name="noPermsPath">Path to page where user is redirected if we get an authentication loop (indicating they are logged in but still unable to access a particular page)</param>
        /// <param name="claimsCallback">Callback to fire to add custom claims to the identity before it is stored (useful for storing roles or bitfield security)</param>
        /// <returns></returns>
        public static IAppBuilder UseCasAuthWithSessionStore(
            this IAppBuilder app,
            string appName,
            ICasSessionStore sessionStore,
            PathString loginPath,
            PathString noPermsPath,
            Action<CasAuthenticationContext, ClaimsIdentity> claimsCallback
        )
        {
            if (sessionStore == null)
            {
                throw new ArgumentNullException(nameof(sessionStore));
            }

            var expiration = sessionStore.ExpirationTime;
            var casSessionStore = new CasAuthenticationSessionStore(app)
            {
                ExpirationTime = expiration,
                GetSession = sessionStore.GetSession,
                StoreSession = sessionStore.StoreSession,
                RemoveSession = sessionStore.RemoveSession,
                RenewSession = sessionStore.RenewSession
            };

            var cookieOptions = new CookieAuthenticationOptions()
            {
                AuthenticationMode = AuthenticationMode.Active,
                AuthenticationType = DefaultAuthenticationTypes.ApplicationCookie,
                CookieName = $"{appName}.Session",
                CookieHttpOnly = true,
                CookieSecure = CookieSecureOption.Always,
                CookieManager = new SystemWebChunkingCookieManager(),
                ExpireTimeSpan = TimeSpan.FromMinutes(expiration),
                Provider = new CasAuthenticationCookieProvider(),
                SessionStore = casSessionStore,
                SlidingExpiration = true,
                ReturnUrlParameter = CAS.Constants.RETURN_URL
            };

            var casOptions = new CasAuthenticationOptions()
            {
                AuthenticationMode = AuthenticationMode.Active,
                AuthenticationType = CAS.Constants.CAS,
                LoginPath = loginPath,
                NoPermsPath = noPermsPath,
                SessionStore = casSessionStore,
                OnMakeClaims = claimsCallback
            };

            // hook up authn
            app.UseCookieAuthentication(cookieOptions);
            app.SetDefaultSignInAsAuthenticationType(DefaultAuthenticationTypes.ApplicationCookie);
            app.UseCasAuthentication(casOptions);

            return app;
        }

        /// <summary>
        /// <para>
        /// Register this application to use CAS authentication with session cookies.
        /// To keep cookie size minimal, the ASP.NET session storage is used to store actual session details
        /// and handle things such as single sign-out.
        /// </para>
        /// <para>
        /// This method does not configure any sort of security. Use app.UseRoleBasedSecurity() or app.UseBitfieldSecurity()
        /// as appropriate. If you are not using shared security management, app.UseCustomSecurity() allows you to register
        /// a callback in your Startup.cs file to set up security details on successful authentication.
        /// </para>
        /// <para>
        /// You should attempt to call UseCasAuthWithSessionStore instead if possible; ASP.NET sessions should only be used
        /// if such use is unavoidable (such as when porting an existing application that already uses session data heavily)
        /// </para>
        /// </summary>
        /// <param name="app"></param>
        /// <param name="appName">Application name, used in session cookie. Should not contain any spaces.</param>
        /// <param name="loginPath">Path to application-specific login code (setting roles, determining access, etc.)</param>
        /// <param name="noPermsPath">Path to page where user is redirected if we get an authentication loop (indicating they are logged in but still unable to access a particular page)</param>
        /// <param name="claimsCallback">Callback to fire to add custom claims to the identity before it is stored (useful for storing roles or bitfield security)</param>
        /// <returns></returns>
        public static IAppBuilder UseCasAuthWithSessionCookies(
            this IAppBuilder app,
            string appName,
            PathString loginPath,
            PathString noPermsPath,
            Action<CasAuthenticationContext, ClaimsIdentity> claimsCallback
        )
        {
            int expiration = 60;
            var sessionStore = new DeferredSessionStore(app, expiration);

            var cookieOptions = new CookieAuthenticationOptions()
            {
                AuthenticationMode = AuthenticationMode.Active,
                AuthenticationType = DefaultAuthenticationTypes.ApplicationCookie,
                CookieName = $"{appName}.Session",
                CookieHttpOnly = true,
                CookieSecure = CookieSecureOption.Always,
                CookieManager = new SystemWebChunkingCookieManager(),
                ExpireTimeSpan = TimeSpan.FromMinutes(expiration),
                Provider = new CasAuthenticationCookieProvider(),
                SessionStore = sessionStore,
                SlidingExpiration = true,
                ReturnUrlParameter = CAS.Constants.RETURN_URL
            };

            var casOptions = new CasAuthenticationOptions()
            {
                AuthenticationMode = AuthenticationMode.Active,
                AuthenticationType = CAS.Constants.CAS,
                LoginPath = loginPath,
                NoPermsPath = noPermsPath,
                SessionStore = sessionStore,
                OnMakeClaims = claimsCallback
            };

            // hook up authn
            app.UseCookieAuthentication(cookieOptions);
            app.SetDefaultSignInAsAuthenticationType(DefaultAuthenticationTypes.ApplicationCookie);
            app.UseCasAuthentication(casOptions);

            // hook up session handling
            app.Use((context, next) =>
            {
                var httpContext = context.Get<HttpContextBase>(typeof(HttpContextBase).FullName);
                httpContext.SetSessionStateBehavior(SessionStateBehavior.Required);

                return next();
            }).UseStageMarker(PipelineStage.MapHandler);

            app.Use((context, next) =>
            {
                sessionStore.RunDeferredActions(HttpContext.Current);

                return next();
            }).UseStageMarker(PipelineStage.PostAcquireState);

            return app;
        }

        /// <summary>
        /// Workaround a bug with OWIN's cookie handling colliding with ASP.NET Session State
        /// </summary>
        /// <param name="app"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static IAppBuilder UseCookieAuthenticationWithSessionStateSupport(this IAppBuilder app, CookieAuthenticationOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            options.CookieManager = new SystemWebChunkingCookieManager();

            return app.UseCookieAuthentication(options);
        }

        /// <summary>
        /// Workaround a bug with OWIN's cookie handling colliding with ASP.NET Session State
        /// </summary>
        /// <param name="app"></param>
        /// <param name="options"></param>
        /// <param name="stage"></param>
        /// <returns></returns>
        public static IAppBuilder UseCookieAuthenticationWithSessionStateSupport(this IAppBuilder app, CookieAuthenticationOptions options, PipelineStage stage)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            options.CookieManager = new SystemWebChunkingCookieManager();

            return app.UseCookieAuthentication(options, stage);
        }
    }
}

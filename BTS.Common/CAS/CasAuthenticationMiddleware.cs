using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Claims;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Infrastructure;
using Owin;

namespace BTS.Common.CAS
{
    /// <summary>
    /// Middleware to log into CAS
    /// </summary>
    public class CasAuthenticationMiddleware : AuthenticationMiddleware<CasAuthenticationOptions>
    {
        /// <summary>
        /// Set up middleware for the app
        /// </summary>
        /// <param name="next"></param>
        /// <param name="app"></param>
        /// <param name="options"></param>
        public CasAuthenticationMiddleware(OwinMiddleware next, IAppBuilder app, CasAuthenticationOptions options)
            : base(next, options)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (String.IsNullOrEmpty(options.SignInAsAuthenticationType))
            {
                options.SignInAsAuthenticationType = app.GetDefaultSignInAsAuthenticationType();
            }
        }

        /// <summary>
        /// Creates the auth handler for this middleware
        /// </summary>
        /// <returns></returns>
        protected override AuthenticationHandler<CasAuthenticationOptions> CreateHandler()
        {
            return new CasAuthenticationHandler();
        }
    }
}

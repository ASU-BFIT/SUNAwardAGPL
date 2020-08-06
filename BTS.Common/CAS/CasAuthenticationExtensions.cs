using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Principal;
using System.Security.Claims;
using Microsoft.Owin;
using Owin;

namespace BTS.Common.CAS
{
    /// <summary>
    /// Extension methods for CAS
    /// </summary>
    public static class CasAuthenticationExtensions
    {
        /// <summary>
        /// Use CAS auth to validate users
        /// </summary>
        /// <param name="app"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static IAppBuilder UseCasAuthentication(this IAppBuilder app, CasAuthenticationOptions options)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            return app.Use(typeof(CasAuthenticationMiddleware), app, options);
        }

        /// <summary>
        /// Tests if the given service ticket matches the existing user's CAS auth session
        /// </summary>
        /// <param name="principal">User to test</param>
        /// <param name="ticket">Ticket to validate against</param>
        /// <returns>True if the saved ticket matches the passed-in ticket, false otherwise</returns>
        public static bool ValidateServiceTicket(this IPrincipal principal, string ticket)
        {
            ClaimsIdentity casId;

            if (principal == null)
            {
                throw new ArgumentNullException(nameof(principal));
            }

            if (ticket == null)
            {
                throw new ArgumentNullException(nameof(ticket));
            }

            if (!(principal is ClaimsPrincipal claimsPrincipal))
            {
                // not a ClaimsPrincipal which means we won't have a service ticket stored in it
                return false;
            }

            try
            {
                casId = claimsPrincipal.Identities.SingleOrDefault(o => o.AuthenticationType == Constants.CAS);
            }
            catch (InvalidOperationException)
            {
                // has more than one CAS identity attached to this principal
                return false;
            }

            if (casId == null || !casId.IsAuthenticated)
            {
                // not logged into CAS
                return false;
            }

            var ourTicket = casId.FindFirst(Constants.SERVICE_TICKET)?.Value;
            if (ourTicket == null)
            {
                // no service ticket on identity?
                // likely only happens if our id isn't fully initialized yet or session store is broken
                return false;
            }

            return ourTicket == ticket;
        }
    }
}

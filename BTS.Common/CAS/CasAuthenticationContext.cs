using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.Owin.Security;

namespace BTS.Common.CAS
{
    /// <summary>
    /// Wraps data used to authenticate against CAS as well as its response.
    /// </summary>
    public class CasAuthenticationContext
    {
        internal CasAuthenticationHandler Handler { get; set; }
        
        /// <summary>
        /// The identity that CAS authenticated us as
        /// </summary>
        public ClaimsIdentity CasIdentity { get; set; }

        /// <summary>
        /// Other identities for this user, e.g. from Active Directory.
        /// The OnAuthenticated handler should fill this in if it needs to persist
        /// data about other identities into the session.
        /// </summary>
        public List<ClaimsIdentity> OtherIdentities { get; } = new List<ClaimsIdentity>();

        /// <summary>
        /// Internal OWIN properties for this context
        /// </summary>
        public AuthenticationProperties Properties { get; set; }

        /// <summary>
        /// ST returned by CAS
        /// </summary>
        public string ServiceTicket { get; set; }

        /// <summary>
        /// PGT returned by CAS, in the event we're talking to a proxy
        /// </summary>
        public string ProxyGrantingTicket { get; set; }

        /// <summary>
        /// PGT IOU returned by CAS, in the event we're talking to a proxy
        /// </summary>
        public string ProxyGrantingTicketIOU { get; set; }

        /// <summary>
        /// Gets a service ticket for the given service. This should be given
        /// to the service who then verifies it with CAS via proxyValidate.
        /// NOTE: This performs an HTTP request to the CAS endpoint!
        /// </summary>
        /// <param name="service"></param>
        /// <returns></returns>
        public async Task<string> GetProxyServiceTicket(string service)
        {
            return await Handler.GetProxyServiceTicket(this, service);
        }
    }
}

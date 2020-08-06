using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics.CodeAnalysis;

namespace BTS.Common.CAS
{
    /// <summary>
    /// Constant strings for internal use
    /// </summary>
    [SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores",
        Justification = "Naming convention chosen for constants was CAPS_WITH_UNDERSCORES")]
    public static class Constants
    {
        /// <summary>
        /// CAS
        /// </summary>
        public const string CAS = "CAS";
        /// <summary>
        /// Return URL
        /// </summary>
        public const string RETURN_URL = "ReturnUrl";
        /// <summary>
        /// Service Ticket
        /// </summary>
        public const string SERVICE_TICKET = "ServiceTicket";
        /// <summary>
        /// Proxy Granting Ticket
        /// </summary>
        public const string PROXY_GRANTING_TICKET = "ProxyGrantingTicket";
        /// <summary>
        /// List of additional identities in the AuthenticationTicket
        /// </summary>
        public const string IDENTITY_PROP = "#*#Identities";

        /// <summary>
        /// Identity Claim
        /// </summary>
        public const string IDENTITY_CLAIM = "http://schemas.microsoft.com/accesscontrolservice/2010/07/claims/identityprovider";
    }
}

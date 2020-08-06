using BTS.Common.AD;
using BTS.Common.CAS;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;

namespace ASU.Web.Auth
{
    /// <summary>
    /// Backwards-compatibility wrapper around our OWIN ClaimsPrincipal.
    /// A ClaimsPrincipal can be transparently down-casted to an instance of this class.
    /// </summary>
    public class AsuritePrincipal : IPrincipal
    {
        private ClaimsPrincipal OrigPrincipal { get; set; }
        private ClaimsIdentity AD { get; set; }

        /// <summary>
        /// The underlying Identity for this Principal
        /// </summary>
        public IIdentity Identity => OrigPrincipal.Identity;

        /// <summary>
        /// The ASURITE id of the user
        /// </summary>
        public string AsuriteID => Identity.Name;

        /// <summary>
        /// The Affiliate id / employee id of the user
        /// </summary>
        public string AffiliateID => AD?.FindFirst(ActiveDirectory.AffiliateId)?.Value;

        /// <summary>
        /// The user's first name
        /// </summary>
        public string FirstName => AD?.FindFirst(ClaimTypes.GivenName)?.Value;

        /// <summary>
        /// The user's last name
        /// </summary>
        public string LastName => AD?.FindFirst(ClaimTypes.Surname)?.Value;

        /// <summary>
        /// The user's display name (usually first and last names)
        /// </summary>
        public string Name => AD?.FindFirst(ClaimTypes.Name)?.Value;

        /// <summary>
        /// Constructs a new AsuritePrincipal.
        /// </summary>
        private AsuritePrincipal()
        {
            // no-op
        }

        /// <summary>
        /// Checks if this principal belongs to the specified role
        /// </summary>
        /// <param name="role"></param>
        /// <returns></returns>
        public bool IsInRole(string role)
        {
            return OrigPrincipal.IsInRole(role);
        }


        /// <summary>
        /// Convert an arbitrary ClaimsPrincipal into an AsuritePrincipal.
        /// The ClaimsPrincipal must have identities which indicate a successful CAS logon.
        /// </summary>
        /// <param name="p"></param>
        [SuppressMessage("Usage", "CA2225:Operator overloads have named alternates",
            Justification = "Named alternatives exist in the PrincipalExtensions class")]
        public static explicit operator AsuritePrincipal(ClaimsPrincipal p)
        {
            if (p == null)
            {
                return null;
            }

            if (!p.Identities.Any(o => o.AuthenticationType == Constants.CAS))
            {
                throw new InvalidCastException("You can only cast a ClaimsPrincipal to an AsuritePrincipal if it was authenticated using CAS.");
            }

            return new AsuritePrincipal()
            {
                OrigPrincipal = p,
                AD = p.Identities.FirstOrDefault(o => o.AuthenticationType == ActiveDirectory.IdentityType)
            };
        }
    }
}

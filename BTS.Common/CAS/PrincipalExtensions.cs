using System;
using System.Security.Claims;
using System.Security.Principal;

namespace ASU.Web.Auth
{
    /// <summary>
    /// Extension methods to convert to AsuritePrincipals
    /// </summary>
    public static class PrincipalExtensions
    {
        /// <summary>
        /// Convert an arbitrary IPrincipal to an AsuritePrincipal. Returns null on failure
        /// (as if using the "as" operator)
        /// </summary>
        /// <param name="principal"></param>
        /// <returns></returns>
        public static AsuritePrincipal AsAsuritePrincipal(this IPrincipal principal)
        {
            try
            {
                return principal.ToAsuritePrincipal();
            }
            catch (InvalidCastException)
            {
                // no-op
            }

            return null;
        }

        /// <summary>
        /// Convert an arbitrary IPrincipal to an AsuritePrincipal. Throws on failure
        /// (as if using a type cast)
        /// </summary>
        /// <param name="principal"></param>
        /// <returns></returns>
        public static AsuritePrincipal ToAsuritePrincipal(this IPrincipal principal)
        {
            // explicit conversion above only works if we're casting from an actual ClaimsPrincipal instance
            // (as opposed to a more generic type such as IPrincipal)
            return (AsuritePrincipal)(ClaimsPrincipal)principal;
        }
    }
}

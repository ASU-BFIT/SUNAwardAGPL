using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Web;
using System.Security.Claims;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Infrastructure;
using Microsoft.Owin.Security.Cookies;

namespace BTS.Common.CAS
{
    /// <summary>
    /// Retrieves user identity data stored in the session cookie
    /// </summary>
    public class CasAuthenticationCookieProvider : CookieAuthenticationProvider
    {
        /// <summary>
        /// Retrieves user identity data stored in the session cookie
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public override Task ValidateIdentity(CookieValidateIdentityContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (context.Properties.Dictionary.ContainsKey(Constants.IDENTITY_PROP))
            {
                var list = IdentityList.Deserialize(context.Properties.Dictionary[Constants.IDENTITY_PROP]);
                var helper = new SecurityHelper(context.OwinContext);

                if (list != null)
                {
                    foreach (var id in list.Items)
                    {
                        helper.AddUserIdentity(id);
                    }
                }
            }

            return base.ValidateIdentity(context);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Security.Claims;

using BTS.Common.Web;

namespace BTS.Common.Mvc
{
    /// <summary>
    /// Denotes that this action or controller requires additional security
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
    public class RequireSecurityAttribute : AuthorizeAttribute
    {
        private readonly Enum[] _permissions;
        private readonly SecurityFlags _level;
        private readonly string[] _roles;

        /// <summary>
        /// If the user is impersonating a different privilege set,
        /// check the original permissions if true instead of that
        /// of the impersonated set
        /// </summary>
        public bool SkipImpersonation { get; set; }

        /// <summary>
        /// This action or controller requires at least one of the permissions at the given level
        /// </summary>
        /// <param name="level"></param>
        /// <param name="permissions">The permission enum values to check, cast to Enum internally</param>
        public RequireSecurityAttribute(SecurityFlags level, params object[] permissions)
        {
            _permissions = permissions.Cast<Enum>().ToArray();
            _level = level;
            _roles = null;
        }

        /// <summary>
        /// This action or controller requires membership in at least one of the roles
        /// </summary>
        /// <param name="grants"></param>
        public RequireSecurityAttribute(params string[] grants)
        {
            _roles = grants;
            _permissions = null;
            _level = SecurityFlags.None;
        }

        /// <summary>
        /// Check whether user is allowed to execute this action or controller
        /// </summary>
        /// <param name="httpContext"></param>
        /// <returns></returns>
        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            // check original AuthorizeAttribute, if that fails we're done
            if (!base.AuthorizeCore(httpContext))
            {
                return false;
            }

            var user = httpContext.User as ClaimsPrincipal;

            if (_roles != null)
            {
                return SecurityManager.IsAllowed(user, _roles);
            }
            else
            {
                return SecurityManager.IsAllowed(user, _level, _permissions);
            }
        }
    }
}
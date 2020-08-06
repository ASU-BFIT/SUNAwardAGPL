using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Security.Principal;
using System.Security.Claims;

using Owin;

namespace BTS.Common.Web
{
    /// <summary>
    /// Common user security functions
    /// </summary>
    public static class SecurityManager
    {
        /// <summary>
        /// View permission
        /// </summary>
        public static readonly string ClaimTypeView = "BTS.Common.Web.SecurityPermissions.View";
        /// <summary>
        /// Edit permission
        /// </summary>
        public static readonly string ClaimTypeEdit = "BTS.Common.Web.SecurityPermissions.Edit";
        /// <summary>
        /// Create permission
        /// </summary>
        public static readonly string ClaimTypeCreate = "BTS.Common.Web.SecurityPermissions.Create";
        /// <summary>
        /// Delete permission
        /// </summary>
        public static readonly string ClaimTypeDelete = "BTS.Common.Web.SecurityPermissions.Delete";

        private static SecurityType SecType = SecurityType.Unconfigured;
        private static Action<ClaimsPrincipal, HttpContext> CustomCallback;

        /// <summary>
        /// Register the type checked for whether or not a user is allowed to
        /// do something. This must be an enum with FlagsAttribute.
        /// The user should then have claims with type SecurityManager.ClaimTypeX
        /// and a value being the bitfiled of all permissions they have (mapped to
        /// permissionsType) for each type X (Read, Edit, Create, Delete).
        /// </summary>
        /// <param name="app"></param>
        /// <param name="permissionsType"></param>
        /// <returns></returns>
        public static IAppBuilder UseBitfieldSecurity(this IAppBuilder app, Type permissionsType)
        {
            if (SecType != SecurityType.Unconfigured)
            {
                throw new InvalidOperationException("Security type can only be configured once.");
            }

            if (permissionsType == null)
            {
                throw new ArgumentNullException(nameof(permissionsType));
            }

            if (!permissionsType.IsEnum || !permissionsType.GetCustomAttributes(typeof(FlagsAttribute), false).Any())
            {
                throw new ArgumentException("permissionsType must be an enum with FlagsAttribute", nameof(permissionsType));
            }

            SecType = SecurityType.Bitfield;

            return app;
        }

        /// <summary>
        /// Registers that users should be checked for role membership to determine whether or not they are
        /// allowed actions.
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        public static IAppBuilder UseRoleBasedSecurity(this IAppBuilder app)
        {
            if (SecType != SecurityType.Unconfigured)
            {
                throw new InvalidOperationException("Security type can only be configured once.");
            }

            SecType = SecurityType.RoleBased;

            return app;
        }

        /// <summary>
        /// Registers that user security is handled by the application. The application specifies a callback
        /// which is called during the auth process and is passed the ClaimsPrincipal for the user as well
        /// as the current HttpContext (ASP.NET sessions are automatically initialized when using custom security).
        /// </summary>
        /// <param name="app"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public static IAppBuilder UseCustomSecurity(this IAppBuilder app, Action<ClaimsPrincipal, HttpContext> callback)
        {
            if (SecType != SecurityType.Unconfigured)
            {
                throw new InvalidOperationException("Security type can only be configured once.");
            }

            SecType = SecurityType.Custom;
            CustomCallback = callback ?? throw new ArgumentNullException(nameof(callback));

            return app;
        }

        /// <summary>
        /// If we are using custom security, fire our callback so the application can set up the user.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="context"></param>
        internal static void MaybeRunCustomCallback(ClaimsPrincipal user, HttpContext context)
        {
            if (SecType == SecurityType.Custom)
            {
                CustomCallback(user, context);
            }
        }

        /// <summary>
        /// Returns true if the user is granted access to any of the following permissions with any of the given flags.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="flags"></param>
        /// <param name="permissions"></param>
        /// <returns></returns>
        public static bool IsAllowed(this IPrincipal user, SecurityFlags flags, params Enum[] permissions)
        {
            if (SecType != SecurityType.Bitfield)
            {
                throw new InvalidOperationException("This overload of IsAllowed can only be used if Bitfield Security is configured.");
            }

            if (permissions.Length == 0)
            {
                throw new ArgumentException("Must specify at least one permission", nameof(permissions));
            }

            if (flags == SecurityFlags.None)
            {
                return true;
            }

            ulong bits = 0UL;

            if (!(user is ClaimsPrincipal cuser))
            {
                throw new InvalidOperationException("Bitfield Security can only be used on ClaimPrincipals.");
            }

            if (flags.HasFlag(SecurityFlags.View))
            {
                bits |= cuser.FindAll(ClaimTypeView).Select(o => Convert.ToUInt64(o.Value)).Aggregate(0UL, (i1, i2) => i1 | i2);
            }

            if (flags.HasFlag(SecurityFlags.Edit))
            {
                bits |= cuser.FindAll(ClaimTypeEdit).Select(o => Convert.ToUInt64(o.Value)).Aggregate(0UL, (i1, i2) => i1 | i2);
            }

            if (flags.HasFlag(SecurityFlags.Create))
            {
                bits |= cuser.FindAll(ClaimTypeCreate).Select(o => Convert.ToUInt64(o.Value)).Aggregate(0UL, (i1, i2) => i1 | i2);
            }

            if (flags.HasFlag(SecurityFlags.Delete))
            {
                bits |= cuser.FindAll(ClaimTypeDelete).Select(o => Convert.ToUInt64(o.Value)).Aggregate(0UL, (i1, i2) => i1 | i2);
            }

            return (permissions.Select(o => Convert.ToUInt64(o.ToString("D"))).Aggregate((i1, i2) => i1 | i2) & bits) != 0UL;
        }

        /// <summary>
        /// Returns true if the user belongs to at least one of the given roles.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="roles"></param>
        /// <returns></returns>
        public static bool IsAllowed(this IPrincipal user, params string[] roles)
        {
            if (SecType != SecurityType.RoleBased)
            {
                throw new InvalidOperationException("This overload of IsAllowed can only be used if Role-Based Security is configured.");
            }

            if (roles.Length == 0)
            {
                throw new ArgumentException("Must specify at least one role", nameof(roles));
            }

            return roles.Any(r => user.IsInRole(r));
        }

        private enum SecurityType
        {
            Unconfigured,
            Bitfield,
            RoleBased,
            Custom
        }
    }
}

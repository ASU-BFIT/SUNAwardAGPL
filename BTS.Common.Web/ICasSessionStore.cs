using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BTS.Common.CAS;

namespace BTS.Common.Web
{
    /// <summary>
    /// Applications should implement this interface when using
    /// CAS authentication with session storage, and pass an instance
    /// of that class to the middleware.
    /// </summary>
    public interface ICasSessionStore
    {
        /// <summary>
        /// Number of minutes for which the session is valid
        /// </summary>
        int ExpirationTime { get; }

        /// <summary>
        /// Retrieve a session given an opaque unique session key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        CasAuthenticationSession GetSession(string key);

        /// <summary>
        /// Store a session in the database
        /// </summary>
        /// <param name="session"></param>
        void StoreSession(CasAuthenticationSession session);

        /// <summary>
        /// Remove a session from the database
        /// </summary>
        /// <param name="session"></param>
        void RemoveSession(CasAuthenticationSession session);

        /// <summary>
        /// Renew the expiration time of a session, and store the updated
        /// expiration time in the database
        /// </summary>
        /// <param name="session"></param>
        void RenewSession(CasAuthenticationSession session);
    }
}

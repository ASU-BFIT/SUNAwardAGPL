using System;

namespace BTS.Common.CAS
{
    /// <summary>
    /// Stores data about a CAS session
    /// </summary>
    public class CasAuthenticationSession
    {
        /// <summary>
        /// Opaque session ID, for storing in a persistent data store
        /// </summary>
        public string SessionId { get; set; }

        /// <summary>
        /// Opaque data corresponding to this session, for storing in a persistent data store
        /// </summary>
        public string Data { get; set; }

        /// <summary>
        /// When the session expires
        /// </summary>
        public DateTime Expires { get; set; }
    }
}

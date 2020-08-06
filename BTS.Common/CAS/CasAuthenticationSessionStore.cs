using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Diagnostics.CodeAnalysis;

using Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.DataHandler;
using Microsoft.Owin.Security.DataProtection;
using Microsoft.Owin.Security.Cookies;

namespace BTS.Common.CAS
{
    /// <summary>
    /// Wrapper around a persistent store to hold CAS session data
    /// </summary>
    public class CasAuthenticationSessionStore : IAuthenticationSessionStore
    {
        /// <summary>
        /// The number of minutes a session needs to be inactive in order to expire.
        /// One is usually still logged into CAS despite local session being inactive,
        /// so this really only serves a purpose of forcing a relog after a while; not sure the usefulness of that
        /// (we could probably force one to actually reauth to CAS rather than a silent redirect if desired)
        /// </summary>
        public int ExpirationTime { get; set; } = 20;

        /// <summary>
        /// Gets session details from the backing store. This callback cannot be null,
        /// although it may return a null result.
        /// </summary>
        public Func<string, CasAuthenticationSession> GetSession { get; set; }
            = (x) => throw new NotImplementedException("GetSession must be provided when instantiating CasAuthenticationSessionStore.");
        
        /// <summary>
        /// Store session details to the backing store. This callback cannot be null.
        /// </summary>
        public Action<CasAuthenticationSession> StoreSession { get; set; }
            = (x) => throw new NotImplementedException("StoreSession must be provided when instantiating CasAuthenticationSessionStore.");

        /// <summary>
        /// Remove a session from the backing store. This callback may be null.
        /// </summary>
        public Action<CasAuthenticationSession> RemoveSession { get; set; }

        /// <summary>
        /// Renew a session in the backing store, updating its expiration time.
        /// This callback may be null.
        /// </summary>
        public Action<CasAuthenticationSession> RenewSession { get; set; }

        private TicketDataFormat Format { get; set; }

        /// <summary>
        /// Constructs a new instance of the session store for the application
        /// </summary>
        /// <param name="app"></param>
        public CasAuthenticationSessionStore(IAppBuilder app)
        {
            Format = new TicketDataFormat(app.CreateDataProtector("BTS.Common.CAS", "AuthenticationTicket"));
        }

        /// <summary>
        /// Destroys a session from the session store
        /// </summary>
        /// <param name="key"></param>
        public void Remove(string key)
        {
            RunCallbackIfNotNullSession(key, RemoveSession);
        }

        /// <summary>
        /// Asynchronously destroys a session from the session store
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public async Task RemoveAsync(string key)
        {
            await Task.Run(() => Remove(key));
        }

        /// <summary>
        /// Extends the expiration time of a session in the session store
        /// </summary>
        /// <param name="key"></param>
        /// <param name="ticket"></param>
        [SuppressMessage("Style", "CA1801:Remove unused parameter",
            Justification = "ticket parameter is required for IAuthenticationSessionStore implementation")]
        public void Renew(string key, AuthenticationTicket ticket)
        {
            RunCallbackIfNotNullSession(key, RenewSession);
        }

        /// <summary>
        /// Asynchronously extends the expiration time of a session in the session store
        /// </summary>
        /// <param name="key"></param>
        /// <param name="ticket"></param>
        /// <returns></returns>
        public async Task RenewAsync(string key, AuthenticationTicket ticket)
        {
            await Task.Run(() => Renew(key, ticket));
        }

        /// <summary>
        /// Fetches a session from the session store
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public AuthenticationTicket Retrieve(string key)
        {
            var session = GetSession(GetKey(key));

            if (session?.Data == null)
            {
                return null;
            }

            return Format.Unprotect(session.Data);
        }

        /// <summary>
        /// Asynchronously fetches a session from the session store
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public async Task<AuthenticationTicket> RetrieveAsync(string key)
        {
            return await Task.Run(() => Retrieve(key));
        }

        /// <summary>
        /// Writes session data to the session store
        /// </summary>
        /// <param name="ticket"></param>
        /// <returns></returns>
        public string Store(AuthenticationTicket ticket)
        {
            if (ticket == null)
            {
                throw new ArgumentNullException(nameof(ticket));
            }

            var session = new CasAuthenticationSession()
            {
                SessionId = GetKey(ticket.Properties.Dictionary["ServiceTicket"]),
                Data = Format.Protect(ticket),
                Expires = DateTime.Now.AddMinutes(ExpirationTime)
            };


            StoreSession(session);

            return session.SessionId;
        }

        /// <summary>
        /// Asynchronously writes session data to the session store
        /// </summary>
        /// <param name="ticket"></param>
        /// <returns></returns>
        public async Task<string> StoreAsync(AuthenticationTicket ticket)
        {
            return await Task.Run(() => Store(ticket));
        }

        private void RunCallbackIfNotNullSession(string key, Action<CasAuthenticationSession> callback)
        {
            if (callback != null)
            {
                var session = GetSession(GetKey(key));

                if (session != null)
                {
                    callback(session);
                }
            }
        }

        /// <summary>
        /// Retrieves the Session ID given a service ticket
        /// </summary>
        /// <param name="ticket"></param>
        /// <returns></returns>
        public static string GetKey(string ticket)
        {
            if (ticket == null)
            {
                throw new ArgumentNullException(nameof(ticket));
            }

            if (!ticket.StartsWith("ST-"))
            {
                return ticket;
            }

            using var hash = SHA256.Create();
            byte[] ticketBytes = Encoding.UTF8.GetBytes(ticket);
            byte[] hashBytes = hash.ComputeHash(ticketBytes);
            var sb = new StringBuilder();

            foreach (byte b in hashBytes)
            {
                sb.AppendFormat("{0:x2}", b);
            }

            return sb.ToString();
        }
    }
}

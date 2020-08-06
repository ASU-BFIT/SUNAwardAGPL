using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace BTS.Common.CAS
{
    /// <summary>
    /// Indicates that the user is not authorized. Applications can throw
    /// this exception in OnAuthorized or OnMakeClaims to tell our library
    /// that we should reject this user account (they can auth to CAS but have
    /// no permission for our application).
    /// </summary>
    [Serializable]
    public class NotAuthorizedException : Exception
    {
        /// <summary>
        /// Construct a new NotAuthorizedException with the given message
        /// </summary>
        /// <param name="message"></param>
        public NotAuthorizedException(string message) : base(message) { }

        /// <summary>
        /// Construct a new NotAuthorizedException with the given message and inner exception
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public NotAuthorizedException(string message, Exception innerException) : base(message, innerException) { }

        /// <summary>
        /// Construct a new NotAuthorizedException
        /// </summary>
        public NotAuthorizedException() { }

        /// <summary>
        /// Serialization constructor
        /// </summary>
        /// <param name="serializationInfo"></param>
        /// <param name="streamingContext"></param>
        protected NotAuthorizedException(SerializationInfo serializationInfo, StreamingContext streamingContext)
            : base(serializationInfo, streamingContext) { }
    }
}

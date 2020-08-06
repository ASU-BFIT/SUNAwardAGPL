using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Claims;

namespace BTS.Common.CAS
{
    /// <summary>
    /// List of identities the user has
    /// </summary>
    public class IdentityList
    {
        /// <summary>
        /// Items in collection
        /// </summary>
        public List<ClaimsIdentity> Items { get; private set; } = new List<ClaimsIdentity>();


        /// <summary>
        /// Constructs an empty IdentityList
        /// </summary>
        public IdentityList() { }

        /// <summary>
        /// Constructs an IdentityList with the specified identities
        /// </summary>
        /// <param name="identities"></param>
        public IdentityList(IEnumerable<ClaimsIdentity> identities)
        {
            Items.AddRange(identities);
        }

        /// <summary>
        /// Stores the identity list into a string for database writes
        /// </summary>
        /// <returns></returns>
        public string Serialize()
        {
            using var ms = new MemoryStream();
            var formatter = new BinaryFormatter();
            formatter.Serialize(ms, Items);

            return Convert.ToBase64String(ms.ToArray());
        }

        /// <summary>
        /// Reads stored version into identity list
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static IdentityList Deserialize(string data)
        {
            byte[] binData = Convert.FromBase64String(data);
            using var ms = new MemoryStream(binData);
            var formatter = new BinaryFormatter();

            return new IdentityList()
            {
                Items = formatter.Deserialize(ms) as List<ClaimsIdentity>
            };
        }
    }
}

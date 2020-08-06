using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Security.Claims;
using BTS.Common.AD;
using System.Security.Principal;

namespace BTS.Common.Crypto
{
    /// <summary>
    /// Represents the scope of who is allowed to unprotect the data once it is protected.
    /// The current user must belong to all of the security principals mentioned here.
    /// </summary>
    public sealed class ProtectionDescriptorNg
    {
        private readonly HashSet<string> mySids = new HashSet<string>();
        private readonly List<string> descriptorSids = new List<string>();

        /// <summary>
        /// Constructs a new protection descriptor instance
        /// </summary>
        /// <param name="usersAndGroups">
        /// Domain users and groups that are allowed to unprotect the data.
        /// If null or empty, only the current user can unprotect it.
        /// Do not pass unvalidated user input into this constructor!
        /// </param>
        public ProtectionDescriptorNg(params string[] usersAndGroups)
        {
            var userInfo = ActiveDirectory.GetUserInfo(Environment.UserName);

            mySids.Add(userInfo.FindFirst(ClaimTypes.PrimarySid).Value);
            foreach (var groupSid in userInfo.FindAll(ClaimTypes.GroupSid))
            {
                mySids.Add(groupSid.Value);
            }

            if (usersAndGroups == null || usersAndGroups.Length == 0)
            {
                descriptorSids.Add(userInfo.FindFirst(ClaimTypes.PrimarySid).Value);
            }
            else
            {
                foreach (var samAccountName in usersAndGroups)
                {
                    Add(samAccountName);
                }
            }
        }

        /// <summary>
        /// Adds the given user or group to the protection descriptor.
        /// The current user must belong to the group specified or be the user specified.
        /// Do not pass unvalidated user input into this function!
        /// </summary>
        /// <param name="userOrGroup">User or group to add</param>
        public void Add(string userOrGroup)
        {
            var validInputRegex = new Regex("^[a-zA-Z0-9-_.]+$");
            var m = validInputRegex.Match(userOrGroup);
            if (!m.Success)
            {
                throw new ArgumentException(
                    String.Format("Invalid user or group name '{0}'. Do not include any domain portion in the name.", userOrGroup),
                    nameof(userOrGroup));
            }

            var searchResult = ActiveDirectory.Search(
                String.Format("(&(|(objectCategory=person)(objectCategory=group))(sAMAccountName={0}))", userOrGroup),
                new string[] { "objectSid" });

            if (searchResult.Count == 0)
            {
                throw new ArgumentException(
                    String.Format("Unknown user or group name '{0}'.", userOrGroup),
                    nameof(userOrGroup));
            }

            var sid = new SecurityIdentifier((byte[])searchResult[0].Properties["objectSid"][0], 0).ToString();
            if (!mySids.Contains(sid))
            {
                throw new ArgumentException(
                    String.Format("Cannot create a protection descriptor for SID {0} because the current user does not belong to it.", sid),
                    nameof(userOrGroup));
            }

            descriptorSids.Add(sid);
        }

        /// <summary>
        /// Retrieves the string representation of this protection descriptor in a format suitable to passing into CNG DPAPI.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return String.Join(" OR ", descriptorSids.Select(s => "SID=" + s));
        }
    }
}

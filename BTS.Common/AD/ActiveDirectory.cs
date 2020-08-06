using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.DirectoryServices;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BTS.Common.AD
{
    /// <summary>
    /// Helper class for AD lookups
    /// </summary>
    [SuppressMessage("Naming", "CA1724:Type names should not match namespaces",
        Justification = "Consumers will use this class instead of importing the System.DirectoryServices.ActiveDirectory namespace")]
    public static class ActiveDirectory
    {
        /// <summary>
        /// Claim type for EmplId / Affiliate Id
        /// </summary>
        public const string AffiliateId = "EMPLID";

        /// <summary>
        /// The type of this ClaimsIdentity
        /// </summary>
        public const string IdentityType = "ActiveDirectory";

        /// <summary>
        /// Fetches the entry for the given LDAP path
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static DirectoryEntry GetObject(string path)
        {
            return new DirectoryEntry("LDAP://" + path);
        }

        /// <summary>
        /// Checks if the given LDAP path exists
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static bool ObjectExists(string path)
        {
            return DirectoryEntry.Exists("LDAP://" + path);
        }

        /// <summary>
        /// Searches the directory
        /// </summary>
        /// <param name="query">LDAP query</param>
        /// <param name="properties">Properties to load</param>
        /// <returns></returns>
        public static List<SearchResult> Search(string query, string[] properties = null)
        {
            var resList = new List<SearchResult>();
            DirectorySearcher searcher;

            if (properties != null)
            {
                searcher = new DirectorySearcher(query, properties);
            }
            else
            {
                searcher = new DirectorySearcher(query);
            }

            using (var results = searcher.FindAll())
            {
                foreach (var obj in results)
                {
                    var result = obj as SearchResult;
                    resList.Add(result);
                }
            }

            searcher.Dispose();

            return resList;
        }

        /// <summary>
        /// Gets user info from AD
        /// </summary>
        /// <param name="samAccountNameOrSid">ASURITE or SID</param>
        /// <param name="loadGroupNames">Whether or not to load group names (if false, the user's groups are not loaded)</param>
        /// <returns></returns>
        public static ClaimsIdentity GetUserInfo(string samAccountNameOrSid, bool loadGroupNames = true)
        {
            Match m;
            var affiliateIdRegex = new Regex("Affiliate id ([0-9]+)");
            var deptIdRegex = new Regex(@"CN=T\.Dept\.([^,]+),OU=TrustedGroups,(?:DC=asurite,)?DC=ad,DC=asu,DC=edu");
            var userAffilRegex = new Regex(@"CN=T\.Affil\.([^,]+),OU=TrustedGroups,(?:DC=asurite,)?DC=ad,DC=asu,DC=edu");
            var validInputRegex = new Regex("^[a-zA-Z0-9-_.]+$");

            if (samAccountNameOrSid == null)
            {
                throw new ArgumentNullException(nameof(samAccountNameOrSid));
            }

            m = validInputRegex.Match(samAccountNameOrSid);
            if (!m.Success)
            {
                throw new ArgumentOutOfRangeException(nameof(samAccountNameOrSid), "Argument contains invalid characters");
            }

            // System.DirectoryServices.AccountManagement is *really* slow, use DirectorySearcher directly instead
            using var ds = new DirectorySearcher();
            if (samAccountNameOrSid.StartsWith("S-"))
            {
                var sid = new SecurityIdentifier(samAccountNameOrSid);
                byte[] sidBytes = new byte[sid.BinaryLength];
                sid.GetBinaryForm(sidBytes, 0);
                var sb = new StringBuilder();
                foreach (var b in sidBytes)
                {
                    sb.AppendFormat(@"\{0:X2}", b);
                }
                ds.Filter = String.Format("(& (objectCategory=person)(objectClass=user)(objectSid={0}))", sb.ToString());
            }
            else
            {
                ds.Filter = String.Format("(& (objectCategory=person)(objectClass=user)(sAMAccountName={0}))", samAccountNameOrSid);
            }

            ds.PropertiesToLoad.AddRange(new string[]
            {
                    "userPrincipalName",
                    "sAMAccountName",
                    "objectSid",
                    "objectGUID",
                    "distinguishedName",
                    "accountExpires",
                    "userAccountControl",
                    "department",
                    "displayName",
                    "givenName",
                    "sn", // surname
                    "title",
                    "mail", // email address
                    "comment", // "Affiliate id 1234567890"
            });

            var result = ds.FindOne();

            if (result == null)
            {
                return null;
            }

            // get group membership, cannot be part of PropertiesToLoad
            using var user = result.GetDirectoryEntry();
            if (loadGroupNames)
            {
                user.RefreshCache(new string[] { "tokenGroups" });
            }

            var identity = new ClaimsIdentity("ActiveDirectory");
            identity.AddClaim(new Claim(ClaimTypes.WindowsAccountName, (string)user.Properties["sAMAccountName"][0]));
            identity.AddClaim(new Claim(ClaimTypes.PrimarySid, new SecurityIdentifier((byte[])user.Properties["objectSid"][0], 0).ToString(), ClaimValueTypes.Sid));
            identity.AddClaim(new Claim(ClaimTypes.X500DistinguishedName, (string)user.Properties["distinguishedName"][0]));
            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, (string)user.Properties["userPrincipalName"][0]));
            identity.AddClaim(new Claim(CAS.Constants.IDENTITY_CLAIM, "ActiveDirectory"));

            if (user.Properties.Contains("givenName"))
            {
                identity.AddClaim(new Claim(ClaimTypes.GivenName, (string)user.Properties["givenName"][0]));
            }

            if (user.Properties.Contains("sn"))
            {
                identity.AddClaim(new Claim(ClaimTypes.Surname, (string)user.Properties["sn"][0]));
            }

            if (user.Properties.Contains("comment"))
            {
                m = affiliateIdRegex.Match((string)user.Properties["comment"][0]);
                if (m.Success)
                {
                    identity.AddClaim(new Claim(AffiliateId, m.Groups[1].Value));
                }
            }

            if (user.Properties.Contains("displayName"))
            {
                identity.AddClaim(new Claim(ClaimTypes.Name, (string)user.Properties["displayName"][0]));
            }
            else if (user.Properties.Contains("givenName"))
            {
                if (user.Properties["sn"].Count > 0)
                {
                    identity.AddClaim(new Claim(ClaimTypes.Name, String.Format("{0} {1}", (string)user.Properties["givenName"][0], (string)user.Properties["sn"][0])));
                }
                else
                {
                    identity.AddClaim(new Claim(ClaimTypes.Name, (string)user.Properties["givenName"][0]));
                }
            }

            if (user.Properties["mail"].Count > 0)
            {
                identity.AddClaim(new Claim(ClaimTypes.Email, (string)user.Properties["mail"][0]));
            }

            if (user.Properties["title"].Count > 0)
            {
                identity.AddClaim(new Claim("Title", (string)user.Properties["title"][0]));
            }

            if (user.Properties["department"].Count > 0)
            {
                identity.AddClaim(new Claim("DepartmentName", (string)user.Properties["department"][0]));
            }

            if (loadGroupNames)
            {
                foreach (byte[] groupSid in user.Properties["tokenGroups"])
                {
                    var sid = new SecurityIdentifier(groupSid, 0).ToString();
                    identity.AddClaim(new Claim(ClaimTypes.GroupSid, sid, ClaimValueTypes.Sid));

                    using var gds = new DirectorySearcher
                    {
                        Filter = String.Format("(& (objectCategory=group)(objectSid={0}))", sid)
                    };

                    gds.PropertiesToLoad.AddRange(new string[] { "distinguishedName", "displayName", "name" });
                    var gres = gds.FindOne();

                    if (gres == null)
                    {
                        continue;
                    }

                    var dn = (string)gres.Properties["distinguishedName"][0];
                    identity.AddClaim(new Claim("Group", dn));

                    m = deptIdRegex.Match(dn);
                    if (m.Success)
                    {
                        identity.AddClaim(new Claim("DepartmentCode", m.Groups[1].Value));
                    }

                    m = userAffilRegex.Match(dn);
                    if (m.Success)
                    {
                        identity.AddClaim(new Claim("Affiliation", m.Groups[1].Value));
                    }

                    if (gres.Properties.Contains("displayName"))
                    {
                        identity.AddClaim(new Claim(ClaimTypes.Role, (string)gres.Properties["displayName"][0]));
                    }
                    else
                    {
                        identity.AddClaim(new Claim(ClaimTypes.Role, (string)gres.Properties["name"][0]));
                    }
                }
            }

            return identity;
        }

        /// <summary>
        /// Gets all members of a given group
        /// </summary>
        /// <param name="groupName">Group name or SID</param>
        /// <param name="includeNestedMembers"></param>
        /// <returns>List of ASURITEs for group members</returns>
        public static List<string> GetGroupMembers(string groupName, bool includeNestedMembers = true)
        {
            Match m;
            string groupDN;
            var members = new List<string>();
            var validInputRegex = new Regex("^[a-zA-Z0-9-_.]+$");

            if (groupName == null)
            {
                throw new ArgumentNullException(nameof(groupName));
            }

            m = validInputRegex.Match(groupName);
            if (!m.Success)
            {
                throw new ArgumentOutOfRangeException(nameof(groupName), "Argument contains invalid characters");
            }

            using (var ds = new DirectorySearcher())
            {
                if (groupName.StartsWith("S-"))
                {
                    var sid = new SecurityIdentifier(groupName);
                    byte[] sidBytes = new byte[sid.BinaryLength];
                    sid.GetBinaryForm(sidBytes, 0);
                    var sb = new StringBuilder();
                    foreach (var b in sidBytes)
                    {
                        sb.AppendFormat(@"\{0:X2}", b);
                    }
                    ds.Filter = String.Format("(& (objectCategory=group)(objectSid={0}))", sb.ToString());
                }
                else
                {
                    ds.Filter = String.Format("(& (objectCategory=group)(sAMAccountName={0}))", groupName);
                }

                var result = ds.FindOne();
                if (result == null)
                {
                    return null;
                }

                using var group = result.GetDirectoryEntry();
                groupDN = (string)group.Properties["distinguishedName"][0];
            }

            using (var ds = new DirectorySearcher())
            {
                if (includeNestedMembers)
                {
                    ds.Filter = String.Format("(& (objectCategory=person)(objectClass=user)(memberOf:1.2.840.113556.1.4.1941:=CN={0}))", groupDN);
                }
                else
                {
                    ds.Filter = String.Format("(& (objectCategory=person)(objectClass=user)(memberOf=CN={0}))", groupDN);
                }

                ds.PropertiesToLoad.Add("sAMAccountName");
                ds.PageSize = 1000;

                using var results = ds.FindAll();
                foreach (SearchResult r in results)
                {
                    members.Add(r.Properties["sAMAccountName"][0].ToString());
                }
            }

            return members;
        }

        /// <summary>
        /// Fetches all groups asynchronously
        /// </summary>
        /// <returns></returns>
        public static Task<SortedSet<string>> GetAllGroupsAsync()
        {
            return Task.Run(() =>
                {
                    var groups = GetAllGroups();
                    return Task.FromResult(groups);
                });
        }

        private static SortedSet<string> GetAllGroups()
        {
            var groups = new SortedSet<string>();

            using var search = new DirectorySearcher
            {
                // groupType filter pulled from MSDN, this returns all security groups while excluding DLs
                Filter = "(&(objectCategory=group)(groupType:1.2.840.113556.1.4.803:=2147483648))",
                PageSize = 1000
            };

            search.PropertiesToLoad.Add("distinguishedName");
            search.PropertiesToLoad.Add("displayName");

            using var results = search.FindAll();
            foreach (SearchResult r in results)
            {
                if (r.Properties.Contains("displayName"))
                {
                    groups.Add(r.Properties["displayName"][0].ToString());
                }
            }

            return groups;
        }
    }
}

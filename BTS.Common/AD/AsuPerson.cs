using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace BTS.Common.AD
{
    /// <summary>
    /// Encapsulates information about someone who is affiliated with ASU
    /// </summary>
    public class AsuPerson
    {
        /// <summary>
        /// The underlying ClaimsPrincipal for this person
        /// </summary>
        public ClaimsPrincipal Principal { get; private set; }

        /// <summary>
        /// The person's full name. May contain (Student) at the end.
        /// </summary>
        public string Name => Principal.FindFirst(ClaimTypes.Name).Value;

        /// <summary>
        /// The person's first name. May be null.
        /// </summary>
        public string FirstName => Principal.FindFirst(ClaimTypes.GivenName)?.Value;

        /// <summary>
        /// The person's last name. May be null.
        /// </summary>
        public string LastName => Principal.FindFirst(ClaimTypes.Surname)?.Value;

        /// <summary>
        /// The person's primary email address
        /// </summary>
        public string Email => Principal.FindFirst(ClaimTypes.Email).Value;

        /// <summary>
        /// The person's ASURITE id
        /// </summary>
        public string Asurite => Principal.FindFirst(ClaimTypes.WindowsAccountName).Value;

        /// <summary>
        /// The person's EmplId (Affiliate Id). May be null for non-employees.
        /// </summary>
        public string EmplId => Principal.FindFirst(ActiveDirectory.AffiliateId)?.Value;

        /// <summary>
        /// All department codes that the user belongs to, in no particular order.
        /// </summary>
        public IEnumerable<string> DepartmentCodes => Principal.FindAll("DepartmentCode").Select(o => o.Value);

        /// <summary>
        /// All affiliations that the user has, in no particular order.
        /// </summary>
        public IEnumerable<string> Affiliations => Principal.FindAll("Affiliation").Select(o => o.Value);

        /// <summary>
        /// The person's job title. May be null.
        /// </summary>
        public string Title => Principal.FindFirst("Title")?.Value;

        /// <summary>
        /// Constructs a new AsuPerson instance. See also AsuPerson.GetByAsurite()
        /// for retrieving a class instance given an asurite.
        /// </summary>
        /// <param name="principal">Principal containing already-loaded user information</param>
        public AsuPerson(ClaimsPrincipal principal)
        {
            Principal = principal;
        }

        /// <summary>
        /// Looks up a person given their ASURITE id.
        /// </summary>
        /// <param name="asurite">ASURITE to look up.</param>
        /// <returns>AsuPerson instance if found, null if the ASURITE cannot be found.</returns>
        public static AsuPerson GetByAsurite(string asurite)
        {
            var id = ActiveDirectory.GetUserInfo(asurite);
            if (id == null)
            {
                return null;
            }

            return new AsuPerson(new ClaimsPrincipal(id));
        }
    }
}

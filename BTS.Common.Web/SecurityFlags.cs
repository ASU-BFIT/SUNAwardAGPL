using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BTS.Common.Web
{
    /// <summary>
    /// The access level to check when looking at security permissions. When assigning access,
    /// a bitfield of these flags should be assigned according to the user's permission level.
    /// </summary>
    [Flags]
    public enum SecurityFlags : int
    {
        /// <summary>No access.</summary>
        None = 0,
        /// <summary>Ability to view existing records.</summary>
        View = 1,
        /// <summary>Ability to modify existing records.</summary>
        Edit = 2,
        /// <summary>Ability to create new records.</summary>
        Create = 4,
        /// <summary>Ability to delete existing records.</summary>
        Delete = 8
    }
}

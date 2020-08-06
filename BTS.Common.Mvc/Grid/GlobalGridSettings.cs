using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BTS.Common.Mvc.Grid
{
    /// <summary>
    /// Grid settings set on a global (instead of per-grid) level
    /// </summary>
    public static class GlobalGridSettings
    {
        /// <summary>
        /// Data storage wrapper used to persist per-user grid settings
        /// </summary>
        public static IGridPersistence PersistenceClass { get; set; }
    }
}

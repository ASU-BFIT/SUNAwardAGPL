using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BTS.Common.Mvc.Grid
{
    /// <summary>
    /// Represents what we can export grid data to
    /// </summary>
    public enum ExportType
    {
        /// <summary>
        /// Export to CSV
        /// </summary>
        CSV = 0,
        /// <summary>
        /// Export to excel (XLSX)
        /// </summary>
        Excel,
        /// <summary>
        /// Export to PDF
        /// </summary>
        PDF
    }
}

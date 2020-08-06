using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BTS.Common.Mvc.Grid
{
    /// <summary>
    /// Row select behavior in grids
    /// </summary>
    public enum RowSelectType
    {
        /// <summary>
        /// Rows cannot be selected
        /// </summary>
        None,
        /// <summary>
        /// A single row can be selected (clicking a row highlights it)
        /// </summary>
        Single,
        /// <summary>
        /// Multiple rows can be selected (a checkbox is rendered in front of each row)
        /// </summary>
        Multiple
    }
}

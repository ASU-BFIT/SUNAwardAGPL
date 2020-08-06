using System;
using System.Runtime.InteropServices;

namespace BTS.Common.Excel
{
    /// <summary>
    /// Style of font used in a cell
    /// </summary>
    [Guid("CF1320EA-D070-4B64-9464-69CD24DFF101")]
    public enum FontFamily : int
    {
        /// <summary>Unknown/Not Applicable/Don't Care</summary>
        Generic = 0,
        /// <summary>Proportional Serif</summary>
        Roman = 1,
        /// <summary>Proportional Sans Serif</summary>
        Swiss = 2,
        /// <summary>Monospace</summary>
        Modern = 3,
        /// <summary>Handwriting</summary>
        Script = 4,
        /// <summary>Novelty Font</summary>
        Decorative = 5
    }
}

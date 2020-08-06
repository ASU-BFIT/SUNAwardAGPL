using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace BTS.Common.Excel
{
    /// <summary>
    /// How cell text is aligned vertically within the cell
    /// </summary>
    // Keep ids in sync with VerticalAlignmentValues enum as we cast between the two
    [Guid("DD54E3DA-37F6-4479-BF1C-3094CBD9C155")]
    public enum VerticalAlignment : int
    {
        /// <summary>
        /// Top
        /// </summary>
        [Description("top")]
        Top = 0,
        /// <summary>
        /// Center
        /// </summary>
        [Description("center")]
        Center = 1,
        /// <summary>
        /// Bottom
        /// </summary>
        [Description("bottom")]
        Bottom = 2,
        /// <summary>
        /// Justify
        /// </summary>
        [Description("justify")]
        Justify = 3,
        /// <summary>
        /// Distributed
        /// </summary>
        [Description("distributed")]
        Distributed = 4
    }
}

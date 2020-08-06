using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace BTS.Common.Excel
{
    /// <summary>
    /// How cell text is aligned horizontally
    /// </summary>
    // Keep ids in sync with HorizontalAlignmentValues enum as we cast between the two
    [Guid("FEEB95CA-4D75-4FAD-8A47-75300E17F994")]
    public enum HorizontalAlignment : int
    {
        /// <summary>
        /// General alignment (autodetect based on content)
        /// </summary>
        [Description("general")]
        General = 0,
        /// <summary>
        /// Align left
        /// </summary>
        [Description("left")]
        Left = 1,
        /// <summary>
        /// Align center
        /// </summary>
        [Description("center")]
        Center = 2,
        /// <summary>
        /// Align right
        /// </summary>
        [Description("right")]
        Right = 3,
        /// <summary>
        /// Fill
        /// </summary>
        [Description("fill")]
        Fill = 4,
        /// <summary>
        /// Justify
        /// </summary>
        [Description("justify")]
        Justify = 5,
        /// <summary>
        /// Center Continuous
        /// </summary>
        [Description("centerContinuous")]
        CenterContinuous = 6,
        /// <summary>
        /// Distributed
        /// </summary>
        [Description("distributed")]
        Distributed = 7
    }
}

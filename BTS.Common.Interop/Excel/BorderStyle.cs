using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace BTS.Common.Excel
{
    /// <summary>
    /// How the border around the cell is drawn
    /// </summary>
    // Keep ids in sync with BorderStyleValues enum as we cast between the two
    [Guid("94E5FDBB-CF2F-44A4-A6DB-E23F2D02F7F3")]
    public enum BorderStyle : int
    {
        /// <summary>
        /// No border
        /// </summary>
        [Description("none")]
        None = 0,
        /// <summary>
        /// Thin solid border
        /// </summary>
        [Description("thin")]
        Solid = 1,
        /// <summary>
        /// Medium solid border
        /// </summary>
        [Description("medium")]
        MediumSolid = 2,
        /// <summary>
        /// Thin dashed border
        /// </summary>
        [Description("dashed")]
        Dashed = 3,
        /// <summary>
        /// Thin dotted border
        /// </summary>
        [Description("dotted")]
        Dotted = 4,
        /// <summary>
        /// Thick solid border
        /// </summary>
        [Description("thick")]
        ThickSolid = 5,
        /// <summary>
        /// Double solid line border
        /// </summary>
        [Description("double")]
        DoubleLine = 6,
        /// <summary>
        /// Hairline border
        /// </summary>
        [Description("hair")]
        Hairline = 7,
        /// <summary>
        /// Medium dashed border
        /// </summary>
        [Description("mediumDashed")]
        MediumDashed = 8,
        /// <summary>
        /// Thin border alternating between dashes and dots
        /// </summary>
        [Description("dashDot")]
        DashDot = 9,
        /// <summary>
        /// Medium border alternating between dashes and dots
        /// </summary>
        [Description("mediumDashDot")]
        MediumDashDot = 10,
        /// <summary>
        /// Thin border alternating between dashes and two dots
        /// </summary>
        [Description("dashDotDot")]
        DashDotDot = 11,
        /// <summary>
        /// Medium border alternating between dashes and two dots
        /// </summary>
        [Description("mediumDashDotDot")]
        MediumDashDotDot = 12,
        /// <summary>
        /// Slant dash dot
        /// </summary>
        [Description("slantDashDot")]
        SlantDashDot = 13
    }
}

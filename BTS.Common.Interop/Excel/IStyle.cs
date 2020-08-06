using System;
using System.Runtime.InteropServices;

namespace BTS.Common.Excel
{
    /// <summary>
    /// Excel cell styling. This interface is meant for COM interoperability.
    /// If not using COM, use the Style class directly.
    /// </summary>
    [Guid("4B50FB3C-3BAE-45E2-9938-42A91F8B851C")]
    public interface IStyle
    {
        /// <summary>
        /// Whether or not the font is bold
        /// </summary>
        [DispId(1)]
        bool Bold { get; set; }

        /// <summary>
        /// Whether or not the font is italic
        /// </summary>
        [DispId(2)]
        bool Italic { get; set; }

        /// <summary>
        /// Color of the cell text
        /// </summary>
        [DispId(3)]
        string TextColor { get; set; }

        /// <summary>
        /// Background color of the cell
        /// </summary>
        [DispId(4)]
        string FillColor { get; set; }

        /// <summary>
        /// How numbers are formatted in the cell, using Excel's number formatting syntax
        /// </summary>
        [DispId(5)]
        string NumberFormat { get; set; }

        /// <summary>
        /// Horizontal alignment
        /// </summary>
        [DispId(10)]
        HorizontalAlignment HorizontalAlignment { get; set; }

        /// <summary>
        /// Vertical alignment
        /// </summary>
        [DispId(11)]
        VerticalAlignment VerticalAlignment { get; set; }

        /// <summary>
        /// Whether or not cell contents should be shrunk to fit the cell
        /// </summary>
        [DispId(12)]
        bool ShrinkToFit { get; set; }

        /// <summary>
        /// Whether or not cell contents can wrap to multiple lines
        /// </summary>
        [DispId(13)]
        bool WrapText { get; set; }

        /// <summary>
        /// Rotation of the cell contents
        /// </summary>
        [DispId(14)]
        uint Rotation { get; set; }

        /// <summary>
        /// Style for the top border
        /// </summary>
        [DispId(20)]
        BorderStyle TopBorderStyle { get; set; }

        /// <summary>
        /// Color for the top border
        /// </summary>
        [DispId(21)]
        string TopBorderColor { get; set; }

        /// <summary>
        /// Style for the bottom border
        /// </summary>
        [DispId(22)]
        BorderStyle BottomBorderStyle { get; set; }

        /// <summary>
        /// Color for the bottom border
        /// </summary>
        [DispId(23)]
        string BottomBorderColor { get; set; }

        /// <summary>
        /// Style for the left border
        /// </summary>
        [DispId(24)]
        BorderStyle LeftBorderStyle { get; set; }

        /// <summary>
        /// Color for the left border
        /// </summary>
        [DispId(25)]
        string LeftBorderColor { get; set; }

        /// <summary>
        /// Style for the right border
        /// </summary>
        [DispId(26)]
        BorderStyle RightBorderStyle { get; set; }

        /// <summary>
        /// Color for the right border
        /// </summary>
        [DispId(27)]
        string RightBorderColor { get; set; }

        /// <summary>
        /// Font to use
        /// </summary>
        [DispId(30)]
        string FontName { get; set; }

        /// <summary>
        /// Font size to use
        /// </summary>
        [DispId(31)]
        int FontSize { get; set; }

        /// <summary>
        /// Font family of the chosen font, needed for
        /// proper cell width calculations
        /// </summary>
        [DispId(32)]
        FontFamily FontFamily { get; set; }

        /// <summary>
        /// Set all borders to the given style
        /// </summary>
        /// <param name="style"></param>
        [DispId(200)]
        void SetBorderStyle(BorderStyle style);

        /// <summary>
        /// Set all borders to the given color
        /// </summary>
        /// <param name="color"></param>
        [DispId(201)]
        void SetBorderColor(string color);

        /// <summary>
        /// Create a clone of this style object
        /// </summary>
        /// <returns></returns>
        [ComVisible(false)]
        IStyle Clone();
    }
}

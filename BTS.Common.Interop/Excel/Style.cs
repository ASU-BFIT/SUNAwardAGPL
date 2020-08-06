using System;
using System.Runtime.InteropServices;

namespace BTS.Common.Excel
{
    /// <summary>
    /// Excel cell styling
    /// </summary>
    [ClassInterface(ClassInterfaceType.None),
     Guid("3E10CFC2-0CBD-4827-BD96-B68880FA2632")]
    public sealed class Style : IStyle, ICloneable
    {
        /// <summary>
        /// Whether or not the font is bold
        /// </summary>
        public bool Bold { get; set; }

        /// <summary>
        /// Whether or not the font is italic
        /// </summary>
        public bool Italic { get; set; }

        /// <summary>
        /// Color of the cell text
        /// </summary>
        public string TextColor { get; set; }

        /// <summary>
        /// Background color of the cell
        /// </summary>
        public string FillColor { get; set; }

        /// <summary>
        /// How numbers are formatted in the cell, using Excel's number formatting syntax
        /// </summary>
        public string NumberFormat { get; set; }

        /// <summary>
        /// Horizontal alignment
        /// </summary>
        public HorizontalAlignment HorizontalAlignment { get; set; }

        /// <summary>
        /// Vertical alignment
        /// </summary>
        public VerticalAlignment VerticalAlignment { get; set; }

        /// <summary>
        /// Whether or not cell contents should be shrunk to fit the cell
        /// </summary>
        public bool ShrinkToFit { get; set; }

        /// <summary>
        /// Whether or not cell contents can wrap to multiple lines
        /// </summary>
        public bool WrapText { get; set; }

        /// <summary>
        /// Rotation of the cell contents
        /// </summary>
        public uint Rotation { get; set; }

        /// <summary>
        /// Style for the top border
        /// </summary>
        public BorderStyle TopBorderStyle { get; set; }

        /// <summary>
        /// Color for the top border
        /// </summary>
        public string TopBorderColor { get; set; }

        /// <summary>
        /// Style for the bottom border
        /// </summary>
        public BorderStyle BottomBorderStyle { get; set; }

        /// <summary>
        /// Color for the bottom border
        /// </summary>
        public string BottomBorderColor { get; set; }

        /// <summary>
        /// Style for the left border
        /// </summary>
        public BorderStyle LeftBorderStyle { get; set; }

        /// <summary>
        /// Color for the left border
        /// </summary>
        public string LeftBorderColor { get; set; }

        /// <summary>
        /// Style for the right border
        /// </summary>
        public BorderStyle RightBorderStyle { get; set; }

        /// <summary>
        /// Color for the right border
        /// </summary>
        public string RightBorderColor { get; set; }

        /// <summary>
        /// Font to use
        /// </summary>
        public string FontName { get; set; }

        /// <summary>
        /// Font size to use
        /// </summary>
        public int FontSize { get; set; }

        /// <summary>
        /// Font family of the chosen font, needed for
        /// proper cell width calculations
        /// </summary>
        public FontFamily FontFamily { get; set; }

        /// <summary>
        /// Constructs a new style with Excel defaults (Calibri 11)
        /// </summary>
        public Style()
        {
            // apply default values
            VerticalAlignment = VerticalAlignment.Bottom;
            FontName = "Calibri";
            FontSize = 11;
        }


        /// <summary>
        /// Set all borders to the given style
        /// </summary>
        /// <param name="style"></param>
        public void SetBorderStyle(BorderStyle style)
        {
            TopBorderStyle = style;
            BottomBorderStyle = style;
            LeftBorderStyle = style;
            RightBorderStyle = style;
        }

        /// <summary>
        /// Set all borders to the given color
        /// </summary>
        /// <param name="color"></param>
        public void SetBorderColor(string color)
        {
            TopBorderColor = color;
            BottomBorderColor = color;
            LeftBorderColor = color;
            RightBorderColor = color;
        }

        /// <summary>
        /// Create a clone of this style
        /// </summary>
        /// <returns></returns>
        object ICloneable.Clone()
        {
            return MemberwiseClone();
        }

        /// <summary>
        /// Create a clone of this style
        /// </summary>
        /// <returns></returns>
        IStyle IStyle.Clone()
        {
            return (IStyle)((ICloneable)this).Clone();
        }
    }
}

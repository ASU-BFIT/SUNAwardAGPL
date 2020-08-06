using System;
using System.Collections.Generic;

using Bitmap = System.Drawing.Bitmap;
using CharacterRange = System.Drawing.CharacterRange;
using DFont = System.Drawing.Font;
using DFontFamily = System.Drawing.FontFamily;
using DFontStyle = System.Drawing.FontStyle;
using DGraphicsUnit = System.Drawing.GraphicsUnit;
using Graphics = System.Drawing.Graphics;
using RectangleF = System.Drawing.RectangleF;
using StringFormat = System.Drawing.StringFormat;

namespace BTS.Common.Excel
{
    /// <summary>
    /// Calculates widths of a string rendered in a given font and caches them for later use
    /// This is not 100% accurate in all cases, but gets close enough to be usable
    /// </summary>
    internal static class WidthCache
    {
        private static readonly Dictionary<Tuple<string, double>, double> cache = new Dictionary<Tuple<string, double>, double>();
        private static readonly string[] nums = new string[] { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };

        /// <summary>
        /// Retrieve the pixel width of the widest digit in this font
        /// </summary>
        /// <param name="font">Font to render</param>
        /// <param name="ptSize">Font size (in points)</param>
        /// <param name="bold">Whether or not the font is bold</param>
        /// <returns></returns>
        public static double GetMaxDigitWidth(string font, double ptSize, bool bold)
        {
            var key = new Tuple<string, double>(font, ptSize);
            if (cache.TryGetValue(key, out double pxSize))
            {
                return pxSize;
            }

            foreach (var n in nums)
            {
                pxSize = Math.Floor(Math.Max(pxSize, GetStringWidth(n, font, ptSize, bold)));
            }

            cache[key] = pxSize;
            return pxSize;
        }

        /// <summary>
        /// Retrieve the pixel width of the string rendered in the given font
        /// </summary>
        /// <param name="data">String to render</param>
        /// <param name="font">Font to render the string in</param>
        /// <param name="ptSize">Font size (in points)</param>
        /// <param name="bold">Whether or not the font is bold</param>
        /// <returns></returns>
        public static double GetStringWidth(string data, string font, double ptSize, bool bold = false)
        {
            using var ff = new DFontFamily(font);
            using var f = new DFont(ff, (float)ptSize, (bold ? DFontStyle.Bold : DFontStyle.Regular), DGraphicsUnit.Point);
            using var b = new Bitmap(1000, 200);
            using var g = Graphics.FromImage(b);
            using var sf = new StringFormat(StringFormat.GenericTypographic);
            var r = new RectangleF(0, 0, 1000, 200);
            CharacterRange[] cr = { new CharacterRange(0, data.Length) };

            sf.SetMeasurableCharacterRanges(cr);
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
            var rg = g.MeasureCharacterRanges(data, f, r, sf);

            if (rg.Length == 0)
            {
                return 0;
            }

            return rg[0].GetBounds(g).Size.Width;
        }
    }
}

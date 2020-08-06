using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

using ExcelWorkbook = DocumentFormat.OpenXml.Spreadsheet.Workbook;

namespace BTS.Common.Excel
{
    /// <summary>
    /// Excel workbook
    /// </summary>
    [ClassInterface(ClassInterfaceType.None),
     Guid("A6E39CFA-00DD-4272-B5F6-78F2D06532C8")]
    public sealed class Workbook : IWorkbook
    {
        private readonly List<Worksheet> worksheets;
        internal SharedStringTable st;
        private readonly Stylesheet ss;

        /// <summary>
        /// Sheets in the workbook
        /// </summary>
        public IEnumerable<Worksheet> Worksheets
        {
            get
            {
                return worksheets;
            }
        }

        IEnumerable IWorkbook.Worksheets
        {
            get
            {
                return worksheets;
            }
        }

        /// <summary>
        /// Constructs a new workbook with no worksheets
        /// </summary>
        public Workbook()
        {
            worksheets = new List<Worksheet>();
            st = new SharedStringTable();
            ss = new Stylesheet();

            st.Count = 0;
            st.UniqueCount = 0;

            // populate initial stylesheet data
            var fonts = new Fonts();
            fonts.Append(new Font()
            {
                FontName = new FontName() { Val = "Calibri" },
                FontSize = new FontSize() { Val = 11 },
                FontFamilyNumbering = new FontFamilyNumbering() { Val = 2 },
            });

            fonts.Count = (uint)fonts.ChildElements.Count;

            var fills = new Fills();
            fills.Append(new Fill() { PatternFill = new PatternFill() { PatternType = PatternValues.None } });
            fills.Append(new Fill() { PatternFill = new PatternFill() { PatternType = PatternValues.Gray125 } });
            fills.Count = (uint)fills.ChildElements.Count;

            var borders = new Borders();
            borders.Append(new Border()
            {
                LeftBorder = new LeftBorder() { Style = BorderStyleValues.None },
                RightBorder = new RightBorder() { Style = BorderStyleValues.None },
                TopBorder = new TopBorder() { Style = BorderStyleValues.None },
                BottomBorder = new BottomBorder() { Style = BorderStyleValues.None }
            });
            borders.Count = (uint)borders.ChildElements.Count;

            var yfmts = new CellStyleFormats();
            yfmts.Append(new CellFormat()
            {
                NumberFormatId = 0,
                FontId = 0,
                FillId = 0,
                BorderId = 0
            });
            yfmts.Count = (uint)yfmts.ChildElements.Count;

            var cfmts = new CellFormats();
            cfmts.Append(new CellFormat()
            {
                BorderId = 0,
                FillId = 0,
                FontId = 0,
                NumberFormatId = 0, // General
                FormatId = 0,
                ApplyNumberFormat = false,
                ApplyFont = false,
                ApplyFill = false,
                ApplyBorder = false,
                ApplyAlignment = false
            });
            cfmts.Append(new CellFormat()
            {
                BorderId = 0,
                FillId = 0,
                FontId = 0,
                NumberFormatId = 14, // date
                FormatId = 0,
                ApplyNumberFormat = true,
                ApplyFont = false,
                ApplyFill = false,
                ApplyBorder = false,
                ApplyAlignment = false
            });
            cfmts.Count = (uint)cfmts.ChildElements.Count;

            var csty = new CellStyles();
            csty.Append(new CellStyle()
            {
                Name = "Normal",
                FormatId = 0,
                BuiltinId = 0
            });
            csty.Count = (uint)csty.ChildElements.Count;

            var num = new NumberingFormats()
            {
                Count = 0
            };

            ss.Fonts = fonts;
            ss.Fills = fills;
            ss.Borders = borders;
            ss.CellStyleFormats = yfmts;
            ss.CellFormats = cfmts;
            ss.CellStyles = csty;
            ss.NumberingFormats = num;
        }

        /// <summary>
        /// Add a new sheet to the workbook.
        /// </summary>
        /// <param name="sheetName">Sheet name. If null, generates an automatic name like Sheet1, Sheet2, etc.</param>
        /// <returns>The newly-created worksheet</returns>
        public IWorksheet AddSheet(string sheetName = null)
        {
            var ws = new Worksheet(this, sheetName);
            worksheets.Add(ws);

            return ws;
        }

        private void SaveInternal(SpreadsheetDocument doc)
        {
            var sheets = new Sheets();
            var wbPart = doc.AddWorkbookPart();
            wbPart.Workbook = new ExcelWorkbook();

            uint i = 1;
            foreach (var sheet in worksheets)
            {
                // ensure most recent row is saved in sheet
                sheet.NextRow();

                // serialize column widths
                var cols = sheet.ws.Elements<Columns>().First();
                if (!cols.Any())
                {
                    foreach (var cw in sheet.colWidths)
                    {
                        // default width is 8.43, don't go below that
                        if (cw.Value > 8.43)
                        {
                            cols.AppendChild(new Column()
                            {
                                CustomWidth = true,
                                Min = cw.Key + 1,
                                Max = cw.Key + 1,
                                Width = cw.Value
                            });
                        }
                    }
                }

                if (!cols.Any())
                {
                    // no columns needed custom formatting, but the <cols> element must have 1+ children
                    sheet.ws.RemoveChild(cols);
                }

                var wsPart = wbPart.AddNewPart<WorksheetPart>();
                wsPart.Worksheet = sheet.ws;
                sheets.AppendChild(new Sheet()
                {
                    Id = wbPart.GetIdOfPart(wsPart),
                    SheetId = i,
                    Name = sheet.Name ?? String.Format("Sheet{0}", i)
                });
                i++;
            }

            wbPart.AddNewPart<SharedStringTablePart>().SharedStringTable = st;
            wbPart.AddNewPart<WorkbookStylesPart>().Stylesheet = ss;
            wbPart.Workbook.AppendChild(sheets);
        }

        /// <summary>
        /// Saves the sheet in xlsx format to the given path
        /// </summary>
        /// <param name="path">Path to file to save, will be overwritten</param>
        public void Save(string path)
        {
            using var doc = SpreadsheetDocument.Create(path, SpreadsheetDocumentType.Workbook);
            SaveInternal(doc);
        }

        /// <summary>
        /// Writes the sheet in xlsx format to the given stream
        /// </summary>
        /// <param name="stream">Binary stream to write data to</param>
        public void Save(Stream stream)
        {
            using var doc = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook);
            SaveInternal(doc);
        }

        /// <summary>
        /// Gets the workbook in xlsx format as a binary byte array
        /// </summary>
        /// <returns>Bytes for the workbook file</returns>
        public byte[] GetWorkbook()
        {
            using var stream = new MemoryStream();
            // need doc to dispose before we convert stream to array as disposing is what
            // writes to the stream
            using (var doc = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
            {
                SaveInternal(doc);
            }

            return stream.ToArray();
        }

        private int FindFont(IStyle style)
        {
            int i = -1;
            var fcolor = ResolveColor(style.TextColor);
            FontFamily? fam = style.FontFamily;

            if (fam == FontFamily.Generic)
            {
                fam = null;
            }

            switch (style.FontName)
            {
                case "Courier New":
                case "Consolas":
                    fam ??= FontFamily.Modern;
                    break;
                case "Calibri":
                case "Arial":
                case "Roboto":
                case "Corbel":
                case "Segoe UI":
                    fam ??= FontFamily.Swiss;
                    break;
                case "Times New Roman":
                case "Constantia":
                case "Georgia":
                    fam ??= FontFamily.Roman;
                    break;
                case null:
                    fam ??= FontFamily.Swiss;
                    break;
                default:
                    fam ??= FontFamily.Generic;
                    break;
            }

            foreach (Font item in ss.Fonts)
            {
                i++;

                if (style.Bold != (item.Bold?.Val.Value ?? false))
                {
                    continue;
                }

                if (style.Italic != (item.Italic?.Val.Value ?? false))
                {
                    continue;
                }

                if (fcolor != (item.Color?.Rgb ?? "FF000000"))
                {
                    continue;
                }

                if ((style.FontName ?? "Calibri") != (item.FontName?.Val.Value ?? "Calibri"))
                {
                    continue;
                }

                if (style.FontSize != (item.FontSize?.Val.Value ?? 11))
                {
                    continue;
                }

                if ((int)fam.Value != (item.FontFamilyNumbering?.Val.Value ?? 2))
                {
                    continue;
                }

                return i;
            }

            var font = new Font();

            if (style.Bold)
            {
                font.Bold = new Bold() { Val = true };
            }

            if (style.Italic)
            {
                font.Italic = new Italic() { Val = true };
            }

            if (fcolor != "FF000000")
            {
                font.Color = new Color() { Rgb = fcolor };
            }

            // always include the following attributes
            font.FontName = new FontName() { Val = style.FontName ?? "Calibri" };
            font.FontSize = new FontSize() { Val = style.FontSize };
            font.FontFamilyNumbering = new FontFamilyNumbering() { Val = (int)fam };

            ss.Fonts.AppendChild(font);
            ss.Fonts.Count++;

            return i + 1;
        }

        internal double GetStringWidthByStyle(string data, uint styleId)
        {
            var fmt = ss.CellFormats.Skip((int)styleId).First() as CellFormat;
            var font = ss.Fonts.Skip((int)fmt.FontId.Value).First() as Font;

            return WidthCache.GetStringWidth(data, font.FontName.Val, font.FontSize.Val, (font.Bold?.Val ?? false));
        }

        internal string FormatData(object data, uint styleIdx)
        {
            // get number format string
            var fmt = ss.CellFormats.Skip((int)styleIdx).First() as CellFormat;
            string nfm;

            if (fmt.ApplyNumberFormat)
            {
                uint nfid = fmt.NumberFormatId.Value;
                if (builtinFormats.ContainsValue(nfid))
                {
                    nfm = builtinFormats.First(kvp => kvp.Value == nfid).Key;
                }
                else
                {
                    nfm = ss.NumberingFormats.Cast<NumberingFormat>().First(o => o.NumberFormatId == nfid).FormatCode;
                }
            }
            else
            {
                nfm = "General";
            }

            return FormatExcel(nfm, data);
        }

        /// <summary>
        /// Format data according to an Excel format string
        /// </summary>
        /// <param name="fmt">Excel format string</param>
        /// <param name="data">Data to format</param>
        /// <returns>String with formatted data</returns>
        [ComVisible(false)]
        public static string FormatExcel(string fmt, object data)
        {
            // apply formatting according to
            // https://support.office.com/en-us/article/Create-or-delete-a-custom-number-format-78f2a361-936b-4c03-8772-09fab54be7f4
            // this is going to be ugly, and likely doesn't exactly match how excel does things

            // split string into parts (on ;), but not if they're escaped or in strings
            // to do this, we first strip out all strings into strip markers            
            var stripped = new List<string>();
            fmt = Regex.Replace(fmt, @"(?<!\\)(([""']).*?(?<!\\)\2)", m =>
            {
                stripped.Add(m.Groups[1].Value);
                return String.Format("\u001aSTRIP-{0}\u001b", stripped.Count);
            });

            var parts = Regex.Split(fmt, @"(?<!\\);");
            string[] formats = new string[4] { null, null, null, null };
            int fIdx = 0;
            var conditionals = new List<Tuple<Func<decimal, bool>, string>>();
            foreach (var part in parts)
            {
                // strip color codes
                var cleansed = Regex.Replace(part, @"^\[(?:Black|Green|White|Blue|Magenta|Yellow|Cyan|Red)\]", "");
                // figure out conditionals
                var match = Regex.Match(cleansed, @"^\[(<|<=|>|>=|=)(-?[0-9]+(?:\.[0-9]+)?)\](.*)$");
                if (match.Success)
                {
                    var p = Expression.Parameter(typeof(decimal), "x");
                    var c = Expression.Constant(Convert.ToDecimal(match.Groups[2].Value), typeof(decimal));
                    BinaryExpression b = null;
                    switch (match.Groups[1].Value)
                    {
                        case "<":
                            b = Expression.LessThan(p, c);
                            break;
                        case ">":
                            b = Expression.GreaterThan(p, c);
                            break;
                        case "=":
                            b = Expression.Equal(p, c);
                            break;
                        case ">=":
                            b = Expression.GreaterThanOrEqual(p, c);
                            break;
                        case "<=":
                            b = Expression.LessThanOrEqual(p, c);
                            break;
                    }

                    conditionals.Add(new Tuple<Func<decimal, bool>, string>(Expression.Lambda<Func<decimal, bool>>(b, p).Compile(), match.Groups[3].Value));
                }
                else
                {
                    formats[fIdx++] = cleansed;
                }
            }

            // at this point, let's figure out what sort of data we have
            if (data == null)
            {
                return String.Empty;
            }

            var t = data.GetType();
            t = Nullable.GetUnderlyingType(t) ?? t;
            var code = Type.GetTypeCode(t);
            bool isText = code == TypeCode.String;

            switch (code)
            {
                case TypeCode.String:
                    if (String.IsNullOrEmpty(formats[3]) || formats[3] == "General")
                    {
                        // return string as-is
                        return data.ToString();
                    }

                    fmt = formats[3];
                    break;
                case TypeCode.Boolean:
                    if ((bool)data == true)
                    {
                        return "TRUE";
                    }
                    else
                    {
                        return "FALSE";
                    }
                case TypeCode.DateTime:
                    if (String.IsNullOrEmpty(formats[0]) || formats[0] == "General")
                    {
                        // return date as OADate string
                        return ((DateTime)data).ToOADate().ToString();
                    }

                    fmt = formats[0];
                    break;
                default:
                    // check for a matching conditional
                    fmt = null;
                    var ddata = Convert.ToDecimal(data);
                    foreach (var tup in conditionals)
                    {
                        if (tup.Item1(ddata))
                        {
                            fmt = tup.Item2;
                            if (fmt == "General")
                            {
                                return ddata.ToString();
                            }

                            break;
                        }
                    }

                    // note that fmt remains null after this point; this is just to handle "General"
                    if (fmt == null)
                    {
                        if (ddata == 0)
                        {
                            if (formats[2] == "General" || (String.IsNullOrEmpty(formats[2]) && formats[0] == "General"))
                            {
                                return "0";
                            }
                        }
                        else if (ddata < 0)
                        {
                            if (formats[1] == "General")
                            {
                                return Math.Abs(ddata).ToString();
                            }
                            else if (String.IsNullOrEmpty(formats[1]) && formats[0] == "General")
                            {
                                return ddata.ToString();
                            }
                        }
                        else if (formats[0] == "General")
                        {
                            return ddata.ToString();
                        }
                    }
                    break;
            }

            // fmt is now the string we need to use in order to format
            // unstrip string literals
            if (fmt != null)
            {
                fmt = Regex.Replace(fmt, @"\u001aSTRIP-([0-9]+)\u001b", m => stripped[Convert.ToInt32(m.Groups[1].Value) - 1]);
            }
            else
            {
                for (int i = 0; i < 4; i++)
                {
                    if (formats[i] == null)
                    {
                        continue;
                    }

                    formats[i] = Regex.Replace(formats[i], @"\u001aSTRIP-([0-9]+)\u001b", m => stripped[Convert.ToInt32(m.Groups[1].Value) - 1]);
                }
            }

            if (isText)
            {
                // @ is a placeholder for the actual text, take everything else as literal
                string final = "";
                bool escape = false;
                char? strip = null;
                foreach (char c in fmt)
                {
                    if (escape)
                    {
                        final += c;
                        escape = false;
                        continue;
                    }

                    if (c == '\\')
                    {
                        escape = true;
                        continue;
                    }

                    if (strip != null)
                    {
                        if (c == strip)
                        {
                            strip = null;
                        }
                        else
                        {
                            final += c;
                        }

                        continue;
                    }

                    if (c == '\'' || c == '"')
                    {
                        strip = c;
                        continue;
                    }

                    if (c == '@')
                    {
                        final += data.ToString();
                        continue;
                    }

                    final += c;
                }
            }
            else if (fmt == null)
            {
                fmt = formats[0];
                if (formats[1] != null)
                {
                    fmt += ";" + formats[1];
                }
                if (formats[2] != null)
                {
                    fmt += ";" + formats[2];
                }
            }

            // builtin String.Format does pretty much everything Excel does, minus the specialness for text
            // there are other minor differences, but those hopefully shouldn't matter
            return String.Format("{0:" + fmt + "}", data);
        }

        private readonly Dictionary<string, uint> builtinFormats = new Dictionary<string, uint>()
        {
            { "General", 0 },
            { "0", 1 },
            { "0.00", 2 },
            { "#,##0", 3 },
            { "#,##0.00", 4 },
            { "0%", 9 },
            { "0.00%", 10 },
            { "0.00E+00", 11 },
            { "# ?/?", 12 },
            { "# ??/??", 13 },
            { "mm-dd-yy", 14 },
            { "d-mmm-yy", 15 },
            { "d-mmm", 16 },
            { "mmm-yy", 17 },
            { "h:mm AM/PM", 18 },
            { "h:mm:ss AM/PM", 19 },
            { "h:mm", 20 },
            { "h:mm:ss", 21 },
            { "m/d/yy h:mm", 22 },
            { "#,##0 }(#,##0)", 37 },
            { "#,##0 }[Red](#,##0)", 38 },
            { "#,##0.00 }(#,##0.00)", 39 },
            { "#,##0.00 }[Red](#,##0.00)", 40 },
            { "mm:ss", 45 },
            { "[h]:mm:ss", 46 },
            { "mmss.0", 47 },
            { "##0.0E+0", 48 },
            { "@", 49 }
        };

        private uint FindNumberFormat(IStyle style)
        {
            if (style.NumberFormat == null)
            {
                return 0;
            }

            if (builtinFormats.ContainsKey(style.NumberFormat))
            {
                return builtinFormats[style.NumberFormat];
            }

            foreach (var child in ss.NumberingFormats)
            {
                var nfm = child as NumberingFormat;

                if (nfm?.FormatCode == style.NumberFormat)
                {
                    return nfm.NumberFormatId.Value;
                }
            }

            uint id = ss.NumberingFormats.Count + 100;
            var newnfm = new NumberingFormat()
            {
                FormatCode = style.NumberFormat,
                NumberFormatId = id
            };

            ss.NumberingFormats.AppendChild(newnfm);
            ss.NumberingFormats.Count++;

            return id;
        }

        private readonly Regex colorRe = new Regex("^[0-9a-fA-F]{6}(?:[0-9a-fA-F]{2})?$");
        private string ResolveColor(string color)
        {
            if (color == null)
            {
                return "FF000000";
            }

            if (colorRe.IsMatch(color))
            {
                if (color.Length == 6)
                {
                    return "FF" + color.ToUpperInvariant();
                }

                return color.ToUpperInvariant();
            }

            var c = System.Drawing.Color.FromName(color);
            var cs = String.Format("{0:X8}", c.ToArgb());

            if (cs == "00000000" && color.ToUpperInvariant() != "TRANSPARENT")
            {
                return "FF000000";
            }

            return cs;
        }

        private int FindFill(IStyle style)
        {
            if (style.FillColor == null)
            {
                return 0;
            }

            var fcolor = ResolveColor(style.FillColor);

            int i = -1;
            foreach (var child in ss.Fills)
            {
                var fill = child as Fill;
                i++;

                if (i < 2)
                {
                    // skip over None and Grey125
                    continue;
                }

                if (fcolor == fill.PatternFill.ForegroundColor.Rgb.Value)
                {
                    return i;
                }
            }

            var newfill = new Fill()
            {
                PatternFill = new PatternFill()
                {
                    PatternType = PatternValues.Solid,
                    ForegroundColor = new ForegroundColor()
                    {
                        Rgb = fcolor
                    }
                }
            };

            ss.Fills.AppendChild(newfill);
            ss.Fills.Count++;
            return i + 1;
        }

        private int FindBorder(IStyle style)
        {
            int i = -1;
            foreach (var child in ss.Borders)
            {
                i++;
                var border = child as Border;

                if ((int)style.LeftBorderStyle != (int)border.LeftBorder.Style.Value
                    || (style.LeftBorderStyle != BorderStyle.None && style.LeftBorderColor != border.LeftBorder.Color?.Rgb))
                {
                    continue;
                }

                if ((int)style.RightBorderStyle != (int)border.RightBorder.Style.Value
                    || (style.RightBorderStyle != BorderStyle.None && style.RightBorderColor != border.RightBorder.Color?.Rgb))
                {
                    continue;
                }

                if ((int)style.TopBorderStyle != (int)border.TopBorder.Style.Value
                    || (style.TopBorderStyle != BorderStyle.None && style.TopBorderColor != border.TopBorder.Color?.Rgb))
                {
                    continue;
                }

                if ((int)style.BottomBorderStyle != (int)border.BottomBorder.Style.Value
                    || (style.BottomBorderStyle != BorderStyle.None && style.BottomBorderColor != border.BottomBorder.Color?.Rgb))
                {
                    continue;
                }

                return i;
            }

            var nb = new Border()
            {
                LeftBorder = new LeftBorder() { Style = (BorderStyleValues)style.LeftBorderStyle },
                RightBorder = new RightBorder() { Style = (BorderStyleValues)style.RightBorderStyle },
                TopBorder = new TopBorder() { Style = (BorderStyleValues)style.TopBorderStyle },
                BottomBorder = new BottomBorder() { Style = (BorderStyleValues)style.BottomBorderStyle }
            };

            var leftColor = ResolveColor(style.LeftBorderColor);
            var rightColor = ResolveColor(style.RightBorderColor);
            var topColor = ResolveColor(style.TopBorderColor);
            var bottomColor = ResolveColor(style.BottomBorderColor);

            if (leftColor != "FF000000")
            {
                nb.LeftBorder.Color = new Color() { Rgb = leftColor };
            }

            if (rightColor != "FF000000")
            {
                nb.RightBorder.Color = new Color() { Rgb = rightColor };
            }

            if (topColor != "FF000000")
            {
                nb.TopBorder.Color = new Color() { Rgb = topColor };
            }

            if (bottomColor != "FF000000")
            {
                nb.BottomBorder.Color = new Color() { Rgb = bottomColor };
            }

            ss.Borders.AppendChild(nb);
            ss.Borders.Count++;
            return i + 1;
        }

        private static bool IsSameAlignmentOrDefault(Alignment ours, Alignment theirs, bool apply)
        {
            if (!apply || theirs == null)
            {
                return ours.Horizontal == HorizontalAlignmentValues.General
                    && ours.Vertical == VerticalAlignmentValues.Bottom
                    && ours.ShrinkToFit == false
                    && ours.WrapText == false
                    && ours.TextRotation == 0;
            }

            return ours.Horizontal == theirs.Horizontal
                && ours.Vertical == theirs.Vertical
                && ours.ShrinkToFit == theirs.ShrinkToFit
                && ours.WrapText == theirs.WrapText
                && ours.TextRotation == theirs.TextRotation;
        }

        internal uint FindStyle(IStyle style)
        {
            var font = FindFont(style);
            var nfm = FindNumberFormat(style);
            var fill = FindFill(style);
            var border = FindBorder(style);
            var aln = new Alignment()
            {
                Horizontal = (HorizontalAlignmentValues)style.HorizontalAlignment,
                Vertical = (VerticalAlignmentValues)style.VerticalAlignment,
                ShrinkToFit = style.ShrinkToFit,
                WrapText = style.WrapText,
                TextRotation = style.Rotation
            };

            uint i = 0;
            foreach (var child in ss.CellFormats)
            {
                var fmt = child as CellFormat;

                if (font == fmt.FontId && nfm == fmt.NumberFormatId && fill == fmt.FillId && border == fmt.BorderId
                    && (font == 0 || fmt.ApplyFont) && (nfm == 0 || fmt.ApplyNumberFormat)
                    && (fill == 0 || fmt.ApplyFill) && (border == 0 || fmt.ApplyBorder)
                    && IsSameAlignmentOrDefault(aln, fmt.Alignment, fmt.ApplyAlignment))
                {
                    return i;
                }

                i++;
            }

            var nfmt = new CellFormat()
            {
                FontId = (uint)font,
                NumberFormatId = nfm,
                FillId = (uint)fill,
                BorderId = (uint)border,
                FormatId = 0,
                Alignment = aln,
                ApplyFont = font != 0,
                ApplyNumberFormat = nfm != 0,
                ApplyFill = fill != 0,
                ApplyBorder = border != 0,
                ApplyAlignment = !IsSameAlignmentOrDefault(aln, null, false)
            };

            ss.CellFormats.AppendChild(nfmt);
            ss.CellFormats.Count++;
            return i;
        }

        internal int FindSharedString(string str)
        {
            int i = 0;
            st.Count++;

            foreach (SharedStringItem item in st)
            {
                if (item.InnerText == str)
                {
                    return i;
                }

                i++;
            }

            st.AppendChild(new SharedStringItem(new Text(str)));
            st.UniqueCount++;

            return i;
        }
    }
}

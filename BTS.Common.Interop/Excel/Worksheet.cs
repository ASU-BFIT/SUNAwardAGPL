using DocumentFormat.OpenXml.Spreadsheet;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

using ExcelWorksheet = DocumentFormat.OpenXml.Spreadsheet.Worksheet;

namespace BTS.Common.Excel
{
    /// <summary>
    /// Excel worksheet
    /// </summary>
    [ClassInterface(ClassInterfaceType.None),
     Guid("D46D8C8A-655F-46B7-9B43-406EDF46B420")]
    public sealed class Worksheet : IWorksheet
    {
        private readonly Workbook wb;
        internal ExcelWorksheet ws;
        private readonly SheetData sd;
        private Row row;
        private uint colNum;
        private uint rowNum;
        private IStyle defaultStyle;
        internal Dictionary<uint, double> colWidths;
        private readonly char[] b26 = { 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M',
                                        'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z' };

        /// <summary>
        /// Name of the worksheet, or null if it should be automatically generated upon save
        /// </summary>
        public string Name { get; private set; }

        internal Worksheet(Workbook workbook, string sheetName)
        {
            wb = workbook;
            ws = new ExcelWorksheet();
            var cols = new Columns();
            sd = new SheetData();
            row = new Row() { RowIndex = 1 };
            var sheetpr = new SheetProperties();

            colNum = 0;
            rowNum = 1;
            defaultStyle = null;
            colWidths = new Dictionary<uint, double>();
            sheetpr.AppendChild(new PageSetupProperties()
            {
                FitToPage = true
            });

            ws.AppendChild(sheetpr);
            ws.AppendChild(cols);
            ws.AppendChild(sd);
            ws.AppendChild(new PageSetup()
            {
                Orientation = OrientationValues.Landscape,
                FitToWidth = 1,
                FitToHeight = 0
            });
            Name = sheetName;
        }

        /// <summary>
        /// Write data to the current cell pointer, then move the cell pointer to the right
        /// </summary>
        /// <param name="data">Data to write</param>
        /// <param name="cellStyle">Style for the cell, instance of IStyle</param>
        /// <param name="cellSpan">How many columns this cell should span</param>
        public void AddCell(object data, object cellStyle = null, uint cellSpan = 1)
        {
            if (cellSpan <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(cellSpan), "cellSpan must be 1 or greater");
            }

            var cs = (IStyle)cellStyle;
            if (cs == null)
            {
                cs = defaultStyle;
            }

            var cell = new Cell()
            {
                CellReference = String.Format("{0}{1}", ToBase26(colNum), rowNum)
            };

            // check if we're applying any customized styles to this cell
            uint? styleIdx = null;
            if (cs != null)
            {
                styleIdx = wb.FindStyle(cs);
                cell.StyleIndex = styleIdx.Value;
            }

            if (data != null && data != DBNull.Value)
            {
                var t = data.GetType();
                t = Nullable.GetUnderlyingType(t) ?? t;
                var code = Type.GetTypeCode(t);

                switch (code)
                {
                    case TypeCode.String:
                        // super hacky, but some data has \u001a (aka the substitute character), which causes export to blow up
                        // I cannot see a reason why we'd need to preserve this character, so strip it instead
                        data = data.ToString()
                            .Replace("\x00", "")
                            .Replace("\u001a", "")
                            .Replace("\u001b", "")
                            .Trim();
                        cell.CellValue = new CellValue(wb.FindSharedString(data.ToString()).ToString());
                        cell.DataType = CellValues.SharedString;
                        break;
                    case TypeCode.DateTime:
                        cell.CellValue = new CellValue(((DateTime)data).ToOADate().ToString());
                        if (cs == null || styleIdx == null)
                        {
                            cell.StyleIndex = 1;
                            styleIdx = 1;
                        }
                        else if (cs.NumberFormat == null)
                        {
                            var csc = cs.Clone();
                            csc.NumberFormat = "mm-dd-yy";
                            styleIdx = wb.FindStyle(csc);
                            cell.StyleIndex = styleIdx.Value;
                        }
                        break;
                    case TypeCode.Boolean:
                        cell.CellValue = new CellValue(((bool)data) ? "1" : "0");
                        cell.DataType = CellValues.Boolean;
                        break;
                    default:
                        cell.CellValue = new CellValue(data.ToString());
                        break;
                }

                string dataStr = wb.FormatData(data, styleIdx ?? 0);

                // per spec, width = Truncate([{Number of Characters} * {Maximum Digit Width} + {5 pixel padding}]/{Maximum Digit Width}*256)/ 256
                // or given width of cell, Truncate(({pixels}-5)/{Maximum Digit Width} * 100+0.5)/100
                // therefore, we calculate the pixel width of dataStr and run it through the above formula to get the character width (Number of Characters),
                // which we then pass through the first formula to get the column width. Sounds fun, right? (no)

                double maxWidth = WidthCache.GetMaxDigitWidth("Calibri", 11, false);
                double ourWidth = wb.GetStringWidthByStyle(dataStr, styleIdx ?? 0);
                // no -5 since ourWidth doesn't include the 5 pixel padding
                double numChars = Math.Floor(ourWidth / maxWidth * 100 + 0.5) / 100;
                // add in 9 pixels of padding since that's what Excel does (sometimes it does 10 though? unless it renders the text differently)
                double width = Math.Floor((numChars * maxWidth + 9) / maxWidth * 256) / 256;

                for (uint i = 0; i < cellSpan; ++i)
                {
                    if (colWidths.ContainsKey(colNum + i))
                    {
                        colWidths[colNum + i] = Math.Max(colWidths[colNum + i], width / cellSpan);
                    }
                    else
                    {
                        colWidths[colNum + i] = width / cellSpan;
                    }
                }
            }

            row.AppendChild(cell);
            colNum++;

            if (cellSpan > 1)
            {
                // we're merging some cells together
                if (!ws.Elements<MergeCells>().Any())
                {
                    // MergeCells needs to go before PageSetup but after SheetData
                    ws.InsertAfter(new MergeCells() { Count = 0 }, ws.Elements<SheetData>().First());
                }

                var mc = ws.Elements<MergeCells>().First();
                mc.AppendChild(new MergeCell()
                {
                    // right now we only handle merges that span a single row, but the following
                    // is flexible enough to handle multirow merges
                    // is -2 instead of -1 because we already incremented colNum above but need to calculate from the initial cell
                    Reference = String.Format("{0}:{1}{2}", cell.CellReference, ToBase26(colNum + cellSpan - 2), rowNum)
                });

                mc.Count++;

                // can't skip these cells since some style information is required on them
                // (even though the spec indicates otherwise, Excel pulls border info from individual cells)
                for (int j = 0; j < cellSpan - 1; j++)
                {
                    cell = new Cell()
                    {
                        CellReference = String.Format("{0}{1}", ToBase26(colNum), rowNum)
                    };

                    if (styleIdx.HasValue)
                    {
                        cell.StyleIndex = styleIdx.Value;
                    }

                    row.AppendChild(cell);
                    colNum++;
                }
            }

        }

        /// <summary>
        /// Move the cell pointer to the right by a number of cells, leaving them blank
        /// </summary>
        /// <param name="count">Number of cells to skip over</param>
        public void SkipCells(uint count)
        {
            colNum += count;
        }

        /// <summary>
        /// Move the cell pointer to the first cell on the next row
        /// </summary>
        public void NextRow()
        {
            if (colNum > 0)
            {
                sd.AppendChild(row);
            }

            rowNum++;
            colNum = 0;
            row = new Row() { RowIndex = rowNum };
        }

        /// <summary>
        /// Move the cell pointer down by a number of rows, leaving them blank
        /// </summary>
        /// <param name="count">Number of rows to skip over</param>
        public void SkipRows(uint count)
        {
            rowNum += count;
            NextRow();
        }

        /// <summary>
        /// Sets the default styling that is applied to cells written to via AddCell
        /// if no style is specified in the call
        /// </summary>
        /// <param name="style">Default style, instance of IStyle</param>
        public void SetDefaultStyle(object style)
        {
            var cs = (IStyle)style;

            if (cs == null)
            {
                defaultStyle = null;
            }
            else
            {
                defaultStyle = cs.Clone();
            }
        }

        private string ToBase26(uint num)
        {
            string str = "";

            do
            {
                str = b26[num % 26] + str;
                num /= 26;
            } while (num > 0);

            return str;
        }
    }
}

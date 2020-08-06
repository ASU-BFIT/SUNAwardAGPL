using System;
using System.Runtime.InteropServices;

namespace BTS.Common.Excel
{
    /// <summary>
    /// Excel worksheet. This interface is meant for COM interoperability.
    /// Use the Worksheet class directly if not using COM.
    /// </summary>
    [Guid("7C35D891-1C7C-4E64-B0D3-5F86A55A1884")]
    public interface IWorksheet
    {
        /// <summary>
        /// Write data to the current cell pointer, then move the cell pointer to the right
        /// </summary>
        /// <param name="data">Data to write</param>
        /// <param name="cellStyle">Style for the cell, instance of IStyle</param>
        /// <param name="cellSpan">How many columns this cell should span</param>
        [DispId(1)]
        void AddCell(object data, object cellStyle = null, uint cellSpan = 1);

        /// <summary>
        /// Move the cell pointer to the right by a number of cells, leaving them blank
        /// </summary>
        /// <param name="count">Number of cells to skip over</param>
        [DispId(2)]
        void SkipCells(uint count);

        /// <summary>
        /// Move the cell pointer to the first cell on the next row
        /// </summary>
        [DispId(3)]
        void NextRow();

        /// <summary>
        /// Move the cell pointer down by a number of rows, leaving them blank
        /// </summary>
        /// <param name="count">Number of rows to skip over</param>
        [DispId(4)]
        void SkipRows(uint count);

        /// <summary>
        /// Sets the default styling that is applied to cells written to via AddCell
        /// if no style is specified in the call
        /// </summary>
        /// <param name="style">Default style, instance of IStyle</param>
        [DispId(5)]
        void SetDefaultStyle(object style);
    }
}

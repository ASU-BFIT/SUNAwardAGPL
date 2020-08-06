using System;
using System.Collections;
using System.Runtime.InteropServices;

namespace BTS.Common.Excel
{
    /// <summary>
    /// Excel Workbook. This interface is meant for COM interoperability.
    /// Use the Workbook class directly if not using COM.
    /// </summary>
    [Guid("199CAFF7-3F43-4330-AA25-B79DF97CD3FC")]
    public interface IWorkbook
    {
        /// <summary>
        /// Worksheets within the workbook
        /// </summary>
        [DispId(1)]
        IEnumerable Worksheets { get; }

        /// <summary>
        /// Add a sheet to the workbook
        /// </summary>
        /// <param name="sheetName">Name of the sheet, will be automatically generated if not specified</param>
        /// <returns>The newly-created sheet</returns>
        [DispId(100)]
        IWorksheet AddSheet(string sheetName = null);

        /// <summary>
        /// Saves a sheet to the given file path
        /// </summary>
        /// <param name="path">File path to save, will be overwritten</param>
        [DispId(200)]
        void Save(string path);

        /// <summary>
        /// Gets the workbook in xlsx format as a byte array for serialization/streaming
        /// </summary>
        /// <returns></returns>
        [DispId(201)]
        byte[] GetWorkbook();
    }
}

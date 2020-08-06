using System;
using System.IO;

namespace BTS.Common.Excel
{
    /// <summary>
    /// Extension methods to export data to excel
    /// </summary>
    public static class ExcelExtensions
    {
        /// <summary>
        /// Export data to an xslx file
        /// </summary>
        /// <typeparam name="T">Type of data being exported</typeparam>
        /// <param name="exp">Exporter containing data to export as well as formatting information</param>
        /// <param name="path">Path to disk where xlsx file will be written</param>
        public static void ToExcel<T>(this Exporter<T> exp, string path)
        {
            using (var stream = File.Open(path, FileMode.Create, FileAccess.ReadWrite, FileShare.Read))
            {
                ToExcel(exp, stream);
            }
        }

        /// <summary>
        /// Export xlsx to a stream
        /// </summary>
        /// <typeparam name="T">Type of data being exported</typeparam>
        /// <param name="exp">Exporter containing data to export as well as formatting information</param>
        /// <param name="stream">Binary stream to write xlsx to</param>
        public static void ToExcel<T>(this Exporter<T> exp, Stream stream)
        {
            if (exp == null)
            {
                throw new ArgumentNullException(nameof(exp));
            }

            var wb = new Workbook();
            var ws = wb.AddSheet();

            foreach (var p in exp.PropInfo)
            {
                ws.AddCell(p.GetDisplayName());
            }

            ws.NextRow();

            foreach (var obj in exp.Data)
            {
                foreach (var p in exp.PropInfo)
                {
                    ws.AddCell(p.GetValue(obj));
                }

                ws.NextRow();
            }

            wb.Save(stream);
        }
    }
}

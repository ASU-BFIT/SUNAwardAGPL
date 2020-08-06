using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;

namespace BTS.Common
{
    /// <summary>
    /// Handles exporting sequences of objects to various formats such as .csv and .xlsx
    /// To export to Excel (xlsx), add a reference to the BTS.Common.Interop NuGet package and import the BTS.Common.Excel namespace
    /// </summary>
    /// <typeparam name="T">Data class to export</typeparam>
    public class Exporter<T>
    {
        /// <summary>
        /// Some export types support embedded metadata. For example, in Excel and PDF exports this metadata will generally
        /// appear above the normal data in a header block. See each individual export method for how it handles metadata.
        /// CSV export does not embed any metadata. This field may be modified after being set, do not rely on it being unchanged
        /// between construction of Exporter&lt;T&gt; and after calling export methods.
        /// </summary>
        [SuppressMessage("Usage", "CA2227:Collection properties should be read only",
            Justification = "Sub-exporters need to set this, and exposing a setter is the cleanest way to do so.")]
        public IDictionary<string, string> Metadata { get; set; }

        /// <summary>
        /// Data to export
        /// </summary>
        public IEnumerable<T> Data { get; private set; }

        /// <summary>
        /// Function to sort the data before export
        /// </summary>
        public Func<string, string, int> SortPredicate { get; private set; }
        /// <summary>
        /// Function to filter the data before export
        /// </summary>
        public Func<string, bool> FilterPredicate { get; private set; }

        /// <summary>
        /// For custom calls that are not pulling public properties from T. The dynamic object should have a string property
        /// Name with the property name, and an instance method object GetValue(object) which, given an instance of T, should return the
        /// data value in that T instance named by Name. This list is further filtered and sorted by FilterPredicate and SortPredicate, respectively.
        /// </summary>
        public List<dynamic> PropInfo { get; private set; }

        /// <summary>
        /// Creates a new Exporter for the given data
        /// </summary>
        /// <param name="data">Data to write, this is expected to be an enumeration of objects whose public properties will be written</param>
        /// <param name="sortPredicate">The sort order of columns in the exported result, return values should allow for a strict total ordering of column names</param>
        /// <param name="filterPredicate">Whether or not a particular public property should be included in the export</param>
        /// <param name="propInfo">Property info, usually not required unless doing funky things</param>
        public Exporter(IEnumerable<T> data, Func<string, string, int> sortPredicate = null, Func<string, bool> filterPredicate = null, IEnumerable<dynamic> propInfo = null)
        {
            Data = data;
            SortPredicate = sortPredicate ?? String.Compare;
            FilterPredicate = filterPredicate ?? (x => true);
            PropInfo = (propInfo ?? typeof(T).GetProperties())
                .Where(o => FilterPredicate((string)o.Name))
                .OrderBy(o => (string)o.Name, Comparer<string>.Create(new Comparison<string>(SortPredicate)))
                .ToList();
        }

        /// <summary>
        /// Creates a new Exporter for the given data
        /// </summary>
        /// <param name="data">Data to write, this is expected to be an enumeration of objects whose public properties will be written</param>
        /// <param name="fields">The fields in data we should write, all of these must be public properties. Ensure there are no duplicates</param>
        /// <param name="propInfo">Property info, usually not required unless doing funky things</param>
        public Exporter(IEnumerable<T> data, IEnumerable<string> fields, IEnumerable<dynamic> propInfo = null)
        {
            if (fields == null)
            {
                throw new ArgumentNullException(nameof(fields));
            }

            var fdict = new Dictionary<string, int>();
            var fset = new HashSet<string>();
            int i = 0;

            foreach (var f in fields)
            {
                fdict[f] = i;
                fset.Add(f);
                i++;
            }

            Data = data ?? throw new ArgumentNullException(nameof(data));
            SortPredicate = (a, b) => fdict[a].CompareTo(fdict[b]);
            FilterPredicate = x => fset.Contains(x);
            PropInfo = (propInfo ?? typeof(T).GetProperties())
                .Where(o => FilterPredicate((string)o.Name))
                .OrderBy(o => (string)o.Name, Comparer<string>.Create(new Comparison<string>(SortPredicate)))
                .ToList();
        }

        /// <summary>
        /// Exports a list of data into a .csv file
        /// </summary>
        /// <param name="path">Filename to write data to</param>
        /// <param name="includeHeaders">If true, the first row will consist of a list of column labels derived either from the DisplayName attribute or the property name</param>
        /// <param name="quoteMode">Determines when values in the csv will be quoted</param>
        public void ToCsv(string path, bool includeHeaders = false, CsvQuoteMode quoteMode = CsvQuoteMode.AsNeeded)
        {
            using var writer = new StreamWriter(path);
            ToCsv(writer, includeHeaders, quoteMode);
        }

        /// <summary>
        /// Exports a list of data into a stream
        /// </summary>
        /// <param name="stream">Stream to write data to</param>
        /// <param name="includeHeaders">If true, the first row will consist of a list of column labels derived either from the DisplayName attribute or the property name</param>
        /// <param name="quoteMode">Determines when values in the csv will be quoted</param>
        public void ToCsv(Stream stream, bool includeHeaders = false, CsvQuoteMode quoteMode = CsvQuoteMode.AsNeeded)
        {
            using var writer = new StreamWriter(stream, new UTF8Encoding(false), 512, true);
            ToCsv(writer, includeHeaders, quoteMode);
        }

        /// <summary>
        /// Exports a list of data into csv format to the specified TextWriter
        /// </summary>
        /// <param name="writer">Place to write data to</param>
        /// <param name="includeHeaders">If true, the first row will consist of a list of column labels derived either from the DisplayName attribute or the property name</param>
        /// <param name="quoteMode">Determines when values in the csv will be quoted</param>
        public void ToCsv(TextWriter writer, bool includeHeaders = false, CsvQuoteMode quoteMode = CsvQuoteMode.AsNeeded)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (includeHeaders)
            {
                writer.WriteLine(String.Join(",", PropInfo.Select(p =>
                {
                    string name = p.Name;

                    if (!String.IsNullOrEmpty(name))
                    {
                        if (quoteMode == CsvQuoteMode.Always
                            || (quoteMode == CsvQuoteMode.AsNeeded
                                && (name.Contains('\r') || name.Contains('\n') || name.Contains('"') || name.Contains(','))))
                        {
                            name = '"' + name.Replace("\"", "\"\"") + '"';
                        }
                    }

                    return name ?? String.Empty;
                })));
            }

            foreach (T obj in Data)
            {
                writer.WriteLine(String.Join(",", PropInfo.Select(p =>
                {
                    object val = p.GetValue(obj);
                    string sVal = null;

                    if (val != null)
                    {
                        sVal = val.ToString();

                        if (quoteMode == CsvQuoteMode.Always
                            || (quoteMode == CsvQuoteMode.AsNeeded
                                && (sVal.Contains('\r') || sVal.Contains('\n') || sVal.Contains('"') || sVal.Contains(','))))
                        {
                            sVal = '"' + sVal.Replace("\"", "\"\"") + '"';
                        }
                    }

                    return sVal ?? String.Empty;
                })));
            }
        }
    }
}

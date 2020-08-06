namespace BTS.Common
{
    /// <summary>
    /// When values should be quoted when exporting to CSV
    /// </summary>
    public enum CsvQuoteMode
    {
        /// <summary>
        /// Values are always quoted
        /// </summary>
        Always,
        /// <summary>
        /// Values are never quoted, this is an error if a value contains the delimiter
        /// </summary>
        Never,
        /// <summary>
        /// Values are only quoted if they contain the delimiter or other special characters
        /// </summary>
        AsNeeded
    }
}
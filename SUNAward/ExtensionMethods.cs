using iText.Forms.Fields;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Diagnostics.CodeAnalysis;

namespace SUNAward
{
    public static class ExtensionMethods
    {
        /// <summary>
        /// Get the string state corresponding to the specified checked value for the given checkbox
        /// </summary>
        /// <param name="field"></param>
        /// <param name="isChecked"></param>
        /// <returns></returns>
        [SuppressMessage("Style", "IDE0060:Remove unused parameter",
            Justification = "Should be an extension method for readability and future expansion in case a field uses something other than Yes/Off")]
        public static string GetCheckBoxState(this PdfFormField field, bool isChecked)
        {
            // By default, Acrobat uses "Yes" for checked and "Off" for unchecked
            return isChecked ? "Yes" : "Off";
        }

        /// <summary>
        /// Joins the sequence using the given separator, returning an empty string if the sequence is empty
        /// </summary>
        /// <param name="values"></param>
        /// <param name="separator"></param>
        /// <returns></returns>
        public static string StringJoin(this IEnumerable<string> values, string separator)
        {
            return String.Join(separator, values);
        }

        /// <summary>
        /// Joins the sequence using the given separator, returning null if the sequence is empty
        /// </summary>
        /// <param name="values"></param>
        /// <param name="separator"></param>
        /// <returns></returns>
        public static string StringJoinOrNull(this IEnumerable<string> values, string separator)
        {
            if (values.Count() == 0)
            {
                return null;
            }

            return String.Join(separator, values);
        }
    }
}
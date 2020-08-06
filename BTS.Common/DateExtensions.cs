using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using System.ComponentModel.DataAnnotations;

namespace BTS.Common
{
    /// <summary>
    /// DateTime extensions
    /// </summary>
    public static class DateExtensions
    {
        private const string DateFormatString = "MMM d, yyyy";
        private const string TimeFormatStringWithMins = "h:mm tt";
        private const string TimeFormatStringNoMins = "h tt";
        private static readonly DateTimeFormatInfo DateFormatInfo = new DateTimeFormatInfo()
        {
            AbbreviatedMonthNames = new string[]
            {
                "Jan.",
                "Feb.",
                "March",
                "April",
                "May",
                "June",
                "July",
                "Aug.",
                "Sept.",
                "Oct.",
                "Nov.",
                "Dec.",
                String.Empty
            },
            AMDesignator = "a.m.",
            PMDesignator = "p.m."
        };

        /// <summary>
        /// Converts this DateTime into a string with date and time formatted according to ASU branding standards
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static string ToAsuDateTimeString(this DateTime date)
        {
            return String.Format("{0} {1}", date.ToAsuDateString(), date.ToAsuTimeString());
        }

        /// <summary>
        /// Converts this DateTime into a string with date formatted according to ASU branding standards
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static string ToAsuDateString(this DateTime date)
        {
            return date.ToString(DateFormatString, DateFormatInfo);
        }

        /// <summary>
        /// Converts this DateTime into a string with time formatted according to ASU branding standards
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static string ToAsuTimeString(this DateTime date)
        {
            return date.TimeOfDay.ToAsuTimeString();
        }

        /// <summary>
        /// Converts this TimeSpan into a string with time formatted according to ASU branding standards
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public static string ToAsuTimeString(this TimeSpan time)
        {
            if (time.Minutes == 0)
            {
                return time.ToString(TimeFormatStringNoMins, DateFormatInfo);
            }

            return time.ToString(TimeFormatStringWithMins, DateFormatInfo);
        }
    }
}

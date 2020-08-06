using System.Reflection;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System;

namespace BTS.Common
{
    /// <summary>
    /// Extension methods which make use of Reflection
    /// </summary>
    public static class ReflectionExtensions
    {
        /// <summary>
        /// Gets the display name from DisplayAttribute or DisplayNameAttribute
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        public static string GetDisplayName(this MemberInfo info)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            var attr = info.GetCustomAttributes(typeof(DisplayAttribute), false);
            string val = null;

            if (attr != null && attr.Length > 0)
            {
                val = ((DisplayAttribute)attr[0]).GetName();
            }

            if (val != null)
            {
                return val;
            }

            attr = info.GetCustomAttributes(typeof(DisplayNameAttribute), false);

            if (attr != null && attr.Length > 0)
            {
                val = ((DisplayNameAttribute)attr[0]).DisplayName;
            }

            if (val != null)
            {
                return val;
            }

            return info.Name;
        }

        /// <summary>
        /// Gets the short name for display in grid columns
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        public static string GetShortName(this MemberInfo info)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            var attr = info.GetCustomAttributes(typeof(DisplayAttribute), false);
            string val = null;

            if (attr != null && attr.Length > 0)
            {
                val = ((DisplayAttribute)attr[0]).GetShortName();
            }

            if (val != null)
            {
                return val;
            }

            return GetDisplayName(info);
        }

        /// <summary>
        /// Gets the description from DisplayAttribute or DescriptionAttribute
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        public static string GetDescription(this MemberInfo info)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            var attr = info.GetCustomAttributes(typeof(DisplayAttribute), false);
            string val = null;

            if (attr != null && attr.Length > 0)
            {
                val = ((DisplayAttribute)attr[0]).GetDescription();
            }

            if (val != null)
            {
                return val;
            }

            attr = info.GetCustomAttributes(typeof(DescriptionAttribute), false);

            if (attr != null && attr.Length > 0)
            {
                val = ((DescriptionAttribute)attr[0]).Description;
            }

            if (val != null)
            {
                return val;
            }

            return info.Name;
        }

        /// <summary>
        /// Gets a string corresponding to the display name of the enum value
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public static string ToDisplayString(this Enum e)
        {
            if (e == null)
            {
                throw new ArgumentNullException(nameof(e));
            }

            var type = e.GetType();
            var member = type.GetMember(e.ToString())[0];

            return member.GetDisplayName();
        }
    }
}

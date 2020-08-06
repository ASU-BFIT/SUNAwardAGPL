using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace BTS.Common
{
    /// <summary>
    /// Extension methods for types
    /// </summary>
    public static class TypeExtensions
    {
        private static readonly HashSet<Type> _numericTypes = new HashSet<Type>()
        {
            // 8 bit integers
            typeof(sbyte), typeof(byte),
            // 16 bit integers
            typeof(short), typeof(ushort), typeof(char),
            // 32 bit integers
            typeof(int), typeof(uint),
            // 64 bit integers
            typeof(long), typeof(ulong),
            // floating point
            typeof(float), typeof(double),
            // fixed point
            typeof(decimal)
        };

        /// <summary>
        /// Check if type is numeric
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsNumericType(this Type type)
        {
            return _numericTypes.Contains(type);
        }

        /// <summary>
        /// Check if type is signed integer
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsSignedInteger(this Type type)
        {
            return type == typeof(sbyte) || type == typeof(short) || type == typeof(int) || type == typeof(long);
        }

        /// <summary>
        /// Check if type is unsigned integer
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsUnsignedInteger(this Type type)
        {
            return type == typeof(byte) || type == typeof(ushort) || type == typeof(char) || type == typeof(uint) || type == typeof(ulong);
        }

        /// <summary>
        /// Check if type is floating point
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsFloatingPoint(this Type type)
        {
            return type == typeof(float) || type == typeof(double);
        }

        /// <summary>
        /// Check if type is fixed point
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsFixedPoint(this Type type)
        {
            return type == typeof(decimal);
        }

        /// <summary>
        /// Check if the object is numeric and if so, if it is equal to 0.
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        public static bool IsZero(this object number)
        {
            if (number == null)
            {
                return false;
            }

            var type = number.GetType();
            
            if (!type.IsNumericType())
            {
                return false;
            }

            var isZero = Expression.Lambda<Func<bool>>(Expression.Equal(Expression.Constant(number), Expression.Constant(0)));
            return isZero.Compile()();
        }

        /// <summary>
        /// Returns the underlying type for a nullable
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static Type DiscardNullable(this Type type)
        {
            return Nullable.GetUnderlyingType(type) ?? type;
        }

        /// <summary>
        /// Checks if type is nullable
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsNullable(this Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }
    }
}

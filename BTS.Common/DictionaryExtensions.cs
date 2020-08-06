using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BTS.Common
{
    /// <summary>
    /// Extension methods for Dictionary
    /// </summary>
    public static class DictionaryExtensions
    {
        /// <summary>
        /// Converts a KeyValuePair into its key and value (as out params)
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="keyValuePair"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> keyValuePair, out TKey key, out TValue value)
        {
            key = keyValuePair.Key;
            value = keyValuePair.Value;
        }
    }
}

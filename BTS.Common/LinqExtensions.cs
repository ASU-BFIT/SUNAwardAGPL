using System;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace BTS.Common
{
    /// <summary>
    /// Extension methods for LINQ
    /// </summary>
    public static class LinqExtensions
    {
        /// <summary>
        /// Retrieves PropertyInfo for the property specified in the lambda expression. The property must be publicly accessible.
        /// Additionally, the property chain to arrive at the specified property must all be public properties (or indexes thereon).
        /// </summary>
        /// <param name="expr"></param>
        /// <returns>Returns null if the lambda expression is not a single expression returning a property</returns>
        public static PropertyInfo GetPropertyInfo(this LambdaExpression expr)
        {
            if (expr == null)
            {
                throw new ArgumentNullException(nameof(expr));
            }

            return GetPropertyInfoReentrant(expr.Body);
        }

        /// <summary>
        /// Gets the chain for this expression tree
        /// </summary>
        /// <param name="expr"></param>
        /// <returns></returns>
        public static List<Tuple<ExpressionType, object>> GetPropertyChain(this LambdaExpression expr)
        {
            if (expr == null)
            {
                throw new ArgumentNullException(nameof(expr));
            }

            var chain = new List<Tuple<ExpressionType, object>>();
            var pinfo = GetPropertyInfoReentrant(expr.Body, chain);
            if (pinfo == null)
            {
                return null;
            }

            // put list in order of operations to reconstruct the expression tree (as it is built up leaf to root)
            chain.Reverse();
            return chain;
        }

        private static PropertyInfo GetPropertyInfoReentrant(Expression expr, List<Tuple<ExpressionType, object>> chain = null)
        {
            PropertyInfo pinfo;

            switch (expr.NodeType)
            {
                case ExpressionType.ArrayIndex:
                    // single array index
                    if (chain != null)
                    {
                        // save off a constant expression or null if the index is non-constant (if non-constant, we then fill in 0 when sorting)
                        chain.Add(new Tuple<ExpressionType, object>(ExpressionType.ArrayIndex, ((BinaryExpression)expr).Right));
                    }
                    return GetPropertyInfoReentrant(((BinaryExpression)expr).Left, chain);
                case ExpressionType.Call:
                    // list index, dictionary index, etc.; we don't support multidimensional arrays
                    // can also be a normal method call -- need to check for this!
                    var cexpr = (MethodCallExpression)expr;
                    var placeholder = new List<object>();

                    if (chain != null)
                    {
                        chain.Add(new Tuple<ExpressionType, object>(ExpressionType.Call, placeholder));
                    }
                    pinfo = GetPropertyInfoReentrant(cexpr.Object, chain);

                    if (pinfo == null
                        || !pinfo.PropertyType.GetDefaultMembers().OfType<PropertyInfo>().Any(m => m.GetIndexParameters().Length == 1 && m.GetMethod == cexpr.Method))
                    {
                        // not an indexer, or a multidimensional indexer
                        return null;
                    }

                    placeholder.Add(cexpr.Method);
                    placeholder.Add(cexpr.Arguments[0]);

                    return pinfo;
                case ExpressionType.MemberAccess:
                    pinfo = ((MemberExpression)expr).Member as PropertyInfo;
                    var pexpr = ((MemberExpression)expr).Expression;
                    
                    if (pinfo == null || !pinfo.CanRead || !pinfo.GetGetMethod(true).IsPublic)
                    {
                        // this isn't a public property
                        return null;
                    }

                    if (pexpr.NodeType != ExpressionType.Parameter && GetPropertyInfoReentrant(pexpr) == null)
                    {
                        // parent isn't a public property
                        return null;
                    }

                    if (chain != null)
                    {
                        chain.Add(new Tuple<ExpressionType, object>(ExpressionType.MemberAccess, pinfo));
                        if (pexpr.NodeType != ExpressionType.Parameter)
                        {
                            pinfo = GetPropertyInfoReentrant(pexpr, chain);
                        }
                    }

                    return pinfo;
            }

            return null;
        }

        /// <summary>
        /// Convert to a keyed collection
        /// </summary>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="collection"></param>
        /// <param name="elementSelector"></param>
        /// <returns></returns>
        public static KeyedCollection<TValue> ToKeyedCollection<TValue>(this NameObjectCollectionBase collection, Func<object, TValue> elementSelector = null)
        {
            // warning: ugly code
            // we can't actually get at any of the values of a NameObjectCollectionBase without using reflection
            var BaseGetKey = typeof(NameObjectCollectionBase).GetMethod("BaseGetKey", BindingFlags.Instance | BindingFlags.NonPublic, null, new Type[] { typeof(int) }, null);
            var BaseGet = typeof(NameObjectCollectionBase).GetMethod("BaseGet", BindingFlags.Instance | BindingFlags.NonPublic, null, new Type[] { typeof(int) }, null);
            var newColl = new KeyedCollection<TValue>();

            if (elementSelector == null)
            {
                // by default we just try to cast
                elementSelector = o => (TValue)o;
            }

            for (int i = 0; i < collection.Count; i++)
            {
                newColl.Add((string)BaseGetKey.Invoke(collection, new object[] { i }), elementSelector(BaseGet.Invoke(collection, new object[] { i })));
            }

            return newColl;
        }

        /// <summary>
        /// Convert to a keyed collection
        /// </summary>
        /// <typeparam name="TInput"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="iterable"></param>
        /// <param name="keySelector"></param>
        /// <param name="elementSelector"></param>
        /// <returns></returns>
        public static KeyedCollection<TValue> ToKeyedCollection<TInput, TValue>(this IEnumerable<TInput> iterable, Func<TInput, string> keySelector, Func<TInput, TValue> elementSelector)
        {
            if (iterable == null)
            {
                throw new ArgumentNullException(nameof(iterable));
            }

            var coll = new KeyedCollection<TValue>();

            foreach (var it in iterable)
            {
                coll.Add(keySelector(it), elementSelector(it));
            }

            return coll;
        }
    }
}

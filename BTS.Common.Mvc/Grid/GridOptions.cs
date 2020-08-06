using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Runtime.Serialization;
using System.Web;
using System.IO;
using System.Xml;

namespace BTS.Common.Mvc.Grid
{
    /// <summary>
    /// Generic wrapper for GridOptions&lt;T&gt; used for type erasure
    /// </summary>
    public interface IGridOptions
    {
        /// <summary>
        /// ID of the grid these options are for to track grid instances across multiple page loads.
        /// </summary>
        string GridId { get; set; }
    }

    /// <summary>
    /// Represents filters, paging, sorting, and other options that control how a grid is rendered.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class GridOptions<T> : IGridOptions
    {
        private string _id = null;
        /// <summary>
        /// ID of the grid these options are for to track grid instances across multiple page loads.
        /// </summary>
        public string GridId
        {
            get
            {
                if (_id == null)
                {
                    // allow tracking this specific grid instance across pageloads
                    _id = Guid.NewGuid().ToString();
                }

                return _id;
            }
            set { _id = value; }
        }

        /// <summary>
        /// Filter that restricts what results are shown
        /// </summary>
        public T Filter { get; set; }

        /// <summary>
        /// Current page being viewed
        /// </summary>
        public int PageIndex { get; set; }

        /// <summary>
        /// How many items are on each page
        /// </summary>
        public int? PageSize { get; set; }

        /// <summary>
        /// What columns are being sorted, in order (for example can sort by column A then B)
        /// </summary>
        public List<IGridSort> SortColumns { get; set; }

        /// <summary>
        /// Ordering in which columns are rendered
        /// </summary>
        public List<string> ColumnOrder { get; set; }

        /// <summary>
        /// The HttpContext firing this grid action. Set internally when methods
        /// such as RenderGrid or ExportGrid are called. Callbacks such as DataSourcePage can
        /// make use of this property to obtain information about the context which called it.
        /// </summary>
        public HttpContextBase HttpContext { get; internal set; }

        /// <summary>
        /// This is true when the grid is intially loaded and false if the grid was refreshed
        /// (due to paging, filter change, etc.).
        /// </summary>
        public bool IsInitialLoad { get; internal set; }

        /// <summary>
        /// Used by grid js and as such needs to be public, do not define/set yourself; if you wish to hardcode sorting please use SortColumns instead
        /// </summary>
        public List<Models.GridSortModel> SortColumnInfo { get; set; }
    }

    /// <summary>
    /// Generic interface for type erasure
    /// </summary>
    public interface IGridSort
    {
        /// <summary>
        /// Model type
        /// </summary>
        Type ModelType { get; }
        /// <summary>
        /// Value type
        /// </summary>
        Type ValueType { get; }
        /// <summary>
        /// Data type
        /// </summary>
        Type DataType { get; }
        /// <summary>
        /// Data value type
        /// </summary>
        Type DataValueType { get; }

        /// <summary>
        /// Column name
        /// </summary>
        string ColumnName { get; }
        /// <summary>
        /// Sort direction
        /// </summary>
        SortOrder SortOrder { get; }
        /// <summary>
        /// Retrieve expression used to sort
        /// </summary>
        /// <typeparam name="TData"></typeparam>
        /// <returns></returns>
        Expression GetSortExpression<TData>();
    }

    /// <summary>
    /// Represents a sorted grid
    /// </summary>
    /// <typeparam name="TModel"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public class GridSort<TModel, TValue> : IGridSort
    {
        private readonly List<Tuple<ExpressionType, object>> _propertyChain;
        /// <summary>
        /// Direction to sort
        /// </summary>
        public SortOrder SortOrder { get; private set; }

        /// <summary>
        /// Column name
        /// </summary>
        public string ColumnName { get; private set; }

        /// <summary>
        /// Model type
        /// </summary>
        public Type ModelType => typeof(TModel);

        /// <summary>
        /// Value type
        /// </summary>
        public Type ValueType => typeof(TValue);

        /// <summary>
        /// Data type
        /// </summary>
        public Type DataType { get; private set; }

        /// <summary>
        /// Data value type
        /// </summary>
        public Type DataValueType { get; private set; }

        /// <summary>
        /// Constructs a new GridSort
        /// </summary>
        /// <param name="columnName"></param>
        /// <param name="sortExpression"></param>
        /// <param name="sortOrder"></param>
        public GridSort(string columnName, Expression<Func<TModel, TValue>> sortExpression, SortOrder sortOrder)
        {
            ColumnName = columnName;
            SortOrder = sortOrder;
            _propertyChain = sortExpression.GetPropertyChain();

            if (_propertyChain == null)
            {
                throw new ArgumentException("Argument must be a lambda expression accessing a property member", "sortExpression");
            }
        }

        /// <summary>
        /// Retrieves expression used to sort grid
        /// </summary>
        /// <typeparam name="TData"></typeparam>
        /// <returns></returns>
        public Expression GetSortExpression<TData>()
        {
            var parameter = Expression.Parameter(typeof(TData), "o");
            Expression expr = parameter;

            foreach (var tup in _propertyChain)
            {
                switch (tup.Item1)
                {
                    case ExpressionType.ArrayIndex:
                        expr = Expression.ArrayIndex(expr, (ConstantExpression)tup.Item2);
                        break;
                    case ExpressionType.Call:
                        // find a method with the same name that is an indexing property, then pass our constant along to it
                        var mi = (MethodInfo)((List<object>)tup.Item2)[0];
                        var arg = (ConstantExpression)((List<object>)tup.Item2)[1];
                        MethodInfo mi2;
                        var pi = Expression.Lambda(expr, parameter).GetPropertyInfo();
                        if (pi == null)
                        {
                            throw new InvalidOperationException("Lambda expression does not match data type");
                        }
                        mi2 = pi.PropertyType.GetDefaultMembers().OfType<PropertyInfo>().Where(o => o.GetIndexParameters().Length == 1 && o.GetMethod.Name == mi.Name).FirstOrDefault()?.GetMethod;
                        if (mi2 == null)
                        {
                            throw new InvalidOperationException("Lambda expression does not match data type");
                        }
                        expr = Expression.Call(expr, mi2, arg);
                        break;
                    case ExpressionType.MemberAccess:
                        pi = Expression.Lambda(expr, parameter).GetPropertyInfo();
                        Type t = (pi != null) ? pi.PropertyType : parameter.Type;
                        string sortByField = ((PropertyInfo)tup.Item2).Name;

                        if (t.GetProperty(sortByField) == null)
                        {
                            throw new InvalidOperationException("The property name on TModel must exactly match a public property on TData");
                        }

                        expr = Expression.Property(expr, sortByField);
                        break;
                }
            }

            var lambda = Expression.Lambda(expr, parameter);
            DataType = typeof(TData);
            DataValueType = lambda.GetPropertyInfo().PropertyType;

            return lambda;
        }
    }

    /// <summary>
    /// Represents a sorted grid with a user-specified sort expression
    /// </summary>
    /// <typeparam name="TModel"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    /// <typeparam name="TDataModel"></typeparam>
    /// <typeparam name="TDataValue"></typeparam>
    public class GridSort<TModel, TValue, TDataModel, TDataValue> : IGridSort
    {
        /// <summary>
        /// Direction to sort
        /// </summary>
        public SortOrder SortOrder { get; private set; }

        /// <summary>
        /// Column name
        /// </summary>
        public string ColumnName { get; private set; }
        
        /// <summary>
        /// Model type
        /// </summary>
        public Type ModelType => typeof(TModel);
        
        /// <summary>
        /// Value type
        /// </summary>
        public Type ValueType => typeof(TValue);
        
        /// <summary>
        /// Data type
        /// </summary>
        public Type DataType => typeof(TDataModel);

        /// <summary>
        /// Data value type
        /// </summary>
        public Type DataValueType => typeof(TDataValue);

        private Expression SortExpression { get; set; }

        /// <summary>
        /// Constructs a new GridSort
        /// </summary>
        /// <param name="columnName"></param>
        /// <param name="sortExpression"></param>
        /// <param name="sortOrder"></param>
        public GridSort(string columnName, Expression<Func<TDataModel, TDataValue>> sortExpression, SortOrder sortOrder)
        {
            ColumnName = columnName;
            SortOrder = sortOrder;
            SortExpression = sortExpression;
        }

        /// <summary>
        /// Retrieves expression used to sort grid
        /// </summary>
        /// <typeparam name="TData"></typeparam>
        /// <returns></returns>
        public Expression GetSortExpression<TData>()
        {
            if (typeof(TData) != typeof(TDataModel))
            {
                throw new ArgumentException("Generic type is not the correct data type");
            }

            return SortExpression;
        }
    }
}

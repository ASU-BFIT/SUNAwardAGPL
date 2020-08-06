using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Security.Principal;
using System.Reflection;
using System.ComponentModel;

using BTS.Common.Web;

namespace BTS.Common.Mvc.Grid
{
    /// <summary>
    /// Generic interface for type erasure
    /// </summary>
    public interface IColumn
    {
        /// <summary>
        /// Model type
        /// </summary>
        Type ModelType { get; }

        /// <summary>
        /// Data type
        /// </summary>
        Type DataType { get; }

        /// <summary>
        /// Reflection info for column model property
        /// </summary>
        PropertyInfo PropertyInfo { get; }

        /// <summary>
        /// Expression to retrieve column data
        /// </summary>
        LambdaExpression PropertyExpr { get; }

        /// <summary>
        /// Column type
        /// </summary>
        ColumnType ColumnType { get; }

        /// <summary>
        /// Render column as-is (do not escape HTML)
        /// </summary>
        bool AllowRawHtml { get; }

        /// <summary>
        /// Format string to render column data, {0} is placeholder
        /// </summary>
        string FormatString { get; }

        /// <summary>
        /// Format string to render column data for export, {0} is placeholder
        /// </summary>
        string FormatStringForExport { get; }

        /// <summary>
        /// Internal column name
        /// </summary>
        string ColumnName { get; }

        /// <summary>
        /// Display name
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// Is editable
        /// </summary>
        bool Editable { get; }

        /// <summary>
        /// Is exportable
        /// </summary>
        bool Exportable { get; }

        /// <summary>
        /// Action for drilldown link
        /// </summary>
        string DrillDownAction { get; }

        /// <summary>
        /// Controller for drilldown link
        /// </summary>
        string DrillDownController { get; }

        /// <summary>
        /// Tab name drilldown link opens
        /// </summary>
        string DrillDownTabName { get; }

        /// <summary>
        /// Is sortable
        /// </summary>
        bool Sortable { get; }

        /// <summary>
        /// Default sort order
        /// </summary>
        SortOrder? DefaultSort { get; }

        /// <summary>
        /// Expression to pass to the db in order to sort by this column
        /// </summary>
        Expression SortExpression { get; }

        /// <summary>
        /// Inline edit HTML
        /// </summary>
        InputField EditTemplate { get; }
    }

    /// <summary>
    /// Generic interface for type erasure
    /// </summary>
    /// <typeparam name="TModel"></typeparam>
    public interface IColumn<TModel> : IColumn
    {
        /// <summary>
        /// Callback to format data for display
        /// </summary>
        Func<TModel, string> FormatCallback { get; }

        /// <summary>
        /// Callback to format data for export
        /// </summary>
        Func<TModel, string> FormatCallbackForExport { get; }

        /// <summary>
        /// Test if user is allowed to see data
        /// </summary>
        Func<IPrincipal, bool> CanDisplay { get; }

        /// <summary>
        /// Test if user is allowed to edit data
        /// </summary>
        Func<IPrincipal, TModel, object, bool> CanEdit { get; }

        /// <summary>
        /// Test if user is allowed to drilldown
        /// </summary>
        Func<IPrincipal, TModel, object, bool> CanDrillDown { get; }

        /// <summary>
        /// The function to call to populate drilldown id link. If null, uses grid default key.
        /// </summary>
        Func<TModel, string> DrillDownIdFunc { get; }

        /// <summary>
        /// Get sort order for column
        /// </summary>
        /// <param name="order"></param>
        /// <returns></returns>
        IGridSort GetGridSort(SortOrder? order = null);

        /// <summary>
        /// Get value for cell
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        object GetValue(TModel row);
    }

    /// <summary>
    /// A column bound to a property on the model for use on the grid.
    /// </summary>
    public class Column<TModel, TData> : IColumn<TModel>
    {
        /// <summary>
        /// Model type
        /// </summary>
        public Type ModelType => typeof(TModel);

        /// <summary>
        /// Data type
        /// </summary>
        public Type DataType => typeof(TData);

        /// <summary>
        /// The model property this column is displaying, read only
        /// </summary>
        public PropertyInfo PropertyInfo { get; private set; }

        /// <summary>
        /// Sets the type of this column, default is DefaultVisible
        /// </summary>
        public ColumnType ColumnType { get; set; }

        /// <summary>
        /// Allow raw html in formatting, otherwise the value returned by Format* or default formatters is encoded.
        /// </summary>
        public bool AllowRawHtml { get; set; }

        /// <summary>
        /// Sets a custom format string (see String.Format) for this column. The {0} parameter will
        /// be the model property for the column to be rendered.
        /// </summary>
        public string FormatString { get; set; }

        /// <summary>
        /// Sets a custom format string when exporting this column to excel or csv. The {0} parameter
        /// will be the model property. If unset, will try to use FormatCallback instead. Failing that,
        /// the property will be rendered raw (in other words, it does not fall back to FormatString).
        /// </summary>
        public string FormatStringForExport { get; set; }

        /// <summary>
        /// Sets a custom format callback function that returns a string for this column. The callback
        /// is passed the entire model being rendered in the row. This takes precedence over FormatString.
        /// </summary>
        public Func<TModel, string> FormatCallback { get; set; }

        /// <summary>
        /// Sets a custom format callback function that returns a string for this column when exporting to
        /// excel or csv. This takes precedence over FormatStringForExport.
        /// </summary>
        public Func<TModel, string> FormatCallbackForExport { get; set; }

        /// <summary>
        /// The internal name for this column.
        /// </summary>
        public string ColumnName { get { return PropertyInfo.Name; } }

        /// <summary>
        /// The display name for this column. If unset, uses the DisplayNameAttribute on the model property,
        /// or if that is unset the string name of the model property.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// If true, allows this column to be modified inline. A save action must be set for the grid for this to have
        /// any actual effect.
        /// </summary>
        public bool Editable { get; set; }

        /// <summary>
        /// If false, this column does not appear when exporting the grid into excel or csv.
        /// </summary>
        public bool Exportable { get; set; }

        /// <summary>
        /// The action non-zero results will drill down into, it is passed a model of type T which is the drilled-down row
        /// </summary>
        public string DrillDownAction { get; set; }

        /// <summary>
        /// The controller non-zero results will drill down into
        /// </summary>
        public string DrillDownController { get; set; }

        /// <summary>
        /// The function to call to populate drilldown id link. If null, uses grid default key.
        /// </summary>
        public Func<TModel, string> DrillDownIdFunc { get; set; }

        /// <summary>
        /// The tab name shown for the drilldown (can be left as null and the tab name updated via javascript).
        /// This is interpreted as a format string where {0} is the column value.
        /// </summary>
        public string DrillDownTabName { get; set; }

        /// <summary>
        /// The edit template to show if this column is editable inline. If null, a default InputField will be generated
        /// for this property.
        /// </summary>
        public InputField EditTemplate { get; set; }

        /// <summary>
        /// Allows for adding restrictions on displaying any particular column (e.g. security).
        /// This method should return true if the user is allowed to view this column, false if they are not.
        /// If this returns false, the column essentially does not exist for that user; it is not shown in any UI
        /// and is not rendered as part of the grid HTML.
        /// </summary>
        public Func<IPrincipal, bool> CanDisplay { get; set; }

        /// <summary>
        /// Allows for adding restrictions on inline editing for any particular column.
        /// This method should return true if the user is allowed to edit this column, false if they are not.
        /// If this returns false, the column is treated as if the Editable property was set to false.
        /// The default implementation simply returns true in all cases.
        /// </summary>
        public Func<IPrincipal, TModel, TData, bool> CanEdit { get; set; }

        /// <summary>
        /// Allows for adding restrictions on drilling down for any particular column.
        /// This method should return true if the user is allowed to drill down this column, false if they are not.
        /// If this returns false, the column is treated as if no DrillDown was defined for it.
        /// The default implementation simlpy returns true in all cases.
        /// </summary>
        public Func<IPrincipal, TModel, TData, bool> CanDrillDown { get; set; }

        // explicit instantation of IColumn<TModel>.CanEdit and CanDrillDown because we want our actual ones to be strongly-typed
        Func<IPrincipal, TModel, object, bool> IColumn<TModel>.CanEdit
        {
            get
            {
                return (u, m, d) => CanEdit != null ? CanEdit(u, m, (TData)d) : true;
            }
        }

        Func<IPrincipal, TModel, object, bool> IColumn<TModel>.CanDrillDown
        {
            get
            {
                return (u, m, d) => CanDrillDown != null ? CanDrillDown(u, m, (TData)d) : true;
            }
        }

        /// <summary>
        /// Expression to retrieve column data
        /// </summary>
        public LambdaExpression PropertyExpr { get { return _property; } }
        internal Expression<Func<TModel, TData>> _property;
        internal Func<TModel, TData> _delegate;

        /// <summary>
        /// Controls whether the grid is sortable, a default sort can be set even if this is false
        /// which means that the default sort cannot be changed.
        /// </summary>
        public bool Sortable { get; set; }

        /// <summary>
        /// Controls the default sort order, multiple sort columns are supported however support for this is flakey at best.
        /// The ordering in which multiple sorts are applied is arbitrary and grid js only suports sorting a single column at this time.
        /// </summary>
        public SortOrder? DefaultSort { get; set; }

        /// <summary>
        /// If set, this expression is used to control the underlying sort in the database.
        /// Use the AsSortable&lt;TDataModel, TDataValue&gt;() overload to set this.
        /// </summary>
        public Expression SortExpression { get; private set; }

        private Type SortExpressionModel { get; set; }
        private Type SortExpressionData { get; set; }

        /// <summary>
        /// Creates a new Column.
        /// </summary>
        /// <param name="property">
        /// The model property to render in the column. Automatic formatting is applied based on
        /// the type of the property. This can be overridden by setting the FormatString or FormatCallback properties.
        /// </param>
        public Column(Expression<Func<TModel, TData>> property)
        {
            _property = property;
            _delegate = property.Compile();

            if (!(property.Body is MemberExpression propBody))
            {
                if (!(property.Body is UnaryExpression uBody))
                {
                    throw new ArgumentException("Argument must be a lambda expression accessing a property member", "property");
                }

                propBody = uBody.Operand as MemberExpression;
            }

            PropertyInfo = propBody.Member as PropertyInfo;

            if (PropertyInfo == null)
            {
                throw new ArgumentException("Argument must be a lambda expression accessing a property member", "property");
            }
        }

        /// <summary>
        /// Gets the property value, or null if that property is not found
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        public object GetValue(TModel row)
        {
            try
            {
                return _delegate(row);
            }
            catch (NullReferenceException)
            {
                return null;
            }
        }

        /// <summary>
        /// Defines the column as sortable or not
        /// </summary>
        /// <param name="sortable"></param>
        /// <returns></returns>
        public Column<TModel, TData> AsSortable(bool sortable = true)
        {
            Sortable = sortable;

            return this;
        }

        /// <summary>
        /// Defines the column as sortable or not
        /// </summary>
        /// <param name="defaultSort"></param>
        /// <param name="sortable"></param>
        /// <returns></returns>
        public Column<TModel, TData> AsSortable(SortOrder defaultSort, bool sortable = true)
        {
            DefaultSort = defaultSort;
            Sortable = sortable;

            return this;
        }

        /// <summary>
        /// Defines the column as sortable or not
        /// </summary>
        /// <typeparam name="TDataModel"></typeparam>
        /// <typeparam name="TDataValue"></typeparam>
        /// <param name="underlyingExpr"></param>
        /// <param name="sortable"></param>
        /// <returns></returns>
        public Column<TModel, TData> AsSortable<TDataModel, TDataValue>(Expression<Func<TDataModel, TDataValue>> underlyingExpr, bool sortable = true)
        {
            Sortable = sortable;
            SortExpression = underlyingExpr;
            SortExpressionModel = typeof(TDataModel);
            SortExpressionData = typeof(TDataValue);

            return this;
        }

        /// <summary>
        /// Defines the column as sortable or not
        /// </summary>
        /// <typeparam name="TDataModel"></typeparam>
        /// <typeparam name="TDataValue"></typeparam>
        /// <param name="defaultSort"></param>
        /// <param name="underlyingExpr"></param>
        /// <param name="sortable"></param>
        /// <returns></returns>
        public Column<TModel, TData> AsSortable<TDataModel, TDataValue>(SortOrder defaultSort, Expression<Func<TDataModel, TDataValue>> underlyingExpr, bool sortable = true)
        {
            DefaultSort = defaultSort;
            Sortable = sortable;
            SortExpression = underlyingExpr;
            SortExpressionModel = typeof(TDataModel);
            SortExpressionData = typeof(TDataValue);

            return this;
        }

        /// <summary>
        /// Defines the column as containing a drilldown link.
        /// </summary>
        /// <param name="action">Action to call, passed in a model of type TModel</param>
        /// <param name="controller">Controller to call</param>
        /// <param name="tabName">Tab name to use, or null for default. Use {0} as placeholder for column contents</param>
        /// <returns>this for fluent method chaining</returns>
        public Column<TModel, TData> WithDrillDown(string action, string controller, string tabName = null)
        {
            DrillDownAction = action;
            DrillDownController = controller;
            DrillDownTabName = tabName;

            return this;
        }

        /// <summary>
        /// Defines the column as containing a drilldown link.
        /// </summary>
        /// <param name="action">Action to call, passed in a model of type TModel</param>
        /// <param name="controller">Controller to call</param>
        /// <param name="idFunc">String appended to the end of the controller/action as the id parameter. If null, uses the overall grid column key.</param>
        /// <param name="tabName">Tab name to use, or null for default. Use {0} as placeholder for column contents.</param>
        /// <returns></returns>
        public Column<TModel, TData> WithDrillDown(string action, string controller, Func<TModel, string> idFunc, string tabName)
        {
            DrillDownAction = action;
            DrillDownController = controller;
            DrillDownTabName = tabName;
            DrillDownIdFunc = idFunc;

            return this;
        }

        /// <summary>
        /// Restricts the ability to view the column to the given security level and permissions
        /// </summary>
        /// <param name="level"></param>
        /// <param name="permissions"></param>
        /// <returns></returns>
        public Column<TModel, TData> WithSecurity(SecurityFlags level, params Enum[] permissions)
        {
            CanDisplay = user => user.IsAllowed(level, permissions);

            return this;
        }

        /// <summary>
        /// Restricts the ability to view the column to the given roles
        /// </summary>
        /// <param name="roles"></param>
        /// <returns></returns>
        public Column<TModel, TData> WithSecurity(params string[] roles)
        {
            CanDisplay = user => user.IsAllowed(roles);

            return this;
        }

        /// <summary>
        /// Restricts the ability to edit the column to the given security level and permissions
        /// </summary>
        /// <param name="level"></param>
        /// <param name="permissions"></param>
        /// <returns></returns>
        public Column<TModel, TData> WithEditSecurity(SecurityFlags level, params Enum[] permissions)
        {
            CanEdit = (user, r, c) => user.IsAllowed(level, permissions);

            return this;
        }

        /// <summary>
        /// Restricts the ability to edit the column to the given roles
        /// </summary>
        /// <param name="roles"></param>
        /// <returns></returns>
        public Column<TModel, TData> WithEditSecurity(params string[] roles)
        {
            CanEdit = (user, r, c) => user.IsAllowed(roles);

            return this;
        }

        /// <summary>
        /// Restricts the ability to drill down to the given security level and permissions
        /// </summary>
        /// <param name="level"></param>
        /// <param name="permissions"></param>
        /// <returns></returns>
        public Column<TModel, TData> WithDrillDownSecurity(SecurityFlags level, params Enum[] permissions)
        {
            CanDrillDown = (user, r, c) => user.IsAllowed(level, permissions);

            return this;
        }

        /// <summary>
        /// Restricts the ability to drill down to the given roles
        /// </summary>
        /// <param name="roles"></param>
        /// <returns></returns>
        public Column<TModel, TData> WithDrillDownSecurity(params string[] roles)
        {
            CanDrillDown = (user, r, c) => user.IsAllowed(roles);

            return this;
        }

        /// <summary>
        /// Returns the name of this column, based on the following sources:
        /// <list type="bulleted">
        ///   <item><description>DisplayName property</description></item>
        ///   <item><description>DisplayAttribute.ShortName on the model property</description></item>
        ///   <item><description>DisplayAttribute.Name on the model property</description></item>
        ///   <item><description>DisplayNameAttribute on the model property</description></item>
        ///   <item><description>model property name</description></item>
        /// </list>
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return DisplayName ?? PropertyInfo.GetShortName();
        }

        /// <summary>
        /// Gets how column is sorted
        /// </summary>
        /// <param name="order"></param>
        /// <returns></returns>
        public IGridSort GetGridSort(SortOrder? order = null)
        {
            order = order ?? DefaultSort;

            if (order == null)
            {
                return null;
            }

            if (SortExpression != null)
            {
                var type = typeof(GridSort<,,,>).MakeGenericType(typeof(TModel), typeof(TData), SortExpressionModel, SortExpressionData);
                return (IGridSort)Activator.CreateInstance(type, new object[] { ColumnName, SortExpression, order.Value });
            }

            return new GridSort<TModel, TData>(ColumnName, _property, order.Value);
        }
    }
}

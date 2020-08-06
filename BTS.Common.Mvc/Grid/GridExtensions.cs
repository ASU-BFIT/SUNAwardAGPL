using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.IO;
using System.Reflection;

using BTS.Common.Mvc.Grid.Infrastructure;
using BTS.Common.Mvc.Grid.Models;

namespace BTS.Common.Mvc.Grid
{
    /// <summary>
    /// Extension methods for Grids
    /// </summary>
    public static class GridExtensions
    {
        /// <summary>
        /// Renders a Grid to the view
        /// </summary>
        /// <param name="helper">HTML helper for view</param>
        /// <param name="grid">Grid to render</param>
        /// <param name="options">Optional options to pass to grid</param>
        /// <returns></returns>
        public static MvcHtmlString RenderGrid(this HtmlHelper helper, IGrid grid, IGridOptions options = null)
        {
            return grid.Render(helper, options);
        }

        /// <summary>
        /// Returns a PartialView containing the rendered grid
        /// </summary>
        /// <param name="controller">Controller rendering the grid</param>
        /// <param name="grid">Grid to render</param>
        /// <param name="options">Optional options to pass to grid</param>
        /// <returns></returns>
        public static PartialViewResult RenderGrid(this Controller controller, IGrid grid, IGridOptions options = null)
        {
            return grid.Render(controller, options);
        }

        /// <summary>
        /// Returns a result containing the grid exported to the given export type
        /// </summary>
        /// <param name="controller"></param>
        /// <param name="grid"></param>
        /// <param name="filename"></param>
        /// <param name="exportType"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static FileStreamResult ExportGrid(this Controller controller, IGrid grid, string filename, ExportType exportType, IGridOptions options = null)
        {
            return grid.Export(controller, filename, exportType, options);
        }

        /// <summary>
        /// Generates a form containing grid filters. AJAX is used to filter the grid.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="helper"></param>
        /// <param name="grid"></param>
        /// <param name="gridOptions"></param>
        /// <param name="horizontal">Whether or not the grid form is horizontal (one field per row)</param>
        /// <returns></returns>
        public static GridForm<T> FiltersForGrid<T>(this HtmlHelper helper, IGrid grid, IGridOptions gridOptions, bool horizontal = false)
            where T : new()
        {
            if (grid as IGrid<T> == null || gridOptions as GridOptions<T> == null)
            {
                throw new InvalidOperationException("FiltersForGrid<T> must have the same underlying type as the grid");
            }

            return new GridForm<T>(helper, grid, gridOptions, horizontal);
        }

        /// <summary>
        /// Generates a form containing grid filters. AJAX is used to filter the grid.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="helper"></param>
        /// <param name="grid"></param>
        /// <param name="gridOptions"></param>
        /// <param name="horizontal">Whether or not the grid form is horizontal (one field per row)</param>
        /// <returns></returns>
        public static GridForm<T> FiltersForGrid<T>(this HtmlHelper helper, IGrid<T> grid, GridOptions<T> gridOptions, bool horizontal = false)
        {
            return new GridForm<T>(helper, grid, gridOptions, horizontal);
        }

        /// <summary>
        /// Marks a column as being editable
        /// </summary>
        /// <typeparam name="TModel"></typeparam>
        /// <typeparam name="TData"></typeparam>
        /// <param name="column"></param>
        /// <param name="editable">Whether or not the column is editable</param>
        /// <returns>Returns the column for chaining method calls</returns>
        public static Column<TModel, TData> AsEditable<TModel, TData>(this Column<TModel, TData> column, bool editable = true)
            where TModel : new()
        {
            column.Editable = editable;
            return column;
        }

        /// <summary>
        /// Generates the HTML field used to edit a grid column
        /// </summary>
        /// <param name="helper"></param>
        /// <param name="cell"></param>
        /// <returns></returns>
        public static InputField EditTemplateFor(this HtmlHelper<GridModel> helper, GridCellModel cell)
        {
            var column = cell.Column.Column;
            var helperType = typeof(HtmlHelper<>).MakeGenericType(column.ModelType);
            var asMethod = typeof(MvcExtensions).GetMethod(nameof(MvcExtensions.As)).MakeGenericMethod(column.ModelType);
            var newHelper = (HtmlHelper)asMethod.Invoke(null, new object[] { helper, cell.Row });

            if (column.EditTemplate != null)
            {
                return column.EditTemplate.WithHelper(newHelper);
            }

            var inputFieldType = typeof(InputField<,>).MakeGenericType(column.ModelType, column.DataType);
            var funcType = typeof(Func<,>).MakeGenericType(column.ModelType, column.DataType);
            var exprType = typeof(Expression<>).MakeGenericType(funcType);
            var inputField = inputFieldType.GetConstructor(new Type[] { exprType, helperType }).Invoke(new object[] { column.PropertyExpr, newHelper });
            inputFieldType.GetMethod(nameof(InputField<object, object>.ForGrid)).Invoke(inputField, null);

            return (InputField)inputField;
        }

        /// <summary>
        /// Takes a query to pull results and applies grid logic to it such as sorting and paging.
        /// The result is cast to the expected grid model if the data type is different.
        /// </summary>
        /// <typeparam name="TModel"></typeparam>
        /// <typeparam name="TData"></typeparam>
        /// <param name="data"></param>
        /// <param name="options"></param>
        /// <param name="selectFunc"></param>
        /// <returns></returns>
        public static IEnumerable<TModel> SelectForGrid<TModel, TData>(this IQueryable<TData> data, GridOptions<TModel> options, Func<TData, TModel> selectFunc = null)
        {
            IOrderedQueryable<TData> sorted = null;
            bool first = true;

            foreach (var sort in options.SortColumns)
            {
                var sortExpr = sort.GetSortExpression<TData>(); // this an Expression representing Expression<Func<TData, TDataValue>>
                string method = null;

                if (first)
                {
                    if (sort.SortOrder == SortOrder.Ascending)
                    {
                        method = nameof(Queryable.OrderBy);
                    }
                    else
                    {
                        method = nameof(Queryable.OrderByDescending);
                    }
                }
                else
                {
                    if (sort.SortOrder == SortOrder.Ascending)
                    {
                        method = nameof(Queryable.ThenBy);
                    }
                    else
                    {
                        method = nameof(Queryable.ThenByDescending);
                    }
                }

                sorted = (IOrderedQueryable<TData>)typeof(Queryable).GetMethods()
                    .Where(m => m.Name == method && m.GetParameters().Length == 2).First()
                    .MakeGenericMethod(sort.DataType, sort.DataValueType)
                    .Invoke(null, new object[] { first ? data : sorted, sortExpr });

                first = false;
            }

            if (sorted != null)
            {
                data = sorted;
            }

            if (options.PageSize > 0)
            {
                if (options.PageIndex > 0)
                {
                    data = data.Skip(options.PageIndex * options.PageSize.Value);
                }

                data = data.Take(options.PageSize.Value);
            }

            if (selectFunc == null)
            {
                if (typeof(TModel) != typeof(TData))
                {
                    // apply a default selector that constructs a new TModel given a TData
                    // can't directly use new TModel() here since only paramless constructors are allowed for that on generics
                    // as a result, this is quite slower than a direct construction and a speedup can be obtained by passing in a selectFunc
                    selectFunc = d => (TModel)Activator.CreateInstance(typeof(TModel), new object[] { d });
                }
                else
                {
                    // same data type, just cast it to appease the compiler
                    return (IEnumerable<TModel>)data;
                }
            }

            return data.Select(selectFunc);
        }
    }
}

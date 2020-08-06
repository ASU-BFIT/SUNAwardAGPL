using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Routing;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using System.Security;
using System.Security.Principal;
using System.Reflection;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

using BTS.Common.Web;
using BTS.Common.Mvc.Grid.Models;
using BTS.Common.Mvc.Grid.Infrastructure;
using BTS.Common.Excel;
using System.Diagnostics.CodeAnalysis;

namespace BTS.Common.Mvc.Grid
{
    /// <summary>
    /// Non-generic grid interface for type erasure
    /// </summary>
    public interface IGrid
    {
        /// <summary>
        /// Type of the grid model
        /// </summary>
        Type ModelType { get; }

        /// <summary>
        /// Type of the grid key column
        /// </summary>
        Type KeyType { get; }

        /// <summary>
        /// Render the grid to HTML
        /// </summary>
        /// <param name="helper"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        MvcHtmlString Render(HtmlHelper helper, IGridOptions options);

        /// <summary>
        /// Render the grid to a partial view
        /// </summary>
        /// <param name="controller"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        PartialViewResult Render(Controller controller, IGridOptions options);

        /// <summary>
        /// Export the grid as a stream
        /// </summary>
        /// <param name="exportType"></param>
        /// <param name="context"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        Stream Export(ExportType exportType, HttpContextBase context, IGridOptions options);

        /// <summary>
        /// Export the grid as a stream, specifying the file name
        /// </summary>
        /// <param name="controller"></param>
        /// <param name="filename"></param>
        /// <param name="exportType"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        FileStreamResult Export(Controller controller, string filename, ExportType exportType, IGridOptions options);
    }

    /// <summary>
    /// Generic grid interface for type erasure
    /// </summary>
    /// <typeparam name="TModel"></typeparam>
    public interface IGrid<TModel> : IGrid
    {
        /// <summary>
        /// Render the grid to HTML
        /// </summary>
        /// <param name="helper"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        MvcHtmlString Render(HtmlHelper helper, GridOptions<TModel> options);

        /// <summary>
        /// Render the grid to a partial view
        /// </summary>
        /// <param name="controller"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        PartialViewResult Render(Controller controller, GridOptions<TModel> options);

        /// <summary>
        /// Export the grid to a stream
        /// </summary>
        /// <param name="exportType"></param>
        /// <param name="context"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        Stream Export(ExportType exportType, HttpContextBase context, GridOptions<TModel> options);

        /// <summary>
        /// Export the grid to a stream, specifying the file name
        /// </summary>
        /// <param name="controller"></param>
        /// <param name="filename"></param>
        /// <param name="exportType"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        FileStreamResult Export(Controller controller, string filename, ExportType exportType, GridOptions<TModel> options);
    }

    /// <summary>
    /// Strongly-typed grid control for ASP.NET MVC
    /// </summary>
    /// <typeparam name="TModel">Model type</typeparam>
    /// <typeparam name="TKey">Key column type</typeparam>
    public class Grid<TModel, TKey> : IGrid<TModel>
        where TModel : new()
    {
        /// <summary>
        /// Name of the grid. Each grid in an application should have a unique name
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Type of the model
        /// </summary>
        public Type ModelType { get { return typeof(TModel); } }

        /// <summary>
        /// Type of the key column
        /// </summary>
        public Type KeyType { get { return typeof(TKey); } }

        /// <summary>
        /// Grid Columns
        /// </summary>
        public List<IColumn<TModel>> Columns { get; set; }

        /// <summary>
        /// If true, allows the user to page through the grid, if false all grid rows will be shown at once
        /// (and page size related options are ignored).
        /// Default true.
        /// </summary>
        public bool AllowPaging { get; set; } = true;

        /// <summary>
        /// The type of row selection to use for the grid. By default, no row selection is enabled.
        /// </summary>
        public RowSelectType RowSelectType { get; set; }

        /// <summary>
        /// The page size options that will be shown on the grid
        /// Default: 5, 10, 20, 50, 100
        /// </summary>
        public List<int> AllowedPageSizes { get; set; } = new List<int>() { 5, 10, 20, 50, 100 };

        /// <summary>
        /// The default page size (doesn't need to appear in AllowedPageSizes, but if
        /// it is missing there, then the user will not be able to reselect it).
        /// Default 10
        /// </summary>
        public int DefaultPageSize { get; set; } = 10;

        /// <summary>
        /// The underlying data source used to retrieve results.
        /// The source should access GridOptions to handle filtering, paging, and sorting.
        /// The returned IEnumerable&lt;T&gt; should only contain the results for the single page being shown.
        /// </summary>
        public Func<GridOptions<TModel>, IEnumerable<TModel>> DataSourcePage { get; set; }

        /// <summary>
        /// A callback that returns the total number of rows in the result set.
        /// Note that DataSourcePage will not be called if this returns 0 (in order to speed up execution time).
        /// </summary>
        public Func<GridOptions<TModel>, int> DataSourceTotal { get; set; }

        /// <summary>
        /// If true, inline editing of grid rows is enabled by clicking the edit icon next to each appropriate row.
        /// </summary>
        public bool AllowRowEditing { get; set; }

        /// <summary>
        /// If true, inline deletion of grid rows is enabled by clicking the delete icon next to each appropriate row.
        /// </summary>
        public bool AllowRowDeletion { get; set; }

        /// <summary>
        /// The Controller/Action to call to refresh the grid, passed in a GridOptions&lt;T&gt; as the model.
        /// The action should return the PartialViewResult obtained by calling Grid&lt;T&gt;.Render(Controller, GridOptions&lt;T&gt;).
        /// </summary>
        public string GridRefreshAction { get; set; }

        /// <summary>
        /// The Controller/Action to call to edit a row, model binding syntax may be used to obtain all of the row's fields.
        /// It is up to the action to double-check appropriate security and other validation.
        /// The action should return JSON {"success": true|false, "model": new row values} (the model key should be the JSON
        /// version of T used to render the grid).
        /// </summary>
        public string RowEditAction { get; set; }

        /// <summary>
        /// The Controller/Action to call to delete a row, model binding syntax may be used to obtain all of the row's fields.
        /// It is up to the action to double-check appropriate security and other validation.
        /// The action should return JSON {"success": true|false}.
        /// </summary>
        public string RowDeleteAction { get; set; }

        /// <summary>
        /// The Controller/Action to call to export a grid. This should simply be a thin wrapper around Grid.Export().
        /// </summary>
        public string GridExportAction { get; set; }

        /// <summary>
        /// Callback to determine if the user has permission to edit a given row. This is called for each row to allow for
        /// fine-grained permissions.
        /// </summary>
        public Func<TModel, IPrincipal, bool> CanEditRow { get; set; }

        /// <summary>
        /// Callback to determine if the user has permission to delete a given row. This is called for each row to allow for
        /// fine-grained permissions.
        /// </summary>
        public Func<TModel, IPrincipal, bool> CanDeleteRow { get; set; }

        /// <summary>
        /// The list of ways the user can export the grid. Exporting will render all rows in the result set, PDF export retains
        /// grid styling/customization options (shown/hidden columns, column order, display formatting) whereas CSV and Excel
        /// output more of a "raw" format more useful for automated processing.
        /// Set to an empty list to disable exporting entirely.
        /// Default: CSV, Excel, PDF.
        /// </summary>
        public List<ExportType> AllowedExportTypes { get; set; } = new List<ExportType>()
        {
            //ExportType.PDF, // disabling PDF for now as it doesn't work
            ExportType.Excel,
            ExportType.CSV
        };

        /// <summary>
        /// Callback to determine if the user has permission to export into a given type.
        /// </summary>
        public Func<IPrincipal, ExportType, bool> CanExport { get; set; }

        /// <summary>
        /// Text to display when there are no grid results. Defaults to "No Results."
        /// </summary>
        public string NoResultsText { get; set; }

        /// <summary>
        /// If true, the grid is initially hidden from view, and a refresh is needed to make it visible.
        /// No data will be retrieved for the initial load in this case. (default false)
        /// </summary>
        public bool InitiallyHidden { get; set; }

        private readonly Expression<Func<TModel, TKey>> _idProperty;
        private Func<TModel, TKey> IdPropertyFunc { get; set; }

        /// <summary>
        /// Creates a new grid control
        /// </summary>
        /// <param name="name">Unique name for the grid, used to persist user-selected grid options to the db</param>
        /// <param name="idProperty">Property for the model's id; this is automatically rendered as a hidden column</param>
        public Grid(string name, Expression<Func<TModel, TKey>> idProperty)
        {
            Name = name;
            _idProperty = idProperty;
            IdPropertyFunc = _idProperty.Compile();

            Controllers.GridController.AddToCache(name, this);
        }

        /// <summary>
        /// Renders the grid to HTML
        /// </summary>
        /// <returns></returns>
        public MvcHtmlString Render(HtmlHelper helper, IGridOptions options = null)
        {
            var opts = RenderInternal(options, helper.ViewContext.HttpContext);

            return helper.Partial("~/Views/Common/Grid/Grid.cshtml", GetGridModel(helper, opts), helper.ViewData);
        }

        /// <summary>
        /// Renders the grid, for use inside of a Controller
        /// </summary>
        /// <param name="controller"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public PartialViewResult Render(Controller controller, IGridOptions options = null)
        {
            var opts = RenderInternal(options, controller.HttpContext);

            // invoke the controller's PartialView() method, which is protected internal
            return (PartialViewResult)controller.GetType().InvokeMember("PartialView", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.InvokeMethod, null, controller, new object[] { "~/Views/Common/Grid/Grid.cshtml", GetGridModel(controller.GetHtmlHelper<TModel>(), opts) });
        }


        /// <summary>
        /// Exports the grid
        /// </summary>
        /// <param name="exportType"></param>
        /// <param name="context"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        [SuppressMessage("Code Quality", "IDE0068:Use recommended dispose pattern", Justification = "We are returning the stream object, so we can't dispose it locally")]
        public Stream Export(ExportType exportType, HttpContextBase context, IGridOptions options = null)
        {
            // If exporting to PDF, we render a basic table and add a cover page noting filter options and timestamp.
            // If exporting to Excel or CSV, we dump all of the data at once. Excel also contains a header with filter options and timestamp. CSV does not.
            // In any case, the grid UI/chrome is not kept for the export -- only simple formatting is applied.
            // The return value is a Stream which contains the file to download. The exact type of Stream varies based on file size.
            var opts = RenderInternal(options, context);
            opts.PageIndex = 0;
            opts.PageSize = 0;
            var model = GetGridModel(null, opts);
            var colNames = new List<string>();
            var colInfo = new List<GridExport>();
            var rows = new List<Dictionary<string, string>>();

            foreach (var c in model.Columns)
            {
                colNames.Add(c.ColumnName);
                colInfo.Add(new GridExport(c.ColumnName, c.DisplayName));
            }

            foreach (var r in model.Rows)
            {
                var d = new Dictionary<string, string>();

                foreach (var c in r.Cells)
                {
                    d[c.Column.ColumnName] = c.ExportValue;
                }

                rows.Add(d);
            }

            var exporter = new Exporter<Dictionary<string, string>>(rows, colNames, colInfo);

            // TODO: Need a new stream type that transparently switches from memory-backed to file-backed should it get too much data
            // as such doesn't exist, it would need to be custom-made (probably in main BTS.Common assembly as it doesn't depend on any MVC stuff)
            var stream = new MemoryStream();

            switch (exportType)
            {
                case ExportType.CSV:
                    exporter.ToCsv(stream, true);
                    break;
                case ExportType.Excel:
                    exporter.ToExcel(stream);
                    break;
                case ExportType.PDF:
                    //exporter.ToPDF(stream);
                    stream.Dispose();
                    throw new NotImplementedException();
            }


            // rewind stream to beginning so that the file actually has data
            stream.Seek(0, SeekOrigin.Begin);

            return stream;
        }

        /// <summary>
        /// Exports the Grid, for use inside of a Controller
        /// </summary>
        /// <param name="controller"></param>
        /// <param name="filename"></param>
        /// <param name="exportType"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        [SuppressMessage("Code Quality", "IDE0068:Use recommended dispose pattern", Justification = "We pass this stream to returned FileStreamResult, which will automatically dispose the stream when it is disposed")]
        public FileStreamResult Export(Controller controller, string filename, ExportType exportType, IGridOptions options = null)
        {
            var stream = Export(exportType, controller.HttpContext, options);

            string extension = null;
            string contentType = null;

            switch (exportType)
            {
                case ExportType.CSV:
                    extension = ".csv";
                    contentType = "text/csv";
                    break;
                case ExportType.Excel:
                    extension = ".xlsx";
                    contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                    break;
                case ExportType.PDF:
                    extension = ".pdf";
                    contentType = "application/pdf";
                    break;
            }

            filename += extension;

            // invoke the controller's File() method, which is protected internal
            return (FileStreamResult)controller.GetType().InvokeMember("File", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.InvokeMethod, null, controller, new object[] { stream, contentType, filename });
        }

        /// <summary>
        /// Restrict row editing in the grid to a given level and permissions.
        /// </summary>
        /// <param name="level">Permission level to restrict to</param>
        /// <param name="permissions">Permissions to restrict to at the specified level</param>
        /// <returns>Returns the grid object to allow method chaining</returns>
        public Grid<TModel, TKey> RestrictEditingTo(SecurityFlags level, params Enum[] permissions)
        {
            CanEditRow = (o, user) => user.IsAllowed(level, permissions);

            return this;
        }

        /// <summary>
        /// Restrict row editing in the grid to a given roles.
        /// </summary>
        /// <param name="roles">Roles to restrict to at the specified level</param>
        /// <returns>Returns the grid object to allow method chaining</returns>
        public Grid<TModel, TKey> RestrictEditingTo(params string[] roles)
        {
            CanDeleteRow = (o, user) => user.IsAllowed(roles);

            return this;
        }

        /// <summary>
        /// Restrict row deletion in the grid to a given level and permissions.
        /// </summary>
        /// <param name="level">Permission level to restrict to</param>
        /// <param name="permissions">Permissions to restrict to at the specified level</param>
        /// <returns>Returns the grid object to allow method chaining</returns>
        public Grid<TModel, TKey> RestrictDeletingTo(SecurityFlags level, params Enum[] permissions)
        {
            CanDeleteRow = (o, user) => user.IsAllowed(level, permissions);

            return this;
        }

        /// <summary>
        /// Restrict row deletion in the grid to a given roles.
        /// </summary>
        /// <param name="roles">Roles to restrict to at the specified level</param>
        /// <returns>Returns the grid object to allow method chaining</returns>
        public Grid<TModel, TKey> RestrictDeletingTo(params string[] roles)
        {
            CanDeleteRow = (o, user) => user.IsAllowed(roles);

            return this;
        }

        MvcHtmlString IGrid<TModel>.Render(HtmlHelper helper, GridOptions<TModel> options)
        {
            return Render(helper, options);
        }

        PartialViewResult IGrid<TModel>.Render(Controller controller, GridOptions<TModel> options)
        {
            return Render(controller, options);
        }

        [SuppressMessage("Code Quality", "IDE0068:Use recommended dispose pattern", Justification = "We are returning the stream object, so we can't dispose it locally")]
        Stream IGrid<TModel>.Export(ExportType exportType, HttpContextBase context, GridOptions<TModel> options)
        {
            return Export(exportType, context, options);
        }

        FileStreamResult IGrid<TModel>.Export(Controller controller, string filename, ExportType exportType, GridOptions<TModel> options)
        {
            return Export(controller, filename, exportType, options);
        }

        private GridOptions<TModel> RenderInternal(IGridOptions opts, HttpContextBase context)
        {
            if (String.IsNullOrEmpty(GridRefreshAction))
            {
                // if we don't have a refresh action, that means that the grid is not interactive
                // (but still could be used to display a static, non-filterable set of data)
                // if any interactivity is enabled, throw an exception
                if (AllowRowEditing || AllowRowDeletion || AllowPaging)
                {
                    throw new InvalidOperationException("Interactive grids require a GridRefreshAction to be set");
                }
            }

            var options = opts as GridOptions<TModel>;

            if (opts != null && options == null)
            {
                throw new InvalidOperationException("Incorrect type of GridOptions passed to Grid.Render");
            }

            if (options == null)
            {
                options = new GridOptions<TModel>()
                {
                    PageIndex = 0,
                    PageSize = DefaultPageSize
                };
            }

            if (options.PageSize == null)
            {
                options.PageSize = DefaultPageSize;
            }

            if (options.Filter == null)
            {
                options.Filter = new TModel();
            }

            if (options.SortColumns == null)
            {
                options.SortColumns = new List<IGridSort>();
            }

            if (options.SortColumnInfo == null)
            {
                options.SortColumnInfo = new List<GridSortModel>();
            }

            if (options.ColumnOrder == null)
            {
                options.ColumnOrder = new List<string>();
                var nonfixed = new List<string>();

                foreach (var column in Columns)
                {
                    switch (column.ColumnType)
                    {
                        case ColumnType.Fixed:
                            options.ColumnOrder.Add(column.ToString());
                            break;
                        case ColumnType.AlwaysVisible:
                        case ColumnType.DefaultVisible:
                            nonfixed.Add(column.ToString());
                            break;
                    }
                }

                options.ColumnOrder.AddRange(nonfixed);
            }

            // let callbacks access this context as well
            options.HttpContext = context;

            // Determine if this is the initial load. If the request is for the refresh action, it is not initial load.
            // Otherwise, it is.
            var routeValues = context.Request.RequestContext.RouteData.Values;
            if (routeValues.ContainsKey("controller") && routeValues.ContainsKey("action"))
            {
                var currentController = routeValues["controller"].ToString();
                var currentAction = routeValues["action"].ToString();
                options.IsInitialLoad = GridRefreshAction != $"{currentController}/{currentAction}";
            }
            else
            {
                // couldn't fetch current controller, action; not an mvc path?
                options.IsInitialLoad = false;
            }

            return options;
        }

        /// <summary>
        /// Gets the actual grid data given the passed-in options
        /// </summary>
        /// <param name="helper">HtmlHelper to render drilldown links, can be null (in which case no drilldown links are rendered)</param>
        /// <param name="options">Grid options</param>
        /// <returns></returns>
        private GridModel GetGridModel(HtmlHelper helper, GridOptions<TModel> options)
        {
            var user = options.HttpContext.User;

            options.PageSize = options.PageSize ?? DefaultPageSize;
            List<ExportType> exportTypes = new List<ExportType>();

            if (!String.IsNullOrEmpty(GridExportAction))
            {
                exportTypes = AllowedExportTypes.Where(t => CanExport == null ? true : CanExport(user, t)).ToList();
            }

            bool loadData = true;
            if (InitiallyHidden && options.IsInitialLoad)
            {
                loadData = false;
            }

            GridModel model = new GridModel()
            {
                GridId = options.GridId,
                RowSelectType = RowSelectType,
                AllowedPageSizes = AllowedPageSizes,
                CurrentPageIndex = options.PageIndex,
                PageSize = options.PageSize.Value,
                GridName = Name,
                ShowNavigation = AllowPaging,
                ShowInlineEdit = AllowRowEditing,
                ShowInlineDelete = AllowRowDeletion,
                InlineEditAction = RowEditAction,
                InlineDeleteAction = RowDeleteAction,
                GridRefreshAction = GridRefreshAction,
                GridExportAction = GridExportAction,
                NumRecords = loadData ? DataSourceTotal(options) : 0,
                GridExportTypes = exportTypes,
                Columns = new List<GridColumnModel>(),
                Rows = new List<GridRowModel>(),
                SortColumns = new List<GridSortModel>(),
                NoResultsText = String.IsNullOrEmpty(NoResultsText) ? "No Results." : NoResultsText,
                Hidden = !loadData
            };

            List<GridColumnModel> movableCols = new List<GridColumnModel>();

            // add hidden id column
            var idCol = new Column<TModel, TKey>(_idProperty)
            {
                ColumnType = ColumnType.AlwaysHidden
            };

            model.Columns.Add(new GridColumnModel()
            {
                ColumnName = idCol.ColumnName,
                DisplayName = idCol.ToString(),
                ColumnProperty = ExpressionHelper.GetExpressionText(_idProperty),
                Column = idCol,
                Visible = false,
                Customizable = false,
                Movable = false,
                Sortable = false,
                SortOrder = null,
                Key = true
            });

            // users cannot set every column to unsorted; check for that and revert to default sort if they do
            // (grid js passes a column name of "" when marking a column as unsorted)
            options.SortColumnInfo.RemoveAll(o => String.IsNullOrEmpty(o.ColumnName));

            foreach (var col in Columns)
            {
                if (col.CanDisplay == null || col.CanDisplay(user))
                {
                    var cmod = new GridColumnModel()
                    {
                        ColumnName = col.ColumnName,
                        DisplayName = col.ToString(),
                        ColumnProperty = ExpressionHelper.GetExpressionText(col.PropertyExpr),
                        Column = col,
                        Visible = !col.ColumnType.HasFlags(ColumnFlags.Hidden),
                        Customizable = !col.ColumnType.HasFlags(ColumnFlags.NotCustomizable),
                        Movable = !col.ColumnType.HasFlags(ColumnFlags.NotMovable),
                        Sortable = col.Sortable
                    };

                    var userSort = options.SortColumnInfo.FirstOrDefault(c => c.ColumnName == col.ColumnName);

                    if (userSort != null)
                    {
                        options.SortColumns.Add(col.GetGridSort(userSort.SortOrder));
                    }
                    else if (col.DefaultSort.HasValue && options.SortColumnInfo.Count == 0)
                    {
                        options.SortColumns.Add(col.GetGridSort());
                    }

                    var hasSort = options.SortColumns.FirstOrDefault(c => c.ColumnName == col.ColumnName);

                    if (hasSort != null)
                    {
                        cmod.SortOrder = hasSort.SortOrder;
                    }

                    if (cmod.Movable)
                    {
                        movableCols.Add(cmod);
                    }
                    else
                    {
                        model.Columns.Add(cmod);
                    }
                }
            }

            model.Columns.AddRange(movableCols);

            IEnumerable<TModel> data = loadData ? DataSourcePage(options) : new TModel[0];

            foreach (var row in data)
            {
                var rmod = new GridRowModel()
                {
                    Cells = new List<GridCellModel>()
                };

                if (CanEditRow != null)
                {
                    rmod.Editable = CanEditRow(row, user);
                }
                else
                {
                    rmod.Editable = true;
                }

                if (CanDeleteRow != null)
                {
                    rmod.Deletable = CanDeleteRow(row, user);
                }
                else
                {
                    rmod.Deletable = true;
                }

                foreach (var col in model.Columns)
                {
                    var cmod = new GridCellModel()
                    {
                        Column = col,
                        Row = row
                    };

                    var column = (IColumn<TModel>)col.Column;
                    object cval = column.GetValue(row);

                    if (column.ColumnType.HasFlags(ColumnFlags.Hidden) || !column.Editable)
                    {
                        cmod.Editable = false;
                    }
                    else if (column.CanEdit != null)
                    {
                        cmod.Editable = column.CanEdit(user, row, cval);
                    }
                    else
                    {
                        cmod.Editable = true;
                    }

                    var attr = column.PropertyInfo.GetCustomAttribute<DisplayFormatAttribute>();

                    // for cmod.Value, we need to apply custom formatters and if those aren't found,
                    // then use our default formatters for pretty-printing
                    if (column.FormatCallback != null)
                    {
                        if (!column.AllowRawHtml)
                        {
                            cmod.Value = new MvcHtmlString(HttpUtility.HtmlEncode(column.FormatCallback(row)));
                        }
                        else
                        {
                            cmod.Value = new MvcHtmlString(column.FormatCallback(row));
                        }
                    }
                    else if (column.FormatString != null)
                    {
                        if (!column.AllowRawHtml)
                        {
                            cmod.Value = new MvcHtmlString(HttpUtility.HtmlEncode(String.Format(column.FormatString, cval)));
                        }
                        else
                        {
                            cmod.Value = new MvcHtmlString(String.Format(column.FormatString, cval));
                        }
                    }
                    else if (attr != null)
                    {
                        if (cval == null && attr.NullDisplayText != null)
                        {
                            if (attr.HtmlEncode)
                            {
                                cmod.Value = new MvcHtmlString(HttpUtility.HtmlEncode(attr.NullDisplayText));
                            }
                            else
                            {
                                cmod.Value = new MvcHtmlString(attr.NullDisplayText);
                            }
                        }
                        else
                        {
                            if (attr.HtmlEncode)
                            {
                                cmod.Value = new MvcHtmlString(HttpUtility.HtmlEncode(String.Format(attr.DataFormatString, cval)));
                            }
                            else
                            {
                                cmod.Value = new MvcHtmlString(String.Format(attr.DataFormatString, cval));
                            }
                        }
                    }
                    else
                    {
                        // apply default formatters, for now that's just the raw value but eventually
                        // we'll want to make things prettier
                        cmod.Value = FormatForDisplay(cval, !column.AllowRawHtml, column.PropertyInfo);
                    }

                    // if we have a drilldown and cval is nonzero, then add a link
                    if (helper != null && column.DrillDownAction != null && column.DrillDownController != null
                        && (column.CanDrillDown == null || column.CanDrillDown(user, row, cval)))
                    {
                        string drillDownTabName = cmod.Value.ToString();

                        if (column.DrillDownTabName != null)
                        {
                            drillDownTabName = String.Format(column.DrillDownTabName, cmod.Value);
                        }

                        if (cval != null && !String.IsNullOrEmpty(cval.ToString()) && !cval.IsZero())
                        {
                            // helper.ActionLink unconditionally encodes our value, which is undesirable
                            // as such we build the link HTML manually
                            var urlHelper = new UrlHelper(helper.ViewContext.RequestContext, helper.RouteCollection);
                            var link = new TagBuilder("a");

                            link.MergeAttribute("target", "_blank");
                            link.MergeAttribute("data-drilldown-tabname", drillDownTabName);
                            link.MergeAttribute("href", urlHelper.Action(
                                column.DrillDownAction,
                                column.DrillDownController,
                                new { id = column.DrillDownIdFunc?.Invoke(row) ?? IdPropertyFunc(row).ToString() }));

                            link.AddCssClass("drilldown");
                            link.InnerHtml = cmod.Value.ToString();

                            cmod.Value = new MvcHtmlString(link.ToString());
                        }
                    }

                    // get our export value, which may be different
                    if (column.FormatCallbackForExport != null)
                    {
                        cmod.ExportValue = column.FormatCallbackForExport(row);
                    }
                    else if (column.FormatStringForExport != null)
                    {
                        cmod.ExportValue = String.Format(column.FormatStringForExport, cval);
                    }
                    else if (column.FormatCallback != null)
                    {
                        cmod.ExportValue = column.FormatCallback(row);
                    }
                    else if (column.FormatString != null)
                    {
                        cmod.ExportValue = String.Format(column.FormatString, cval);
                    }
                    else if (attr != null)
                    {
                        if (cval == null && attr.NullDisplayText != null)
                        {
                            cmod.ExportValue = attr.NullDisplayText;
                        }
                        else
                        {
                            cmod.ExportValue = String.Format(attr.DataFormatString, cval);
                        }
                    }
                    else
                    {
                        // this we DO want to keep raw though unlike cmod.Value
                        // do not apply formatters to this
                        if (cval == null)
                        {
                            cmod.ExportValue = String.Empty;
                        }
                        else
                        {
                            cmod.ExportValue = cval.ToString();
                        }
                    }

                    rmod.Cells.Add(cmod);
                }

                model.Rows.Add(rmod);
            }

            foreach (var sort in options.SortColumns)
            {
                model.SortColumns.Add(new GridSortModel()
                {
                    ColumnName = sort.ColumnName,
                    SortOrder = sort.SortOrder
                });
            }

            return model;
        }

        private MvcHtmlString FormatForDisplay(object val, bool htmlEncode, PropertyInfo valInfo)
        {
            if (val == null)
            {
                return MvcHtmlString.Empty;
            }

            var type = val.GetType();

            // booleans are converted to checkmarks or empty strings
            if (type == typeof(bool) && val is bool bVal)
            {
                if (bVal)
                {
                    return new MvcHtmlString("<span class=\"fa fa-check fa-lg\" aria-hidden=\"true\"></span><span class=\"sr-only\">Yes</span>");
                }
                else
                {
                    return new MvcHtmlString("<span class=\"sr-only\">No</span>");
                }
            }

            // nullable booleans get check (true), dash (false), and blank (null) for their three values
            if (type == typeof(bool?))
            {
                var nbVal = (bool?)val; // `val is bool? nbVal` was giving errors for some reason

                if (nbVal == true)
                {
                    return new MvcHtmlString("<span class=\"fa fa-check fa-lg\" aria-hidden=\"true\"></span><span class=\"sr-only\">Yes</span>");
                }
                else if (nbVal == false)
                {
                    return new MvcHtmlString("<span class=\"fa fa-minus fa-lg\" aria-hidden=\"true\"></span><span class=\"sr-only\">No</span>");
                }
                else
                {
                    return new MvcHtmlString("<span class=\"sr-only\">N/A</span>");
                }
            }

            // enumerables list their values
            if (type != typeof(string) && val is IEnumerable list)
            {
                List<string> vals = new List<string>();
                foreach (var obj in list)
                {
                    vals.Add(FormatForDisplay(obj, htmlEncode, valInfo).ToString());
                }

                return new MvcHtmlString(String.Join(", ", vals));
            }

            // enums get formatted for their display value (if one exists)
            if (val is Enum enVal)
            {
                return new MvcHtmlString(htmlEncode ? HttpUtility.HtmlEncode(enVal.ToDisplayString()) : enVal.ToDisplayString());
            }

            // DateTimes check if they are meant to be formatted as dates or times based on DataTypeAttribute
            if (type == typeof(DateTime) || type == typeof(DateTime?))
            {
                var dtAttr = valInfo.GetCustomAttribute<DataTypeAttribute>();
                var dt = (DateTime)val;

                if (dtAttr != null)
                {
                    switch (dtAttr.DataType)
                    {
                        case DataType.Date:
                            return new MvcHtmlString(dt.ToString("d"));
                        case DataType.Time:
                            return new MvcHtmlString(dt.ToString("t"));
                    }
                }

                return new MvcHtmlString(dt.ToString("g"));
            }

            // everything else just emit raw
            if (htmlEncode)
            {
                return new MvcHtmlString(HttpUtility.HtmlEncode(val.ToString()));
            }

            return new MvcHtmlString(val.ToString());
        }
    }
}

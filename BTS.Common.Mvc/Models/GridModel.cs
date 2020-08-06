using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Reflection;

namespace BTS.Common.Mvc.Grid.Models
{
    /// <summary>
    /// ViewModel for grids
    /// </summary>
    public class GridModel
    {
        /// <summary>
        /// Grid GUID
        /// </summary>
        public string GridId { get; set; }

        /// <summary>
        /// Grid Name
        /// </summary>
        public string GridName { get; set; }

        /// <summary>
        /// Total number of results
        /// </summary>
        public int NumRecords { get; set; }

        /// <summary>
        /// Whether or not to show pagination controls
        /// </summary>
        public bool ShowNavigation { get; set; }

        /// <summary>
        /// Current page (0-based)
        /// </summary>
        public int CurrentPageIndex { get; set; }

        /// <summary>
        /// Whether or not to show inline edit (pencil icon on each row)
        /// </summary>
        public bool ShowInlineEdit { get; set; }

        /// <summary>
        /// Action to call when saving an inline edit
        /// </summary>
        public string InlineEditAction { get; set; }

        /// <summary>
        /// Whether or not to show inline delete (x icon on each row)
        /// </summary>
        public bool ShowInlineDelete { get; set; }

        /// <summary>
        /// Action to call when performing inline delete
        /// </summary>
        public string InlineDeleteAction { get; set; }

        /// <summary>
        /// Action to call when refreshing grid
        /// </summary>
        public string GridRefreshAction { get; set; }

        /// <summary>
        /// Action to call when exporting grid
        /// </summary>
        public string GridExportAction { get; set; }

        /// <summary>
        /// Action to call when saving grid customizations
        /// </summary>
        public string GridCustomizeAction { get; set; }

        /// <summary>
        /// How rows can be selected
        /// </summary>
        public RowSelectType RowSelectType { get; set; }

        /// <summary>
        /// Allowable export types; if empty export is not allowed
        /// </summary>
        public List<ExportType> GridExportTypes { get; set; }

        /// <summary>
        /// Text to show if there are no results
        /// </summary>
        public string NoResultsText { get; set; }

        /// <summary>
        /// If true, grid UI is hidden (still on page to enable AJAX refresh, but no content)
        /// </summary>
        public bool Hidden { get; set; }

        /// <summary>
        /// Current page (1-based)
        /// </summary>
        public int CurrentPage
        {
            get
            {
                return CurrentPageIndex + 1;
            }
        }

        /// <summary>
        /// Allowed pagination sizes
        /// </summary>
        public List<int> AllowedPageSizes { get; set; }
        /// <summary>
        /// Current page size
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// What record to start at
        /// </summary>
        public int StartRecord
        {
            get
            {
                if (PageSize == 0)
                {
                    // not using paging
                    return 1;
                }

                return (CurrentPageIndex * PageSize) + 1;
            }
        }

        /// <summary>
        /// What record to end at
        /// </summary>
        public int EndRecord
        {
            get
            {
                if (PageSize == 0)
                {
                    // not using paging
                    return NumRecords;
                }

                return Math.Min(NumRecords, (CurrentPageIndex * PageSize) + PageSize);
            }
        }

        /// <summary>
        /// How many pages there are total
        /// </summary>
        public int NumPages
        {
            get
            {
                if (PageSize == 0)
                {
                    // not using paging
                    return 0;
                }

                return (NumRecords / PageSize) + (NumRecords % PageSize == 0 ? 0 : 1);
            }
        }

        /// <summary>
        /// What page should be the start of the pagination control list
        /// </summary>
        public int StartPageIndex
        {
            get
            {
                return (CurrentPageIndex / 10) * 10;
            }
        }

        /// <summary>
        /// What page should be the end of the pagination control list
        /// </summary>
        public int EndPageIndex
        {
            get
            {
                return StartPageIndex + 10;
            }
        }

        /// <summary>
        /// Columns in the grid
        /// </summary>
        public List<GridColumnModel> Columns { get; set; }
        /// <summary>
        /// Rows in the grid
        /// </summary>
        public List<GridRowModel> Rows { get; set; }
        /// <summary>
        /// How columns are sorted
        /// </summary>
        public List<GridSortModel> SortColumns { get; set; }
    }

    /// <summary>
    /// ViewModel for a grid column
    /// </summary>
    public class GridColumnModel
    {
        /// <summary>
        /// Internal name
        /// </summary>
        public string ColumnName { get; set; }
        /// <summary>
        /// Dsiplay name
        /// </summary>
        public string DisplayName { get; set; }
        /// <summary>
        /// Internal property
        /// </summary>
        public string ColumnProperty { get; set; }
        /// <summary>
        /// If column is rendered
        /// </summary>
        public bool Visible { get; set; }
        /// <summary>
        /// If column can be sorted
        /// </summary>
        public bool Sortable { get; set; }
        /// <summary>
        /// Current sort direction (null if not sorted)
        /// </summary>
        public SortOrder? SortOrder { get; set; }
        /// <summary>
        /// If column can be rearranged
        /// </summary>
        public bool Movable { get; set; }
        /// <summary>
        /// If column can be customized
        /// </summary>
        public bool Customizable { get; set; }
        /// <summary>
        /// If column is the key column
        /// </summary>
        public bool Key { get; set; }
        internal IColumn Column { get; set; }
    }

    /// <summary>
    /// View model for grid row
    /// </summary>
    public class GridRowModel
    {
        /// <summary>
        /// Cells in the row
        /// </summary>
        public List<GridCellModel> Cells { get; set; }
        /// <summary>
        /// If row is editable
        /// </summary>
        public bool Editable { get; set; }
        /// <summary>
        /// If row is deletable
        /// </summary>
        public bool Deletable { get; set; }
    }

    /// <summary>
    /// View model for grid cell
    /// </summary>
    public class GridCellModel
    {
        /// <summary>
        /// HTML value of cell
        /// </summary>
        public MvcHtmlString Value { get; set; }
        /// <summary>
        /// Export value of cell
        /// </summary>
        public string ExportValue { get; set; }
        /// <summary>
        /// If cell is editable
        /// </summary>
        public bool Editable { get; set; }
        /// <summary>
        /// Column this cell belongs to
        /// </summary>
        public GridColumnModel Column { get; set; }
        internal object Row { get; set; }
    }

    /// <summary>
    /// View model for a sort
    /// </summary>
    public class GridSortModel
    {
        /// <summary>
        /// Column we're sorting
        /// </summary>
        public string ColumnName { get; set; }
        /// <summary>
        /// Sort direction
        /// </summary>
        public SortOrder SortOrder { get; set; }
    }
}

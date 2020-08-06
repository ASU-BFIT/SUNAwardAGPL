using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Linq.Expressions;
using System.Web.Mvc;
using System.Diagnostics.CodeAnalysis;

namespace BTS.Common.Mvc.Grid.Infrastructure
{
    /// <summary>
    /// HTML form to filter grids
    /// </summary>
    /// <typeparam name="TModel"></typeparam>
    public class GridForm<TModel> : IDisposable
    {
        /// <summary>
        /// Helper used to render form fields
        /// </summary>
        public HtmlHelper<TModel> GridHelper { get; private set; }

        /// <summary>
        /// Whether or not to hide the submit/reset buttons
        /// </summary>
        public bool HideSubmitGroup { get; set; }
        private readonly TagBuilder _tag;
        private bool _disposed = false;

        /// <summary>
        /// Constructs a new GridForm, wrap in a using() block
        /// </summary>
        /// <param name="helper"></param>
        /// <param name="grid"></param>
        /// <param name="gridOptions"></param>
        /// <param name="horizontal"></param>
        [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "grid parameter is present for potential future use")]
        public GridForm(HtmlHelper helper, IGrid grid, IGridOptions gridOptions, bool horizontal)
        {
            // helper's type is likely not the grid's type, so construct a new HtmlHelper for use within the grid
            GridHelper = helper.As(((GridOptions<TModel>)gridOptions).Filter);
            _tag = new TagBuilder("div");
            _tag.AddCssClass("grid-filter");
            _tag.AddCssClass("row");
            _tag.MergeAttribute("data-grid", gridOptions.GridId);
            helper.ViewData[InputField.IS_HORIZ_FORM] = horizontal;
            helper.ViewData[InputField.RENDERED_COLS_SM] = 0;
            helper.ViewData[InputField.RENDERED_COLS_MD] = 0;

            if (horizontal)
            {
                _tag.AddCssClass("form-horizontal");
            }

            GridHelper.ViewContext.Writer.Write(_tag.ToString(TagRenderMode.StartTag));
        }

        /// <summary>
        /// Writes the submit group and end tag for the form
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                // Add in a SubmitGroup to handle refreshing the grid filters or resetting them
                if (!HideSubmitGroup)
                {
                    using (GridHelper.SubmitGroup())
                    {
                        GridHelper.ViewContext.Writer.Write(GridHelper.Button(ButtonType.Default, "Search", "grid-filter-search").ToHtmlString());
                        GridHelper.ViewContext.Writer.Write(GridHelper.Button(ButtonType.Reset, "Reset", "grid-filter-reset").ToHtmlString());
                    }
                }
                GridHelper.ViewContext.Writer.Write(_tag.ToString(TagRenderMode.EndTag));
                GC.SuppressFinalize(this);
            }
        }

        /// <summary>
        /// Renders a filter field for the grid
        /// </summary>
        /// <typeparam name="TProperty"></typeparam>
        /// <param name="field">Field to render</param>
        /// <returns></returns>
        public InputField<TModel, TProperty> FieldFor<TProperty>(Expression<Func<TModel, TProperty>> field)
        {
            return GridHelper.FieldFor(field);
        }
    }
}

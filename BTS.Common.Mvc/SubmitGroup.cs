using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace BTS.Common.Mvc
{
    /// <summary>
    /// Wraps a group of submit buttons, to be used in a using() block.
    /// </summary>
    public class SubmitGroup : IDisposable
    {
        private readonly HtmlHelper _helper;
        private bool _disposed = false;

        /// <summary>
        /// Constructs a new SubmitGroup. Call HtmlHelper.SubmitGroup() instead.
        /// </summary>
        /// <param name="helper"></param>
        public SubmitGroup(HtmlHelper helper)
        {
            _helper = helper;

            _helper.ViewData.TryGetValue(InputField.IS_HORIZ_FORM, out object temp);
            bool isHorizontalForm = (temp == null) ? false : (bool)temp;

            if (isHorizontalForm)
            {
                _helper.ViewContext.Writer.Write("<div class=\"submit-group offset-sm-2 col-sm-10\">");
            }
            else
            {
                _helper.ViewContext.Writer.Write("<div class=\"w-100\"></div><div class=\"submit-group\">");
            }
        }

        /// <summary>
        /// Finishes up a submit group
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _helper.ViewContext.Writer.Write("</div><br class=\"clearfix\">");

                GC.SuppressFinalize(this);
            }
        }
    }
}

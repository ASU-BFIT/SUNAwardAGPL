using System;
using System.Web.Mvc;

namespace BTS.Common.Mvc
{
    /// <summary>
    /// Encapulates a group of accordions. Only one accordion in the group can be expanded at a time.
    /// Expanding one collapses the rest. Call via a using() block. Accordion groups cannot be nested.
    /// </summary>
    public class AccordionGroup : IDisposable
    {
        private readonly TagBuilder _acc;
        private readonly HtmlHelper _helper;
        private bool _disposed = false;

        /// <summary>
        /// Begins a new accordion group
        /// </summary>
        /// <param name="helper"></param>
        public AccordionGroup(HtmlHelper helper)
        {
            string id = "accordiongroup-" + Guid.NewGuid().ToString();

            _acc = new TagBuilder("div");
            _acc.MergeAttribute("class", "accordion-group panel-group");
            _acc.MergeAttribute("id", id);
            _helper = helper;
            
            helper.ViewData["AccordionGroup"] = id;
            helper.ViewContext.Writer.Write(_acc.ToString(TagRenderMode.StartTag));
        }

        /// <summary>
        /// Closes the accordion group
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _helper.ViewData.Remove("AccordionGroup");
                _helper.ViewContext.Writer.Write(_acc.ToString(TagRenderMode.EndTag));
                GC.SuppressFinalize(this);
            }
        }
    }
}

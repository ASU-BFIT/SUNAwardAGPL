using System;
using System.Collections.Generic;
using System.Web.Mvc;

namespace BTS.Common.Mvc
{
    /// <summary>
    /// A collapsible HTML widget. If loading via AJAX, call as @Html.Accordion(). If specifying content,
    /// wrap in a using block.
    /// </summary>
    public class Accordion : IDisposable
    {
        private readonly Stack<TagBuilder> _tags;
        private readonly HtmlHelper _helper;
        private bool _disposed = false;

        /// <summary>
        /// Constructs a new Accordion
        /// </summary>
        /// <param name="helper">Helper to render the accordion to</param>
        /// <param name="title">Text to display as accordion title (always shown)</param>
        /// <param name="expanded">Initial expanded state, by default accordions are collapsed</param>
        /// <param name="ajaxTarget">If loading accordion contents via AJAX, this should be the Controller/Action string to load</param>
        public Accordion(HtmlHelper helper, string title, bool expanded = false, string ajaxTarget = null)
        {
            _tags = new Stack<TagBuilder>();
            _helper = helper;
            var writer = helper.ViewContext.Writer;
            string id = Guid.NewGuid().ToString();
            
            var tag = new TagBuilder("div");
            tag.AddCssClass("panel panel-default");
            writer.Write(tag.ToString(TagRenderMode.StartTag));
            _tags.Push(tag);

            tag = new TagBuilder("div");
            tag.AddCssClass("panel-heading");
            tag.MergeAttribute("id", "accordion-title-" + id);
            writer.Write(tag.ToString(TagRenderMode.StartTag));
            _tags.Push(tag);

            tag = new TagBuilder("h3");
            tag.AddCssClass("panel-title");
            writer.Write(tag.ToString(TagRenderMode.StartTag));
            _tags.Push(tag);

            tag = new TagBuilder("a");
            tag.MergeAttribute("role", "button");
            tag.MergeAttribute("href", "#accordion-content-" + id);
            tag.MergeAttribute("data-toggle", "collapse");
            tag.MergeAttribute("aria-expanded", expanded.ToString().ToLowerInvariant());
            if (helper.ViewData.ContainsKey("AccordionGroup"))
            {
                tag.MergeAttribute("data-parent", "#" + helper.ViewData["AccordionGroup"].ToString());
            }
            tag.SetInnerText(title);
            writer.Write(tag.ToString(TagRenderMode.Normal));

            tag = _tags.Pop(); // h3
            writer.Write(tag.ToString(TagRenderMode.EndTag));

            tag = _tags.Pop(); // div.panel-heading
            writer.Write(tag.ToString(TagRenderMode.EndTag));

            tag = new TagBuilder("div");
            tag.MergeAttribute("id", "accordion-content-" + id);
            tag.MergeAttribute("aria-labelledby", "accordion-title-" + id);
            tag.AddCssClass("panel-collapse collapse accordion");
            if (expanded)
            {
                tag.AddCssClass("in");
            }
            writer.Write(tag.ToString(TagRenderMode.StartTag));
            _tags.Push(tag);

            tag = new TagBuilder("div");
            tag.MergeAttribute("class", "panel-body");
            if (ajaxTarget != null)
            {
                tag.MergeAttribute("data-ajax", "pending");
                tag.MergeAttribute("data-ajax-target", ajaxTarget);
            }
            writer.Write(tag.ToString(TagRenderMode.StartTag));
            _tags.Push(tag);
        }

        /// <summary>
        /// Closes the accordion
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;

                while (_tags.Count > 0)
                {
                    var tag = _tags.Pop();
                    _helper.ViewContext.Writer.Write(tag.ToString(TagRenderMode.EndTag));
                }

                GC.SuppressFinalize(this);
            }
        }
    }
}

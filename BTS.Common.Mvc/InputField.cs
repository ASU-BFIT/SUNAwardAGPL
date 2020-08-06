using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using BTS.Common.Mvc.Grid.Models;

using BTS.Common.Web;

namespace BTS.Common.Mvc
{
    /// <summary>
    /// Represents a field in a form
    /// </summary>
    public abstract class InputField : IHtmlString
    {
        internal const string RENDERED_COLS_SM = "_inputField_RenderedCols_sm";
        internal const string RENDERED_COLS_MD = "_inputField_RenderedCols_md";
        internal const string IS_HORIZ_FORM = "_inputField_HorizontalForm";

        /// <summary>
        /// Generate an HTML string for this input field
        /// </summary>
        /// <returns></returns>
        public abstract string ToHtmlString();

        internal abstract InputField WithHelper(HtmlHelper helper);
    }

    /// <summary>
    /// Represents a field in a form
    /// </summary>
    /// <typeparam name="TModel"></typeparam>
    /// <typeparam name="TProperty"></typeparam>
    public class InputField<TModel, TProperty> : InputField
    {
        /// <summary>
        /// How wide the field is (ignored if the form is horizontal)
        /// </summary>
        public FieldWidth Width { get; set; }

        /// <summary>
        /// What kind of field to render. Automatically detected based on TProperty,
        /// but can be set manually if the autodetection is wrong.
        /// </summary>
        public FieldType FieldType { get; set; }

        /// <summary>
        /// Expression representing the field to render and bind to on submit
        /// </summary>
        public Expression<Func<TModel, TProperty>> Expr { get; private set; }

        /// <summary>
        /// HtmlHelper used to get context data
        /// </summary>
        public HtmlHelper<TModel> Helper { get; private set; }

        /// <summary>
        /// If this field is a dropdown or multiselect, this determines what items are available
        /// </summary>
        public List<SelectListItem> Items { get; set; }

        /// <summary>
        /// Field label, may be null
        /// </summary>
        public string Label { get; set; }


        /// <summary>
        /// Field placeholder text, may be null. Only works on Text fields.
        /// </summary>
        public string Placeholder { get; set; }

        /// <summary>
        /// If true, the field is disabled. It is still submitted, but cannot be edited.
        /// Usually used in conjunction with chaining to enable/disable a field dynamically.
        /// </summary>
        public bool Disabled { get; set; }

        /// <summary>
        /// If true, the field is rendered as inline instead of in its own group.
        /// Useful when embedding fields inside of other elements (as opposed to forms),
        /// e.g. inline grid editing.
        /// </summary>
        public bool Inline { get; set; }

        /// <summary>
        /// If the field is filterable, this sets the threshold for when the filter box appears
        /// </summary>
        public int SearchThreshold { get; set; } = 20;

        /// <summary>
        /// Chains this field to another field using javascript
        /// </summary>
        public Chain Chain { get; set; }

        /// <summary>
        /// Maximum number of options that can be selected in a multiselect
        /// </summary>
        public int MaxOptions { get; set; }

        /// <summary>
        /// Help text shown below the field
        /// </summary>
        public string HelpText { get; set; }

        /// <summary>
        /// Security restriction for this field. If the permissions are not met, this restriction
        /// is applied.
        /// </summary>
        public SecurityRestriction Restriction { get; set; }

        /// <summary>
        /// Flags and Permissions can be used to determine how this field is rendered depending
        /// on user security. Either use these two or Roles, but not both.
        /// </summary>
        public SecurityFlags Flags { get; set; }

        /// <summary>
        /// Flags and Permissions can be used to determine how this field is rendered depending
        /// on user security. Either use these two or Roles, but not both.
        /// </summary>
        public Enum[] Permissions { get; set; }

        /// <summary>
        /// Roles can be used to determine how this field is rendered depending
        /// on user security. Either use this or Flags+Permissions, but not both.
        /// </summary>
        public string[] Roles { get; set; }

        private bool GridEditTemplate = false;

        /// <summary>
        /// Create a new input field. This should generally not be constructed directly,
        /// but rather use the HtmlHelper.FieldFor() extension method.
        /// </summary>
        /// <param name="expr"></param>
        /// <param name="helper"></param>
        public InputField(Expression<Func<TModel, TProperty>> expr, HtmlHelper<TModel> helper)
        {
            Expr = expr;
            Helper = helper;
            Width = FieldWidth.Normal;
            Items = null;
            Label = null;
            Disabled = false;
            Chain = null;

            // autodetect initial FieldType based on expr
            // if bool default is checkbox
            // if bool? default is a dropdown allowing true/false/null
            // if an enum, default is a dropdown populated with all enum values
            // else default is text

            var prop = expr.GetPropertyInfo();
            var propType = prop.PropertyType.DiscardNullable();
            bool isNullable = prop.PropertyType.IsNullable();

            if (prop.PropertyType == typeof(bool))
            {
                FieldType = FieldType.Checkbox;
            }
            else if (propType == typeof(bool))
            {
                FieldType = FieldType.DropDown;
                Items = new List<SelectListItem>()
                {
                    new SelectListItem { Text = "Both", Value = null },
                    new SelectListItem { Text = "Yes", Value = "true" },
                    new SelectListItem { Text = "No", Value = "false" }
                };
            }
            else if (propType.IsEnum)
            {
                FieldType = FieldType.DropDown;
                Items = new List<SelectListItem>();

                foreach (var value in Enum.GetNames(propType))
                {
                    var e = propType.GetField(value);

                    Items.Add(new SelectListItem() { Text = e.GetDisplayName(), Value = value });
                }

                Items.Sort(new Comparison<SelectListItem>((l, r) => l.Text.CompareTo(r.Text)));

                if (isNullable)
                {
                    Items.Insert(0, new SelectListItem());
                }
            }
            else if (propType.GetInterface(nameof(IList)) != null && propType.IsGenericType)
            {
                var genParam = propType.GetGenericArguments()[0];

                if (genParam.IsEnum)
                {
                    FieldType = FieldType.MultiSelect;
                    Items = new List<SelectListItem>();

                    foreach (var value in Enum.GetNames(genParam))
                    {
                        var e = genParam.GetField(value);

                        Items.Add(new SelectListItem() { Text = e.GetDisplayName(), Value = value });
                    }

                    Items.Sort(new Comparison<SelectListItem>((l, r) => l.Text.CompareTo(r.Text)));
                }
            }
            else if (propType == typeof(DateTime))
            {
                var dataType = (DataTypeAttribute)prop.GetCustomAttributes(typeof(DataTypeAttribute), false).SingleOrDefault();
                if (dataType == null || dataType.DataType == DataType.Date)
                {
                    FieldType = FieldType.DatePicker;
                }
                else if (dataType.DataType == DataType.DateTime)
                {
                    FieldType = FieldType.DateTimePicker;
                }
                else if (dataType.DataType == DataType.Time)
                {
                    FieldType = FieldType.TimePicker;
                }
                else
                {
                    FieldType = FieldType.Text;
                }
            }
            else
            {
                FieldType = FieldType.Text;
            }

            if (prop.GetCustomAttributes(typeof(DisplayAttribute), false).SingleOrDefault() is DisplayAttribute disp)
            {
                HelpText = disp.GetDescription();
            }

            if (HelpText == null && prop.GetCustomAttributes(typeof(DescriptionAttribute), false).SingleOrDefault() is DescriptionAttribute desc)
            {
                HelpText = desc.Description;
            }
        }

        /// <summary>
        /// Fluent method to set the width of an input field
        /// </summary>
        /// <param name="width"></param>
        /// <returns></returns>
        public InputField<TModel, TProperty> AsWideField(FieldWidth width = FieldWidth.Large)
        {
            Width = width;
            return this;
        }

        /// <summary>
        /// Fluent method to set the label for an input field
        /// </summary>
        /// <param name="label">Label to set</param>
        /// <returns></returns>
        public InputField<TModel, TProperty> WithLabel(string label)
        {
            Label = label;
            return this;
        }

        /// <summary>
        /// Fluent method to set the placeholder text for an input field
        /// </summary>
        /// <param name="placeholder">Placeholder text to set</param>
        /// <returns></returns>
        public InputField<TModel, TProperty> WithPlaceholder(string placeholder)
        {
            Placeholder = placeholder;
            return this;
        }

        /// <summary>
        /// Fluent method to set the help text for an input field
        /// </summary>
        /// <param name="helpText"></param>
        /// <returns></returns>
        public InputField<TModel, TProperty> WithHelpText(string helpText)
        {
            HelpText = helpText;
            return this;
        }

        /// <summary>
        /// Fluent method to denote this field is a text input
        /// </summary>
        /// <returns></returns>
        public InputField<TModel, TProperty> AsTextBox()
        {
            FieldType = FieldType.Text;
            return this;
        }


        /// <summary>
        /// Fluent method to denote this field is a password input
        /// </summary>
        /// <returns></returns>
        public InputField<TModel, TProperty> AsPassword()
        {
            FieldType = FieldType.Password;
            return this;
        }


        /// <summary>
        /// Fluent method to denote this field is a date input
        /// </summary>
        /// <returns></returns>
        public InputField<TModel, TProperty> AsDatePicker()
        {
            FieldType = FieldType.DatePicker;
            return this;
        }

        /// <summary>
        /// Fluent method to denote this field is a date+time input
        /// </summary>
        /// <returns></returns>
        public InputField<TModel, TProperty> AsDateTimePicker()
        {
            FieldType = FieldType.DateTimePicker;
            return this;
        }

        /// <summary>
        /// Fluent method to denote this field is a time input
        /// </summary>
        /// <returns></returns>
        public InputField<TModel, TProperty> AsTimePicker()
        {
            FieldType = FieldType.TimePicker;
            return this;
        }

        /// <summary>
        /// Fluent method to denote this field is a color input
        /// </summary>
        /// <returns></returns>
        public InputField<TModel, TProperty> AsColorPicker()
        {
            FieldType = FieldType.ColorPicker;
            return this;
        }

        /// <summary>
        /// Fluent method to denote this field is a dropdown
        /// </summary>
        /// <typeparam name="TDropDownItem"></typeparam>
        /// <param name="items">Items to show in the dropdown</param>
        /// <param name="value">Expression to determine the submitted value for each item</param>
        /// <param name="text">Expression to determine the display text for each item</param>
        /// <param name="group">Expression to determine if items are grouped together</param>
        /// <param name="addBlank">If true, adds a blank option at the top</param>
        /// <returns></returns>
        public InputField<TModel, TProperty> AsDropDown<TDropDownItem>(
            IEnumerable<TDropDownItem> items,
            Func<TDropDownItem, object> value,
            Func<TDropDownItem, object> text,
            Func<TDropDownItem, object> group = null,
            bool addBlank = false)
        {
            var list = new List<SelectListItem>();

            if (addBlank)
            {
                list.Add(new SelectListItem());
            }

            SelectListGroup prevGroup = null;
            foreach (var item in items)
            {
                if (group != null)
                {
                    object g = group(item);

                    if (g != null && (prevGroup == null || prevGroup.Name != g.ToString()))
                    {
                        prevGroup = new SelectListGroup() { Name = g.ToString() };
                    }
                    else if (g == null && prevGroup != null)
                    {
                        prevGroup = null;
                    }
                }

                list.Add(new SelectListItem()
                {
                    Value = value(item).ToString(),
                    Text = text(item).ToString(),
                    Group = prevGroup
                });
            }

            return AsDropDown(list);
        }

        /// <summary>
        /// Fluent method to denote this field is a dropdown
        /// </summary>
        /// <param name="items">Items to show in the dropdown</param>
        /// <param name="addBlank">If true, adds a blank option to the top</param>
        /// <returns></returns>
        public InputField<TModel, TProperty> AsDropDown(IEnumerable<SelectListItem> items, bool addBlank = false)
        {
            FieldType = FieldType.DropDown;
            Items = items.ToList();

            if (addBlank)
            {
                Items.Insert(0, new SelectListItem());
            }

            return this;
        }

        /// <summary>
        /// Fluent method to denote this field is a multiselect
        /// </summary>
        /// <typeparam name="TMultiSelectItem"></typeparam>
        /// <param name="items">Items to show in the multiselect</param>
        /// <param name="value">Expression to determine the submitted value for each item</param>
        /// <param name="text">Expression to determine the display text for each item</param>
        /// <param name="group">Expression to determine if items are grouped together</param>
        /// <param name="maxOptions">Maximum number of options that can be chosen</param>
        /// <returns></returns>
        public InputField<TModel, TProperty> AsMultiSelect<TMultiSelectItem>(
            IEnumerable<TMultiSelectItem> items,
            Func<TMultiSelectItem, object> value,
            Func<TMultiSelectItem, object> text,
            Func<TMultiSelectItem, object> group = null,
            int maxOptions = 0)
        {
            AsDropDown(items, value, text, group, addBlank: false);
            FieldType = FieldType.MultiSelect;
            MaxOptions = maxOptions;

            return this;
        }

        /// <summary>
        /// Fluent method to denote this field is a multiselect
        /// </summary>
        /// <param name="items">Items to show in the multiselect</param>
        /// <param name="maxOptions">Maximum number of options that can be chosen</param>
        /// <returns></returns>
        public InputField<TModel, TProperty> AsMultiSelect(IEnumerable<SelectListItem> items, int maxOptions = 0)
        {
            FieldType = FieldType.MultiSelect;
            Items = items.ToList();
            MaxOptions = maxOptions;

            return this;
        }

        /// <summary>
        /// Fluent method to denote this field is a checkbox
        /// </summary>
        /// <returns></returns>
        public InputField<TModel, TProperty> AsCheckBox()
        {
            FieldType = FieldType.Checkbox;
            return this;
        }

        /// <summary>
        /// Fluent method to denote this field is a group of radio buttons.
        /// This overload should only be used on fields that were already populated as dropdowns (such as enums)
        /// </summary>
        /// <returns></returns>
        public InputField<TModel, TProperty> AsRadioButtonGroup()
        {
            FieldType = FieldType.RadioButtonGroup;
            return this;
        }

        /// <summary>
        /// Fluent method to denote this field is a group of radio buttons
        /// </summary>
        /// <param name="items">Radio buttons to render</param>
        /// <returns></returns>
        public InputField<TModel, TProperty> AsRadioButtonGroup(IEnumerable<SelectListItem> items)
        {
            FieldType = FieldType.RadioButtonGroup;
            Items = items.ToList();
            return this;
        }

        /// <summary>
        /// Fluent method to denote this field is a toggle switch
        /// </summary>
        /// <returns></returns>
        public InputField<TModel, TProperty> AsToggleSwitch()
        {
            FieldType = FieldType.ToggleSwitch;
            return this;
        }

        /// <summary>
        /// Fluent method to denote this field is an email address
        /// </summary>
        /// <returns></returns>
        public InputField<TModel, TProperty> AsEmailAddress()
        {
            FieldType = FieldType.EmailAddress;
            return this;
        }

        /// <summary>
        /// Fluent method to denote this field is a telephone number
        /// </summary>
        /// <returns></returns>
        public InputField<TModel, TProperty> AsTelephoneNumber()
        {
            FieldType = FieldType.TelephoneNumber;
            return this;
        }

        /// <summary>
        /// Fluent method to denote this field is a number
        /// </summary>
        /// <returns></returns>
        public InputField<TModel, TProperty> AsNumber()
        {
            FieldType = FieldType.Number;
            return this;
        }

        /// <summary>
        /// Fluent method to denote this field is a url
        /// </summary>
        /// <returns></returns>
        public InputField<TModel, TProperty> AsUrl()
        {
            FieldType = FieldType.Url;
            return this;
        }

        /// <summary>
        /// Fluent method to set the field as read only (not editable but still submitted)
        /// </summary>
        /// <param name="readOnly"></param>
        /// <returns></returns>
        public InputField<TModel, TProperty> AsReadOnly(bool readOnly = true)
        {
            if (readOnly)
            {
                FieldType = FieldType.ReadOnly;
            }

            return this;
        }

        /// <summary>
        /// Fluent method to set the field as hidden
        /// </summary>
        /// <param name="hidden"></param>
        /// <returns></returns>
        public InputField<TModel, TProperty> AsHidden(bool hidden = true)
        {
            if (hidden)
            {
                FieldType = FieldType.Hidden;
            }

            return this;
        }

        /// <summary>
        /// Fluent method to omit the field entirely
        /// </summary>
        /// <param name="omitted"></param>
        /// <returns></returns>
        public InputField<TModel, TProperty> AsOmitted(bool omitted = true)
        {
            if (omitted)
            {
                FieldType = FieldType.None;
            }

            return this;
        }

        /// <summary>
        /// Fluent method to disable the field (disabled fields are still submitted)
        /// </summary>
        /// <param name="disabled"></param>
        /// <returns></returns>
        public InputField<TModel, TProperty> AsDisabled(bool disabled = true)
        {
            if (disabled)
            {
                Disabled = true;
            }

            return this;
        }

        /// <summary>
        /// Fluent method to mark the field as inline, so it will not attempt to render
        /// a column around it.
        /// </summary>
        /// <param name="inline"></param>
        /// <returns></returns>
        public InputField<TModel, TProperty> AsInline(bool inline = true)
        {
            if (inline)
            {
                Inline = true;
            }

            return this;
        }

        /// <summary>
        /// Fluent method to mark a dropdown or multiselect as searchable
        /// </summary>
        /// <param name="searchable"></param>
        /// <returns></returns>
        public InputField<TModel, TProperty> AsSearchable(bool searchable = true)
        {
            SearchThreshold = searchable ? 1 : 0;
            return this;
        }

        /// <summary>
        /// Fluent method to mark a dropdown or multiselect as searchable as
        /// long it has threshold or more items in it.
        /// </summary>
        /// <param name="threshold"></param>
        /// <returns></returns>
        public InputField<TModel, TProperty> AsSearchable(int threshold)
        {
            SearchThreshold = threshold;
            return this;
        }

        /// <summary>
        /// Fluent method to set the security of a field. If the security is not met,
        /// the field is omitted entirely.
        /// </summary>
        /// <param name="flags"></param>
        /// <param name="permissions"></param>
        /// <returns></returns>
        public InputField<TModel, TProperty> WithSecurity(SecurityFlags flags, params Enum[] permissions)
        {
            return WithSecurity(SecurityRestriction.Omit, flags, permissions);
        }

        /// <summary>
        /// Fluent method to set the security of a field with the specified restriction should
        /// security not be met.
        /// </summary>
        /// <param name="restriction"></param>
        /// <param name="flags"></param>
        /// <param name="permissions"></param>
        /// <returns></returns>
        public InputField<TModel, TProperty> WithSecurity(SecurityRestriction restriction, SecurityFlags flags, params Enum[] permissions)
        {
            Restriction = restriction;
            Flags = flags;
            Permissions = permissions;
            return this;
        }

        /// <summary>
        /// Fluent method to set the security of a field. If the security is not met,
        /// the field is omitted entirely.
        /// </summary>
        /// <param name="roles"></param>
        /// <returns></returns>
        public InputField<TModel, TProperty> WithSecurity(params string[] roles)
        {
            return WithSecurity(SecurityRestriction.Omit, roles);
        }

        /// <summary>
        /// Fluent method to set the security of a field with the specified restriction should
        /// security not be met.
        /// </summary>
        /// <param name="restriction"></param>
        /// <param name="roles"></param>
        /// <returns></returns>
        public InputField<TModel, TProperty> WithSecurity(SecurityRestriction restriction, params string[] roles)
        {
            Restriction = restriction;
            Roles = roles;
            return this;
        }

        /// <summary>
        /// Fluent method to chain this field to another field
        /// </summary>
        /// <typeparam name="TChild"></typeparam>
        /// <param name="child">Field to chain to</param>
        /// <param name="disableNext">If true, the chained field is Disabled whenever this one is blank</param>
        /// <param name="updateOnInit">If true, chain logic runs on form init</param>
        /// <returns></returns>
        public InputField<TModel, TProperty> ChainTo<TChild>(
            Expression<Func<TModel, TChild>> child,
            bool disableNext = true,
            bool updateOnInit = true)
        {
            Chain = new Chain()
            {
                Children = new List<string>() { Helper.NameFor(child).ToString() },
                Include = new List<string>(),
                DisableNext = disableNext,
                UpdateOnInit = updateOnInit
            };

            return this;
        }

#if DEBUG
        private const bool CHAIN_WARN_MISSING = true;
#else
        private const bool CHAIN_WARN_MISSING = false;
#endif

        /// <summary>
        /// Fluent method to chain this field to another field
        /// </summary>
        /// <typeparam name="TChild"></typeparam>
        /// <param name="action">Action to call via AJAX whenever this field is updated</param>
        /// <param name="child">Field to chain to</param>
        /// <param name="disableNext">If true, the chained field is Disabled whenever this one is blank</param>
        /// <param name="warnMissing">If true, a warning is logged to the console if the chain element isn't present in the AJAX response</param>
        /// <param name="nameSpace">Namespace attached to js events for easier filtering</param>
        /// <param name="includes">Additional fields to include in the AJAX request</param>
        /// <param name="updateOnInit">If true, chain logic runs on form init</param>
        /// <returns></returns>
        public InputField<TModel, TProperty> ChainTo<TChild>(
            string action,
            Expression<Func<TModel, TChild>> child,
            bool disableNext = true,
            bool warnMissing = CHAIN_WARN_MISSING,
            string nameSpace = null,
            List<IChainInclude<TModel>> includes = null,
            bool updateOnInit = true)
        {
            UrlHelper url = new UrlHelper(Helper.ViewContext.RequestContext, Helper.RouteCollection);
            Chain = new Chain()
            {
                Children = new List<string>() { Helper.NameFor(child).ToString() },
                Include = new List<string>(),
                DisableNext = disableNext,
                WarnMissing = warnMissing,
                UpdateOnInit = updateOnInit,
                Namespace = nameSpace,
                DataSource = url.Action(action)
            };

            if (includes != null)
            {
                foreach (var inc in includes)
                {
                    Chain.Include.Add(inc.GetName());
                }
            }

            return this;
        }

        /// <summary>
        /// Fluent method to chain this field to another field
        /// </summary>
        /// <typeparam name="TChild"></typeparam>
        /// <param name="action">Action to call via AJAX whenever this field is updated</param>
        /// <param name="controller">Controller to call via AJAX whenever this field is updated</param>
        /// <param name="child">Field to chain to</param>
        /// <param name="disableNext">If true, the chained field is Disabled whenever this one is blank</param>
        /// <param name="warnMissing">If true, a warning is logged to the console if the chain element isn't present in the AJAX response</param>
        /// <param name="nameSpace">Namespace attached to js events for easier filtering</param>
        /// <param name="includes">Additional fields to include in the AJAX request</param>
        /// <param name="updateOnInit">If true, chain logic runs on form init</param>
        /// <returns></returns>
        public InputField<TModel, TProperty> ChainTo<TChild>(
            string action,
            string controller,
            Expression<Func<TModel, TChild>> child,
            bool disableNext = true,
            bool warnMissing = false,
            string nameSpace = null,
            List<IChainInclude<TModel>> includes = null,
            bool updateOnInit = true)
        {
            UrlHelper url = new UrlHelper(Helper.ViewContext.RequestContext, Helper.RouteCollection);
            Chain = new Chain()
            {
                Children = new List<string>() { Helper.NameFor(child).ToString() },
                Include = new List<string>(),
                DisableNext = disableNext,
                WarnMissing = warnMissing,
                Namespace = nameSpace,
                UpdateOnInit = updateOnInit,
                DataSource = url.Action(action, controller)
            };

            if (includes != null)
            {
                foreach (var inc in includes)
                {
                    Chain.Include.Add(inc.GetName());
                }
            }

            return this;
        }

        /// <summary>
        /// Indicates that this field is being rendered inside of a grid
        /// </summary>
        /// <returns></returns>
        public InputField<TModel, TProperty> ForGrid()
        {
            GridEditTemplate = true;
            Inline = true;

            return this;
        }

        /// <summary>
        /// Tell this InputField to use the specified HtmlHelper when rendering. This is useful when generating
        /// an input field "template" that can be used on multiple objects (passing null as the helper in the constructor)
        /// </summary>
        /// <param name="helper"></param>
        /// <returns></returns>
        internal override InputField WithHelper(HtmlHelper helper)
        {
            Helper = (HtmlHelper<TModel>)helper;

            return this;
        }

        private readonly int[] mdWidths = new int[] { 3, 4, 6, 12 };
        private readonly int[] smWidths = new int[] { 4, 6, 12, 12 };

        /// <summary>
        /// Renders the field to a string
        /// </summary>
        /// <returns></returns>
        public override string ToHtmlString()
        {
            if ((Roles?.Length > 0 && !Helper.GetUser().IsAllowed(Roles))
                || (Permissions?.Length > 0 && !Helper.GetUser().IsAllowed(Flags, Permissions)))
            {
                switch (Restriction)
                {
                    case SecurityRestriction.Omit:
                        return String.Empty;
                    case SecurityRestriction.Hide:
                        AsHidden(true);
                        break;
                    case SecurityRestriction.Disable:
                        AsDisabled(true);
                        break;
                    case SecurityRestriction.ReadOnly:
                        AsReadOnly(true);
                        break;
                }
            }

            if (FieldType == FieldType.None)
            {
                return String.Empty;
            }
            else if (FieldType == FieldType.Hidden)
            {
                return Helper.HiddenFor(Expr).ToHtmlString();
            }

            var html = new StringBuilder();

            Helper.ViewData.TryGetValue(IS_HORIZ_FORM, out object temp);
            bool isHorizontalForm = (temp == null) ? false : (bool)temp;

            if (!Inline && !isHorizontalForm)
            {
                if (!Helper.ViewData.ContainsKey(RENDERED_COLS_SM))
                {
                    Helper.ViewData[RENDERED_COLS_SM] = 0;
                    Helper.ViewData[RENDERED_COLS_MD] = 0;
                }

                int colsSm = (int)Helper.ViewData[RENDERED_COLS_SM] + smWidths[(int)Width];
                int colsMd = (int)Helper.ViewData[RENDERED_COLS_MD] + mdWidths[(int)Width];
                var classList = new List<string>();

                if (colsSm > 12)
                {
                    colsSm = smWidths[(int)Width];
                    classList.Add("d-sm-block");

                    if (colsMd <= 12)
                    {
                        classList.Add("d-md-none");
                    }
                }

                if (colsMd > 12)
                {
                    colsMd = mdWidths[(int)Width];
                    classList.Add("d-md-block");
                }

                if (classList.Count > 0)
                {
                    html.AppendFormat("<div class=\"w-100 d-none {0}\"></div>", String.Join(" ", classList));
                }

                html.AppendFormat("<div class=\"col-md-{0} col-sm-{1}\">", mdWidths[(int)Width], smWidths[(int)Width]);
                Helper.ViewData[RENDERED_COLS_SM] = colsSm;
                Helper.ViewData[RENDERED_COLS_MD] = colsMd;
            }

            // render inner contents based on type
            if (FieldType == FieldType.Checkbox || FieldType == FieldType.ToggleSwitch)
            {
                RenderCheckbox(html);
            }
            else
            {
                RenderFormGroup(html);
            }

            if (!Inline && !isHorizontalForm)
            {
                html.AppendLine("</div>"); // end col
            }

            return html.ToString();
        }

        // Renders a single input element in a .form-group with label
        // (some controls may render hidden backing inputs for a js control)
        private void RenderFormGroup(StringBuilder sb)
        {
            Helper.ViewData.TryGetValue(IS_HORIZ_FORM, out object temp);
            bool isHorizontalForm = (temp == null) ? false : (bool)temp;
            string baseClass = "form-control";

            // radio button groups use custom styling instead of rendering the browser control
            if (FieldType == FieldType.RadioButtonGroup)
            {
                baseClass = "custom-control-input";
            }

            var attrs = new Dictionary<string, object>()
            {
                { "class", baseClass }
            };

            if (Chain != null)
            {
                attrs = Chain.GetAttributes();
                attrs["class"] = baseClass;
            }

            if (Disabled)
            {
                attrs["class"] += " disabled";
                attrs["readonly"] = true;
            }

            if (HelpText != null)
            {
                attrs["aria-describedby"] = "help-" + Helper.IdFor(Expr);
            }

            if (isHorizontalForm)
            {
                sb.Append("<div class=\"form-group row\">");
            }
            else
            {
                sb.Append("<div class=\"form-group\">");
            }

            if (GridEditTemplate)
            {
                attrs["class"] += " grid-edit-template";
            }
            else
            {
                string classes = "control-label";
                if (isHorizontalForm)
                {
                    classes += " col-form-label col-sm-2";
                }

                if (Label != null)
                {
                    sb.Append(Helper.LabelFor(Expr, Label, new { @class = classes }));
                }
                else
                {
                    sb.Append(Helper.LabelFor(Expr, new { @class = classes }));
                }
            }

            if (isHorizontalForm)
            {
                sb.Append("<div class=\"col-sm-10\">");
            }

            string format = "{0}";
            switch (FieldType)
            {
                case FieldType.Text:
                    if (Placeholder != null)
                    {
                        attrs["placeholder"] = Placeholder;
                    }

                    sb.Append(Helper.TextBoxFor(Expr, format, attrs));
                    break;
                case FieldType.Password:
                    sb.Append(Helper.PasswordFor(Expr, attrs));
                    break;
                case FieldType.DropDown:
                case FieldType.MultiSelect:
                    attrs["class"] += " selectpicker";
                    if (SearchThreshold == 1 || Items.Count >= SearchThreshold)
                    {
                        attrs["data-live-search"] = true;
                    }

                    if (FieldType == FieldType.MultiSelect)
                    {
                        // display select all/deselect all boxes if we aren't in a Grid
                        // (if we are, the field is probably too narrow for it to show correctly)
                        if (!GridEditTemplate)
                        {
                            attrs["data-actions-box"] = true;
                        }

                        if (MaxOptions > 0)
                        {
                            attrs["data-max-options"] = MaxOptions;
                        }

                        sb.Append(Helper.ListBoxFor(Expr, Items, attrs));
                    }
                    else
                    {
                        sb.Append(Helper.DropDownListFor(Expr, Items, attrs));
                    }
                    break;
                case FieldType.TimePicker:
                    format = "{0:HH:mm}";
                    attrs["type"] = "time";
                    goto case FieldType.Text;
                case FieldType.DateTimePicker:
                    format = "{0:yyyy-MM-ddTHH:mm}";
                    attrs["type"] = "datetime-local";
                    goto case FieldType.Text;
                case FieldType.DatePicker:
                    format = "{0:yyyy-MM-dd}";
                    attrs["type"] = "date";
                    goto case FieldType.Text;
                case FieldType.ColorPicker:
                    attrs["type"] = "color";
                    goto case FieldType.Text;
                case FieldType.EmailAddress:
                    attrs["type"] = "email";
                    goto case FieldType.Text;
                case FieldType.Number:
                    attrs["type"] = "number";
                    goto case FieldType.Text;
                case FieldType.TelephoneNumber:
                    attrs["type"] = "tel";
                    goto case FieldType.Text;
                case FieldType.Url:
                    attrs["type"] = "url";
                    goto case FieldType.Text;
                case FieldType.ReadOnly:
                    sb.Append(String.Format("<p class=\"form-control-static readonly\">{0}</p>", Helper.DisplayFor(Expr)));
                    sb.Append(Helper.HiddenFor(Expr, attrs));
                    break;
                case FieldType.RadioButtonGroup:
                    for (var i = 0; i < Items.Count; i++)
                    {
                        var item = Items[i];
                        var itemId = Helper.IdFor(Expr) + "-" + i.ToString();
                        attrs["id"] = itemId;

                        sb.Append("<div class=\"custom-control custom-radio custom-control-inline\">");
                        sb.Append(Helper.RadioButtonFor(Expr, item.Value, attrs));
                        sb.AppendFormat("<label for=\"{0}\" class=\"custom-control-label\">{1}</label>", itemId, item.Text);
                        sb.Append("</div>"); // .custom-control.custom-radio.custom-control-inline
                    }

                    break;
            }

            if (HelpText != null)
            {
                sb.AppendFormat("<small id=\"help-{0}\" class=\"form-text text-muted\">{1}</small>", Helper.IdFor(Expr), Helper.Encode(HelpText));
            }

            sb.Append(Helper.ValidationMessageFor(Expr, null, new { @class = "form-text" }));

            if (isHorizontalForm)
            {
                sb.Append("</div>"); // .col-sm-10
            }

            sb.Append("</div>"); // .form-group
        }

        // Renders one checkbox
        private void RenderCheckbox(StringBuilder sb)
        {
            var attrs = new Dictionary<string, object>();
            Helper.ViewData.TryGetValue(IS_HORIZ_FORM, out object temp);
            bool isHorizontalForm = (temp == null) ? false : (bool)temp;

            if (Chain != null)
            {
                attrs = Chain.GetAttributes();
            }

            string label = Label != null
                            ? Helper.Encode(Label)
                            : Helper.DisplayNameFor(Expr).ToHtmlString();

            attrs["class"] = "custom-control-input";

            if (Disabled)
            {
                attrs["class"] += " disabled";
                attrs["disabled"] = true;
            }

            string checkId = "checkbox-" + Guid.NewGuid().ToString();
            attrs["id"] = checkId;

            if (isHorizontalForm)
            {
                sb.Append("<div class=\"offset-sm-2 col-sm-10\">");
            }

            sb.Append("<div class=\"form-group\">");

            if (FieldType == FieldType.ToggleSwitch)
            {
                sb.Append("<div class=\"custom-control custom-switch\">");
            }
            else
            {
                sb.Append("<div class=\"custom-control custom-checkbox\">");
            }

            if (typeof(TProperty) == typeof(bool))
            {
                sb.Append(Helper.CheckBoxFor(Expr as Expression<Func<TModel, bool>>, attrs));
            }
            else if (typeof(TProperty) == typeof(bool?))
            {
                sb.Append(Helper.CheckBoxFor(Expr as Expression<Func<TModel, bool?>>, attrs));
            }
            else
            {
                throw new InvalidOperationException("Cannot render a checkbox for a non-boolean property");
            }

            // the disabled attribute is only applied to the checkbox and not the backing hidden input; so if we are supposed
            // to disable this, make sure we also disable the backing input. Sadly there is no easy way of doing this
            if (Disabled)
            {
                sb.Replace("type=\"hidden\"", "type=\"hidden\" disabled=\"disabled\"");
            }

            sb.AppendFormat("<label class=\"custom-control-label\" for=\"{0}\">{1}</label>", checkId, label);

            sb.Append("</div>"); // .custom-control.custom-checkbox
            sb.Append("</div>"); // .form-group

            if (isHorizontalForm)
            {
                sb.Append("</div>"); // .offset-sm-2.col-sm-10
            }
        }
    }
}

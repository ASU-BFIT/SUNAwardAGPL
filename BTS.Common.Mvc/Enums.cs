using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace BTS.Common.Mvc
{
    /// <summary>
    /// Button styles
    /// </summary>
    public enum ButtonType
    {
        /// <summary>Standard button</summary>
        Default,
        /// <summary>AjaxForm submission button (styled per ButtonType.Primary, submits the form)</summary>
        Submit,
        /// <summary>AjaxForm reset button (styled per ButtonType.Link, prompts user, then resets all fields to their original states)</summary>
        Reset,
        /// <summary>AjaxForm submission button (styled per ButtonType.Danger, prompts user, then submits the form)</summary>
        Delete,
        /// <summary>Provides extra visual weight and identifies the primary action in a set of buttons</summary>
        Primary,
        /// <summary>Indicates a successful or positive action</summary>
        Success,
        /// <summary>Contextual button for informational alert messages</summary>
        Info,
        /// <summary>Indicates caution should be taken with this action</summary>
        Warning,
        /// <summary>Indicates a dangerous or potentially negative action</summary>
        Danger,
        /// <summary>Deemphasize a button by making it look like a link while maintaining button behavior</summary>
        Link
    }

    /// <summary>
    /// What ASU shared element to render
    /// </summary>
    public enum AsuSharedElement
    {
        /// <summary>
        /// Head (put in head element)
        /// </summary>
        Head = 0,
        /// <summary>
        /// GTM (put in head element)
        /// </summary>
        GoogleTagManager,
        /// <summary>
        /// Header (put at top of body)
        /// </summary>
        Header,
        /// <summary>
        /// Footer (put at bottom of body)
        /// </summary>
        Footer
    }

    /// <summary>
    /// How wide a field is, if not using horizontal forms
    /// </summary>
    public enum FieldWidth
    {
        /// <summary>
        /// 4-wide on desktop, 3-wide on tablet, stacked on phone
        /// </summary>
        Normal = 0,
        /// <summary>
        /// 3-wide on desktop, 2-wide on tablet, stacked on phone
        /// </summary>
        Modal = 1,
        /// <summary>
        /// 2-wide on desktop, stacked on tablet, stacked on phone
        /// </summary>
        Large = 2,
        /// <summary>
        /// always stacked (field takes full width of container)
        /// </summary>
        Max = 3
    }

    /// <summary>
    /// Field types supported by Html.FieldFor and Html.FieldForGrid
    /// (generally this is autodetected, but can be passed in manually if needed)
    /// </summary>
    public enum FieldType
    {
        /// <summary>Simple text box (type="text")</summary>
        Text = 0,
        /// <summary>Password box (type="password")</summary>
        Password,
        /// <summary>Checkbox (type="checkbox")</summary>
        Checkbox,
        /// <summary>Radio buttons (type="radio"), all on the same line. Should not have more than 3 buttons per RadioButtonGroup</summary>
        RadioButtonGroup,
        /// <summary>Filterable dropdown list (js widget backed by select)</summary>
        DropDown,
        /// <summary>Dropdown list allowing multiple selections (js widget backed by select)</summary>
        MultiSelect,
        /// <summary>Allows picking a year/month/day (type="date")</summary>
        DatePicker,
        /// <summary>Allows picking a time (type="time")</summary>
        TimePicker,
        /// <summary>Allows picking both a date and time (type="datetime-local")</summary>
        DateTimePicker,
        /// <summary>Allows picking a color (type="color")</summary>
        ColorPicker,
        /// <summary>Hidden field (type="hidden"), no label is rendered</summary>
        Hidden,
        /// <summary>Read-only field (plain text backed by a hidden field to show a fixed current value plus post the existing value on submit)</summary>
        ReadOnly,
        /// <summary>Does not render a field; no HTML is generated. Useful when one wants to completely remove a field (e.g. access control)</summary>
        None,
        /// <summary>Toggle switch (js widget backed by checkbox)</summary>
        ToggleSwitch,
        /// <summary>Text box for entering email addresses (type="email")</summary>
        EmailAddress,
        /// <summary>Text box for entering phone numbers (type="tel")</summary>
        TelephoneNumber,
        /// <summary>Text box for entering phone numbers (type="number")</summary>
        Number,
        /// <summary>Text box for entering URLs (type="url")</summary>
        Url
    }

    /// <summary>
    /// Restriction to place on a form field if the user does not have sufficient security
    /// </summary>
    public enum SecurityRestriction
    {
        /// <summary>Leave the field out of the form entirely. This is the default.</summary>
        Omit = 0,
        /// <summary>Leave the field in the form as a hidden input.</summary>
        Hide = 1,
        /// <summary>Render the field but in a disabled state.</summary>
        Disable = 2,
        /// <summary>Render the field as a text string and a hidden input.</summary>
        ReadOnly = 3
    }

    /// <summary>
    /// What to do when clicking on a link that adds a tab
    /// </summary>
    public enum AddTabBehavior
    {
        /// <summary>
        /// Always add a new tab
        /// </summary>
        AlwaysAdd = 0,
        /// <summary>
        /// If a tab already exists with the same params, switch to it instead of adding a duplicate
        /// </summary>
        SwitchExisting
    }

    /// <summary>
    /// How big the modal is
    /// </summary>
    public enum ModalSize
    {
        /// <summary>
        /// Small modal. Use for message boxes.
        /// </summary>
        [Display(Name = "small")]
        Small,
        /// <summary>
        /// Large modal. Use for modal forms.
        /// </summary>
        [Display(Name = "large")]
        Large
    }
}

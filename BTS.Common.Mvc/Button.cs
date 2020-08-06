using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

using BTS.Common.Web;

namespace BTS.Common.Mvc
{
    /// <summary>
    /// An HTML widget for a clickable button
    /// </summary>
    public class Button : IHtmlString
    {
        // public because this needs to be gettable/settable in extension methods
        /// <summary>
        /// TagBuilder for this button. It should usually never be necessary to access this directly.
        /// </summary>
        public TagBuilder Tag { get; set; }

        /// <summary>
        /// HtmlHelper for this button. It should usually never be necessary to access this directly.
        /// </summary>
        public HtmlHelper Helper { get; set; }

        /// <summary>
        /// What action to perform on the button should the user fail security checks
        /// </summary>
        public SecurityRestriction Restriction { get; set; }

        /// <summary>
        /// Security flags to check on the permissions, if not using role-based security
        /// </summary>
        public SecurityFlags Flags { get; set; }

        /// <summary>
        /// Permissions to check, if not using role-based security
        /// </summary>
        public Enum[] Permissions { get; set; }

        /// <summary>
        /// Roles to check, if using role-based security
        /// </summary>
        public string[] Roles { get; set; }

        /// <summary>
        /// Constructs a new button
        /// </summary>
        /// <param name="helper"></param>
        /// <param name="type"></param>
        /// <param name="buttonText"></param>
        /// <param name="classNames"></param>
        public Button(HtmlHelper helper, ButtonType type, string buttonText = null, string classNames = null)
        {
            Helper = helper;
            Tag = new TagBuilder("button");
            Tag.AddCssClass("btn");
            Tag.AddCssClass(GetButtonClass(type));

            if (classNames != null)
            {
                Tag.AddCssClass(classNames);
            }

            switch (type)
            {
                case ButtonType.Delete:
                    Tag.Attributes["data-prompt"] = "Are you sure you want to delete?";
                    Tag.Attributes["type"] = "submit";
                    buttonText = buttonText ?? "Delete";
                    break;
                case ButtonType.Submit:
                    Tag.Attributes["type"] = "submit";
                    buttonText = buttonText ?? "Submit";
                    break;
                case ButtonType.Reset:
                    Tag.Attributes["data-prompt"] = "Are you sure you want to reset?";
                    Tag.Attributes["type"] = "reset";
                    buttonText = buttonText ?? "Reset";
                    break;
                default:
                    Tag.Attributes["type"] = "button";
                    break;
            }

            Tag.SetInnerText(buttonText);
        }

        /// <summary>
        /// Set the HTML type to button, even if it would normally be a submit input element
        /// </summary>
        /// <returns></returns>
        public Button AsButton()
        {
            Tag.Attributes["type"] = "button";
            return this;
        }

        /// <summary>
        /// Set the HTML type to submit, even if it would normally be a button element
        /// </summary>
        /// <param name="name">Button name attribute, if wishing to submit the button with form data</param>
        /// <param name="value">Button value attribute, if wishing to submit the button with form data</param>
        /// <returns></returns>
        public Button AsSubmit(string name = null, string value = null)
        {
            Tag.Attributes["type"] = "submit";

            if (!String.IsNullOrEmpty(name))
            {
                Tag.Attributes["name"] = name;
                Tag.Attributes["value"] = value;
            }

            return this;
        }

        /// <summary>
        /// Set the HTML type to reset
        /// </summary>
        /// <returns></returns>
        public Button AsReset()
        {
            Tag.Attributes["type"] = "reset";
            return this;
        }

        /// <summary>
        /// Mark the button as disabled
        /// </summary>
        /// <param name="disabled">If true, the button is disabled</param>
        /// <returns></returns>
        public Button AsDisabled(bool disabled = true)
        {
            if (disabled)
            {
                Tag.Attributes["disabled"] = "disabled";
            }
            else if (Tag.Attributes.ContainsKey("disabled"))
            {
                Tag.Attributes.Remove("disabled");
            }

            return this;
        }

        /// <summary>
        /// Denotes that this button opens a modal window
        /// </summary>
        /// <param name="action"></param>
        /// <param name="controller"></param>
        /// <param name="include"></param>
        /// <param name="forceReload"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public Button AsModalButton(
            string action,
            string controller,
            IEnumerable<string> include = null,
            bool forceReload = false,
            ModalSize size = ModalSize.Large)
        {
            Tag.Attributes["data-modal"] = $"{controller}/{action}";
            Tag.Attributes["data-modal-size"] = size.ToDisplayString();

            if (forceReload)
            {
                Tag.Attributes["data-modal-reload"] = "true";
            }

            if (include != null)
            {
                Tag.Attributes["data-modal-include"] = String.Join(",", include);
            }

            return this;
        }

        /// <summary>
        /// Do not render the button if the user does not meet any of these security checks
        /// </summary>
        /// <param name="flags"></param>
        /// <param name="permissions"></param>
        /// <returns></returns>
        public Button WithSecurity(SecurityFlags flags, params Enum[] permissions)
        {
            return WithSecurity(SecurityRestriction.Omit, flags, permissions);
        }

        /// <summary>
        /// Apply the given restriction if the user does not meet any of these security checks
        /// </summary>
        /// <param name="restriction"></param>
        /// <param name="flags"></param>
        /// <param name="permissions"></param>
        /// <returns></returns>
        public Button WithSecurity(SecurityRestriction restriction, SecurityFlags flags, params Enum[] permissions)
        {
            Restriction = restriction;
            Flags = flags;
            Permissions = permissions;
            return this;
        }

        /// <summary>
        /// Do not render the button if the user does not belong to any of the given roles
        /// </summary>
        /// <param name="roles"></param>
        /// <returns></returns>
        public Button WithSecurity(params string[] roles)
        {
            return WithSecurity(SecurityRestriction.Omit, roles);
        }

        /// <summary>
        /// Apply the given restriction if the user does not belong to any of the given roles
        /// </summary>
        /// <param name="restriction"></param>
        /// <param name="roles"></param>
        /// <returns></returns>
        public Button WithSecurity(SecurityRestriction restriction, params string[] roles)
        {
            Restriction = restriction;
            Roles = roles;
            return this;
        }

        /// <summary>
        /// Renders the button to HTML
        /// </summary>
        /// <returns></returns>
        public string ToHtmlString()
        {
            if ((Roles?.Length > 0 && !Helper.GetUser().IsAllowed(Roles))
                || (Permissions?.Length > 0 && !Helper.GetUser().IsAllowed(Flags, Permissions)))
            {
                // Hide and ReadOnly don't make sense for buttons
                switch (Restriction)
                {
                    case SecurityRestriction.Omit:
                        return String.Empty;
                    case SecurityRestriction.Disable:
                        AsDisabled(true);
                        break;
                    case SecurityRestriction.Hide:
                    case SecurityRestriction.ReadOnly:
                        throw new InvalidOperationException("Invalid SecurityRestriction on Button -- must be Omit or Disable");
                }
            }

            return Tag.ToString(TagRenderMode.Normal);
        }

        /// <summary>
        /// Get the CSS class for the button
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string GetButtonClass(ButtonType type)
        {
            switch (type)
            {
                case ButtonType.Danger:
                case ButtonType.Delete:
                    return "btn-danger";
                case ButtonType.Primary:
                case ButtonType.Submit:
                    return "btn-primary";
                case ButtonType.Info:
                    return "btn-info";
                case ButtonType.Warning:
                    return "btn-warning";
                case ButtonType.Success:
                    return "btn-success";
                case ButtonType.Link:
                case ButtonType.Reset:
                    return "btn-link";
                default:
                    return "btn-default";
            }
        }
    }
}

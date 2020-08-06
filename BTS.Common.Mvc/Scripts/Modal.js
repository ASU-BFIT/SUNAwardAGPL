/* global BTS */

$(function () {
    "use strict";

    function setupModalFromData() {
        var $this = $(this),
            modalData = {};

        if ($this.data("modal-widget")) {
            return; // already set up
        }

        modalData.action = $this.data("modal");
        modalData.ajaxOptions = null;
        modalData.size = $this.data("modal-size") || "small";
        modalData.reload = $this.data("modal-reload");
        modalData.include = $this.data("modal-include") || [];

        if (typeof modalData.include === "string") {
            modalData.include = modalData.include.split(",");
        }

        if (modalData.size.toLowerCase() === "large") {
            modalData.size = BTS.Modal.large;
        } else {
            modalData.size = BTS.Modal.small;
        }

        modalData.listener = function () {
            var data = $(this).data("modal-widget"),
                form = $(this).closest("form"),
                include = {},
                i, name;

            for (i = 0; i < data.include.length; i++) {
                name = data.include[i];
                include[name] = form.find("[name=\"" + name + "\"]").val();
            }

            BTS.Modal.show(data.action, data.reload, include, data.ajaxOptions, data.size);
        };

        $this.on("click", modalData.listener);
        $this.data("modal-widget", modalData);
    }

    $("#modal").modal({
        show: false
    });

    $(document).ajaxReady(function () {
        $(".tab-pane.active [data-modal], #modal [data-modal]").each(setupModalFromData);
    });

    $("[data-modal]").each(setupModalFromData);
});

BTS.Modal.show = function (title, content, footer, ajaxOptions, size) {
    /// <signature>
    ///   <summary>Shows a modal dialog using passed-in content.</summary>
    ///   <param name="title" type="String">Modal title, cannot contain HTML.</param>
    ///   <param name="content" type="String">Modal content, can contain HTML.</param>
    ///   <param name="footer" type="String" optional="true">Modal footer, can contain HTML.</param>
    ///   <param name="size" type="String" optional="true">
    ///     Modal size class, pass in either BTS.Modal.small or BTS.Modal.large
    ///   </param>
    /// </signature>
    /// <signature>
    ///   <summary>Shows a modal dialog using content loaded via AJAX.</summary>
    ///   <param name="action" type="String">Controller/Action to load.</param>
    ///   <param name="reload" type="Boolean" optional="true">
    ///     If true, forces content to reload via a new AJAX call.<br />
    ///     If false, will re-use existing content if possible.<br />
    ///     Default false.
    ///   </param>
    ///   <param name="data" type="Object" optional="true">
    ///     Additional data to pass to AJAX request, e.g. query string data.
    ///   </param>
    ///   <param name="ajaxOptions" type="Object" optional="true">
    ///     Additional options to pass to the AJAX request.
    ///     See $.ajax documentation for more details.
    ///   </param>
    ///   <param name="size" type="String" optional="true">
    ///     Modal size class, pass in either BTS.Modal.small or BTS.Modal.large
    ///   </param>
    /// </signature>
    "use strict";
    var action = title,
        reload = content,
        data = footer,
        doAjax = (typeof content === "boolean" || typeof content === "undefined"),
        modalBase = $("#modal > .modal-dialog"),
        modalContent = modalBase.children(".modal-content"),
        modalCache = $("#modal > .modal-content-cache"),
        cacheItem,
        cacheKey;

    if (!doAjax) {
        size = ajaxOptions;
    }

    data = data || {};
    ajaxOptions = ajaxOptions || {};
    size = size || "";

    // undo custom sizing from previous modal and show ours
    if (modalBase.hasClass("modal-lg")) {
        modalContent.data("size", "modal-lg");
    } else if (modalBase.hasClass("modal-sm")) {
        modalContent.data("size", "modal-sm");
    }

    modalBase.removeClass("modal-lg modal-sm");

    if (size) {
        modalBase.addClass(size);
    }

    // if a modal is currently being shown, cache it so we can show the new one
    if (!modalBase.is(":hidden")) {
        cacheKey = modalContent.attr("data-cachekey");

        if (cacheKey) {
            // remove any existing cached entry for this modal
            modalCache.children('.modal-content[data-cachekey="' + cacheKey + '"]').remove();

            // detach instead of remove to keep any event handlers on it live for future re-attachment
            modalContent.detach();
            modalCache.append(modalContent);
        }
    }

    if (doAjax && !reload) {
        // try to load an existing modal from cache
        cacheItem = modalCache.children('.modal-content[data-cachekey="' + action + '"]');

        if (cacheItem.length) {
            // existing cache item was found, re-show it
            cacheItem.detach();
            modalBase.html(cacheItem);
            $("#modal").modal("show");
            return;
        }
    }

    if (doAjax) {
        // load modal contents from ajax request, it is assumed that the returned html will
        // be compliant with modal layout
        modalBase.html('<div class="modal-content">' + BTS.spinnerHtml + "</div>");
        modalContent = modalBase.children(".modal-content");
        $("#modal").modal("show");

        if (data) {
            ajaxOptions.data = data;
        }

        ajaxOptions.success = function (data) {
            modalContent.attr("data-cachekey", action).html(data);
            $.validator.unobtrusive.parse(modalContent);
            // highlight serverside validation errors
            modalContent.find(".input-validation-error").each(function () {
                $(this).closest(".form-group").addClass("has-error");
            });
        };

        ajaxOptions.url = BTS.controllerPath + "/" + action;

        $.ajax(ajaxOptions);
    } else {
        // we were passed in data to display
        // add in framework html and then add our other stuff to that
        modalBase.html(
            '<div class="modal-content">\
                <div class="modal-header">\
                    <h5 class="modal-title"></h5>\
                    <button type="button" class="close" data-dismiss="modal" aria-label="Close">\
                        <span aria-hidden="true" class="fas fa-times"></span>\
                    </button>\
                </div>\
                <div class="modal-body"></div>\
                <div class="modal-footer"></div>\
            </div>'
        );

        modalContent = modalBase.children(".modal-content");

        modalContent.find(".modal-title").text(title);
        modalContent.find(".modal-body").html(content);

        if (footer) {
            modalContent.find(".modal-footer").html(footer);
        } else {
            // hide the footer if we don't have one, this prevents an ugly line from appearing
            modalContent.find(".modal-footer").remove();
        }

        $("#modal").modal("show");
    }
};

BTS.Modal.close = function () {
    /// <summary>Closes the modal dialog.</summary>
    "use strict";

    $("#modal").modal("hide");
    // TODO: if a previous modal was being shown, closing this should re-show the previous one
};

$(document).on("hidden.bs.modal", function () {
    "use strict";
    var modalBase = $("#modal > .modal-dialog"),
        modalContent = modalBase.children(".modal-content"),
        modalCache = $("#modal > .modal-content-cache"),
        cacheKey = modalContent.attr("data-cachekey");

    if (cacheKey) {
        // remove any existing cached entry for this modal
        modalCache.children('.modal-content[data-cachekey="' + cacheKey + '"]').remove();

        // detach instead of remove to keep any event handlers on it live for future re-attachment
        modalContent.detach();
        modalCache.append(modalContent);
    }
});

/* global BTS */
/* eslint eqeqeq: ["error", "smart"] */

BTS.Grid.refresh = function (id) {
    /// <signature>
    ///   <summary>Reloads a grid, keeping current filters, page number, etc.</summary>
    ///   <param name="id" type="String">Grid ID to refresh</param>
    ///   <returns value="$.ajax()">
    ///     jQXHR object to attach additional functionality on refresh completion or to abort the refresh.
    ///   </returns>
    /// </signature>
    /// <signature>
    ///   <summary>Reloads a grid, keeping current filters, page number, etc.</summary>
    ///   <param name="grid" type="DOMElement">The grid container div to refresh</param>
    ///   <returns value="$.ajax()">
    ///     jQXHR object to attach additional functionality on refresh completion or to abort the refresh.
    ///   </returns>
    /// </signature>
    "use strict";
    var grid, data, oldHtml;

    if (typeof id === "string") {
        grid = $("#" + id);
    } else {
        grid = $(id);
    }

    if (grid.length !== 1) {
        throw new Error("Invalid input to BTS.Grid.refresh");
    }

    if (!grid.data("refresh-action")) {
        throw new Error("This grid cannot be refreshed");
    }

    // First, serialize any form data attached to the grid as filters if needed
    data = BTS.Grid.formData(grid, BTS.Grid._filterData(grid));

    // Second, replace grid contents with a spinner
    // save off current html in case we need to restore it (on error)
    oldHtml = grid.children().detach();
    grid.html(BTS.spinnerHtml);

    // Finally, send a POST request to the refresh action.
    return $.post(
        BTS.controllerPath + "/" + grid.data("refresh-action") + "/" + grid.attr("id"),
        data
    ).done(function (gridHtml) {
        var newGrid = $(gridHtml);

        // no longer need old data, and set our new data
        oldHtml.remove();
        grid.replaceWith(newGrid);
        newGrid.trigger($.Event("refresh.grid"));
    }).fail(function () {
        // set back our old stuff
        grid.empty().append(oldHtml);
    });
};

BTS.Grid._filterData = function (grid, refresh) {
    /// <summary>Internal function for getting/setting the file data saved with a grid.</summary>
    /// <param name="grid" type="jQuery">Grid container.</param>
    /// <param name="refresh" type="Boolean" optional="true">
    ///   If true, causes the filter data to update from live values instead of returning cached data.
    /// </param>
    /// <returns type="Array" elementType="Object">Grid filter data in serializeArray() format</returns>
    "use strict";
    var filterData;

    if (refresh || grid.data("grid-filter") == null) {
        filterData = grid.closest(".tab-pane, .modal-content, .body-content")
            .find('.grid-filter[data-grid="' + grid.attr("id") + '"] :input')
            .serializeArray();
        grid.data("grid-filter", filterData);
    } else {
        filterData = grid.data("grid-filter");
    }

    return filterData;
};

BTS.Grid._formData = function (id, filter) {
    /// <summary>Internal function for retrieving form data as an object. See public formData for overload information.</summary>
    "use strict";
    var grid, data, inputs, i;

    if (typeof id === "string") {
        grid = $("#" + id);
    } else {
        grid = $(id);
    }

    // enable our inputs
    inputs = grid.find("div.grid-tokens :input");
    inputs.prop("disabled", false);

    if (filter) {
        if ($.isArray(filter)) {
            // we need to make a copy of filter (as we modify it, we don't want to modify the original source)
            filter = $.extend(true, [], filter);

            // given an array in presumably serializeArray() format, so concat it with our own data
            // also append this data with the Filter key so it gets rolled up in a GridOptions<T> properly
            // keep __RequestVerificationToken as-is in order to support grids that aren't in tabs and therefore
            // need to inject the anti-forgery token manually into the filter form.
            for (i = 0; i < filter.length; i++) {
                if (filter[i].name !== "__RequestVerificationToken") {
                    filter[i].name = "Filter." + filter[i].name;
                }
            }

            data = filter.concat(inputs.serializeArray());
        } else {
            filter = $.extend(true, {}, filter);

            // given a js object, add our own fields on as attributes
            data = { Filter: filter };
            if (filter.__RequestVerificationToken) {
                data.__RequestVerificationToken = filter.__RequestVerificationToken;
                delete filter.__RequestVerificationToken;
            }

            inputs.each(function () {
                data[$(this).attr("name")] = $(this).val();
            });
        }
    } else {
        // no filter given, just get our own stuff
        data = inputs.serializeArray();
    }

    inputs.prop("disabled", true);

    return data;
};

BTS.Grid.formData = function (id, filter) {
    /// <signature>
    ///   <summary>Obtains form data for a grid in array format, to be passed into a grid ajax request</summary>
    ///   <param name="id" type="String">Grid ID to serialize</param>
    ///   <param name="filter" type="Array" elementType="Object" optional="true">
    ///     Additional form fields to pass as a filter, in the format returned by jQuery's serializeArray() function
    ///   </param>
    ///   <returns type="String">Serialized form data</returns>
    /// </signature>
    /// <signature>
    ///   <summary>Obtains form data for a grid in array format, to be passed into a grid ajax request</summary>
    ///   <param name="id" type="String">Grid ID to serialize</param>
    ///   <param name="filter" type="Object" optional="true">
    ///     Additional form fields to pass as a filter, as a javascript object (nesting is allowed)
    ///   </param>
    ///   <returns type="String">Serialized form data</returns>
    /// </signature>
    /// <signature>
    ///   <summary>Obtains form data for a grid in array format, to be passed into a grid ajax request</summary>
    ///   <param name="id" type="DOMElement">The grid container div to serialize</param>
    ///   <param name="filter" type="Array" elementType="Object" optional="true">
    ///     Additional form fields to pass as a filter, in the format returned by jQuery's serializeArray() function
    ///   </param>
    ///   <returns type="String">Serialized form data</returns>
    /// </signature>
    /// <signature>
    ///   <summary>Obtains form data for a grid in array format, to be passed into a grid ajax request</summary>
    ///   <param name="id" type="DOMElement">The grid container div to serialize</param>
    ///   <param name="filter" type="Object" optional="true">
    ///     Additional form fields to pass as a filter, as a javascript object (nesting is allowed)
    ///   </param>
    ///   <returns type="String">Serialized form data</returns>
    /// </signature>
    "use strict";
    
    return $.param(BTS.Grid._formData(id, filter));
};

BTS.Grid.selectedRows = function (id) {
    /// <signature>
    ///   <summary>Retrieves the row(s) selected in the grid.</summary>
    ///   <param name="id" type="String">Grid ID to retrieve selected rows for.</param>
    ///   <returns type="Array" elementType="Object">Grid data for the selected rows.</returns>
    /// </signature>
    /// <signature>
    ///   <summary>Retrieves the row(s) selected in the grid.</summary>
    ///   <param name="id" type="DOMElement">Grid container div to retrieve selected rows for.</param>
    ///   <returns type="Array" elementType="Object">Grid data for the selected rows.</returns>
    /// </signature>
    "use strict";
    var grid,
        rowselect,
        rows = [];

    if (typeof id === "string") {
        grid = $("#" + id);
    } else {
        grid = $(id);
    }

    rowselect = grid.data("rowselect");

    if (rowselect === "None") {
        throw new TypeError("Grid does not support row selection.");
    }

    grid.find("table.grid tr.table-active").each(function () {
        var row = {};

        $(this).find("td").each(function () {
            if (!$(this).data("property")) {
                return;
            }

            row[$(this).data("property")] = $(this).text().trim();
        });

        rows.push(row);
    });

    return rows;
};

$(document).on("click", "button.grid-action", function () {
    "use strict";
    var btn = $(this),
        row = btn.closest(".grid-row"),
        actionArea = btn.closest(".grid-action-cell"),
        grid = btn.closest("div.grid-container"),
        formData, id, key;

    // figure out what action we're doing
    // options are edit, delete, save, and cancel
    // clicking edit makes fields editable and shows the save/cancel buttons (they are NOT wrapped in a <form>)
    // clicking delete deletes the row (with confirmation), then fires a rowDeleted event
    // clicking save saves the changes to the row, then fires a rowEdited event
    // clicking cancel reverts the row to what it was before and hides the editing interface
    // (the rowDeleted event can be used for "undo" functionality, if desired, as the id is passed to the event)

    if (btn.hasClass("grid-action-edit")) {
        row.find("td.editable").each(function () {
            var $this = $(this);
            $this.find(".grid-cell-value").hide();
            $this.find(".grid-edit-template").show();
        });

        actionArea.data("origHtml", actionArea.html());
        actionArea.html(
            '<button type="button" class="close grid-action grid-action-cancel" aria-label="Cancel" title="Cancel">\
                <span class="fas fa-ban" aria-hidden="true"></span>\
            </button>\
            <button type="button" class="close grid-action grid-action-save" aria-label="Save" title="Save">\
                <span class="fas fa-check" aria-hidden="true"></span>\
            </button>'
        );
    } else if (btn.hasClass("grid-action-save")) {
        formData = row.find(":input").serializeArray();
        id = parseInt(row.find("td.key").text(), 10) || -1;
        key = grid.find("thead th.key").data("column");
        formData.push({ name: key, value: id });
        formData = BTS.Grid.formData(grid, formData);
        $.post(BTS.controllerPath + "/" + grid.data("edit-action"), formData).done(function (data) {
            if (data.success) {
                BTS.Grid.refresh(grid);
            } else {
                // TODO: show an error message or something? Not sure what to do here if save fails.
            }
        });
    } else if (btn.hasClass("grid-action-cancel")) {
        actionArea.html(actionArea.data("origHtml"));
        row.find("td.editable").each(function () {
            var $this = $(this);
            $this.find(".grid-edit-template").hide();
            $this.find(".grid-cell-value").show();
        });
    } else if (btn.hasClass("grid-action-delete")) {
        formData = {};
        id = parseInt(row.find("td.key").text(), 10) || -1;
        key = grid.find("thead th.key").data("column");
        formData[key] = id;
        formData = BTS.Grid.formData(grid, formData);
        $.post(BTS.controllerPath + "/" + grid.data("delete-action"), formData).done(function (data) {
            // it is expected that the delete action returns JSON {"success": true} on success.
            // don't refresh the grid if the delete failed
            if (data.success) {
                BTS.Grid.refresh(grid);
            }
        });
    }
});

$(document).on("click", "table.grid a.drilldown", function (event) {
    "use strict";
    // if we don't have tabs, just open the link normally
    if (!$(this).closest(".tab-pane").length) {
        return;
    }

    var link = $(this);
    // open drilldown links that lead in-site in a new tab
    if (link.attr("href").indexOf(BTS.controllerPath) === 0) {
        event.preventDefault();
        BTS.Tab.addTab(link.data("drilldown-tabname"), link.attr("href").substr(BTS.controllerPath.length + 1));
    }
});

$(document).on("click", ".grid-container .grid-nav a[data-page]", function (event) {
    "use strict";
    var grid = $(this).closest(".grid-container"),
        page = $(this).data("page"),
        action = grid.data("refresh-action"),
        formData, pageIndex, curPage, curHtml;

    pageIndex = grid.find('div.grid-tokens input[name="PageIndex"]');
    curPage = pageIndex.val();
    pageIndex.val(page);
    formData = BTS.Grid.formData(grid, BTS.Grid._filterData(grid));
    pageIndex.val(curPage);

    curHtml = grid.children().detach();
    grid.html(BTS.spinnerHtml);

    event.preventDefault();
    $.post(BTS.controllerPath + "/" + action, formData).done(function (data) {
        var newGrid = $(data);
        curHtml.remove();
        grid.replaceWith(newGrid);
        newGrid.trigger($.Event("refresh.grid"));
    }).fail(function () {
        grid.empty().append(curHtml);
    });
});

$(document).on("click", ".grid-export-container a[data-export]", function (event) {
    "use strict";
    var grid = $(this).closest(".grid-container"),
        exportType = $(this).data("export"),
        action = grid.data("export-action"),
        formData, form;

    formData = $.param(BTS.Grid._formData(grid, BTS.Grid._filterData(grid)), { returnArray: true });
    form = $("<form></form>").attr({
        method: "POST",
        action: BTS.controllerPath + "/" + action
    });

    $.each(formData, function (i, value) {
        $("<input>").attr({
            type: "hidden",
            name: value.name,
            value: value.value
        }).appendTo(form);
    });

    $("<input>").attr({
        type: "hidden",
        name: "exportType",
        value: exportType
    }).appendTo(form);

    grid.append(form);
    form.submit();
    form.remove();
});

$(document).on("keydown", ".grid-filter", function (evt) {
    /// <param name="evt" type="KeyboardEvent" />
    "use strict";
    if (evt.which === 13) {
        evt.preventDefault();
        $(this).find("button.grid-filter-search").click();
    }
});

$(document).on("click", ".grid-filter button.grid-filter-search", function (evt) {
    /// <param name="evt" type="Event" />
    "use strict";
    // can't call .valid() on the form itself since it may contain non-filter fields,
    // so validate each field individually
    var filterBox = $(this).closest(".grid-filter"),
        grid = $("#" + filterBox.data("grid"));

    evt.preventDefault();

    if (filterBox.find(":input").valid()) {
        // update grid data with our current form data, then refresh the grid
        // also reset back to page 1
        BTS.Grid._filterData(grid, true);
        grid.find('div.grid-tokens input[name="PageIndex"]').val('0');
        BTS.Grid.refresh(grid);
    }
});

$(document).on("click", ".grid-container table.grid tr", function () {
    "use strict";
    var row = $(this),
        grid = row.closest(".grid"),
        selectType = grid.data("rowselect"),
        wasSelected = row.hasClass("table-active"),
        event;

    if (selectType === "Single") {
        row.toggleClass("table-active");
        row.siblings().removeClass("table-active");
    } else if (selectType === "Multiple") {
        row.toggleClass("table-active");
        row.find(".grid-row-select input").prop("checked", row.hasClass("table-active"));
    }

    if (wasSelected) {
        event = $.Event("deselect.grid.row");
    } else {
        event = $.Event("select.grid.row");
    }

    row.trigger(event);
});

$(document).on("change", '.grid-container .grid-nav select[name="PageSize"]', function (event) {
    "use strict";
    var grid = $(this).closest(".grid-container"),
        size = $(this).val(),
        action = grid.data("refresh-action"),
        formData, pageSize, curSize, pageIndex, page, curPage, curHtml;

    pageSize = grid.find('div.grid-tokens input[name="PageSize"]');
    pageIndex = grid.find('div.grid-tokens input[name="PageIndex"]');
    curSize = pageSize.val();
    curPage = pageIndex.val();

    // calculate the page we should swap to so that we are showing the same data as before
    // (e.g. when showing page 2 of a 20-row-per-page grid, we should be showing page 3
    // if we switch down to 10 rows per page)
    page = Math.floor((curSize * curPage) / size);

    pageSize.val(size);
    pageIndex.val(page);
    formData = BTS.Grid.formData(grid, BTS.Grid._filterData(grid));
    pageSize.val(curSize);
    pageIndex.val(curPage);

    curHtml = grid.children().detach();
    grid.html(BTS.spinnerHtml);

    event.preventDefault();
    $.post(BTS.controllerPath + "/" + action, formData).done(function (data) {
        var newGrid = $(data);
        curHtml.remove();
        grid.replaceWith(newGrid);
        newGrid.trigger($.Event("refresh.grid"));
    }).fail(function () {
        grid.empty().append(curHtml);
    });
});

$(document).on("click", ".grid-container .grid-sortable", function () {
    // Remove all of our other sorts and apply a sort to the clicked column
    // Order goes Unsorted -> Ascending -> Descending -> Unsorted
    "use strict";
    var grid = $(this).closest(".grid-container"),
        action = grid.data("refresh-action"),
        formData, tokens, sortCol, sortOrder, curSort, curHtml;

    tokens = grid.find("div.grid-tokens");
    curSort = tokens.find('input[name^="SortColumnInfo"]').detach();
    sortCol = $('<input id="SortColumnInfo_0__ColumnName" name="SortColumnInfo[0].ColumnName" disabled="disabled" type="hidden" value="" />')
        .val($(this).data("column"));
    sortOrder = $('<input id="SortColumnInfo_0__SortOrder" name="SortColumnInfo[0].SortOrder" disabled="disabled" type="hidden" value="" />');

    if ($(this).hasClass("grid-sort-ascending")) {
        sortOrder.val("Descending");
    } else {
        sortOrder.val("Ascending");
    }
    tokens.append(sortCol, sortOrder);
    formData = BTS.Grid.formData(grid, BTS.Grid._filterData(grid));
    sortCol.remove();
    sortOrder.remove();
    tokens.append(curSort);

    curHtml = grid.children().detach();
    grid.html(BTS.spinnerHtml);

    $.post(BTS.controllerPath + "/" + action, formData).done(function (data) {
        var newGrid = $(data);
        curHtml.remove();
        grid.replaceWith(newGrid);
        newGrid.trigger($.Event("refresh.grid"));
    }).fail(function () {
        grid.empty().append(curHtml);
    });
});

BTS.Grid._setupValidation = function () {
    "use strict";
    var form, filters = $(this);

    if (filters.closest("form").length === 0) {
        form = $('<form></form>').insertBefore(filters);
        filters.detach();
        form.html(filters);
        $.validator.unobtrusive.parse(form);
    }
};

// Ensure that our grid filters are all within <form>s so that jQuery validation works
// by default they are rendered in <div>s to support being nested within AjaxForms,
// but such a wrapper is not a requirement for grids
// Do the same for the grid itself if it is editable and not wrapped in a <form> already.
$(document).ajaxReady(function () {
    "use strict";
    var container;
    // determine if we are in a Modal or Tab
    if ($("#modal").hasClass("in")) {
        container = $("#modal > .modal-dialog > .modal-content");
    } else {
        container = $(".tab-pane.active");
    }

    container.find(".grid-filter, table.grid").each(BTS.Grid._setupValidation);
});

// Not every application makes use of ajax tabs, so ensure that if we're using normal
// views instead of loading tabs in partials that grids work as expected there too.
$(function () {
    "use strict";
    $(document).find(".grid-filter, table.grid").each(BTS.Grid._setupValidation);
});

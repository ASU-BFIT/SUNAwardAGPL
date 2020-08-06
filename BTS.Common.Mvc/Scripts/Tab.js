/* global BTS */

BTS.Tab.show = function (id) {
    /// <signature>
    ///   <summary>Switches to the specified tab.</summary>
    ///   <param name="id" type="Number" integer="true">0-indexed tab id to switch to.</param>
    /// </signature>
    /// <signature>
    ///   <summary>Switches to the specified tab.</summary>
    ///   <param name="tab" type="HTMLElement">
    ///     The tab to switch to.
    ///     This should be the tab's &lt;a&gt; element in the tab bar, or a jQuery wrapper around it.
    ///   </param>
    ///   <returns type="Boolean">true if the tab was successfully switched to, false if it was not.</returns>
    /// </signature>
    "use strict";

    if (id instanceof Object) {
        // second overload, passed in element or jquery object, can just show the tab
        if ($(id).length === 0 || !$.contains(document, $(id)[0])) {
            return false;
        }

        $(id).tab("show");
        return true;
    } else {
        // first overload, given a number
        // for this we get the underlying tab corresponding to the passed-in id and then
        // show it with the second overload
        return BTS.Tab.show($('#tabBar li a[href="#tabsection-' + id + '"]'));
    }
};

BTS.Tab.addTab = function (name, action, data, refresh, ajaxOptions) {
    /// <signature>
    ///   <summary>Adds a new dynamic tab and switches focus to it.</summary>
    ///   <param name="name" type="String">Name to show in the tab bar.</param>
    ///   <param name="action" type="String">Controller/Action to load.</param>
    ///   <param name="data" type="Object" optional="true" mayBeNull="true">
    ///     Data to send to the action, will be put in the query string for a GET request or request body for a POST.
    ///   </param>
    ///   <param name="refresh" type="Boolean" optional="true">
    ///     If true, will always load the action in a new tab.
    ///     If false, will switch to an existing tab if possible instead of loading a new one.
    ///     Default is false.
    ///   </param>
    ///   <param name="ajaxOptions" type="Object" optional="true" value="$.ajaxSetup()">
    ///     Additional options to pass to the underlying $.ajax call.
    ///     Furthermore, a "data-guid" key may be specified to explicitly set the GUID of the new tab.
    ///     See jQuery's $.ajax() documentation for more details.
    ///   </param>
    ///   <returns type="HTMLElement">The element for the new tab, can be passed to show and closeTab.</returns>
    /// </signature>
    /* jshint eqnull:true */
    "use strict";
    var tab, guid = false;

    ajaxOptions = ajaxOptions || {};

    if (data) {
        ajaxOptions.data = data;
    }

    if (ajaxOptions["data-guid"] != null) {
        guid = ajaxOptions["data-guid"];
        ajaxOptions["data-guid"] = undefined;
    }

    if (!refresh) {
        // search for an existing tab
        tab = $('#tabBar li[data-dynamictab="' + action + '"');

        if (tab.length) {
            // found one, switch to it
            BTS.Tab.show(tab);
            return tab;
        }
    }

    // refresh is true or we didn't find an existing tab, make a new one
    tab = $("<a></a>")
        .attr("data-dynamictab", action)
        .attr("data-toggle", "tab")
        .attr("id", "tab-" + BTS.Tab._tabIndex)
        .attr("class", "nav-link")
        .attr("href", "#tabsection-" + BTS.Tab._tabIndex)
        .data("dynamictab-options", ajaxOptions)
        .attr("role", tab)
        .attr("data-ajax-url", BTS.controllerPath + "/" + action)
        .text(name)
        .append('<span class="fa fa-close tab-close" title="Close"></span>');

    if (guid !== false) {
        tab.data("guid", guid);
    }

    $('<li class="nav-item tab dynamic-tab"></li>').append(tab).insertBefore('#tabBar .ml-auto');
    BTS.Tab._tabIndex++;

    // finally, switch focus to the tab
    BTS.Tab.show(tab);
    return tab;
};

BTS.Tab.updateName = function (id, name) {
    /// <signature>
    ///   <summary>Updates the name of the current tab</summary>
    ///   <param name="name" type="String">New name</param>
    ///   <returns type="String">The previous name of the tab</returns>
    /// </signature>
    /// <signature>
    ///   <summary>Updates the name of the specified tab</summary>
    ///   <param name="id" type="Number" integer="true">0-indexed tab id to modify</param>
    ///   <param name="name" type="String">New name</param>
    ///   <returns type="String">The previous name of the tab</returns>
    /// </signature>
    /* jshint eqnull:true */
    "use strict";
    var tab, prevName;

    if (name == null) {
        name = id;
        tab = $("#tabBar .tab.active a");
    } else {
        tab = $("#tabBar .tab:nth-child(" + id + ") a");
    }

    // if we have a close button, keep it
    var close = tab.find("span.tab-close").detach();

    prevName = tab.text();
    tab.text(name);
    tab.append(close);
    return prevName;
};

$(function () {
    "use strict";

    // Keep track of how many tabs have been opened so we can generate unique ids for tab content sections
    BTS.Tab._tabIndex = $("#tabBar li").length;

    // This event is fired whenever a Bootstrap tab is clicked but before being shown
    // we hook into this in order to determine if the tab has been AJAXed in or not yet
    $(document).on("show.bs.tab", function (evt) {
        var contentId,
            contentArea,
            xhr,
            prevHash,
            tabPath,
            sharesController = false,
            tab = $(evt.target),
            href = tab.data("ajax-url"),
            options = tab.data("dynamictab-options"),
            guid = tab.data("guid");

        if (!options) {
            options = {};
        }

        // don't show an error modal for this request, we put the error content directly in the tab
        options.displayErrorModal = false;

        // get a unique id for this tab (not actually a uuid per se, but good enough for our purposes)
        // may replace with proper guid later
        guid = guid || Math.floor(Math.random() * 1000000000);
        tab.data("guid", guid);

        // update the hash to reflect the tab to be shown
        // (allows for copy/pasting URL bar to get a link to a specific tab)
        BTS._internal.suppressHashChange = true;
        prevHash = window.location.hash;
        tabPath = href.substr(BTS.controllerPath.length + 1).split("/");

        if (window.location.pathname.substr(BTS.controllerPath.length + 1).split("/")[0] === tabPath[0]) {
            tabPath.shift();
            sharesController = true;
        }

        tabPath = "/" + tabPath.join("/");

        if (tab.parent("li").hasClass("static-tab")) {
            window.location.hash = "#" + tabPath;
        } else if (window.btoa && (!options.method || options.method === "GET")) {
            window.location.hash = "#" + tabPath + "!" + btoa(JSON.stringify({
                n: tab.text(),
                g: guid,
                c: Number(sharesController),
                d: options.data
            }));
        }

        if (prevHash === window.location.hash) {
            // hash is the same, this won't fire a hashchange event so don't suppress any
            BTS._internal.suppressHashChange = false;
        }

        if (!tab.data("ajax") || (tab.data("ajax") !== "loading" && tab.data("ajax") !== "complete")) {
            tab.data("ajax", "loading");

            // generate a new place to put tab contents if one doesn't exist
            contentId = tab.attr("href").substr(1);

            contentArea = $("#" + contentId);

            if (contentArea.length === 0) {
                $("#tabContent .tab-pane.active").removeClass("active");
                contentArea = $('<div role="tabpanel" class="tab-pane active" id="' + contentId + '" aria-labelledby="' + tab.attr("id") + '"></div>');
                $("#tabContent").append(contentArea);
            }

            // add spinny thing so user knows we're doing stuff
            contentArea.html(BTS.spinnerHtml);

            xhr = $.ajax(href, options).done(function (data) {
                // data is the HTML from a PartialView, inject it into our tab contents
                // the ajaxReady event is fired when this function completes
                tab.data("ajax", "complete");
                tab.removeData("ajax-xhr");

                contentArea.html(data);
                $.validator.unobtrusive.parse(contentArea);
                // highlight serverside validation errors
                contentArea.find(".input-validation-error").each(function () {
                    $(this).closest(".form-group").addClass("has-error");
                });
            }).fail(function errorCb(xhr, textStatus) {
                /// <param name="xhr" value="$.ajax()" />
                /// <param name="textStatus" type="String" />
                if (textStatus === "abort") {
                    // aborted prematurely, do nothing
                    return;
                }

                if (xhr.status === 401) {
                    // this puts a bar on the top of the page, so we don't want to replace that with non-existent data
                    // as such, just remove the spinner (this also lets the user resubmit after re-authenticating)
                    contentArea.find(".spinner").remove();
                } else {
                    var errorBody = $(xhr.responseText);

                    contentArea.html(errorBody.find(".errorContent")[0].outerHTML);
                }
            });

            tab.data("ajax-xhr", xhr);
        }
    });

    // This event is fired whenever a button/link to add a new tab is clicked
    $(document).on("click", "button[data-addtab]", function (evt) {
        var btn = $(evt.target);

        if (btn.prop("disabled")) {
            // button is disabled, don't do anything with it
            return;
        }

        var refresh = (btn.data("addtab") === "always");

        BTS.Tab.addTab(btn.data("addtab-name"), btn.data("addtab-action"), null, refresh);
    });

    // This event is fired whenever the close "x" on a tab is clicked
    $("#tabBar").on("click", ".tab-close", function (evt) {
        var tab = $(evt.target).closest("a");
        var container = tab.closest("li");
        var content = $(tab.data("target"));
        var active = tab.hasClass("active");

        if (!container.hasClass("dynamic-tab")) {
            // not a dynamic tab
            return;
        }

        // fire off a tabClose event, this can be used for things like confirmation prompts
        // the event is namespaced to the tab's controller/action
        var tabClose = $.Event("tabClose." + tab.data("dynamictab"));
        tab.trigger(tabClose);

        if (tabClose.isDefaultPrevented()) {
            // don't remove the tab in this instance
            return;
        }

        if (tab.data("ajax") === "loading") {
            tab.data("ajax-xhr").abort();
        }

        tab.tab("dispose");
        container.remove();
        content.remove();

        if (active) {
            // TODO: keep track of tab click order to return focus to the previous tab in case this one is shown
            BTS.Tab.show(0);
        }
    });

    // Finally, load our first tab, or if a tab was specified in the URL fragment then load that one
    // The fragment can either be numeric (tab index, 0-based), a string beginning with / (find by path)
    // or a string beginning with ! (adds a new dynamic tab with the specified params)
    // loadTabFromHash() is also used whenever the hash changes for navigational purposes
    function loadTabFromHash() {
        var hash, data, action,
            found = false;

        if (BTS._internal.suppressHashChange) {
            BTS._internal.suppressHashChange = false;
            return;
        }

        if ($("#tabBar").length > 0) {
            if (window.location.hash && window.location.hash.length > 1) {
                hash = window.location.hash.substr(1);
                action = window.location.pathname.substr(BTS.controllerPath.length + 1).split("/")[0];

                if (hash[0] === "/") {
                    if (hash.indexOf("!") === -1) {
                        // search for this tab in our existing tab bar.
                        // note this can either be just an action (in which case we use same controller)
                        // or a controller/action pair. either way searching for it is the same :)
                        $("#tabBar li.static-tab a").each(function () {
                            var tab = $(this);

                            if (tab.attr("data-ajax-url").substr(-hash.length) === hash) {
                                found = BTS.Tab.show(tab);
                            }
                        });
                    } else {
                        // create a new dynamic tab based on the hash data
                        // hash data is meant to be a base64-encoded data structure used to permalink to a tab
                        // for security reasons this can only run GET requests, never POSTs
                        if (window.atob) {
                            try {
                                data = hash.split("!")[1];
                                hash = hash.split("!")[0].substr(1);
                                data = JSON.parse(atob(data));

                                $("#tabBar li.dynamic-tab a").each(function () {
                                    var tab = $(this);

                                    if (tab.data("guid") === data.g) {
                                        found = BTS.Tab.show(tab);
                                    }
                                });

                                if (data.c) {
                                    // sharing controller with parent page
                                    hash = action + "/" + hash;
                                }

                                if (!found) {
                                    BTS.Tab.addTab(data.n, hash, data.d, true, { "data-guid": data.g });
                                    found = true;
                                }
                            } catch (e) {
                                found = false;
                            }
                        } else {
                            // old browser that doesn't support native base64-decoding
                            // can put a fallback here if that support is needed but for now just don't handle it
                            // and load the first tab instead
                            found = false;
                        }
                    }
                } else {
                    found = BTS.Tab.show(hash);
                }
            }

            if (!found) {
                BTS.Tab.show(0);
            }
        }
    }
    loadTabFromHash();
    BTS._internal.suppressHashChange = true;
    window.onhashchange = loadTabFromHash;

    // Wire up the ability to refresh a tab
    $('button[data-refresh="tab"]').click(function () {
        var tab = $("li.tab a.active");
        tab.removeClass("active");
        tab.data("ajax", "refresh");
        tab.tab("show");
    });
});

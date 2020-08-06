/* eslint eqeqeq: ["error", "smart"] */
// Framework javascript required by shared modules

/// <var>Namespace for widgets and utility functions in BTS applications.</var>
var BTS = {
    /// <field>Utility functions for displaying/hiding modal dialogs.</field>
    Modal: {
        /// <field>Small modal</field>
        small: "modal-sm",
        /// <field>Large modal</field>
        large: "modal-lg"
    },
    /// <field>Utility functions for managing tabs.</field>
    Tab: {},
    /// <field>Utility functions for managing grids.</field>
    Grid: {},
    /// <field>Prefix for application-relative urls</field>
    controllerPath: "", // Note: proper value is set in Site.js
    /// <field>Prefix for application-relative urls</field>
    contentPath: "", // Note: proper value is set in Site.js
    /// <field>The unprefixed Controller/Action to go to the user's profile page</field>
    profilePath: "", // Note: proper value is set in Site.js
    /// <field>The unprefixed Controller/Action to go to the site's search page</field>
    searchPath: "", // Note: proper value is set in Site.js
    /// <field>The unprefixed Controller/Action to reauthenticate the user</field>
    reauthPath: "Session/Reauth", // Note: can be customized in Site.js if desired, but usually not necessary
    /// <field>HTML containing the loading spinner</field>
    spinnerHtml: '<div class="spinner"><div class="hexdots-loader">Loading...</div></div>',
    /// <field>Ajax token for POST requests, automatically added if $.ajax() is used</field>
    __requestVerificationToken: "",
    _internal: {}
};

//#region AJAX global handlers

BTS._internal.XMLHttpRequestWrapper = function () {
    /// <summary>
    /// Proxy around an XMLHttpRequest to allow modifying the jQuery ajax pipeline.
    /// IT IS NOT MEANT FOR USE BY CODE OUTSIDE JQUERY INTERNALS.
    /// This class fully tracks the XMLHttpRequest specification in terms of exposed properties and members.
    /// </summary>
    "use strict";
    var self = this,
        xhr = new XMLHttpRequest(),
        _status, _statusText,
        _events = {};

    // custom functionality that shouldn't be directly proxied
    Object.defineProperties(this, {
        status: {
            get: function () {
                return (_status != null) ? _status : xhr.status;
            },
            enumerable: true
        },
        statusText: {
            get: function () {
                return (_statusText != null) ? _statusText : xhr.statusText;
            },
            enumerable: true
        },
        // constants (no need to proxy these through)
        UNSENT: {
            value: 0,
            enumerable: true
        },
        OPENED: {
            value: 1,
            enumerable: true
        },
        HEADERS_RECEIVED: {
            value: 2,
            enumerable: true
        },
        LOADING: {
            value: 3,
            enumerable: true
        },
        DONE: {
            value: 4,
            enumerable: true
        },
        // custom properties (not part of XMLHttpRequest spec)
        displayErrorModal: {
            value: false,
            writable: true
        }
    });

    // readonly attributes
    $.each(["readyState", "responseURL", "response", "responseText", "upload"],
        function (i, v) {
            Object.defineProperty(self, v, {
                get: function () { return xhr[v]; },
                enumerable: true
            });
        });

    // read/write attributes
    $.each(["responseType", "timeout", "withCredentials"],
        function (i, v) {
            Object.defineProperty(self, v, {
                get: function () { return xhr[v]; },
                set: function (o) { xhr[v] = o; },
                enumerable: true
            });
        });

    // responseXML should only be defined in a document/window context
    if (typeof window !== "undefined") {
        Object.defineProperty(self, "responseXML", {
            get: function () { return xhr.responseXML; },
            enumerable: true
        });
    }

    // proxy methods, open is special otherwise we just pass everything through to xhr.
    this.open = function (method, url, async, username, password) {
        /// <signature>
        ///   <summary>Opens a new asynchronous request.</summary>
        ///   <param name="method" type="String">Request method (e.g. GET or POST)</param>
        ///   <param name="url" type="String">Request URL, including query string components</param>
        /// </signature>
        /// <signature>
        ///   <summary>
        ///     Opens a new request, with the ability to specify a username and password for basic auth.
        ///   </summary>
        ///   <param name="method" type="String">Request method (e.g. GET or POST)</param>
        ///   <param name="url" type="String">Request URL, including query string components</param>
        ///   <param name="async" type="Boolean">
        ///     Whether or not this request should be performed asynchronously.
        ///     <para>You should always set this to true.</para>
        ///   </param>
        ///   <param name="username" type="String" optional="true">
        ///     Username for basic auth. Sent in the clear unless using SSL/TLS.
        ///   </param>
        ///   <param name="password" type="String" optional="true">
        ///     Password for basic auth. Sent in the clear unless using SSL/TLS.
        ///   </param>
        /// </signature>
        if (arguments.length < 3) {
            // no async passed in, spec differentiates between async being omitted and a value of undefined
            // so emulate that distinction here
            xhr.open(method, url);
        } else {
            xhr.open(method, url, async, username, password);
        }

        // wire up events to chain off our object
        xhr.onreadystatechange = function () {
            if (this.readyState === this.DONE) {
                var responseJson = this.getResponseHeader("X-Responded-JSON");
                var antiForgeryToken = this.getResponseHeader("X-AntiForgeryToken");
                var contentArea = $(".tab-pane.active");

                if (!contentArea.length) {
                    // not using tabs
                    contentArea = $(".body-content");
                }

                if (xhr.status === 200) {
                    if (responseJson) {
                        responseJson = JSON.parse(responseJson);

                        // prompt user to reauth if we get a 401 response
                        // - we can't redirect since the ReturnUrl will lead to a partial view
                        // - we can't automatically reauth with AJAX due to lack of Access-Control-Allow-Origin header
                        //   on CAS endpoint
                        // - prompting the user to manually perform it allows minimal disruption into the workflow
                        if (responseJson.status === 401) {
                            contentArea.prepend(
                                '<div class="alert alert-warning alert-dismissable">\
                                    <button type="button" class="close" data-dismiss="alert" aria-label="Close">\
                                        <span aria-hidden="true" class="fa fa-close"></span>\
                                    </button>\
                                    <strong>Your session has expired.</strong>\
                                    Please <a href="' + BTS.controllerPath + '/' + BTS.reauthPath + '" target="_blank">\
                                    click here</a> to re-authenticate (opens in new tab), then try again.\
                                </div>');

                            // Override the default 200 OK status with 401 so jQuery sees it as a proper error
                            _status = 401;
                            _statusText = "Unauthorized";
                        }
                    }

                    if (antiForgeryToken) {
                        BTS.__requestVerificationToken = antiForgeryToken;
                    }
                } else if (self.displayErrorModal && xhr.status !== 401) {
                    // error happened, display modal error dialog (unless we were told to suppress it via prefilter)
                    var errorBody = $(xhr.responseText).find(".errorContent").get(0) || {},
                        className = $(errorBody).find(".stacktrace").length ? BTS.Modal.large : "";

                    BTS.Modal.show("An unknown error has occurred",
                        (errorBody.innerHTML || "No error details are available."),
                        '<button type="button" class="btn btn-primary" data-dismiss="modal">Close</button>',
                        className);
                }
            }

            if (typeof _events.readystatechange === "function") {
                var event;

                try {
                    event = new Event("readystatechange", { bubbles: false, cancelable: false });
                } catch (e) {
                    // Event constructor not supported (IE)
                    event = document.createEvent("Event");
                    event.initEvent("readystatechange", false, false);
                }

                _events.readystatechange.call(self, event);
            }
        };
    };

    function propagateEvent(e) {
        var event;

        /// <param name="e" type="ProgressEvent" />
        if (e.type === "readystatechange") {
            return;
        }

        try {
            event = new ProgressEvent(e.type, {
                bubbles: false,
                cancelable: false,
                lengthComputable: e.lengthComputable,
                loaded: e.loaded,
                total: e.total
            });
        } catch (e) {
            // ProgressEvent constructor not supported (so probably IE)
            event = document.createEvent("ProgressEvent");
            event.initProgressEvent(e.type, false, false, e.lengthComputable, e.loaded, e.total);
        }

        if (typeof _events[e.type] === "function") {
            _events[e.type].call(self, event);
        }
    }

    $.each(["loadstart", "progress", "abort", "load", "error", "timeout", "loadend", "readystatechange"],
        function (i, eventType) {
            xhr.addEventListener(eventType, propagateEvent);

            Object.defineProperty(self, "on" + eventType, {
                get: function () { return _events[eventType]; },
                set: function (v) { _events[eventType] = v; },
                enumerable: true
            });
        });

    this.setRequestHeader = function (name, value) {
        /// <summary>Sets a header to be sent along with the request.</summary>
        /// <param name="name" type="String">Header name</param>
        /// <param name="value" type="String">Header value</param>
        xhr.setRequestHeader(name, value);
    };

    this.send = function (body) {
        /// <signature>
        ///   <summary>Dispatches the request.</summary>
        ///   <param name="body" type="Document" optional="true">Request body</param>
        /// </signature>
        /// <signature>
        ///   <summary>Dispatches the request.</summary>
        ///   <param name="body" type="String" optional="true">Request body</param>
        /// </signature>
        xhr.send((body == null) ? null : body);
    };

    this.abort = function () {
        /// <summary>Aborts the current request.</summary>
        xhr.abort();
    };

    this.getResponseHeader = function (name) {
        /// <summary>Retrieves a header from the response.</summary>
        /// <param name="name" type="String">Header name</param>
        /// <returns type="String" mayBeNull="true" />
        return xhr.getResponseHeader(name);
    };

    this.getAllResponseHeaders = function () {
        /// <summary>Retrieve every response header.</summary>
        /// <returns type="String" />
        return xhr.getAllResponseHeaders();
    };

    this.overrideMimeType = function (mime) {
        /// <summary>
        ///   Sets the MIME type of the response to the passed value instead of the Content-Type response header.
        /// </summary>
        /// <param name="mime" type="String">MIME type to set, as per a Content-Type header.</summary>
        xhr.overrideMimeType(mime);
    };
};

$.ajaxSettings.xhr = function () {
    /// <summary>Returns an XMLHttpRequest instance transparently wrapped by XMLHttpRequestWrapper</summary>
    "use strict";
    return new BTS._internal.XMLHttpRequestWrapper();
};

$.ajaxPrefilter(function (options) {
    "use strict";
    if (options.url.indexOf(BTS.controllerPath) === 0) {
        options.xhrFields = {
            displayErrorModal: (options.displayErrorModal != null) ? Boolean(options.displayErrorModal) : true
        };

        // if this is a POST, add in our AntiForgeryToken as a field named __RequestVerificationToken
        if (options.type.toLowerCase() === "post" && BTS.__requestVerificationToken) {
            if (options.contentType.indexOf("application/x-www-form-urlencoded") === 0) {
                if (options.data) {
                    // trim existing verification tokens
                    options.data = options.data.replace(/__RequestVerificationToken=[^&]+/, "").replace(/^&|&$/, "").replace("&&", "&");

                    if (options.data) {
                        options.data += "&";
                    }
                }

                options.data += "__RequestVerificationToken=" + BTS.__requestVerificationToken;
            } else if (options.contentType.indexOf("application/json") === 0) {
                var json = JSON.parse(options.data);
                json.__RequestVerificationToken = BTS.__requestVerificationToken;
                options.data = JSON.stringify(json);
            }
        }
    }
});

// Catch generic ajaxCompletes fired from jQuery and trigger a namespaced ajaxReady event to make it easier for our code
// to only listen to events coming from a particular partial view. Note that we only fire this event if we were given
// an HTML response, as it is assumed any other response type is being handled specially from javascript and does not
// need an ajaxReady event in order to finish setting everything up.
$(document).ajaxComplete(function (event, xhr, ajaxOptions) {
    /// <param name="xhr" value="$.ajax()" />
    /// <param name="ajaxOptions" value="$.ajaxSetup()" />
    var url = xhr.getResponseHeader("X-Request-Url");
    var contentType = xhr.getResponseHeader("Content-type");

    // only fire ajaxReady events for responses with Content-type text/html and a 200 OK HTTP code.
    if (contentType === null || !/^text\/html/.test(contentType) || xhr.status !== 200) {
        return;
    }

    // strip out things from url; notably it should start with BTS.controllerPath
    if (url === null || url.indexOf(BTS.controllerPath + "/") !== 0) {
        return;
    }

    url = url.substr(BTS.controllerPath.length + 1);

    // sometimes the url has additional things at the end, our namespace should only ever be Controller/Action
    // regardless of additional params
    url = url.split("/", 3);
    if (url.length === 3) {
        url.pop();
    }
    url = url.join("/");

    // split into two events instead of a single ajaxReady.global.url so that global handlers are always
    // run before page-specific handlers
    $(document).trigger("ajaxReady.global", [xhr, ajaxOptions]);
    $(document).trigger("ajaxReady." + url, [xhr, ajaxOptions]);
});

// allow $(document).ajaxReady(namespace, handler) shortcut
$.fn.extend({
    ajaxReady: function (namespace, handler) {
        /// <signature>
        ///   <summary>
        ///     Registers a namespaced event handler to be called when a partial view has been loaded via AJAX.
        ///   </summary>
        ///   <param name="namespace" type="String">The Controller/Action of the partial view to handle</param>
        ///   <param name="handler" type="Function">
        ///     Event handler<br />
        ///     Function(Event event, jqXHR jqXHR, Object ajaxOptions)
        ///   </param>
        /// </signature>
        /// <signature>
        ///   <summary>
        ///     Registers a global event handler to be called when a partial view has been loaded via AJAX.
        ///   </summary>
        ///   <param name="handler" type="Function">
        ///     Event handler<br />
        ///     Function(Event event, jqXHR jqXHR, Object ajaxOptions)
        ///   </param>
        /// </signature>
        "use strict";
        var $this = $(this);

        if (handler) {
            $this.on("ajaxReady." + namespace, handler);
        } else if ($this.is("form[data-ajaxform]")) {
            $this.on("ajaxReady.ajaxform", namespace);
        } else {
            $this.on("ajaxReady.global", namespace);
        }
    }
});

//#endregion

//#region AJAX Forms

// update jQuery serialization to match the ASP.NET MVC model binder
BTS._internal.param = $.param;
$.extend({
    param: function (obj, traditional) {
        /// <signature>
        ///   <summary>
        ///     Create a serialized representation of an array, a plain object, or a jQuery object suitable for use
        ///     in a URL query string or Ajax request.
        ///     <para>
        ///       In case a jQuery object is passed, it should contain input elements with name/value properties.
        ///     </para>
        ///   </summary>
        ///   <param name="obj" type="Object">An array, plain object, or a jQuery object to serialize.</param>
        /// </signature>
        /// <signature>
        ///   <summary>
        ///     Create a serialized representation of an array, a plain object, or a jQuery object suitable for use
        ///     in a URL query string or Ajax request.
        ///     <para>
        ///       In case a jQuery object is passed, it should contain input elements with name/value properties.
        ///     </para>
        ///   </summary>
        ///   <param name="obj" type="Object">An array, plain object, or a jQuery object to serialize.</param>
        ///   <param name="traditional" type="Boolean">
        ///     A Boolean indicating whether to perform a traditional "shallow" serialization.
        ///   </param>
        /// </signature>
        /// <signature>
        ///   <summary>
        ///     Create a serialized representation of an array, a plain object, or a jQuery object suitable for use
        ///     in a URL query string or Ajax request.
        ///     <para>
        ///       In case a jQuery object is passed, it should contain input elements with name/value properties.
        ///     </para>
        ///   </summary>
        ///   <param name="obj" type="Object">An array, plain object, or a jQuery object to serialize.</param>
        ///   <param name="options" type="Object">
        ///     An object containing 0 or more of the following keys:
        ///     <para>traditional: A Boolean indicating whether to perform a traditional "shallow" serialization. Default false</para>
        ///     <para>returnArray: A Boolean indicating whether to return a string (false) or array (true). Default false</para>
        ///   </param>
        /// </signature>
        "use strict";
        var prefix,
            s = [],
            options;

        options = $.extend({}, {
            traditional: $.ajaxSettings && $.ajaxSettings.traditional,
            returnArray: false
        });
        if (typeof traditional === "object") {
            $.extend(options, traditional);
        } else if (typeof traditional === "boolean") {
            options.traditional = traditional;
        }

        function add(key, value) {
            // if value is a function, invoke it and return its value
            value = $.isFunction(value) ? value() : (value == null ? "" : value);

            if (options.returnArray) {
                s.push({ name: key, value: value });
            } else {
                s.push(encodeURIComponent(key) + "=" + encodeURIComponent(value));
            }
        }

        function buildParams(prefix, obj) {
            var name;

            if ($.isArray(obj)) {
                // serialize array item (include numeric index)
                $.each(obj, function (i, v) {
                    buildParams(prefix + "[" + i + "]", v);
                });
            } else if ($.type(obj) === "object") {
                // serialize object
                for (name in obj) {
                    buildParams(prefix + "." + name, obj[name]);
                }
            } else {
                // serialize scalar
                add(prefix, obj);
            }
        }

        if (options.traditional) {
            return BTS._internal.param(obj, options.traditional);
        }

        // otherwise use our custom version
        if ($.isArray(obj) || (obj.jquery && !$.isPlainObject(obj))) {
            // serialize the form elements
            $.each(obj, function () {
                add(this.name, this.value);
            });
        } else {
            // recursively build out our param string
            // include prototype here because default $.param does that too for some unknown reason
            for (prefix in obj) {
                buildParams(prefix, obj[prefix]);
            }
        }

        if (options.returnArray) {
            return s;
        }

        return s.join("&").replace(/%20/g, "+");
    }
});

$(document).on("submit", 'form[data-ajaxform]', function (event) {
    /// <param name="event" value="$.Event('submit')"></param>
    "use strict";
    event.preventDefault();

    var form = $(this),
        contentArea = form.closest(".tab-pane, .modal-content"),
        tab = $('#tabBar a[data-target="#' + contentArea.id + ']'),
        formData = form.serializeArray(),
        spinner = $(BTS.spinnerHtml);
    tab.data("ajax", "loading");

    // add in a spinner and disable all active form elements
    contentArea.prepend(spinner);
    form.find(":input:not(:disabled)").addClass("ajaxform-submit-disabled").prop("disabled", true);

    $.post(form.attr("action"), $.param(formData)).done(function (data, textStatus, xhr) {
        var contentType = xhr.getResponseHeader("Content-type");

        tab.data("ajax", "complete");

        if (contentType !== null && !/^text\/html/.test(contentType) && xhr.status === 200) {
            // not an HTML response, so fire an event. This event will let things handle ajaxforms
            // which return non-HTML data especially, and is triggered on the element instead of the document.
            form.find(".ajaxform-submit-disabled").prop("disabled", false);
            spinner.remove();
            form.trigger("ajaxReady.ajaxform", data);
            return;
        }

        // data is the HTML from a PartialView, inject it into our tab contents
        // jQuery fires off global ajaxComplete, which is handled elsewhere to also fire a namespaced ajaxReady event
        contentArea.html(data);
        $.validator.unobtrusive.parse(contentArea);
        // highlight serverside validation errors
        contentArea.find(".input-validation-error").each(function () {
            $(this).closest(".form-group").addClass("has-error");
        });
    }).fail(function (xhr, textStatus) {
        if (textStatus === "abort") {
            return;
        }

        // remove the spinner and reenable the form
        contentArea.find(".spinner").remove();
        form.find(".ajaxform-submit-disabled").removeClass("ajaxform-submit-disabled").prop("disabled", false);
    });
});

//#endregion

//#region Validation

BTS._internal.validator = {};
BTS._internal.validator.highlight = $.validator.defaults.highlight;
BTS._internal.validator.unhighlight = $.validator.defaults.unhighlight;
$.validator.setDefaults({
    highlight: function (element, errorClass, validClass) {
        "use strict";
        BTS._internal.validator.highlight(element, errorClass, validClass);
        $(element).closest(".form-group").addClass("has-error");
    },
    unhighlight: function (element, errorClass, validClass) {
        "use strict";
        BTS._internal.validator.unhighlight(element, errorClass, validClass);
        $(element).closest(".form-group").removeClass("has-error");
    }
});

//#endregion

//#region Prompts

$(document).ajaxReady(function () {
    "use strict";
    $("button[data-prompt]").off("click.prompt").on("click.prompt", function (event) {
        /// <param name="event" value="$.Event('click')"></param>
        if (!window.confirm($(this).data("prompt"))) {
            event.preventDefault();
            event.stopImmediatePropagation();
        }
    });
});

//#endregion

//#region Accordions

$(document).on("show.bs.collapse", ".accordion", function () {
    var content = $(this).find(".panel-body");

    if (!content.data("ajax") || content.data("ajax") === "loading" || content.data("ajax") === "complete") {
        return;
    }

    // we have an AJAX accordion, add a spinner and fire off our ajax request
    content.html(BTS.spinnerHtml);
    content.data("ajax", "loading");

    $.get(BTS.controllerPath + "/" + content.data("ajax-target")).done(function (data) {
        content.data("ajax", "complete");

        content.html(data);
        $.validator.unobtrusive.parse(content);
        // highlight serverside validation errors
        content.find(".input-validation-error").each(function () {
            $(this).closest(".form-group").addClass("has-error");
        });
    }).fail(function (xhr, textStatus) {
        /// <param name="xhr" value="$.ajax()" />
        /// <param name="textStatus" type="String" />
        if (textStatus === "abort") {
            return;
        }

        // revert to pending and close the accordion to allow them to try again
        // (closing accordion happens in the shown event because we can't hide it during show)
        content.data("ajax", "error");
    });
});

$(document).on("shown.bs.collapse", ".accordion", function () {
    var accordion = $(this),
        content = $(this).find(".panel-body");

    if (content.data("ajax") === "error") {
        content.empty();
        accordion.collapse("hide");
    }
});

//#endregion

//#region Selectpicker (dropdowns/multiselects)
$.fn.selectpicker.defaults = {
    selectedTextFormat: "count > 3",
    iconBase: "fa",
    tickIcon: "fa-check",
    template: {
        caret: '<span class="fa fa-caret-down"></span>'
    }
};

$(document).ajaxReady(function () {
    $(".tab-pane.active .selectpicker, #modal .modal-dialog .selectpicker").each(function () {
        $(this).selectpicker($(this).data());
    });
});
//#endregion

//#region Chains
$(document).ajaxReady(function () {
    $.fn.chain.initFromData($(".tab-pane.active, #modal .modal-dialog"));
});

$(document).on("enable.chain disable.chain update.chain", function (evt) {
    $(evt.target).selectpicker("refresh");
});
//#endregion

//#region Expanded dataset API parsing
BTS._internal.data = $.fn.data;

$.fn.extend({
    data: function (key, value) {
        /// <signature>
        ///   <summary>Store arbitrary data associated with the matched elements.</summary>
        ///   <param name="key" type="String">A string naming the piece of data to set.</param>
        ///   <param name="value" type="Object">The new data value; this can be any Javascript type except undefined.</param>
        ///   <returns type="jQuery" />
        /// </signature>
        /// <signature>
        ///   <summary>Store arbitrary data associated with the matched elements.</summary>
        ///   <param name="obj" type="Object">An object of key-value pairs of data to update.</param>
        ///   <returns type="jQuery" />
        /// </signature>
        /// <signature>
        ///   <summary>
        ///     Return the value at the named data store for the first element in the jQuery collection,
        ///     as set by data(name, value) or by an HTML5 data-* attribute.
        ///   </summary>
        ///   <param name="key" type="String">Name of the data stored.</param>
        ///   <returns type="Object" />
        /// </signature>
        /// <signature>
        ///   <summary>
        ///     Return the value at the named data store for the first element in the jQuery collection,
        ///     as set by data(name, value) or by an HTML5 data-* attribute.
        ///   </summary>
        ///   <returns type="Object" />
        /// </signature>
        "use strict";
        var d = BTS._internal.data.apply(this, arguments),
            k;

        if (key === undefined || (typeof key === "string" && value === undefined)) {
            if (typeof d == "string" && d.toLowerCase() === "true") {
                return true;
            } else if (typeof d == "string" && d.toLowerCase() === "false") {
                return false;
            } else if (key === undefined) {
                for (k in d) {
                    if (typeof d[k] == "string" && d[k].toLowerCase() === "true") {
                        d[k] = true;
                    } else if (typeof d[k] == "string" && d[k].toLowerCase() === "false") {
                        d[k] = false;
                    }
                }
            }
        }
        
        return d;
    }
});

//#endregion

//#region ASU Header hooks

// This applies additional functionality to the shared ASU header, such as making the username a link to their local
// account page and expanding the search feature to work with the local website as well as performing an ASU-wide
// google search. Note: this works with version 4.3 of the ASU header, and may need to be updated/corrected for
// other versions.
$(function () {
    "use strict";
    /* global ASUHeader */

    if (ASUHeader.user_signedin && BTS.profilePath) {
        $("#asu_login_module li:first-child").html(
            $("<a></a>").attr("href", BTS.controllerPath + "/" + BTS.profilePath).text(ASUHeader.user_displayname)
        );
    }

    if (BTS.searchPath) {
        // the asu search box isn't set up on documentready yet, since it's created via javascript we can't hook into
        // utilize MutationObserver to check for when it's finally set up
        var observer = new MutationObserver(function (changes) {
            $.each(changes, function (i, v) {
                /// <param name="v" type="MutationRecord" />
                if (v.type === "childList" && v.addedNodes.length) {
                    observer.disconnect();

                    $("#asu_search_module").append('<div class="search-options">\
                        <label><input type="radio" name="searchlocal" value="true" checked> This Site</label>\
                        <label><input type="radio" name="searchlocal" value="false"> All of ASU</label>\
                    </div>');

                    $('#asu_search_module .search-options input[name="searchlocal"]').change(function () {
                        var form = $(this).closest("#asu_search_module").find("form");

                        if ($(this).val() === "true") {
                            form.attr("action", BTS.controllerPath + "/" + BTS.searchPath).attr("method", "POST");
                        } else {
                            form.attr("action", "https://search.asu.edu/search").attr("method", "GET");
                        }
                    });

                    $("#asu_search_module form")
                        .attr("action", BTS.controllerPath + "/" + BTS.searchPath)
                        .attr("method", "POST");
                }
            });
        });

        observer.observe(document.getElementById("asu_search_module"), { childList: true });
    }
});

//#endregion

// Fixes up the reset buttons to actually function with our custom controls
$(function () {
    "use strict";

    $(document).on("reset", function (e) {
        var form = $(e.target);

        if (!form.is("form")) {
            return;
        }

        setTimeout(function () {
            var validator;

            form.find(".selectpicker").each(function () {
                $(this).selectpicker("refresh");
            });

            form.find(".datepicker").each(function () {
                $(this).trigger("change");
            });

            validator = form.validate();
            form.find("[name]").each(function () {
                // clears validation error messages
                validator.successList.push(this);
                validator.showErrors();
            });
            validator.resetForm();
            validator.reset();
        }, 0);
    });
});

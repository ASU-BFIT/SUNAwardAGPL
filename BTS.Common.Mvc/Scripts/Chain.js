/**
 * Chain jQuery plugin complete with data API.
 * Version 1.0.0
 * 
 * Copyright (c) 2016 Ryan Schmidt
 *
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files
 * (the "Software"), to deal in the Software without restriction,
 * including without limitation the rights to use, copy, modify, merge,
 * publish, distribute, sublicense, and/or sell copies of the Software,
 * and to permit persons to whom the Software is furnished to do so,
 * subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included
 * in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL
 * THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
 * DEALINGS IN THE SOFTWARE.
 */

(function ($) {
    "use strict";

    var _defaults = {
            /// <field value="$.ajax()">AJAX Options to pass to jQuery.</field>
            ajaxOptions: {},
            /// <field>URL to load via AJAX.</field>
            dataSource: "",
            /// <field>Function to call to get updated data.</field>
            dataCallback: null,
            /// <field>Whether or not to disable the childElement if this field is empty.</field>
            disableNext: true,
            /// <field type="jQuery">Additional fields to include in the AJAX request.</field>
            include: null,
            /// <field>Namespace used for chain events.</field>
            namespace: "",
            /// <field>Whether or not we fire an update when the chain is first initialized.</field>
            updateOnInit: true,
            /// <field>
            ///   Whether to emit a console warning whenever we cannot update a child element
            ///   due to it being missing in the response.
            /// </field>
            warnMissing: false
        },
        _noConflict = $.fn.chain;

    function Chain(childElement, options, parentElement) {
        /// <signature>
        ///   <summary>Constructor function for a new chain (internal use only).</summary>
        ///   <param name="childElement" type="jQuery">Child element(s) to chain to.</param>
        ///   <param name="options" value="_defaults">Chain options.</param>
        ///   <param name="parentElement" type="jQuery">Parent element to chain from.</param>
        ///   <returns type="Chain">The newly constructed Chain.</returns>
        /// </signature>
        /// <signature>
        ///   <summary>Factory method to create a new chain.</summary>
        ///   <param name="childElement" type="jQuery">Child element(s) to chain to.</param>
        ///   <param name="options" value="_defaults" optional="true">Chain options.</param>
        ///   <returns type="jQuery">jQuery object to continue chaining.</returns>
        /// </signature>
        /// <signature>
        ///   <summary>Calls a method on a given chain or all chains attached to this element.</summary>
        ///   <param name="method" type="String">Method name to call.</param>
        ///   <param name="filter" type="jQuery" optional="true">
        ///     A value passed into childElement in the factory to limit the method to that chain.
        ///     If unspecified, the method is executed on all chains attached to the element.
        ///   </param>
        ///   <param name="arg" optional="true">Argument to pass to the chain method.</param>
        ///   <returns type="jQuery">jQuery object to continue chaining.</returns>
        /// </signature>
        /// <field name="childElement" type="jQuery">The elements updated whenever the parent element changes.</field>
        /// <field name="parentElement" type="jQuery">
        ///   The element to listen for changes to in order to update changed values.
        /// </field>
        /// <field name="options" type="Object">Chain options.</field>
        /// <field name="defaults" type="Object" static="true">
        ///   Chain defaults, set to override built-in defaults with custom values.
        /// </field>

        var method = childElement,
            filter = options,
            arg = parentElement,
            self = this;

        if (this instanceof Chain) {
            // constructing a new chain
            this.childElement = childElement;
            this.parentElement = parentElement;
            this.options = $.extend(true, {}, _defaults, (Chain.defaults || {}), (options || {}));
            // wrapper function is needed because if this.update is passed directly to the event listener,
            // "this" in the callback is the element that triggered the change (e.g. parent element) rather
            // than our chain, which is not a good thing
            parentElement.on("change.chain", function (e) { self.update(e); });
            // (maybe) fire off an update to initialize any child elements
            // Can also override $.fn.chain.prototype.update and check whether or not it has a parameter
            // (it will have one if called via event listener) if conditional logic is required for updateOnInit.
            if (this.options.updateOnInit) {
                this.update();
            }

            return;
        }

        if (typeof method === "string") {
            if (method[0] !== "_" && typeof Chain.prototype[method] === "function") {
                Chain.get.call(this, filter).forEach(function (v) {
                    /// <param name="v" type="Chain" />
                    v[method](arg);
                });
            }
        } else {
            if (childElement.length === 0) {
                $.error("Chain childElement must have at least 1 element");
                // normally can't get here as $.error throws an exception, but just in case that changes
                // we don't want to continue on with setting up the chain
                return this;
            }

            options = options || {};
            Chain.register.call(this, new Chain(childElement, options, this));
        }

        return this;
    }

    // Static/utility methods
    Chain.initFromData = function (context) {
        /// <summary>Initializes all chains that follow the data API in the given context.</summary>
        /// <param name="context" type="jQuery" optional="true">
        ///   Context to initialize chains in. If omitted, the entire document is scanned for chains.
        /// </param>
        context = context || $(document);

        context.find("[data-chain]").each(function () {
            var $this = $(this),
                data = $this.data(),
                childElement = Chain.getElementsFromData(context, $this, data.chain),
                // extend ignores undefined properties, so we don't need to bother with existence checks here
                options = {
                    ajaxOptions: data.chainAjaxOptions,
                    dataSource: data.chainDataSource,
                    dataCallback: data.chainDataCallback,
                    disableNext: data.chainDisableNext,
                    namespace: data.chainNamespace,
                    include: Chain.getElementsFromData(context, $this, data.chainInclude),
                    warnMissing: data.chainWarnMissing,
                    updateOnInit: data.chainUpdateOnInit
                };

            // don't re-init an element that was already initialized
            if (Chain.get.call($this, childElement).length === 0) {
                Chain.call($this, childElement, options);
            }
        });
    };

    Chain.getElementsFromData = function (context, parent, data) {
        /// <summary>
        ///   Given a comma-separated list of names, gets the elements in our context that match those names.
        /// </summary>
        /// <param name="context" type="jQuery">Context to get elements from.</param>
        /// <param name="parent" type="jQuery">Parent element (the source of the chain).</param>
        /// <param name="data" type="String">Comma-separated list of names to retrieve.</param>
        /// <returns type="jQuery">The elements matching the given names or null if there aren't any.</returns>

        // Note: parent is unused here, it is passed in so that functions overriding this one have it
        // in case they need it, for example if they want to find elements relative to the parent element.
        if (!data) {
            return null;
        }

        return context.find(data.split(",").map(function (v) { return '[name="' + v + '"]'; }).join(","));
    };

    Chain.register = function (chain) {
        /// <summary>Registers a chain to this element.</summary>
        /// <param name="chain" type="Chain">Chain to register.</param>
        var data = this.data("chains");

        /* jshint eqnull:true */
        if (data == null) {
            data = [chain];
        } else {
            data.push(chain);
        }

        this.data("chains", data);
    };

    Chain.remove = function (chain) {
        /// <summary>
        ///   Internal function. Removes a chain from this element.
        ///   Does NOT destroy the listeners, use .chain("destroy") for that!
        /// </summary>
        /// <param name="chain" type="Chain">Chain to remove.</param>
        var data = this.data("chains");

        /* jshint eqnull:true */
        if (data != null) {
            data = data.filter(function (v) { return v !== chain; });
        }

        this.data("chains", data);
    };

    Chain.get = function (filter) {
        /// <summary>Gets all chains attached to our element that matches the given filter.</summary>
        /// <param name="filter" type="jQuery" optional="true">Filter to match, or if undefined get all chains.</param>
        /// <returns type="Array" elementType="Chain">All Chains that match the filter.</returns>
        var data = this.data("chains");

        if (!data) {
            return [];
        }

        return this.data("chains").filter(function (v) {
            /// <param name="v" type="Chain" />
            /* jshint eqnull:true */
            return filter == null || v.childElement.selector === filter.selector;
        });
    };

    Chain.noConflict = function () {
        /// <summary>
        ///   Restores the previous value of $.fn.chain, to avoid conflicts with other code that uses that value.
        /// </summary>
        /// <returns type="Chain">Our chain function.</returns>
        $.fn.chain = _noConflict;
        return Chain;
    };

    Chain.prototype.update = function () {
        /// <summary>
        ///   Triggers a chain update, enabling/disabling child fields if applicable and issuing an AJAX request.
        /// </summary>
        var self = this,
            eventNamespace = (this.options.namespace ? "." + this.options.namespace : ""),
            val = getVal(this.parentElement),
            ajaxOptions, cb;

        if ((!val || val.length === 0) && this.options.disableNext) {
            setVal(this.childElement, null).prop("disabled", true);
            // notify child selects that they were modified
            this.childElement.filter("select").trigger($.Event("disable.chain" + eventNamespace), { chain: self });
        } else if (this.options.dataSource) {
            ajaxOptions = {
                url: this.options.dataSource,
                method: "GET",
                data: {},
                success: function (data) { self._update(data); }
            };

            if (!this.parentElement.attr("name")) {
                $.error("Chain was defined on element that lacks a name attribute.");
                return;
            }

            $.extend(true, ajaxOptions, this.options.ajaxOptions);
            ajaxOptions.data[this.parentElement.attr("name")] = this.parentElement.val();

            if (this.options.include) {
                this.options.include.each(function () {
                    var $this = $(this);

                    if (!$this.attr("name")) {
                        $.error("Chain was specified to include an element that lacks a name attribute.");
                        return;
                    }

                    ajaxOptions.data[$this.attr("name")] = $this.val();
                });
            }

            $.ajax(ajaxOptions);
        } else if (this.options.dataCallback) {
            cb = this.options.dataCallback;

            if (!(cb instanceof Function)) {
                cb = window[cb];

                if (!(cb instanceof Function)) {
                    $.error("Chain was given an invalid dataCallback, it must be a function.");
                    return;
                }
            }

            this._update(cb(this.parentElement, this.childElement, this.options.include));
        } else if (this.options.disableNext) {
            // no data to update but our value isn't empty, so simply re-enable our child elements
            this.childElement.prop("disabled", false)
                .trigger($.Event("enable.chain" + eventNamespace), { chain: self });
        }
    };

    function findChild(ctx, path, next) {
        /// <summary>Finds a child element matching path from the given context.</summary>
        /// <param name="ctx" type="Anything">Current context.</param>
        /// <param name="path" type="String">Path to find relative to current context.</param>
        /// <param name="next" type="Function">
        ///   Callback used to find a direct descendent of the current context.
        ///   Must return null if the descendent cannot be found.
        ///   <para>Anything next(Anything ctx, String name)</para>
        ///   <para>Anything next(Anything ctx, Number index)</para>
        /// </param>
        /// <returns type="Anything">New child context, or null if child was not found.</returns>
        var name = path.split("."),
            i, j, m, idx;

        for (i = 0; i < name.length; ++i) {
            m = name[i].split("[");

            for (j = 1; j < m.length; ++j) {
                if (m[j][m[j].length - 1] !== "]") {
                    // can't do anything with this element
                    throw new Error("Element name has invalid format");
                }

                m[j] = m[j].substr(0, m[j].length - 1);
            }

            ctx = next(ctx, m[0]);

            if (ctx === null) {
                return null;
            }

            for (j = 1; j < m.length; ++j) {
                idx = parseInt(m[j], 10);

                if (isNaN(idx)) {
                    // given a string key, e.g. foo[bar]
                    ctx = next(ctx, m[j]);
                } else {
                    // numeric (0-based) index, e.g. foo[0]
                    ctx = next(ctx, idx);
                }

                if (ctx === null) {
                    return null;
                }
            }
        }

        return ctx;
    }

    function isTextNode() {
        /// <summary>Determines if the current node is a text node.</summary>
        /// <returns type="Boolean" />
        /* jshint -W040 */
        return this.nodeType === Node.TEXT_NODE;
    }

    function getText(v) {
        /// <summary>Gets the text value of the node.</summary>
        /// <param name="v" type="Node">Node to retrieve value of.</param>
        /// <returns type="String">Node value.</returns>
        return v.nodeValue;
    }

    Chain.prototype._update = function (data) {
        /// <summary>Internal update method for children elements, using data obtained from the AJAX request.</summary>
        /// <param name="data">Data from AJAX request.</param>
        var self = this,
            eventNamespace = (this.options.namespace ? "." + this.options.namespace : ""),
            trimmed;

        this.childElement.prop("disabled", false)
            .trigger($.Event("enable.chain" + eventNamespace), { chain: self });

        // parse the data; we accept multiple forms in order to enable a wide variety of chaining
        // if data is HTML, we look to see if fields with names matching items in childElement are defined.
        // If so, each matching childElement is updated (value and/or innerHTML) with the corresponding thing in data.
        // If data is a JSON/XML object, then values are updated based on the object keys.
        // If data is a string, then child elements will have their values set to the string.
        if (typeof data === "undefined") {
            // this should only happen in an updateCallback is specified. In this case, the callback took care of updating
            // the child elements, so we do not need to. We should still fire the alert that the child was updated.
            this.childElement.each(function () {
                $(this).trigger($.Event("update.chain" + eventNamespace), { chain: self });
            });
        } else if (typeof data === "string") {
            // could either be HTML as a string or just plain text
            // we determine this by checking if it begins with < and ends with > after being trimmed
            trimmed = data.trim();
            if (trimmed[0] === "<" && trimmed[trimmed.length - 1] === ">") {
                // probably HTML, parse it as such
                data = $(trimmed);
                this.childElement.each(function () {
                    var name = $(this).attr("name"),
                        oldHtml = $(this).html(),
                        oldVal = $(this).val(),
                        el;

                    if (!name || name.indexOf('"') !== -1) {
                        // can't do anything with this element
                        console.error("Element does not have a name attribute or name attribute is invalid", this);
                        return;
                    }

                    el = data.find('[name="' + name + '"]');

                    if (el.length !== 1 && self.options.warnMissing) {
                        console.warn("Child was specified in chain, but HTML document did not contain an element " +
                            "for child or it contained multiple elements for child. To suppress this warning, " +
                            "set the option warnMissing to false.", this);
                        return;
                    }

                    $(this).html(el.html()).val(el.val());

                    if (el.html() !== oldHtml || el.val() !== oldVal) {
                        $(this).trigger($.Event("update.chain" + eventNamespace), { chain: self });
                    }
                });
            } else {
                // plaintext
                this.childElement.each(function () {
                    var oldVal = $(this).val();

                    $(this).val(data);

                    if (data !== oldVal) {
                        $(this).trigger($.Event("update.chain" + eventNamespace), { chain: self });
                    }
                });
            }
        } else if ($.isArray(data)) {
            // JSON parsed to an array; for this we assume we're chained to a multiselect, so pass it through raw
            this.childElement.each(function () {
                var oldVal = $(this).val();

                $(this).val(data);

                if (data !== oldVal) {
                    $(this).trigger($.Event("update.chain" + eventNamespace), { chain: self });
                }
            });
        } else if (data instanceof Document) {
            // XML document
            if (!data.hasChildren || data.children.length !== 1) {
                // invalid document, we require a root node
                $.error("Invalid XML response; root node required");
                return;
            }

            this.childElement.each(function () {
                var ctx = $(data.firstChild),
                    name = $(this).attr("name"),
                    oldVal = $(this).val(),
                    newVal;

                if (!name) {
                    // can't do anything with this element
                    console.error("Element does not have a name attribute", this);
                    return;
                }

                ctx = findChild(ctx, name, function (ctx, name) {
                    /// <param name="ctx" type="jQuery" />
                    var children;

                    if (typeof name === "number") {
                        // index is 0-based, nth-child is 1-based, so need to add one
                        children = ctx.children(":nth-child(" + (name + 1) + ")");
                    } else {
                        children = ctx.children(name);
                    }

                    if (children.length === 0) {
                        return null;
                    }

                    return children;
                });

                if (ctx === null && self.options.warnMissing) {
                    console.warn("Child was specified in chain, but XML document did not contain an element " +
                        "for child or it contained multiple elements for child. To suppress this warning, " +
                        "set the option warnMissing to false.", this);
                    return;
                }

                // get all of the text contents of our node and use that as the val
                if (ctx.children().length > 0) {
                    // we have a list of children nodes, so collect their text nodes into an array as our val
                    newVal = ctx.children().contents().filter(isTextNode).toArray().map(getText);
                } else {
                    newVal = ctx.text();
                }

                $(this).val(newVal);

                if (oldVal !== newVal) {
                    $(this).trigger($.Event("update.chain" + eventNamespace), { chain: self });
                }
            });
        } else {
            // JSON parsed to an object, keys should match childElement name properties
            // (with support for nested objects via . and [] notation)
            this.childElement.each(function () {
                var ctx = data,
                    oldVal = $(this).val(),
                    name = $(this).attr("name");

                if (!name) {
                    // can't do anything with this element
                    console.error("Element does not have a name attribute", this);
                    return;
                }

                ctx = findChild(ctx, name, function (ctx, name) {
                    return ctx[name] === undefined ? null : ctx[name];
                });

                if (ctx === null && self.options.warnMissing) {
                    console.warn("Child was specified in chain, but JSON document did not contain an element " +
                        "for child or it contained multiple elements for child. To suppress this warning, " +
                        "set the option warnMissing to false.", this);
                    return;
                }

                // at this point, ctx is the final value we're looking for; use it directly
                $(this).val(ctx);

                if (ctx !== oldVal) {
                    $(this).trigger($.Event("update.chain" + eventNamespace), { chain: self });
                }
            });
        }
    };

    Chain.prototype.destroy = function () {
        /// <summary>Removes the chain.</summary>
        this.parentElement.off("change.chain", this.update);
        Chain.remove.call(this.parentElement, this);
    };

    $.fn.chain = Chain;

    // initialize chains defined by the data API on document ready
    $(function () {
        Chain.initFromData();
    });

    // internal helper function used to make .val() play more nicely with checkboxes
    // (e.g. it operates on the checked property for checkboxes instead of the value attribute)
    function getVal(ele) {
        /// <param name="ele" type="jQuery" />
        if (ele.is(":checkbox")) {
            return ele.prop("checked");
        }

        return ele.val();
    }

    function setVal(ele, val) {
        /// <param name="ele" type="jQuery" />
        if (ele.is(":checkbox")) {
            ele.prop("checked", val);
        } else {
            if (val === null) {
                val = ele.is("select") ? [] : "";
            }

            ele.val(val);
        }

        return ele;
    }
})(jQuery);

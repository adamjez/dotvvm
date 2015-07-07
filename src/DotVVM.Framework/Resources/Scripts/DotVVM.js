var __extends = this.__extends || function (d, b) {
    for (var p in b) if (b.hasOwnProperty(p)) d[p] = b[p];
    function __() { this.constructor = d; }
    __.prototype = b.prototype;
    d.prototype = new __();
};
/// <reference path="typings/knockout/knockout.d.ts" />
/// <reference path="typings/knockout.mapper/knockout.mapper.d.ts" />
/// <reference path="typings/globalize/globalize.d.ts" />
var DotVVM = (function () {
    function DotVVM() {
        this.postBackCounter = 0;
        this.resourceSigns = {};
        this.extensions = {};
        this.viewModels = {};
        this.events = {
            init: new DotvvmEvent("dotvvm.events.init", true),
            beforePostback: new DotvvmEvent("dotvvm.events.beforePostback"),
            afterPostback: new DotvvmEvent("dotvvm.events.afterPostback"),
            error: new DotvvmEvent("dotvvm.events.error"),
            spaNavigating: new DotvvmEvent("dotvvm.events.spaNavigating"),
            spaNavigated: new DotvvmEvent("dotvvm.events.spaNavigated")
        };
    }
    DotVVM.prototype.init = function (viewModelName, culture) {
        var _this = this;
        this.culture = culture;
        var thisVm = this.viewModels[viewModelName] = JSON.parse(document.getElementById("__rw_viewmodel_" + viewModelName).value);
        if (thisVm.renderedResources) {
            thisVm.renderedResources.forEach(function (r) { return _this.resourceSigns[r] = true; });
        }
        var viewModel = thisVm.viewModel = ko.mapper.fromJS(this.viewModels[viewModelName].viewModel);
        ko.applyBindings(viewModel, document.documentElement);
        this.events.init.trigger(new DotvvmEventArgs(viewModel));
        // handle SPA
        var spaPlaceHolder = this.getSpaPlaceHolder();
        if (spaPlaceHolder) {
            this.attachEvent(window, "hashchange", function () { return _this.handleHashChange(viewModelName, spaPlaceHolder); });
            this.handleHashChange(viewModelName, spaPlaceHolder);
        }
        // persist the viewmodel in the hidden field so the Back button will work correctly
        this.attachEvent(window, "beforeunload", function (e) {
            _this.persistViewModel(viewModelName);
        });
    };
    DotVVM.prototype.handleHashChange = function (viewModelName, spaPlaceHolder) {
        if (document.location.hash.indexOf("#!/") === 0) {
            this.navigateCore(viewModelName, document.location.hash.substring(2));
        }
        else {
            // redirect to the default URL
            var url = spaPlaceHolder.getAttribute("data-rw-spacontentplaceholder-defaultroute");
            if (url) {
                document.location.hash = "#!/" + url;
            }
            else {
                this.navigateCore(viewModelName, "/");
            }
        }
    };
    DotVVM.prototype.onDocumentReady = function (callback) {
        // many thanks to http://dustindiaz.com/smallest-domready-ever
        /in/.test(document.readyState) ? setTimeout('dotvvm.onDocumentReady(' + callback + ')', 9) : callback();
    };
    DotVVM.prototype.persistViewModel = function (viewModelName) {
        var viewModel = this.viewModels[viewModelName];
        var persistedViewModel = {};
        for (var p in viewModel) {
            if (viewModel.hasOwnProperty(p)) {
                persistedViewModel[p] = viewModel[p];
            }
        }
        persistedViewModel["viewModel"] = ko.mapper.toJS(persistedViewModel["viewModel"]);
        document.getElementById("__rw_viewmodel_" + viewModelName).value = JSON.stringify(persistedViewModel);
    };
    DotVVM.prototype.backUpPostBackConter = function () {
        this.postBackCounter++;
        return this.postBackCounter;
    };
    DotVVM.prototype.isPostBackStillActive = function (currentPostBackCounter) {
        return this.postBackCounter === currentPostBackCounter;
    };
    DotVVM.prototype.postBack = function (viewModelName, sender, path, command, controlUniqueId, validationTargetPath) {
        var _this = this;
        var viewModel = this.viewModels[viewModelName].viewModel;
        // prevent double postbacks
        var currentPostBackCounter = this.backUpPostBackConter();
        // trigger beforePostback event
        var beforePostbackArgs = new DotvvmBeforePostBackEventArgs(sender, viewModel, viewModelName, validationTargetPath);
        this.events.beforePostback.trigger(beforePostbackArgs);
        if (beforePostbackArgs.cancel) {
            // trigger afterPostback event
            var afterPostBackArgsCanceled = new DotvvmAfterPostBackEventArgs(sender, viewModel, viewModelName, validationTargetPath, null);
            afterPostBackArgsCanceled.wasInterrupted = true;
            this.events.afterPostback.trigger(afterPostBackArgsCanceled);
            return;
        }
        // perform the postback
        this.updateDynamicPathFragments(sender, path);
        var data = {
            viewModel: ko.mapper.toJS(viewModel),
            currentPath: path,
            command: command,
            controlUniqueId: controlUniqueId,
            validationTargetPath: validationTargetPath || null,
            renderedResources: this.viewModels[viewModelName].renderedResources
        };
        this.postJSON(this.viewModels[viewModelName].url, "POST", ko.toJSON(data), function (result) {
            // if another postback has already been passed, don't do anything
            if (!_this.isPostBackStillActive(currentPostBackCounter)) {
                var afterPostBackArgsCanceled = new DotvvmAfterPostBackEventArgs(sender, viewModel, viewModelName, validationTargetPath, null);
                afterPostBackArgsCanceled.wasInterrupted = true;
                _this.events.afterPostback.trigger(afterPostBackArgsCanceled);
                return;
            }
            var resultObject = JSON.parse(result.responseText);
            if (!resultObject.viewModel && resultObject.viewModelDiff) {
                // TODO: patch (~deserialize) it to ko.observable viewModel
                resultObject.viewModel = _this.patch(data.viewModel, resultObject.viewModelDiff);
            }
            _this.loadResourceList(resultObject.resources, function () {
                var isSuccess = false;
                if (resultObject.action === "successfulCommand") {
                    // remove updated controls
                    var updatedControls = _this.cleanUpdatedControls(resultObject);
                    // update the viewmodel
                    if (resultObject.viewModel)
                        ko.mapper.fromJS(resultObject.viewModel, {}, _this.viewModels[viewModelName].viewModel);
                    isSuccess = true;
                    // add updated controls
                    _this.restoreUpdatedControls(resultObject, updatedControls, true);
                }
                else if (resultObject.action === "redirect") {
                    // redirect
                    _this.handleRedirect(resultObject);
                    return;
                }
                // trigger afterPostback event
                var afterPostBackArgs = new DotvvmAfterPostBackEventArgs(sender, viewModel, viewModelName, validationTargetPath, resultObject);
                _this.events.afterPostback.trigger(afterPostBackArgs);
                if (!isSuccess && !afterPostBackArgs.isHandled) {
                    throw "Invalid response from server!";
                }
            });
        }, function (xhr) {
            // if another postback has already been passed, don't do anything
            if (!_this.isPostBackStillActive(currentPostBackCounter))
                return;
            // execute error handlers
            var errArgs = new DotvvmErrorEventArgs(viewModel, xhr);
            _this.events.error.trigger(errArgs);
            if (!errArgs.handled) {
                alert(xhr.responseText);
            }
        });
    };
    DotVVM.prototype.loadResourceList = function (resources, callback) {
        var html = "";
        for (var name in resources) {
            if (this.resourceSigns[name])
                continue;
            this.resourceSigns[name] = true;
            html += resources[name] + " ";
        }
        if (html.trim() == "") {
            setTimeout(callback, 4);
            return;
        }
        else {
            var tmp = document.createElement("div");
            tmp.innerHTML = html;
            var elements = [];
            for (var i = 0; i < tmp.children.length; i++) {
                elements.push(tmp.children.item(i));
            }
            this.loadResourceElements(elements, 0, callback);
        }
    };
    DotVVM.prototype.loadResourceElements = function (elements, offset, callback) {
        var _this = this;
        if (offset >= elements.length) {
            callback();
            return;
        }
        var el = elements[offset];
        var waitForScriptLoaded = true;
        if (el.tagName.toLowerCase() == "script") {
            // create the script element
            var script = document.createElement("script");
            if (el.src) {
                script.src = el.src;
            }
            if (el.type) {
                script.type = el.type;
            }
            if (el.text) {
                script.text = el.text;
                waitForScriptLoaded = false;
            }
            el = script;
        }
        else if (el.tagName.toLowerCase() == "link") {
            // create link
            var link = document.createElement("link");
            if (el.href) {
                link.href = el.href;
            }
            if (el.rel) {
                link.rel = el.rel;
            }
            if (el.type) {
                link.type = el.type;
            }
            el = link;
        }
        // load next script when this is finished
        if (waitForScriptLoaded) {
            el.onload = function () { return _this.loadResourceElements(elements, offset + 1, callback); };
        }
        document.head.appendChild(el);
        if (!waitForScriptLoaded) {
            this.loadResourceElements(elements, offset + 1, callback);
        }
    };
    DotVVM.prototype.evaluateOnViewModel = function (context, expression) {
        var result = eval("(function (c) { return c." + expression + "; })")(context);
        if (result && result.$data) {
            result = result.$data;
        }
        return result;
    };
    DotVVM.prototype.getSpaPlaceHolder = function () {
        var elements = document.getElementsByName("__rw_SpaContentPlaceHolder");
        if (elements.length == 1) {
            return elements[0];
        }
        return null;
    };
    DotVVM.prototype.navigateCore = function (viewModelName, url) {
        var _this = this;
        var viewModel = this.viewModels[viewModelName].viewModel;
        // prevent double postbacks
        var currentPostBackCounter = this.backUpPostBackConter();
        // trigger spaNavigating event
        var spaNavigatingArgs = new DotvvmSpaNavigatingEventArgs(viewModel, viewModelName, url);
        this.events.spaNavigating.trigger(spaNavigatingArgs);
        if (spaNavigatingArgs.cancel) {
            return;
        }
        // add virtual directory prefix
        var fullUrl = this.addLeadingSlash(this.concatUrl(this.viewModels[viewModelName].virtualDirectory || "", url));
        // find SPA placeholder
        var spaPlaceHolder = this.getSpaPlaceHolder();
        if (!spaPlaceHolder) {
            document.location.href = fullUrl;
            return;
        }
        // send the request
        var spaPlaceHolderUniqueId = spaPlaceHolder.attributes["data-rw-spacontentplaceholder"].value;
        this.getJSON(fullUrl, "GET", spaPlaceHolderUniqueId, function (result) {
            // if another postback has already been passed, don't do anything
            if (!_this.isPostBackStillActive(currentPostBackCounter))
                return;
            var resultObject = JSON.parse(result.responseText);
            _this.loadResourceList(resultObject.resources, function () {
                var isSuccess = false;
                if (resultObject.action === "successfulCommand" || !resultObject.action) {
                    // remove updated controls
                    var updatedControls = _this.cleanUpdatedControls(resultObject);
                    // update the viewmodel
                    ko.cleanNode(document.documentElement);
                    _this.viewModels[viewModelName] = {};
                    for (var p in resultObject) {
                        if (resultObject.hasOwnProperty(p)) {
                            _this.viewModels[viewModelName][p] = resultObject[p];
                        }
                    }
                    ko.mapper.fromJS(resultObject.viewModel, {}, _this.viewModels[viewModelName].viewModel);
                    isSuccess = true;
                    // add updated controls
                    _this.restoreUpdatedControls(resultObject, updatedControls, false);
                    ko.applyBindings(_this.viewModels[viewModelName].viewModel, document.documentElement);
                }
                else if (resultObject.action === "redirect") {
                    _this.handleRedirect(resultObject);
                    return;
                }
                // trigger spaNavigated event
                var spaNavigatedArgs = new DotvvmSpaNavigatedEventArgs(viewModel, viewModelName, resultObject);
                _this.events.spaNavigated.trigger(spaNavigatedArgs);
                if (!isSuccess && !spaNavigatedArgs.isHandled) {
                    throw "Invalid response from server!";
                }
            });
        }, function (xhr) {
            // if another postback has already been passed, don't do anything
            if (!_this.isPostBackStillActive(currentPostBackCounter))
                return;
            // execute error handlers
            var errArgs = new DotvvmErrorEventArgs(viewModel, xhr, true);
            _this.events.error.trigger(errArgs);
            if (!errArgs.handled) {
                alert(xhr.responseText);
            }
        });
    };
    DotVVM.prototype.handleRedirect = function (resultObject) {
        // redirect
        if (this.getSpaPlaceHolder() && resultObject.url.indexOf("//") < 0) {
            // relative URL - keep in SPA mode
            document.location.href = "#!" + resultObject.url;
        }
        else {
            // absolute URL - load the URL
            document.location.href = resultObject.url;
        }
    };
    DotVVM.prototype.addLeadingSlash = function (url) {
        if (url.length > 0 && url.substring(0, 1) != "/") {
            return "/" + url;
        }
        return url;
    };
    DotVVM.prototype.concatUrl = function (url1, url2) {
        if (url1.length > 0 && url1.substring(url1.length - 1) == "/") {
            url1 = url1.substring(0, url1.length - 1);
        }
        return url1 + this.addLeadingSlash(url2);
    };
    DotVVM.prototype.patch = function (source, patch) {
        var _this = this;
        if (source instanceof Array && patch instanceof Array) {
            return patch.map(function (val, i) { return _this.patch(source[i], val); });
        }
        else if (source instanceof Array || patch instanceof Array)
            return patch;
        else if (typeof source == "object" && typeof patch == "object") {
            for (var p in patch) {
                if (patch[p] == null)
                    source[p] = null;
                else if (source[p] == null)
                    source[p] = patch[p];
                else
                    source[p] = this.patch(source[p], patch[p]);
            }
        }
        else
            return patch;
        return source;
    };
    DotVVM.prototype.format = function (format) {
        var values = [];
        for (var _i = 1; _i < arguments.length; _i++) {
            values[_i - 1] = arguments[_i];
        }
        return format.replace(/\{([1-9]?[0-9]+)(:[^}])?\}/g, function (match, group0, group1) {
            var value = values[parseInt(group0)];
            if (group1) {
                return dotvvm.formatString(group1, value);
            }
            else {
                return value;
            }
        });
    };
    DotVVM.prototype.formatString = function (format, value) {
        if (format == "g") {
            return dotvvm.formatString("d", value) + " " + dotvvm.formatString("t", value);
        }
        else if (format == "G") {
            return dotvvm.formatString("d", value) + " " + dotvvm.formatString("T", value);
        }
        value = ko.unwrap(value);
        if (typeof value === "string" && value.match("^[0-9]{4}-[0-9]{2}-[0-9]{2}T[0-9]{2}:[0-9]{2}:[0-9]{2}(\\.[0-9]{1,3})?$")) {
            // JSON date in string
            value = new Date(value);
        }
        return Globalize.format(value, format, dotvvm.culture);
    };
    DotVVM.prototype.getDataSourceItems = function (viewModel) {
        var value = ko.unwrap(viewModel);
        return value.Items || value;
    };
    DotVVM.prototype.updateDynamicPathFragments = function (sender, path) {
        var context = ko.contextFor(sender);
        for (var i = path.length - 1; i >= 0; i--) {
            if (path[i].indexOf("[$index]") >= 0) {
                path[i] = path[i].replace("[$index]", "[" + context.$index() + "]");
            }
            context = context.$parentContext;
        }
    };
    DotVVM.prototype.postJSON = function (url, method, postData, success, error) {
        var xhr = this.getXHR();
        xhr.open(method, url, true);
        xhr.setRequestHeader("Content-Type", "application/json");
        xhr.onreadystatechange = function () {
            if (xhr.readyState != 4)
                return;
            if (xhr.status < 400) {
                success(xhr);
            }
            else {
                error(xhr);
            }
        };
        xhr.send(postData);
    };
    DotVVM.prototype.getJSON = function (url, method, spaPlaceHolderUniqueId, success, error) {
        var xhr = this.getXHR();
        xhr.open(method, url, true);
        xhr.open("GET", url, true);
        xhr.setRequestHeader("Content-Type", "application/json");
        xhr.setRequestHeader("X-DotVVM-SpaContentPlaceHolder", spaPlaceHolderUniqueId);
        xhr.onreadystatechange = function () {
            if (xhr.readyState != 4)
                return;
            if (xhr.status < 400) {
                success(xhr);
            }
            else {
                error(xhr);
            }
        };
        xhr.send();
    };
    DotVVM.prototype.getXHR = function () {
        return XMLHttpRequest ? new XMLHttpRequest() : new ActiveXObject("Microsoft.XMLHTTP");
    };
    DotVVM.prototype.cleanUpdatedControls = function (resultObject) {
        var updatedControls = {};
        for (var id in resultObject.updatedControls) {
            if (resultObject.updatedControls.hasOwnProperty(id)) {
                var control = document.getElementById(id);
                var nextSibling = control.nextSibling;
                var parent = control.parentNode;
                ko.removeNode(control);
                updatedControls[id] = { control: control, nextSibling: nextSibling, parent: parent };
            }
        }
        return updatedControls;
    };
    DotVVM.prototype.restoreUpdatedControls = function (resultObject, updatedControls, applyBindingsOnEachControl) {
        for (var id in resultObject.updatedControls) {
            if (resultObject.updatedControls.hasOwnProperty(id)) {
                var updatedControl = updatedControls[id];
                if (updatedControl.nextSibling) {
                    updatedControl.parent.insertBefore(updatedControl.control, updatedControl.nextSibling);
                }
                else {
                    updatedControl.parent.appendChild(updatedControl.control);
                }
                updatedControl.control.outerHTML = resultObject.updatedControls[id];
                if (applyBindingsOnEachControl) {
                    ko.applyBindings(ko.dataFor(updatedControl.parent), updatedControl.control);
                }
            }
        }
    };
    DotVVM.prototype.attachEvent = function (target, name, callback, useCapture) {
        if (useCapture === void 0) { useCapture = false; }
        if (target.addEventListener) {
            target.addEventListener(name, callback, useCapture);
        }
        else {
            target.attachEvent("on" + name, callback);
        }
    };
    DotVVM.prototype.buildRouteUrl = function (routePath, params) {
        return routePath.replace(/\{[^\}]+\}/g, function (s) { return params[s.substring(1, s.length - 1)] || ""; });
    };
    return DotVVM;
})();
// DotvvmEvent is used because CustomEvent is not browser compatible and does not support 
// calling missed events for handler that subscribed too late.
var DotvvmEvent = (function () {
    function DotvvmEvent(name, triggerMissedEventsOnSubscribe) {
        if (triggerMissedEventsOnSubscribe === void 0) { triggerMissedEventsOnSubscribe = false; }
        this.name = name;
        this.triggerMissedEventsOnSubscribe = triggerMissedEventsOnSubscribe;
        this.handlers = [];
        this.history = [];
    }
    DotvvmEvent.prototype.subscribe = function (handler) {
        this.handlers.push(handler);
        if (this.triggerMissedEventsOnSubscribe) {
            for (var i = 0; i < this.history.length; i++) {
                handler(history[i]);
            }
        }
    };
    DotvvmEvent.prototype.unsubscribe = function (handler) {
        var index = this.handlers.indexOf(handler);
        if (index >= 0) {
            this.handlers = this.handlers.splice(index, 1);
        }
    };
    DotvvmEvent.prototype.trigger = function (data) {
        for (var i = 0; i < this.handlers.length; i++) {
            this.handlers[i](data);
        }
        if (this.triggerMissedEventsOnSubscribe) {
            this.history.push(data);
        }
    };
    return DotvvmEvent;
})();
var DotvvmEventArgs = (function () {
    function DotvvmEventArgs(viewModel) {
        this.viewModel = viewModel;
    }
    return DotvvmEventArgs;
})();
var DotvvmErrorEventArgs = (function (_super) {
    __extends(DotvvmErrorEventArgs, _super);
    function DotvvmErrorEventArgs(viewModel, xhr, isSpaNavigationError) {
        if (isSpaNavigationError === void 0) { isSpaNavigationError = false; }
        _super.call(this, viewModel);
        this.viewModel = viewModel;
        this.xhr = xhr;
        this.isSpaNavigationError = isSpaNavigationError;
        this.handled = false;
    }
    return DotvvmErrorEventArgs;
})(DotvvmEventArgs);
var DotvvmBeforePostBackEventArgs = (function (_super) {
    __extends(DotvvmBeforePostBackEventArgs, _super);
    function DotvvmBeforePostBackEventArgs(sender, viewModel, viewModelName, validationTargetPath) {
        _super.call(this, viewModel);
        this.sender = sender;
        this.viewModel = viewModel;
        this.viewModelName = viewModelName;
        this.validationTargetPath = validationTargetPath;
        this.cancel = false;
        this.clientValidationFailed = false;
    }
    return DotvvmBeforePostBackEventArgs;
})(DotvvmEventArgs);
var DotvvmAfterPostBackEventArgs = (function (_super) {
    __extends(DotvvmAfterPostBackEventArgs, _super);
    function DotvvmAfterPostBackEventArgs(sender, viewModel, viewModelName, validationTargetPath, serverResponseObject) {
        _super.call(this, viewModel);
        this.sender = sender;
        this.viewModel = viewModel;
        this.viewModelName = viewModelName;
        this.validationTargetPath = validationTargetPath;
        this.serverResponseObject = serverResponseObject;
        this.isHandled = false;
        this.wasInterrupted = false;
    }
    return DotvvmAfterPostBackEventArgs;
})(DotvvmEventArgs);
var DotvvmSpaNavigatingEventArgs = (function (_super) {
    __extends(DotvvmSpaNavigatingEventArgs, _super);
    function DotvvmSpaNavigatingEventArgs(viewModel, viewModelName, newUrl) {
        _super.call(this, viewModel);
        this.viewModel = viewModel;
        this.viewModelName = viewModelName;
        this.newUrl = newUrl;
        this.cancel = false;
    }
    return DotvvmSpaNavigatingEventArgs;
})(DotvvmEventArgs);
var DotvvmSpaNavigatedEventArgs = (function (_super) {
    __extends(DotvvmSpaNavigatedEventArgs, _super);
    function DotvvmSpaNavigatedEventArgs(viewModel, viewModelName, serverResponseObject) {
        _super.call(this, viewModel);
        this.viewModel = viewModel;
        this.viewModelName = viewModelName;
        this.serverResponseObject = serverResponseObject;
        this.isHandled = false;
    }
    return DotvvmSpaNavigatedEventArgs;
})(DotvvmEventArgs);
var dotvvm = new DotVVM();
// add knockout binding handler for update progress control
ko.bindingHandlers["dotvvmpdateProgressVisible"] = {
    init: function (element, valueAccessor, allBindingsAccessor, viewModel, bindingContext) {
        element.style.display = "none";
        dotvvm.events.beforePostback.subscribe(function (e) {
            element.style.display = "";
        });
        dotvvm.events.spaNavigating.subscribe(function (e) {
            element.style.display = "";
        });
        dotvvm.events.afterPostback.subscribe(function (e) {
            element.style.display = "none";
        });
        dotvvm.events.spaNavigated.subscribe(function (e) {
            element.style.display = "none";
        });
        dotvvm.events.error.subscribe(function (e) {
            element.style.display = "none";
        });
    }
};
//# sourceMappingURL=DotVVM.js.map
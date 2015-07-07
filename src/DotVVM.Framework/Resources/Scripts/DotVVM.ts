/// <reference path="typings/knockout/knockout.d.ts" />
/// <reference path="typings/knockout.mapper/knockout.mapper.d.ts" />
/// <reference path="typings/globalize/globalize.d.ts" />
interface RenderedResourceList {
    [name: string]: string;
}

class DotVVM {

    private postBackCounter = 0;
    private resourceSigns: { [name: string]: boolean } = {}

    public extensions: any = {};
    public viewModels: any = {};
    public culture: string;
    public events = {
        init: new DotvvmEvent<DotvvmEventArgs>("dotvvm.events.init", true),
        beforePostback: new DotvvmEvent<DotvvmBeforePostBackEventArgs>("dotvvm.events.beforePostback"),
        afterPostback: new DotvvmEvent<DotvvmAfterPostBackEventArgs>("dotvvm.events.afterPostback"),
        error: new DotvvmEvent<DotvvmErrorEventArgs>("dotvvm.events.error"),
        spaNavigating: new DotvvmEvent<DotvvmSpaNavigatingEventArgs>("dotvvm.events.spaNavigating"),
        spaNavigated: new DotvvmEvent<DotvvmSpaNavigatedEventArgs>("dotvvm.events.spaNavigated")
    };

    public init(viewModelName: string, culture: string): void {
        this.culture = culture;
        var thisVm = this.viewModels[viewModelName] = JSON.parse((<HTMLInputElement>document.getElementById("__rw_viewmodel_" + viewModelName)).value);
        if (thisVm.renderedResources) {
            thisVm.renderedResources.forEach(r => this.resourceSigns[r] = true);
        }
        var viewModel = thisVm.viewModel = ko.mapper.fromJS(this.viewModels[viewModelName].viewModel);

        ko.applyBindings(viewModel, document.documentElement);
        this.events.init.trigger(new DotvvmEventArgs(viewModel));

        // handle SPA
        var spaPlaceHolder = this.getSpaPlaceHolder();
        if (spaPlaceHolder) {
            this.attachEvent(window, "hashchange",() => this.handleHashChange(viewModelName, spaPlaceHolder));
            this.handleHashChange(viewModelName, spaPlaceHolder);
        }

        // persist the viewmodel in the hidden field so the Back button will work correctly
        this.attachEvent(window, "beforeunload", e => {
            this.persistViewModel(viewModelName);
        });
    }

    private handleHashChange(viewModelName: string, spaPlaceHolder: HTMLElement) {
        if (document.location.hash.indexOf("#!/") === 0) {
            this.navigateCore(viewModelName, document.location.hash.substring(2));
        } else {
            // redirect to the default URL
            var url = spaPlaceHolder.getAttribute("data-rw-spacontentplaceholder-defaultroute");
            if (url) {
                document.location.hash = "#!/" + url;
            } else {
                this.navigateCore(viewModelName, "/");
            }
        }
    }

    public onDocumentReady(callback: () => void) {
        // many thanks to http://dustindiaz.com/smallest-domready-ever
        /in/.test(document.readyState) ? setTimeout('dotvvm.onDocumentReady(' + callback + ')', 9) : callback();
    }

    private persistViewModel(viewModelName: string) {
        var viewModel = this.viewModels[viewModelName];
        var persistedViewModel = {};
        for (var p in viewModel) {
            if (viewModel.hasOwnProperty(p)) {
                persistedViewModel[p] = viewModel[p];
            }
        }
        persistedViewModel["viewModel"] = ko.mapper.toJS(persistedViewModel["viewModel"]);
        (<HTMLInputElement>document.getElementById("__rw_viewmodel_" + viewModelName)).value = JSON.stringify(persistedViewModel);
    }

    private backUpPostBackConter(): number {
        this.postBackCounter++;
        return this.postBackCounter;
    }

    private isPostBackStillActive(currentPostBackCounter: number): boolean {
        return this.postBackCounter === currentPostBackCounter;
    }

    public postBack(viewModelName: string, sender: HTMLElement, path: string[], command: string, controlUniqueId: string, validationTargetPath?: any): void {
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
        this.postJSON(this.viewModels[viewModelName].url, "POST", ko.toJSON(data), result => {
            // if another postback has already been passed, don't do anything
            if (!this.isPostBackStillActive(currentPostBackCounter)) {
                var afterPostBackArgsCanceled = new DotvvmAfterPostBackEventArgs(sender, viewModel, viewModelName, validationTargetPath, null);
                afterPostBackArgsCanceled.wasInterrupted = true;
                this.events.afterPostback.trigger(afterPostBackArgsCanceled);
                return;
            }

            var resultObject = JSON.parse(result.responseText);
            if (!resultObject.viewModel && resultObject.viewModelDiff) {
                // TODO: patch (~deserialize) it to ko.observable viewModel
                resultObject.viewModel = this.patch(data.viewModel, resultObject.viewModelDiff);
            }

            this.loadResourceList(resultObject.resources,() => {
                var isSuccess = false;
                if (resultObject.action === "successfulCommand") {
                    // remove updated controls
                    var updatedControls = this.cleanUpdatedControls(resultObject);

                    // update the viewmodel
                    if (resultObject.viewModel)
                        ko.mapper.fromJS(resultObject.viewModel, {}, this.viewModels[viewModelName].viewModel);
                    isSuccess = true;

                    // add updated controls
                    this.restoreUpdatedControls(resultObject, updatedControls, true);

                } else if (resultObject.action === "redirect") {
                    // redirect
                    this.handleRedirect(resultObject);
                    return;
                } 
            
                // trigger afterPostback event
                var afterPostBackArgs = new DotvvmAfterPostBackEventArgs(sender, viewModel, viewModelName, validationTargetPath, resultObject);
                this.events.afterPostback.trigger(afterPostBackArgs);
                if (!isSuccess && !afterPostBackArgs.isHandled) {
                    throw "Invalid response from server!";
                }
            });
        }, xhr => {
                // if another postback has already been passed, don't do anything
                if (!this.isPostBackStillActive(currentPostBackCounter)) return;

                // execute error handlers
                var errArgs = new DotvvmErrorEventArgs(viewModel, xhr);
                this.events.error.trigger(errArgs);
                if (!errArgs.handled) {
                    alert(xhr.responseText);
                }
            });
    }

    private loadResourceList(resources: RenderedResourceList, callback: () => void) {
        var html = "";
        for (var name in resources) {
            if (this.resourceSigns[name]) continue;
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
            var elements: HTMLElement[] = [];
            for (var i = 0; i < tmp.children.length; i++) {
                elements.push(<HTMLElement>tmp.children.item(i));
            }
            this.loadResourceElements(elements, 0, callback);
        }
    }

    private loadResourceElements(elements: HTMLElement[], offset: number, callback: () => void) {
        if (offset >= elements.length) {
            callback();
            return;
        }
        var el = <any> elements[offset];
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
            el.onload = () => this.loadResourceElements(elements, offset + 1, callback);
        }
        document.head.appendChild(el);
        if (!waitForScriptLoaded) {
            this.loadResourceElements(elements, offset + 1, callback);
        }
    }

    public evaluateOnViewModel(context, expression) {
    var result = eval("(function (c) { return c." + expression + "; })")(context);
    if (result && result.$data) {
        result = result.$data;
    }
    return result;
}

    private getSpaPlaceHolder() : HTMLElement {
    var elements = document.getElementsByName("__rw_SpaContentPlaceHolder");
    if (elements.length == 1) {
        return <HTMLElement>elements[0];
    }
    return null;
}

    private navigateCore(viewModelName: string, url: string) {
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
    this.getJSON(fullUrl, "GET", spaPlaceHolderUniqueId, result => {
        // if another postback has already been passed, don't do anything
        if (!this.isPostBackStillActive(currentPostBackCounter)) return;

        var resultObject = JSON.parse(result.responseText);
        this.loadResourceList(resultObject.resources,() => {
            var isSuccess = false;
            if (resultObject.action === "successfulCommand" || !resultObject.action) {
                // remove updated controls
                var updatedControls = this.cleanUpdatedControls(resultObject);

                // update the viewmodel
                ko.cleanNode(document.documentElement);
                this.viewModels[viewModelName] = {};
                for (var p in resultObject) {
                    if (resultObject.hasOwnProperty(p)) {
                        this.viewModels[viewModelName][p] = resultObject[p];
                    }
                }
                ko.mapper.fromJS(resultObject.viewModel, {}, this.viewModels[viewModelName].viewModel);
                isSuccess = true;

                // add updated controls
                this.restoreUpdatedControls(resultObject, updatedControls, false);
                ko.applyBindings(this.viewModels[viewModelName].viewModel, document.documentElement);

            } else if (resultObject.action === "redirect") {
                this.handleRedirect(resultObject);
                return;
            } 
            
            // trigger spaNavigated event
            var spaNavigatedArgs = new DotvvmSpaNavigatedEventArgs(viewModel, viewModelName, resultObject);
            this.events.spaNavigated.trigger(spaNavigatedArgs);
            if (!isSuccess && !spaNavigatedArgs.isHandled) {
                throw "Invalid response from server!";
            }
        });
    }, xhr => {
            // if another postback has already been passed, don't do anything
            if (!this.isPostBackStillActive(currentPostBackCounter)) return;

            // execute error handlers
            var errArgs = new DotvvmErrorEventArgs(viewModel, xhr, true);
            this.events.error.trigger(errArgs);
            if (!errArgs.handled) {
                alert(xhr.responseText);
            }
        });
}

    private handleRedirect(resultObject: any) {
    // redirect
    if (this.getSpaPlaceHolder() && resultObject.url.indexOf("//") < 0) {
        // relative URL - keep in SPA mode
        document.location.href = "#!" + resultObject.url;
    } else {
        // absolute URL - load the URL
        document.location.href = resultObject.url;
    }
}

    private addLeadingSlash(url: string) {
    if (url.length > 0 && url.substring(0, 1) != "/") {
        return "/" + url;
    }
    return url;
}

    private concatUrl(url1: string, url2: string) {
    if (url1.length > 0 && url1.substring(url1.length - 1) == "/") {
        url1 = url1.substring(0, url1.length - 1);
    }
    return url1 + this.addLeadingSlash(url2);
}

    public patch(source: any, patch: any): any {
    if (source instanceof Array && patch instanceof Array) {
        return patch.map((val, i) =>
            this.patch(source[i], val));
    }
    else if (source instanceof Array || patch instanceof Array)
        return patch;
    else if (typeof source == "object" && typeof patch == "object") {
        for (var p in patch) {
            if (patch[p] == null) source[p] = null;
            else if (source[p] == null) source[p] = patch[p];
            else source[p] = this.patch(source[p], patch[p]);
        }
    }
    else return patch;

    return source;
}

    public format(format: string, ...values: string[]) {
    return format.replace(/\{([1-9]?[0-9]+)(:[^}])?\}/g,(match, group0, group1) => {
        var value = values[parseInt(group0)];
        if (group1) {
            return dotvvm.formatString(group1, value);
        } else {
            return value;
        }
    });
}

    public formatString(format: string, value: any) {
    if (format == "g") {
        return dotvvm.formatString("d", value) + " " + dotvvm.formatString("t", value);
    } else if (format == "G") {
        return dotvvm.formatString("d", value) + " " + dotvvm.formatString("T", value);
    }

    value = ko.unwrap(value);
    if (typeof value === "string" && value.match("^[0-9]{4}-[0-9]{2}-[0-9]{2}T[0-9]{2}:[0-9]{2}:[0-9]{2}(\\.[0-9]{1,3})?$")) {
        // JSON date in string
        value = new Date(value);
    }
    return Globalize.format(value, format, dotvvm.culture);
}

    public getDataSourceItems(viewModel: any) {
    var value = ko.unwrap(viewModel);
    return value.Items || value;
}

    private updateDynamicPathFragments(sender: HTMLElement, path: string[]): void {
    var context = ko.contextFor(sender);

    for(var i = path.length - 1; i >= 0; i--) {
    if (path[i].indexOf("[$index]") >= 0) {
        path[i] = path[i].replace("[$index]", "[" + context.$index() + "]");
    }
    context = context.$parentContext;
}
    }

    private postJSON(url: string, method: string, postData: any, success: (request: XMLHttpRequest) => void, error: (response: XMLHttpRequest) => void) {
    var xhr = this.getXHR();
    xhr.open(method, url, true);
    xhr.setRequestHeader("Content-Type", "application/json");
    xhr.onreadystatechange = () => {
        if (xhr.readyState != 4) return;
        if (xhr.status < 400) {
            success(xhr);
        } else {
            error(xhr);
        }
    };
    xhr.send(postData);
}

    private getJSON(url: string, method: string, spaPlaceHolderUniqueId: string, success: (request: XMLHttpRequest) => void, error: (response: XMLHttpRequest) => void) {
    var xhr = this.getXHR();
    xhr.open(method, url, true);
    xhr.open("GET", url, true);
    xhr.setRequestHeader("Content-Type", "application/json");
    xhr.setRequestHeader("X-DotVVM-SpaContentPlaceHolder", spaPlaceHolderUniqueId);
    xhr.onreadystatechange = () => {
        if (xhr.readyState != 4) return;
        if (xhr.status < 400) {
            success(xhr);
        } else {
            error(xhr);
        }
    };
    xhr.send();
}

    public getXHR(): XMLHttpRequest {
    return XMLHttpRequest ? new XMLHttpRequest() : <XMLHttpRequest>new ActiveXObject("Microsoft.XMLHTTP");
}
    
    private cleanUpdatedControls(resultObject: any) {
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
}

    private restoreUpdatedControls(resultObject: any, updatedControls: any, applyBindingsOnEachControl: boolean) {
    for (var id in resultObject.updatedControls) {
        if (resultObject.updatedControls.hasOwnProperty(id)) {
            var updatedControl = updatedControls[id];
            if (updatedControl.nextSibling) {
                updatedControl.parent.insertBefore(updatedControl.control, updatedControl.nextSibling);
            } else {
                updatedControl.parent.appendChild(updatedControl.control);
            }
            updatedControl.control.outerHTML = resultObject.updatedControls[id];

            if (applyBindingsOnEachControl) {
                ko.applyBindings(ko.dataFor(updatedControl.parent), updatedControl.control);
            }
        }
    }
}

    private attachEvent(target: any, name: string, callback: (ev: PointerEvent) => any, useCapture: boolean = false) {
    if (target.addEventListener) {
        target.addEventListener(name, callback, useCapture);
    }
    else {
        target.attachEvent("on" + name, callback);
    }
}

    public buildRouteUrl(routePath: string, params: any): string {
    return routePath.replace(/\{[^\}]+\}/g, s => params[s.substring(1, s.length - 1)] || "");
}
}

// DotvvmEvent is used because CustomEvent is not browser compatible and does not support 
// calling missed events for handler that subscribed too late.
class DotvvmEvent<T extends DotvvmEventArgs> {
    private handlers = [];
    private history = [];

    constructor(public name: string, private triggerMissedEventsOnSubscribe: boolean = false) {
    }

    public subscribe(handler: (data: T) => void) {
        this.handlers.push(handler);

        if (this.triggerMissedEventsOnSubscribe) {
            for (var i = 0; i < this.history.length; i++) {
                handler(history[i]);
            }
        }
    }

    public unsubscribe(handler: (data: T) => void) {
        var index = this.handlers.indexOf(handler);
        if (index >= 0) {
            this.handlers = this.handlers.splice(index, 1);
        }
    }

    public trigger(data: T): void {
        for (var i = 0; i < this.handlers.length; i++) {
            this.handlers[i](data);
        }

        if (this.triggerMissedEventsOnSubscribe) {
            this.history.push(data);
        }
    }
}

class DotvvmEventArgs {
    constructor(public viewModel: any) {
    }
}
class DotvvmErrorEventArgs extends DotvvmEventArgs {
    public handled = false;
    constructor(public viewModel: any, public xhr: XMLHttpRequest, public isSpaNavigationError: boolean = false) {
        super(viewModel);
    }
}
class DotvvmBeforePostBackEventArgs extends DotvvmEventArgs {
    public cancel: boolean = false;
    public clientValidationFailed: boolean = false;
    constructor(public sender: HTMLElement, public viewModel: any, public viewModelName: string, public validationTargetPath: any) {
        super(viewModel);
    }
}
class DotvvmAfterPostBackEventArgs extends DotvvmEventArgs {
    public isHandled: boolean = false;
    public wasInterrupted: boolean = false;
    constructor(public sender: HTMLElement, public viewModel: any, public viewModelName: string, public validationTargetPath: any, public serverResponseObject: any) {
        super(viewModel);
    }
}
class DotvvmSpaNavigatingEventArgs extends DotvvmEventArgs {
    public cancel: boolean = false;
    constructor(public viewModel: any, public viewModelName: string, public newUrl: string) {
        super(viewModel);
    }
}
class DotvvmSpaNavigatedEventArgs extends DotvvmEventArgs {
    public isHandled: boolean = false;
    constructor(public viewModel: any, public viewModelName: string, public serverResponseObject: any) {
        super(viewModel);
    }
}

var dotvvm = new DotVVM();


// add knockout binding handler for update progress control
ko.bindingHandlers["dotvvmpdateProgressVisible"] = {
    init(element: any, valueAccessor: () => any, allBindingsAccessor: KnockoutAllBindingsAccessor, viewModel: any, bindingContext: KnockoutBindingContext) {
        element.style.display = "none";
        dotvvm.events.beforePostback.subscribe(e => {
            element.style.display = "";
        });
        dotvvm.events.spaNavigating.subscribe(e => {
            element.style.display = "";
        });
        dotvvm.events.afterPostback.subscribe(e => {
            element.style.display = "none";
        });
        dotvvm.events.spaNavigated.subscribe(e => {
            element.style.display = "none";
        });
        dotvvm.events.error.subscribe(e => {
            element.style.display = "none";
        });
    }
};
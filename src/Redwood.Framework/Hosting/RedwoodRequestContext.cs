﻿using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Newtonsoft.Json.Linq;
using Redwood.Framework.Configuration;
using Redwood.Framework.Controls;
using Redwood.Framework.Parser;
using Redwood.Framework.Routing;
using Redwood.Framework.ResourceManagement;
using Redwood.Framework.Runtime;

namespace Redwood.Framework.Hosting
{
    public class RedwoodRequestContext
    {
        internal string CsrfToken { get; set; }

        /// <summary>
        /// Gets the underlying <see cref="HttpContext"/> object for this HTTP request.
        /// </summary>
        public HttpContext HttpContext { get; internal set; }

        /// <summary>
        /// Gets the <see cref="IRedwoodPresenter"/> that is responsible for handling this HTTP request.
        /// </summary>
        public IRedwoodPresenter Presenter { get; internal set; }

        /// <summary>
        /// Gets the global configuration of Redwood.
        /// </summary>
        public RedwoodConfiguration Configuration { get; internal set; }

        /// <summary>
        /// Gets the route that was used for this request.
        /// </summary>
        public RouteBase Route { get; internal set; }

        /// <summary>
        /// Determines whether this HTTP request is a postback or a classic GET request.
        /// </summary>
        public bool IsPostBack { get; internal set; }

        /// <summary>
        /// Gets the values of parameters specified in the <see cref="P:Route" /> property.
        /// </summary>
        public IDictionary<string, object> Parameters { get; set; }

        /// <summary>
        /// Gets the resource manager that is responsible for rendering script and stylesheet resources.
        /// </summary>
        public ResourceManager ResourceManager { get; internal set; }

        /// <summary>
        /// Gets the view model object for the current HTTP request.
        /// </summary>
        public object ViewModel { get; internal set; }

        /// <summary>
        /// Gets the <see cref="ModelState"/> object that manages validation errors for the viewmodel.
        /// </summary>
        public ModelState ModelState { get; private set; }

        internal Dictionary<string, string> PostBackUpdatedControls { get; private set; }

        internal string RenderedHtml { get; set; }

        internal JObject ViewModelJson { get; set; }

        internal JObject ReceivedViewModelJson { get; set; }

        /// <summary>
        /// Gets the query string parameters specified in the URL of the current HTTP request.
        /// </summary>
        public IReadableStringCollection Query
        {
            get
            {
                return HttpContext.Request.Query;
            }
        }

        /// <summary>
        /// Gets or sets the value indiciating whether the exception that occured in the command execution was handled. 
        /// This property is typically set from the exception filter.
        /// </summary>
        public bool IsCommandExceptionHandled { get; set; }

        /// <summary>
        /// Gets a value indicating whether the HTTP request wants to render only content of a specific SpaContentPlaceHolder.
        /// </summary>
        public bool IsSpaRequest
        {
            get { return RedwoodPresenter.DetermineSpaRequest(HttpContext); }
        }

        /// <summary>
        /// Gets a value indicating whether this HTTP request is made from single page application and only the SpaContentPlaceHolder content will be rendered.
        /// </summary>
        public bool IsInPartialRenderingMode
        {
            get { return RedwoodPresenter.DeterminePartialRendering(HttpContext); }
        }

        /// <summary>
        /// Gets the unique id of the SpaContentPlaceHolder that should be loaded.
        /// </summary>
        public string GetSpaContentPlaceHolderUniqueId()
        {
            return RedwoodPresenter.DetermineSpaContentPlaceHolderUniqueId(HttpContext);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RedwoodRequestContext"/> class.
        /// </summary>
        public RedwoodRequestContext()
        {
            ModelState = new ModelState();
            PostBackUpdatedControls = new Dictionary<string, string>();
        }

        /// <summary>
        /// Changes the current culture of this HTTP request.
        /// </summary>
        public void ChangeCurrentCulture(string cultureName)
        {
            CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = new CultureInfo(cultureName);
        }

        /// <summary>
        /// Interrupts the execution of the current request.
        /// </summary>
        public void InterruptRequest()
        {
            throw new RedwoodInterruptRequestExecutionException();    
        }

        /// <summary>
        /// Returns the redirect response and interrupts the execution of current request.
        /// </summary>
        public void Redirect(string url)
        {
            SetRedirectResponse(HttpContext, url, (int)HttpStatusCode.Redirect);
            InterruptRequest();
        }

        /// <summary>
        /// Returns the redirect response and interrupts the execution of current request.
        /// </summary>
        public void Redirect(string routeName, object newRouteValues)
        {
            var route = Configuration.RouteTable[routeName];
            var url = route.BuildUrl(Parameters, newRouteValues);
            Redirect(url);
        }

        /// <summary>
        /// Returns the permanent redirect response and interrupts the execution of current request.
        /// </summary>
        public void RedirectPermanent(string url)
        {
            SetRedirectResponse(HttpContext, url, (int)HttpStatusCode.MovedPermanently);
            InterruptRequest();
        }

        /// <summary>
        /// Returns the permanent redirect response and interrupts the execution of current request.
        /// </summary>
        public void RedirectPermanent(string routeName, object newRouteValues)
        {
            var route = Configuration.RouteTable[routeName];
            var url = route.BuildUrl(Parameters, newRouteValues);
            RedirectPermanent(url);
        }

        /// <summary>
        /// Renders the redirect response.
        /// </summary>
        public static void SetRedirectResponse(HttpContext OwinContext, string url, int statusCode)
        {
            if (!RedwoodPresenter.DeterminePartialRendering(OwinContext))
            {
                OwinContext.Response.Headers["Location"] = url;
                OwinContext.Response.StatusCode = statusCode;
            }
            else
            {
                OwinContext.Response.StatusCode = 200;
                OwinContext.Response.ContentType = "application/json";
                Task.Run(async () => await OwinContext.Response.WriteAsync(DefaultViewModelSerializer.GenerateRedirectActionResponse(url)));
            }
        }

        /// <summary>
        /// Ends the request execution when the <see cref="ModelState"/> is not valid and displays the validation errors in <see cref="ValidationSummary"/> control.
        /// If it is, it does nothing.
        /// </summary>
        public void FailOnInvalidModelState()
        {
            if (!ModelState.IsValid)
            {
                HttpContext.Response.ContentType = "application/json";
                Task.Run(async () => await HttpContext.Response.WriteAsync(Presenter.ViewModelSerializer.SerializeModelState(this)));
                throw new RedwoodInterruptRequestExecutionException("The ViewModel contains validation errors!");
            }
        }

        /// <summary>
        /// Gets the serialized view model.
        /// </summary>
        internal string GetSerializedViewModel()
        {
            return Presenter.ViewModelSerializer.SerializeViewModel(this);
        }
    }
}

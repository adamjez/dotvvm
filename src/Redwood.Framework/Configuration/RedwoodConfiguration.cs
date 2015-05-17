﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using Newtonsoft.Json;
using Redwood.Framework.Hosting;
using Redwood.Framework.Routing;
using Redwood.Framework.Parser;
using Redwood.Framework.ResourceManagement;
using Redwood.Framework.Runtime;
using Redwood.Framework.Runtime.Compilation;
using Redwood.Framework.Runtime.Filters;
using Redwood.Framework.Security;
using Redwood.Framework.ResourceManagement.ClientGlobalize;
using Microsoft.AspNet.Hosting;
using Microsoft.Framework.DependencyInjection;

namespace Redwood.Framework.Configuration
{
    public class RedwoodConfiguration
    {
        public const string RedwoodControlTagPrefix = "rw";

        /// <summary>
        /// Gets or sets the application physical path.
        /// </summary>
        [JsonIgnore]
        public string ApplicationPhysicalPath
        {
            get { return HostingEnvironment.WebRootPath; }
        }

        /// <summary>
        /// Gets the hosting environment instance.
        /// </summary>
        public IHostingEnvironment HostingEnvironment { get; internal set; }

        /// <summary>
        /// Gets the settings of the markup.
        /// </summary>
        [JsonProperty("markup", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public RedwoodMarkupConfiguration Markup { get; private set; }

        /// <summary>
        /// Gets the route table.
        /// </summary>
        [JsonIgnore]
        public RedwoodRouteTable RouteTable { get; private set; }

        /// <summary>
        /// Gets the configuration of resources.
        /// </summary>
        [JsonIgnore()]
        public RedwoodResourceRepository Resources { get; private set; }

        /// <summary>
        /// Gets the security configuration.
        /// </summary>
        [JsonProperty("security")]
        public RedwoodSecurityConfiguration Security { get; private set; }

        /// <summary>
        /// Gets the runtime configuration.
        /// </summary>
        [JsonProperty("runtime", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public RedwoodRuntimeConfiguration Runtime { get; private set; }

        /// <summary>
        /// Gets or sets the default culture.
        /// </summary>
        [JsonProperty("defaultCulture", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string DefaultCulture { get; set; }

        /// <summary>
        /// Gets or sets whether the application should run in debug mode.
        /// </summary>
        [JsonProperty("debug", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool Debug { get; set; }
        

        /// <summary>
        /// Gets the service provider.
        /// </summary>
        [JsonIgnore]
        public IServiceProvider ServiceProvider { get; internal set; }

        /// <summary>
        /// Gets a virtual directory in which the application runs (e.g. "/myApp").
        /// </summary>
        [JsonIgnore]
        public string VirtualDirectory { get; internal set; }

        internal AssemblyHelper AssemblyHelper { get; set; }


        /// <summary>
        /// Initializes a new instance of the <see cref="RedwoodConfiguration"/> class.
        /// </summary>
        internal RedwoodConfiguration()
        {
            DefaultCulture = CultureInfo.CurrentCulture.Name;
            Markup = new RedwoodMarkupConfiguration();
            RouteTable = new RedwoodRouteTable(this);
            Resources = new RedwoodResourceRepository();
            Security = new RedwoodSecurityConfiguration();
            Runtime = new RedwoodRuntimeConfiguration();
            Debug = true;
        }

        /// <summary>
        /// Creates the default configuration.
        /// </summary>
        public static RedwoodConfiguration CreateDefault()
        {
            var configuration = new RedwoodConfiguration();

            configuration.Runtime.GlobalFilters.Add(new ModelValidationFilterAttribute());
            
            configuration.Markup.Controls.AddRange(new[]
            {
                new RedwoodControlConfiguration() { TagPrefix = "rw", Namespace = "Redwood.Framework.Controls", Assembly = "Redwood.Framework" },
                new RedwoodControlConfiguration() { TagPrefix = "bootstrap", Namespace = "Redwood.Framework.Controls.Bootstrap", Assembly = "Redwood.Framework" },
            });

            configuration.Resources.Register(Constants.JQueryResourceName,
                new ScriptResource()
                {
                    CdnUrl = "https://code.jquery.com/jquery-2.1.1.min.js",
                    Url = "Redwood.Framework.Resources.Scripts.jquery-2.1.1.min.js",
                    EmbeddedResourceAssembly = typeof(RedwoodConfiguration).GetTypeInfo().Assembly.GetName().Name,
                    GlobalObjectName = "$"
                });
            configuration.Resources.Register(Constants.KnockoutJSResourceName,
                new ScriptResource()
                {
                    Url = "Redwood.Framework.Resources.Scripts.knockout-3.2.0.js",
                    EmbeddedResourceAssembly = typeof(RedwoodConfiguration).GetTypeInfo().Assembly.GetName().Name,
                    GlobalObjectName = "ko"
                });
            configuration.Resources.Register(Constants.KnockoutMapperResourceName,
                new ScriptResource()
                {
                    Url = "Redwood.Framework.Resources.Scripts.knockout.mapper.js",
                    EmbeddedResourceAssembly = typeof(RedwoodConfiguration).GetTypeInfo().Assembly.GetName().Name,
                    GlobalObjectName = "ko.mapper",
                    Dependencies = new[] { Constants.KnockoutJSResourceName }
                });
            configuration.Resources.Register(Constants.RedwoodResourceName,
                new ScriptResource()
                {
                    Url = "Redwood.Framework.Resources.Scripts.Redwood.js",
                    EmbeddedResourceAssembly = typeof(RedwoodConfiguration).GetTypeInfo().Assembly.GetName().Name,
                    GlobalObjectName = "redwood",
                    Dependencies = new[] { Constants.KnockoutJSResourceName, Constants.KnockoutMapperResourceName }
                });
            configuration.Resources.Register(Constants.RedwoodValidationResourceName,
                new ScriptResource()
                {
                    Url = "Redwood.Framework.Resources.Scripts.Redwood.Validation.js",
                    EmbeddedResourceAssembly = typeof(RedwoodConfiguration).GetTypeInfo().Assembly.GetName().Name,
                    GlobalObjectName = "redwood.validation",
                    Dependencies = new[] { Constants.RedwoodResourceName }
                });
            configuration.Resources.Register(Constants.RedwoodDebugResourceName,
                new ScriptResource()
                {
                    Url = "Redwood.Framework.Resources.Scripts.Redwood.Debug.js",
                    EmbeddedResourceAssembly = typeof(RedwoodConfiguration).GetTypeInfo().Assembly.GetName().Name,
                    Dependencies = new[] { Constants.RedwoodResourceName, Constants.JQueryResourceName }
                });
            configuration.Resources.Register(Constants.BootstrapResourceName,
                new ScriptResource()
                {
                    Url = "/Scripts/bootstrap.min.js",
                    CdnUrl = "https://maxcdn.bootstrapcdn.com/bootstrap/3.3.1/js/bootstrap.min.js",
                    GlobalObjectName = "typeof $().emulateTransitionEnd == 'function'",
                    Dependencies = new[] { Constants.BootstrapCssResourceName, Constants.JQueryResourceName }
                });
            configuration.Resources.Register(Constants.BootstrapCssResourceName,
                new StylesheetResource()
                {
                    Url = "/Content/bootstrap.min.css"
                });

            configuration.Resources.Register(Constants.RedwoodFileUploadResourceName, 
                new ScriptResource()
                {
                    Url = "Redwood.Framework.Resources.Scripts.Redwood.FileUpload.js",
                    EmbeddedResourceAssembly = typeof(RedwoodConfiguration).GetTypeInfo().Assembly.GetName().Name,
                    Dependencies = new[] { Constants.RedwoodResourceName }
                });
            configuration.Resources.Register(Constants.RedwoodFileUploadCssResourceName,
                new StylesheetResource()
                {
                    Url = "Redwood.Framework.Resources.Scripts.Redwood.FileUpload.css",
                    EmbeddedResourceAssembly = typeof(RedwoodConfiguration).GetTypeInfo().Assembly.GetName().Name,
                    Dependencies = new[] { Constants.RedwoodFileUploadResourceName }
                });

            RegisterGlobalizeResources(configuration);

            return configuration;
        }


        private static void RegisterGlobalizeResources(RedwoodConfiguration configuration)
        {
            configuration.Resources.Register(Constants.GlobalizeResourceName, new ScriptResource()
            {
                Url = "Redwood.Framework.Resources.Scripts.Globalize.globalize.js",
                EmbeddedResourceAssembly = typeof(RedwoodConfiguration).GetTypeInfo().Assembly.GetName().Name
            });


            configuration.Resources.RegisterNamedParent("globalize", new JQueryGlobalizeResourceRepository());
        }

    }
}

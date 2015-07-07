using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using Newtonsoft.Json;
using DotVVM.Framework.Hosting;
using DotVVM.Framework.Routing;
using DotVVM.Framework.Parser;
using DotVVM.Framework.ResourceManagement;
using DotVVM.Framework.Runtime;
using DotVVM.Framework.Runtime.Compilation;
using DotVVM.Framework.Runtime.Filters;
using DotVVM.Framework.Security;
using DotVVM.Framework.ResourceManagement.ClientGlobalize;
using Microsoft.AspNet.Hosting;
using Microsoft.Framework.DependencyInjection;

namespace DotVVM.Framework.Configuration
{
    public class DotvvmConfiguration
    {
        public const string DotvvmControlTagPrefix = "rw";
        private const string ResourceNamePrefix = "Resources/Scripts/";

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
        public DotvvmMarkupConfiguration Markup { get; private set; }

        /// <summary>
        /// Gets the route table.
        /// </summary>
        [JsonIgnore]
        public DotvvmRouteTable RouteTable { get; private set; }

        /// <summary>
        /// Gets the configuration of resources.
        /// </summary>
        [JsonIgnore()]
        public DotvvmResourceRepository Resources { get; private set; }

        /// <summary>
        /// Gets the security configuration.
        /// </summary>
        [JsonProperty("security")]
        public DotvvmSecurityConfiguration Security { get; private set; }

        /// <summary>
        /// Gets the runtime configuration.
        /// </summary>
        [JsonProperty("runtime", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public DotvvmRuntimeConfiguration Runtime { get; private set; }

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
        /// Initializes a new instance of the <see cref="DotvvmConfiguration"/> class.
        /// </summary>
        internal DotvvmConfiguration()
        {
            DefaultCulture = CultureInfo.CurrentCulture.Name;
            Markup = new DotvvmMarkupConfiguration();
            RouteTable = new DotvvmRouteTable(this);
            Resources = new DotvvmResourceRepository();
            Security = new DotvvmSecurityConfiguration();
            Runtime = new DotvvmRuntimeConfiguration();
            Debug = true;
        }

        /// <summary>
        /// Creates the default configuration.
        /// </summary>
        public static DotvvmConfiguration CreateDefault()
        {
            var configuration = new DotvvmConfiguration();

            configuration.Runtime.GlobalFilters.Add(new ModelValidationFilterAttribute());
            
            configuration.Markup.Controls.AddRange(new[]
            {
                new DotvvmControlConfiguration() { TagPrefix = "rw", Namespace = "DotVVM.Framework.Controls", Assembly = "DotVVM.Framework" },
                new DotvvmControlConfiguration() { TagPrefix = "bootstrap", Namespace = "DotVVM.Framework.Controls.Bootstrap", Assembly = "DotVVM.Framework" },
            });

            configuration.Resources.Register(Constants.JQueryResourceName,
                new ScriptResource()
                {
                    CdnUrl = "https://code.jquery.com/jquery-2.1.1.min.js",
                    Url = ResourceNamePrefix + "jquery-2.1.1.min.js",
                    EmbeddedResourceAssembly = typeof(DotvvmConfiguration).GetTypeInfo().Assembly.GetName().Name,
                    GlobalObjectName = "$"
                });
            configuration.Resources.Register(Constants.KnockoutJSResourceName,
                new ScriptResource()
                {
                    Url = ResourceNamePrefix + "knockout-3.2.0.js",
                    EmbeddedResourceAssembly = typeof(DotvvmConfiguration).GetTypeInfo().Assembly.GetName().Name,
                    GlobalObjectName = "ko"
                });
            configuration.Resources.Register(Constants.KnockoutMapperResourceName,
                new ScriptResource()
                {
                    Url = ResourceNamePrefix + "knockout.mapper.js",
                    EmbeddedResourceAssembly = typeof(DotvvmConfiguration).GetTypeInfo().Assembly.GetName().Name,
                    GlobalObjectName = "ko.mapper",
                    Dependencies = new[] { Constants.KnockoutJSResourceName }
                });
            configuration.Resources.Register(Constants.DotvvmResourceName,
                new ScriptResource()
                {
                    Url = ResourceNamePrefix + "DotVVM.js",
                    EmbeddedResourceAssembly = typeof(DotvvmConfiguration).GetTypeInfo().Assembly.GetName().Name,
                    GlobalObjectName = "dotvvm",
                    Dependencies = new[] { Constants.KnockoutJSResourceName, Constants.KnockoutMapperResourceName }
                });
            configuration.Resources.Register(Constants.DotvvmValidationResourceName,
                new ScriptResource()
                {
                    Url = ResourceNamePrefix + "DotVVM.Validation.js",
                    EmbeddedResourceAssembly = typeof(DotvvmConfiguration).GetTypeInfo().Assembly.GetName().Name,
                    GlobalObjectName = "dotvvm.validation",
                    Dependencies = new[] { Constants.DotvvmResourceName }
                });
            configuration.Resources.Register(Constants.DotvvmDebugResourceName,
                new ScriptResource()
                {
                    Url = ResourceNamePrefix + "DotVVM.Debug.js",
                    EmbeddedResourceAssembly = typeof(DotvvmConfiguration).GetTypeInfo().Assembly.GetName().Name,
                    Dependencies = new[] { Constants.DotvvmResourceName, Constants.JQueryResourceName }
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

            configuration.Resources.Register(Constants.DotvvmFileUploadResourceName, 
                new ScriptResource()
                {
                    Url = ResourceNamePrefix + "DotVVM.FileUpload.js",
                    EmbeddedResourceAssembly = typeof(DotvvmConfiguration).GetTypeInfo().Assembly.GetName().Name,
                    Dependencies = new[] { Constants.DotvvmResourceName }
                });
            configuration.Resources.Register(Constants.DotvvmFileUploadCssResourceName,
                new StylesheetResource()
                {
                    Url = ResourceNamePrefix + "DotVVM.FileUpload.css",
                    EmbeddedResourceAssembly = typeof(DotvvmConfiguration).GetTypeInfo().Assembly.GetName().Name,
                    Dependencies = new[] { Constants.DotvvmFileUploadResourceName }
                });

            RegisterGlobalizeResources(configuration);

            return configuration;
        }


        private static void RegisterGlobalizeResources(DotvvmConfiguration configuration)
        {
            configuration.Resources.Register(Constants.GlobalizeResourceName, new ScriptResource()
            {
                Url = ResourceNamePrefix + "Globalize/globalize.js",
                EmbeddedResourceAssembly = typeof(DotvvmConfiguration).GetTypeInfo().Assembly.GetName().Name
            });


            configuration.Resources.RegisterNamedParent("globalize", new JQueryGlobalizeResourceRepository());
        }

    }
}

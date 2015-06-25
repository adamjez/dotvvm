using System.IO;
using System.Reflection;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Runtime;
using Newtonsoft.Json;
using Redwood.Framework.Configuration;
using Redwood.Framework.Hosting;
using Redwood.Framework.Runtime;
using Redwood.Framework.Runtime.Compilation;
using Redwood.Framework.Security;

namespace Redwood.Framework
{
    public static class AspNetExtensions
    {

        public static RedwoodConfiguration UseRedwood(this IApplicationBuilder app, RedwoodConfiguration configuration, IHostingEnvironment hostingEnvironment, Assembly mainApplicationAssembly = null, string virtualDirectory = "")
        {
            if (virtualDirectory.StartsWith("/"))
            {
                virtualDirectory = virtualDirectory.Substring(1);
            }

            var configurationFilePath = Path.Combine(hostingEnvironment.WebRootPath, "redwood.json");
            
            // load or create default configuration
            if (File.Exists(configurationFilePath))
            {
                var fileContents = File.ReadAllText(configurationFilePath);
                JsonConvert.PopulateObject(fileContents, configuration);
            }
            configuration.HostingEnvironment = hostingEnvironment;
            configuration.VirtualDirectory = virtualDirectory;
            configuration.ServiceProvider = app.ApplicationServices;
            configuration.AssemblyHelper = new AssemblyHelper(configuration);

            if (mainApplicationAssembly != null)
            {
                configuration.Markup.AddAssembly(mainApplicationAssembly.FullName);
            }

            // add middlewares
            app.UseMiddleware<RedwoodErrorPageMiddleware>();

            app.UseMiddleware<RedwoodRestrictedStaticFilesMiddleware>();
            app.UseMiddleware<RedwoodEmbeddedResourceMiddleware>();
            app.UseMiddleware<RedwoodFileUploadMiddleware>(configuration);
            app.UseMiddleware<JQueryGlobalizeCultureMiddleware>();

            app.UseMiddleware<RedwoodMiddleware>(configuration);
            
            return configuration;
        }

        public static void AddRedwood(this IServiceCollection serviceCollection, RedwoodConfiguration configuration)
        {
            serviceCollection.AddSingleton<RedwoodConfiguration>(provider => configuration);
            serviceCollection.AddSingleton<IViewModelProtector>(provider => new DefaultViewModelProtector());
            serviceCollection.AddSingleton<ICsrfProtector>(provider => new DefaultCsrfProtector());
            serviceCollection.AddSingleton<IRedwoodViewBuilder>(provider => new DefaultRedwoodViewBuilder(configuration));
            serviceCollection.AddSingleton<IViewModelLoader>(provider => new DefaultViewModelLoader());
            serviceCollection.AddSingleton<IViewModelSerializer>(provider => new DefaultViewModelSerializer(configuration) { SendDiff = true });
            serviceCollection.AddSingleton<IOutputRenderer>(provider => new DefaultOutputRenderer());
            serviceCollection.AddSingleton<IRedwoodPresenter>(provider => new RedwoodPresenter(configuration));
            serviceCollection.AddSingleton<IMarkupFileLoader>(provider => new DefaultMarkupFileLoader());
            serviceCollection.AddSingleton<IControlBuilderFactory>(provider => new DefaultControlBuilderFactory(configuration));
            serviceCollection.AddSingleton<IControlResolver>(provider => new DefaultControlResolver(configuration));
            serviceCollection.AddSingleton<IAssemblyMetadataCache>(provider => new AssemblyMetadataCache(configuration));
            serviceCollection.AddTransient<IViewCompiler>(provider => new DefaultViewCompiler(configuration));
        }
           
    }
}

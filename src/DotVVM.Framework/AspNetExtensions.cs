using System.IO;
using System.Reflection;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Runtime;
using Newtonsoft.Json;
using DotVVM.Framework.Configuration;
using DotVVM.Framework.Hosting;
using DotVVM.Framework.Runtime;
using DotVVM.Framework.Runtime.Compilation;
using DotVVM.Framework.Security;

namespace DotVVM.Framework
{
    public static class AspNetExtensions
    {

        public static DotvvmConfiguration UseDotVVM(this IApplicationBuilder app, DotvvmConfiguration configuration, IHostingEnvironment hostingEnvironment, Assembly mainApplicationAssembly = null, string virtualDirectory = "")
        {
            if (virtualDirectory.StartsWith("/"))
            {
                virtualDirectory = virtualDirectory.Substring(1);
            }

            var configurationFilePath = Path.Combine(hostingEnvironment.WebRootPath, "dotvvm.json");
            
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
            app.UseMiddleware<DotvvmErrorPageMiddleware>();

            app.UseMiddleware<DotvvmRestrictedStaticFilesMiddleware>();
            app.UseMiddleware<DotvvmEmbeddedResourceMiddleware>();
            app.UseMiddleware<DotvvmFileUploadMiddleware>(configuration);
            app.UseMiddleware<JQueryGlobalizeCultureMiddleware>();

            app.UseMiddleware<DotvvmMiddleware>(configuration);
            
            return configuration;
        }

        public static void AddDotVVM(this IServiceCollection serviceCollection, DotvvmConfiguration configuration)
        {
            serviceCollection.AddSingleton<DotvvmConfiguration>(provider => configuration);
            serviceCollection.AddSingleton<IViewModelProtector>(provider => new DefaultViewModelProtector());
            serviceCollection.AddSingleton<ICsrfProtector>(provider => new DefaultCsrfProtector());
            serviceCollection.AddSingleton<IDotvvmViewBuilder>(provider => new DefaultDotvvmViewBuilder(configuration));
            serviceCollection.AddSingleton<IViewModelLoader>(provider => new DefaultViewModelLoader());
            serviceCollection.AddSingleton<IViewModelSerializer>(provider => new DefaultViewModelSerializer(configuration) { SendDiff = true });
            serviceCollection.AddSingleton<IOutputRenderer>(provider => new DefaultOutputRenderer());
            serviceCollection.AddSingleton<IDotvvmPresenter>(provider => new DotvvmPresenter(configuration));
            serviceCollection.AddSingleton<IMarkupFileLoader>(provider => new DefaultMarkupFileLoader());
            serviceCollection.AddSingleton<IControlBuilderFactory>(provider => new DefaultControlBuilderFactory(configuration));
            serviceCollection.AddSingleton<IControlResolver>(provider => new DefaultControlResolver(configuration));
            serviceCollection.AddSingleton<IAssemblyMetadataCache>(provider => new AssemblyMetadataCache(configuration));
            serviceCollection.AddTransient<IViewCompiler>(provider => new DefaultViewCompiler(configuration));
        }
           
    }
}

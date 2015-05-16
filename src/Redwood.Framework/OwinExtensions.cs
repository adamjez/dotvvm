using System.IO;
using System.Reflection;
using Microsoft.AspNet.Builder;
using Newtonsoft.Json;
using Redwood.Framework.Configuration;
using Redwood.Framework.Hosting;

namespace Redwood.Framework
{
    public static class OwinExtensions
    {

        public static RedwoodConfiguration UseRedwood(this IApplicationBuilder app, string applicationRootDirectory, string virtualDirectory = "")
        {
            if (virtualDirectory.StartsWith("/"))
            {
                virtualDirectory = virtualDirectory.Substring(1);
            }

            var configurationFilePath = Path.Combine(applicationRootDirectory, "redwood.json");
            
            // load or create default configuration
            var configuration = RedwoodConfiguration.CreateDefault();
            if (File.Exists(configurationFilePath))
            {
                var fileContents = File.ReadAllText(configurationFilePath);
                JsonConvert.PopulateObject(fileContents, configuration);
            }
            configuration.ApplicationPhysicalPath = applicationRootDirectory;
            configuration.VirtualDirectory = virtualDirectory;
            configuration.Markup.AddAssembly(Assembly.GetCallingAssembly().FullName);
            
            // add middlewares
            app.UseMiddleware<RedwoodErrorPageMiddleware>();

            app.UseMiddleware<RedwoodRestrictedStaticFilesMiddleware>();
            app.UseMiddleware<RedwoodEmbeddedResourceMiddleware>();
            app.UseMiddleware<RedwoodFileUploadMiddleware>(configuration);
            app.UseMiddleware<JQueryGlobalizeCultureMiddleware>();

            app.UseMiddleware<RedwoodMiddleware>(configuration);
            
            return configuration;
        }
        
    }
}

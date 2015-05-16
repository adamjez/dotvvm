using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Redwood.Framework.Parser;

namespace Redwood.Framework.Hosting
{
    /// <summary>
    /// Provides access to embedded resources in the Redwood.Framework assembly.
    /// </summary>
    public class RedwoodEmbeddedResourceMiddleware
    {
        public RequestDelegate Next { get; private set; }

        public RedwoodEmbeddedResourceMiddleware(RequestDelegate next)
        {
            Next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            // try resolve the route
            var url = context.Request.Path.Value.TrimStart('/').TrimEnd('/');

            // disable access to the redwood.json file
            if (url.StartsWith("redwood.json", StringComparison.CurrentCultureIgnoreCase))
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                throw new UnauthorizedAccessException("The redwood.json cannot be served!");
            }

            // embedded resource handler URL
            if (url.StartsWith(Constants.ResourceHandlerMatchUrl))
            {
                RenderEmbeddedResource(context);
            }
            else
            {
                await Next.Invoke(context);
            }
        }



        /// <summary>
        /// Renders the embedded resource.
        /// </summary>
        private void RenderEmbeddedResource(HttpContext context)
        {
            context.Response.StatusCode = (int)HttpStatusCode.OK;

            var resourceName = context.Request.Query["name"];
            var assembly = Assembly.Load(new AssemblyName(context.Request.Query["assembly"]));

            if (resourceName.EndsWith(".js"))
            {
                context.Response.ContentType = "text/javascript";
            }
            else if (resourceName.EndsWith(".css"))
            {
                context.Response.ContentType = "text/css";
            }
            else
            {
                context.Response.ContentType = "application/octet-stream";
            }

            using (var resourceStream = assembly.GetManifestResourceStream(resourceName))
            {
                resourceStream.CopyTo(context.Response.Body);
            }
        }
    }
}

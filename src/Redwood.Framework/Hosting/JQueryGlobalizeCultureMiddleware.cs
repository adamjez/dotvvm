using Redwood.Framework.Parser;
using Redwood.Framework.ResourceManagement.ClientGlobalize;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;

namespace Redwood.Framework.Hosting
{
    public class JQueryGlobalizeCultureMiddleware
    {
        public RequestDelegate Next { get; private set; }

        public JQueryGlobalizeCultureMiddleware(RequestDelegate next)
        {
            Next = next;
        }

        public Task Invoke(HttpContext context)
        {
            var url = context.Request.Path.Value.TrimStart('/').TrimEnd('/');
            
            if (url.StartsWith(Constants.GlobalizeCultureUrlPath))
            {
                return RenderResponse(context);
            }
            else
            {
                return Next.Invoke(context);
            }
        }



        /// <summary>
        /// Renders the embedded resource.
        /// </summary>
        private Task RenderResponse(HttpContext context)
        {
            context.Response.StatusCode = (int)HttpStatusCode.OK;
            context.Response.ContentType = "text/javascript";

            var id = context.Request.Query[Constants.GlobalizeCultureUrlIdParameter];

            var js = JQueryGlobalizeScriptCreator.BuildCultureInfoScript(new CultureInfo(id));

            return context.Response.WriteAsync(js);
        }
    }
}

using Redwood.Framework.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;

namespace Redwood.Framework.Hosting
{
    public class RedwoodErrorPageMiddleware
    {
        public RequestDelegate Next { get; private set; }

        public RedwoodErrorPageMiddleware(RequestDelegate next)
        {
            Next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            Exception error = null;
            try
            {
                await Next.Invoke(context);
            }
            catch (Exception ex)
            {
                error = ex;
            }

            if (error != null)
            {
                context.Response.StatusCode = 500;
                await RenderErrorResponse(context, error);
            }
        }

        /// <summary>
        /// Renders the error response.
        /// </summary>
        public static Task RenderErrorResponse(HttpContext context, Exception error)
        {
            context.Response.ContentType = "text/html";

            var template = new ErrorPageTemplate()
            {
                Exception = error,
                ErrorCode = context.Response.StatusCode,
                ErrorDescription = ((HttpStatusCode)context.Response.StatusCode).ToString(),
                IpAddress = context.GetFeature<IHttpConnectionFeature>().RemoteIpAddress.ToString(),
                CurrentUserName = context.User != null ? context.User.Identity.Name : "",
                Url = context.Request.GetAbsoluteUrl(),
                Verb = context.Request.Method
            };
            if (error is ParserException)
            {
                template.FileName = ((ParserException)error).FileName;
                template.LineNumber = ((ParserException)error).LineNumber;
                template.PositionOnLine = ((ParserException)error).PositionOnLine;
            }

            var text = template.TransformText();
            return context.Response.WriteAsync(text);
        }
    }
}

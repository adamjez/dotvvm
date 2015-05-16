﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;

namespace Redwood.Framework.Hosting
{
    /// <summary>
    /// Restricts access to static files that shouldn't be downloaded.
    /// </summary>
    public class RedwoodRestrictedStaticFilesMiddleware
    {
        public RequestDelegate Next { get; set; }

        public RedwoodRestrictedStaticFilesMiddleware(RequestDelegate next)
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
            else
            {
                await Next.Invoke(context);
            }
        }
    }
}

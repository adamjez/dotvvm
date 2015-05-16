using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Redwood.Framework.Configuration;
using Redwood.Framework.ResourceManagement;

namespace Redwood.Framework.Hosting
{
    /// <summary>
    /// A middleware that handles Redwood HTTP requests.
    /// </summary>
    public class RedwoodMiddleware 
    {
        private readonly RedwoodConfiguration configuration;

        private const string GooglebotHashbangEscapedFragment = "_escaped_fragment_=";
        public RequestDelegate Next { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RedwoodMiddleware"/> class.
        /// </summary>
        public RedwoodMiddleware(RequestDelegate next, RedwoodConfiguration configuration)
        {
            Next = next;
            this.configuration = configuration;
        }

        /// <summary>
        /// Process an individual request.
        /// </summary>
        public async Task Invoke(HttpContext context)
        {
            // attempt to translate Googlebot hashbang espaced fragment URL to a plain URL string.
            string url;
            if (!TryParseGooglebotHashbangEscapedFragment(context.Request.QueryString, out url))
            {
                url = context.Request.Path.Value;
            }

            // try resolve the route
            url = url.TrimStart('/').TrimEnd('/');

            // handle virtual directory
            if (url.StartsWith(configuration.VirtualDirectory))
            {
                url = url.Substring(configuration.VirtualDirectory.Length);
                url = url.TrimStart('/');

                // find the route
                IDictionary<string, object> parameters = null;
                var route = configuration.RouteTable.FirstOrDefault(r => r.IsMatch(url, out parameters));

                if (route != null)
                {
                    // handle the request
                    await route.ProcessRequest(new RedwoodRequestContext()
                    {
                        Route = route,
                        HttpContext = context,
                        Configuration = configuration,
                        Parameters = parameters,
                        ResourceManager = new ResourceManager(configuration)
                    });
                    return;
                }
            }

            // we cannot handle the request, pass it to another component
            await Next.Invoke(context);
        }

        /// <summary>
        /// Attempts to recognize request made by Googlebot in its effort to crawl links for AJAX SPAs.
        /// </summary>
        /// <param name="queryString">
        /// The query string of the request to try to match the Googlebot hashbang escaped fragment on.
        /// </param>
        /// <param name="url">
        /// The plain URL string that the hasbang escaped fragment represents.
        /// </param>
        /// <returns>
        /// <code>true</code>, if the URL contains valid Googlebot hashbang escaped fragment; otherwise <code>false</code>.
        /// </returns>
        /// <seealso cref="https://developers.google.com/webmasters/ajax-crawling/docs/getting-started"/>
        private bool TryParseGooglebotHashbangEscapedFragment(QueryString queryString, out string url)
        {
            if (queryString.Value.StartsWith(GooglebotHashbangEscapedFragment, StringComparison.Ordinal))
            {
                url = queryString.Value.Substring(GooglebotHashbangEscapedFragment.Length);
                return true;
            }

            url = null;
            return false;
        }
    }
}

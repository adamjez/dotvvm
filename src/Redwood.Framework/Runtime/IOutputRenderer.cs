using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Redwood.Framework.Controls.Infrastructure;
using Redwood.Framework.Hosting;
using Redwood.Framework.Storage;

namespace Redwood.Framework.Runtime
{
    public interface IOutputRenderer
    {

        void RenderPage(RedwoodRequestContext context, RedwoodView view);

        Task WriteHtmlResponse(RedwoodRequestContext context);

        Task WriteViewModelResponse(RedwoodRequestContext context, RedwoodView view);

        Task RenderPlainJsonResponse(HttpContext context, object data);

        Task RenderHtmlResponse(HttpContext context, string html);

        Task RenderPlainTextResponse(HttpContext context, string text);
    }
}
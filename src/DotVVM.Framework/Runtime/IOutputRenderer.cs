using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using DotVVM.Framework.Controls.Infrastructure;
using DotVVM.Framework.Hosting;
using DotVVM.Framework.Storage;

namespace DotVVM.Framework.Runtime
{
    public interface IOutputRenderer
    {

        void RenderPage(DotvvmRequestContext context, DotvvmView view);

        Task WriteHtmlResponse(DotvvmRequestContext context);

        Task WriteViewModelResponse(DotvvmRequestContext context, DotvvmView view);

        Task RenderPlainJsonResponse(HttpContext context, object data);

        Task RenderHtmlResponse(HttpContext context, string html);

        Task RenderPlainTextResponse(HttpContext context, string text);
    }
}
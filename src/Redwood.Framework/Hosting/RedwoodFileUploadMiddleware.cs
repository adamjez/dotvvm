using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.WebUtilities;
using Newtonsoft.Json;
using Redwood.Framework.Configuration;
using Redwood.Framework.Parser;
using Redwood.Framework.Runtime;
using Redwood.Framework.Storage;

namespace Redwood.Framework.Hosting
{
    public class RedwoodFileUploadMiddleware
    {
        public RequestDelegate Next { get; set; }
        private readonly RedwoodConfiguration configuration;


        public RedwoodFileUploadMiddleware(RequestDelegate next, RedwoodConfiguration configuration)
        {
            Next = next;
            this.configuration = configuration;
        }

        public async Task Invoke(HttpContext context)
        {
            // try resolve the route
            var url = context.Request.Path.Value.TrimStart('/').TrimEnd('/');

            // file upload handler
            if (url == Constants.FileUploadHandlerMatchUrl)
            {
                await ProcessMultipartRequest(context);
            }
            else
            {
                await Next.Invoke(context);
            }
        }

        private async Task ProcessMultipartRequest(HttpContext context)
        {
            // verify the request
            var isPost = context.Request.Method == "POST";
            if (isPost && !context.Request.ContentType.StartsWith("multipart/form-data"))
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return;
            }

            var uploadedFiles = new List<UploadedFile>();
            var errorMessage = "";
            if (isPost)
            {
                try
                {
                    // get the boundary
                    var boundary = Regex.Match(context.Request.ContentType, @"boundary=""?(?<boundary>[^\n\;\"" ]*)").Groups["boundary"];
                    if (!boundary.Success || string.IsNullOrWhiteSpace(boundary.Value))
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                        return;
                    }

                    // parse request and save files
                    await SaveFiles(context, boundary, uploadedFiles);
                }
                catch (Exception ex)
                {
                    errorMessage = ex.Message;
                }
            }

            // return the response
            await RenderResponse(context, isPost, errorMessage, uploadedFiles);
        }

        private async Task RenderResponse(HttpContext context, bool isPost, string errorMessage, List<UploadedFile> uploadedFiles)
        {
            var outputRenderer = configuration.ServiceProvider.GetService<IOutputRenderer>();
            if (isPost && context.Request.Headers.Get(Constants.RedwoodFileUploadAsyncHeaderName) == "true")
            {
                // modern browser - return JSON
                if (string.IsNullOrEmpty(errorMessage))
                {
                    await outputRenderer.RenderPlainJsonResponse(context, uploadedFiles);
                }
                else
                {
                    await outputRenderer.RenderPlainTextResponse(context, errorMessage);
                    context.Response.StatusCode = (int) HttpStatusCode.InternalServerError;
                }
            }
            else
            {
                // old browser - return HTML
                var template = new FileUploadPageTemplate();

                if (isPost)
                {
                    if (string.IsNullOrEmpty(errorMessage))
                    {
                        template.StartupScript = string.Format("reportProgress(false, 100, {0})",
                            JsonConvert.SerializeObject(uploadedFiles));
                    }
                    else
                    {
                        template.StartupScript = string.Format("reportProgress(false, 100, {0})",
                            JsonConvert.SerializeObject(errorMessage));
                    }
                }
                await outputRenderer.RenderHtmlResponse(context, template.TransformText());
            }
        }

        private async Task SaveFiles(HttpContext context, Group boundary, List<UploadedFile> uploadedFiles)
        {
            // get the file store
            var fileStore = configuration.ServiceProvider.GetService<IUploadedFileStorage>();

            // parse the stream
            var multiPartReader = new MultipartReader(boundary.Value, context.Request.Body);
            MultipartSection section;
            while ((section = await multiPartReader.ReadNextSectionAsync()) != null)
            {
                // process the section
                var result = await StoreFile(section, fileStore);
                if (result != null)
                {
                    uploadedFiles.Add(result);
                }
            }
        }

        /// <summary>
        /// Stores the file and returns an object that will be sent to the client.
        /// </summary>
        private async Task<UploadedFile> StoreFile(MultipartSection section, IUploadedFileStorage fileStore)
        {
            var fileId = await fileStore.StoreFile(section.Body);
            var fileName = Regex.Match(section.ContentDisposition, @"filename=""?(?<fileName>[^\""]*)", RegexOptions.IgnoreCase).Groups["fileName"];

            return new UploadedFile()
            {
                FileId = fileId,
                FileName = fileName.Success ? fileName.Value : string.Empty
            };
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Http;
using Microsoft.Framework.DependencyInjection;
using DotVVM.Framework;
using DotVVM.Framework.Configuration;
using DotVVM.Framework.Storage;

namespace DotVVM.Samples.BasicSamples
{
    public class Startup
    {
        private DotvvmConfiguration configuration;
        private readonly IHostingEnvironment hostingEnvironment;

        public Startup(IHostingEnvironment hostingEnvironment)
        {
            this.configuration = DotvvmConfiguration.CreateDefault();
            this.hostingEnvironment = hostingEnvironment;
        }

        // For more information on how to configure your application, visit http://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDotVVM(configuration);

            // configure file upload
            var uploadPath = Path.Combine(hostingEnvironment.WebRootPath, "TempUpload");
            services.AddSingleton<IUploadedFileStorage>(provider => new FileSystemUploadedFileStorage(uploadPath, TimeSpan.FromMinutes(30)));
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseDotVVM(
                configuration,
                hostingEnvironment, 
                mainApplicationAssembly: typeof (Startup).GetTypeInfo().Assembly,
                virtualDirectory: ""
            );

            // use DotVVM
            configuration.RouteTable.Add("Sample1", "Sample1", "sample1.dothtml", null);
            configuration.RouteTable.Add("Sample2", "Sample2", "sample2.dothtml", null);
            configuration.RouteTable.Add("Sample3", "Sample3", "sample3.dothtml", null);
            configuration.RouteTable.Add("Sample4", "Sample4", "sample4.dothtml", null);
            configuration.RouteTable.Add("Sample5", "Sample5", "sample5.dothtml", null);
            configuration.RouteTable.Add("Sample6", "Sample6", "sample6.dothtml", null);
            configuration.RouteTable.Add("Sample7", "Sample7", "sample7.dothtml", null);
            configuration.RouteTable.Add("Sample8", "Sample8", "sample8.dothtml", null);
            configuration.RouteTable.Add("Sample9", "Sample9", "sample9.dothtml", null);
            configuration.RouteTable.Add("Sample10", "Sample10", "sample10.dothtml", null);
            configuration.RouteTable.Add("Sample11", "Sample11", "sample11.dothtml", null);
            configuration.RouteTable.Add("Sample12", "Sample12", "sample12.dothtml", null);
            configuration.RouteTable.Add("Sample13", "Sample13", "sample13.dothtml", null);
            configuration.RouteTable.Add("Sample14", "Sample14", "sample14.dothtml", null);
            configuration.RouteTable.Add("Sample15", "Sample15", "sample15.dothtml", null);
            configuration.RouteTable.Add("Sample16", "Sample16", "sample16.dothtml", null);
            configuration.RouteTable.Add("Sample17_SPA", "Sample17", "sample17.dothtml", null);
            configuration.RouteTable.Add("Sample17_A", "Sample17/A/{Id}", "sample17_a.dothtml", null);
            configuration.RouteTable.Add("Sample17_B", "Sample17/B", "sample17_b.dothtml", null);
            configuration.RouteTable.Add("Sample18", "Sample18", "sample18.dothtml", null);
            configuration.RouteTable.Add("Sample19", "Sample19", "sample19.dothtml", null);
        }
    }
}

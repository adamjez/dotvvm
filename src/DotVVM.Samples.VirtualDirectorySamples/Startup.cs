using System;
using System.Threading.Tasks;
using Microsoft.Owin;
using Owin;
using DotVVM.Framework;
using System.Web.Hosting;

[assembly: OwinStartup(typeof(DotVVM.Samples.VirtualDirectorySamples.Startup))]

namespace DotVVM.Samples.VirtualDirectorySamples
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            var config = app.UseDotVVM(HostingEnvironment.ApplicationPhysicalPath, "my/virt/directory");

            config.RouteTable.Add("Sample1", "Sample1", "Views/sample1.dothtml", null);
            config.RouteTable.Add("Sample2", "Sample2", "Views/sample2.dothtml", null);
        }
    }
}

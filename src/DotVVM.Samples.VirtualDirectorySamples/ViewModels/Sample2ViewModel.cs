using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DotVVM.Samples.VirtualDirectorySamples.ViewModels
{
    public class Sample2ViewModel : MasterViewModel
    {

        public string RandomString { get; set; }

        public void Generate()
        {
            RandomString = Guid.NewGuid().ToString();
        }

        public void Redirect()
        {
            Context.Redirect("/Sample1");
        }

    }
}
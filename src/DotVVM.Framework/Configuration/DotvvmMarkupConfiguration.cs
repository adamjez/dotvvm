using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace DotVVM.Framework.Configuration
{
    public class DotvvmMarkupConfiguration
    {

        /// <summary>
        /// Gets the registered control namespaces.
        /// </summary>
        [JsonProperty("controls", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public List<DotvvmControlConfiguration> Controls { get; private set; }

        /// <summary>
        /// Gets or sets the list of referenced assemblies.
        /// </summary>
        [JsonProperty("assemblies", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public List<string> Assemblies { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DotvvmMarkupConfiguration"/> class.
        /// </summary>
        public DotvvmMarkupConfiguration()
        {
            Controls = new List<DotvvmControlConfiguration>();
            Assemblies = new List<string>();
        }


        /// <summary>
        /// Adds the assembly to the list of required assemblies.
        /// </summary>
        public void AddAssembly(string assemblyName)
        {
            if (!Assemblies.Contains(assemblyName))
            {
                Assemblies.Add(assemblyName);
            }
        }
    }
}
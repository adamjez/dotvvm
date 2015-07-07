using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Framework.Runtime;
using DotVVM.Framework.Configuration;

namespace DotVVM.Framework.Hosting
{
    public class AssemblyHelper
    {
        private readonly DotvvmConfiguration configuration;

        public ILibraryManager LibraryManager { get; private set; }
        public IAssemblyLoadContext AssemblyLoadContext { get; private set; }


        public AssemblyHelper(DotvvmConfiguration configuration)
        {
            this.configuration = configuration;
            AssemblyLoadContext = configuration.ServiceProvider.GetService<IAssemblyLoadContextAccessor>().Default;
            LibraryManager = configuration.ServiceProvider.GetService<ILibraryManager>();
        }


        public IEnumerable<Assembly> GetAllAssemblies()
        {
            return LibraryManager.GetLibraries().SelectMany(l => l.LoadableAssemblies).Select(a =>
            {
                try
                {
                    return Assembly.Load(a);
                }
                catch (FileNotFoundException)
                {
                    return null;
                }
            })
            .Where(a => a != null);
        }

        public Assembly LoadAssembly(Stream assemblyStream, Stream pdbStream)
        {
            return AssemblyLoadContext.LoadStream(assemblyStream, pdbStream);
        }
    }
}

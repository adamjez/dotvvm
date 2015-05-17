using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Framework.Runtime;
using Redwood.Framework.Configuration;

namespace Redwood.Framework.Hosting
{
    public class AssemblyHelper
    {
        private readonly RedwoodConfiguration configuration;

        public ILibraryManager LibraryManager { get; private set; }
        public IAssemblyLoadContext AssemblyLoadContext { get; private set; }


        public AssemblyHelper(RedwoodConfiguration configuration)
        {
            this.configuration = configuration;
            AssemblyLoadContext = configuration.ServiceProvider.GetService<IAssemblyLoadContext>();
            LibraryManager = configuration.ServiceProvider.GetService<ILibraryManager>();
        }


        public IEnumerable<Assembly> GetAllAssemblies()
        {
            return LibraryManager.GetLibraries().SelectMany(l => l.LoadableAssemblies.Select(Assembly.Load));
        }

        public Assembly LoadAssembly(Stream assemblyStream, Stream pdbStream)
        {
            return AssemblyLoadContext.LoadStream(assemblyStream, pdbStream);
        }
    }
}

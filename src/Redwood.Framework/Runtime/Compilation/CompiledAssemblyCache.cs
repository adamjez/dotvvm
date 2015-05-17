using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;

namespace Redwood.Framework.Runtime.Compilation
{
    public class CompiledAssemblyCache
    {


        private ConcurrentDictionary<Assembly, MetadataReference> cachedAssemblyMetadata = new ConcurrentDictionary<Assembly, MetadataReference>();
        private ConcurrentDictionary<string, Assembly> cachedAssemblies = new ConcurrentDictionary<string, Assembly>();


        /// <summary>
        /// Gets the <see cref="MetadataReference"/> for the specified assembly.
        /// </summary>
        public MetadataReference GetAssemblyMetadata(Assembly assembly)
        {
            return cachedAssemblyMetadata.GetOrAdd(assembly, MetadataReference.CreateFromAssembly);
        }

        /// <summary>
        /// Adds the assembly to the cache.
        /// </summary>
        internal void AddAssembly(Assembly assembly, CompilationReference compilationReference)
        {
            cachedAssemblyMetadata[assembly] = compilationReference;
            cachedAssemblies[assembly.FullName] = assembly;
        }



        /// <summary>
        /// Gets the singleton instance of the assembly cache.
        /// </summary>
        public static CompiledAssemblyCache Instance { get; private set; }

        static CompiledAssemblyCache()
        {
            Instance = new CompiledAssemblyCache();
        }
    }
}

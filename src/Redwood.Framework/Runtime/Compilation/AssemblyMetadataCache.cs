using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.Framework.Runtime;
using Microsoft.Framework.Runtime.Roslyn;
using Redwood.Framework.Configuration;

namespace Redwood.Framework.Runtime.Compilation
{
    public class AssemblyMetadataCache : IAssemblyMetadataCache
    {

        private readonly ConcurrentDictionary<Assembly, MetadataReference> cachedAssemblyMetadata = new ConcurrentDictionary<Assembly, MetadataReference>();
        private readonly ILibraryManager libraryManager;


        public AssemblyMetadataCache(RedwoodConfiguration configuration)
        {
            libraryManager = configuration.ServiceProvider.GetService<ILibraryManager>();
        }

        /// <summary>
        /// Gets the <see cref="MetadataReference"/> for the specified assembly.
        /// </summary>
        public MetadataReference GetAssemblyMetadata(Assembly assembly)
        {
            return cachedAssemblyMetadata.GetOrAdd(assembly, GetAssemblyMetadataReference);
        }

        private MetadataReference GetAssemblyMetadataReference(Assembly assembly)
        {
            var libraryExport = libraryManager.GetLibraryExport(assembly.GetName().Name);
            var reference = libraryExport.MetadataReferences.First();
            return ConvertMetadataReference(reference);
        }

        /// <summary>
        /// Adds the assembly to the cache.
        /// </summary>
        public void AddAssembly(Assembly assembly, CompilationReference compilationReference)
        {
            cachedAssemblyMetadata[assembly] = compilationReference;
        }


        private MetadataReference ConvertMetadataReference(IMetadataReference metadataReference)
        {
            var roslynReference = metadataReference as IRoslynMetadataReference;

            if (roslynReference != null)
            {
                return roslynReference.MetadataReference;
            }

            var embeddedReference = metadataReference as IMetadataEmbeddedReference;

            if (embeddedReference != null)
            {
                return MetadataReference.CreateFromImage(embeddedReference.Contents);
            }

            var fileMetadataReference = metadataReference as IMetadataFileReference;
            if (fileMetadataReference != null)
            {
                return MetadataReference.CreateFromFile(fileMetadataReference.Path);
            }

            var projectReference = metadataReference as IMetadataProjectReference;
            if (projectReference != null)
            {
                using (var ms = new MemoryStream())
                {
                    projectReference.EmitReferenceAssembly(ms);
                    return MetadataReference.CreateFromImage(ms.ToArray());
                }
            }

            throw new NotSupportedException();
        }

    }
}

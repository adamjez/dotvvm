using System.Reflection;
using Microsoft.CodeAnalysis;

namespace Redwood.Framework.Runtime.Compilation
{
    public interface IAssemblyMetadataCache
    {
        /// <summary>
        /// Gets the <see cref="MetadataReference"/> for the specified assembly.
        /// </summary>
        MetadataReference GetAssemblyMetadata(Assembly assembly);

        /// <summary>
        /// Adds the assembly to the cache.
        /// </summary>
        void AddAssembly(Assembly assembly, CompilationReference compilationReference);
    }
}
using System;
using System.Reflection;
using Eva.Node.Service;

namespace Eva.Node.Loader;

/// <summary>
/// Interface for loading assemblies and extracting service metadata.
/// </summary>
public interface IAssemblyLoader : IDisposable{
    /// <summary>
    /// Loads the service description from the assembly.
    /// </summary>
    /// <returns>The loaded <see cref="ServiceDescription"/>.</returns>
    ServiceDescription LoadDescription();

    /// <summary>
    /// Gets the main type of the service based on the loaded service description.
    /// </summary>
    /// <returns>The main <see cref="Type"/> of the service.</returns>
    Type GetMainType();
}
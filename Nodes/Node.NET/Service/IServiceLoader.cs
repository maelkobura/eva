using System;

namespace Eva.Node.Service;

/// <summary>
/// Interface for loading and providing the nodes EvaService.
/// </summary>
public interface IServiceLoader : IDisposable
{
    /// <summary>
    /// Gets the service description.
    /// </summary>
    ServiceDescription? Description { get; }

    /// <summary>
    /// Loads and returns the service.
    /// </summary>
    /// <returns>The loaded EvaService instance.</returns>
    EvaService LoadService();
}

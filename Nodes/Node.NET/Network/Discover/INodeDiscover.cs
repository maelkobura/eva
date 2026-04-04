using System;
using Eva.Node.Service;

namespace Eva.Node.Network.Discover;

/// <summary>
/// Interface for discovering and authenticating with other nodes in the network.
/// </summary>
public interface INodeDiscover : IDisposable
{
    /// <summary>
    /// Gets the service description of this node.
    /// </summary>
    ServiceDescription Self { get; }

    /// <summary>
    /// Discovers nodes from the tracker and authenticates with them.
    /// </summary>
    /// <param name="firstConnection">Indicates if this is the first connection attempt.</param>
    void Discover(bool firstConnection = false);

    /// <summary>
    /// Authenticates with a specific node by name.
    /// </summary>
    /// <param name="name">The name of the node.</param>
    /// <param name="firstConnection">Indicates if this is the first connection attempt.</param>
    void Authenticate(string name, bool firstConnection = false);
}

using System.Collections.Generic;

namespace Eva.Node.Network;

/// <summary>
/// Interface for managing network nodes.
/// </summary>
public interface INetworkNodeManager : IDisposable{
    /// <summary>
    /// Gets the list of node entities.
    /// </summary>
    List<NodeEntity> Nodes { get; }

    /// <summary>
    /// Resets the certificate for a specific node by name.
    /// </summary>
    /// <param name="nodeName">The name of the node.</param>
    void ResetCertificateForNode(string nodeName);
}

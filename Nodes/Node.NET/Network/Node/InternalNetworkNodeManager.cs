using System.Collections.Generic;
using System.Linq;
using Eva.Commons.Util;
using Microsoft.Extensions.Logging;

namespace Eva.Node.Network;

public class InternalNetworkNodeManager : INetworkNodeManager
{
    private static ILogger logger = EvaLogger.CreateLogger<InternalNetworkNodeManager>();

    public List<NodeEntity> Nodes { get; private set; }
    
    public InternalNetworkNodeManager()
    {
        Nodes = new List<NodeEntity>();
    }
    
    public void ResetCertificateForNode(string nodeName)
    {
        Nodes.First(entity => entity.Name == nodeName).ResetCertificate();
    }

    public void Dispose()
    {}
}
using Eva.Commons.Util;
using Microsoft.Extensions.Logging;

namespace Eva.Node.Network;

public class NetworkNodeManager
{
    public static NetworkNodeManager? Instance { get; private set; }
    private static ILogger logger = EvaLogger.CreateLogger<NetworkNodeManager>();

    public List<NodeEntity> Nodes { get; private set; } = new List<NodeEntity>();
    
    public static void Init()
    {
        if (Instance != null) return;
        Instance = new NetworkNodeManager();
    }
    
    public void ResetCertificateForNode(string nodeName)
    {
        Nodes.First(entity => entity.Name == nodeName).ResetCertificate();
    }

    
}
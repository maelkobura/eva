using Eva.Commons.Security.Certificate;
using Eva.Commons.Util;
using Eva.Node.Authority;
using Eva.Node.Authority.Certificate;
using Eva.Node.Service;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Eva.Node.Network.Discover;

public class NodeDiscover
{
    public static NodeDiscover? Instance { get; private set; }
    private static ILogger logger = EvaLogger.CreateLogger<NodeDiscover>();

    public readonly ServiceDescription Self;
    
    public static void Init(ServiceDescription self)
    {
        if (Instance != null) return;
        Instance = new NodeDiscover(self);
    }

    public NodeDiscover(ServiceDescription self)
    {
        this.Self = self;
    }

    public void Discover(bool firstConnection = false)
    {
        logger.LogInformation("Discovering nodes...");
        var nodesDiscovered = GetNodesFromTracker();
        foreach (var nodeDiscovered in nodesDiscovered)
        {
            if(nodeDiscovered.Key == Self.Name) continue;
            if (EntityManager.Instance!.Nodes.FirstOrDefault(entity => entity.Name == nodeDiscovered.Key) == null)
            {
                NodeEntity entity = new NodeEntity(nodeDiscovered.Key, nodeDiscovered.Value);
                EntityManager.Instance!.Nodes.Add(entity);
            }
        }

        foreach (var node in EntityManager.Instance!.Nodes)
        {
            Authenticate(node.Name, firstConnection);
        }
        
    }

    public void Authenticate(string name, bool firstConnection = false)
    {
        var node = EntityManager.Instance!.Nodes.FirstOrDefault(entity => entity.Name == name);
        if (node == null) throw new Exception("Node not found: " + name);
        if (!node.IsExpirated()) return;
        
        logger.LogInformation("Try to authenticate to node {0}...", name);

        var handshakeClient = new HandshakeClient(node.Address, node.Name);
        handshakeClient.Handshake(firstConnection).ContinueWith(task =>
        {
            if(!string.IsNullOrEmpty(task.Result) && task.IsCompletedSuccessfully){
                logger.LogInformation("Authenticated to node {0}", node.Name);
                node.NodeTrustCertificate = Certificate.Parser.ParseFrom(Convert.FromBase64String(task.Result));
            }
        });
    }

    private Dictionary<string, string> GetNodesFromTracker()
    {
        var response = AuthorityClient.Instance!.SendGetRequest("/nodes").Result;
        response.EnsureSuccessStatusCode();
        var content = response.Content.ReadAsStringAsync().Result;
        if(string.IsNullOrEmpty(content)) throw new Exception("No EAS response");
        return JsonConvert.DeserializeObject<Dictionary<string, string>>(content);
    }


}
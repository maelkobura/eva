using System;
using System.Collections.Generic;
using System.Linq;
using Eva.Node.Events.Bus;
using Eva.Commons.Events;
using Eva.Commons.Security.Certificate;
using Eva.Commons.System;
using Eva.Commons.Util;
using Eva.Node.Authority;
using Eva.Node.Authority.Certificate;
using Eva.Node.Service;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Eva.Node.Network.Discover;



public class InternalNodeDiscover : INodeDiscover
{
    private static ILogger logger = EvaLogger.CreateLogger<InternalNodeDiscover>();

    public ServiceDescription Self { get; }

    public InternalNodeDiscover(ServiceDescription self)
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
            if (EvaSystem.Singleton<INetworkNodeManager>().Nodes.FirstOrDefault(entity => entity.Name == nodeDiscovered.Key) == null)
            {
                NodeEntity entity = new NodeEntity(nodeDiscovered.Key, nodeDiscovered.Value);
                EvaSystem.Singleton<INetworkNodeManager>().Nodes.Add(entity);
            }
        }

        foreach (var node in EvaSystem.Singleton<INetworkNodeManager>().Nodes)
        {
            Authenticate(node.Name, firstConnection);
        }
        
    }

    public void Authenticate(string name, bool firstConnection = false)
    {
        var node = EvaSystem.Singleton<INetworkNodeManager>().Nodes.FirstOrDefault(entity => entity.Name == name);
        if (node == null) throw new Exception("Node not found: " + name);
        if (!node.IsExpirated()) return;
        
        logger.LogInformation("Try to authenticate to node {0}...", name);

        var handshakeClient = new HandshakeClient(node.Address, node.Name);
        handshakeClient.Handshake(firstConnection).ContinueWith(task =>
        {
            if(!string.IsNullOrEmpty(task.Result) && task.IsCompletedSuccessfully){
                logger.LogInformation("Authenticated to node {0}", node.Name);
                node.NodeTrustCertificate = Certificate.Parser.ParseFrom(Convert.FromBase64String(task.Result));
                node.RefreshPanelAsync();
                EvaSystem.Singleton<IEventBus>().EmitSignal<NodeConnectedEvent>("services.connected", new NodeConnectedEvent()
                {
                    NodeName = node.Name,
                    NodeAddress = node.Address,
                    FirstConnection = firstConnection
                });
            }
        });
    }

    private Dictionary<string, string> GetNodesFromTracker()
    {
        var response = EvaSystem.Singleton<IAuthorityClient>().SendGetRequest("/nodes").Result;
        response.EnsureSuccessStatusCode();
        var content = response.Content.ReadAsStringAsync().Result;
        if(string.IsNullOrEmpty(content)) throw new Exception("No EAS response");
        return JsonConvert.DeserializeObject<Dictionary<string, string>>(content);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
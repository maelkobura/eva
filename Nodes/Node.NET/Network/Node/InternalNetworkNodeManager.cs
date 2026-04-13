using System.Collections.Generic;
using System.Linq;
using Eva.Commons.System;
using Eva.Commons.Util;
using Eva.Node.Events.Bus;
using Eva.Node.Events.Dispatcher;
using Eva.Node.Network.Event;
using Microsoft.Extensions.Logging;

namespace Eva.Node.Network;

public class InternalNetworkNodeManager : INetworkNodeManager, INetworkEventSubscriber
{
    private static ILogger logger = EvaLogger.CreateLogger<InternalNetworkNodeManager>();
    
    private readonly Dictionary<string, NetworkEventClient> _clients = new();
    private readonly Lock _lock = new();

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

    public async Task SubscribeAsync(string eventName)
    {
        // "@node.category.event" → "node"
        var nodeName = eventName.TrimStart('@').Split('.')[0];

        var node = Nodes.FirstOrDefault(n => n.Name == nodeName);
        if (node is null)
        {
            logger.LogWarning("No node found for event '{Event}'", eventName);
            return;
        }

        lock (_lock)
        {
            if (_clients.ContainsKey(eventName)) return;

            var client = new NetworkEventClient(
                node,
                node.Address,
                eventName,
                EvaSystem.Singleton<IEventBus>(),
                EvaSystem.Singleton<IEventBus>().TypeRegistry
            );

            _clients[eventName] = client;
        }

        await _clients[eventName].StartAsync();
    }
}
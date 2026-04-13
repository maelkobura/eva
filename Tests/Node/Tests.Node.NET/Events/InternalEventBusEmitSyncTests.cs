using Eva.Commons.Events;
using Eva.Commons.System;
using Eva.Commons.Util;
using Eva.Node.Events;
using Eva.Node.Events.Bus;
using Xunit;

namespace Tests.Node.NET.Events;

public class InternalEventBusEmitSyncTests
{
    public InternalEventBusEmitSyncTests()
    {
        EvaLogger.Init("Eva Commons Test");
        EvaSystem.Clear();
    }

    // ── Test listeners ───────────────────────────────────────────────────────

    private class CallbackSyncListener(Func<NodeConnectedEvent, NodeConnectedEvent> callback) : Listener
    {
        [SyncEventAttribute("test.sync")]
        public NodeConnectedEvent OnSync(NodeConnectedEvent msg) => callback(msg);
    }

    private class CancellingSyncListener : Listener
    {
        [SyncEventAttribute("test.sync")]
        public NodeConnectedEvent? OnSync(NodeConnectedEvent msg) => null;
    }

    private class LowPrioritySyncListener(List<int> order) : Listener
    {
        [SyncEventAttribute("test.sync", Priority = 1)]
        public NodeConnectedEvent OnSync(NodeConnectedEvent msg)
        {
            order.Add(1);
            return msg;
        }
    }

    private class MediumPrioritySyncListener(List<int> order) : Listener
    {
        [SyncEventAttribute("test.sync", Priority = 5)]
        public NodeConnectedEvent OnSync(NodeConnectedEvent msg)
        {
            order.Add(5);
            return msg;
        }
    }

    private class HighPrioritySyncListener(List<int> order) : Listener
    {
        [SyncEventAttribute("test.sync", Priority = 10)]
        public NodeConnectedEvent OnSync(NodeConnectedEvent msg)
        {
            order.Add(10);
            return msg;
        }
    }

    private class WrongTypeSyncListener : Listener
    {
        [SyncEventAttribute("test.sync")]
        public NodeRefreshEvent OnSync(NodeRefreshEvent msg) => msg;
    }

    // ── Tests ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task EmitSync_NoHandlers_ReturnsPayloadAsIs()
    {
        var bus = new InternalEventBus();
        var payload = new NodeConnectedEvent { NodeName = "node-a" };

        var result = bus.EmitSync("test.sync", payload).Result;

        Assert.Equal(payload, result);
    }

    [Fact]
    public async Task EmitSync_SingleHandler_TransformsPayload()
    {
        var bus = new InternalEventBus();
        await bus.RegisterListener(new CallbackSyncListener(msg =>
        {
            msg.NodeName = "transformed";
            return msg;
        }));

        var result = bus.EmitSync("test.sync", new NodeConnectedEvent { NodeName = "original" }).Result;

        Assert.Equal("transformed", result!.NodeName);
    }

    [Fact]
    public async Task EmitSync_MultipleHandlers_PipelineAppliedInOrder()
    {
        var bus = new InternalEventBus();
        await bus.RegisterListener(new CallbackSyncListener(msg =>
        {
            msg.NodeName += "-first";
            return msg;
        }));
        await bus.RegisterListener(new CallbackSyncListener(msg =>
        {
            msg.NodeName += "-second";
            return msg;
        }));

        var result = bus.EmitSync("test.sync", new NodeConnectedEvent { NodeName = "node" }).Result;

        Assert.Equal("node-first-second", result!.NodeName);
    }

    [Fact]
    public async Task EmitSync_HandlerReturnsNull_ThrowsCancellationException()
    {
        var bus = new InternalEventBus();
        await bus.RegisterListener(new CancellingSyncListener());

        await Assert.ThrowsAsync<CancellationException>(() =>
            bus.EmitSync("test.sync", new NodeConnectedEvent { NodeName = "node-a" }));
    }

    [Fact]
    public async Task EmitSync_PriorityOrder_HandlersCalledInPriorityOrder()
    {
        var bus = new InternalEventBus();
        var order = new List<int>();

        await bus.RegisterListener(new HighPrioritySyncListener(order));
        await bus.RegisterListener(new LowPrioritySyncListener(order));
        await bus.RegisterListener(new MediumPrioritySyncListener(order));

        bus.EmitSync("test.sync", new NodeConnectedEvent());

        Assert.Equal([1, 5, 10], order);
    }

    [Fact]
    public async Task EmitSync_WrongPayloadType_Throws()
    {
        var bus = new InternalEventBus();
        await bus.RegisterListener(new WrongTypeSyncListener());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            bus.EmitSync("test.sync", new NodeConnectedEvent { NodeName = "node-a" }));
    }
}
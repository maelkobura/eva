using Eva.Commons.Events;
using Eva.Commons.System;
using Eva.Commons.Util;
using Eva.Node.Events;
using Eva.Node.Events.Bus;
using Eva.Node.Events.Dispatcher;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Moq;
using Xunit;

namespace Tests.Node.NET.Events;

public class InternalEventBusNetworkTests
{
    public InternalEventBusNetworkTests()
    {
        EvaLogger.Init("Eva Commons Test");
        EvaSystem.Clear();
    }

    // ── Test listeners ───────────────────────────────────────────────────────

    private class NetworkSignalListener : Listener
    {
        [SignalEventAttribute("@node-a.test.signal")]
        public void OnSignal(NodeConnectedEvent msg) { }
    }

    private class NetworkSyncListener : Listener
    {
        [SyncEventAttribute("@node-a.test.sync")]
        public NodeConnectedEvent OnSync(NodeConnectedEvent msg) => msg;
    }

    private class NetworkAsyncListener : Listener
    {
        [AsyncEventAttribute("@node-a.test.async")]
        public Task<NodeConnectedEvent> OnAsync(NodeConnectedEvent msg) => Task.FromResult(msg);
    }

    // ── Mocks ────────────────────────────────────────────────────────────────

    private static (Mock<INetworkEventDispatcher>, Mock<INetworkEventSubscriber>) CreateMocks()
    {
        var dispatcher = new Mock<INetworkEventDispatcher>();
        var subscriber = new Mock<INetworkEventSubscriber>();

        subscriber
            .Setup(s => s.SubscribeAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        dispatcher
            .Setup(d => d.DispatchSyncAsync(It.IsAny<string>(), It.IsAny<NetworkEventFrame>(), It.IsAny<TypeRegistry>()))
            .ReturnsAsync((IMessage?)null);

        dispatcher
            .Setup(d => d.DispatchAsync(It.IsAny<string>(), It.IsAny<NetworkEventFrame>(), It.IsAny<TypeRegistry>()))
            .ReturnsAsync([]);

        return (dispatcher, subscriber);
    }

    // ── RegisterListener ─────────────────────────────────────────────────────

    [Fact]
    public async Task RegisterListener_NetworkSignal_SubscribeAsyncCalled()
    {
        var bus = new InternalEventBus();
        var (dispatcher, subscriber) = CreateMocks();
        bus.SetNetworkDispatcher(dispatcher.Object);
        bus.SetNetworkSubscriber(subscriber.Object);

        await bus.RegisterListener(new NetworkSignalListener());

        subscriber.Verify(s => s.SubscribeAsync("@node-a.test.signal"), Times.Once);
    }

    [Fact]
    public async Task RegisterListener_NetworkSync_SubscribeAsyncCalled()
    {
        var bus = new InternalEventBus();
        var (dispatcher, subscriber) = CreateMocks();
        bus.SetNetworkDispatcher(dispatcher.Object);
        bus.SetNetworkSubscriber(subscriber.Object);

        await bus.RegisterListener(new NetworkSyncListener());

        subscriber.Verify(s => s.SubscribeAsync("@node-a.test.sync"), Times.Once);
    }

    [Fact]
    public async Task RegisterListener_NetworkAsync_SubscribeAsyncCalled()
    {
        var bus = new InternalEventBus();
        var (dispatcher, subscriber) = CreateMocks();
        bus.SetNetworkDispatcher(dispatcher.Object);
        bus.SetNetworkSubscriber(subscriber.Object);

        await bus.RegisterListener(new NetworkAsyncListener());

        subscriber.Verify(s => s.SubscribeAsync("@node-a.test.async"), Times.Once);
    }

    [Fact]
    public async Task RegisterListener_NetworkEvent_SubscriberNull_DoesNotThrow()
    {
        var bus = new InternalEventBus();
        await bus.RegisterListener(new NetworkSignalListener());
    }

    // ── EmitSignal @ ─────────────────────────────────────────────────────────

    [Fact]
    public async Task EmitSignal_NetworkEvent_DispatchSignalCalled()
    {
        var bus = new InternalEventBus();
        var (dispatcher, subscriber) = CreateMocks();
        bus.SetNetworkDispatcher(dispatcher.Object);
        bus.SetNetworkSubscriber(subscriber.Object);

        var payload = new NodeConnectedEvent { NodeName = "node-a" };
        bus.EmitSignal("@node-a.test.signal", payload);

        dispatcher.Verify(d => d.DispatchSignal(
            "@node-a.test.signal",
            It.IsAny<NetworkEventFrame>(),
            It.IsAny<TypeRegistry>()), Times.Once);
    }

    [Fact]
    public async Task EmitSignal_NetworkEvent_DispatcherNull_Throws()
    {
        var bus = new InternalEventBus();

        Assert.Throws<InvalidOperationException>(() =>
            bus.EmitSignal("@node-a.test.signal", new NodeConnectedEvent()));
    }

    // ── EmitSync @ ───────────────────────────────────────────────────────────

    [Fact]
    public async Task EmitSync_NetworkEvent_DispatchSyncAsyncCalled()
    {
        var bus = new InternalEventBus();
        var (dispatcher, subscriber) = CreateMocks();
        bus.SetNetworkDispatcher(dispatcher.Object);
        bus.SetNetworkSubscriber(subscriber.Object);

        await bus.EmitSync("@node-a.test.sync", new NodeConnectedEvent { NodeName = "node-a" });

        dispatcher.Verify(d => d.DispatchSyncAsync(
            "@node-a.test.sync",
            It.IsAny<NetworkEventFrame>(),
            It.IsAny<TypeRegistry>()), Times.Once);
    }

    [Fact]
    public async Task EmitSync_NetworkEvent_DispatcherNull_Throws()
    {
        var bus = new InternalEventBus();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            bus.EmitSync("@node-a.test.sync", new NodeConnectedEvent()));
    }

    // ── EmitAsync @ ──────────────────────────────────────────────────────────

    [Fact]
    public async Task EmitAsync_NetworkEvent_DispatchAsyncCalled()
    {
        var bus = new InternalEventBus();
        var (dispatcher, subscriber) = CreateMocks();
        bus.SetNetworkDispatcher(dispatcher.Object);
        bus.SetNetworkSubscriber(subscriber.Object);

        await bus.EmitAsync("@node-a.test.async", new NodeConnectedEvent { NodeName = "node-a" });

        dispatcher.Verify(d => d.DispatchAsync(
            "@node-a.test.async",
            It.IsAny<NetworkEventFrame>(),
            It.IsAny<TypeRegistry>()), Times.Once);
    }

    [Fact]
    public async Task EmitAsync_NetworkEvent_DispatcherNull_Throws()
    {
        var bus = new InternalEventBus();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            bus.EmitAsync("@node-a.test.async", new NodeConnectedEvent()));
    }
}
using Eva.Commons.Events;
using Eva.Commons.System;
using Eva.Commons.Util;
using Eva.Node.Events;
using Eva.Node.Events.Bus;
using Xunit;

namespace Tests.Node.NET.Events;

public class InternalEventBusEmitSignalTests
{
    public InternalEventBusEmitSignalTests()
    {
        EvaLogger.Init("Eva Commons Test");
        EvaSystem.Clear();
    }

    // ── Test listeners ───────────────────────────────────────────────────────

    private class CallbackSignalListener(Action callback) : Listener
    {
        [SignalEventAttribute("test.signal")]
        public void OnSignal() => callback();
    }

    private class CallbackSignalWithParamListener(Action<NodeConnectedEvent> callback) : Listener
    {
        [SignalEventAttribute("test.signal")]
        public void OnSignal(NodeConnectedEvent msg) => callback(msg);
    }

    // ── Tests ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task EmitSignal_NoPayload_HandlerWithNoParam_Called()
    {
        var bus = new InternalEventBus();
        var called = false;
        await bus.RegisterListener(new CallbackSignalListener(() => called = true));

        bus.EmitSignal("test.signal");

        Assert.True(called);
    }

    [Fact]
    public async Task EmitSignal_WithPayload_HandlerWithParam_Called()
    {
        var bus = new InternalEventBus();
        NodeConnectedEvent? received = null;
        await bus.RegisterListener(new CallbackSignalWithParamListener(msg => received = msg));

        var payload = new NodeConnectedEvent { NodeName = "node-a", NodeAddress = "localhost" };
        bus.EmitSignal("test.signal", payload);

        Assert.NotNull(received);
        Assert.Equal("node-a", received.NodeName);
    }

    [Fact]
    public async Task EmitSignal_NullPayload_HandlerWithParam_Throws()
    {
        var bus = new InternalEventBus();
        await bus.RegisterListener(new CallbackSignalWithParamListener(_ => { }));

        Assert.Throws<InvalidOperationException>(() => bus.EmitSignal("test.signal"));
    }

    [Fact]
    public async Task EmitSignal_WrongPayloadType_Throws()
    {
        var bus = new InternalEventBus();
        await bus.RegisterListener(new CallbackSignalWithParamListener(_ => { }));

        var wrongPayload = new NodeRefreshEvent { NodeName = "node-a", NodeAddress = "localhost" };
        Assert.Throws<InvalidOperationException>(() => bus.EmitSignal("test.signal", wrongPayload));
    }

    [Fact]
    public async Task EmitSignal_NoHandlers_DoesNotThrow()
    {
        var bus = new InternalEventBus();
        bus.EmitSignal("test.signal");
        bus.EmitSignal("test.signal", new NodeConnectedEvent());
    }

    [Fact]
    public async Task EmitSignal_MultipleHandlers_AllCalled()
    {
        var bus = new InternalEventBus();
        var count = 0;
        await bus.RegisterListener(new CallbackSignalListener(() => count++));
        await bus.RegisterListener(new CallbackSignalListener(() => count++));
        await bus.RegisterListener(new CallbackSignalListener(() => count++));

        bus.EmitSignal("test.signal");

        Assert.Equal(3, count);
    }
}
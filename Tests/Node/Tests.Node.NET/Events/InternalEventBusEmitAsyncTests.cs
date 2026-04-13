using Eva.Commons.Events;
using Eva.Commons.System;
using Eva.Commons.Util;
using Eva.Node.Events;
using Eva.Node.Events.Bus;
using Xunit;

namespace Tests.Node.NET.Events;

public class InternalEventBusEmitAsyncTests
{
    public InternalEventBusEmitAsyncTests()
    {
        EvaLogger.Init("Eva Commons Test");
        EvaSystem.Clear();
    }

    // ── Test listeners ───────────────────────────────────────────────────────

    private class CallbackAsyncListener(Func<NodeConnectedEvent, Task<NodeConnectedEvent>> callback) : Listener
    {
        [AsyncEventAttribute("test.async")]
        public Task<NodeConnectedEvent> OnAsync(NodeConnectedEvent msg) => callback(msg);
    }

    private class DirectReturnAsyncListener(Func<NodeConnectedEvent, NodeConnectedEvent> callback) : Listener
    {
        [AsyncEventAttribute("test.async")]
        public NodeConnectedEvent OnAsync(NodeConnectedEvent msg) => callback(msg);
    }

    private class NullReturningAsyncListener : Listener
    {
        [AsyncEventAttribute("test.async")]
        public NodeConnectedEvent? OnAsync(NodeConnectedEvent msg) => null;
    }

    private class WrongTypeAsyncListener : Listener
    {
        [AsyncEventAttribute("test.async")]
        public Task<NodeRefreshEvent> OnAsync(NodeRefreshEvent msg) => Task.FromResult(msg);
    }

    // ── Tests ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task EmitAsync_NoHandlers_ReturnsEmptyList()
    {
        var bus = new InternalEventBus();

        var results = await bus.EmitAsync("test.async", new NodeConnectedEvent());

        Assert.Empty(results);
    }

    [Fact]
    public async Task EmitAsync_SingleHandler_ReturnsResult()
    {
        var bus = new InternalEventBus();
        await bus.RegisterListener(new CallbackAsyncListener(msg =>
            Task.FromResult(new NodeConnectedEvent { NodeName = "result" })));

        var results = await bus.EmitAsync("test.async", new NodeConnectedEvent());

        Assert.Single(results);
        Assert.Equal("result", results[0].NodeName);
    }

    [Fact]
    public async Task EmitAsync_MultipleHandlers_AllResultsReturned()
    {
        var bus = new InternalEventBus();
        await bus.RegisterListener(new CallbackAsyncListener(_ =>
            Task.FromResult(new NodeConnectedEvent { NodeName = "first" })));
        await bus.RegisterListener(new CallbackAsyncListener(_ =>
            Task.FromResult(new NodeConnectedEvent { NodeName = "second" })));
        await bus.RegisterListener(new CallbackAsyncListener(_ =>
            Task.FromResult(new NodeConnectedEvent { NodeName = "third" })));

        var results = await bus.EmitAsync("test.async", new NodeConnectedEvent());

        Assert.Equal(3, results.Count);
    }

    [Fact]
    public async Task EmitAsync_HandlersCalledInParallel()
    {
        var bus = new InternalEventBus();
        var barrier = new Barrier(3);

        await bus.RegisterListener(new CallbackAsyncListener(async _ =>
        {
            barrier.SignalAndWait();
            return new NodeConnectedEvent { NodeName = "a" };
        }));
        await bus.RegisterListener(new CallbackAsyncListener(async _ =>
        {
            barrier.SignalAndWait();
            return new NodeConnectedEvent { NodeName = "b" };
        }));
        await bus.RegisterListener(new CallbackAsyncListener(async _ =>
        {
            barrier.SignalAndWait();
            return new NodeConnectedEvent { NodeName = "c" };
        }));

        var results = await bus.EmitAsync("test.async", new NodeConnectedEvent());

        Assert.Equal(3, results.Count);
    }

    [Fact]
    public async Task EmitAsync_NullResultsFiltered()
    {
        var bus = new InternalEventBus();
        await bus.RegisterListener(new CallbackAsyncListener(_ =>
            Task.FromResult(new NodeConnectedEvent { NodeName = "valid" })));
        await bus.RegisterListener(new NullReturningAsyncListener());

        var results = await bus.EmitAsync("test.async", new NodeConnectedEvent());

        Assert.Single(results);
        Assert.Equal("valid", results[0].NodeName);
    }

    [Fact]
    public async Task EmitAsync_DirectReturnMessage_Accepted()
    {
        var bus = new InternalEventBus();
        await bus.RegisterListener(new DirectReturnAsyncListener(_ =>
            new NodeConnectedEvent { NodeName = "direct" }));

        var results = await bus.EmitAsync("test.async", new NodeConnectedEvent());

        Assert.Single(results);
        Assert.Equal("direct", results[0].NodeName);
    }

    [Fact]
    public async Task EmitAsync_WrongPayloadType_FilteredSilently()
    {
        var bus = new InternalEventBus();
        await bus.RegisterListener(new WrongTypeAsyncListener());

        var results = await bus.EmitAsync("test.async", new NodeConnectedEvent());

        Assert.Empty(results);
    }
}
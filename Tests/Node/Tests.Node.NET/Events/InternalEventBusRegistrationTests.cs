using Eva.Commons.Events;
using Eva.Commons.Util;
using Eva.Node.Events;
using Eva.Node.Events.Bus;
using Xunit;

namespace Tests.Node.NET.Events;

public class InternalEventBusRegistrationTests
{
    // ── Test listeners ───────────────────────────────────────────────────────

    private class ValidSignalNoParam : Listener
    {
        [SignalEventAttribute("test.signal")]
        public void OnSignal() { }
    }

    private class ValidSignalWithParam : Listener
    {
        [SignalEventAttribute("test.signal")]
        public void OnSignal(NodeConnectedEvent msg) { }
    }

    private class InvalidSignalNonIMessage : Listener
    {
        [SignalEventAttribute("test.signal")]
        public void OnSignal(string msg) { }
    }

    private class InvalidSignalTooManyParams : Listener
    {
        [SignalEventAttribute("test.signal")]
        public void OnSignal(NodeConnectedEvent a, NodeConnectedEvent b) { }
    }

    private class ValidSync : Listener
    {
        [SyncEventAttribute("test.sync")]
        public NodeConnectedEvent OnSync(NodeConnectedEvent msg) => msg;
    }

    private class InvalidSyncNoParam : Listener
    {
        [SyncEventAttribute("test.sync")]
        public NodeConnectedEvent OnSync() => new();
    }

    private class InvalidSyncBadReturn : Listener
    {
        [SyncEventAttribute("test.sync")]
        public string OnSync(NodeConnectedEvent msg) => "";
    }

    private class ValidAsyncTaskOfMessage : Listener
    {
        [AsyncEventAttribute("test.async")]
        public Task<NodeConnectedEvent> OnAsync(NodeConnectedEvent msg) => Task.FromResult(msg);
    }

    private class ValidAsyncDirectMessage : Listener
    {
        [AsyncEventAttribute("test.async")]
        public NodeConnectedEvent OnAsync(NodeConnectedEvent msg) => msg;
    }

    private class InvalidAsyncBadReturn : Listener
    {
        [AsyncEventAttribute("test.async")]
        public string OnAsync(NodeConnectedEvent msg) => "";
    }

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

    private class CallbackSyncListener(Func<NodeConnectedEvent, NodeConnectedEvent> callback) : Listener
    {
        [SyncEventAttribute("test.sync")]
        public NodeConnectedEvent OnSync(NodeConnectedEvent msg) => callback(msg);
    }

    private class CallbackAsyncListener(Func<NodeConnectedEvent, Task<NodeConnectedEvent>> callback) : Listener
    {
        [AsyncEventAttribute("test.async")]
        public Task<NodeConnectedEvent> OnAsync(NodeConnectedEvent msg) => callback(msg);
    }
    
    public InternalEventBusRegistrationTests()
    {
        EvaLogger.Init("Eva Commons Test");
    }

    // ── Registration ─────────────────────────────────────────────────────────

    [Fact]
    public async Task RegisterListener_SignalNoParam_Registers()
    {
        var bus = new InternalEventBus();
        await bus.RegisterListener(new ValidSignalNoParam());
    }

    [Fact]
    public async Task RegisterListener_SignalWithParam_Registers()
    {
        var bus = new InternalEventBus();
        await bus.RegisterListener(new ValidSignalWithParam());
    }

    [Fact]
    public async Task RegisterListener_SignalNonIMessageParam_Throws()
    {
        var bus = new InternalEventBus();
        await Assert.ThrowsAsync<InvalidOperationException>(() => bus.RegisterListener(new InvalidSignalNonIMessage()));
    }

    [Fact]
    public async Task RegisterListener_SignalTooManyParams_Throws()
    {
        var bus = new InternalEventBus();
        await Assert.ThrowsAsync<InvalidOperationException>(() => bus.RegisterListener(new InvalidSignalTooManyParams()));
    }

    [Fact]
    public async Task RegisterListener_SyncValid_Registers()
    {
        var bus = new InternalEventBus();
        await bus.RegisterListener(new ValidSync());
    }

    [Fact]
    public async Task RegisterListener_SyncNoParam_Throws()
    {
        var bus = new InternalEventBus();
        await Assert.ThrowsAsync<InvalidOperationException>(() => bus.RegisterListener(new InvalidSyncNoParam()));
    }

    [Fact]
    public async Task RegisterListener_SyncBadReturn_Throws()
    {
        var bus = new InternalEventBus();
        await Assert.ThrowsAsync<InvalidOperationException>(() => bus.RegisterListener(new InvalidSyncBadReturn()));
    }

    [Fact]
    public async Task RegisterListener_AsyncTaskOfMessage_Registers()
    {
        var bus = new InternalEventBus();
        await bus.RegisterListener(new ValidAsyncTaskOfMessage());
    }

    [Fact]
    public async Task RegisterListener_AsyncDirectMessage_Registers()
    {
        var bus = new InternalEventBus();
        await bus.RegisterListener(new ValidAsyncDirectMessage());
    }

    [Fact]
    public async Task RegisterListener_AsyncBadReturn_Throws()
    {
        var bus = new InternalEventBus();
        await Assert.ThrowsAsync<InvalidOperationException>(() => bus.RegisterListener(new InvalidAsyncBadReturn()));
    }

    // ── Unregister ───────────────────────────────────────────────────────────

    [Fact]
    public async Task UnregisterListener_Signal_HandlerNotCalled()
    {
        var bus = new InternalEventBus();
        var called = false;
        var listener = new CallbackSignalListener(() => called = true);

        await bus.RegisterListener(listener);
        bus.UnregisterListener(listener);
        bus.EmitSignal("test.signal");

        Assert.False(called);
    }

    [Fact]
    public async Task UnregisterListener_Sync_HandlerNotCalled()
    {
        var bus = new InternalEventBus();
        var called = false;
        var listener = new CallbackSyncListener(msg => { called = true; return msg; });

        await bus.RegisterListener(listener);
        bus.UnregisterListener(listener);
        bus.EmitSync("test.sync", new NodeConnectedEvent());

        Assert.False(called);
    }

    [Fact]
    public async Task UnregisterListener_Async_HandlerNotCalled()
    {
        var bus = new InternalEventBus();
        var called = false;
        var listener = new CallbackAsyncListener(msg => { called = true; return Task.FromResult(msg); });

        await bus.RegisterListener(listener);
        bus.UnregisterListener(listener);
        await bus.EmitAsync("test.async", new NodeConnectedEvent());

        Assert.False(called);
    }

    [Fact]
    public async Task UnregisterListener_NotRegistered_DoesNotThrow()
    {
        var bus = new InternalEventBus();
        bus.UnregisterListener(new ValidSignalNoParam());
    }
}
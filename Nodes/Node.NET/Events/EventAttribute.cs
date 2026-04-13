namespace Eva.Node.Events;

[AttributeUsage(AttributeTargets.Method)]
public abstract class EventAttribute(string eventName) : Attribute
{
    public string EventName { get; } = eventName;
}

[AttributeUsage(AttributeTargets.Method)]
public class SignalEventAttribute(string eventName) : EventAttribute(eventName);

[AttributeUsage(AttributeTargets.Method)]
public class SyncEventAttribute(string eventName, int priority = int.MaxValue) : EventAttribute(eventName)
{
    public int Priority { get; init; } = priority;
}
 
[AttributeUsage(AttributeTargets.Method)]
public class AsyncEventAttribute(string eventName) : EventAttribute(eventName);
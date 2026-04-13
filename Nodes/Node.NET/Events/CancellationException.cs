using Google.Protobuf;

namespace Eva.Node.Events;

public class CancellationException : Exception
{

    public readonly IMessage? LastPayload;
    
    public CancellationException(IMessage payload) => LastPayload = payload;
    
}
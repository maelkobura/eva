using System.Text;
using EmbedIO.WebSockets;

namespace Eva.AuthorityServer.Server.Node;

public class NodeWebSocketHandler : WebSocketModule{
    public NodeWebSocketHandler(string urlPath) : base(urlPath, true)
    {
        
    }

    protected override Task OnMessageReceivedAsync(IWebSocketContext context, byte[] buffer, IWebSocketReceiveResult result)
    {
        var message = Encoding.UTF8.GetString(buffer);
        Console.WriteLine($"Message reçu: {message}");

        // Répond en renvoyant le même message
        return context.WebSocket.SendAsync(
            Encoding.UTF8.GetBytes("Echo: " + message),
            true,
            CancellationToken.None);
    }

    protected override Task OnClientConnectedAsync(IWebSocketContext context)
    {
        Console.WriteLine("Client connecté");
        return Task.CompletedTask;
    }

    protected override Task OnClientDisconnectedAsync(IWebSocketContext context)
    {
        Console.WriteLine("Client déconnecté");
        return Task.CompletedTask;
    }
}
    
    

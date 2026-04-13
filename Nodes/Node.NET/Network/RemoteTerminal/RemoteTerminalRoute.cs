using EmbedIO.WebSockets;
using Eva.Commons.Messages;
using Eva.Commons.Security;
using Eva.Commons.Security.Certificate;
using Eva.Commons.System;
using Eva.Node.Terminal;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace Eva.Node.Network.RemoteTerminal;

public class RemoteTerminalRoute : WebSocketModule {
    
    public RemoteTerminalRoute(string urlPath, bool enableConnectionWatchdog) : base(urlPath, enableConnectionWatchdog) {}

    protected override Task OnMessageReceivedAsync(IWebSocketContext context, byte[] buffer,
        IWebSocketReceiveResult result)
    {
        var message = TerminalMessage.Parser.ParseFrom(buffer);

        TerminalSession session = (TerminalSession)context.Items["session"];

        switch (message.PayloadCase)
        {
            case TerminalMessage.PayloadOneofCase.Command:
                var command = message.Command;

                var rtvalue = session.Execute(command.Command, command.BorrowCertificate);

                var (type, value) = TerminalUtil.ConvertFromJavascript(rtvalue);

                var response = new TerminalResponse
                {
                    ReturnValue = type,
                    Value = ByteString.CopyFrom(value)
                };

                var returnMessage = new TerminalMessage
                {
                    Returns = response
                };

                return context.WebSocket.SendAsync(returnMessage.ToByteArray(), false);

        }
        return Task.CompletedTask;
    
    }

    protected override Task OnClientConnectedAsync(IWebSocketContext context)
    {
        var cert = (Certificate) context.Items["certificate"];

        if (Authorizations.Has(cert, "eva.terminal.openSession"))
        {
            TerminalSession session = EvaSystem.Singleton<ITerminalManager>().CreateSession(cert.Payload.Content.UniqueId);
            context.Items["session"] = session;
            return base.OnClientConnectedAsync(context);
        }
        else
        {
            context.WebSocket.CloseAsync();
            return Task.FromCanceled(context.CancellationToken);
        }
    }
}
using System.Security.Cryptography;
using EmbedIO.WebSockets;
using Eva.Commons.Messages;
using Eva.Commons.Security;
using Eva.Commons.Security.Certificate;
using Eva.Commons.Util;
using Eva.Node.Authority.Certificate;
using Eva.Node.Network.Discover;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Type = Eva.Commons.Security.Certificate.Type;
using Version = Eva.Commons.Security.Certificate.Version;

namespace Eva.Node.Network;

public class HandshakeRoute : WebSocketModule
{
    private static readonly ILogger logger = EvaLogger.CreateLogger<HandshakeRoute>();

    public HandshakeRoute(string urlPath, bool enableConnectionWatchdog)
        : base(urlPath, enableConnectionWatchdog) { }

    protected override Task OnMessageReceivedAsync(
        IWebSocketContext context, byte[] buffer, IWebSocketReceiveResult result)
    {
        var message = Handshake.Parser.ParseFrom(buffer);

        return message.Step switch
        {
            HandshakeStep.Initialization    => HandleInitializationAsync(context, message),
            HandshakeStep.ChallengeResult   => HandleChallengeResultAsync(context, message),
            _                               => RejectAsync(context)
        };
    }

    private async Task HandleInitializationAsync(IWebSocketContext context, Handshake message)
    {
        var initPayload  = HandshakeInitialization.Parser.ParseFrom(message.Payload.ToByteArray());
        var certificate  = Certificate.Parser.ParseFrom(Base64.Base64UrlDecode(initPayload.Certificate));

        bool isValid = CertificateUtil.CheckCertificate(certificate, CertificateManager.Instance!.EasPublicKey)
                    && certificate.Payload.Content.Subject == initPayload.Name;

        if (!isValid)
        {
            await RejectAsync(context);
            return;
        }

        StoreSession(context, certificate, initPayload.Name, initPayload.Initialization);

        var challenge = new HandshakeChallenge
        {
            FirstFactor  = RandomNumberGenerator.GetInt32(-999999, 999999),
            SecondFactor = RandomNumberGenerator.GetInt32(-99999,  99999)
        };

        context.Session["challenge"] = challenge.FirstFactor * challenge.SecondFactor;
        await context.WebSocket.SendAsync(challenge.ToByteArray(), false, CancellationToken.None);
    }

    private async Task HandleChallengeResultAsync(IWebSocketContext context, Handshake message)
    {
        var resultPayload = HandshakeChallengeResult.Parser.ParseFrom(message.Payload.ToByteArray());
        var certificate   = GetSessionCertificate(context);

        bool isValid = resultPayload.Result == (int)context.Session["challenge"]
                    && SignatureUtil.VerifyIntSignature(
                           resultPayload.Result,
                           resultPayload.Signature.ToByteArray(),
                           certificate.Payload.Content.EntityPublicKey);

        if (!isValid)
        {
            await SendValidationAsync(context, success: false);
            await RejectAsync(context);
            return;
        }

        var nodeTrustCert = BuildNodeTrustCertificate(context, certificate);

        await SendValidationAsync(context, success: true, nodeTrustCert);
        await context.WebSocket.CloseAsync();

        if ((bool)context.Session["first"])
        {
            EntityManager.Instance!.ResetCertificateForNode(GetSessionName(context));
        }
        
        NodeDiscover.Instance!.Authenticate(GetSessionName(context));
    }

    private static void StoreSession(IWebSocketContext context, Certificate certificate, string name, bool firstConnection)
    {
        context.Session["certificate"] = certificate;
        context.Session["name"]        = name;
        context.Session["first"] = firstConnection;
    }

    private static Certificate GetSessionCertificate(IWebSocketContext context)
        => (Certificate)context.Session["certificate"];

    private static string GetSessionName(IWebSocketContext context)
        => (string)context.Session["name"];

    private static Certificate BuildNodeTrustCertificate(IWebSocketContext context, Certificate sourceCert)
    {
        var content = new CertificateContent
        {
            Issuer          = NodeDiscover.Instance!.Self.Name,
            Subject         = GetSessionName(context),
            UniqueId        = Base64.GenerateToken(),
            Expiration      = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 3600,
            EntityPublicKey = CertificateManager.Instance!.CertificateUnit.Payload.Content.EntityPublicKey,
        };
        content.Authorization.Add(sourceCert.Payload.Content.Authorization);

        var payload = new CertificatePayload
        {
            Header = new CertificateHeader
            {
                Algorithm = "Ed25519",
                Version   = Version.V1,
                Type      = Type.NodeTrust
            },
            Content = content
        };

        return CertificateUtil.SignCertificate(
            payload,
            CertificateManager.Instance!.PrivateKey,
            CertificateManager.Instance!.EasPublicKey);
    }

    private static async Task SendValidationAsync(
        IWebSocketContext context, bool success, Certificate? nodeTrustCert = null)
    {
        var validation = new HandshakeValidation { Success = success };

        if (nodeTrustCert is not null)
            validation.NodeTrustCertificate = nodeTrustCert.ToByteString().ToBase64();

        await context.WebSocket.SendAsync(validation.ToByteArray(), false, CancellationToken.None);
    }

    private static async Task RejectAsync(IWebSocketContext context)
        => await context.WebSocket.CloseAsync();
    

    protected override Task OnClientConnectedAsync(IWebSocketContext context)
    {
        logger.LogInformation("Handshake opened from {ip}", FormatIp(context));
        return Task.CompletedTask;
    }

    protected override Task OnClientDisconnectedAsync(IWebSocketContext context)
    {
        logger.LogInformation("Handshake closed from {ip}", FormatIp(context));
        return Task.CompletedTask;
    }

    private static string FormatIp(IWebSocketContext context)
        => string.Join(".", context.RemoteEndPoint.Address.MapToIPv4().GetAddressBytes());
}
using System.Net.WebSockets;
using System.Security.Cryptography.X509Certificates;
using Eva.Commons.Messages;
using Eva.Commons.Security;
using Eva.Commons.Security.Certificate;
using Eva.Commons.System;
using Eva.Commons.Util;
using Eva.Node.Authority.Certificate;
using Google.Protobuf;
using Microsoft.Extensions.Logging;

namespace Eva.Node.Network;

public class HandshakeClient
{
    private static ILogger logger = EvaLogger.CreateLogger<HandshakeClient>();
    private string url;
    private string name;
    
    
    public HandshakeClient(string url,string name)
    {
        this.url = url;
        this.name = name;
    }
    
public async Task<string?> Handshake(bool firstConnection = false)
{
    using var ws = new ClientWebSocket();
    var cm = EvaSystem.Singleton<ICertificateManager>();

    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10)); // timeout global

    bool secure = false;
    
    if (cm.TlsEasCertificate is not null)
    {
        ws.Options.RemoteCertificateValidationCallback = (_, cert, chain, errors) =>
        {
            chain!.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
            chain.ChainPolicy.CustomTrustStore.Add(cm.TlsEasCertificate);
            return chain.Build(new X509Certificate2(cert!));
        };
        secure = true;
    }
    
    
    try
    {
        var uri = new Uri($"ws{(secure ? "s" : "")}://" + url + "/handshake");
        await ws.ConnectAsync(uri, cts.Token);
        
        if (ws.State != WebSocketState.Open)
            return null;

        // ---- INIT ----
        var initPayload = new HandshakeInitialization
        {
            Name = cm!.CertificateUnit.Payload.Content.Subject,
            Certificate = cm.CertificateRaw,
            Initialization = firstConnection
        };

        var init = new Handshake
        {
            Step = HandshakeStep.Initialization,
            Payload = ByteString.CopyFrom(initPayload.ToByteArray())
        };

        await ws.SendAsync(init.ToByteArray(), WebSocketMessageType.Binary, true, cts.Token);

        // ---- RECEIVE CHALLENGE ----
        var challengeBytes = await ReceiveFullMessage(ws, cts.Token);
        if (challengeBytes == null) return null;

        var challenge = HandshakeChallenge.Parser.ParseFrom(challengeBytes);

        // ---- SOLVE CHALLENGE ----
        var challengeProduct = challenge.FirstFactor * challenge.SecondFactor;
        var signature = SignatureUtil.SignInt(
            challengeProduct,
            cm!.PrivateKey
        );
        var resultPayload = new HandshakeChallengeResult
        {
            Result = challengeProduct,
            Signature = ByteString.CopyFrom(signature)
        };

        var resultMsg = new Handshake
        {
            Step = HandshakeStep.ChallengeResult,
            Payload = ByteString.CopyFrom(resultPayload.ToByteArray())
        };

        await ws.SendAsync(resultMsg.ToByteArray(), WebSocketMessageType.Binary, true, cts.Token);

        // ---- RECEIVE VALIDATION ----
        var validationBytes = await ReceiveFullMessage(ws, cts.Token);
        if (validationBytes == null) return null;

        var validation = HandshakeValidation.Parser.ParseFrom(validationBytes);

        // ---- CLOSE CLEAN ----
        if (ws.State == WebSocketState.Open || ws.State == WebSocketState.CloseReceived)
        {
            await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", CancellationToken.None);
        }

        return validation.Success ? validation.NodeTrustCertificate : null;
    }
    catch (OperationCanceledException)
    {
        logger.LogWarning($"[{name}] Handshake cancelled (timeout)");
        return null;
    }
    catch (WebSocketException e)
    {
        logger.LogWarning($"[{name}] {e.Message} (Code: {e.WebSocketErrorCode})");
        return null;
    }
    catch (Exception e)
    {
        logger.LogWarning($"[{name}] {e.Message}");
        return null;
    }
}

private static async Task<byte[]?> ReceiveFullMessage(ClientWebSocket ws, CancellationToken ct)
{
    var buffer = new byte[4096];
    using var ms = new MemoryStream();

    while (true)
    {
        var result = await ws.ReceiveAsync(buffer, ct);

        if (result.MessageType == WebSocketMessageType.Close)
        {
            return null;
        }

        ms.Write(buffer, 0, result.Count);

        if (result.EndOfMessage)
            break;
    }

    return ms.ToArray();
}
}
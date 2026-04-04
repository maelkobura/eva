using Eva.Commons.Security.Certificate;
using Eva.Commons.System;
using Eva.Commons.Util;
using Eva.Node.Authority.Certificate;
using Eva.Node.Network;
using Microsoft.Extensions.Logging;

namespace Eva.Node.Terminal;

public class TerminalManager
{
    public static TerminalManager? Instance { get; private set; }
    private static ILogger logger = EvaLogger.CreateLogger<TerminalManager>();

    public static void Init()
    {
        if (Instance != null) return;
        Instance = new TerminalManager();
    }

    private static readonly Dictionary<string, TerminalSession> _sessions = new();

    public static TerminalSession CreateSession(string sessionId)
    {
        var session = new TerminalSession(EvaSystem.Singleton<ICertificateManager>().CertificateUnit!);
        _sessions[sessionId] = session;
        return session;
    }

    public static TerminalSession? GetSession(string sessionId)
        => _sessions.TryGetValue(sessionId, out var s) ? s : null;

    public static void CloseSession(string sessionId)
    {
        if (_sessions.Remove(sessionId, out var session))
            session.Dispose();
    }
}
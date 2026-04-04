using Eva.Commons.Security.Certificate;
using Eva.Commons.System;
using Eva.Commons.Util;
using Eva.Node.Authority.Certificate;
using Eva.Node.Network;
using Microsoft.Extensions.Logging;

namespace Eva.Node.Terminal;

public class InternalTerminalManager : ITerminalManager{
    private static ILogger logger = EvaLogger.CreateLogger<InternalTerminalManager>();

    private readonly Dictionary<string, TerminalSession> _sessions = new();

    public TerminalSession CreateSession(string sessionId)
    {
        var session = new TerminalSession(EvaSystem.Singleton<ICertificateManager>().CertificateUnit!);
        _sessions[sessionId] = session;
        return session;
    }

    public TerminalSession? GetSession(string sessionId)
        => _sessions.TryGetValue(sessionId, out var s) ? s : null;

    public void CloseSession(string sessionId)
    {
        if (_sessions.Remove(sessionId, out var session))
            session.Dispose();
    }

    public void Dispose()
    {
        foreach (var session in _sessions.Keys)
            CloseSession(session);
    }
}
namespace Eva.Node.Terminal;

public interface ITerminalManager
: IDisposable{
    public TerminalSession CreateSession(string sessionId);
    public TerminalSession? GetSession(string sessionId);
    public void CloseSession(string sessionId);
}
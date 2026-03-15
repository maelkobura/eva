using Microsoft.Extensions.Configuration;

namespace Eva.Node.Authority;

public class AuthorityClient
{
    public static AuthorityClient? Instance { get; private set; }

    public static void Init(AuthorityConnectionInfo? main, AuthorityConnectionInfo? backup)
    {
        if (Instance != null) return;
        Instance = new AuthorityClient(main, backup);
    }
    
    private readonly AuthorityConnectionInfo? main;
    private readonly AuthorityConnectionInfo? backup;
    
    private AuthorityClient(AuthorityConnectionInfo? main, AuthorityConnectionInfo? backup)
    {
        this.main = main;
        this.backup = backup;
    }

    public Uri GetMainUri()
    {
        return new Uri($"http://{main.Host}:{main.Port}");
    }
    
    public Uri GetBackupUri()
    {
        return new Uri($"http://{backup.Host}:{backup.Port}");
    }
    
    
    
    
    
}
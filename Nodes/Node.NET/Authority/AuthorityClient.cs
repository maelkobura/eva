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

    public string EasCertificate;
    
    private readonly AuthorityConnectionInfo? main;
    private readonly AuthorityConnectionInfo? backup;
    private HttpClient client;
    
    private AuthorityClient(AuthorityConnectionInfo? main, AuthorityConnectionInfo? backup)
    {
        this.main = main;
        this.backup = backup;
        this.client = new HttpClient();
    }

    public Uri GetMainUri()
    {
        return new Uri($"http://{main.Host}:{main.Port}");
    }
    
    public Uri GetBackupUri()
    {
        return new Uri($"http://{backup.Host}:{backup.Port}");
    }

    public async Task<HttpResponseMessage> SendPostRequest(string route, HttpContent content)
    {
        var mainUri = new Uri(GetMainUri(), route);
        var backupUri = new Uri(GetBackupUri(), route);

        if (!string.IsNullOrEmpty(EasCertificate)) {
            content.Headers.Add("Authorization", $"Bearer {EasCertificate}");
        }
        
        try
        {
            var response = await client.PostAsync(mainUri, content);

            return response;
        }
        catch (HttpRequestException)
        {
            return await client.PostAsync(backupUri, content);
        }
    }
    
    public async Task<HttpResponseMessage> SendGetRequest(string route)
    {
        var mainUri = new Uri(GetMainUri(), route);
        var backupUri = new Uri(GetBackupUri(), route);

        using var request = new HttpRequestMessage(HttpMethod.Get, mainUri);

        if (!string.IsNullOrEmpty(EasCertificate))
        {
            request.Headers.Add("Authorization", $"Bearer {EasCertificate}");
        }

        try
        {
            var response = await client.SendAsync(request);
            return response;
        }
        catch (HttpRequestException)
        {
            using var backupRequest = new HttpRequestMessage(HttpMethod.Get, backupUri);

            if (!string.IsNullOrEmpty(EasCertificate))
            {
                backupRequest.Headers.Add("Authorization", $"Bearer {EasCertificate}");
            }

            return await client.SendAsync(backupRequest);
        }
    }
    
    
    
    
    
}
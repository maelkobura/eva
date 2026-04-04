using System;
using System.Net.Http;
using System.Threading.Tasks;
using Eva.AuthorityServer.System;

namespace Eva.Node.Authority;

public class InternalAuthorityClient : IAuthorityClient
{
    private readonly AuthorityConnectionInfo? _main;
    private readonly AuthorityConnectionInfo? _backup;
    private readonly HttpClient _client;
    private bool _disposed;

    public string EasCertificate { get; set; } = string.Empty;

    public InternalAuthorityClient(AuthorityConnectionInfo? main, AuthorityConnectionInfo? backup)
    {
        _main = main;
        _backup = backup;
        _client = new HttpClient();
    }

    public Uri GetMainUri()
    {
        if (_main == null) throw new InvalidOperationException("Main Authority connection info not provided");
        return new Uri($"http{(_main.Tls ? "s" : "")}://{_main.Host}:{_main.Port}");
    }

    public Uri GetBackupUri()
    {
        if (_backup == null) throw new InvalidOperationException("Backup Authority connection info not provided");
        return new Uri($"http{(_backup.Tls ? "s" : "")}://{_backup.Host}:{_backup.Port}");
    }

    public async Task<HttpResponseMessage> SendPostRequest(string route, HttpContent content)
    {
        var mainUri = new Uri(GetMainUri(), route);
        var backupUri = new Uri(GetBackupUri(), route);

        if (!string.IsNullOrEmpty(EasCertificate))
            content.Headers.Add("Authorization", $"Bearer {EasCertificate}");

        try
        {
            return await _client.PostAsync(mainUri, content);
        }
        catch (HttpRequestException)
        {
            return await _client.PostAsync(backupUri, content);
        }
    }

    public async Task<HttpResponseMessage> SendGetRequest(string route)
    {
        var mainUri = new Uri(GetMainUri(), route);
        var backupUri = new Uri(GetBackupUri(), route);

        using var request = new HttpRequestMessage(HttpMethod.Get, mainUri);
        if (!string.IsNullOrEmpty(EasCertificate))
            request.Headers.Add("Authorization", $"Bearer {EasCertificate}");

        try
        {
            return await _client.SendAsync(request);
        }
        catch (HttpRequestException)
        {
            using var backupRequest = new HttpRequestMessage(HttpMethod.Get, backupUri);
            if (!string.IsNullOrEmpty(EasCertificate))
                backupRequest.Headers.Add("Authorization", $"Bearer {EasCertificate}");

            return await _client.SendAsync(backupRequest);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        _client.Dispose();
        _disposed = true;
    }
}
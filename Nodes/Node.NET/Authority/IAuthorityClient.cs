using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Eva.Node.Authority;

/// <summary>
/// Interface for sending HTTP requests to the Authority service.
/// </summary>
public interface IAuthorityClient : IDisposable{
    /// <summary>
    /// Gets the URI of the main Authority server.
    /// </summary>
    Uri GetMainUri();

    /// <summary>
    /// Gets the URI of the backup Authority server.
    /// </summary>
    Uri GetBackupUri();

    /// <summary>
    /// Sends a POST request to the Authority service.
    /// </summary>
    /// <param name="route">Route relative to the base URI.</param>
    /// <param name="content">HTTP content to send.</param>
    /// <returns>The HTTP response.</returns>
    Task<HttpResponseMessage> SendPostRequest(string route, HttpContent content);

    /// <summary>
    /// Sends a GET request to the Authority service.
    /// </summary>
    /// <param name="route">Route relative to the base URI.</param>
    /// <returns>The HTTP response.</returns>
    Task<HttpResponseMessage> SendGetRequest(string route);

    /// <summary>
    /// Optional bearer token for authorization.
    /// </summary>
    string EasCertificate { get; set; }
}
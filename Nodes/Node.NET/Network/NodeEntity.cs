using System.Net.Http.Headers;
using Eva.Commons.Messages;
using Eva.Commons.Security.Certificate;
using Google.Protobuf;
using NSec.Cryptography;

namespace Eva.Node.Network;

public class NodeEntity {
    
    public string Name { get;}
    public string DisplayName;
    public string Address { get; }
    
    public Certificate? NodeTrustCertificate;

    public NodeEntity(string name, string address)
    {
        Name = name;
        Address = address;
    }

    public virtual bool IsExpirated()
    {
        return NodeTrustCertificate == null || NodeTrustCertificate.Payload.Content.Expiration < DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }
    
    public void ResetCertificate()
    {
        NodeTrustCertificate = null;
    }
    
    public FunctionPanel? FunctionPanel { get; private set; }

    public virtual async Task RefreshPanelAsync()
    {
        using var http = HTTP.CreateHttpClient(out var secure);
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Convert.ToBase64String(NodeTrustCertificate!.ToByteArray()));

        var response = await http.GetAsync($"http{(secure ? "s" : "")}://{Address}/funcs");
        if (!response.IsSuccessStatusCode)
            throw new Exception($"Failed to fetch function panel from '{Name}' (code {(int)response.StatusCode})");

        var bytes = await response.Content.ReadAsByteArrayAsync();
        FunctionPanel = FunctionPanel.Parser.ParseFrom(bytes);
    }

    public virtual EvaFunctionDescriptor? GetFunction(string name)
    {
        return FunctionPanel?.Functions.FirstOrDefault(f => f.Name == name);
    }
    
    public virtual async Task<InvokeResponse> InvokeAsync(string functionName, Dictionary<string, ByteString> parameters, Certificate? callerCert)
    {
        var function = GetFunction(functionName);
        if (function is null)
            throw new Exception($"Function '{functionName}' not found on node '{Name}'");

        using var http = HTTP.CreateHttpClient(out var secure);
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Convert.ToBase64String(callerCert.ToByteArray() ?? NodeTrustCertificate!.ToByteArray()));

        var request = new InvokeRequest
        {
            CallerId = callerCert.Payload.Content.Subject,
            Parameters = { parameters }
        };

        var content = new ByteArrayContent(request.ToByteArray());
        content.Headers.ContentType = new MediaTypeHeaderValue("application/x-protobuf");

        var response = await http.PostAsync($"http{(secure ? "s" : "")}://{Address}/funcs/{functionName}", content);
        var responseBytes = await response.Content.ReadAsByteArrayAsync();
        return InvokeResponse.Parser.ParseFrom(responseBytes);
    }
}
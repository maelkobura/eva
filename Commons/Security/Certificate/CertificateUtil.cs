using System.Text.Json;
using Eva.Commons.Util;

namespace Eva.Commons.Security.Certificate;

public class CertificateUtil
{
    public static CertificateEntity? ParseTokenPayload(string token)
    {
        try
        {
            var parts = token.Split('.');
            if (parts.Length != 3) return null;

            var payloadJson = Base64.Base64UrlDecode(parts[1]);
            var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(payloadJson);

            if (payload is null) return null;

            var exp = payload["exp"].GetInt64();
            var name = payload["sub"].GetString()!;
            var type = Enum.Parse<CertificateType>(payload["type"].GetString()!);
            var roles = payload["roles"].EnumerateArray()
                .Select(r => r.GetString()!)
                .ToArray();

            return new CertificateEntity(name, type, roles, exp);
        }
        catch
        {
            return null;
        }
    }
}
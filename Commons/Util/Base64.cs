using System.Security.Cryptography;

namespace Eva.Commons.Util;

public class Base64
{
    public static string Base64UrlEncode(byte[] data) =>
        Convert.ToBase64String(data)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    public static byte[] Base64UrlDecode(string data)
    {
        var s = data.Replace('-', '+').Replace('_', '/');
        s = s.PadRight(s.Length + (4 - s.Length % 4) % 4, '=');
        return Convert.FromBase64String(s);
    }
    
    public static string GenerateToken(int length = 32)
    {
        byte[] bytes = new byte[length];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(bytes);
        }
        string token = Convert.ToBase64String(bytes);
        return token.Substring(0, length);
    }
}
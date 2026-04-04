using System.Text;
using System.Text.RegularExpressions;

namespace Eva.Commons.Util;

public static class StringExtensions
{
    public static string ToSnakeCase(this string str)
    {
        if (string.IsNullOrEmpty(str)) return str;

        var sb = new StringBuilder();
        for (int i = 0; i < str.Length; i++)
        {
            if (char.IsUpper(str[i]) && i > 0)
                sb.Append('_');
            sb.Append(char.ToLower(str[i]));
        }
        return sb.ToString();
    }
    
    public static string SplitSingletonName(this string input)
    {
        // Remove prefix
        input = Regex.Replace(input, "^Internal", "");

        // Split PascalCase
        return Regex.Replace(input, "(\\B[A-Z])", " $1");
    }
}
using System.Text;

namespace Shizou.Data.Extensions;

public static class StringExtensions
{
    public static string WithoutSpaces(this string str)
    {
        return str.Replace(" ", null);
    }

    public static string UpperCaseSpaced(this string str)
    {
        var output = new StringBuilder();

        for (var i = 0; i < str.Length; i++)
        {
            if (i > 0 && char.IsUpper(str[i]) && !char.IsUpper(str[i - 1]))
                output.Append(' ');
            output.Append(str[i]);
        }

        return output.ToString();
    }

    public static string TrimEnd(this string str, string value) =>
        !string.IsNullOrEmpty(value) && str.EndsWith(value) ? str.Remove(str.LastIndexOf(value, StringComparison.Ordinal)) : str;

    public static string TrimStart(this string str, string value) => !string.IsNullOrEmpty(value) && str.StartsWith(value) ? str.Remove(0, value.Length) : str;
}

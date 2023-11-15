namespace Shizou.Data.Utilities.Extensions;

public static class StringExtensions
{
    public static string WithoutSpaces(this string str)
    {
        return str.Replace(" ", null);
    }
}

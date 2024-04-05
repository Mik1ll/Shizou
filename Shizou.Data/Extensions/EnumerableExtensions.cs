using System.Text.RegularExpressions;

namespace Shizou.Data.Extensions;

public static class EnumerableExtensions
{
    private static readonly Regex DigitRegex = new(@"\d+", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static IOrderedEnumerable<T> OrderByAlphaNumeric<T>(this IEnumerable<T> source, Func<T, string> selector)
    {
        return source.OrderBy(i => DigitRegex.Replace(selector(i), m => m.Value.PadLeft(10, '0')));
    }
}

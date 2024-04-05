using System.Text.RegularExpressions;

namespace Shizou.Data.Extensions;

public static class EnumerableExtensions
{
    private static readonly Regex DigitRegex = new(@"\d+", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static IOrderedEnumerable<T> OrderByAlphaNumeric<T>(this IEnumerable<T> source, Func<T, string> selector)
    {
        var list = source.ToList();
        var max = list
            .SelectMany(i => DigitRegex.Matches(selector(i)).Select(m => m.Value.Length))
            .DefaultIfEmpty().Max();

        return list.OrderBy(i => DigitRegex.Replace(selector(i), m => m.Value.PadLeft(max, '0')));
    }
}

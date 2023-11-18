using System.Linq.Expressions;
using System.Text.Json.Serialization;
using Shizou.Data.Models;

namespace Shizou.Data.Filters;

public enum AirDateCriteria
{
    OnOrAfter = 1,
    Before = 2,
    Missing = 3
}

public class AirDateFilter : AnimeFilter
{
    [JsonConstructor]
    public AirDateFilter(bool negate, AirDateCriteria airDateCriteria, int? year = null, int? month = null, int? day = null)
        : base(Create(airDateCriteria, year, month, day), negate)
    {
        AirDateCriteria = airDateCriteria;
        Year = year;
        Month = month;
        Day = day;
    }

    [JsonInclude]
    public int? Year { get; }

    [JsonInclude]
    public int? Month { get; }

    [JsonInclude]
    public int? Day { get; }

    [JsonInclude]
    public AirDateCriteria AirDateCriteria { get; }

    private static Expression<Func<AniDbAnime, bool>> Create(AirDateCriteria airDateCriteria, int? year, int? month, int? day)
    {
        if (new[] { year, month, day }.Any(x => x is not null) && airDateCriteria == AirDateCriteria.Missing)
            throw new ArgumentException("Date cannot be specified for missing criteria");
        if (month is null && day is not null)
            throw new ArgumentException("Cannot specify day without month");
        var date = $"{year:d4}";
        if (month is not null)
            date += $"-{month:d2}";
        if (day is not null)
            date += $"-{day:d2}";
        return airDateCriteria switch
        {
            // ReSharper disable once StringCompareIsCultureSpecific.1
            AirDateCriteria.OnOrAfter => anime => anime.AirDate != null && string.Compare(anime.AirDate, date) >= 0,
            // ReSharper disable once StringCompareIsCultureSpecific.1
            AirDateCriteria.Before => anime => anime.AirDate != null && string.Compare(anime.AirDate, date) < 0,
            AirDateCriteria.Missing => anime => anime.AirDate == null,
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}

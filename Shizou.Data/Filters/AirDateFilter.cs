using System.Linq.Expressions;
using Shizou.Data.Models;

namespace Shizou.Data.Filters;

public enum AirDateCriteria
{
    OnOrAfter = 1,
    Before = 2,
    Missing = 3
}

public class AirDateFilter : IAnimeFilter
{
    public int? Year { get; init; }
    public int? Month { get; init; }
    public int? Day { get; init; }
    public AirDateCriteria AirDateCriteria { get; init; }

    public Expression<Func<AniDbAnime, bool>> AnimeFilter
    {
        get
        {
            if (new[] { Year, Month, Day }.Any(x => x is not null) && AirDateCriteria == AirDateCriteria.Missing)
                throw new ArgumentException("Date cannot be specified for missing criteria");
            if (Month is null && Day is not null)
                throw new ArgumentException("Cannot specify day without month");
            var date = $"{Year:d4}";
            if (Month is not null)
                date += $"-{Month:d2}";
            if (Day is not null)
                date += $"-{Day:d2}";
            return AirDateCriteria switch
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
}

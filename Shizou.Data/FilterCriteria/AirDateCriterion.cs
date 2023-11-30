using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using Shizou.Data.Models;

namespace Shizou.Data.FilterCriteria;

public enum AirDateCriterionType
{
    OnOrAfter = 1,
    Before = 2,
    Missing = 3
}

public record AirDateCriterion(bool Negated, AirDateCriterionType AirDateCriterionType, int? Year = null, int? Month = null, int? Day = null)
    : TermCriterion(Negated)
{
    public AirDateCriterionType AirDateCriterionType { get; set; } = AirDateCriterionType;

    [Range(1900, 2100)]
    public int? Year { get; set; } = Year;

    public int? Month { get; set; } = Month;
    public int? Day { get; set; } = Day;

    protected override Expression<Func<AniDbAnime, bool>> MakeTerm()
    {
        if (new[] { Year, Month, Day }.Any(x => x is not null) && AirDateCriterionType == AirDateCriterionType.Missing)
            throw new ArgumentException("Date cannot be specified for missing criteria");
        if (Month is null && Day is not null)
            throw new ArgumentException("Cannot specify day without month");
        var date = $"{Year:d4}";
        if (Month is not null)
            date += $"-{Month:d2}";
        if (Day is not null)
            date += $"-{Day:d2}";
        return AirDateCriterionType switch
        {
            // ReSharper disable once StringCompareIsCultureSpecific.1
            AirDateCriterionType.OnOrAfter => anime => anime.AirDate != null && string.Compare(anime.AirDate, date) >= 0,
            // ReSharper disable once StringCompareIsCultureSpecific.1
            AirDateCriterionType.Before => anime => anime.AirDate != null && string.Compare(anime.AirDate, date) < 0,
            AirDateCriterionType.Missing => anime => anime.AirDate == null,
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}

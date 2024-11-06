using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Shizou.Data.Models;

namespace Shizou.Data.FilterCriteria;

public enum AirDateTermRange
{
    OnOrAfter = 1,
    Before = 2,
    Missing = 3
}

public enum AirDateTermType
{
    AirDate = 1,
    EndDate = 2
}

public record AirDateCriterion(bool Negated, AirDateTermType AirDateTermType, AirDateTermRange AirDateTermRange, int? Year = null, int? Month = null,
        int? Day = null)
    : TermCriterion(Negated)
{
    // ReSharper disable once UnusedMember.Global
    public AirDateCriterion() : this(false, AirDateTermType.AirDate, AirDateTermRange.Before)
    {
    }
    
    public AirDateTermType AirDateTermType { get; set; } = AirDateTermType;
    public AirDateTermRange AirDateTermRange { get; set; } = AirDateTermRange;

    [Range(1900, 2100)]
    public int? Year { get; set; } = Year;

    public int? Month { get; set; } = Month;
    public int? Day { get; set; } = Day;

    [SuppressMessage("ReSharper", "StringCompareIsCultureSpecific.1")]
    protected override Expression<Func<AniDbAnime, bool>> MakeTerm()
    {
        if (Year is null && Month is not null)
            throw new ArgumentException("Cannot specify month without year");
        if (Month is null && Day is not null)
            throw new ArgumentException("Cannot specify day without month");
        var date = $"{Year:d4}";
        if (Month is not null)
            date += $"-{Month:d2}";
        if (Day is not null)
            date += $"-{Day:d2}";
        return (AirDateTermRange, AirDateTermType) switch
        {
            (AirDateTermRange.OnOrAfter, AirDateTermType.AirDate) => anime => anime.AirDate != null && string.Compare(anime.AirDate, date) >= 0,
            (AirDateTermRange.OnOrAfter, AirDateTermType.EndDate) => anime => anime.EndDate != null && string.Compare(anime.EndDate, date) >= 0,
            (AirDateTermRange.Before, AirDateTermType.AirDate) => anime => anime.AirDate != null && string.Compare(anime.AirDate, date) < 0,
            (AirDateTermRange.Before, AirDateTermType.EndDate) => anime => anime.EndDate != null && string.Compare(anime.EndDate, date) < 0,
            (AirDateTermRange.Missing, AirDateTermType.AirDate) => anime => anime.AirDate == null,
            (AirDateTermRange.Missing, AirDateTermType.EndDate) => anime => anime.EndDate == null,
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}

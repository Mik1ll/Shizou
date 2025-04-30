using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using Shizou.Data.Models;

namespace Shizou.Data.FilterCriteria;

public enum AirDateTermRange
{
    OnOrAfter = 1,
    Before = 2,
    Missing = 3,
}

public enum AirDateTermType
{
    AirDate = 1,
    EndDate = 2,
}

public record AirDateCriterion : TermCriterion
{
    public AirDateTermType AirDateTermType { get; set; } = AirDateTermType.AirDate;
    public AirDateTermRange AirDateTermRange { get; set; } = AirDateTermRange.Before;

    [Range(1900, 2100)]
    public int? Year { get; set; }

    public int? Month { get; set; }
    private int? HasMonth => Month.HasValue ? Day : null;

    [Compare(nameof(HasMonth))]
    public int? Day { get; set; }

    protected override Expression<Func<AniDbAnime, bool>> MakeTerm()
    {
        DateOnly? date = null;
        // ReSharper disable once InvertIf
        if (AirDateTermRange is not AirDateTermRange.Missing)
        {
            if (Year is null)
                throw new ArgumentException("Year is required");
            if (Month is null && Day is not null)
                throw new ArgumentException("Cannot specify day without month");
            date = new DateOnly(Year.Value, Month ?? 1, Day ?? 1);
        }

        return (AirDateTermRange, AirDateTermType) switch
        {
            (AirDateTermRange.OnOrAfter, AirDateTermType.AirDate) => anime => anime.AirDate != null && anime.AirDate >= date,
            (AirDateTermRange.OnOrAfter, AirDateTermType.EndDate) => anime => anime.EndDate != null && anime.EndDate >= date,
            (AirDateTermRange.Before, AirDateTermType.AirDate) => anime => anime.AirDate != null && anime.AirDate < date,
            (AirDateTermRange.Before, AirDateTermType.EndDate) => anime => anime.EndDate != null && anime.EndDate < date,
            (AirDateTermRange.Missing, AirDateTermType.AirDate) => anime => anime.AirDate == null,
            (AirDateTermRange.Missing, AirDateTermType.EndDate) => anime => anime.EndDate == null,
            _ => throw new ArgumentOutOfRangeException(),
        };
    }
}

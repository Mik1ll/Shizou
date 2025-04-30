using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using JetBrains.Annotations;
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

public record AirDateCriterion(
    bool Negated,
    AirDateTermType AirDateTermType,
    AirDateTermRange AirDateTermRange,
    int? Year = null,
    int? Month = null,
    int? Day = null)
    : TermCriterion(Negated)
{
    [UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
    public AirDateCriterion() : this(false, AirDateTermType.AirDate, AirDateTermRange.Before)
    {
    }

    public AirDateTermType AirDateTermType { get; set; } = AirDateTermType;
    public AirDateTermRange AirDateTermRange { get; set; } = AirDateTermRange;

    [Range(1900, 2100)]
    public int? Year { get; set; } = Year;

    public int? Month { get; set; } = Month;
    private int? HasMonth => Month.HasValue ? Day : null;

    [Compare(nameof(HasMonth))]
    public int? Day { get; set; } = Day;

    [SuppressMessage("ReSharper", "StringCompareIsCultureSpecific.1")]
    protected override Expression<Func<AniDbAnime, bool>> MakeTerm()
    {
        DateOnly? date = null;
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
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}

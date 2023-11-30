﻿using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using Shizou.Data.Models;

namespace Shizou.Data.FilterCriteria;

public enum AirDateCriterionType
{
    OnOrAfter = 1,
    Before = 2,
    Missing = 3
}

public record AirDateCriterion(bool Negated, AirDateCriterionType AirDateCriterionType, int? Year = null, int? Month = null, int? Day = null) : TermCriterion(
    Negated, Create(AirDateCriterionType, Year, Month, Day))
{
    public AirDateCriterionType AirDateCriterionType { get; set; } = AirDateCriterionType;

    [Range(1900, 2100)]
    public int? Year { get; set; } = Year;

    public int? Month { get; set; } = Month;
    public int? Day { get; set; } = Day;

    private static Expression<Func<AniDbAnime, bool>> Create(AirDateCriterionType airDateCriterionType, int? year, int? month, int? day)
    {
        if (new[] { year, month, day }.Any(x => x is not null) && airDateCriterionType == AirDateCriterionType.Missing)
            throw new ArgumentException("Date cannot be specified for missing criteria");
        if (month is null && day is not null)
            throw new ArgumentException("Cannot specify day without month");
        var date = $"{year:d4}";
        if (month is not null)
            date += $"-{month:d2}";
        if (day is not null)
            date += $"-{day:d2}";
        return airDateCriterionType switch
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

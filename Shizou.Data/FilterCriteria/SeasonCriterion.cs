﻿using System.Linq.Expressions;
using Shizou.Data.Models;

namespace Shizou.Data.FilterCriteria;

public enum AnimeSeason
{
    Winter = 1,
    Spring,
    Summer,
    Fall
}

public record SeasonCriterion : TermCriterion
{
    public AnimeSeason Season { get; set; } = AnimeSeason.Winter;

    protected override Expression<Func<AniDbAnime, bool>> MakeTerm()
    {
        var startMonth = Season switch
        {
            AnimeSeason.Winter => 1,
            AnimeSeason.Spring => 4,
            AnimeSeason.Summer => 7,
            AnimeSeason.Fall => 10,
            _ => throw new ArgumentOutOfRangeException()
        };
        return anime => anime.AirDate != null && anime.AirDate.Value.Month >= startMonth && anime.AirDate.Value.Month < startMonth + 3;
    }
}

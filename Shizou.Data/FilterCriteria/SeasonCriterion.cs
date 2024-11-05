using System.Linq.Expressions;
using Shizou.Data.Models;

namespace Shizou.Data.FilterCriteria;

public enum AnimeSeason
{
    Winter = 1,
    Spring,
    Summer,
    Fall
}

public record SeasonCriterion(bool Negated, AnimeSeason Season) : TermCriterion(Negated)
{
    public AnimeSeason Season { get; set; } = Season;

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
        return anime => anime.AirDate != null && string.Compare(anime.AirDate.Substring(5), $"{startMonth:d2}") >= 0 &&
                        string.Compare(anime.AirDate.Substring(5), $"{startMonth + 3:d2}") < 0;
    }
}

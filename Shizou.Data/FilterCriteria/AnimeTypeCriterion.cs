using System.Linq.Expressions;
using Shizou.Data.Enums;
using Shizou.Data.Models;

namespace Shizou.Data.FilterCriteria;

public record AnimeTypeCriterion : TermCriterion
{
    public AnimeType AnimeType { get; set; } = AnimeType.TvSeries;

    protected override Expression<Func<AniDbAnime, bool>> MakeTerm()
    {
        return anime => anime.AnimeType == AnimeType;
    }
}

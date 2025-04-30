using System.Linq.Expressions;
using Shizou.Data.Enums;
using Shizou.Data.Models;

namespace Shizou.Data.FilterCriteria;

public record EpisodeWithoutFilesCriterion : TermCriterion
{
    protected override Expression<Func<AniDbAnime, bool>> MakeTerm()
    {
        return anime => anime.AniDbEpisodes.Where(e => e.EpisodeType == EpisodeType.Episode)
            .Any(e => e.AniDbFiles.All(gf => !gf.LocalFiles.Any()));
    }
}

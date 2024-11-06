using System.Linq.Expressions;
using Shizou.Data.Enums;
using Shizou.Data.Models;

namespace Shizou.Data.FilterCriteria;

public record EpisodeWithoutFilesCriterion(bool Negated) : TermCriterion(Negated)
{
    // ReSharper disable once UnusedMember.Global
    public EpisodeWithoutFilesCriterion() : this(false)
    {
    }

    protected override Expression<Func<AniDbAnime, bool>> MakeTerm()
    {
        return anime => anime.AniDbEpisodes.Where(e => e.EpisodeType == EpisodeType.Episode)
            .Any(e => !e.AniDbFiles.Any(gf => gf.LocalFiles.Any()));
    }
}

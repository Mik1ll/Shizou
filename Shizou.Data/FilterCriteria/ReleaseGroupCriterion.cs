using System.Linq.Expressions;
using Shizou.Data.Models;

namespace Shizou.Data.FilterCriteria;

public record ReleaseGroupCriterion : TermCriterion
{
    public int? GroupId { get; set; }

    protected override Expression<Func<AniDbAnime, bool>> MakeTerm()
    {
        return anime => anime.AniDbEpisodes.Any(ep => ep.AniDbFiles.Cast<AniDbNormalFile>().Any(f => f.AniDbGroupId == GroupId));
    }
}

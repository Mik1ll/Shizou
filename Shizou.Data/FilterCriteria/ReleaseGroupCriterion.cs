using System.Linq.Expressions;
using Shizou.Data.Models;

namespace Shizou.Data.FilterCriteria;

public record ReleaseGroupCriterion(bool Negated, int? GroupId) : TermCriterion(Negated)
{
    public int? GroupId { get; set; } = GroupId;

    protected override Expression<Func<AniDbAnime, bool>> MakeTerm()
    {
        return anime => anime.AniDbEpisodes.Any(ep => ep.AniDbFiles.OfType<AniDbNormalFile>().Any(f => f.AniDbGroupId == GroupId));
    }
}

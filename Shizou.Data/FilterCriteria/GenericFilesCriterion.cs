using System.Linq.Expressions;
using Shizou.Data.Models;

namespace Shizou.Data.FilterCriteria;

public record GenericFilesCriterion(bool Negated) : TermCriterion(Negated)
{
    protected override Expression<Func<AniDbAnime, bool>> MakeTerm()
    {
        return anime => anime.AniDbEpisodes.Any(e => e.AniDbFiles.OfType<AniDbGenericFile>().Any(f => f.LocalFiles.Any()));
    }
}

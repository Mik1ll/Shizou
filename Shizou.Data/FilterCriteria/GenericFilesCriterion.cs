using System.Linq.Expressions;
using Shizou.Data.Models;

namespace Shizou.Data.FilterCriteria;

public record GenericFilesCriterion(bool Negated) : TermCriterion(Negated)
{
    // ReSharper disable once UnusedMember.Global
    public GenericFilesCriterion() : this(false)
    {
    }

    protected override Expression<Func<AniDbAnime, bool>> MakeTerm()
    {
        return anime => anime.AniDbEpisodes.Any(e => e.AniDbFiles.OfType<AniDbGenericFile>().Any(f => f.LocalFiles.Any()));
    }
}

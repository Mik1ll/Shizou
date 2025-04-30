using System.Linq.Expressions;
using Shizou.Data.Models;

namespace Shizou.Data.FilterCriteria;

public record RestrictedCriterion : TermCriterion
{
    protected override Expression<Func<AniDbAnime, bool>> MakeTerm() =>
        anime => anime.Restricted;
}

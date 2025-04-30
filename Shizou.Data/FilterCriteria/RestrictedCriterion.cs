using System.Linq.Expressions;
using JetBrains.Annotations;
using Shizou.Data.Models;

namespace Shizou.Data.FilterCriteria;

public record RestrictedCriterion(bool Negated) : TermCriterion(Negated)
{
    [UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
    public RestrictedCriterion() : this(false)
    {
    }

    protected override Expression<Func<AniDbAnime, bool>> MakeTerm() =>
        anime => anime.Restricted;
}

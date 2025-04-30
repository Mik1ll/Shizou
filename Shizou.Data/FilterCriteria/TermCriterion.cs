using System.Linq.Expressions;
using Shizou.Data.Models;

namespace Shizou.Data.FilterCriteria;

/// <summary>
/// Inheritors must have a parameterless constructor
/// </summary>
public abstract record TermCriterion : AnimeCriterion
{
    public bool Negated { get; set; }

    protected abstract Expression<Func<AniDbAnime, bool>> MakeTerm();

    protected override Expression<Func<AniDbAnime, bool>> Create()
    {
        var term = MakeTerm();
        return Negated ? Expression.Lambda<Func<AniDbAnime, bool>>(Expression.Not(term.Body), term.Parameters) : term;
    }
}

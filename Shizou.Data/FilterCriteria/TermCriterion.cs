using System.Linq.Expressions;
using System.Text.Json.Serialization;
using Shizou.Data.Models;

namespace Shizou.Data.FilterCriteria;

/// <summary>
/// Inheritors must have a parameterless constructor
/// </summary>
public abstract record TermCriterion : AnimeCriterion
{
    public bool Negated { get; set; }

    protected abstract Expression<Func<AniDbAnime, bool>> PredicateInner { get; }

    [JsonIgnore]
    public sealed override Expression<Func<AniDbAnime, bool>> Predicate => PredicateInner is var term && Negated
        ? Expression.Lambda<Func<AniDbAnime, bool>>(Expression.Not(term.Body), term.Parameters)
        : term;
}

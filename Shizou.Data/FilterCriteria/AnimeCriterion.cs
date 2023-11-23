using System.Linq.Expressions;
using System.Text.Json.Serialization;
using Shizou.Data.Models;

namespace Shizou.Data.FilterCriteria;

public abstract class AnimeCriterion
{
    private readonly Expression<Func<AniDbAnime, bool>> _criterion;

    protected AnimeCriterion(Expression<Func<AniDbAnime, bool>> criterion, bool negated)
    {
        _criterion = criterion;
        Negated = negated;
    }

    [JsonInclude]
    public bool Negated { get; }

    [JsonIgnore]
    public Expression<Func<AniDbAnime, bool>> Criterion =>
        Negated ? Expression.Lambda<Func<AniDbAnime, bool>>(Expression.Not(_criterion.Body), false, _criterion.Parameters) : _criterion;
}

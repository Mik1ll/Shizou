using System.Linq.Expressions;
using System.Text.Json.Serialization;
using Shizou.Data.Models;

namespace Shizou.Data.FilterCriteria;

public abstract record AnimeCriterion
{
    private readonly Expression<Func<AniDbAnime, bool>> _criterion;

    protected AnimeCriterion(bool negated, Expression<Func<AniDbAnime, bool>> criterion)
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

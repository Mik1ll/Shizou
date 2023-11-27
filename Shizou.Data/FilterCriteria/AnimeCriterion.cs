using System.Linq.Expressions;
using System.Text.Json.Serialization;
using Shizou.Data.Models;

namespace Shizou.Data.FilterCriteria;

public abstract record AnimeCriterion(bool Negated, Expression<Func<AniDbAnime, bool>> Criterion)
{
    private readonly Expression<Func<AniDbAnime, bool>> _criterion = Criterion;

    [JsonIgnore]
    public Expression<Func<AniDbAnime, bool>> Criterion =>
        Negated ? Expression.Lambda<Func<AniDbAnime, bool>>(Expression.Not(_criterion.Body), false, _criterion.Parameters) : _criterion;
}

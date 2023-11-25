using System.Linq.Expressions;
using System.Text.Json.Serialization;
using Shizou.Data.Models;

namespace Shizou.Data.FilterCriteria;

public record OrAnyCriterion : AnimeCriterion
{
    [JsonConstructor]
    public OrAnyCriterion(params AnimeCriterion[] criteria) : base(Create(criteria), false) => Criteria = criteria;

    [JsonInclude]
    public AnimeCriterion[] Criteria { get; }

    private static Expression<Func<AniDbAnime, bool>> Create(AnimeCriterion[] criteria)
    {
        var animeParam = Expression.Parameter(typeof(AniDbAnime), "anime");
        var expression = criteria.Length == 0
            ? Expression.Constant(false)
            : criteria.Select(y => ParameterReplacer.Replace(y.Criterion, animeParam)).Aggregate(Expression.OrElse);
        var lambda = Expression.Lambda<Func<AniDbAnime, bool>>(expression, animeParam);
        return lambda;
    }
}

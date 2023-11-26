using System.Linq.Expressions;
using System.Text.Json.Serialization;
using Shizou.Data.Models;

namespace Shizou.Data.FilterCriteria;

public record AndAllCriterion : AnimeCriterion
{
    [JsonConstructor]
    public AndAllCriterion(bool negated, List<AnimeCriterion> criteria) : base(negated, Create(criteria)) => Criteria = criteria;

    [JsonInclude]
    public List<AnimeCriterion> Criteria { get; }

    private static Expression<Func<AniDbAnime, bool>> Create(List<AnimeCriterion> criteria)
    {
        var animeParam = Expression.Parameter(typeof(AniDbAnime), "anime");
        var expression = criteria.Count == 0
            ? Expression.Constant(true)
            : criteria.Select(y => ParameterReplacer.Replace(y.Criterion, animeParam)).Aggregate(Expression.AndAlso);
        var lambda = Expression.Lambda<Func<AniDbAnime, bool>>(expression, animeParam);
        return lambda;
    }
}

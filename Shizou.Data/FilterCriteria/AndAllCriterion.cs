using System.Linq.Expressions;
using System.Text.Json.Serialization;
using Shizou.Data.Models;

namespace Shizou.Data.FilterCriteria;

public record AndAllCriterion(List<TermCriterion> Criteria) : AnimeCriterion
{
    [JsonIgnore]
    public override Expression<Func<AniDbAnime, bool>> Predicate
    {
        get
        {
            var animeParam = Expression.Parameter(typeof(AniDbAnime), "anime");
            var expression = Criteria.Count == 0
                ? Expression.Constant(true)
                : Criteria.Select(y => ParameterReplacer.Replace(y.Predicate, animeParam)).Aggregate(Expression.AndAlso);
            var lambda = Expression.Lambda<Func<AniDbAnime, bool>>(expression, animeParam);
            return lambda;
        }
    }
}

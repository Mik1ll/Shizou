using System.Linq.Expressions;
using Shizou.Data.Models;

namespace Shizou.Data.FilterCriteria;

public record AndAllCriterion(List<AnimeCriterion> Criteria) : AnimeCriterion(false, Create(Criteria))
{
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

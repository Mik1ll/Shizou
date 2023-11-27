using System.Linq.Expressions;
using Shizou.Data.Models;

namespace Shizou.Data.FilterCriteria;

public record OrAnyCriterion(List<AnimeCriterion> Criteria) : AnimeCriterion(false, Create(Criteria))
{
    private static Expression<Func<AniDbAnime, bool>> Create(List<AnimeCriterion> criteria)
    {
        var animeParam = Expression.Parameter(typeof(AniDbAnime), "anime");
        var expression = criteria.Count == 0
            ? Expression.Constant(false)
            : criteria.Select(y => ParameterReplacer.Replace(y.Criterion, animeParam)).Aggregate(Expression.OrElse);
        var lambda = Expression.Lambda<Func<AniDbAnime, bool>>(expression, animeParam);
        return lambda;
    }
}

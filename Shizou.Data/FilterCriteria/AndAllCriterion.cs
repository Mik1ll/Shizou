using System.Linq.Expressions;
using Shizou.Data.Models;

namespace Shizou.Data.FilterCriteria;

public record AndAllCriterion(List<TermCriterion> Criteria) : AnimeCriterion
{
    protected override Expression<Func<AniDbAnime, bool>> Create()
    {
        var animeParam = Expression.Parameter(typeof(AniDbAnime), "anime");
        var expression = Criteria.Count == 0
            ? Expression.Constant(true)
            : Criteria.Select(y => ParameterReplacer.Replace(y.Criterion, animeParam)).Aggregate(Expression.AndAlso);
        var lambda = Expression.Lambda<Func<AniDbAnime, bool>>(expression, animeParam);
        return lambda;
    }
}

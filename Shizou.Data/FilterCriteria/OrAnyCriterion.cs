using System.Linq.Expressions;
using Shizou.Data.Models;

namespace Shizou.Data.FilterCriteria;

public record OrAnyCriterion(List<AndAllCriterion> Criteria) : AnimeCriterion
{
    protected override Expression<Func<AniDbAnime, bool>> Create()
    {
        var animeParam = Expression.Parameter(typeof(AniDbAnime), "anime");
        var expression = Criteria.Count == 0
            ? Expression.Constant(false)
            : Criteria.Select(y => ParameterReplacer.Replace(y.Criterion, animeParam)).Aggregate(Expression.OrElse);
        var lambda = Expression.Lambda<Func<AniDbAnime, bool>>(expression, animeParam);
        return lambda;
    }
}

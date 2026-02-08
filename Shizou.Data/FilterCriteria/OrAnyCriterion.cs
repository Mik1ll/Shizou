using System.Collections;
using System.Linq.Expressions;
using Shizou.Data.Models;

namespace Shizou.Data.FilterCriteria;

public sealed record OrAnyCriterion(List<AndAllCriterion> Criteria) : AnimeCriterion, IEnumerable<AndAllCriterion>
{
    public OrAnyCriterion() : this(new List<AndAllCriterion>())
    {
    }

    public override Expression<Func<AniDbAnime, bool>> Predicate
    {
        get
        {
            var animeParam = Expression.Parameter(typeof(AniDbAnime), "anime");
            var expression = Criteria.Count == 0
                ? Expression.Constant(false)
                : Criteria.Select(y => ParameterReplacer.Replace(y.Predicate, animeParam)).Aggregate(Expression.OrElse);
            var lambda = Expression.Lambda<Func<AniDbAnime, bool>>(expression, animeParam);
            return lambda;
        }
    }

    public void Add(AndAllCriterion criterion) => Criteria.Add(criterion);

    public IEnumerator<AndAllCriterion> GetEnumerator() => Criteria.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

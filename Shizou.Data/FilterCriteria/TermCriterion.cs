using System.Linq.Expressions;
using Shizou.Data.Models;

namespace Shizou.Data.FilterCriteria;

public record TermCriterion(bool Negated, Expression<Func<AniDbAnime, bool>> Criterion) : AnimeCriterion(Negated, Criterion)
{
}

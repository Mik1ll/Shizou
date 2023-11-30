using System.Linq.Expressions;
using System.Text.Json.Serialization;
using Shizou.Data.Models;

namespace Shizou.Data.FilterCriteria;

public abstract record AnimeCriterion
{
    [JsonIgnore]
    public Expression<Func<AniDbAnime, bool>> Criterion => Create();

    protected abstract Expression<Func<AniDbAnime, bool>> Create();
}

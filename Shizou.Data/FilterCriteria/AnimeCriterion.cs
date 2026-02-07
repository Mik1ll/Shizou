using System.Linq.Expressions;
using System.Text.Json.Serialization;
using Shizou.Data.Models;

namespace Shizou.Data.FilterCriteria;

public abstract record AnimeCriterion
{
    [JsonIgnore]
    // ReSharper disable once UnusedMemberInSuper.Global
    public abstract Expression<Func<AniDbAnime, bool>> Predicate { get; }
}

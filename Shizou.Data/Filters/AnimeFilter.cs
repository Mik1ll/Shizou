using System.Linq.Expressions;
using System.Text.Json.Serialization;
using Shizou.Data.Models;

namespace Shizou.Data.Filters;

[JsonDerivedType(typeof(AirDateFilter), nameof(AirDateFilter))]
public abstract class AnimeFilter
{
    private readonly Expression<Func<AniDbAnime, bool>> _filterCore;

    protected AnimeFilter(Expression<Func<AniDbAnime, bool>> filterCore, bool negate)
    {
        _filterCore = filterCore;
        Negate = negate;
    }

    [JsonInclude]
    public bool Negate { get; }

    [JsonIgnore]
    public Expression<Func<AniDbAnime, bool>> Filter =>
        Negate ? Expression.Lambda<Func<AniDbAnime, bool>>(Expression.Not(_filterCore.Body), false, _filterCore.Parameters) : _filterCore;
}

using System.Linq.Expressions;
using System.Text.Json.Serialization;
using Shizou.Data.Models;

namespace Shizou.Data.Filters;

[JsonDerivedType(typeof(AirDateFilter), nameof(AirDateFilter))]
public interface IAnimeFilter
{
    Expression<Func<AniDbAnime, bool>> AnimeFilter { get; }
}

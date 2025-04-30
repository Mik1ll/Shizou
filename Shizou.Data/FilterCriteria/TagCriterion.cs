using System.Linq.Expressions;
using Shizou.Data.Models;

namespace Shizou.Data.FilterCriteria;

public record TagCriterion : TermCriterion
{
    public string Tag { get; set; } = string.Empty;

    protected override Expression<Func<AniDbAnime, bool>> MakeTerm()
    {
        return anime => anime.Tags.Contains(Tag);
    }
}

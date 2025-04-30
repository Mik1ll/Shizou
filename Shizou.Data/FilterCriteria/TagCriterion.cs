using System.Linq.Expressions;
using JetBrains.Annotations;
using Shizou.Data.Models;

namespace Shizou.Data.FilterCriteria;

public record TagCriterion(bool Negated, string Tag) : TermCriterion(Negated)
{
    [UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
    public TagCriterion() : this(false, string.Empty)
    {
    }

    public string Tag { get; set; } = Tag;

    protected override Expression<Func<AniDbAnime, bool>> MakeTerm()
    {
        return anime => anime.Tags.Contains(Tag);
    }
}

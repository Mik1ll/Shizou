using System.Linq.Expressions;
using JetBrains.Annotations;
using Shizou.Data.Models;

namespace Shizou.Data.FilterCriteria;

public record ReleaseGroupCriterion(bool Negated, int? GroupId) : TermCriterion(Negated)
{
    [UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
    public ReleaseGroupCriterion() : this(false, null)
    {
    }

    public int? GroupId { get; set; } = GroupId;

    protected override Expression<Func<AniDbAnime, bool>> MakeTerm()
    {
        return anime => anime.AniDbEpisodes.Any(ep => ep.AniDbFiles.Cast<AniDbNormalFile>().Any(f => f.AniDbGroupId == GroupId));
    }
}

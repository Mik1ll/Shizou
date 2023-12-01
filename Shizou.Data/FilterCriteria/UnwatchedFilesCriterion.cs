using System.Linq.Expressions;
using Shizou.Data.Models;

namespace Shizou.Data.FilterCriteria;

public record UnwatchedFilesCriterion(bool Negated) : TermCriterion(Negated)
{
    protected override Expression<Func<AniDbAnime, bool>> MakeTerm()
    {
        return anime => anime.AniDbEpisodes.Any(e =>
            e.AniDbFiles.Any(f => f.FileWatchedState.Watched == false) || (e.ManualLinkLocalFiles.Any() && e.EpisodeWatchedState.Watched == false));
    }
}

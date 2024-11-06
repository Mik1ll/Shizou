using System.Linq.Expressions;
using Shizou.Data.Enums;
using Shizou.Data.Models;

namespace Shizou.Data.FilterCriteria;

public record WatchedFilesCriterion(bool Negated) : TermCriterion(Negated)
{
    // ReSharper disable once UnusedMember.Global
    public WatchedFilesCriterion() : this(false)
    {
    }

    protected override Expression<Func<AniDbAnime, bool>> MakeTerm()
    {
        return anime => anime.AniDbEpisodes.Any(e =>
            e.EpisodeType != EpisodeType.Credits && e.EpisodeType != EpisodeType.Trailer &&
            e.AniDbFiles.Any(f => f.FileWatchedState.Watched && f.LocalFiles.Any()));
    }
}

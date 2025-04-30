using Microsoft.AspNetCore.Components;
using Shizou.Data.Database;
using Shizou.Data.FilterCriteria;
using Shizou.Data.Models;

namespace Shizou.Blazor.Components.Pages.Collection.Components;

public partial class CriteriaPicker
{
    private static readonly Dictionary<string, (string Name, Func<TermCriterion> Factory)> TermFactories = new()
    {
        { nameof(AirDateCriterion), ("Air Date", () => new AirDateCriterion()) },
        { nameof(AnimeTypeCriterion), ("Anime Type", () => new AnimeTypeCriterion()) },
        { nameof(EpisodeWithoutFilesCriterion), ("Has Episode Without Files", () => new EpisodeWithoutFilesCriterion()) },
        { nameof(GenericFilesCriterion), ("Has Generic Files", () => new GenericFilesCriterion()) },
        { nameof(RestrictedCriterion), ("Restricted", () => new RestrictedCriterion()) },
        { nameof(ReleaseGroupCriterion), ("Release Group", () => new ReleaseGroupCriterion()) },
        { nameof(SeasonCriterion), ("Season", () => new SeasonCriterion()) },
        { nameof(TagCriterion), ("Tag", () => new TagCriterion()) },
        { nameof(UnwatchedFilesCriterion), ("Unwatched Files", () => new UnwatchedFilesCriterion()) },
        { nameof(WatchedFilesCriterion), ("Watched Files", () => new WatchedFilesCriterion()) },
    };

    private List<AniDbGroup>? _anidbGroups;

    [Inject]
    private IShizouContextFactory ContextFactory { get; set; } = null!;

    [Parameter]
    [EditorRequired]
    public AnimeFilter Filter { get; set; } = null!;

    private void Update(OrAnyCriterion or, AndAllCriterion? and, int index, string? newTermType)
    {
        if (string.IsNullOrWhiteSpace(newTermType))
        {
            if (and?.Criteria.Count > index)
                and.Criteria.RemoveAt(index);
            or.Criteria.RemoveAll(a => a.Criteria.Count == 0);
            StateHasChanged();
            return;
        }

        var term = TermFactories[newTermType].Factory();
        if (and?.Criteria.Count > index)
        {
            and.Criteria[index] = term;
        }
        else
        {
            if (and is null)
                or.Criteria.Add(and = new AndAllCriterion([]));
            and.Criteria.Add(term);
        }

        StateHasChanged();
    }

    private string GetReleaseGroupPlaceholder(int? groupId)
    {
        GetAnidbGroups();
        return _anidbGroups!.FirstOrDefault(g => g.Id == groupId)?.ShortName ?? "Search group...";
    }

    private Task<List<(int, string)>?> GetReleaseGroupsAsync(string search)
    {
        GetAnidbGroups();

        var results = _anidbGroups!.Where(g =>
                g.Name.StartsWith(search, StringComparison.OrdinalIgnoreCase) ||
                g.ShortName.StartsWith(search, StringComparison.OrdinalIgnoreCase))
            .Take(20).ToList();

        return Task.FromResult(results.Select(g => (g.Id, g.ShortName)).ToList())!;
    }

    private void GetAnidbGroups()
    {
        if (_anidbGroups is not null) return;
        using var context = ContextFactory.CreateDbContext();
        _anidbGroups = context.AniDbGroups.ToList();
    }
}

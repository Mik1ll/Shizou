using Microsoft.AspNetCore.Components;
using Shizou.Data.Database;
using Shizou.Data.Enums;
using Shizou.Data.FilterCriteria;
using Shizou.Data.Models;

namespace Shizou.Blazor.Components.Pages.Collection.Components;

public partial class CriteriaPicker
{
    private Dictionary<string, Type> _validTermTypes = new()
    {
        { nameof(AirDateCriterion), typeof(AirDateCriterion) },
        { nameof(UnwatchedFilesCriterion), typeof(UnwatchedFilesCriterion) },
        { nameof(EpisodeWithoutFilesCriterion), typeof(EpisodeWithoutFilesCriterion) },
        { nameof(AnimeTypeCriterion), typeof(AnimeTypeCriterion) },
        { nameof(GenericFilesCriterion), typeof(GenericFilesCriterion) },
        { nameof(ReleaseGroupCriterion), typeof(ReleaseGroupCriterion) }
    };

    private List<AniDbGroup>? _anidbGroups;

    [Inject]
    private IShizouContextFactory ContextFactory { get; set; } = default!;

    [Parameter]
    [EditorRequired]
    public AnimeFilter Filter { get; set; } = default!;

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

        TermCriterion term = _validTermTypes[newTermType] switch
        {
            { } t when t == typeof(AirDateCriterion) => new AirDateCriterion(false, AirDateTermType.AirDate, AirDateTermRange.Before),
            { } t when t == typeof(UnwatchedFilesCriterion) => new UnwatchedFilesCriterion(false),
            { } t when t == typeof(EpisodeWithoutFilesCriterion) => new EpisodeWithoutFilesCriterion(false),
            { } t when t == typeof(AnimeTypeCriterion) => new AnimeTypeCriterion(false, AnimeType.TvSeries),
            { } t when t == typeof(GenericFilesCriterion) => new GenericFilesCriterion(false),
            { } t when t == typeof(ReleaseGroupCriterion) => new ReleaseGroupCriterion(false, 0),
            _ => throw new ArgumentOutOfRangeException()
        };
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

    private void Replace(AndAllCriterion and, int index, TermCriterion newCriterion)
    {
        and.Criteria[index] = newCriterion;
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
        if (_anidbGroups is null)
        {
            using var context = ContextFactory.CreateDbContext();
            _anidbGroups = context.AniDbGroups.ToList();
        }
    }
}

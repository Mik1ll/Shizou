using Microsoft.AspNetCore.Components;
using Shizou.Data.Database;
using Shizou.Data.FilterCriteria;
using Shizou.Data.Models;

namespace Shizou.Blazor.Components.Pages.Collection.Components;

public partial class CriteriaPicker
{
    private static readonly Dictionary<string, Type> TermTypes = [];

    static CriteriaPicker()
    {
        var baseType = typeof(TermCriterion);
        foreach (var type in baseType.Assembly.GetTypes().Where(t => t.IsSubclassOf(baseType)))
            TermTypes.Add(type.Name, type);
    }

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

        var term = (TermCriterion)(Activator.CreateInstance(TermTypes[newTermType]) ??
                                   throw new NullReferenceException($"CreateInstance returned null for term: {newTermType}"));
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

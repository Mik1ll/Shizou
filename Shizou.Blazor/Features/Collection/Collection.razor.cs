using Microsoft.AspNetCore.Components;
using Shizou.Data.Database;
using Shizou.Data.Models;
using Shizou.Server.Extensions.Query;

namespace Shizou.Blazor.Features.Collection;

// ReSharper disable once ClassNeverInstantiated.Global
public partial class Collection
{
    private List<AniDbAnime> _anime = default!;
    private int? _selectedFilterId;

    [Inject]
    private IShizouContextFactory ContextFactory { get; set; } = default!;

    protected override void OnInitialized()
    {
        RefreshAnime();
    }

    private void RefreshAnime()
    {
        using var context = ContextFactory.CreateDbContext();
        var filter = context.AnimeFilters.FirstOrDefault(f => f.Id == _selectedFilterId);
        _anime = context.AniDbAnimes.HasLocalFiles()
            .Where(filter?.Criteria.Criterion ?? (a => true))
            .ToList();
    }

    private void OnSelectedFilterChanged(int? id)
    {
        _selectedFilterId = id;
        RefreshAnime();
    }
}

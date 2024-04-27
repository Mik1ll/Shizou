using System.Web;
using Microsoft.AspNetCore.Components;
using Shizou.Blazor.Components.Shared;
using Shizou.Blazor.Services;
using Shizou.Data.Database;
using Shizou.Data.FilterCriteria;
using Shizou.Data.Models;

namespace Shizou.Blazor.Components.Pages.Collection.Components;

public partial class FilterOffcanvas
{
    private Offcanvas _offcanvas = default!;
    private AnimeFilter? _filter = default!;

    [Inject]
    private IShizouContextFactory ContextFactory { get; set; } = default!;

    [Inject]
    private ToastService ToastService { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Parameter]
    [EditorRequired]
    public Collection Collection { get; set; } = default!;

    public async Task OpenAsync(int? filterId = null)
    {
        if (filterId is null)
        {
            _filter = new AnimeFilter
            {
                Name = "New Filter",
                Criteria = new OrAnyCriterion([])
            };
        }
        else
        {
            using var context = ContextFactory.CreateDbContext();
            _filter = context.AnimeFilters.FirstOrDefault(f => f.Id == filterId);
        }

        await _offcanvas.OpenAsync();
    }

    private async Task SaveFilterAsync()
    {
        if (_filter is null)
        {
            ToastService.ShowError("Filter Error", "No filter was being edited when trying to save filter");
            return;
        }

        using var context = ContextFactory.CreateDbContext();
        var eFilter = context.AnimeFilters.FirstOrDefault(f => f.Id == _filter.Id);
        if (eFilter is not null)
            context.Entry(eFilter).CurrentValues.SetValues(_filter);
        else
            context.AnimeFilters.Add(_filter);
        context.SaveChanges();
        var oldFilterId = HttpUtility.ParseQueryString(new Uri(NavigationManager.Uri).Query).Get(nameof(Collection.FilterId));
        if (oldFilterId != _filter.Id.ToString())
            NavigationManager.NavigateTo(NavigationManager.GetUriWithQueryParameter(nameof(Collection.FilterId), (int?)_filter.Id));
        else
            Collection.Load();
        await CloseAsync();
    }

    private async Task CloseAsync()
    {
        await _offcanvas.CloseAsync();
    }
}

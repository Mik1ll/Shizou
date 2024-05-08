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
    private bool _editing;

    [Inject]
    private IShizouContextFactory ContextFactory { get; set; } = default!;

    [Inject]
    private ToastService ToastService { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Parameter]
    [EditorRequired]
    public EventCallback ReloadCollection { get; set; }

    public async Task OpenNewAsync()
    {
        _editing = false;
        _filter = new AnimeFilter
        {
            Name = "New Filter",
            Criteria = new OrAnyCriterion([])
        };
        await _offcanvas.OpenAsync();
    }

    public async Task OpenEditAsync(AnimeFilter filter)
    {
        _editing = true;
        _filter = filter;
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

        if (_editing)
            await ReloadCollection.InvokeAsync();
        else
            NavigationManager.NavigateTo(NavigationManager.GetUriWithQueryParameter(nameof(Collection.FilterId), _filter.Id));
        await CloseAsync();
    }

    private async Task DeleteFilterAsync()
    {
        if (_filter is null)
            return;
        using var context = ContextFactory.CreateDbContext();
        var eFilter = context.AnimeFilters.FirstOrDefault(f => f.Id == _filter.Id);
        if (eFilter is not null)
        {
            context.AnimeFilters.Remove(eFilter);
            context.SaveChanges();
            await ReloadCollection.InvokeAsync();
        }

        await CloseAsync();
    }

    private async Task CloseAsync()
    {
        await _offcanvas.CloseAsync();
    }
}

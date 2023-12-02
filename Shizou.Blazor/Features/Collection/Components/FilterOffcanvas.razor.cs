using Microsoft.AspNetCore.Components;
using Shizou.Blazor.Services;
using Shizou.Data.Database;
using Shizou.Data.FilterCriteria;
using Shizou.Data.Models;

namespace Shizou.Blazor.Features.Collection.Components;

public partial class FilterOffcanvas
{
    private List<AnimeFilter> _filters = default!;
    private AnimeFilter? _editingFilter;

    [Inject]
    private IShizouContextFactory ContextFactory { get; set; } = default!;

    [Inject]
    private ToastService ToastService { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Parameter]
    public int? SelectedFilterId { get; set; }

    [Parameter]
    public EventCallback<int?> SelectedFilterIdChanged { get; set; }

    protected override void OnInitialized()
    {
        RefreshFilters();
    }

    private async Task UpdateSelectedFilterIdAsync(int? id)
    {
        SelectedFilterId = id;
        await SelectedFilterIdChanged.InvokeAsync(id);
    }

    private void OnSelect(ChangeEventArgs e)
    {
        NavigationManager.NavigateTo(NavigationManager.GetUriWithQueryParameter(nameof(Collection.FilterId), (string?)e.Value));
    }

    private void RefreshFilters()
    {
        using var context = ContextFactory.CreateDbContext();
        _filters = context.AnimeFilters.ToList();
    }

    private void CreateFilter()
    {
        _editingFilter = new AnimeFilter
        {
            Name = "New Filter",
            Criteria = new OrAnyCriterion(new List<AndAllCriterion>())
        };
    }

    private async Task SaveFilterAsync()
    {
        if (_editingFilter is null)
        {
            ToastService.ShowError("Filter Error", "No filter was being edited when trying to save filter");
            return;
        }

        using var context = ContextFactory.CreateDbContext();
        var eFilter = context.AnimeFilters.FirstOrDefault(f => f.Id == SelectedFilterId);
        if (eFilter is not null)
            context.Entry(eFilter).CurrentValues.SetValues(_editingFilter);
        else
            context.AnimeFilters.Add(_editingFilter);
        context.SaveChanges();
        await UpdateSelectedFilterIdAsync(_editingFilter.Id);
        _editingFilter = null;
        RefreshFilters();
    }

    private void EditFilter()
    {
        if (SelectedFilterId is null)
        {
            ToastService.ShowError("Filter Error", "No filter selected when trying to edit filter");
            return;
        }

        _editingFilter = _filters.First(f => f.Id == SelectedFilterId);
    }
}

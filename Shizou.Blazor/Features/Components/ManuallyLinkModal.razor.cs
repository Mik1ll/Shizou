using Blazored.Modal;
using Blazored.Modal.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Shizou.Data.Database;
using Shizou.Data.Models;
using Shizou.Server.Commands.AniDb;
using Shizou.Server.Services;

namespace Shizou.Blazor.Features.Components;

public partial class ManuallyLinkModal
{
    private int? _selected;
    private AniDbAnime? _selectedAnime;
    private bool _restrictInCollection = true;

    [Inject]
    private AnimeTitleSearchService AnimeTitleSearchService { get; set; } = default!;

    [Inject]
    private CommandService CommandService { get; set; } = default!;

    [Inject]
    private IDbContextFactory<ShizouContext> ContextFactory { get; set; } = default!;

    [CascadingParameter]
    private IModalService ModalService { get; set; } = default!;

    [CascadingParameter]
    private BlazoredModalInstance ModalInstance { get; set; } = default!;

    [CascadingParameter]
    private ToastDisplay ToastDisplay { get; set; } = default!;

    [Parameter]
    public List<LocalFile> SelectedFiles { get; set; } = default!;

    protected override void OnParametersSet()
    {
        if (SelectedFiles is null)
            throw new ArgumentNullException(nameof(SelectedFiles));
    }

    private async Task Cancel()
    {
        await ModalInstance.CancelAsync();
    }

    private async Task<List<(int, string)>?> GetTitles(string query)
    {
        return (await AnimeTitleSearchService.Search(query, _restrictInCollection))?.Select(p => (p.Item1, $"{p.Item1} {p.Item2}")).ToList();
    }

    private void SelectAnime()
    {
        if (_selected is null)
            return;
        using var context = ContextFactory.CreateDbContext();
        if (context.AniDbAnimes.FirstOrDefault(a => a.Id == _selected) is { } anime)
        {
            _selectedAnime = anime;
        }
        else
        {
            CommandService.Dispatch(new AnimeArgs(_selected.Value));
            ToastDisplay.AddToast($"Queueing add anime {_selected}", "You must wait for the command to complete before it is available",
                ToastStyle.Success);
        }
    }

    private void ClearSelection()
    {
        _selectedAnime = null;
    }
}

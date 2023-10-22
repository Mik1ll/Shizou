using Blazored.Modal;
using Blazored.Modal.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Shizou.Blazor.Extensions;
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
    private readonly Dictionary<AniDbEpisode, LocalFile> _mapping = new();

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

    public override Task SetParametersAsync(ParameterView parameters)
    {
        parameters.EnsureParametersSet(nameof(SelectedFiles));
        return base.SetParametersAsync(parameters);
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
        if (context.AniDbAnimes.Include(a => a.AniDbEpisodes.OrderBy(ep => ep.EpisodeType)
                .ThenBy(ep => ep.Number)).FirstOrDefault(a => a.Id == _selected) is { } anime)
        {
            _selectedAnime = anime;
            if (SelectedFiles.Count > _selectedAnime.AniDbEpisodes.Count)
            {
                ToastDisplay.AddToast("More files than episodes to link", "You are trying to link more files than there are existing episodes in the anime",
                    ToastStyle.Error);
                _selectedAnime = null;
                return;
            }

            foreach (var (file, ep) in SelectedFiles.Zip(_selectedAnime.AniDbEpisodes)) _mapping[ep] = file;
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
        _mapping.Clear();
    }

    private void MoveLocalFile(AniDbEpisode ep, LocalFile localFile, int idx)
    {
        var newEp = _selectedAnime!.AniDbEpisodes[idx];
        if (_mapping.TryGetValue(newEp, out var otherLocal))
            _mapping[ep] = otherLocal;
        else
            _mapping.Remove(ep);
        _mapping[newEp] = localFile;
    }

    private async Task LinkFiles()
    {
        // ReSharper disable once UseAwaitUsing
        // ReSharper disable once MethodHasAsyncOverload
        using var context = ContextFactory.CreateDbContext();
        context.LocalFiles.AttachRange(SelectedFiles);
        foreach (var ep in _selectedAnime!.AniDbEpisodes)
            if (_mapping.TryGetValue(ep, out var localFile))
                localFile.ManualLinkEpisodeId = ep.Id;

        // ReSharper disable once MethodHasAsyncOverload
        context.SaveChanges();
        await ModalInstance.CloseAsync();
    }
}

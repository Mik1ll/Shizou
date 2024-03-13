using Blazored.Modal.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Shizou.Blazor.Extensions;
using Shizou.Blazor.Services;
using Shizou.Data.CommandInputArgs;
using Shizou.Data.Database;
using Shizou.Data.Models;
using Shizou.Server.Services;

namespace Shizou.Blazor.Components.Shared;

public partial class ManuallyLinkModal
{
    private readonly Dictionary<AniDbEpisode, LocalFile> _mapping = new();
    private int? _selected;
    private AniDbAnime? _selectedAnime;
    private bool _restrictInCollection = true;
    private Modal _modal = default!;

    [Inject]
    private AnimeTitleSearchService AnimeTitleSearchService { get; set; } = default!;

    [Inject]
    private CommandService CommandService { get; set; } = default!;

    [Inject]
    private IShizouContextFactory ContextFactory { get; set; } = default!;

    [Inject]
    private ToastService ToastService { get; set; } = default!;

    [Inject]
    private ManualLinkService ManualLinkService { get; set; } = default!;

    [CascadingParameter]
    private IModalService ModalService { get; set; } = default!;

    [Parameter]
    public List<LocalFile> SelectedFiles { get; set; } = default!;

    public override Task SetParametersAsync(ParameterView parameters)
    {
        parameters.EnsureParametersSet(nameof(SelectedFiles));
        return base.SetParametersAsync(parameters);
    }

    private async Task CancelAsync()
    {
        await _modal.CancelAsync();
    }

    private async Task<List<(int, string)>?> GetTitlesAsync(string query)
    {
        return (await AnimeTitleSearchService.SearchAsync(query, _restrictInCollection))?.Select(p => (p.Item1, $"{p.Item1} {p.Item2}")).ToList();
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
                ToastService.ShowError("More files than episodes to link", "You are trying to link more files than there are existing episodes in the anime");
                _selectedAnime = null;
                return;
            }

            foreach (var (file, ep) in SelectedFiles.Zip(_selectedAnime.AniDbEpisodes)) _mapping[ep] = file;
        }
        else
        {
            CommandService.Dispatch(new AnimeArgs(_selected.Value));
            ToastService.ShowSuccess($"Queueing add anime {_selected}", "You must wait for the command to complete before it is available");
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

    private async Task LinkFilesAsync()
    {
        foreach (var ep in _selectedAnime!.AniDbEpisodes)
            if (_mapping.TryGetValue(ep, out var localFile))
            {
                var result = ManualLinkService.LinkFile(localFile, ep.Id);
                if (result is ManualLinkService.LinkResult.Maybe)
                    ToastService.ShowWarn("Manual Link Result",
                        $"Manual link for \"{Path.GetFileName(localFile.PathTail)}\" pending, wait for update mylist udp command to finish");
            }

        await _modal.CloseAsync();
    }
}

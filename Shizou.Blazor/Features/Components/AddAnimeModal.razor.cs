using Blazored.Modal;
using Blazored.Modal.Services;
using Microsoft.AspNetCore.Components;
using Shizou.Server.Commands.AniDb;
using Shizou.Server.Services;

namespace Shizou.Blazor.Features.Components;

public partial class AddAnimeModal
{
    private int? _selected;

    [CascadingParameter]
    public IModalService ModalService { get; set; } = default!;

    [CascadingParameter]
    public BlazoredModalInstance ModalInstance { get; set; } = default!;

    [CascadingParameter]
    public ToastDisplay ToastDisplay { get; set; } = default!;

    [Inject]
    public AnimeTitleSearchService AnimeTitleSearchService { get; set; } = default!;

    [Inject]
    public CommandService CommandService { get; set; } = default!;


    private async Task<List<(int, string)>?> GetTitles(string query)
    {
        return (await AnimeTitleSearchService.Search(query))?.Select(p => (p.Item1, $"{p.Item1} {p.Item2}")).ToList();
    }

    private void AddAnime()
    {
        if (_selected is null)
        {
            ToastDisplay.AddToast("Add anime failed", "No anime to add!", ToastStyle.Error);
        }
        else
        {
            CommandService.Dispatch(new AnimeArgs(_selected.Value));
            ToastDisplay.AddToast($"Adding anime {_selected}", "You may need to wait for the anime to be processed before it is available", ToastStyle.Success);
        }

        ModalInstance.CloseAsync();
    }
}

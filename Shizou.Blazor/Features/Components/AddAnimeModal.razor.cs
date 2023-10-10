using Blazored.Modal;
using Blazored.Modal.Services;
using Microsoft.AspNetCore.Components;
using Shizou.Server.Services;

namespace Shizou.Blazor.Features.Components;

public partial class AddAnimeModal
{
    private int? _selected;

    [CascadingParameter]
    public IModalService ModalService { get; set; } = default!;

    [CascadingParameter]
    public BlazoredModalInstance ModalInstance { get; set; } = default!;

    [Inject]
    public AnimeTitleSearchService AnimeTitleSearchService { get; set; } = default!;

    private async Task<List<(int, string)>?> GetTitles(string query)
    {
        return (await AnimeTitleSearchService.Search(query))?.Select(p => (p.Item1, $"{p.Item1} {p.Item2}")).ToList();
    }

    private async Task Cancel()
    {
        await ModalInstance.CancelAsync();
    }

    private async Task Close()
    {
        await ModalInstance.CloseAsync();
    }
}

using Blazored.Modal;
using Blazored.Modal.Services;
using Microsoft.AspNetCore.Components;
using Shizou.Server.Services;

namespace Shizou.Blazor.Features.Components;

public partial class ManuallyLinkModal
{
    private int? _selected;

    [Inject]
    private AnimeTitleSearchService AnimeTitleSearchService { get; set; } = default!;

    [CascadingParameter]
    private IModalService ModalService { get; set; } = default!;

    [CascadingParameter]
    private BlazoredModalInstance ModalInstance { get; set; } = default!;

    private async Task Cancel()
    {
        await ModalInstance.CancelAsync();
    }

    private async Task<List<(int, string)>?> GetTitles(string query)
    {
        return (await AnimeTitleSearchService.Search(query, true))?.Select(p => (p.Item1, $"{p.Item1} {p.Item2}")).ToList();
    }

    private async Task OpenAnimeModal()
    {
        await ModalService.Show<AddAnimeModal>().Result;
    }
}

using Microsoft.AspNetCore.Components;
using Shizou.Server.Services;

namespace Shizou.Blazor.Features.Components;

public partial class AddAnimeModal
{
    private int? _selected;
    private bool _dialogIsOpen;

    [Inject]
    public AnimeTitleSearchService AnimeTitleSearchService { get; set; } = default!;

    private async Task<List<(int, string)>?> GetTitles(string query)
    {
        return (await AnimeTitleSearchService.Search(query))?.Select(p => (p.Item1, $"{p.Item1} {p.Item2}")).ToList();
    }

    private void OnClose(bool accepted)
    {
    }
}

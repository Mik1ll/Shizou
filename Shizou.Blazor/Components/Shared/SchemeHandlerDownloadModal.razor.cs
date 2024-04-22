using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace Shizou.Blazor.Components.Shared;

public partial class SchemeHandlerDownloadModal : ComponentBase
{
    private Modal _modal = default!;

    [Inject]
    private ProtectedLocalStorage LocalStorage { get; set; } = default!;

    private async Task InstalledAsync()
    {
        await LocalStorage.SetAsync(LocalStorageKeys.SchemeHandlerInstalled, true);
        await _modal.CloseAsync();
    }

    private async Task DismissAsync()
    {
        await _modal.CancelAsync();
    }
}

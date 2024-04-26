using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Shizou.Blazor.Components.Shared;

public partial class SchemeHandlerDownloadModal : ComponentBase
{
    private Modal _modal = default!;

    [Inject]
    private IJSRuntime JsRuntime { get; set; } = default!;

    private async Task InstalledAsync()
    {
        await JsRuntime.InvokeVoidAsync("window.localStorage.setItem", LocalStorageKeys.SchemeHandlerInstalled, true);
        await _modal.CloseAsync();
    }

    private async Task DismissAsync()
    {
        await _modal.CancelAsync();
    }
}

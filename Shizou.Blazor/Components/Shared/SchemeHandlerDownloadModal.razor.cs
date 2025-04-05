using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Shizou.Blazor.Components.Shared;

public partial class SchemeHandlerDownloadModal : ComponentBase
{
    private Modal _modal = null!;

    [Inject]
    private IJSRuntime JsRuntime { get; set; } = null!;

    private async Task InstalledAsync()
    {
        var settings = await BrowserSettings.GetSettingsAsync(JsRuntime);
        settings.ExternalPlayerInstalled = true;
        await settings.SaveSettingsAsync(JsRuntime);
        await _modal.CloseAsync();
    }

    private async Task DismissAsync()
    {
        await _modal.CancelAsync();
    }
}

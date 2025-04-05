using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using Shizou.Server.Options;

namespace Shizou.Blazor.Components.Pages.Settings;

public partial class Settings
{
    private bool _render;
    private ShizouOptions? ServerSettings { get; set; }

    private BrowserSettings? BrowserSettings { get; set; }

    [Inject]
    private IOptionsSnapshot<ShizouOptions> OptionsSnapShot { get; set; } = null!;

    [Inject]
    private IJSRuntime JsRuntime { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        ServerSettings = OptionsSnapShot.Value;
        BrowserSettings = await BrowserSettings.GetSettingsAsync(JsRuntime);
        _render = true;
    }

    private void Save()
    {
        ServerSettings!.SaveToFile();
    }

    private async Task SaveBrowserAsync()
    {
        BrowserSettings!.ExternalPlayerScheme = BrowserSettings.ExternalPlayerScheme.Trim();
        if (!string.IsNullOrWhiteSpace(BrowserSettings.ExternalPlayerScheme) && !BrowserSettings.ExternalPlayerScheme.EndsWith(':'))
            BrowserSettings.ExternalPlayerScheme += ':';
        await BrowserSettings.SaveSettingsAsync(JsRuntime);
    }
}

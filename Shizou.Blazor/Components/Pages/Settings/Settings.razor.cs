using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using Shizou.Server.Options;

namespace Shizou.Blazor.Components.Pages.Settings;

public partial class Settings
{
    private ShizouOptions _options = default!;

    private string _externalPlayerScheme = LocalStorageKeys.ExternalPlayerSchemeDefault;

    private string FileExtensionWrapper
    {
        get => string.Join(" ", _options.Import.FileExtensions);
        set => _options.Import.FileExtensions = value.Split(" ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToArray();
    }

    [Inject]
    private IOptionsSnapshot<ShizouOptions> OptionsSnapShot { get; set; } = default!;

    [Inject]
    private IJSRuntime JsRuntime { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        _options = OptionsSnapShot.Value;
        var extPlayerSchemeResult = await JsRuntime.InvokeAsync<JsonElement>("window.localStorage.getItem", LocalStorageKeys.ExternalPlayerScheme);
        _externalPlayerScheme = extPlayerSchemeResult is { ValueKind: JsonValueKind.String }
            ? extPlayerSchemeResult.GetString()!
            : LocalStorageKeys.ExternalPlayerSchemeDefault;
    }

    private async Task SaveAsync()
    {
        _externalPlayerScheme = _externalPlayerScheme.Trim();
        if (!string.IsNullOrWhiteSpace(_externalPlayerScheme) && !_externalPlayerScheme.EndsWith(':'))
            _externalPlayerScheme += ':';
        await JsRuntime.InvokeVoidAsync("window.localStorage.setItem", LocalStorageKeys.ExternalPlayerScheme, _externalPlayerScheme);
        _options.SaveToFile();
    }
}

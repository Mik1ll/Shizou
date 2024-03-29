﻿using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.Extensions.Options;
using Shizou.Server.Options;

namespace Shizou.Blazor.Components.Pages.Settings;

public partial class Settings
{
    private ShizouOptions _options = default!;

    private string _externalPlayerScheme = LocalStorageKeys.ExternalPlayerSchemeDefault;

    [Inject]
    private IOptionsSnapshot<ShizouOptions> OptionsSnapShot { get; set; } = default!;

    [Inject]
    private ProtectedLocalStorage LocalStorage { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        _options = OptionsSnapShot.Value;
        var extPlayerSchemeResult = await LocalStorage.GetAsync<string>(LocalStorageKeys.ExternalPlayerScheme);
        _externalPlayerScheme = extPlayerSchemeResult.Value ?? LocalStorageKeys.ExternalPlayerSchemeDefault;
    }

    private async Task SaveAsync()
    {
        _options.SaveToFile();
        await LocalStorage.SetAsync(LocalStorageKeys.ExternalPlayerScheme, _externalPlayerScheme);
    }
}

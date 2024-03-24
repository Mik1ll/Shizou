using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Shizou.Data.Database;
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

    [Inject]
    private IServiceProvider ServiceProvider { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        _options = OptionsSnapShot.Value;
        var extPlayerSchemeResult = await LocalStorage.GetAsync<string>(LocalStorageKeys.ExternalPlayerScheme);
        _externalPlayerScheme = extPlayerSchemeResult.Value ?? LocalStorageKeys.ExternalPlayerSchemeDefault;
    }

    private async Task SaveAsync()
    {
        await LocalStorage.SetAsync(LocalStorageKeys.ExternalPlayerScheme, _externalPlayerScheme);
        _options.SaveToFile();
        using var context = ServiceProvider.GetRequiredService<IShizouContext>();
        // ReSharper disable once MethodHasAsyncOverload
        context.Database.Migrate();
    }
}

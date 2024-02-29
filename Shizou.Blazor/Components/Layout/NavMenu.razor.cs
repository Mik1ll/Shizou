using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.JSInterop;

namespace Shizou.Blazor.Components.Layout;

public partial class NavMenu
{
    private bool _collapsed = true;
    private bool _isDarkMode = false;
    private string? _themeColor;
    private bool _expandFileUtils = false;

    private string? NavMenuCssClass => _collapsed ? null : "show";

    [Inject]
    private IJSRuntime JsRuntime { get; set; } = default!;

    [Inject]
    private ProtectedLocalStorage LocalStorage { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        if (await LocalStorage.GetAsync<string>(LocalStorageKeys.Theme) is { Success: true, Value: var themeRes })
            _isDarkMode = themeRes == "dark";
        else
            _isDarkMode = await JsRuntime.InvokeAsync<string>("getPreferredTheme") == "dark";

        if (_isDarkMode)
            await JsRuntime.InvokeVoidAsync("document.documentElement.setAttribute", "data-bs-theme", "dark");
        _themeColor = await JsRuntime.InvokeAsync<string>("getThemeColor");
    }

    private void ToggleCollapse() => _collapsed = !_collapsed;

    private void ToggleFileUtilDropdown() => _expandFileUtils = !_expandFileUtils;

    private void CloseFileUtilDropdown() => _expandFileUtils = false;

    private async Task ToggleDarkModeAsync()
    {
        _isDarkMode = !_isDarkMode;
        var theme = _isDarkMode ? "dark" : "light";
        await JsRuntime.InvokeVoidAsync("document.documentElement.setAttribute", "data-bs-theme", theme);
        _themeColor = await JsRuntime.InvokeAsync<string>("getThemeColor");
        await LocalStorage.SetAsync(LocalStorageKeys.Theme, theme);
    }
}

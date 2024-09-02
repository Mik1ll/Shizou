using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Shizou.Blazor.Components.Layout;

public partial class NavMenu
{
    private bool _collapsed = true;
    private bool _expandFileUtils = false;
    private string _theme = "auto";
    private Dictionary<string, string> _themeIconClasses = new() { { "light", "bi-sun-fill" }, { "dark", "bi-moon-stars-fill" }, { "auto", "bi-circle-half" } };
    private IJSObjectReference? _themeModule;

    private string? NavMenuCssClass => _collapsed ? null : "show";

    [Inject]
    private IJSRuntime JsRuntime { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;


    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _themeModule = await JsRuntime.InvokeAsync<IJSObjectReference>("import", "/js/theme.js");
            _theme = await _themeModule.InvokeAsync<string>("getStoredTheme");
            StateHasChanged();
        }
    }

    private void ToggleCollapse() => _collapsed = !_collapsed;

    private void ToggleFileUtilDropdown() => _expandFileUtils = !_expandFileUtils;

    private void CloseFileUtilDropdown() => _expandFileUtils = false;

    private async Task ToggleDarkModeAsync()
    {
        _theme = await _themeModule!.InvokeAsync<string>("cycleTheme");
    }

    private void Logout()
    {
        var currentUrl = NavigationManager.ToBaseRelativePath(NavigationManager.Uri);
        NavigationManager.NavigateTo(string.IsNullOrWhiteSpace(currentUrl)
                ? "Account/Logout"
                : $"Account/Logout?returnUrl={Uri.EscapeDataString(currentUrl)}",
            true);
    }
}

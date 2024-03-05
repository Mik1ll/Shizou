using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Shizou.Blazor.Components.Layout;

public partial class NavMenu
{
    private bool _collapsed = true;
    private bool _expandFileUtils = false;
    private string _theme = "auto";
    private Dictionary<string, string> _themeIconClasses = new() { { "light", "bi-sun-fill" }, { "dark", "bi-moon-stars-fill" }, { "auto", "bi-circle-half" } };

    private string? NavMenuCssClass => _collapsed ? null : "show";

    [Inject]
    private IJSRuntime JsRuntime { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        _theme = await JsRuntime.InvokeAsync<string>("getStoredTheme");
    }

    private void ToggleCollapse() => _collapsed = !_collapsed;

    private void ToggleFileUtilDropdown() => _expandFileUtils = !_expandFileUtils;

    private void CloseFileUtilDropdown() => _expandFileUtils = false;

    private async Task ToggleDarkModeAsync()
    {
        _theme = await JsRuntime.InvokeAsync<string>("cycleTheme");
    }
}

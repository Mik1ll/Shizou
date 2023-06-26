using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Shizou.Blazor.Shared;

public partial class ImportFolders
{
    [Inject]
    private IJSRuntime JsRuntime { get; set; } = default!;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await JsRuntime.InvokeVoidAsync("loadTooltip", "scanButton");
        await base.OnAfterRenderAsync(firstRender);
    }
}

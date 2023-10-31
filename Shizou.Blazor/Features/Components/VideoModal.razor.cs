using Blazored.Modal;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Shizou.Blazor.Extensions;

namespace Shizou.Blazor.Features.Components;

public partial class VideoModal
{
    private readonly string _videoId = "videoModalId";
    private readonly List<string> _assSubUrls = new() { "/test.ass", "/test2.ass" };

    [Inject]
    private IJSRuntime JsRuntime { get; set; } = default!;

    [CascadingParameter]
    private BlazoredModalInstance ModalInstance { get; set; } = default!;

    [Parameter]
    public int LocalFileId { get; set; }

    public override Task SetParametersAsync(ParameterView parameters)
    {
        parameters.EnsureParametersSet(nameof(LocalFileId));
        return base.SetParametersAsync(parameters);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await JsRuntime.InvokeVoidAsync("subtitleHandler.loadSubtitle", _videoId, _assSubUrls);
    }

    private async Task Cancel()
    {
        await JsRuntime.InvokeVoidAsync("subtitleHandler.dispose");
        await ModalInstance.CancelAsync();
    }
}

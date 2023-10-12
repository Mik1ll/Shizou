using Microsoft.AspNetCore.Components;

namespace Shizou.Blazor.Features.Components;

public partial class ModalVideo
{
    [Parameter]
    [EditorRequired]
    public int LocalFileId { get; set; }

    [Parameter]
    [EditorRequired]
    public EventCallback<bool> OnClose { get; set; }

    protected override void OnInitialized()
    {
    }
}

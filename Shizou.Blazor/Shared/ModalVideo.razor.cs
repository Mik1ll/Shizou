using Microsoft.AspNetCore.Components;

namespace Shizou.Blazor.Shared;

public partial class ModalVideo
{
    [Parameter] [EditorRequired] public int LocalFileId { get; set; }

    [Parameter] [EditorRequired] public EventCallback<bool> OnClose { get; set; }

    protected override void OnInitialized()
    {
    }
}
using Microsoft.AspNetCore.Components;

namespace Shizou.Blazor.Components.Shared;

public partial class AnimeCard
{
    [Parameter]
    [EditorRequired]
    public int AnimeId { get; set; }

    [Parameter]
    [EditorRequired]
    public string PosterPath { get; set; } = null!;

    [Parameter]
    [EditorRequired]
    public string Title { get; set; } = null!;

    [Parameter]
    public RenderFragment? ChildContent { get; set; }
}

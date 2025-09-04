using Microsoft.AspNetCore.Components;

namespace Shizou.Blazor.Components.Shared;

public class ComponentWithExtraClasses : ComponentBase
{
    protected string ExtraClasses = string.Empty;

    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? AdditionalAttributes { get; set; }

    protected override void OnParametersSet()
    {
        if (AdditionalAttributes is not null && AdditionalAttributes.Remove("class", out var addClasses))
            ExtraClasses = addClasses as string ?? string.Empty;
    }
}

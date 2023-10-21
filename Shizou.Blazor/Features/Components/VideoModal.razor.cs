using Microsoft.AspNetCore.Components;
using Shizou.Blazor.Extensions;

namespace Shizou.Blazor.Features.Components;

public partial class VideoModal
{
    [Parameter]
    public int LocalFileId { get; set; }

    public override Task SetParametersAsync(ParameterView parameters)
    {
        parameters.EnsureParametersSet(nameof(LocalFileId));
        return base.SetParametersAsync(parameters);
    }
}

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using Shizou.Server.Options;

namespace Shizou.Blazor.Features.Settings;

public partial class Settings
{
    private ShizouOptions _options = default!;

    [Inject]
    private IOptionsSnapshot<ShizouOptions> OptionsSnapShot { get; set; } = default!;

    protected override void OnInitialized()
    {
        _options = OptionsSnapShot.Value;
    }
}

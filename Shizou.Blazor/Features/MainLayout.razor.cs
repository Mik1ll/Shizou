using Microsoft.AspNetCore.Components.Web;

namespace Shizou.Blazor.Features;

public partial class MainLayout
{
    private ErrorBoundary? _errorBoundary;

    protected override void OnParametersSet()
    {
        _errorBoundary?.Recover();
        base.OnParametersSet();
    }
}
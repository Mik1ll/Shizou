using Microsoft.AspNetCore.Components;
using Shizou.Server.Commands;
using Shizou.Server.Commands.AniDb;
using Shizou.Server.Services;

namespace Shizou.Blazor.Shared;

public partial class Actions
{
    [Inject]
    private IServiceProvider ServiceProvider { get; set; } = default!;

    private void DispatchNoop()
    {
        ServiceProvider.GetRequiredService<CommandService>().DispatchRange(Enumerable.Range(1, 10).Select(n => new NoopArgs(n)));
    }

    private void RunImport()
    {
        ServiceProvider.GetRequiredService<ImportService>().Import();
    }

    private void DispatchMyListSync()
    {
        ServiceProvider.GetRequiredService<CommandService>().Dispatch(new SyncMyListArgs());
    }
}

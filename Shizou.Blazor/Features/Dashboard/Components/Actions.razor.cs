using Microsoft.AspNetCore.Components;
using Shizou.Server.Commands;
using Shizou.Server.Commands.AniDb;
using Shizou.Server.Services;

namespace Shizou.Blazor.Features.Dashboard.Components;

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

    private void RemoveMissingFiles()
    {
        ServiceProvider.GetRequiredService<ImportService>().RemoveMissingFiles();
    }

    private void DispatchMyListSync()
    {
        ServiceProvider.GetRequiredService<CommandService>().Dispatch(new SyncMyListArgs());
    }

    private void ScheduleNoop()
    {
        ServiceProvider.GetRequiredService<CommandService>().ScheduleCommand(new NoopArgs(5), 3, DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));
    }

    private void GetMissingPosters()
    {
        ServiceProvider.GetRequiredService<ImageService>().GetMissingAnimePosters();
    }
}

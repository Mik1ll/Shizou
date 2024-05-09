using Blazored.Modal;
using Blazored.Modal.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Shizou.Blazor.Components.Shared;
using Shizou.Blazor.Services;
using Shizou.Data;
using Shizou.Data.CommandInputArgs;
using Shizou.Server.Services;

namespace Shizou.Blazor.Components.Pages.Dashboard.Components;

public partial class Actions
{
    [Inject]
    private IServiceProvider ServiceProvider { get; set; } = default!;

    [Inject]
    private IJSRuntime JsRuntime { get; set; } = default!;

    [Inject]
    private ToastService ToastService { get; set; } = default!;

    [Inject]
    private IHttpContextAccessor HttpContextAccessor { get; set; } = default!;

    [Inject]
    private AnimeService AnimeService { get; set; } = default!;

    [CascadingParameter]
    private IModalService ModalService { get; set; } = default!;

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

    private async Task OpenMalAuthAsync()
    {
        var url = ServiceProvider.GetRequiredService<MyAnimeListService>()
            .GetAuthenticationUrl(HttpContextAccessor.HttpContext ?? throw new ArgumentNullException());
        if (url is not null)
            await JsRuntime.InvokeVoidAsync("open", url, "_blank");
    }

    private async Task GetMalListAsync()
    {
        await ServiceProvider.GetRequiredService<MyAnimeListService>().GetUserAnimeListAsync();
    }

    private async Task RestoreFromBackupFileAsync()
    {
        var res = await ModalService.Show<FilePickerModal>("", new ModalParameters
        {
            { nameof(FilePickerModal.FilePickerType), FilePickerType.File },
            { nameof(FilePickerModal.InitialPath), FilePaths.MyListBackupDir }
        }).Result;
        if (res is { Confirmed: true, Data: string path })
            ServiceProvider.GetRequiredService<CommandService>().Dispatch(new RestoreMyListBackupArgs(Path: path));
    }

    private void DisplayToast()
    {
        ToastService.ShowSuccess("blah", "blah");
    }

    private void GetMissingAnime()
    {
        AnimeService.GetMissingAnime();
    }
}

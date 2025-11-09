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
    private AnimeService AnimeService { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = null!;

    [CascadingParameter]
    private IModalService ModalService { get; set; } = default!;

    private void DispatchNoop()
    {
        ServiceProvider.GetRequiredService<CommandService>().Dispatch(Enumerable.Range(1, 10).Select(n => new NoopArgs(n)));
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

    private void OpenMalAuth()
    {
        var url = ServiceProvider.GetRequiredService<MyAnimeListService>().MalAuthorization
            .GetAuthenticationUrl(new Uri(NavigationManager.BaseUri));
        if (url is not null)
            NavigationManager.NavigateTo(url);
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
            { nameof(FilePickerModal.InitialPath), FilePaths.MyListBackupDir },
        }).Result;
        if (res is { Confirmed: true, Data: string path })
            ServiceProvider.GetRequiredService<CommandService>().Dispatch(new RestoreMyListBackupArgs(path));
    }

    private void DisplayToast()
    {
        ToastService.ShowSuccess("blah", "blah");
    }

    private void GetMissingAnime()
    {
        AnimeService.GetMissingEpisodesAndAnime();
    }
}

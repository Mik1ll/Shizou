using Blazored.Modal;
using Blazored.Modal.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Shizou.Data.Database;
using Shizou.Data.Models;

namespace Shizou.Blazor.Features.Components;

public partial class ImportFolderModal
{
    [Inject]
    private IDbContextFactory<ShizouContext> ContextFactory { get; set; } = default!;

    [CascadingParameter]
    private IModalService ModalService { get; set; } = default!;

    [CascadingParameter]
    private BlazoredModalInstance ModalInstance { get; set; } = default!;

    [Parameter]
    public ImportFolder MyImportFolder { get; set; } = default!;

    [Parameter]
    public bool IsDelete { get; set; }


    private async Task OpenFolderPicker()
    {
        var res = await ModalService
            .Show<FolderPickerModal>(string.Empty, new ModalParameters()
                .Add(nameof(FolderPickerModal.FolderPath), MyImportFolder.Path)).Result;
        if (res.Confirmed)
            MyImportFolder.Path = (string?)res.Data ?? string.Empty;
    }

    private async Task Upsert()
    {
        // ReSharper disable once MethodHasAsyncOverload
        // ReSharper disable once UseAwaitUsing
        using var context = ContextFactory.CreateDbContext();
        if (MyImportFolder.Id == 0)
        {
            context.ImportFolders.Add(MyImportFolder);
        }
        else
        {
            // ReSharper disable once MethodHasAsyncOverload
            var importFolder = context.ImportFolders.Find(MyImportFolder.Id);
            if (importFolder is not null)
                context.Entry(importFolder).CurrentValues.SetValues(MyImportFolder);
        }

        // ReSharper disable once MethodHasAsyncOverload
        context.SaveChanges();
        await ModalInstance.CloseAsync();
    }

    private async Task Remove()
    {
        // ReSharper disable once MethodHasAsyncOverload
        // ReSharper disable once UseAwaitUsing
        using var context = ContextFactory.CreateDbContext();
        context.ImportFolders.Remove(MyImportFolder);
        // ReSharper disable once MethodHasAsyncOverload
        context.SaveChanges();
        await ModalInstance.CloseAsync();
    }

    private async Task Cancel()
    {
        await ModalInstance.CancelAsync();
    }
}

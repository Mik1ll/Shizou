using Blazored.Modal;
using Blazored.Modal.Services;
using Microsoft.AspNetCore.Components;
using Shizou.Blazor.Extensions;
using Shizou.Data.Database;
using Shizou.Data.Models;

namespace Shizou.Blazor.Components.Shared;

public partial class ImportFolderModal
{
    private Modal _modal = default!;

    [Inject]
    private IShizouContextFactory ContextFactory { get; set; } = default!;

    [CascadingParameter]
    private IModalService ModalService { get; set; } = default!;

    [Parameter]
    public ImportFolder MyImportFolder { get; set; } = default!;

    [Parameter]
    public bool IsDelete { get; set; }

    public override Task SetParametersAsync(ParameterView parameters)
    {
        parameters.EnsureParametersSet(nameof(MyImportFolder), nameof(IsDelete));
        return base.SetParametersAsync(parameters);
    }

    private async Task OpenFolderPickerAsync()
    {
        var res = await ModalService
            .Show<FilePickerModal>(string.Empty, new ModalParameters
            {
                { nameof(FilePickerModal.InitialPath), MyImportFolder.Path },
                { nameof(FilePickerModal.FilePickerType), FilePickerType.Directory }
            }).Result;
        if (res.Confirmed)
            MyImportFolder.Path = (string?)res.Data ?? string.Empty;
    }

    private async Task UpsertAsync()
    {
        using var context = ContextFactory.CreateDbContext();
        if (MyImportFolder.Id == 0)
        {
            context.ImportFolders.Add(MyImportFolder);
        }
        else
        {
            var importFolder = context.ImportFolders.Find(MyImportFolder.Id);
            if (importFolder is not null)
                context.Entry(importFolder).CurrentValues.SetValues(MyImportFolder);
        }

        context.SaveChanges();
        await _modal.CloseAsync();
    }

    private async Task RemoveAsync()
    {
        using var context = ContextFactory.CreateDbContext();

        // Need to load related for client set null cascading
        // ReSharper disable once MethodHasAsyncOverload
        context.Attach(MyImportFolder).Collection(i => i.LocalFiles).Load();
        context.ImportFolders.Remove(MyImportFolder);

        context.SaveChanges();
        await _modal.CloseAsync();
    }

    private async Task CancelAsync()
    {
        await _modal.CancelAsync();
    }
}

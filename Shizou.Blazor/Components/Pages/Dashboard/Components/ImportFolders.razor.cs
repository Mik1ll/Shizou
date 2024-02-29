using Blazored.Modal;
using Blazored.Modal.Services;
using Microsoft.AspNetCore.Components;
using Shizou.Blazor.Components.Shared;
using Shizou.Data.Database;
using Shizou.Data.Models;
using Shizou.Server.Services;

namespace Shizou.Blazor.Components.Pages.Dashboard.Components;

public partial class ImportFolders
{
    private List<ImportFolder> _importFolders = default!;

    [Inject]
    private IShizouContextFactory ContextFactory { get; set; } = default!;

    [Inject]
    private ImportService ImportService { get; set; } = default!;

    [CascadingParameter]
    private IModalService ModalService { get; set; } = default!;

    protected override void OnInitialized()
    {
        RefreshFolders();
    }

    private void RefreshFolders()
    {
        using var context = ContextFactory.CreateDbContext();
        _importFolders = context.ImportFolders.ToList();
    }

    private async Task AddAsync()
    {
        await ModalService.Show<ImportFolderModal>(string.Empty, new ModalParameters()
            .Add(nameof(ImportFolderModal.MyImportFolder), new ImportFolder
            {
                Name = string.Empty,
                Path = string.Empty
            })
            .Add(nameof(ImportFolderModal.IsDelete), false)
        ).Result;
        RefreshFolders();
    }

    private async Task EditAsync(ImportFolder importFolder)
    {
        await ModalService.Show<ImportFolderModal>(string.Empty, new ModalParameters()
            .Add(nameof(ImportFolderModal.MyImportFolder), importFolder)
            .Add(nameof(ImportFolderModal.IsDelete), false)).Result;
        RefreshFolders();
    }

    private async Task RemoveAsync(ImportFolder importFolder)
    {
        await ModalService.Show<ImportFolderModal>(string.Empty, new ModalParameters()
            .Add(nameof(ImportFolderModal.MyImportFolder), importFolder)
            .Add(nameof(ImportFolderModal.IsDelete), true)).Result;
        RefreshFolders();
    }
}

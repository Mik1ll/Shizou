using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Shizou.Blazor.Services;
using Shizou.Data.CommandInputArgs;
using Shizou.Data.Database;
using Shizou.Data.Enums;
using Shizou.Data.Models;
using Shizou.Server.Services;

namespace Shizou.Blazor.Components.Pages.Utilities.ManualLinks;

public partial class ManualLinks
{
    private List<LocalFile> _manuallyLinkedFiles = default!;
    private List<LocalFile> _selectedFiles = [];

    [Inject]
    private IShizouContextFactory ContextFactory { get; set; } = default!;

    [Inject]
    private CommandService CommandService { get; set; } = default!;

    [Inject]
    private ManualLinkService ManualLinkService { get; set; } = default!;

    [Inject]
    private ToastService ToastService { get; set; } = default!;

    private static string GetFilePath(LocalFile file) => Path.Combine(file.ImportFolder?.Path ?? "<MISSING IMPORT FLD>", file.PathTail);

    protected override void OnInitialized()
    {
        LoadFiles();
    }

    private void LoadFiles()
    {
        _selectedFiles = [];
        using var context = ContextFactory.CreateDbContext();
        _manuallyLinkedFiles = context.LocalFiles
            .Include(lf => lf.ImportFolder).Where(lf => lf.AniDbFile is AniDbGenericFile).ToList();
    }


    private void ScanFiles(List<LocalFile> localFiles)
    {
        CommandService.DispatchRange(localFiles.Select(lf => new ProcessArgs(lf.Id, IdTypeLocalOrFile.LocalId)));
        ToastService.ShowInfo("Queued Process Commands", $"Queued Process command for {localFiles.Count} files");
    }

    private void HashFiles(List<LocalFile> localFiles)
    {
        var hashableFiles = localFiles.Where(lf => lf.ImportFolder is not null).ToList();
        CommandService.DispatchRange(hashableFiles.Select(lf => new HashArgs(Path.Combine(lf.ImportFolder!.Path, lf.PathTail))));
        if (hashableFiles.Count > 0)
            ToastService.ShowInfo("Queued Hash Commands", $"Queued hash commands for {hashableFiles.Count} files");
        foreach (var lf in localFiles.Where(lf => lf.ImportFolder is null))
            ToastService.ShowWarn("Failed to hash", $"Cannot hash local file id: {lf.Id}, name: \"{Path.GetFileName(lf.PathTail)}\", import folder is null");
    }

    private void UnlinkFiles(List<LocalFile> localFiles)
    {
        foreach (var localFile in localFiles)
            ManualLinkService.UnlinkFile(localFile.Id);
        LoadFiles();
        ToastService.ShowInfo("Files Unlinked",
            "Unlinked the selected files from respective episodes, they should now be found inside the unrecognized files utility");
    }


    private void AvDumpFiles(List<LocalFile> localFiles)
    {
        CommandService.DispatchRange(localFiles.Select(lf => new AvDumpArgs(lf.Id)));
        ToastService.ShowInfo("Queued AVDump Commands", $"Queued AVDump for {localFiles.Count} files");
    }

    private void OnSelectChanged(List<LocalFile> selectedFiles) => _selectedFiles = selectedFiles;
}

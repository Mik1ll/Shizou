using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
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
    }


    private void HashFiles(List<LocalFile> localFiles)
    {
        CommandService.DispatchRange(localFiles.Select(lf =>
            new HashArgs(Path.Combine(lf.ImportFolder?.Path ?? throw new NullReferenceException("Import folder can't be null, need a complete path"),
                lf.PathTail))));
    }

    private void OnSelectChanged(List<LocalFile> selectedFiles) => _selectedFiles = selectedFiles;
}

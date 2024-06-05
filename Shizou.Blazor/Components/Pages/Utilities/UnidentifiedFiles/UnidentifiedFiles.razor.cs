using Blazored.Modal;
using Blazored.Modal.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Shizou.Blazor.Components.Shared;
using Shizou.Blazor.Services;
using Shizou.Data.CommandInputArgs;
using Shizou.Data.Database;
using Shizou.Data.Enums;
using Shizou.Data.Models;
using Shizou.Server.Services;

namespace Shizou.Blazor.Components.Pages.Utilities.UnidentifiedFiles;

public partial class UnidentifiedFiles
{
    private List<LocalFile> _localFiles = [];
    private List<LocalFile> _selectedFiles = [];
    private bool _includeIgnored;

    [Inject]
    private IShizouContextFactory ContextFactory { get; set; } = default!;

    [Inject]
    private CommandService CommandService { get; set; } = default!;

    [Inject]
    private ImportService ImportService { get; set; } = default!;

    [Inject]
    private IModalService ModalService { get; set; } = default!;

    [Inject]
    private ILogger<UnidentifiedFiles> Logger { get; set; } = default!;

    [Inject]
    private ToastService ToastService { get; set; } = default!;


    private static string GetFilePath(LocalFile file) => Path.Combine(file.ImportFolder?.Path ?? "<MISSING IMPORT FLD>", file.PathTail);

    private static string GetEd2KLink(LocalFile localFile) =>
        $"ed2k://|file|{Path.GetFileName(localFile.PathTail).Replace('|', '-')}|{localFile.FileSize}|{localFile.Ed2k}|/";


    protected override void OnInitialized()
    {
        LoadFiles();
    }

    private void LoadFiles()
    {
        _selectedFiles = [];
        using var context = ContextFactory.CreateDbContext();
        _localFiles = context.LocalFiles.Include(lf => lf.ImportFolder)
            .Where(lf => lf.AniDbFile == null && (!lf.Ignored || _includeIgnored)).ToList();
    }

    private void ScanFiles(List<LocalFile> localFiles)
    {
        CommandService.DispatchRange(localFiles.Select(lf => new ProcessArgs(lf.Id, IdTypeLocalOrFile.LocalId)));
        ToastService.ShowInfo("Queued Process Commands", $"Queued Process command for {localFiles.Count} files");
    }

    private void OnSelectChanged(List<LocalFile> values)
    {
        _selectedFiles = values;
    }

    private void HashFiles(List<LocalFile> localFiles)
    {
        var hashableFiles = localFiles.Where(lf => lf.ImportFolder is not null).ToList();
        CommandService.DispatchRange(hashableFiles.Select(lf => new HashArgs(Path.Combine(lf.ImportFolder!.Path, lf.PathTail))));
        if (hashableFiles.Count > 0)
            ToastService.ShowInfo("Queued Hash Commands", $"Queued hash commands for {hashableFiles.Count} files");
        foreach (var lf in localFiles.Where(lf => lf.ImportFolder is null))
        {
            Logger.LogWarning("Cannot hash local file id: {LocalFileId}, name: \"{LocalFileName}\", import folder is null", lf.Id,
                Path.GetFileName(lf.PathTail));
            ToastService.ShowWarn("Failed to hash", $"Cannot hash local file id: {lf.Id}, name: \"{Path.GetFileName(lf.PathTail)}\", import folder is null");
        }
    }

    private void AvDumpFiles(List<LocalFile> localFiles)
    {
        CommandService.DispatchRange(localFiles.Select(lf => new AvDumpArgs(lf.Id)));
        ToastService.ShowInfo("Queued AVDump Commands", $"Queued AVDump for {localFiles.Count} files");
    }

    private void SetIgnored(List<LocalFile> localFiles, bool ignored)
    {
        ImportService.SetIgnored(localFiles.Select(lf => lf.Id), ignored);
        LoadFiles();
    }

    private void ToggleIncludeIgnored()
    {
        _includeIgnored = !_includeIgnored;
        LoadFiles();
    }

    private async Task CreateManualLinkAsync(List<LocalFile> localFiles)
    {
        await ModalService.Show<ManuallyLinkModal>(string.Empty, new ModalParameters()
            .Add(nameof(ManuallyLinkModal.SelectedFiles), localFiles)).Result;
        LoadFiles();
    }
}

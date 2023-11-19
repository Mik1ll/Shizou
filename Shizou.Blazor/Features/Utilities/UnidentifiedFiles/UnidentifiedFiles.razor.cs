using Blazored.Modal;
using Blazored.Modal.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Shizou.Blazor.Features.Components;
using Shizou.Data.Database;
using Shizou.Data.Enums;
using Shizou.Data.Models;
using Shizou.Server.Commands;
using Shizou.Server.Commands.AniDb;
using Shizou.Server.Extensions;
using Shizou.Server.Extensions.Query;
using Shizou.Server.Services;

namespace Shizou.Blazor.Features.Utilities.UnidentifiedFiles;

public partial class UnidentifiedFiles
{
    private List<LocalFile> _localFiles = default!;
    private List<LocalFile> _selectedFiles = new();
    private bool _includeIgnored;

    [Inject]
    private IDbContextFactory<ShizouContext> ContextFactory { get; set; } = default!;

    [Inject]
    private CommandService CommandService { get; set; } = default!;

    [Inject]
    private ImportService ImportService { get; set; } = default!;

    [Inject]
    private IModalService ModalService { get; set; } = default!;


    protected override void OnInitialized()
    {
        LoadFiles();
    }

    private void LoadFiles()
    {
        _selectedFiles = new List<LocalFile>();
        using var context = ContextFactory.CreateDbContext();
        _localFiles = context.LocalFiles.Include(lf => lf.ImportFolder).Unidentified()
            .Where(lf => lf.ImportFolder != null && (!lf.Ignored || _includeIgnored)).AsEnumerable()
            .Where(lf => !lf.IsMissing()).ToList();
    }

    private void ScanFiles(List<LocalFile> localFiles)
    {
        CommandService.DispatchRange(localFiles.Select(lf => new ProcessArgs(lf.Id, IdTypeLocalFile.LocalId)));
    }

    private void OnSelectChanged(List<LocalFile> values)
    {
        _selectedFiles = values;
    }

    private void HashFiles(List<LocalFile> localFiles)
    {
        CommandService.DispatchRange(localFiles.Select(lf =>
            new HashArgs(Path.Combine(lf.ImportFolder?.Path ?? throw new NullReferenceException("Import folder can't be null, need a complete path"),
                lf.PathTail))));
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

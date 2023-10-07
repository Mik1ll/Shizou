using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Shizou.Data.Database;
using Shizou.Data.Enums;
using Shizou.Data.Models;
using Shizou.Server.Commands;
using Shizou.Server.Commands.AniDb;
using Shizou.Server.Extensions.Query;
using Shizou.Server.Services;

namespace Shizou.Blazor.Features.Utilities.UnidentifiedFiles;

public partial class UnidentifiedFiles
{
    private List<LocalFile> _localFiles = default!;
    private List<LocalFile> _selectedFiles = new();
    private bool _includeIgnored;

    [Inject]
    public IDbContextFactory<ShizouContext> ContextFactory { get; set; } = default!;

    [Inject]
    public CommandService CommandService { get; set; } = default!;

    [Inject]
    public ImportService ImportService { get; set; } = default!;


    protected override void OnInitialized()
    {
        LoadFiles();
    }

    private void LoadFiles()
    {
        _selectedFiles = new List<LocalFile>();
        using var context = ContextFactory.CreateDbContext();
        _localFiles = context.LocalFiles.Include(lf => lf.ImportFolder).Unidentified()
            .Where(lf => lf.ImportFolder != null && (!lf.Ignored || _includeIgnored)).ToList();
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
}

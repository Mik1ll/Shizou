using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Shizou.Data.Database;
using Shizou.Data.Enums;
using Shizou.Data.Models;
using Shizou.Server.Commands.AniDb;
using Shizou.Server.Extensions.Query;
using Shizou.Server.Services;

namespace Shizou.Blazor.Features.Utilities.UnidentifiedFiles;

public partial class UnidentifiedFiles
{
    private List<LocalFile> _localFiles = default!;
    private List<LocalFile> _selectedFiles = new();

    [Inject]
    public IDbContextFactory<ShizouContext> ContextFactory { get; set; } = default!;

    [Inject]
    public CommandService CommandService { get; set; } = default!;


    protected override void OnInitialized()
    {
        _selectedFiles = new List<LocalFile>();
        using var context = ContextFactory.CreateDbContext();
        _localFiles = context.LocalFiles.Unrecognized().Include(lf => lf.ImportFolder).ToList();
    }

    private void ScanAll()
    {
        CommandService.DispatchRange(_localFiles.Select(lf => new ProcessArgs(lf.Id, IdTypeLocalFile.LocalId)));
    }

    private void OnSelectChanged(List<LocalFile> values)
    {
        _selectedFiles = values;
    }
}

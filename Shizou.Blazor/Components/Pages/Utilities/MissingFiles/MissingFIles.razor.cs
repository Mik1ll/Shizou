using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Shizou.Data.Database;
using Shizou.Data.Models;
using Shizou.Server.Services;

namespace Shizou.Blazor.Components.Pages.Utilities.MissingFiles;

public partial class MissingFiles
{
    private List<LocalFile> _localFiles = default!;

    [Inject]
    private IShizouContextFactory ContextFactory { get; set; } = default!;

    [Inject]
    private ImportService ImportService { get; set; } = default!;

    protected override void OnInitialized()
    {
        LoadFiles();
    }

    private void LoadFiles()
    {
        using var context = ContextFactory.CreateDbContext();
        _localFiles = context.LocalFiles.Include(lf => lf.ImportFolder).AsEnumerable()
            .Where(lf => lf.ImportFolder == null || !File.Exists(Path.Combine(lf.ImportFolder.Path, lf.PathTail))).ToList();
    }

    private void RemoveMissing()
    {
        ImportService.RemoveMissingFiles();
    }
}

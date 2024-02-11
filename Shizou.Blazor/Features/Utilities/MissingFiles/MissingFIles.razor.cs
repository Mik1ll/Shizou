using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Shizou.Data.Database;
using Shizou.Data.Models;
using Shizou.Server.Extensions.Query;

namespace Shizou.Blazor.Features.Utilities.MissingFiles;

public partial class MissingFiles
{
    private List<LocalFile> _localFiles = default!;
    private List<LocalFile> _selectedFiles = new();

    [Inject]
    private IShizouContextFactory ContextFactory { get; set; } = default!;

    protected override void OnInitialized()
    {
        LoadFiles();
    }


    private void OnSelectChanged(List<LocalFile> values)
    {
        _selectedFiles = values;
    }


    private void LoadFiles()
    {
        _selectedFiles = [];
        using var context = ContextFactory.CreateDbContext();
        _localFiles = context.LocalFiles.Include(lf => lf.ImportFolder).Unidentified()
            .Where(lf => lf.ImportFolder != null).AsEnumerable()
            .Where(lf => !File.Exists(Path.Combine(lf.ImportFolder!.Path, lf.PathTail))).ToList();
    }


    private void RemoveMissing(List<LocalFile> files)
    {
        // TODO
        throw new NotImplementedException();
    }
}

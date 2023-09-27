using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Shizou.Data.Database;
using Shizou.Data.Models;
using Shizou.Server.Extensions.Query;

namespace Shizou.Blazor.Features.Utilities.UnidentifiedFiles;

public partial class UnidentifiedFiles
{
    private List<LocalFile> _localFiles = default!;

    [Inject]
    public IDbContextFactory<ShizouContext> ContextFactory { get; set; } = default!;


    protected override void OnInitialized()
    {
        using var context = ContextFactory.CreateDbContext();
        _localFiles = context.LocalFiles.Include(lf => lf.ImportFolder).GetUnrecognized(context.AniDbFiles).ToList();
    }

    private void OnSelectChanged(List<LocalFile> values)
    {
        ;
    }
}

using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Shizou.Data.Database;
using Shizou.Data.Models;

namespace Shizou.Blazor.Features.Utilities.UnidentifiedFiles;

public partial class UnidentifiedFiles
{
    private List<LocalFile> _localFiles = default!;

    [Inject]
    public IDbContextFactory<ShizouContext> ContextFactory { get; set; } = default!;


    protected override void OnInitialized()
    {
        using var context = ContextFactory.CreateDbContext();
        _localFiles = (from lf in context.LocalFiles.Include(lf => lf.ImportFolder)
            where lf.ManualLinkEpisodeId == null && !context.AniDbFiles.Any(f => f.Ed2k == lf.Ed2k)
            select lf).ToList();
    }

    private void OnSelectChanged(List<LocalFile> values)
    {
        ;
    }
}

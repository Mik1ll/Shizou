using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using Shizou.Data.Database;
using Shizou.Data.Models;
using Shizou.Server.Services;

namespace Shizou.Blazor.Shared;

public partial class ImportFolders
{
    private ImportFolderModal? _importFolderModal;
    private List<ImportFolder> _importFolders = default!;

    [Inject]
    private IJSRuntime JsRuntime { get; set; } = default!;

    [Inject]
    private IDbContextFactory<ShizouContext> ContextFactory { get; set; } = default!;

    [Inject]
    private ImportService ImportService { get; set; } = default!;

    protected override void OnInitialized()
    {
        _importFolders = ContextFactory.CreateDbContext().ImportFolders.ToList();
    }

    private void ImportFolderModalClosed()
    {
        _importFolders = ContextFactory.CreateDbContext().ImportFolders.ToList();
        StateHasChanged();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await JsRuntime.InvokeVoidAsync("loadTooltip", "scan-button");
        await base.OnAfterRenderAsync(firstRender);
    }
}

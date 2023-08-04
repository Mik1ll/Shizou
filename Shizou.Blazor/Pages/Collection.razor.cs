﻿using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Shizou.Data.Database;
using Shizou.Data.Models;
using Shizou.Server.Services;

namespace Shizou.Blazor.Pages;

public partial class Collection
{
    private List<AniDbAnime> _anime = default!;

    [Inject]
    public IDbContextFactory<ShizouContext> ContextFactory { get; set; } = default!;

    [Inject]
    public ImageService ImageService { get; set; } = default!;

    protected override void OnInitialized()
    {
        RefreshAnime();
    }

    private void RefreshAnime()
    {
        using var context = ContextFactory.CreateDbContext();
        _anime = context.AniDbAnimes.ToList();
    }
    
}

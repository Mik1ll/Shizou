﻿using Microsoft.EntityFrameworkCore;

namespace Shizou.Data.Database;

public class ShizouDbContextFactory : IShizouDbContextFactory
{
    private readonly IDbContextFactory<ShizouContext> _contextFactory;

    public ShizouDbContextFactory(IDbContextFactory<ShizouContext> contextFactory) => _contextFactory = contextFactory;
    public IShizouContext CreateDbContext() => _contextFactory.CreateDbContext();
}

using Microsoft.EntityFrameworkCore;

namespace Shizou.Data.Database;

public class ShizouContextFactory : IShizouContextFactory
{
    private readonly IDbContextFactory<ShizouContext> _contextFactory;

    public ShizouContextFactory(IDbContextFactory<ShizouContext> contextFactory) => _contextFactory = contextFactory;
    public IShizouContext CreateDbContext() => _contextFactory.CreateDbContext();
}

using Microsoft.EntityFrameworkCore;

namespace Shizou.Data.Database;

/// <inheritdoc cref="IDbContextFactory{TContext}" />
public interface IShizouContextFactory
{
    /// <inheritdoc cref="IDbContextFactory{TContext}.CreateDbContext" />
    IShizouContext CreateDbContext();
}

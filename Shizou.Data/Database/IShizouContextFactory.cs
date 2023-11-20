namespace Shizou.Data.Database;

public interface IShizouContextFactory
{
    IShizouContext CreateDbContext();
}

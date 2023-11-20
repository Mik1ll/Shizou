namespace Shizou.Data.Database;

public interface IShizouDbContextFactory
{
    IShizouContext CreateDbContext();
}

using System.Data;
using System.Data.SQLite;

namespace Shizou.Database
{
    public interface IDatabase
    {
        void CreateDatabase();
        IDbConnection GetConnection();
        string GetConnectionString();
        string GetDatabasePath();
    }
}
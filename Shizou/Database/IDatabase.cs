using Shizou.Repositories;
using System.Data;
using System.Data.SQLite;

namespace Shizou.Database
{
    public interface IDatabase
    {
        bool DatabaseExists();
        void CreateDatabase();
        IDbConnection GetConnection();
        string GetConnectionString();
        void BackupDatabase();
        void CreateSchema();

    }
}
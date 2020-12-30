using System;
using System.Data;

namespace Shizou.Database
{
    public interface IDatabase : IDisposable
    {
        string ConnectionString { get; }

        bool DatabaseExists();

        void CreateDatabase();

        IDbConnection GetConnection();

        void BackupDatabase();

        void CreateSchema();
    }
}
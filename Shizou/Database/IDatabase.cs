using System;
using System.Data;

namespace Shizou.Database
{
    public interface IDatabase : IDisposable
    {
        string ConnectionString { get; }

        bool DatabaseExists { get; }

        void CreateDatabase();

        IDbConnection Connection { get; }

        void BackupDatabase();

        void CreateSchema();
    }
}
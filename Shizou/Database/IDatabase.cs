using System;
using System.Data;

namespace Shizou.Database
{
    public interface IDatabase : IDisposable
    {
        string ConnectionString { get; }

        bool DatabaseExists { get; }

        IDbConnection Connection { get; }

        void CreateDatabase();

        void BackupDatabase();

        void CreateSchema();
    }
}

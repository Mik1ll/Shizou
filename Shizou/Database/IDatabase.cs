﻿using Shizou.Repositories;
using System;
using System.Data;
using System.Data.SQLite;

namespace Shizou.Database
{
    public interface IDatabase : IDisposable
    {
        bool DatabaseExists();
        void CreateDatabase();
        IDbConnection GetConnection();
        string ConnectionString { get; }
        void BackupDatabase();
        void CreateSchema();

    }
}
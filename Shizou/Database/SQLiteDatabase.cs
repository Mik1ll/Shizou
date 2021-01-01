﻿using System;
using System.Data.SQLite;
using System.IO;
using Microsoft.Extensions.Logging;

namespace Shizou.Database
{
    public sealed class SQLiteDatabase : BaseDatabase
    {
        public SQLiteDatabase(ILogger<SQLiteDatabase> logger) : base(logger, new SQLiteConnection())
        {
        }

        public override string ConnectionString => @$"Data Source={DatabaseFilePath};Version=3;Foreign Keys=True;";

        public string DatabaseFilePath => Path.Combine(Program.ApplicationData, "ShizouDB.sqlite3");

        public override void CreateDatabase()
        {
            if (!DatabaseExists())
            {
                SQLiteConnection.CreateFile(DatabaseFilePath);
                if (!DatabaseExists())
                    throw new IOException($"Failed to create sqlite database: {DatabaseFilePath}");
            }
            var cnn = GetConnection() as SQLiteConnection;
            SQLiteCommand[]? cmds = new[] { new SQLiteCommand("CREATE TABLE IF NOT EXISTS ImportFolders (Id INTEGER PRIMARY KEY, Location TEXT UNIQUE)", cnn) };
            foreach (SQLiteCommand? cmd in cmds)
            {
                cmd.ExecuteNonQuery();
            }
        }

        public override bool DatabaseExists()
        {
            return File.Exists(DatabaseFilePath);
        }

        public override void BackupDatabase()
        {
            throw new NotImplementedException();
        }

        public override void CreateSchema()
        {
            throw new NotImplementedException();
        }
    }
}
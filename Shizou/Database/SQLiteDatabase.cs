using System;
using System.Data.SQLite;
using System.IO;
using Microsoft.Extensions.Logging;

namespace Shizou.Database
{
    // ReSharper disable once InconsistentNaming
    public sealed class SQLiteDatabase : BaseDatabase
    {
        public SQLiteDatabase(ILogger<SQLiteDatabase> logger) : base(logger, new SQLiteConnection())
        {
        }

        public override string ConnectionString => @$"Data Source={DatabaseFilePath};Version=3;Foreign Keys=True;";

        public static string DatabaseFilePath => Path.Combine(Program.ApplicationData, "ShizouDB.sqlite3");

        public override bool DatabaseExists => File.Exists(DatabaseFilePath);

        public override void CreateDatabase()
        {
            if (!DatabaseExists)
            {
                SQLiteConnection.CreateFile(DatabaseFilePath);
                if (!DatabaseExists)
                    throw new IOException($"Failed to create sqlite database: {DatabaseFilePath}");
            }
            var cnn = Connection as SQLiteConnection;
            string[] cmds =
            {
                "CREATE TABLE IF NOT EXISTS ImportFolders (Id INTEGER PRIMARY KEY, Location TEXT UNIQUE)",
                "CREATE TABLE IF NOT EXISTS CommandRequests (Id INTEGER PRIMARY KEY, Type INTEGER NOT NULL, Priority INTEGER NOT NULL, CommandId TEXT NOT NULL UNIQUE, CommandParams TEXT NOT NULL)"
            };
            foreach (var cmd in cmds) new SQLiteCommand(cmd, cnn).ExecuteNonQuery();
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

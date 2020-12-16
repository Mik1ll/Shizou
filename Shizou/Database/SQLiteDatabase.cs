using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Shizou.Database
{
    public class SQLiteDatabase : BaseDatabase
    {

        public SQLiteDatabase(ILogger<SQLiteDatabase> logger) : base(logger)
        {
        }

        public override string GetConnectionString()
        {
            return $@"data source={DatabasePath};version=3;foreign keys=true;";
        }

        public override SQLiteConnection GetConnection()
        {
            return new SQLiteConnection(GetConnectionString());
        }

        public string DatabasePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Shizou");

        public string DatabaseFilePath => Path.Combine(DatabasePath, "ShizouDB.sqlite3");

        public override void CreateDatabase()
        {
            if (!Directory.Exists(DatabasePath))
                Directory.CreateDirectory(DatabasePath);
            if (!DatabaseExists())
            {
                SQLiteConnection.CreateFile(DatabaseFilePath);
                if (!DatabaseExists())
                    throw new IOException($"Failed to create sqlite database: {DatabaseFilePath}");
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

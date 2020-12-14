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

        public SQLiteDatabase(ILogger<SQLiteDatabase> logger, IConfiguration configuration) : base(logger, configuration)
        {
        }

        public override string GetConnectionString()
        {
            return $@"data source={GetDatabasePath()};version=3;foreign keys=true;";
        }

        public override SQLiteConnection GetConnection()
        {
            return new SQLiteConnection(GetConnectionString());
        }

        public override string GetDatabasePath()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Shizou", "ShizouDB.sqlite3");
        }

        public override void CreateDatabase()
        {
            if (!File.Exists(GetDatabasePath()))
            {
                SQLiteConnection.CreateFile(GetDatabasePath());
            }
        }
    }
}

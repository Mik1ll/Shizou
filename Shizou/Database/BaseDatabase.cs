using System.Data;
using Microsoft.Extensions.Logging;

namespace Shizou.Database
{
    public abstract class BaseDatabase : IDatabase
    {
        protected readonly ILogger<BaseDatabase> _logger;
        protected readonly IDbConnection _connection;
        private bool disposedValue;

        public BaseDatabase(ILogger<BaseDatabase> logger, IDbConnection connection)
        {
            _logger = logger;
            _connection = connection;
        }

        public IDbConnection GetConnection()
        {
            _connection.ConnectionString = ConnectionString;
            if (_connection.State == ConnectionState.Closed)
                _connection.Open();
            return _connection;
        }

        public abstract string ConnectionString { get; }

        public abstract bool DatabaseExists();

        public abstract void CreateDatabase();

        public abstract void BackupDatabase();

        public abstract void CreateSchema();

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                    _connection?.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~BaseDatabase()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            System.GC.SuppressFinalize(this);
        }
    }
}
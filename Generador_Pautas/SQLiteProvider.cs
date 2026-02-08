using System;
using System.Data.Common;
using System.Data.SQLite;
using System.Threading.Tasks;

namespace Generador_Pautas
{
    /// <summary>
    /// Proveedor de base de datos SQLite
    /// </summary>
    public class SQLiteProvider : IDatabaseProvider
    {
        private readonly string _connectionString;

        public string ProviderName => "SQLite";
        public string ConnectionString => _connectionString;

        public SQLiteProvider(string connectionString)
        {
            _connectionString = connectionString;
        }

        public DbConnection CreateConnection()
        {
            return new SQLiteConnection(_connectionString);
        }

        public DbCommand CreateCommand(string query, DbConnection connection)
        {
            return new SQLiteCommand(query, (SQLiteConnection)connection);
        }

        public DbParameter CreateParameter(string name, object value)
        {
            return new SQLiteParameter(name, value ?? DBNull.Value);
        }

        public async Task<DbConnection> OpenConnectionAsync()
        {
            var connection = new SQLiteConnection(_connectionString);
            await connection.OpenAsync();
            return connection;
        }

        public string GetDateFunction(string column)
        {
            return $"date({column})";
        }

        public string GetAutoIncrementSyntax()
        {
            return "INTEGER PRIMARY KEY AUTOINCREMENT";
        }

        public string GetParameterPrefix()
        {
            return "@";
        }
    }
}

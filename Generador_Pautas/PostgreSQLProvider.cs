using System;
using System.Data.Common;
using System.Threading.Tasks;
using Npgsql;

namespace Generador_Pautas
{
    /// <summary>
    /// Proveedor de base de datos PostgreSQL
    /// Optimizado para conexiones de red en entornos empresariales
    /// </summary>
    public class PostgreSQLProvider : IDatabaseProvider
    {
        private readonly string _connectionString;

        public string ProviderName => "PostgreSQL";
        public string ConnectionString => _connectionString;

        public PostgreSQLProvider(string connectionString)
        {
            _connectionString = connectionString;
        }

        public PostgreSQLProvider(string host, int port, string database, string username, string password)
        {
            // Connection string con pooling optimizado para rendimiento
            _connectionString = $"Host={host};Port={port};Database={database};Username={username};Password={password};" +
                               "Pooling=true;MinPoolSize=5;MaxPoolSize=100;ConnectionIdleLifetime=300;" +
                               "CommandTimeout=30;Timeout=15";
        }

        public DbConnection CreateConnection()
        {
            return new NpgsqlConnection(_connectionString);
        }

        public DbCommand CreateCommand(string query, DbConnection connection)
        {
            return new NpgsqlCommand(query, (NpgsqlConnection)connection);
        }

        public DbParameter CreateParameter(string name, object value)
        {
            return new NpgsqlParameter(name, value ?? DBNull.Value);
        }

        public async Task<DbConnection> OpenConnectionAsync()
        {
            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            return connection;
        }

        public string GetDateFunction(string column)
        {
            // PostgreSQL usa DATE() o simplemente el casting
            return $"{column}::date";
        }

        public string GetAutoIncrementSyntax()
        {
            return "SERIAL PRIMARY KEY";
        }

        public string GetParameterPrefix()
        {
            return "@";
        }

        /// <summary>
        /// Verifica la conexion al servidor PostgreSQL
        /// </summary>
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                using (var conn = new NpgsqlConnection(_connectionString))
                {
                    await conn.OpenAsync();
                    using (var cmd = new NpgsqlCommand("SELECT 1", conn))
                    {
                        await cmd.ExecuteScalarAsync();
                    }
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Obtiene estadisticas del servidor PostgreSQL
        /// </summary>
        public async Task<string> GetServerStatsAsync()
        {
            try
            {
                using (var conn = new NpgsqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    var sb = new System.Text.StringBuilder();

                    // Version del servidor
                    using (var cmd = new NpgsqlCommand("SELECT version()", conn))
                    {
                        var version = await cmd.ExecuteScalarAsync();
                        sb.AppendLine($"Servidor: {version}");
                    }

                    // Tamano de la base de datos
                    using (var cmd = new NpgsqlCommand("SELECT pg_size_pretty(pg_database_size(current_database()))", conn))
                    {
                        var size = await cmd.ExecuteScalarAsync();
                        sb.AppendLine($"Tamano BD: {size}");
                    }

                    // Conexiones activas
                    using (var cmd = new NpgsqlCommand("SELECT count(*) FROM pg_stat_activity WHERE datname = current_database()", conn))
                    {
                        var connections = await cmd.ExecuteScalarAsync();
                        sb.AppendLine($"Conexiones activas: {connections}");
                    }

                    return sb.ToString();
                }
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }
    }
}

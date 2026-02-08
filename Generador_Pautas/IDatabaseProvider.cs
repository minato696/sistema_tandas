using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;

namespace Generador_Pautas
{
    /// <summary>
    /// Interfaz para abstraccion de proveedores de base de datos.
    /// Permite usar SQLite o PostgreSQL de forma transparente.
    /// </summary>
    public interface IDatabaseProvider
    {
        string ProviderName { get; }
        string ConnectionString { get; }

        DbConnection CreateConnection();
        DbCommand CreateCommand(string query, DbConnection connection);
        DbParameter CreateParameter(string name, object value);

        Task<DbConnection> OpenConnectionAsync();

        // Metodos de utilidad
        string GetDateFunction(string column);
        string GetAutoIncrementSyntax();
        string GetParameterPrefix();
    }

    /// <summary>
    /// Enumeracion de tipos de base de datos soportados
    /// </summary>
    public enum DatabaseType
    {
        SQLite,
        PostgreSQL
    }
}

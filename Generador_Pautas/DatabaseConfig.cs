using System;

namespace Generador_Pautas
{
    /// <summary>
    /// Configuración de la base de datos centralizada
    /// </summary>
    public static class DatabaseConfig
    {
        public const string TableName = "Comerciales";

        /// <summary>
        /// Connection string de PostgreSQL - base de datos en red
        /// </summary>
        public static string ConnectionString => PostgreSQLMigration.ConnectionString;
    }
}

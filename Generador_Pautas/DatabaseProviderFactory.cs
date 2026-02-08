using System;

namespace Generador_Pautas
{
    /// <summary>
    /// Factory para crear el proveedor de base de datos segun la configuracion
    /// </summary>
    public static class DatabaseProviderFactory
    {
        private static IDatabaseProvider _currentProvider;
        private static readonly object _lock = new object();

        /// <summary>
        /// Obtiene el proveedor de base de datos actual
        /// </summary>
        public static IDatabaseProvider CurrentProvider
        {
            get
            {
                if (_currentProvider == null)
                {
                    lock (_lock)
                    {
                        if (_currentProvider == null)
                        {
                            _currentProvider = CreateProvider();
                        }
                    }
                }
                return _currentProvider;
            }
        }

        /// <summary>
        /// Crea el proveedor segun la configuracion en config.ini
        /// </summary>
        private static IDatabaseProvider CreateProvider()
        {
            var dbType = ConfigManager.ObtenerTipoBaseDatos();

            switch (dbType)
            {
                case DatabaseType.PostgreSQL:
                    return CreatePostgreSQLProvider();

                case DatabaseType.SQLite:
                default:
                    return CreateSQLiteProvider();
            }
        }

        private static IDatabaseProvider CreateSQLiteProvider()
        {
            string connectionString = ConfigManager.ObtenerConnectionString();
            return new SQLiteProvider(connectionString);
        }

        private static IDatabaseProvider CreatePostgreSQLProvider()
        {
            string host = ConfigManager.ObtenerValor("PostgreSQL", "Host", "192.168.10.188");
            int port = int.Parse(ConfigManager.ObtenerValor("PostgreSQL", "Port", "9134"));
            string database = ConfigManager.ObtenerValor("PostgreSQL", "Database", "generador_pautas");
            string username = ConfigManager.ObtenerValor("PostgreSQL", "Username", "pautas_user");
            string password = ConfigManager.ObtenerValor("PostgreSQL", "Password", "Pautas2024!");

            return new PostgreSQLProvider(host, port, database, username, password);
        }

        /// <summary>
        /// Fuerza la recarga del proveedor (usar despues de cambiar config)
        /// </summary>
        public static void ReloadProvider()
        {
            lock (_lock)
            {
                _currentProvider = null;
            }
        }

        /// <summary>
        /// Obtiene el tipo de base de datos actual
        /// </summary>
        public static DatabaseType GetCurrentDatabaseType()
        {
            return ConfigManager.ObtenerTipoBaseDatos();
        }

        /// <summary>
        /// Verifica si se esta usando PostgreSQL
        /// </summary>
        public static bool IsPostgreSQL => GetCurrentDatabaseType() == DatabaseType.PostgreSQL;

        /// <summary>
        /// Verifica si se esta usando SQLite
        /// </summary>
        public static bool IsSQLite => GetCurrentDatabaseType() == DatabaseType.SQLite;
    }
}

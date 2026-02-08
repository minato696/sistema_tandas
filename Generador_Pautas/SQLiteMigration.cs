using System;
using System.Windows.Forms;
using Npgsql;

namespace Generador_Pautas
{
    public static class PostgreSQLMigration
    {
        public static string ConnectionString => ConfigManager.ObtenerPostgreSQLConnectionString();

        public static void InicializarBaseDeDatos()
        {
            try
            {
                using (NpgsqlConnection conn = new NpgsqlConnection(ConnectionString))
                {
                    conn.Open();

                    // Las tablas ya fueron creadas en PostgreSQL en el servidor
                    // Solo verificamos que existan y creamos las que falten

                    // Crear tabla Comerciales si no existe
                    CrearTablaComercialesAsync(conn);

                    // Crear tabla ComercialesAsignados si no existe
                    CrearTablaComercialesAsignadosAsync(conn);

                    // Crear tabla Usuarios si no existe
                    CrearTablaUsuarios(conn);

                    // Crear tabla Ciudades si no existe
                    CrearTablaCiudades(conn);

                    // Crear tabla TandasProgramacion si no existe
                    CrearTablaTandasProgramacion(conn);

                    // Crear tabla Radios si no existe
                    CrearTablaRadios(conn);

                    // Crear tabla RadiosCiudades si no existe
                    CrearTablaRadiosCiudades(conn);

                    // Crear indices para mejorar rendimiento
                    CrearIndices(conn);

                    // Migración: Agregar columna Fecha a ComercialesAsignados si no existe
                    AgregarColumnaFechaSiNoExiste(conn);

                    // Crear usuarios por defecto
                    CrearUsuariosPorDefecto(conn);

                    // Insertar ciudades por defecto
                    InsertarCiudadesPorDefecto(conn);
                }

                System.Diagnostics.Debug.WriteLine("Base de datos PostgreSQL inicializada correctamente");
            }
            catch (Exception ex)
            {
                string mensaje = $"Error al conectar con PostgreSQL:\n{ex.Message}\n\n" +
                                $"--- DEBUG INFO ---\n" +
                                $"Host: {ConfigManager.ObtenerValor("PostgreSQL", "Host", "")}\n" +
                                $"Port: {ConfigManager.ObtenerValor("PostgreSQL", "Port", "")}\n" +
                                $"Database: {ConfigManager.ObtenerValor("PostgreSQL", "Database", "")}\n\n" +
                                "Verifique que:\n" +
                                "1. El servidor PostgreSQL este ejecutandose\n" +
                                "2. La IP y puerto sean correctos\n" +
                                "3. El usuario y contrasena sean validos\n" +
                                "4. El firewall permita la conexion";

                MessageBox.Show(mensaje, "Error de Conexion PostgreSQL", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void CrearTablaComercialesAsync(NpgsqlConnection conn)
        {
            string createTableQuery = @"
                CREATE TABLE IF NOT EXISTS Comerciales (
                    Codigo VARCHAR(50) PRIMARY KEY,
                    FilePath TEXT NOT NULL,
                    FechaInicio TIMESTAMP NOT NULL,
                    FechaFinal TIMESTAMP NOT NULL,
                    Ciudad VARCHAR(100) NOT NULL,
                    Radio VARCHAR(100) NOT NULL,
                    Posicion VARCHAR(10) NOT NULL,
                    Estado VARCHAR(20) NOT NULL DEFAULT 'Activo',
                    TipoProgramacion VARCHAR(50) DEFAULT 'Cada 00-30'
                )";

            using (NpgsqlCommand cmd = new NpgsqlCommand(createTableQuery, conn))
            {
                cmd.ExecuteNonQuery();
            }
        }

        private static void CrearTablaComercialesAsignadosAsync(NpgsqlConnection conn)
        {
            string createTableQuery = @"
                CREATE TABLE IF NOT EXISTS ComercialesAsignados (
                    Id SERIAL PRIMARY KEY,
                    Fila INTEGER NOT NULL,
                    Columna INTEGER NOT NULL,
                    ComercialAsignado TEXT NOT NULL,
                    Codigo VARCHAR(50) NOT NULL REFERENCES Comerciales(Codigo) ON DELETE CASCADE
                )";

            using (NpgsqlCommand cmd = new NpgsqlCommand(createTableQuery, conn))
            {
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Agrega la columna Fecha a ComercialesAsignados si no existe (migración)
        /// </summary>
        private static void AgregarColumnaFechaSiNoExiste(NpgsqlConnection conn)
        {
            // Verificar si la columna existe
            string checkQuery = @"
                SELECT COUNT(*) FROM information_schema.columns
                WHERE table_name = 'comercialesasignados' AND column_name = 'fecha'";

            using (var cmd = new NpgsqlCommand(checkQuery, conn))
            {
                long count = (long)cmd.ExecuteScalar();
                if (count == 0)
                {
                    // La columna no existe, agregarla
                    string alterQuery = "ALTER TABLE ComercialesAsignados ADD COLUMN Fecha DATE";
                    using (var alterCmd = new NpgsqlCommand(alterQuery, conn))
                    {
                        alterCmd.ExecuteNonQuery();
                    }

                    // Crear índice para mejorar búsquedas por fecha
                    string indexQuery = "CREATE INDEX IF NOT EXISTS idx_comercialesasignados_fecha ON ComercialesAsignados(Fecha)";
                    using (var indexCmd = new NpgsqlCommand(indexQuery, conn))
                    {
                        indexCmd.ExecuteNonQuery();
                    }

                }
            }
        }

        private static void CrearTablaUsuarios(NpgsqlConnection conn)
        {
            string createTableQuery = @"
                CREATE TABLE IF NOT EXISTS Usuarios (
                    Id SERIAL PRIMARY KEY,
                    Usuario VARCHAR(100) NOT NULL UNIQUE,
                    Contrasena VARCHAR(256) NOT NULL,
                    Rol VARCHAR(50) NOT NULL DEFAULT 'Usuario',
                    NombreCompleto VARCHAR(200),
                    Estado VARCHAR(20) NOT NULL DEFAULT 'Activo',
                    FechaCreacion TIMESTAMP NOT NULL
                )";

            using (NpgsqlCommand cmd = new NpgsqlCommand(createTableQuery, conn))
            {
                cmd.ExecuteNonQuery();
            }
        }

        private static void CrearTablaCiudades(NpgsqlConnection conn)
        {
            string createTableQuery = @"
                CREATE TABLE IF NOT EXISTS Ciudades (
                    Id SERIAL PRIMARY KEY,
                    Nombre VARCHAR(100) NOT NULL UNIQUE,
                    Estado VARCHAR(20) NOT NULL DEFAULT 'Activo'
                )";

            using (NpgsqlCommand cmd = new NpgsqlCommand(createTableQuery, conn))
            {
                cmd.ExecuteNonQuery();
            }
        }

        private static void InsertarCiudadesPorDefecto(NpgsqlConnection conn)
        {
            string[] ciudadesPorDefecto = {
                "ABANCAY", "ANDAHUAYLAS", "AYACUCHO", "BARRANCA", "CAJAMARCA",
                "CAÑETE", "CERRO DE PASCO", "CHACHAPOYAS", "CHICLAYO", "CHIMBOTE",
                "CHINCHA", "CHULUCANAS", "CUSCO", "HUACHO", "HUANCABAMBA",
                "HUANCAVELICA", "HUANUCO", "HUARAL", "HUARAZ", "HUARMEY",
                "ILO", "JAEN", "JAUJA", "JULIACA", "LIMA",
                "LOS ORGANOS", "MOLLENDO", "MOQUEGUA", "MOYOBAMBA", "PACASMAYO",
                "PAITA", "PISCO", "PIURA", "PUCALLPA", "PUNO",
                "PUERTO MALDONADO", "SULLANA", "TACNA", "TALARA", "TARAPOTO",
                "TINGO MARIA", "TRUJILLO", "TUMBES", "VENTANILLA", "YURIMAGUAS"
            };

            foreach (string ciudad in ciudadesPorDefecto)
            {
                string insertQuery = "INSERT INTO Ciudades (Nombre, Estado) VALUES (@Nombre, 'Activo') ON CONFLICT (Nombre) DO NOTHING";
                using (NpgsqlCommand cmd = new NpgsqlCommand(insertQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@Nombre", ciudad);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private static void CrearTablaTandasProgramacion(NpgsqlConnection conn)
        {
            string createTableQuery = @"
                CREATE TABLE IF NOT EXISTS TandasProgramacion (
                    Id SERIAL PRIMARY KEY,
                    Nombre VARCHAR(100) NOT NULL UNIQUE,
                    Horarios TEXT NOT NULL,
                    Estado VARCHAR(20) NOT NULL DEFAULT 'Activo'
                )";

            using (NpgsqlCommand cmd = new NpgsqlCommand(createTableQuery, conn))
            {
                cmd.ExecuteNonQuery();
            }

            // Insertar tandas por defecto para cada radio si no existen
            InsertarTandasPorDefecto(conn);
        }

        private static void InsertarTandasPorDefecto(NpgsqlConnection conn)
        {
            // Horarios para EXITOSA - Tandas 00 y 30 (48 tandas)
            string horariosExitosa = "00:00,00:30,01:00,01:30,02:00,02:30,03:00,03:30," +
                "04:00,04:30,05:00,05:30,06:00,06:30,07:00,07:30," +
                "08:00,08:30,09:00,09:30,10:00,10:30,11:00,11:30," +
                "12:00,12:30,13:00,13:30,14:00,14:30,15:00,15:30," +
                "16:00,16:30,17:00,17:30,18:00,18:30,19:00,19:30," +
                "20:00,20:30,21:00,21:30,22:00,22:30,23:00,23:30";

            // Horarios para KARIBEÑA - Tandas 20 y 50 (48 tandas)
            string horariosKaribena = "00:20,00:50,01:20,01:50,02:20,02:50,03:20,03:50," +
                "04:20,04:50,05:20,05:50,06:20,06:50,07:20,07:50," +
                "08:20,08:50,09:20,09:50,10:20,10:50,11:20,11:50," +
                "12:20,12:50,13:20,13:50,14:20,14:50,15:20,15:50," +
                "16:20,16:50,17:20,17:50,18:20,18:50,19:20,19:50," +
                "20:20,20:50,21:20,21:50,22:20,22:50,23:20,23:50";

            // Horarios para LAKALLE - Tandas 10 y 40 (48 tandas)
            string horariosLakalle = "00:10,00:40,01:10,01:40,02:10,02:40,03:10,03:40," +
                "04:10,04:40,05:10,05:40,06:10,06:40,07:10,07:40," +
                "08:10,08:40,09:10,09:40,10:10,10:40,11:10,11:40," +
                "12:10,12:40,13:10,13:40,14:10,14:40,15:10,15:40," +
                "16:10,16:40,17:10,17:40,18:10,18:40,19:10,19:40," +
                "20:10,20:40,21:10,21:40,22:10,22:40,23:10,23:40";

            // Insertar tandas si no existen
            var tandas = new[]
            {
                ("EXITOSA", horariosExitosa),
                ("KARIBEÑA", horariosKaribena),
                ("LAKALLE", horariosLakalle)
            };

            string insertQuery = "INSERT INTO TandasProgramacion (Nombre, Horarios, Estado) VALUES (@Nombre, @Horarios, 'Activo') ON CONFLICT (Nombre) DO NOTHING";

            foreach (var (nombre, horarios) in tandas)
            {
                using (NpgsqlCommand cmd = new NpgsqlCommand(insertQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@Nombre", nombre);
                    cmd.Parameters.AddWithValue("@Horarios", horarios);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private static void CrearTablaRadios(NpgsqlConnection conn)
        {
            string createTableQuery = @"
                CREATE TABLE IF NOT EXISTS Radios (
                    Id SERIAL PRIMARY KEY,
                    Nombre VARCHAR(100) NOT NULL UNIQUE,
                    Descripcion TEXT,
                    Estado VARCHAR(20) NOT NULL DEFAULT 'Activo'
                )";

            using (NpgsqlCommand cmd = new NpgsqlCommand(createTableQuery, conn))
            {
                cmd.ExecuteNonQuery();
            }

            // Insertar radios por defecto si no existen
            string[] radiosPorDefecto = { "EXITOSA", "KARIBEÑA", "LAKALLE" };
            foreach (string radio in radiosPorDefecto)
            {
                string insertQuery = "INSERT INTO Radios (Nombre, Estado) VALUES (@Nombre, 'Activo') ON CONFLICT (Nombre) DO NOTHING";
                using (NpgsqlCommand cmd = new NpgsqlCommand(insertQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@Nombre", radio);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private static void CrearTablaRadiosCiudades(NpgsqlConnection conn)
        {
            string createTableQuery = @"
                CREATE TABLE IF NOT EXISTS RadiosCiudades (
                    Id SERIAL PRIMARY KEY,
                    RadioId INTEGER NOT NULL REFERENCES Radios(Id) ON DELETE CASCADE,
                    CiudadId INTEGER NOT NULL REFERENCES Ciudades(Id) ON DELETE CASCADE,
                    Estado VARCHAR(20) NOT NULL DEFAULT 'Activo',
                    UNIQUE(RadioId, CiudadId)
                )";

            using (NpgsqlCommand cmd = new NpgsqlCommand(createTableQuery, conn))
            {
                cmd.ExecuteNonQuery();
            }
        }

        private static void CrearUsuariosPorDefecto(NpgsqlConnection conn)
        {
            // Usar UPSERT para crear o actualizar usuarios por defecto
            // Esto asegura que siempre existan los usuarios con las credenciales correctas

            string upsertQuery = @"
                INSERT INTO Usuarios (Usuario, Contrasena, Rol, NombreCompleto, Estado, FechaCreacion)
                VALUES (@Usuario, @Contrasena, @Rol, @NombreCompleto, 'Activo', @Fecha)
                ON CONFLICT (Usuario) DO UPDATE SET
                    Contrasena = EXCLUDED.Contrasena,
                    Rol = EXCLUDED.Rol,
                    NombreCompleto = EXCLUDED.NombreCompleto,
                    Estado = 'Activo'";

            // Crear/actualizar usuario administrador
            using (NpgsqlCommand cmd = new NpgsqlCommand(upsertQuery, conn))
            {
                cmd.Parameters.AddWithValue("@Usuario", "admin");
                cmd.Parameters.AddWithValue("@Contrasena", HashPassword("admin123"));
                cmd.Parameters.AddWithValue("@Rol", "Administrador");
                cmd.Parameters.AddWithValue("@NombreCompleto", "Administrador del Sistema");
                cmd.Parameters.AddWithValue("@Fecha", DateTime.Now);
                cmd.ExecuteNonQuery();
            }

            // Crear/actualizar usuario normal
            using (NpgsqlCommand cmd = new NpgsqlCommand(upsertQuery, conn))
            {
                cmd.Parameters.AddWithValue("@Usuario", "usuario");
                cmd.Parameters.AddWithValue("@Contrasena", HashPassword("usuario123"));
                cmd.Parameters.AddWithValue("@Rol", "Usuario");
                cmd.Parameters.AddWithValue("@NombreCompleto", "Usuario Estandar");
                cmd.Parameters.AddWithValue("@Fecha", DateTime.Now);
                cmd.ExecuteNonQuery();
            }

            System.Diagnostics.Debug.WriteLine("Usuarios por defecto creados/actualizados correctamente");
        }

        private static string HashPassword(string password)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                var builder = new System.Text.StringBuilder();
                foreach (byte b in bytes)
                {
                    builder.Append(b.ToString("x2"));
                }
                return builder.ToString();
            }
        }

        private static void CrearIndices(NpgsqlConnection conn)
        {
            string[] indices = new string[]
            {
                // =============================================
                // INDICES PARA COMERCIALES - Optimizados para consultas frecuentes
                // =============================================
                "CREATE INDEX IF NOT EXISTS idx_comerciales_estado ON Comerciales(Estado)",
                "CREATE INDEX IF NOT EXISTS idx_comerciales_fechas ON Comerciales(FechaInicio, FechaFinal)",
                "CREATE INDEX IF NOT EXISTS idx_comerciales_ciudad_radio ON Comerciales(Ciudad, Radio)",
                "CREATE INDEX IF NOT EXISTS idx_comerciales_posicion ON Comerciales(Posicion)",
                "CREATE INDEX IF NOT EXISTS idx_comerciales_filepath ON Comerciales(FilePath)",
                "CREATE INDEX IF NOT EXISTS idx_comerciales_codigo_desc ON Comerciales(Codigo DESC)",
                "CREATE INDEX IF NOT EXISTS idx_comerciales_radio ON Comerciales(Radio)",
                "CREATE INDEX IF NOT EXISTS idx_comerciales_ciudad ON Comerciales(Ciudad)",
                "CREATE INDEX IF NOT EXISTS idx_comerciales_fechafinal_desc ON Comerciales(FechaFinal DESC)",

                // ÍNDICE COMPUESTO PRINCIPAL - Para consultas agrupadas (el más importante)
                "CREATE INDEX IF NOT EXISTS idx_comerciales_consulta_principal ON Comerciales(Ciudad, Radio, Estado, FechaFinal DESC)",

                // ÍNDICE NORMALIZADO - Para búsquedas sin espacios (UPPER/REPLACE)
                "CREATE INDEX IF NOT EXISTS idx_comerciales_ciudad_norm ON Comerciales(UPPER(REPLACE(Ciudad, ' ', '')))",
                "CREATE INDEX IF NOT EXISTS idx_comerciales_radio_norm ON Comerciales(UPPER(REPLACE(Radio, ' ', '')))",

                // ÍNDICE COMPUESTO PARA VISTA RÁPIDA - Ciudad+Radio+Estado+Posición
                "CREATE INDEX IF NOT EXISTS idx_comerciales_vista_rapida ON Comerciales(Ciudad, Radio, Estado, Posicion)",

                // Indice para acelerar split_part en agrupamiento
                "CREATE INDEX IF NOT EXISTS idx_comerciales_codigo_numerico ON Comerciales((split_part(Codigo, '-', 2)))",

                // =============================================
                // INDICES PARA COMERCIALES ASIGNADOS - CRÍTICO para rendimiento de spots
                // =============================================
                "CREATE INDEX IF NOT EXISTS idx_asignados_codigo ON ComercialesAsignados(Codigo)",
                "CREATE INDEX IF NOT EXISTS idx_asignados_fecha ON ComercialesAsignados(Fecha)",
                "CREATE INDEX IF NOT EXISTS idx_asignados_fila_col ON ComercialesAsignados(Fila, Columna)",

                // ÍNDICE COMPUESTO PRINCIPAL para vista previa de spots (el más importante)
                "CREATE INDEX IF NOT EXISTS idx_asignados_spots_principal ON ComercialesAsignados(Fecha, Fila, Columna, Codigo)",

                // ÍNDICE COVERING para evitar lookup a tabla principal
                "CREATE INDEX IF NOT EXISTS idx_asignados_covering ON ComercialesAsignados(Fecha, Fila, Columna) INCLUDE (ComercialAsignado, Codigo)",

                // ÍNDICE para consultas por día completo
                "CREATE INDEX IF NOT EXISTS idx_asignados_dia ON ComercialesAsignados(Fecha, Codigo)"
            };

            foreach (string indexQuery in indices)
            {
                try
                {
                    using (NpgsqlCommand cmd = new NpgsqlCommand(indexQuery, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Warning al crear indice: {ex.Message}");
                }
            }

            // Actualizar estadisticas
            try
            {
                using (NpgsqlCommand cmd = new NpgsqlCommand("ANALYZE", conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Warning al ejecutar ANALYZE: {ex.Message}");
            }
        }

        public static bool VerificarConexion()
        {
            try
            {
                using (NpgsqlConnection conn = new NpgsqlConnection(ConnectionString))
                {
                    conn.Open();
                    using (NpgsqlCommand cmd = new NpgsqlCommand("SELECT 1", conn))
                    {
                        cmd.ExecuteScalar();
                    }
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        public static string OptimizarBaseDeDatos()
        {
            try
            {
                using (NpgsqlConnection conn = new NpgsqlConnection(ConnectionString))
                {
                    conn.Open();

                    // Actualizar estadisticas
                    using (NpgsqlCommand cmd = new NpgsqlCommand("ANALYZE", conn))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // Reindexar
                    using (NpgsqlCommand cmd = new NpgsqlCommand("REINDEX DATABASE generador_pautas", conn))
                    {
                        cmd.CommandTimeout = 300; // 5 minutos para bases grandes
                        try { cmd.ExecuteNonQuery(); } catch { /* Puede fallar si no hay permisos */ }
                    }

                    // Obtener tamano de la BD
                    string tamano = "";
                    using (NpgsqlCommand cmd = new NpgsqlCommand("SELECT pg_size_pretty(pg_database_size(current_database()))", conn))
                    {
                        tamano = cmd.ExecuteScalar()?.ToString() ?? "N/A";
                    }

                    return $"Base de datos PostgreSQL optimizada correctamente.\nTamano: {tamano}";
                }
            }
            catch (Exception ex)
            {
                return $"Error al optimizar: {ex.Message}";
            }
        }

        public static string ObtenerEstadisticas()
        {
            try
            {
                using (NpgsqlConnection conn = new NpgsqlConnection(ConnectionString))
                {
                    conn.Open();

                    var sb = new System.Text.StringBuilder();

                    // Contar registros en tablas principales
                    using (NpgsqlCommand cmd = new NpgsqlCommand("SELECT COUNT(*) FROM Comerciales", conn))
                    {
                        sb.AppendLine($"Comerciales: {cmd.ExecuteScalar()} registros");
                    }

                    using (NpgsqlCommand cmd = new NpgsqlCommand("SELECT COUNT(*) FROM ComercialesAsignados", conn))
                    {
                        sb.AppendLine($"Asignaciones: {cmd.ExecuteScalar()} registros");
                    }

                    // Obtener tamano de la BD
                    using (NpgsqlCommand cmd = new NpgsqlCommand("SELECT pg_size_pretty(pg_database_size(current_database()))", conn))
                    {
                        sb.AppendLine($"Tamano BD: {cmd.ExecuteScalar()}");
                    }

                    // Version de PostgreSQL
                    using (NpgsqlCommand cmd = new NpgsqlCommand("SELECT version()", conn))
                    {
                        string version = cmd.ExecuteScalar()?.ToString() ?? "N/A";
                        sb.AppendLine($"PostgreSQL: {version.Split(',')[0]}");
                    }

                    // Conexiones activas
                    using (NpgsqlCommand cmd = new NpgsqlCommand("SELECT count(*) FROM pg_stat_activity WHERE datname = current_database()", conn))
                    {
                        sb.AppendLine($"Conexiones activas: {cmd.ExecuteScalar()}");
                    }

                    return sb.ToString();
                }
            }
            catch (Exception ex)
            {
                return $"Error obteniendo estadisticas: {ex.Message}";
            }
        }

        public static string ObtenerInfoConexion()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Tipo: PostgreSQL");
            sb.AppendLine($"Host: {ConfigManager.ObtenerValor("PostgreSQL", "Host", "")}");
            sb.AppendLine($"Port: {ConfigManager.ObtenerValor("PostgreSQL", "Port", "")}");
            sb.AppendLine($"Database: {ConfigManager.ObtenerValor("PostgreSQL", "Database", "")}");
            sb.AppendLine($"Usuario: {ConfigManager.ObtenerValor("PostgreSQL", "Username", "")}");
            sb.AppendLine($"Conexion: {(VerificarConexion() ? "OK" : "Error")}");
            return sb.ToString();
        }
    }

    // Alias para compatibilidad con codigo existente
    public static class SQLiteMigration
    {
        public static string ConnectionString => PostgreSQLMigration.ConnectionString;
        public static void InicializarBaseDeDatos() => PostgreSQLMigration.InicializarBaseDeDatos();
        public static bool VerificarConexion() => PostgreSQLMigration.VerificarConexion();
        public static string OptimizarBaseDeDatos() => PostgreSQLMigration.OptimizarBaseDeDatos();
        public static string ObtenerEstadisticas() => PostgreSQLMigration.ObtenerEstadisticas();
        public static string ObtenerInfoConexion() => PostgreSQLMigration.ObtenerInfoConexion();
    }
}

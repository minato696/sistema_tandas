using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Npgsql;

namespace Generador_Pautas
{
    public static class DataAccess
    {
        public static async Task<DataTable> CargarDatosDesdeBaseDeDatosAsync(string connectionString, string tableName, int limite = 500)
        {
            DataTable dt = new DataTable();
            using (NpgsqlConnection conn = new NpgsqlConnection(connectionString))
            {
                await conn.OpenAsync();
                string query = $"SELECT * FROM {tableName} ORDER BY Codigo DESC LIMIT {limite}";
                using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn))
                {
                    using (NpgsqlDataAdapter adapter = new NpgsqlDataAdapter(cmd))
                    {
                        adapter.Fill(dt);
                    }
                }
            }
            return dt;
        }

        /// <summary>
        /// Carga datos con paginacion (LIMIT y OFFSET)
        /// </summary>
        public static async Task<DataTable> CargarDatosPaginadosAsync(string connectionString, string tableName, int limite, int offset)
        {
            DataTable dt = new DataTable();
            using (NpgsqlConnection conn = new NpgsqlConnection(connectionString))
            {
                await conn.OpenAsync();
                string query = $"SELECT * FROM {tableName} ORDER BY Codigo DESC LIMIT {limite} OFFSET {offset}";
                using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn))
                {
                    using (NpgsqlDataAdapter adapter = new NpgsqlDataAdapter(cmd))
                    {
                        adapter.Fill(dt);
                    }
                }
            }
            return dt;
        }

        public static async Task InsertarDatosEnBaseDeDatosAsync(string connectionString, string tableName, AgregarComercialesData comercialesData, string filePath)
        {
            using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (NpgsqlCommand command = new NpgsqlCommand("", connection))
                {
                    command.CommandText =
                        $"INSERT INTO {tableName} (Codigo, FilePath, FechaInicio, FechaFinal, Ciudad, Radio, Posicion, Estado, TipoProgramacion) " +
                        $"VALUES (@Codigo, @FilePath, @FechaInicio, @FechaFinal, @Ciudad, @Radio, @Posicion, @Estado, @TipoProgramacion)";

                    command.Parameters.AddWithValue("@Codigo", comercialesData.Codigo);
                    command.Parameters.AddWithValue("@FilePath", filePath);
                    command.Parameters.AddWithValue("@FechaInicio", comercialesData.FechaInicio);
                    command.Parameters.AddWithValue("@FechaFinal", comercialesData.FechaFinal);
                    command.Parameters.AddWithValue("@Ciudad", comercialesData.Ciudad);
                    command.Parameters.AddWithValue("@Radio", comercialesData.Radio);
                    command.Parameters.AddWithValue("@Posicion", comercialesData.Posicion);
                    command.Parameters.AddWithValue("@Estado", comercialesData.Estado);
                    command.Parameters.AddWithValue("@TipoProgramacion", comercialesData.TipoProgramacion ?? "Cada 00-30");

                    await command.ExecuteNonQueryAsync();

                    // Invalidar caché después de insertar
                    CacheService.InvalidarComerciales();
                }
            }
        }

        public static async Task EliminarDatosDeBaseDeDatosAsync(string connectionString, string tableName, string codigo)
        {
            using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Primero eliminar las asignaciones de tandas (tabla ComercialesAsignados)
                        using (NpgsqlCommand cmdAsignados = new NpgsqlCommand("", connection, transaction))
                        {
                            cmdAsignados.CommandText = "DELETE FROM ComercialesAsignados WHERE Codigo = @Codigo";
                            cmdAsignados.Parameters.AddWithValue("@Codigo", codigo);
                            await cmdAsignados.ExecuteNonQueryAsync();
                        }

                        // Luego eliminar el comercial principal
                        using (NpgsqlCommand command = new NpgsqlCommand("", connection, transaction))
                        {
                            command.CommandText = $"DELETE FROM {tableName} WHERE Codigo = @Codigo";
                            command.Parameters.AddWithValue("@Codigo", codigo);
                            await command.ExecuteNonQueryAsync();
                        }

                        transaction.Commit();

                        // Invalidar caché después de eliminar
                        CacheService.InvalidarComerciales();
                        CacheService.InvalidarAsignaciones();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public static async Task EliminarMultiplesDatosDeBaseDeDatosAsync(string connectionString, string tableName, string[] codigos)
        {
            if (codigos == null || codigos.Length == 0) return;

            using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // OPTIMIZADO: Usar IN clause en lugar de múltiples queries
                        const int batchSize = 100;
                        for (int i = 0; i < codigos.Length; i += batchSize)
                        {
                            var batch = codigos.Skip(i).Take(batchSize).ToArray();
                            var parameters = new List<string>();

                            // Primero eliminar asignaciones
                            using (NpgsqlCommand cmdAsig = new NpgsqlCommand("", connection, transaction))
                            {
                                for (int j = 0; j < batch.Length; j++)
                                {
                                    string paramName = $"@Codigo{j}";
                                    parameters.Add(paramName);
                                    cmdAsig.Parameters.AddWithValue(paramName, batch[j]);
                                }
                                cmdAsig.CommandText = $"DELETE FROM ComercialesAsignados WHERE Codigo IN ({string.Join(",", parameters)})";
                                await cmdAsig.ExecuteNonQueryAsync();
                            }

                            // Luego eliminar comerciales
                            parameters.Clear();
                            using (NpgsqlCommand command = new NpgsqlCommand("", connection, transaction))
                            {
                                for (int j = 0; j < batch.Length; j++)
                                {
                                    string paramName = $"@Codigo{j}";
                                    parameters.Add(paramName);
                                    command.Parameters.AddWithValue(paramName, batch[j]);
                                }
                                command.CommandText = $"DELETE FROM {tableName} WHERE Codigo IN ({string.Join(",", parameters)})";
                                await command.ExecuteNonQueryAsync();
                            }
                        }

                        transaction.Commit();

                        // Invalidar caché
                        CacheService.InvalidarComerciales();
                        CacheService.InvalidarAsignaciones();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// Elimina TODOS los comerciales que coincidan con el FilePath, Ciudad y Radio especificados.
        /// También elimina las asignaciones relacionadas de ComercialesAsignados.
        /// </summary>
        public static async Task EliminarComercialesPorFilePathAsync(string connectionString, string filePath, string ciudad, string radio)
        {
            using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Primero obtener todos los códigos que coinciden
                        var codigos = new List<string>();
                        using (NpgsqlCommand cmdSelect = new NpgsqlCommand("", connection, transaction))
                        {
                            cmdSelect.CommandText = @"SELECT Codigo FROM Comerciales
                                                      WHERE LOWER(FilePath) = LOWER(@FilePath)
                                                        AND Ciudad = @Ciudad
                                                        AND Radio = @Radio";
                            cmdSelect.Parameters.AddWithValue("@FilePath", filePath);
                            cmdSelect.Parameters.AddWithValue("@Ciudad", ciudad);
                            cmdSelect.Parameters.AddWithValue("@Radio", radio);

                            using (var reader = await cmdSelect.ExecuteReaderAsync())
                            {
                                while (await reader.ReadAsync())
                                {
                                    codigos.Add(reader["Codigo"].ToString());
                                }
                            }
                        }

                        // Eliminar asignaciones de ComercialesAsignados para cada código
                        foreach (string codigo in codigos)
                        {
                            using (NpgsqlCommand cmdAsignados = new NpgsqlCommand("", connection, transaction))
                            {
                                cmdAsignados.CommandText = "DELETE FROM ComercialesAsignados WHERE Codigo = @Codigo";
                                cmdAsignados.Parameters.AddWithValue("@Codigo", codigo);
                                await cmdAsignados.ExecuteNonQueryAsync();
                            }
                        }

                        // Eliminar los comerciales principales
                        using (NpgsqlCommand cmdDelete = new NpgsqlCommand("", connection, transaction))
                        {
                            cmdDelete.CommandText = @"DELETE FROM Comerciales
                                                      WHERE LOWER(FilePath) = LOWER(@FilePath)
                                                        AND Ciudad = @Ciudad
                                                        AND Radio = @Radio";
                            cmdDelete.Parameters.AddWithValue("@FilePath", filePath);
                            cmdDelete.Parameters.AddWithValue("@Ciudad", ciudad);
                            cmdDelete.Parameters.AddWithValue("@Radio", radio);
                            await cmdDelete.ExecuteNonQueryAsync();
                        }

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public static async Task ActualizarEstadoComercialAsync(string connectionString, string tableName, string codigo, string nuevoEstado)
        {
            using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (NpgsqlCommand command = new NpgsqlCommand("", connection))
                {
                    command.CommandText = $"UPDATE {tableName} SET Estado = @Estado WHERE Codigo = @Codigo";
                    command.Parameters.AddWithValue("@Estado", nuevoEstado);
                    command.Parameters.AddWithValue("@Codigo", codigo);
                    await command.ExecuteNonQueryAsync();
                }
            }

            // Invalidar caché después de actualizar
            CacheService.InvalidarComerciales();
        }

        public static async Task ActualizarEstadoMultiplesComercialesAsync(string connectionString, string tableName, string[] codigos, string nuevoEstado)
        {
            if (codigos == null || codigos.Length == 0) return;

            using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
            {
                await connection.OpenAsync();

                // OPTIMIZADO: Usar IN clause en lugar de múltiples queries
                // Dividir en lotes de 100 para evitar queries muy largas
                const int batchSize = 100;
                for (int i = 0; i < codigos.Length; i += batchSize)
                {
                    var batch = codigos.Skip(i).Take(batchSize).ToArray();
                    var parameters = new List<string>();
                    using (NpgsqlCommand command = new NpgsqlCommand("", connection))
                    {
                        for (int j = 0; j < batch.Length; j++)
                        {
                            string paramName = $"@Codigo{j}";
                            parameters.Add(paramName);
                            command.Parameters.AddWithValue(paramName, batch[j]);
                        }

                        command.CommandText = $"UPDATE {tableName} SET Estado = @Estado WHERE Codigo IN ({string.Join(",", parameters)})";
                        command.Parameters.AddWithValue("@Estado", nuevoEstado);
                        await command.ExecuteNonQueryAsync();
                    }
                }

                // Invalidar caché
                CacheService.InvalidarComerciales();
            }
        }

        public static async Task<bool> ExisteCodigoEnBaseDeDatosAsync(string connectionString, string tableName, string codigo)
        {
            using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (NpgsqlCommand command = new NpgsqlCommand($"SELECT EXISTS(SELECT 1 FROM {tableName} WHERE Codigo = @Codigo)", connection))
                {
                    command.Parameters.AddWithValue("@Codigo", codigo);
                    return (bool)await command.ExecuteScalarAsync();
                }
            }
        }

        public static async Task ActualizarDatosEnBaseDeDatosAsync(string connectionString, string tableName, AgregarComercialesData comercialesData, string filePath)
        {
            using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (NpgsqlCommand command = new NpgsqlCommand("", connection))
                {
                    command.CommandText = $"UPDATE {tableName} SET FilePath = @FilePath, FechaInicio = @FechaInicio, FechaFinal = @FechaFinal, " +
                                         $"Ciudad = @Ciudad, Radio = @Radio, Posicion = @Posicion, Estado = @Estado, TipoProgramacion = @TipoProgramacion WHERE Codigo = @Codigo";

                    command.Parameters.AddWithValue("@FilePath", filePath);
                    command.Parameters.AddWithValue("@FechaInicio", comercialesData.FechaInicio);
                    command.Parameters.AddWithValue("@FechaFinal", comercialesData.FechaFinal);
                    command.Parameters.AddWithValue("@Ciudad", comercialesData.Ciudad);
                    command.Parameters.AddWithValue("@Radio", comercialesData.Radio);
                    command.Parameters.AddWithValue("@Posicion", comercialesData.Posicion);
                    command.Parameters.AddWithValue("@Estado", comercialesData.Estado);
                    command.Parameters.AddWithValue("@TipoProgramacion", comercialesData.TipoProgramacion ?? "Cada 00-30");
                    command.Parameters.AddWithValue("@Codigo", comercialesData.Codigo);

                    await command.ExecuteNonQueryAsync();
                }
            }

            // Invalidar caché después de actualizar
            CacheService.InvalidarComerciales();
        }

        public static async Task<DataTable> CargarDatosFiltradosPorEstadoAsync(string connectionString, string tableName, string estadoFiltro)
        {
            DataTable dt = new DataTable();
            using (NpgsqlConnection conn = new NpgsqlConnection(connectionString))
            {
                await conn.OpenAsync();
                string query;

                if (estadoFiltro == "Todos")
                {
                    query = $"SELECT * FROM {tableName}";
                }
                else
                {
                    query = $"SELECT * FROM {tableName} WHERE Estado = @Estado";
                }

                using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn))
                {
                    if (estadoFiltro != "Todos")
                    {
                        cmd.Parameters.AddWithValue("@Estado", estadoFiltro);
                    }

                    using (NpgsqlDataAdapter adapter = new NpgsqlDataAdapter(cmd))
                    {
                        adapter.Fill(dt);
                    }
                }
            }
            return dt;
        }

        public static async Task<int> ObtenerUltimoNumeroCodigoAsync(string connectionString, string tableName)
        {
            using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (NpgsqlCommand command = new NpgsqlCommand("", connection))
                {
                    // Obtener el máximo número de código considerando varios formatos:
                    // 1. Solo números: "957", "42263"
                    // 2. Formato CU-: "CU-957-ABA-EXI"
                    // 3. Formato ACC-: "ACC-957-ABA-EXI-0015"
                    command.CommandText = $@"
                        SELECT COALESCE(MAX(numero), 0) as MaxNumero
                        FROM (
                            SELECT
                                CASE
                                    -- Solo números
                                    WHEN Codigo ~ '^\d+$' THEN CAST(Codigo AS INTEGER)
                                    -- Formato CU-numero-... o ACC-numero-...
                                    WHEN Codigo ~ '^[A-Z]+-\d+-' THEN
                                        CAST(NULLIF(SPLIT_PART(Codigo, '-', 2), '') AS INTEGER)
                                    ELSE 0
                                END as numero
                            FROM {tableName}
                            WHERE Codigo IS NOT NULL
                        ) sub
                        WHERE numero IS NOT NULL";

                    var result = await command.ExecuteScalarAsync();
                    if (result != null && result != DBNull.Value)
                    {
                        return Convert.ToInt32(result);
                    }
                }
            }
            return 0;
        }

        public static async Task LimpiarTodasLasBaseDeDatosAsync(string connectionString)
        {
            using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
            {
                await connection.OpenAsync();

                using (NpgsqlCommand command = new NpgsqlCommand("DELETE FROM Comerciales", connection))
                {
                    await command.ExecuteNonQueryAsync();
                }

                using (NpgsqlCommand command = new NpgsqlCommand("DELETE FROM ComercialesAsignados", connection))
                {
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        /// <summary>
        /// Actualiza un comercial existente en la base de datos
        /// </summary>
        public static async Task ActualizarComercialAsync(string connectionString, string tableName, AgregarComercialesData data)
        {
            using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
            {
                await connection.OpenAsync();
                string query = $@"
                    UPDATE {tableName} SET
                        FechaInicio = @FechaInicio,
                        FechaFinal = @FechaFinal,
                        Ciudad = @Ciudad,
                        Radio = @Radio,
                        Posicion = @Posicion,
                        Estado = @Estado,
                        TipoProgramacion = @TipoProgramacion
                    WHERE Codigo = @Codigo";

                using (NpgsqlCommand cmd = new NpgsqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@Codigo", data.Codigo);
                    cmd.Parameters.AddWithValue("@FechaInicio", data.FechaInicio);
                    cmd.Parameters.AddWithValue("@FechaFinal", data.FechaFinal);
                    cmd.Parameters.AddWithValue("@Ciudad", data.Ciudad ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@Radio", data.Radio ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@Posicion", data.Posicion ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@Estado", data.Estado ?? "Activo");
                    cmd.Parameters.AddWithValue("@TipoProgramacion", data.TipoProgramacion ?? (object)DBNull.Value);

                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        /// <summary>
        /// Elimina todas las asignaciones de un comercial por su código
        /// </summary>
        public static async Task EliminarAsignacionesPorCodigoAsync(string connectionString, string codigo)
        {
            using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
            {
                await connection.OpenAsync();
                string query = "DELETE FROM ComercialesAsignados WHERE Codigo = @Codigo";

                using (NpgsqlCommand cmd = new NpgsqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@Codigo", codigo);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
            System.Diagnostics.Debug.WriteLine($"[DataAccess] Eliminadas asignaciones para código: {codigo}");
        }

        /// <summary>
        /// Obtiene las tandas asignadas para un comercial específico - OPTIMIZADO
        /// </summary>
        public static async Task<List<string>> ObtenerTandasAsignadasAsync(string connectionString, string codigo)
        {
            var tandas = new List<string>();
            using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
            {
                await connection.OpenAsync();

                // Extraer la parte numérica del código si tiene formato XXX-numero-...
                string codigoNumerico = "";
                if (codigo.Contains("-"))
                {
                    var partes = codigo.Split('-');
                    if (partes.Length >= 2 && int.TryParse(partes[1], out _))
                    {
                        codigoNumerico = partes[1];
                    }
                }
                else if (int.TryParse(codigo, out _))
                {
                    codigoNumerico = codigo;
                }

                // OPTIMIZADO: Query única con UNION ALL para evitar múltiples roundtrips
                string query = @"
                    SELECT DISTINCT Fila FROM (
                        SELECT Fila FROM ComercialesAsignados WHERE Codigo = @Codigo
                        UNION ALL
                        SELECT Fila FROM ComercialesAsignados WHERE @CodigoNumerico != '' AND Codigo = @CodigoNumerico
                        UNION ALL
                        SELECT Fila FROM ComercialesAsignados WHERE @CodigoNumerico != '' AND split_part(Codigo, '-', 2) = @CodigoNumerico
                    ) sub
                    ORDER BY Fila";

                using (NpgsqlCommand cmd = new NpgsqlCommand(query, connection))
                {
                    cmd.CommandTimeout = 15;
                    cmd.Parameters.AddWithValue("@Codigo", codigo);
                    cmd.Parameters.AddWithValue("@CodigoNumerico", codigoNumerico);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            tandas.Add(reader.GetInt32(0).ToString());
                        }
                    }
                }
            }
            return tandas;
        }

        /// <summary>
        /// Busca comerciales con filtros combinados (optimizado para grandes volumenes)
        /// </summary>
        public static async Task<DataTable> BuscarComercialesAsync(
            string connectionString,
            string tableName,
            string textoBusqueda = null,
            string columnaBusqueda = null,
            string estadoFiltro = "Todos",
            string ciudadFiltro = null,
            string radioFiltro = null,
            int limite = 1000,
            DateTime? fechaInicio = null,
            DateTime? fechaFinal = null)
        {
            DataTable dt = new DataTable();
            using (NpgsqlConnection conn = new NpgsqlConnection(connectionString))
            {
                await conn.OpenAsync();

                var condiciones = new System.Collections.Generic.List<string>();
                var cmd = new NpgsqlCommand();
                cmd.Connection = conn;

                // Filtro por estado
                if (!string.IsNullOrEmpty(estadoFiltro) && estadoFiltro != "Todos")
                {
                    condiciones.Add("Estado = @Estado");
                    cmd.Parameters.AddWithValue("@Estado", estadoFiltro);
                }

                // Filtro por ciudad (normalizado para manejar variaciones)
                if (!string.IsNullOrEmpty(ciudadFiltro))
                {
                    string ciudadNormalizada = ciudadFiltro.Replace(" ", "").ToUpper();
                    condiciones.Add("(UPPER(REPLACE(Ciudad, ' ', '')) = @CiudadNormalizada OR Ciudad = @Ciudad)");
                    cmd.Parameters.AddWithValue("@CiudadNormalizada", ciudadNormalizada);
                    cmd.Parameters.AddWithValue("@Ciudad", ciudadFiltro);
                }

                // Filtro por radio (normalizado para manejar variaciones como "LA KALLE" vs "LAKALLE")
                if (!string.IsNullOrEmpty(radioFiltro))
                {
                    string radioNormalizado = radioFiltro.Replace(" ", "").ToUpper();
                    condiciones.Add("(UPPER(REPLACE(Radio, ' ', '')) = @RadioNormalizado OR Radio = @Radio)");
                    cmd.Parameters.AddWithValue("@RadioNormalizado", radioNormalizado);
                    cmd.Parameters.AddWithValue("@Radio", radioFiltro);
                }

                // Filtro por rango de fechas (comerciales vigentes en el rango)
                if (fechaInicio.HasValue && fechaFinal.HasValue)
                {
                    // Buscar comerciales cuyo periodo de vigencia se solape con el rango seleccionado
                    condiciones.Add("FechaInicio <= @FechaFinal AND FechaFinal >= @FechaInicio");
                    cmd.Parameters.AddWithValue("@FechaInicio", fechaInicio.Value);
                    cmd.Parameters.AddWithValue("@FechaFinal", fechaFinal.Value);
                }

                // Filtro por texto de busqueda
                if (!string.IsNullOrEmpty(textoBusqueda) && !string.IsNullOrEmpty(columnaBusqueda))
                {
                    string columnaSQL;
                    switch (columnaBusqueda)
                    {
                        case "Código": columnaSQL = "Codigo"; break;
                        case "Spot": columnaSQL = "FilePath"; break;
                        case "Ciudad": columnaSQL = "Ciudad"; break;
                        case "Radio": columnaSQL = "Radio"; break;
                        case "Estado": columnaSQL = "Estado"; break;
                        default: columnaSQL = "FilePath"; break;
                    }
                    condiciones.Add($"{columnaSQL} ILIKE @Busqueda");
                    cmd.Parameters.AddWithValue("@Busqueda", $"%{textoBusqueda}%");
                }

                // Construir query
                string whereClause = condiciones.Count > 0 ? "WHERE " + string.Join(" AND ", condiciones) : "";
                cmd.CommandText = $"SELECT * FROM {tableName} {whereClause} ORDER BY Codigo DESC LIMIT {limite}";

                using (NpgsqlDataAdapter adapter = new NpgsqlDataAdapter(cmd))
                {
                    adapter.Fill(dt);
                }
            }
            return dt;
        }

        /// <summary>
        /// Cuenta el total de registros con los filtros aplicados
        /// </summary>
        /// <summary>
        /// Desactiva automáticamente todos los comerciales cuya FechaFinal haya pasado.
        /// Retorna la cantidad de comerciales desactivados.
        /// </summary>
        public static async Task<int> DesactivarComercialesVencidosAsync(string connectionString, string tableName)
        {
            using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (NpgsqlCommand command = new NpgsqlCommand("", connection))
                {
                    // Actualizar a 'Inactivo' todos los comerciales activos cuya FechaFinal sea anterior a hoy
                    command.CommandText = $@"UPDATE {tableName}
                                             SET Estado = 'Inactivo'
                                             WHERE Estado = 'Activo'
                                             AND FechaFinal < @FechaHoy";
                    command.Parameters.AddWithValue("@FechaHoy", DateTime.Today);
                    return await command.ExecuteNonQueryAsync();
                }
            }
        }

        public static async Task<int> ContarComercialesAsync(
            string connectionString,
            string tableName,
            string textoBusqueda = null,
            string columnaBusqueda = null,
            string estadoFiltro = "Todos",
            string ciudadFiltro = null,
            string radioFiltro = null,
            DateTime? fechaInicio = null,
            DateTime? fechaFinal = null)
        {
            using (NpgsqlConnection conn = new NpgsqlConnection(connectionString))
            {
                await conn.OpenAsync();

                var condiciones = new System.Collections.Generic.List<string>();
                var cmd = new NpgsqlCommand();
                cmd.Connection = conn;

                if (!string.IsNullOrEmpty(estadoFiltro) && estadoFiltro != "Todos")
                {
                    condiciones.Add("Estado = @Estado");
                    cmd.Parameters.AddWithValue("@Estado", estadoFiltro);
                }

                // Filtro por ciudad (normalizado para manejar variaciones)
                if (!string.IsNullOrEmpty(ciudadFiltro))
                {
                    string ciudadNormalizada = ciudadFiltro.Replace(" ", "").ToUpper();
                    condiciones.Add("(UPPER(REPLACE(Ciudad, ' ', '')) = @CiudadNormalizada OR Ciudad = @Ciudad)");
                    cmd.Parameters.AddWithValue("@CiudadNormalizada", ciudadNormalizada);
                    cmd.Parameters.AddWithValue("@Ciudad", ciudadFiltro);
                }

                // Filtro por radio (normalizado para manejar variaciones como "LA KALLE" vs "LAKALLE")
                if (!string.IsNullOrEmpty(radioFiltro))
                {
                    string radioNormalizado = radioFiltro.Replace(" ", "").ToUpper();
                    condiciones.Add("(UPPER(REPLACE(Radio, ' ', '')) = @RadioNormalizado OR Radio = @Radio)");
                    cmd.Parameters.AddWithValue("@RadioNormalizado", radioNormalizado);
                    cmd.Parameters.AddWithValue("@Radio", radioFiltro);
                }

                if (!string.IsNullOrEmpty(textoBusqueda) && !string.IsNullOrEmpty(columnaBusqueda))
                {
                    string columnaSQL;
                    switch (columnaBusqueda)
                    {
                        case "Código": columnaSQL = "Codigo"; break;
                        case "Spot": columnaSQL = "FilePath"; break;
                        case "Ciudad": columnaSQL = "Ciudad"; break;
                        case "Radio": columnaSQL = "Radio"; break;
                        case "Estado": columnaSQL = "Estado"; break;
                        default: columnaSQL = "FilePath"; break;
                    }
                    condiciones.Add($"{columnaSQL} ILIKE @Busqueda");
                    cmd.Parameters.AddWithValue("@Busqueda", $"%{textoBusqueda}%");
                }

                // Filtro por rango de fechas
                if (fechaInicio.HasValue && fechaFinal.HasValue)
                {
                    condiciones.Add("FechaInicio <= @FechaFinal AND FechaFinal >= @FechaInicio");
                    cmd.Parameters.AddWithValue("@FechaInicio", fechaInicio.Value);
                    cmd.Parameters.AddWithValue("@FechaFinal", fechaFinal.Value);
                }

                string whereClause = condiciones.Count > 0 ? "WHERE " + string.Join(" AND ", condiciones) : "";
                cmd.CommandText = $"SELECT COUNT(*) FROM {tableName} {whereClause}";

                return Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }
        }

        /// <summary>
        /// Carga comerciales agrupados por codigo numerico, ciudad y radio
        /// </summary>
        public static async Task<DataTable> CargarComercialesAgrupadosAsync(
            string connectionString,
            string tableName,
            int limite = 500,
            int offset = 0,
            string textoBusqueda = null,
            string estadoFiltro = "Todos",
            string ciudadFiltro = null,
            string radioFiltro = null)
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("FilePath", typeof(string));
            dt.Columns.Add("CodigoNumerico", typeof(string));
            dt.Columns.Add("NombreArchivo", typeof(string));
            dt.Columns.Add("TotalRegistros", typeof(int));
            dt.Columns.Add("Ciudad", typeof(string));
            dt.Columns.Add("Radio", typeof(string));
            dt.Columns.Add("FechaMinima", typeof(DateTime));
            dt.Columns.Add("FechaMaxima", typeof(DateTime));
            dt.Columns.Add("EstadoGeneral", typeof(string));
            dt.Columns.Add("Posicion", typeof(string));

            using (NpgsqlConnection conn = new NpgsqlConnection(connectionString))
            {
                await conn.OpenAsync();

                var condiciones = new System.Collections.Generic.List<string>();
                var cmd = new NpgsqlCommand();
                cmd.Connection = conn;

                // Filtro por estado
                if (!string.IsNullOrEmpty(estadoFiltro) && estadoFiltro != "Todos")
                {
                    condiciones.Add("Estado = @Estado");
                    cmd.Parameters.AddWithValue("@Estado", estadoFiltro);
                }

                // Filtro por texto de búsqueda
                if (!string.IsNullOrEmpty(textoBusqueda))
                {
                    condiciones.Add("FilePath ILIKE @Busqueda");
                    cmd.Parameters.AddWithValue("@Busqueda", $"%{textoBusqueda}%");
                }

                // Filtro por ciudad
                if (!string.IsNullOrEmpty(ciudadFiltro))
                {
                    string ciudadNormalizada = ciudadFiltro.Replace(" ", "").ToUpper();
                    condiciones.Add("(UPPER(REPLACE(Ciudad, ' ', '')) = @CiudadNormalizada OR Ciudad = @Ciudad)");
                    cmd.Parameters.AddWithValue("@CiudadNormalizada", ciudadNormalizada);
                    cmd.Parameters.AddWithValue("@Ciudad", ciudadFiltro);
                }

                // Filtro por radio
                if (!string.IsNullOrEmpty(radioFiltro))
                {
                    string radioNormalizado = radioFiltro.Replace(" ", "").ToUpper();
                    condiciones.Add("(UPPER(REPLACE(Radio, ' ', '')) = @RadioNormalizado OR Radio = @Radio)");
                    cmd.Parameters.AddWithValue("@RadioNormalizado", radioNormalizado);
                    cmd.Parameters.AddWithValue("@Radio", radioFiltro);
                }

                string whereClause = condiciones.Count > 0 ? "WHERE " + string.Join(" AND ", condiciones) : "";

                // Query con GROUP BY - usar LOWER para normalizar rutas (Windows es case-insensitive)
                string query = $@"
                    SELECT
                        MAX(FilePath) as FilePath,
                        COALESCE(NULLIF(split_part(Codigo, '-', 2), ''), Codigo) as CodigoNumerico,
                        Ciudad,
                        Radio,
                        COUNT(*) as TotalRegistros,
                        MIN(FechaInicio) as FechaMinima,
                        MAX(FechaFinal) as FechaMaxima,
                        MAX(Posicion) as Posicion,
                        MAX(Estado) as EstadoGeneral
                    FROM {tableName}
                    {whereClause}
                    GROUP BY LOWER(FilePath), COALESCE(NULLIF(split_part(Codigo, '-', 2), ''), Codigo), Ciudad, Radio
                    ORDER BY MAX(FechaFinal) DESC
                    LIMIT {limite} OFFSET {offset}";

                cmd.CommandText = query;

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        string filePath = reader["FilePath"].ToString();
                        string nombreArchivo = System.IO.Path.GetFileName(filePath);

                        dt.Rows.Add(
                            filePath,
                            reader["CodigoNumerico"].ToString(),
                            nombreArchivo,
                            Convert.ToInt32(reader["TotalRegistros"]),
                            reader["Ciudad"].ToString(),
                            reader["Radio"].ToString(),
                            reader["FechaMinima"],
                            reader["FechaMaxima"],
                            reader["EstadoGeneral"].ToString(),
                            reader["Posicion"]?.ToString() ?? ""
                        );
                    }
                }
            }
            return dt;
        }

        /// <summary>
        /// Cuenta el total de codigos numericos unicos (agrupados por codigo numerico + Ciudad + Radio) con filtros
        /// </summary>
        public static async Task<int> ContarArchivosUnicosAsync(
            string connectionString,
            string tableName,
            string textoBusqueda = null,
            string estadoFiltro = "Todos",
            string ciudadFiltro = null,
            string radioFiltro = null)
        {
            using (NpgsqlConnection conn = new NpgsqlConnection(connectionString))
            {
                await conn.OpenAsync();

                var condiciones = new System.Collections.Generic.List<string>();
                var cmd = new NpgsqlCommand();
                cmd.Connection = conn;

                if (!string.IsNullOrEmpty(estadoFiltro) && estadoFiltro != "Todos")
                {
                    condiciones.Add("Estado = @Estado");
                    cmd.Parameters.AddWithValue("@Estado", estadoFiltro);
                }

                if (!string.IsNullOrEmpty(textoBusqueda))
                {
                    condiciones.Add("FilePath ILIKE @Busqueda");
                    cmd.Parameters.AddWithValue("@Busqueda", $"%{textoBusqueda}%");
                }

                // Filtro por ciudad (normalizado para manejar variaciones)
                if (!string.IsNullOrEmpty(ciudadFiltro))
                {
                    string ciudadNormalizada = ciudadFiltro.Replace(" ", "").ToUpper();
                    condiciones.Add("(UPPER(REPLACE(Ciudad, ' ', '')) = @CiudadNormalizada OR Ciudad = @Ciudad)");
                    cmd.Parameters.AddWithValue("@CiudadNormalizada", ciudadNormalizada);
                    cmd.Parameters.AddWithValue("@Ciudad", ciudadFiltro);
                }

                // Filtro por radio (normalizado para manejar variaciones como "LA KALLE" vs "LAKALLE")
                if (!string.IsNullOrEmpty(radioFiltro))
                {
                    string radioNormalizado = radioFiltro.Replace(" ", "").ToUpper();
                    condiciones.Add("(UPPER(REPLACE(Radio, ' ', '')) = @RadioNormalizado OR Radio = @Radio)");
                    cmd.Parameters.AddWithValue("@RadioNormalizado", radioNormalizado);
                    cmd.Parameters.AddWithValue("@Radio", radioFiltro);
                }

                string whereClause = condiciones.Count > 0 ? "WHERE " + string.Join(" AND ", condiciones) : "";
                // Contar grupos únicos de codigo numerico + Ciudad + Radio
                cmd.CommandText = $"SELECT COUNT(*) FROM (SELECT DISTINCT split_part(Codigo, '-', 2) as CodigoNumerico, Ciudad, Radio FROM {tableName} {whereClause}) as grupos";

                return Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }
        }

        /// <summary>
        /// Obtiene todos los registros de un archivo específico (todos los pauteos de un MP3)
        /// </summary>
        public static async Task<DataTable> ObtenerRegistrosPorFilePathAsync(
            string connectionString,
            string tableName,
            string filePath)
        {
            DataTable dt = new DataTable();
            using (NpgsqlConnection conn = new NpgsqlConnection(connectionString))
            {
                await conn.OpenAsync();

                string query = $@"
                    SELECT Codigo, FilePath, FechaInicio, FechaFinal, Ciudad, Radio, Posicion, Estado, TipoProgramacion
                    FROM {tableName}
                    WHERE LOWER(FilePath) = LOWER(@FilePath)
                    ORDER BY Ciudad, Radio, FechaInicio";

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@FilePath", filePath);
                    using (NpgsqlDataAdapter adapter = new NpgsqlDataAdapter(cmd))
                    {
                        adapter.Fill(dt);
                    }
                }
            }
            return dt;
        }

        /// <summary>
        /// Obtiene las pautas (ComercialesAsignados) de todos los registros de un FilePath
        /// Opcionalmente filtra por ciudad y radio
        /// </summary>
        public static async Task<DataTable> ObtenerPautasPorFilePathAsync(
            string connectionString,
            string filePath,
            string ciudadFiltro = null,
            string radioFiltro = null)
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("Codigo", typeof(string));
            dt.Columns.Add("Ciudad", typeof(string));
            dt.Columns.Add("Radio", typeof(string));
            dt.Columns.Add("FechaInicio", typeof(DateTime));
            dt.Columns.Add("FechaFinal", typeof(DateTime));
            dt.Columns.Add("TotalPautas", typeof(int));
            dt.Columns.Add("Estado", typeof(string));
            dt.Columns.Add("Posicion", typeof(string));

            using (NpgsqlConnection conn = new NpgsqlConnection(connectionString))
            {
                await conn.OpenAsync();

                // Construir condiciones WHERE
                var condiciones = new System.Collections.Generic.List<string>();
                condiciones.Add("c.FilePath = @FilePath");

                // Filtro por ciudad (normalizado para manejar variaciones)
                if (!string.IsNullOrEmpty(ciudadFiltro))
                {
                    condiciones.Add("(UPPER(REPLACE(c.Ciudad, ' ', '')) = @CiudadNormalizada OR c.Ciudad = @Ciudad)");
                }

                // Filtro por radio (normalizado para manejar variaciones como "LA KALLE" vs "LAKALLE")
                if (!string.IsNullOrEmpty(radioFiltro))
                {
                    condiciones.Add("(UPPER(REPLACE(c.Radio, ' ', '')) = @RadioNormalizado OR c.Radio = @Radio)");
                }

                string whereClause = "WHERE " + string.Join(" AND ", condiciones);

                // Query que obtiene los comerciales con el conteo de pautas
                string query = $@"
                    SELECT
                        c.Codigo, c.Ciudad, c.Radio, c.FechaInicio, c.FechaFinal,
                        c.Estado, c.Posicion,
                        COUNT(ca.Codigo) as TotalPautas
                    FROM Comerciales c
                    LEFT JOIN ComercialesAsignados ca ON c.Codigo = ca.Codigo
                    {whereClause}
                    GROUP BY c.Codigo, c.Ciudad, c.Radio, c.FechaInicio, c.FechaFinal, c.Estado, c.Posicion
                    ORDER BY c.Ciudad, c.Radio, c.FechaInicio";

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@FilePath", filePath);

                    if (!string.IsNullOrEmpty(ciudadFiltro))
                    {
                        string ciudadNormalizada = ciudadFiltro.Replace(" ", "").ToUpper();
                        cmd.Parameters.AddWithValue("@CiudadNormalizada", ciudadNormalizada);
                        cmd.Parameters.AddWithValue("@Ciudad", ciudadFiltro);
                    }

                    if (!string.IsNullOrEmpty(radioFiltro))
                    {
                        string radioNormalizado = radioFiltro.Replace(" ", "").ToUpper();
                        cmd.Parameters.AddWithValue("@RadioNormalizado", radioNormalizado);
                        cmd.Parameters.AddWithValue("@Radio", radioFiltro);
                    }

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            dt.Rows.Add(
                                reader["Codigo"].ToString(),
                                reader["Ciudad"].ToString(),
                                reader["Radio"].ToString(),
                                reader["FechaInicio"],
                                reader["FechaFinal"],
                                Convert.ToInt32(reader["TotalPautas"]),
                                reader["Estado"].ToString(),
                                reader["Posicion"].ToString()
                            );
                        }
                    }
                }
            }
            return dt;
        }

        /// <summary>
        /// Obtiene las combinaciones de FECHA + HORA para un archivo, como el sistema antiguo.
        /// Genera las fechas basandose en el rango FechaInicio-FechaFinal y la hora del codigo del comercial.
        /// El codigo tiene formato: ACC-42262-ABA-EXI-0000 donde 0000 es la hora (HHMM)
        /// </summary>
        public static async Task<DataTable> ObtenerFechasHorasPorFilePathAsync(
            string connectionString,
            string filePath,
            string ciudadFiltro = null,
            string radioFiltro = null)
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("Fecha", typeof(DateTime));
            dt.Columns.Add("Hora", typeof(string));

            System.Diagnostics.Debug.WriteLine($"DEBUG ObtenerFechasHoras - FilePath: {filePath}");
            System.Diagnostics.Debug.WriteLine($"DEBUG ObtenerFechasHoras - Ciudad: {ciudadFiltro}, Radio: {radioFiltro}");

            using (NpgsqlConnection conn = new NpgsqlConnection(connectionString))
            {
                await conn.OpenAsync();

                // Construir condiciones WHERE - usar solo FilePath para buscar (case-insensitive)
                // Los filtros de ciudad/radio se aplicarán después si es necesario
                // Esto soluciona problemas de coincidencia por diferencias de encoding o espacios
                string query = @"
                    SELECT Codigo, FechaInicio, FechaFinal, TipoProgramacion, Ciudad, Radio
                    FROM Comerciales
                    WHERE LOWER(FilePath) = LOWER(@FilePath)
                    ORDER BY Codigo";

                Logger.Log($"DB - ObtenerFechasHoras - FilePath: {filePath}");
                Logger.Log($"DB - ObtenerFechasHoras - CiudadFiltro: {ciudadFiltro}, RadioFiltro: {radioFiltro}");

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@FilePath", filePath);

                    // Recopilar los códigos para procesar - filtrar por ciudad/radio en memoria
                    // Esto es más flexible que filtrar en SQL porque maneja diferencias de encoding/espacios
                    var comercialesInfo = new List<(string Codigo, DateTime FechaInicio, DateTime FechaFinal, string TipoProgramacion)>();

                    // Normalizar filtros para comparación flexible
                    string ciudadFiltroNorm = (ciudadFiltro ?? "").Replace(" ", "").ToUpper()
                        .Replace("Ñ", "N").Replace("Á", "A").Replace("É", "E").Replace("Í", "I").Replace("Ó", "O").Replace("Ú", "U");
                    string radioFiltroNorm = (radioFiltro ?? "").Replace(" ", "").ToUpper()
                        .Replace("Ñ", "N").Replace("Á", "A").Replace("É", "E").Replace("Í", "I").Replace("Ó", "O").Replace("Ú", "U");

                    int totalEncontrados = 0;
                    int totalFiltrados = 0;

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            totalEncontrados++;
                            string ciudadBD = reader["Ciudad"]?.ToString() ?? "";
                            string radioBD = reader["Radio"]?.ToString() ?? "";

                            // Normalizar valores de BD
                            string ciudadBDNorm = ciudadBD.Replace(" ", "").ToUpper()
                                .Replace("Ñ", "N").Replace("Á", "A").Replace("É", "E").Replace("Í", "I").Replace("Ó", "O").Replace("Ú", "U");
                            string radioBDNorm = radioBD.Replace(" ", "").ToUpper()
                                .Replace("Ñ", "N").Replace("Á", "A").Replace("É", "E").Replace("Í", "I").Replace("Ó", "O").Replace("Ú", "U");

                            // Comparar con filtros (si hay filtro, debe coincidir; si no hay filtro, pasa)
                            bool ciudadCoincide = string.IsNullOrEmpty(ciudadFiltroNorm) || ciudadBDNorm == ciudadFiltroNorm;
                            bool radioCoincide = string.IsNullOrEmpty(radioFiltroNorm) || radioBDNorm == radioFiltroNorm;

                            if (ciudadCoincide && radioCoincide)
                            {
                                comercialesInfo.Add((
                                    reader["Codigo"].ToString(),
                                    Convert.ToDateTime(reader["FechaInicio"]),
                                    Convert.ToDateTime(reader["FechaFinal"]),
                                    reader["TipoProgramacion"]?.ToString() ?? ""
                                ));
                                totalFiltrados++;
                            }
                        }
                    }

                    Logger.Log($"DB - ObtenerFechasHoras - Total en BD: {totalEncontrados}, Filtrados: {totalFiltrados}");

                    // Si no se encontraron comerciales después del filtro, intentar sin filtros
                    if (comercialesInfo.Count == 0 && totalEncontrados > 0)
                    {
                        Logger.Log("DB - ObtenerFechasHoras - No coincidieron filtros, cargando todos los del FilePath");

                        // Recargar todos sin filtro
                        using (var reader2 = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader2.ReadAsync())
                            {
                                comercialesInfo.Add((
                                    reader2["Codigo"].ToString(),
                                    Convert.ToDateTime(reader2["FechaInicio"]),
                                    Convert.ToDateTime(reader2["FechaFinal"]),
                                    reader2["TipoProgramacion"]?.ToString() ?? ""
                                ));
                            }
                        }
                        Logger.Log($"DB - ObtenerFechasHoras - Cargados sin filtro: {comercialesInfo.Count}");
                    }

                    // Recopilar todas las horas únicas de todos los códigos ACC del FilePath
                    var todasLasHorasACC = new HashSet<string>();
                    foreach (var info in comercialesInfo)
                    {
                        if (info.Codigo.StartsWith("ACC-", StringComparison.OrdinalIgnoreCase))
                        {
                            string[] partes = info.Codigo.Split('-');
                            if (partes.Length >= 5)
                            {
                                string horaStr = partes[partes.Length - 1];
                                if (horaStr.Length == 4)
                                {
                                    todasLasHorasACC.Add($"{horaStr.Substring(0, 2)}:{horaStr.Substring(2, 2)}");
                                }
                            }
                        }
                    }
                    Logger.Log($"DB - ObtenerFechasHoras - Horas ACC únicas encontradas: {todasLasHorasACC.Count} - {string.Join(", ", todasLasHorasACC.OrderBy(h => h).Take(10))}...");

                    // Procesar cada comercial
                    foreach (var info in comercialesInfo)
                    {
                        string codigo = info.Codigo;
                        DateTime fechaInicio = info.FechaInicio;
                        DateTime fechaFinal = info.FechaFinal;
                        string tipoProgramacionBD = info.TipoProgramacion;

                        // Extraer la hora del codigo (formato: ACC-42262-ABA-EXI-0000)
                        // El ultimo segmento es la hora en formato HHMM
                        List<string> horasEncontradas = new List<string>();
                        string[] partes = codigo.Split('-');
                        if (partes.Length >= 5)
                        {
                            string horaStr = partes[partes.Length - 1]; // Ultimo segmento: 0000, 0030, 0100, etc.
                            if (horaStr.Length == 4)
                            {
                                horasEncontradas.Add($"{horaStr.Substring(0, 2)}:{horaStr.Substring(2, 2)}");
                            }
                        }

                        // Si no se pudo extraer la hora del código, buscar TODAS las horas en ComercialesAsignados
                        if (horasEncontradas.Count == 0)
                        {
                            System.Diagnostics.Debug.WriteLine($"DEBUG ObtenerFechasHoras - Código {codigo}: buscando filas en ComercialesAsignados");
                            string queryHora = @"SELECT DISTINCT Fila FROM ComercialesAsignados WHERE Codigo = @Codigo ORDER BY Fila";
                            using (var cmdHora = new NpgsqlCommand(queryHora, conn))
                            {
                                cmdHora.Parameters.AddWithValue("@Codigo", codigo);
                                using (var readerHora = await cmdHora.ExecuteReaderAsync())
                                {
                                    // Usar el TipoProgramacion guardado en la BD para determinar el tipo de tanda
                                    TipoTanda tipoTanda = TandasHorarias.GetTipoTandaFromString(tipoProgramacionBD);

                                    // Si no hay TipoProgramacion guardado o es genérico, detectar por radio como fallback
                                    if (string.IsNullOrEmpty(tipoProgramacionBD) || tipoProgramacionBD == "Importado Access")
                                    {
                                        string radioActual = radioFiltro ?? "";
                                        string radioUpper = radioActual.ToUpper();
                                        if (radioUpper.Contains("KARIBE") || radioUpper.Contains("KALLE"))
                                        {
                                            tipoTanda = TipoTanda.Tandas_00_20_30_50;
                                        }
                                        else
                                        {
                                            tipoTanda = TipoTanda.Tandas_00_30;
                                        }
                                    }

                                    string[] horarios = TandasHorarias.GetHorarios(tipoTanda);

                                    int filasLeidas = 0;
                                    while (await readerHora.ReadAsync())
                                    {
                                        int fila = Convert.ToInt32(readerHora["Fila"]);
                                        filasLeidas++;
                                        System.Diagnostics.Debug.WriteLine($"DEBUG ObtenerFechasHoras - Fila encontrada: {fila}, Tipo tanda: {tipoTanda}, Horarios disponibles: {horarios.Length}");
                                        if (fila >= 0 && fila < horarios.Length)
                                        {
                                            horasEncontradas.Add(horarios[fila]);
                                        }
                                    }
                                    System.Diagnostics.Debug.WriteLine($"DEBUG ObtenerFechasHoras - Total filas leídas de ComercialesAsignados: {filasLeidas}");
                                }
                            }
                        }

                        System.Diagnostics.Debug.WriteLine($"DEBUG ObtenerFechasHoras - Código {codigo}: horas encontradas = {horasEncontradas.Count}");

                        // Si aún no hay horas, significa que no hay pautas asignadas
                        if (horasEncontradas.Count == 0)
                        {
                            System.Diagnostics.Debug.WriteLine($"DEBUG ObtenerFechasHoras - Código {codigo}: SIN horas asignadas, no agregar fechas");
                            continue; // No agregar nada para este código
                        }

                        // Obtener las fechas REALES donde hay pautas (no generar todas del rango)
                        // Primero intentar con registros que tienen Fecha
                        var fechasReales = new HashSet<DateTime>();
                        string queryFechasReales = @"SELECT DISTINCT Fecha FROM ComercialesAsignados
                                                     WHERE Codigo = @Codigo AND Fecha IS NOT NULL";
                        using (var cmdFechas = new NpgsqlCommand(queryFechasReales, conn))
                        {
                            cmdFechas.Parameters.AddWithValue("@Codigo", codigo);
                            using (var readerFechas = await cmdFechas.ExecuteReaderAsync())
                            {
                                while (await readerFechas.ReadAsync())
                                {
                                    fechasReales.Add(Convert.ToDateTime(readerFechas["Fecha"]).Date);
                                }
                            }
                        }

                        System.Diagnostics.Debug.WriteLine($"DEBUG ObtenerFechasHoras - Código {codigo}: fechas reales encontradas = {fechasReales.Count}");

                        // Si hay fechas reales en ComercialesAsignados, usarlas
                        if (fechasReales.Count > 0)
                        {
                            foreach (DateTime fecha in fechasReales.OrderBy(f => f))
                            {
                                foreach (string hora in horasEncontradas)
                                {
                                    dt.Rows.Add(fecha.Date, hora);
                                }
                            }
                        }
                        else
                        {
                            // Para comerciales ACC (Access) que tienen la hora en el código,
                            // generar fechas del rango FechaInicio-FechaFinal directamente
                            // ya que estos NO tienen registros en ComercialesAsignados
                            bool esCodigoACC = codigo.StartsWith("ACC-", StringComparison.OrdinalIgnoreCase);

                            if (esCodigoACC && horasEncontradas.Count > 0)
                            {
                                // Detectar si es parte de una secuencia (archivo termina en 01, 02, 03, 04, 05)
                                string nombreArchivo = System.IO.Path.GetFileNameWithoutExtension(filePath);
                                int? numeroSecuencia = ExtraerNumeroSecuencia(nombreArchivo);

                                // Verificar si hay otros archivos en la misma secuencia
                                bool esSecuencia = false;
                                int totalArchivosSecuencia = 1;

                                if (numeroSecuencia.HasValue)
                                {
                                    // Extraer la clave base de la secuencia (sin el número)
                                    string claveBase = ExtraerClaveBaseSecuencia(nombreArchivo);
                                    if (!string.IsNullOrEmpty(claveBase))
                                    {
                                        // Buscar otros archivos con la misma clave base en la BD
                                        string querySecuencia = @"
                                            SELECT COUNT(DISTINCT FilePath)
                                            FROM Comerciales
                                            WHERE FilePath LIKE @Pattern
                                              AND Ciudad = @Ciudad
                                              AND Radio = @Radio";
                                        using (var cmdSeq = new NpgsqlCommand(querySecuencia, conn))
                                        {
                                            // Buscar archivos que empiecen con la misma base
                                            string directorio = System.IO.Path.GetDirectoryName(filePath);
                                            string pattern = System.IO.Path.Combine(directorio, claveBase + "%").Replace("\\", "\\\\");
                                            cmdSeq.Parameters.AddWithValue("@Pattern", pattern.Replace("\\\\", "\\"));
                                            cmdSeq.Parameters.AddWithValue("@Ciudad", ciudadFiltro ?? "");
                                            cmdSeq.Parameters.AddWithValue("@Radio", radioFiltro ?? "");

                                            var result = await cmdSeq.ExecuteScalarAsync();
                                            if (result != null && result != DBNull.Value)
                                            {
                                                totalArchivosSecuencia = Convert.ToInt32(result);
                                                esSecuencia = totalArchivosSecuencia > 1;
                                            }
                                        }
                                    }
                                }

                                // Comercial Access: generar fechas del rango
                                foreach (string hora in horasEncontradas)
                                {
                                    DateTime fechaActual = fechaInicio;
                                    while (fechaActual <= fechaFinal)
                                    {
                                        bool agregarFecha = true;

                                        // Si es parte de una secuencia, aplicar lógica de rotación
                                        if (esSecuencia && numeroSecuencia.HasValue && totalArchivosSecuencia > 1)
                                        {
                                            DayOfWeek diaSemana = fechaActual.DayOfWeek;

                                            // Secuencias solo aplican de Lunes a Viernes
                                            if (diaSemana == DayOfWeek.Saturday || diaSemana == DayOfWeek.Sunday)
                                            {
                                                // Sábados y domingos: no mostrar secuencias
                                                agregarFecha = false;
                                            }
                                            else
                                            {
                                                // Calcular qué número de secuencia corresponde a este día
                                                // Lunes=0, Martes=1, Miércoles=2, Jueves=3, Viernes=4
                                                int indiceDia = ((int)diaSemana - 1); // Lunes=0
                                                if (indiceDia < 0) indiceDia = 0;

                                                // El número de archivo en la secuencia (1-based)
                                                int numeroArchivoEsperado = (indiceDia % totalArchivosSecuencia) + 1;

                                                // Solo mostrar si este archivo corresponde a este día
                                                agregarFecha = (numeroSecuencia.Value == numeroArchivoEsperado);
                                            }
                                        }

                                        if (agregarFecha)
                                        {
                                            dt.Rows.Add(fechaActual.Date, hora);
                                        }
                                        fechaActual = fechaActual.AddDays(1);
                                    }
                                }
                            }
                            else
                            {
                                // Comerciales CU: verificar si hay filas en ComercialesAsignados
                                string queryConteo = "SELECT COUNT(*) FROM ComercialesAsignados WHERE Codigo = @Codigo";
                                using (var cmdConteo = new NpgsqlCommand(queryConteo, conn))
                                {
                                    cmdConteo.Parameters.AddWithValue("@Codigo", codigo);
                                    int conteo = Convert.ToInt32(await cmdConteo.ExecuteScalarAsync());

                                    if (conteo > 0)
                                    {
                                        // Generar todas las combinaciones fecha+hora del rango
                                        foreach (string hora in horasEncontradas)
                                        {
                                            DateTime fechaActual = fechaInicio;
                                            while (fechaActual <= fechaFinal)
                                            {
                                                dt.Rows.Add(fechaActual.Date, hora);
                                                fechaActual = fechaActual.AddDays(1);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            Logger.Log($"DB - ObtenerFechasHoras - Total filas generadas: {dt.Rows.Count}");

            // Ordenar por Fecha y Hora
            DataView dv = dt.DefaultView;
            dv.Sort = "Fecha ASC, Hora ASC";
            return dv.ToTable();
        }

        /// <summary>
        /// Extrae el número de secuencia de un nombre de archivo (ej: "SECUENCIA PAPILLON KARIBEÑA-2025 01" -> 1)
        /// </summary>
        private static int? ExtraerNumeroSecuencia(string nombreArchivo)
        {
            if (string.IsNullOrEmpty(nombreArchivo))
                return null;

            // Buscar patrón de número al final: " 01", " 02", etc. o "-01", "-02", etc.
            var match = System.Text.RegularExpressions.Regex.Match(
                nombreArchivo,
                @"[\s\-_](\d{2})$",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            if (match.Success && int.TryParse(match.Groups[1].Value, out int numero))
            {
                return numero;
            }

            return null;
        }

        /// <summary>
        /// Extrae la clave base de una secuencia (sin el número final)
        /// Ej: "SECUENCIA PAPILLON KARIBEÑA-2025 01" -> "SECUENCIA PAPILLON KARIBEÑA-2025"
        /// </summary>
        private static string ExtraerClaveBaseSecuencia(string nombreArchivo)
        {
            if (string.IsNullOrEmpty(nombreArchivo))
                return null;

            // Eliminar el número final con su separador
            var match = System.Text.RegularExpressions.Regex.Match(
                nombreArchivo,
                @"^(.+?)[\s\-_]\d{2}$",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            if (match.Success)
            {
                return match.Groups[1].Value;
            }

            return null;
        }

        /// <summary>
        /// Inserta múltiples asignaciones de comerciales de forma masiva y eficiente.
        /// Usado por el Pauteo Rápido para insertar todas las tandas seleccionadas.
        /// </summary>
        public static async Task InsertarAsignacionesMasivasAsync(
            string connectionString,
            List<(int fila, int columna, string comercial, string codigo, DateTime fecha)> asignaciones)
        {
            if (asignaciones == null || asignaciones.Count == 0)
                return;

            using (NpgsqlConnection conn = new NpgsqlConnection(connectionString))
            {
                await conn.OpenAsync();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        // Usar COPY para inserción masiva (mucho más rápido que INSERTs individuales)
                        // Pero como puede haber problemas de compatibilidad, usamos INSERT por lotes

                        const int batchSize = 500;
                        for (int i = 0; i < asignaciones.Count; i += batchSize)
                        {
                            var batch = asignaciones.Skip(i).Take(batchSize).ToList();

                            var sb = new System.Text.StringBuilder();
                            sb.Append("INSERT INTO ComercialesAsignados (Fila, Columna, ComercialAsignado, Codigo, Fecha) VALUES ");

                            var parameters = new List<NpgsqlParameter>();
                            var valuesList = new List<string>();

                            for (int j = 0; j < batch.Count; j++)
                            {
                                var (fila, columna, comercial, codigo, fecha) = batch[j];

                                string filaParam = $"@Fila{j}";
                                string colParam = $"@Col{j}";
                                string comParam = $"@Com{j}";
                                string codParam = $"@Cod{j}";
                                string fechaParam = $"@Fecha{j}";

                                valuesList.Add($"({filaParam}, {colParam}, {comParam}, {codParam}, {fechaParam})");

                                parameters.Add(new NpgsqlParameter(filaParam, fila));
                                parameters.Add(new NpgsqlParameter(colParam, columna));
                                parameters.Add(new NpgsqlParameter(comParam, comercial));
                                parameters.Add(new NpgsqlParameter(codParam, codigo));
                                parameters.Add(new NpgsqlParameter(fechaParam, fecha.Date));
                            }

                            sb.Append(string.Join(", ", valuesList));

                            using (var cmd = new NpgsqlCommand(sb.ToString(), conn, transaction))
                            {
                                cmd.Parameters.AddRange(parameters.ToArray());
                                await cmd.ExecuteNonQueryAsync();
                            }
                        }

                        transaction.Commit();
                        System.Diagnostics.Debug.WriteLine($"[DataAccess] Insertadas {asignaciones.Count} asignaciones masivas");

                        // Invalidar caché después de insertar asignaciones
                        CacheService.InvalidarAsignaciones();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// Verifica si ya existe un comercial con el mismo FilePath, Ciudad y Radio en el rango de fechas.
        /// </summary>
        public static async Task<bool> ExisteComercialDuplicadoAsync(
            string connectionString,
            string tableName,
            string filePath,
            string ciudad,
            string radio,
            DateTime fechaInicio,
            DateTime fechaFin)
        {
            using (NpgsqlConnection conn = new NpgsqlConnection(connectionString))
            {
                await conn.OpenAsync();
                string query = $@"
                    SELECT COUNT(*) FROM {tableName}
                    WHERE LOWER(FilePath) = LOWER(@FilePath)
                      AND Ciudad = @Ciudad
                      AND Radio = @Radio
                      AND NOT (FechaFinal < @FechaInicio OR FechaInicio > @FechaFin)";

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@FilePath", filePath);
                    cmd.Parameters.AddWithValue("@Ciudad", ciudad);
                    cmd.Parameters.AddWithValue("@Radio", radio);
                    cmd.Parameters.AddWithValue("@FechaInicio", fechaInicio);
                    cmd.Parameters.AddWithValue("@FechaFin", fechaFin);

                    int count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                    return count > 0;
                }
            }
        }
    }
}

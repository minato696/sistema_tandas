using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Npgsql;

namespace Generador_Pautas
{
    public class DatabaseService
    {
        public string ConnectionString { get; set; }
        public string TableName { get; set; }

        public DatabaseService()
        {
            ConnectionString = DatabaseConfig.ConnectionString;
            TableName = DatabaseConfig.TableName;
        }

        public async Task<List<ComercialAsignadoInfo>> ObtenerComercialesAsignadosDesdeBaseDeDatosAsync(string codigo)
        {
            List<ComercialAsignadoInfo> comercialesAsignados = new List<ComercialAsignadoInfo>();

            using (NpgsqlConnection conn = new NpgsqlConnection(ConnectionString))
            {
                await conn.OpenAsync();
                string query = "SELECT Fila, Columna, ComercialAsignado, Fecha FROM ComercialesAsignados WHERE Codigo = @Codigo";
                using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Codigo", codigo);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            int fila = Convert.ToInt32(reader["Fila"]);
                            int columna = Convert.ToInt32(reader["Columna"]);
                            string comercialAsignado = reader["ComercialAsignado"].ToString();
                            DateTime? fecha = reader["Fecha"] != DBNull.Value ? Convert.ToDateTime(reader["Fecha"]) : (DateTime?)null;

                            comercialesAsignados.Add(new ComercialAsignadoInfo
                            {
                                Fila = fila,
                                Columna = columna,
                                ComercialAsignado = comercialAsignado,
                                Fecha = fecha
                            });
                        }
                    }
                }
            }

            return comercialesAsignados;
        }

        /// <summary>
        /// Obtiene todos los comerciales asignados para un archivo/ciudad/radio específico.
        /// Esto busca en todos los códigos que pertenecen al mismo archivo.
        /// Retorna el valor con formato "P## NombreArchivo" para mostrar en el grid.
        /// </summary>
        public async Task<List<ComercialAsignadoInfo>> ObtenerComercialesAsignadosPorFilePathAsync(string filePath, string ciudad, string radio)
        {
            Logger.LogSeparador();
            Logger.Log("DB_SERVICE - ObtenerComercialesAsignadosPorFilePathAsync INICIADO");
            Logger.Log($"DB_SERVICE - FilePath: {filePath}");
            Logger.Log($"DB_SERVICE - Ciudad: {ciudad}");
            Logger.Log($"DB_SERVICE - Radio: {radio}");

            List<ComercialAsignadoInfo> comercialesAsignados = new List<ComercialAsignadoInfo>();

            using (NpgsqlConnection conn = new NpgsqlConnection(ConnectionString))
            {
                await conn.OpenAsync();

                // Primero verificar si existe el comercial en la tabla Comerciales
                string queryVerificar = @"SELECT Codigo FROM Comerciales WHERE FilePath = @FilePath AND Ciudad = @Ciudad AND Radio = @Radio";
                using (NpgsqlCommand cmdVerificar = new NpgsqlCommand(queryVerificar, conn))
                {
                    cmdVerificar.Parameters.AddWithValue("@FilePath", filePath ?? "");
                    cmdVerificar.Parameters.AddWithValue("@Ciudad", ciudad ?? "");
                    cmdVerificar.Parameters.AddWithValue("@Radio", radio ?? "");
                    var codigoEncontrado = await cmdVerificar.ExecuteScalarAsync();
                    Logger.Log($"DB_SERVICE - Codigo encontrado en Comerciales: {codigoEncontrado ?? "NULL"}");
                }

                // Verificar registros en ComercialesAsignados para este código
                string queryContarAsignados = @"SELECT COUNT(*) FROM ComercialesAsignados ca
                                                INNER JOIN Comerciales c ON ca.Codigo = c.Codigo
                                                WHERE c.FilePath = @FilePath AND c.Ciudad = @Ciudad AND c.Radio = @Radio";
                using (NpgsqlCommand cmdContar = new NpgsqlCommand(queryContarAsignados, conn))
                {
                    cmdContar.Parameters.AddWithValue("@FilePath", filePath ?? "");
                    cmdContar.Parameters.AddWithValue("@Ciudad", ciudad ?? "");
                    cmdContar.Parameters.AddWithValue("@Radio", radio ?? "");
                    var totalAsignados = await cmdContar.ExecuteScalarAsync();
                    Logger.Log($"DB_SERVICE - Total asignados en ComercialesAsignados: {totalAsignados}");
                }

                // Buscar todos los comerciales asignados con la posición del comercial (incluir Fecha)
                string query = @"SELECT ca.Fila, ca.Columna, ca.ComercialAsignado, ca.Fecha, c.Posicion
                                FROM ComercialesAsignados ca
                                INNER JOIN Comerciales c ON ca.Codigo = c.Codigo
                                WHERE c.FilePath = @FilePath
                                  AND c.Ciudad = @Ciudad
                                  AND c.Radio = @Radio
                                ORDER BY ca.Fecha, ca.Fila";

                using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@FilePath", filePath ?? "");
                    cmd.Parameters.AddWithValue("@Ciudad", ciudad ?? "");
                    cmd.Parameters.AddWithValue("@Radio", radio ?? "");

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            int fila = Convert.ToInt32(reader["Fila"]);
                            int columna = Convert.ToInt32(reader["Columna"]);
                            string comercialAsignado = reader["ComercialAsignado"].ToString();
                            string posicion = reader["Posicion"].ToString();

                            // Construir el valor para mostrar en el grid con formato "P## NombreArchivo"
                            string valorParaGrid = comercialAsignado;

                            // Si el comercialAsignado no tiene la prioridad (no empieza con P##), agregarla
                            if (!comercialAsignado.StartsWith("P", StringComparison.OrdinalIgnoreCase) ||
                                comercialAsignado.Length < 4 ||
                                comercialAsignado[3] != ' ')
                            {
                                // Formatear la posición correctamente
                                string posicionFormateada = posicion;
                                if (!string.IsNullOrEmpty(posicion) && !posicion.StartsWith("P", StringComparison.OrdinalIgnoreCase))
                                {
                                    posicionFormateada = $"P{posicion}";
                                }
                                valorParaGrid = $"{posicionFormateada} {comercialAsignado}";
                            }

                            DateTime? fecha = reader["Fecha"] != DBNull.Value ? Convert.ToDateTime(reader["Fecha"]) : (DateTime?)null;

                            comercialesAsignados.Add(new ComercialAsignadoInfo
                            {
                                Fila = fila,
                                Columna = columna,
                                ComercialAsignado = valorParaGrid,
                                Fecha = fecha
                            });
                        }
                    }
                }
            }

            Logger.Log($"DB_SERVICE - Total comercialesAsignados retornados: {comercialesAsignados.Count}");
            return comercialesAsignados;
        }

        /// <summary>
        /// Obtiene las filas (horas) únicas que tienen comerciales para un archivo/ciudad/radio.
        /// Útil para comerciales importados de Access: retorna solo las filas, no genera columnas.
        /// </summary>
        /// <param name="tipoTanda">Tipo de tanda para calcular las filas correctamente (48 o 96 tandas)</param>
        public async Task<HashSet<int>> ObtenerFilasUnicasDeAccess(string filePath, string ciudad, string radio, TipoTanda tipoTanda = TipoTanda.Tandas_00_30)
        {
            var filas = new HashSet<int>();

            using (NpgsqlConnection conn = new NpgsqlConnection(ConnectionString))
            {
                await conn.OpenAsync();

                string query = @"SELECT DISTINCT Codigo FROM Comerciales
                                WHERE FilePath = @FilePath
                                  AND Ciudad = @Ciudad
                                  AND Radio = @Radio";

                using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@FilePath", filePath ?? "");
                    cmd.Parameters.AddWithValue("@Ciudad", ciudad ?? "");
                    cmd.Parameters.AddWithValue("@Radio", radio ?? "");

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            string codigo = reader["Codigo"].ToString();
                            // Extraer hora del código: ACC-550-ABA-EXI-0600 -> 0600
                            string[] partes = codigo.Split('-');
                            if (partes.Length >= 5)
                            {
                                string horaStr = partes[partes.Length - 1];
                                if (horaStr.Length == 4 &&
                                    int.TryParse(horaStr.Substring(0, 2), out int hora) &&
                                    int.TryParse(horaStr.Substring(2, 2), out int minuto))
                                {
                                    // Usar TandasHorarias para calcular la fila correcta según el tipo de tanda
                                    int fila = TandasHorarias.GetFilaParaHoraMinutos(hora, minuto, tipoTanda);
                                    if (fila >= 0)
                                    {
                                        filas.Add(fila);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return filas;
        }

        /// <summary>
        /// Para registros importados de Access que no tienen ComercialesAsignados,
        /// reconstruye las asignaciones basándose en la hora del código.
        /// El código tiene formato ACC-550-ABA-EXI-0600 donde 0600 = 06:00.
        /// </summary>
        /// <param name="tipoTanda">Tipo de tanda para calcular las filas correctamente (48 o 96 tandas)</param>
        public async Task<List<ComercialAsignadoInfo>> ReconstruirAsignacionesDesdeCodigosAsync(string filePath, string ciudad, string radio, DateTime fechaInicio, DateTime fechaFinal, TipoTanda tipoTanda = TipoTanda.Tandas_00_30)
        {
            List<ComercialAsignadoInfo> asignaciones = new List<ComercialAsignadoInfo>();

            using (NpgsqlConnection conn = new NpgsqlConnection(ConnectionString))
            {
                await conn.OpenAsync();

                // Obtener todos los códigos para este archivo/ciudad/radio
                string query = @"SELECT Codigo FROM Comerciales
                                WHERE FilePath = @FilePath
                                  AND Ciudad = @Ciudad
                                  AND Radio = @Radio
                                ORDER BY Codigo";

                using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@FilePath", filePath ?? "");
                    cmd.Parameters.AddWithValue("@Ciudad", ciudad ?? "");
                    cmd.Parameters.AddWithValue("@Radio", radio ?? "");

                    var codigos = new List<string>();
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            codigos.Add(reader["Codigo"].ToString());
                        }
                    }

                    // Para cada código, extraer la hora y convertirla a fila
                    // Formato código: ACC-550-ABA-EXI-0600 donde 0600 = 06:00
                    string nombreArchivo = System.IO.Path.GetFileNameWithoutExtension(filePath);

                    foreach (string codigo in codigos)
                    {
                        string[] partes = codigo.Split('-');
                        if (partes.Length >= 5)
                        {
                            string horaStr = partes[partes.Length - 1]; // 0600, 0630, 0700, etc.
                            if (horaStr.Length == 4 && int.TryParse(horaStr.Substring(0, 2), out int hora) && int.TryParse(horaStr.Substring(2, 2), out int minuto))
                            {
                                // Usar TandasHorarias para calcular la fila correcta según el tipo de tanda
                                int fila = TandasHorarias.GetFilaParaHoraMinutos(hora, minuto, tipoTanda);

                                if (fila >= 0)
                                {
                                    // Para cada día de la semana en el rango de fechas
                                    // Columnas: 2=Lunes, 3=Martes, 4=Miércoles, 5=Jueves, 6=Viernes, 7=Sábado, 8=Domingo
                                    for (int columna = 2; columna <= 8; columna++)
                                    {
                                        asignaciones.Add(new ComercialAsignadoInfo
                                        {
                                            Fila = fila,
                                            Columna = columna,
                                            ComercialAsignado = nombreArchivo
                                        });
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return asignaciones;
        }

        public async Task InsertarComercialAsignadoEnBaseDeDatosAsync(string codigo, ComercialAsignadoInfo info)
        {
            using (NpgsqlConnection conn = new NpgsqlConnection(ConnectionString))
            {
                await conn.OpenAsync();

                string query = "INSERT INTO ComercialesAsignados (Codigo, Fila, Columna, ComercialAsignado) " +
                               "VALUES (@codigo, @fila, @columna, @comercialAsignado)";

                using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@codigo", codigo);
                    cmd.Parameters.AddWithValue("@fila", info.Fila);
                    cmd.Parameters.AddWithValue("@columna", info.Columna);
                    cmd.Parameters.AddWithValue("@comercialAsignado", info.ComercialAsignado);

                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        /// <summary>
        /// Inserta múltiples comerciales asignados en una sola transacción para mejor rendimiento
        /// </summary>
        public async Task InsertarComercialesAsignadosPorLoteAsync(string codigo, List<ComercialAsignadoInfo> lote)
        {
            if (lote == null || lote.Count == 0) return;

            using (NpgsqlConnection conn = new NpgsqlConnection(ConnectionString))
            {
                await conn.OpenAsync();

                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        // Usar COPY para inserción masiva (incluyendo Fecha)
                        using (var writer = conn.BeginBinaryImport(
                            "COPY ComercialesAsignados (Codigo, Fila, Columna, ComercialAsignado, Fecha) FROM STDIN (FORMAT BINARY)"))
                        {
                            foreach (var info in lote)
                            {
                                writer.StartRow();
                                writer.Write(codigo, NpgsqlTypes.NpgsqlDbType.Text);
                                writer.Write(info.Fila, NpgsqlTypes.NpgsqlDbType.Integer);
                                writer.Write(info.Columna, NpgsqlTypes.NpgsqlDbType.Integer);
                                writer.Write(info.ComercialAsignado ?? "", NpgsqlTypes.NpgsqlDbType.Text);

                                // Guardar la fecha (si existe)
                                if (info.Fecha.HasValue)
                                    writer.Write(info.Fecha.Value, NpgsqlTypes.NpgsqlDbType.Date);
                                else
                                    writer.WriteNull();
                            }
                            writer.Complete();
                        }

                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// NUEVO: Inserta TODOS los comerciales asignados en UNA SOLA operación COPY.
        /// Mucho más rápido que múltiples lotes porque usa una sola conexión y transacción.
        /// </summary>
        public async Task InsertarTodosComercialesAsignadosMasivoAsync(
            string codigo,
            List<ComercialAsignadoInfo> comerciales,
            IProgress<(int porcentaje, string mensaje)> progress = null)
        {
            if (comerciales == null || comerciales.Count == 0) return;

            int total = comerciales.Count;
            int procesados = 0;
            int ultimoReporte = 0;

            using (NpgsqlConnection conn = new NpgsqlConnection(ConnectionString))
            {
                await conn.OpenAsync();

                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        // Una sola operación COPY para todos los registros (incluyendo Fecha)
                        using (var writer = conn.BeginBinaryImport(
                            "COPY ComercialesAsignados (Codigo, Fila, Columna, ComercialAsignado, Fecha) FROM STDIN (FORMAT BINARY)"))
                        {
                            foreach (var info in comerciales)
                            {
                                writer.StartRow();
                                writer.Write(codigo, NpgsqlTypes.NpgsqlDbType.Text);
                                writer.Write(info.Fila, NpgsqlTypes.NpgsqlDbType.Integer);
                                writer.Write(info.Columna, NpgsqlTypes.NpgsqlDbType.Integer);
                                writer.Write(info.ComercialAsignado ?? "", NpgsqlTypes.NpgsqlDbType.Text);

                                // Guardar la fecha (si existe)
                                if (info.Fecha.HasValue)
                                    writer.Write(info.Fecha.Value, NpgsqlTypes.NpgsqlDbType.Date);
                                else
                                    writer.WriteNull();

                                procesados++;

                                // Reportar progreso cada 2% (para no sobrecargar la UI)
                                if (progress != null)
                                {
                                    int porcentajeActual = (procesados * 100) / total;
                                    if (porcentajeActual >= ultimoReporte + 2)
                                    {
                                        ultimoReporte = porcentajeActual;
                                        progress.Report((porcentajeActual, $"Insertando... {procesados:N0}/{total:N0}"));
                                    }
                                }
                            }
                            await writer.CompleteAsync();
                        }

                        await transaction.CommitAsync();
                        progress?.Report((100, $"Completado: {total:N0} registros"));
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                }
            }
        }

        public async Task<int> EliminarComercialesAsignadosPorCodigoAsync(string codigo)
        {
            using (NpgsqlConnection conn = new NpgsqlConnection(ConnectionString))
            {
                await conn.OpenAsync();

                string query = "DELETE FROM ComercialesAsignados WHERE Codigo LIKE @codigo";

                using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@codigo", $"%{codigo}%");
                    return await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        /// <summary>
        /// Elimina TODOS los comerciales asignados que pertenecen a comerciales con el mismo FilePath/Ciudad/Radio.
        /// Esto asegura que al guardar un comercial, se eliminen TODOS los registros relacionados
        /// (de todas las posiciones) antes de insertar los nuevos.
        /// </summary>
        public async Task<int> EliminarComercialesAsignadosPorFilePathAsync(string filePath, string ciudad, string radio)
        {
            using (NpgsqlConnection conn = new NpgsqlConnection(ConnectionString))
            {
                await conn.OpenAsync();

                // Eliminar de ComercialesAsignados todos los registros cuyos códigos
                // pertenecen a comerciales con el mismo FilePath/Ciudad/Radio
                string query = @"DELETE FROM ComercialesAsignados
                                WHERE Codigo IN (
                                    SELECT Codigo FROM Comerciales
                                    WHERE FilePath = @FilePath
                                    AND Ciudad = @Ciudad
                                    AND Radio = @Radio
                                )";

                using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@FilePath", filePath ?? "");
                    cmd.Parameters.AddWithValue("@Ciudad", ciudad ?? "");
                    cmd.Parameters.AddWithValue("@Radio", radio ?? "");
                    int eliminados = await cmd.ExecuteNonQueryAsync();
                    Logger.Log($"DB_SERVICE - EliminarComercialesAsignadosPorFilePathAsync - Eliminados: {eliminados}");
                    return eliminados;
                }
            }
        }

        /// <summary>
        /// Elimina comerciales asignados por codigo y rango de fechas.
        /// Busca comerciales que coincidan con el codigo (parcial) y que tengan fechas dentro del rango.
        /// </summary>
        public async Task<int> EliminarComercialesAsignadosPorCodigoYFechasAsync(
            string codigo, DateTime fechaInicio, DateTime fechaFin, string ciudad, string radio)
        {
            int totalEliminados = 0;

            using (NpgsqlConnection conn = new NpgsqlConnection(ConnectionString))
            {
                await conn.OpenAsync();

                // Obtener la FechaInicio del comercial para calcular columnas (registros legacy)
                DateTime? fechaInicioComercial = null;
                string queryFechaInicio = @"SELECT FechaInicio FROM Comerciales
                                            WHERE Codigo LIKE @Codigo
                                            AND LOWER(Ciudad) = LOWER(@Ciudad)
                                            AND LOWER(Radio) = LOWER(@Radio)
                                            LIMIT 1";
                using (var cmdFecha = new NpgsqlCommand(queryFechaInicio, conn))
                {
                    cmdFecha.Parameters.AddWithValue("@Codigo", $"%{codigo}%");
                    cmdFecha.Parameters.AddWithValue("@Ciudad", ciudad ?? "");
                    cmdFecha.Parameters.AddWithValue("@Radio", radio ?? "");
                    var result = await cmdFecha.ExecuteScalarAsync();
                    if (result != null && result != DBNull.Value)
                    {
                        fechaInicioComercial = (DateTime)result;
                    }
                }

                // 1. Eliminar registros CON fecha (registros nuevos)
                string queryEliminarConFecha = @"DELETE FROM ComercialesAsignados
                                         WHERE Codigo IN (
                                             SELECT Codigo FROM Comerciales
                                             WHERE Codigo LIKE @Codigo
                                             AND LOWER(Ciudad) = LOWER(@Ciudad)
                                             AND LOWER(Radio) = LOWER(@Radio)
                                         )
                                         AND Fecha IS NOT NULL
                                         AND Fecha::date >= @FechaInicio::date
                                         AND Fecha::date <= @FechaFin::date";

                using (NpgsqlCommand cmdEliminar = new NpgsqlCommand(queryEliminarConFecha, conn))
                {
                    cmdEliminar.Parameters.AddWithValue("@Codigo", $"%{codigo}%");
                    cmdEliminar.Parameters.AddWithValue("@Ciudad", ciudad ?? "");
                    cmdEliminar.Parameters.AddWithValue("@Radio", radio ?? "");
                    cmdEliminar.Parameters.AddWithValue("@FechaInicio", fechaInicio);
                    cmdEliminar.Parameters.AddWithValue("@FechaFin", fechaFin);

                    int eliminadosConFecha = await cmdEliminar.ExecuteNonQueryAsync();
                    totalEliminados += eliminadosConFecha;
                }

                // 2. Eliminar registros SIN fecha (registros legacy que usan Columna)
                if (fechaInicioComercial.HasValue)
                {
                    // Calcular las columnas correspondientes al rango de fechas
                    int columnaInicio = (fechaInicio - fechaInicioComercial.Value).Days + 2;
                    int columnaFinal = (fechaFin - fechaInicioComercial.Value).Days + 2;

                    string queryEliminarSinFecha = @"DELETE FROM ComercialesAsignados
                                             WHERE Codigo IN (
                                                 SELECT Codigo FROM Comerciales
                                                 WHERE Codigo LIKE @Codigo
                                                 AND LOWER(Ciudad) = LOWER(@Ciudad)
                                                 AND LOWER(Radio) = LOWER(@Radio)
                                             )
                                             AND Fecha IS NULL
                                             AND Columna >= @ColumnaInicio
                                             AND Columna <= @ColumnaFinal";

                    using (NpgsqlCommand cmdEliminarLegacy = new NpgsqlCommand(queryEliminarSinFecha, conn))
                    {
                        cmdEliminarLegacy.Parameters.AddWithValue("@Codigo", $"%{codigo}%");
                        cmdEliminarLegacy.Parameters.AddWithValue("@Ciudad", ciudad ?? "");
                        cmdEliminarLegacy.Parameters.AddWithValue("@Radio", radio ?? "");
                        cmdEliminarLegacy.Parameters.AddWithValue("@ColumnaInicio", columnaInicio);
                        cmdEliminarLegacy.Parameters.AddWithValue("@ColumnaFinal", columnaFinal);

                        int eliminadosSinFecha = await cmdEliminarLegacy.ExecuteNonQueryAsync();
                        totalEliminados += eliminadosSinFecha;
                    }
                }
            }

            return totalEliminados;
        }

        /// <summary>
        /// Elimina comerciales asignados por codigo y hora (fila) especifica.
        /// </summary>
        public async Task<int> EliminarComercialesAsignadosPorCodigoYHoraAsync(
            string codigo, int fila, string ciudad, string radio)
        {
            int totalEliminados = 0;

            using (NpgsqlConnection conn = new NpgsqlConnection(ConnectionString))
            {
                await conn.OpenAsync();

                // Primero contar cuántos registros hay antes de eliminar
                string queryContar = @"SELECT COUNT(*) FROM ComercialesAsignados ca
                                       INNER JOIN Comerciales c ON ca.Codigo = c.Codigo
                                       WHERE ca.Codigo LIKE @Codigo
                                       AND c.Ciudad = @Ciudad
                                       AND c.Radio = @Radio
                                       AND ca.Fila = @Fila";

                using (NpgsqlCommand cmdContar = new NpgsqlCommand(queryContar, conn))
                {
                    cmdContar.Parameters.AddWithValue("@Codigo", $"%{codigo}%");
                    cmdContar.Parameters.AddWithValue("@Ciudad", ciudad ?? "");
                    cmdContar.Parameters.AddWithValue("@Radio", radio ?? "");
                    cmdContar.Parameters.AddWithValue("@Fila", fila);
                    var count = await cmdContar.ExecuteScalarAsync();
                }

                // Eliminar directamente con una sola query (más eficiente)
                string queryEliminar = @"DELETE FROM ComercialesAsignados
                                         WHERE Codigo IN (
                                             SELECT Codigo FROM Comerciales
                                             WHERE Codigo LIKE @Codigo
                                             AND Ciudad = @Ciudad
                                             AND Radio = @Radio
                                         )
                                         AND Fila = @Fila";

                using (NpgsqlCommand cmdEliminar = new NpgsqlCommand(queryEliminar, conn))
                {
                    cmdEliminar.Parameters.AddWithValue("@Codigo", $"%{codigo}%");
                    cmdEliminar.Parameters.AddWithValue("@Ciudad", ciudad ?? "");
                    cmdEliminar.Parameters.AddWithValue("@Radio", radio ?? "");
                    cmdEliminar.Parameters.AddWithValue("@Fila", fila);

                    totalEliminados = await cmdEliminar.ExecuteNonQueryAsync();
                }
            }

            return totalEliminados;
        }

        /// <summary>
        /// Elimina comerciales asignados por codigo, hora (fila) especifica Y rango de fechas.
        /// Combina filtro de hora + fechas para eliminación precisa.
        /// </summary>
        public async Task<int> EliminarComercialesAsignadosPorCodigoHoraYFechasAsync(
            string codigo, int fila, DateTime fechaInicio, DateTime fechaFin, string ciudad, string radio)
        {
            int totalEliminados = 0;

            using (NpgsqlConnection conn = new NpgsqlConnection(ConnectionString))
            {
                await conn.OpenAsync();

                // Primero contar cuántos registros hay antes de eliminar
                string queryContar = @"SELECT COUNT(*) FROM ComercialesAsignados ca
                                       INNER JOIN Comerciales c ON ca.Codigo = c.Codigo
                                       WHERE ca.Codigo LIKE @Codigo
                                       AND c.Ciudad = @Ciudad
                                       AND c.Radio = @Radio
                                       AND ca.Fila = @Fila
                                       AND ca.Fecha IS NOT NULL
                                       AND ca.Fecha::date >= @FechaInicio::date
                                       AND ca.Fecha::date <= @FechaFin::date";

                using (NpgsqlCommand cmdContar = new NpgsqlCommand(queryContar, conn))
                {
                    cmdContar.Parameters.AddWithValue("@Codigo", $"%{codigo}%");
                    cmdContar.Parameters.AddWithValue("@Ciudad", ciudad ?? "");
                    cmdContar.Parameters.AddWithValue("@Radio", radio ?? "");
                    cmdContar.Parameters.AddWithValue("@Fila", fila);
                    cmdContar.Parameters.AddWithValue("@FechaInicio", fechaInicio);
                    cmdContar.Parameters.AddWithValue("@FechaFin", fechaFin);
                    var count = await cmdContar.ExecuteScalarAsync();
                }

                // Eliminar con filtro de hora + rango de fechas
                string queryEliminar = @"DELETE FROM ComercialesAsignados
                                         WHERE Codigo IN (
                                             SELECT Codigo FROM Comerciales
                                             WHERE Codigo LIKE @Codigo
                                             AND Ciudad = @Ciudad
                                             AND Radio = @Radio
                                         )
                                         AND Fila = @Fila
                                         AND Fecha IS NOT NULL
                                         AND Fecha::date >= @FechaInicio::date
                                         AND Fecha::date <= @FechaFin::date";

                using (NpgsqlCommand cmdEliminar = new NpgsqlCommand(queryEliminar, conn))
                {
                    cmdEliminar.Parameters.AddWithValue("@Codigo", $"%{codigo}%");
                    cmdEliminar.Parameters.AddWithValue("@Ciudad", ciudad ?? "");
                    cmdEliminar.Parameters.AddWithValue("@Radio", radio ?? "");
                    cmdEliminar.Parameters.AddWithValue("@Fila", fila);
                    cmdEliminar.Parameters.AddWithValue("@FechaInicio", fechaInicio);
                    cmdEliminar.Parameters.AddWithValue("@FechaFin", fechaFin);

                    totalEliminados = await cmdEliminar.ExecuteNonQueryAsync();
                }
            }

            return totalEliminados;
        }

        /// <summary>
        /// Obtiene todos los comerciales asignados para un codigo especifico (busqueda parcial).
        /// </summary>
        public async Task<List<ComercialAsignadoInfo>> ObtenerComercialesAsignadosPorCodigoAsync(string codigo)
        {
            var resultado = new List<ComercialAsignadoInfo>();

            using (NpgsqlConnection conn = new NpgsqlConnection(ConnectionString))
            {
                await conn.OpenAsync();

                string query = @"SELECT ca.Fila, ca.Columna, ca.ComercialAsignado
                                FROM ComercialesAsignados ca
                                WHERE ca.Codigo LIKE @Codigo
                                ORDER BY ca.Columna, ca.Fila";

                using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Codigo", $"%{codigo}%");

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            resultado.Add(new ComercialAsignadoInfo
                            {
                                Fila = Convert.ToInt32(reader["Fila"]),
                                Columna = Convert.ToInt32(reader["Columna"]),
                                ComercialAsignado = reader["ComercialAsignado"]?.ToString() ?? ""
                            });
                        }
                    }
                }
            }

            return resultado;
        }

        /// <summary>
        /// ULTRA OPTIMIZADO: Obtiene fechas y horas únicas en UNA sola consulta usando arrays de PostgreSQL.
        /// </summary>
        public async Task<(HashSet<DateTime> Fechas, HashSet<int> Horas)> ObtenerFechasYHorasUnicasAsync(string codigo)
        {
            var fechasUnicas = new HashSet<DateTime>();
            var horasUnicas = new HashSet<int>();

            using (var conn = new NpgsqlConnection(ConnectionString))
            {
                await conn.OpenAsync();

                // UNA sola consulta que retorna 2 arrays: fechas únicas y horas únicas
                // Esto es MUCHO más rápido que 2 consultas separadas o iterar registros
                // Usa LIKE para soportar tanto "0009" como "CU-0009"
                string query = @"
                    SELECT
                        array_agg(DISTINCT Fecha ORDER BY Fecha) FILTER (WHERE Fecha IS NOT NULL) as fechas,
                        array_agg(DISTINCT Fila ORDER BY Fila) as horas
                    FROM ComercialesAsignados
                    WHERE Codigo LIKE @Codigo";

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Codigo", $"%{codigo}%");

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            // Leer array de fechas
                            if (reader["fechas"] != DBNull.Value)
                            {
                                var fechasArray = reader["fechas"] as DateTime[];
                                if (fechasArray != null)
                                {
                                    foreach (var f in fechasArray)
                                        fechasUnicas.Add(f.Date);
                                }
                            }

                            // Leer array de horas
                            if (reader["horas"] != DBNull.Value)
                            {
                                var horasArray = reader["horas"] as int[];
                                if (horasArray != null)
                                {
                                    foreach (var h in horasArray)
                                        horasUnicas.Add(h);
                                }
                            }
                        }
                    }
                }
            }

            return (fechasUnicas, horasUnicas);
        }

        /// <summary>
        /// Obtiene las horas únicas (como strings "HH:MM") que tienen pautas para un código específico.
        /// Busca en ComercialesAsignados las filas (horas) que tienen ese código asignado.
        /// Filtra por ciudad y radio para obtener solo las horas correctas según la estación.
        /// </summary>
        public async Task<List<string>> ObtenerHorasUnicasPorCodigoAsync(string codigo, TipoTanda tipoTanda, string ciudad, string radio, string debugPath = null)
        {
            var horasUnicas = new HashSet<string>();
            string[] horarios = TandasHorarias.GetHorarios(tipoTanda);

            // Helper para escribir debug
            Action<string> writeDebug = (msg) =>
            {
                if (!string.IsNullOrEmpty(debugPath))
                {
                    try { System.IO.File.AppendAllText(debugPath, msg + Environment.NewLine); } catch { }
                }
            };

            writeDebug($"");
            writeDebug($"=== DEBUG ObtenerHorasUnicasPorCodigoAsync ===");
            writeDebug($"Codigo numerico buscado: '{codigo}'");
            writeDebug($"Ciudad: '{ciudad}', Radio: '{radio}'");
            writeDebug($"TipoTanda: {tipoTanda}, Total horarios disponibles: {horarios.Length}");

            using (NpgsqlConnection conn = new NpgsqlConnection(ConnectionString))
            {
                await conn.OpenAsync();

                // DEBUG 1: Ver TODOS los registros en ComercialesAsignados que tengan este codigo numerico
                // El codigo en BD es "CU-0001" pero recibimos solo "0001", usamos split_part para comparar
                writeDebug($"");
                writeDebug($"--- DEBUG: Buscando registros donde split_part(Codigo,'-',2) = '{codigo}' ---");
                string queryDebug1 = @"SELECT ca.Codigo, ca.Fila, ca.Columna, ca.ComercialAsignado
                                       FROM ComercialesAsignados ca
                                       WHERE split_part(ca.Codigo, '-', 2) = @CodigoNumerico";
                using (NpgsqlCommand cmd1 = new NpgsqlCommand(queryDebug1, conn))
                {
                    cmd1.Parameters.AddWithValue("@CodigoNumerico", codigo);
                    using (var reader1 = await cmd1.ExecuteReaderAsync())
                    {
                        int count = 0;
                        while (await reader1.ReadAsync())
                        {
                            count++;
                            writeDebug($"  [{count}] Codigo='{reader1["Codigo"]}', Fila={reader1["Fila"]}, Columna={reader1["Columna"]}, Comercial='{reader1["ComercialAsignado"]}'");
                        }
                        writeDebug($"  Total encontrados en ComercialesAsignados: {count}");
                    }
                }

                // DEBUG 2: Ver el registro en Comerciales para este código numerico
                writeDebug($"");
                writeDebug($"--- DEBUG: Registro en tabla Comerciales donde split_part(Codigo,'-',2) = '{codigo}': ---");
                string queryDebug2 = @"SELECT Codigo, Ciudad, Radio, FilePath FROM Comerciales WHERE split_part(Codigo, '-', 2) = @CodigoNumerico";
                using (NpgsqlCommand cmd2 = new NpgsqlCommand(queryDebug2, conn))
                {
                    cmd2.Parameters.AddWithValue("@CodigoNumerico", codigo);
                    using (var reader2 = await cmd2.ExecuteReaderAsync())
                    {
                        int count = 0;
                        while (await reader2.ReadAsync())
                        {
                            count++;
                            writeDebug($"  [{count}] Codigo='{reader2["Codigo"]}', Ciudad='{reader2["Ciudad"]}', Radio='{reader2["Radio"]}', FilePath='{reader2["FilePath"]}'");
                        }
                        if (count == 0)
                        {
                            writeDebug($"  NO ENCONTRADO en tabla Comerciales!");
                        }
                    }
                }

                // CONSULTA PRINCIPAL: Buscar filas únicas usando split_part para comparar codigo numerico
                writeDebug($"");
                writeDebug($"--- CONSULTA PRINCIPAL: Filas con filtro ciudad='{ciudad}' y radio='{radio}': ---");

                string queryPrincipal = @"SELECT DISTINCT ca.Fila
                                          FROM ComercialesAsignados ca
                                          INNER JOIN Comerciales c ON ca.Codigo = c.Codigo
                                          WHERE split_part(ca.Codigo, '-', 2) = @CodigoNumerico
                                            AND c.Ciudad = @Ciudad
                                            AND c.Radio = @Radio";

                writeDebug($"Query: {queryPrincipal}");

                using (NpgsqlCommand cmd = new NpgsqlCommand(queryPrincipal, conn))
                {
                    cmd.Parameters.AddWithValue("@CodigoNumerico", codigo);
                    cmd.Parameters.AddWithValue("@Ciudad", ciudad ?? "");
                    cmd.Parameters.AddWithValue("@Radio", radio ?? "");

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            int fila = Convert.ToInt32(reader["Fila"]);
                            writeDebug($"  Fila encontrada: {fila}");
                            if (fila >= 0 && fila < horarios.Length)
                            {
                                string hora = horarios[fila];
                                writeDebug($"    -> Hora correspondiente: {hora}");
                                horasUnicas.Add(hora);
                            }
                            else
                            {
                                writeDebug($"    -> ERROR: Fila {fila} fuera de rango (max: {horarios.Length - 1})");
                            }
                        }
                    }
                }
            }

            writeDebug($"");
            writeDebug($"=== RESULTADO FINAL: {horasUnicas.Count} horas únicas ===");
            foreach (var h in horasUnicas.OrderBy(x => x))
            {
                writeDebug($"  {h}");
            }

            // Ordenar las horas cronológicamente
            return horasUnicas.OrderBy(h => h).ToList();
        }

        /// <summary>
        /// Obtiene todos los comerciales programados para una hora (fila) y fecha especifica,
        /// filtrados por ciudad y radio, excluyendo el archivo actual (por FilePath).
        /// Busca tanto en ComercialesAsignados como directamente en Comerciales (para datos importados de Access).
        /// </summary>
        public async Task<List<(string ComercialAsignado, string Posicion, string Codigo, string FilePath)>>
            ObtenerComercialesPorHoraYFechaAsync(int fila, int columna, DateTime fecha, string ciudad, string radio, string filePathExcluir)
        {
            var resultado = new List<(string ComercialAsignado, string Posicion, string Codigo, string FilePath)>();
            var filePathsYaAgregados = new HashSet<string>(); // Para evitar duplicados

            // Detectar tipo de tanda según la radio
            TipoTanda tipoTanda = DetectarTipoProgramacionPorRadio(radio);

            // Obtener los horarios para este tipo de tanda y calcular hora/minuto de la fila
            var horarios = TandasHorarias.GetHorarios(tipoTanda);
            int hora, minuto;
            if (fila >= 0 && fila < horarios.Length)
            {
                // Parsear el horario del array (formato "HH:MM")
                string[] partes = horarios[fila].Split(':');
                hora = int.Parse(partes[0]);
                minuto = int.Parse(partes[1]);
            }
            else
            {
                // Fallback: cálculo tradicional para 48 tandas
                hora = fila / 2;
                minuto = (fila % 2) * 30;
            }
            string horaStr = $"{hora:D2}{minuto:D2}"; // Formato: "0600", "0630", etc.

            using (NpgsqlConnection conn = new NpgsqlConnection(ConnectionString))
            {
                await conn.OpenAsync();

                // 1. Primero buscar en ComercialesAsignados (para comerciales guardados con el nuevo sistema)
                int diaSemana = (int)fecha.DayOfWeek;
                int columnaParaBuscar = diaSemana == 0 ? 8 : diaSemana + 1;

                string queryAsignados = @"SELECT ca.ComercialAsignado, c.Posicion, c.Codigo, c.FilePath
                                FROM ComercialesAsignados ca
                                INNER JOIN Comerciales c ON ca.Codigo = c.Codigo
                                WHERE ca.Fila = @Fila
                                  AND ca.Columna = @Columna
                                  AND c.FechaInicio::date <= @Fecha::date
                                  AND c.FechaFinal::date >= @Fecha::date
                                  AND c.Ciudad = @Ciudad
                                  AND c.Radio = @Radio
                                  AND c.Estado = 'Activo'
                                  AND c.FilePath <> @FilePathExcluir
                                ORDER BY c.Posicion ASC";

                using (NpgsqlCommand cmd = new NpgsqlCommand(queryAsignados, conn))
                {
                    cmd.Parameters.AddWithValue("@Fila", fila);
                    cmd.Parameters.AddWithValue("@Columna", columnaParaBuscar);
                    cmd.Parameters.AddWithValue("@Fecha", fecha);
                    cmd.Parameters.AddWithValue("@Ciudad", ciudad ?? "");
                    cmd.Parameters.AddWithValue("@Radio", radio ?? "");
                    cmd.Parameters.AddWithValue("@FilePathExcluir", filePathExcluir ?? "");

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            string comercialAsignado = reader["ComercialAsignado"].ToString();
                            string posicion = reader["Posicion"].ToString();
                            string codigo = reader["Codigo"].ToString();
                            string filePath = reader["FilePath"].ToString();

                            resultado.Add((comercialAsignado, posicion, codigo, filePath));
                            filePathsYaAgregados.Add(filePath);
                        }
                    }
                }

                // 2. Buscar directamente en Comerciales (para datos importados de Access que no tienen ComercialesAsignados)
                // La hora está en el último segmento del código: ACC-550-ABA-EXI-0600
                string queryDirecto = @"SELECT DISTINCT c.Posicion, c.Codigo, c.FilePath
                                FROM Comerciales c
                                WHERE c.Codigo LIKE '%-' || @HoraStr
                                  AND c.FechaInicio::date <= @Fecha::date
                                  AND c.FechaFinal::date >= @Fecha::date
                                  AND c.Ciudad = @Ciudad
                                  AND c.Radio = @Radio
                                  AND c.Estado = 'Activo'
                                  AND c.FilePath <> @FilePathExcluir
                                ORDER BY c.Posicion ASC";

                using (NpgsqlCommand cmd = new NpgsqlCommand(queryDirecto, conn))
                {
                    cmd.Parameters.AddWithValue("@HoraStr", horaStr);
                    cmd.Parameters.AddWithValue("@Fecha", fecha);
                    cmd.Parameters.AddWithValue("@Ciudad", ciudad ?? "");
                    cmd.Parameters.AddWithValue("@Radio", radio ?? "");
                    cmd.Parameters.AddWithValue("@FilePathExcluir", filePathExcluir ?? "");

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            string filePath = reader["FilePath"].ToString();

                            // Evitar duplicados (si ya se agregó desde ComercialesAsignados)
                            if (filePathsYaAgregados.Contains(filePath))
                                continue;

                            string posicion = reader["Posicion"].ToString();
                            string codigo = reader["Codigo"].ToString();
                            string nombreArchivo = System.IO.Path.GetFileNameWithoutExtension(filePath);

                            // Crear el formato de ComercialAsignado: "P04 NombreArchivo"
                            string comercialAsignado = $"{posicion} {nombreArchivo}";

                            resultado.Add((comercialAsignado, posicion, codigo, filePath));
                            filePathsYaAgregados.Add(filePath);
                        }
                    }
                }
            }

            // Ordenar por posición
            return resultado.OrderBy(r => r.Posicion).ToList();
        }

        /// <summary>
        /// Obtiene el conteo de comerciales por cada celda (fila, columna) para un rango de fechas,
        /// ciudad y radio especificos. Util para mostrar indicador visual de ocupacion.
        /// </summary>
        public async Task<Dictionary<(int fila, int columna), int>> ObtenerConteoComercialerPorCeldaAsync(
            DateTime fechaInicio, DateTime fechaFin, string ciudad, string radio, string codigoExcluir)
        {
            var conteo = new Dictionary<(int fila, int columna), int>();

            using (NpgsqlConnection conn = new NpgsqlConnection(ConnectionString))
            {
                await conn.OpenAsync();

                string query = @"SELECT ca.Fila, ca.Columna, COUNT(*) as Cantidad
                                FROM ComercialesAsignados ca
                                INNER JOIN Comerciales c ON ca.Codigo = c.Codigo
                                WHERE c.FechaInicio::date <= @FechaFin::date
                                  AND c.FechaFinal::date >= @FechaInicio::date
                                  AND c.Ciudad = @Ciudad
                                  AND c.Radio = @Radio
                                  AND c.Estado = 'Activo'
                                  AND c.Codigo <> @CodigoExcluir
                                GROUP BY ca.Fila, ca.Columna";

                using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@FechaInicio", fechaInicio);
                    cmd.Parameters.AddWithValue("@FechaFin", fechaFin);
                    cmd.Parameters.AddWithValue("@Ciudad", ciudad ?? "");
                    cmd.Parameters.AddWithValue("@Radio", radio ?? "");
                    cmd.Parameters.AddWithValue("@CodigoExcluir", codigoExcluir ?? "");

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            int fila = Convert.ToInt32(reader["Fila"]);
                            int columna = Convert.ToInt32(reader["Columna"]);
                            int cantidad = Convert.ToInt32(reader["Cantidad"]);
                            conteo[(fila, columna)] = cantidad;
                        }
                    }
                }
            }

            return conteo;
        }

        /// <summary>
        /// Obtiene los días de la semana (como DayOfWeek) que tienen comerciales asignados para un código específico.
        /// Útil para pre-marcar los checkboxes de días antes de generar el grid.
        /// La columna en ComercialesAsignados representa: 2=Lunes, 3=Martes, ..., 8=Domingo
        /// </summary>
        public async Task<HashSet<DayOfWeek>> ObtenerDiasConComercialesAsync(string codigo)
        {
            var dias = new HashSet<DayOfWeek>();

            using (NpgsqlConnection conn = new NpgsqlConnection(ConnectionString))
            {
                await conn.OpenAsync();

                string query = "SELECT DISTINCT Columna FROM ComercialesAsignados WHERE Codigo = @Codigo";

                using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Codigo", codigo);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            int columna = Convert.ToInt32(reader["Columna"]);
                            // Convertir columna a DayOfWeek: 2=Lunes, 3=Martes, ..., 8=Domingo
                            DayOfWeek dia;
                            switch (columna)
                            {
                                case 2: dia = DayOfWeek.Monday; break;
                                case 3: dia = DayOfWeek.Tuesday; break;
                                case 4: dia = DayOfWeek.Wednesday; break;
                                case 5: dia = DayOfWeek.Thursday; break;
                                case 6: dia = DayOfWeek.Friday; break;
                                case 7: dia = DayOfWeek.Saturday; break;
                                case 8: dia = DayOfWeek.Sunday; break;
                                default: continue;
                            }
                            dias.Add(dia);
                        }
                    }
                }
            }

            return dias;
        }

        /// <summary>
        /// Verifica si existe un comercial con la misma prioridad en una celda especifica.
        /// Retorna el codigo del comercial en conflicto si existe.
        /// La columna en ComercialesAsignados representa el dia de semana: 2=Lunes, 3=Martes, ..., 8=Domingo
        /// </summary>
        public async Task<string> VerificarConflictoPrioridadAsync(
            int fila, int columna, DateTime fecha, string ciudad, string radio, string posicion, string codigoExcluir)
        {
            // Convertir el dia de la semana de la fecha a columna (2=Lunes, 3=Martes, ..., 8=Domingo)
            int diaSemana = (int)fecha.DayOfWeek;
            int columnaParaBuscar = diaSemana == 0 ? 8 : diaSemana + 1;

            using (NpgsqlConnection conn = new NpgsqlConnection(ConnectionString))
            {
                await conn.OpenAsync();

                string query = @"SELECT c.Codigo, c.FilePath
                                FROM ComercialesAsignados ca
                                INNER JOIN Comerciales c ON ca.Codigo = c.Codigo
                                WHERE ca.Fila = @Fila
                                  AND ca.Columna = @Columna
                                  AND c.FechaInicio::date <= @Fecha::date
                                  AND c.FechaFinal::date >= @Fecha::date
                                  AND c.Ciudad = @Ciudad
                                  AND c.Radio = @Radio
                                  AND c.Posicion = @Posicion
                                  AND c.Estado = 'Activo'
                                  AND c.Codigo <> @CodigoExcluir
                                LIMIT 1";

                using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Fila", fila);
                    cmd.Parameters.AddWithValue("@Columna", columnaParaBuscar); // Usar columna basada en dia de semana
                    cmd.Parameters.AddWithValue("@Fecha", fecha);
                    cmd.Parameters.AddWithValue("@Ciudad", ciudad ?? "");
                    cmd.Parameters.AddWithValue("@Radio", radio ?? "");
                    cmd.Parameters.AddWithValue("@Posicion", posicion ?? "");
                    cmd.Parameters.AddWithValue("@CodigoExcluir", codigoExcluir ?? "");

                    var result = await cmd.ExecuteScalarAsync();
                    return result?.ToString();
                }
            }
        }

        /// <summary>
        /// Detecta el tipo de tanda basandose en el nombre de la radio.
        /// </summary>
        private TipoTanda DetectarTipoProgramacionPorRadio(string radio)
        {
            if (string.IsNullOrEmpty(radio)) return TipoTanda.Tandas_00_30;

            string radioUpper = radio.ToUpper();
            if (radioUpper.Contains("KARIBEÑA") || radioUpper.Contains("KARIBENA") ||
                radioUpper.Contains("KARIBEÃ") || radioUpper.Contains("KARIBE") ||
                radioUpper.Contains("LAKALLE") || radioUpper.Contains("LA KALLE") || radioUpper.Contains("KALLE"))
            {
                return TipoTanda.Tandas_00_20_30_50;
            }
            return TipoTanda.Tandas_00_30;
        }

        /// <summary>
        /// Crea índices para mejorar el rendimiento de las consultas.
        /// Se ejecuta una vez al iniciar la aplicación.
        /// </summary>
        public static async Task CrearIndicesAsync()
        {
            try
            {
                using (var conn = new NpgsqlConnection(DatabaseConfig.ConnectionString))
                {
                    await conn.OpenAsync();

                    // Lista de índices a crear (IF NOT EXISTS evita errores si ya existen)
                    string[] indices = new string[]
                    {
                        // Índices para tabla Comerciales
                        "CREATE INDEX IF NOT EXISTS idx_comerciales_filepath ON Comerciales(FilePath)",
                        "CREATE INDEX IF NOT EXISTS idx_comerciales_ciudad_radio ON Comerciales(Ciudad, Radio)",
                        "CREATE INDEX IF NOT EXISTS idx_comerciales_fechas ON Comerciales(FechaInicio, FechaFinal)",
                        "CREATE INDEX IF NOT EXISTS idx_comerciales_estado ON Comerciales(Estado)",
                        "CREATE INDEX IF NOT EXISTS idx_comerciales_codigo ON Comerciales(Codigo)",
                        "CREATE INDEX IF NOT EXISTS idx_comerciales_posicion ON Comerciales(Posicion)",
                        "CREATE INDEX IF NOT EXISTS idx_comerciales_consulta ON Comerciales(Ciudad, Radio, Estado, FechaInicio, FechaFinal)",

                        // Índices para tabla ComercialesAsignados
                        "CREATE INDEX IF NOT EXISTS idx_asignados_codigo ON ComercialesAsignados(Codigo)",
                        "CREATE INDEX IF NOT EXISTS idx_asignados_fila_columna ON ComercialesAsignados(Fila, Columna)",
                        "CREATE INDEX IF NOT EXISTS idx_asignados_fecha ON ComercialesAsignados(Fecha)",
                        "CREATE INDEX IF NOT EXISTS idx_asignados_fila_fecha ON ComercialesAsignados(Fila, Fecha)"
                    };

                    foreach (string indexSql in indices)
                    {
                        try
                        {
                            using (var cmd = new NpgsqlCommand(indexSql, conn))
                            {
                                await cmd.ExecuteNonQueryAsync();
                            }
                        }
                        catch (Exception)
                        {
                            // Ignorar errores individuales (puede que ya exista)
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Ignorar errores
            }
        }
    }
}

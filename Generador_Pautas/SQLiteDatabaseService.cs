using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Npgsql;

namespace Generador_Pautas
{
    public class PostgreSQLDatabaseService
    {
        private readonly string ConnectionString;

        public PostgreSQLDatabaseService(string connectionString)
        {
            ConnectionString = connectionString;
        }

        /// <summary>
        /// Guarda comerciales asignados de forma optimizada usando insercion masiva
        /// </summary>
        public async Task GuardarComercialesAsignadosAsync(string codigo, List<CeldaAsignada> celdasAsignadas)
        {
            if (celdasAsignadas == null || celdasAsignadas.Count == 0)
                return;

            using (NpgsqlConnection conn = new NpgsqlConnection(ConnectionString))
            {
                await conn.OpenAsync();

                // Usar transaccion para mejor rendimiento
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        // Eliminar asignaciones anteriores
                        using (var deleteCmd = new NpgsqlCommand("DELETE FROM ComercialesAsignados WHERE Codigo = @Codigo", conn, transaction))
                        {
                            deleteCmd.Parameters.AddWithValue("@Codigo", codigo);
                            await deleteCmd.ExecuteNonQueryAsync();
                        }

                        // Insercion masiva usando COPY (mucho mas rapido)
                        if (celdasAsignadas.Count > 100)
                        {
                            await InsertarMasivoCopyAsync(conn, transaction, codigo, celdasAsignadas);
                        }
                        else
                        {
                            // Para pocas filas, usar batch insert
                            await InsertarBatchAsync(conn, transaction, codigo, celdasAsignadas);
                        }

                        await transaction.CommitAsync();
                    }
                    catch
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// Insercion masiva usando COPY de PostgreSQL (mas rapido para grandes volumenes)
        /// </summary>
        private async Task InsertarMasivoCopyAsync(NpgsqlConnection conn, NpgsqlTransaction transaction, string codigo, List<CeldaAsignada> celdasAsignadas)
        {
            using (var writer = conn.BeginBinaryImport("COPY ComercialesAsignados (Fila, Columna, ComercialAsignado, Codigo) FROM STDIN (FORMAT BINARY)"))
            {
                foreach (var celda in celdasAsignadas)
                {
                    writer.StartRow();
                    writer.Write(celda.Fila, NpgsqlTypes.NpgsqlDbType.Integer);
                    writer.Write(celda.Columna, NpgsqlTypes.NpgsqlDbType.Integer);
                    writer.Write(celda.Valor ?? "", NpgsqlTypes.NpgsqlDbType.Text);
                    writer.Write(codigo, NpgsqlTypes.NpgsqlDbType.Text);
                }
                await writer.CompleteAsync();
            }
        }

        /// <summary>
        /// Insercion por lotes para volumenes medianos
        /// </summary>
        private async Task InsertarBatchAsync(NpgsqlConnection conn, NpgsqlTransaction transaction, string codigo, List<CeldaAsignada> celdasAsignadas)
        {
            const int batchSize = 500;

            for (int i = 0; i < celdasAsignadas.Count; i += batchSize)
            {
                var batch = celdasAsignadas.GetRange(i, Math.Min(batchSize, celdasAsignadas.Count - i));

                var sb = new StringBuilder();
                sb.Append("INSERT INTO ComercialesAsignados (Fila, Columna, ComercialAsignado, Codigo) VALUES ");

                var parameters = new List<NpgsqlParameter>();

                for (int j = 0; j < batch.Count; j++)
                {
                    if (j > 0) sb.Append(",");
                    sb.Append($"(@Fila{j}, @Columna{j}, @Valor{j}, @Codigo{j})");

                    parameters.Add(new NpgsqlParameter($"@Fila{j}", batch[j].Fila));
                    parameters.Add(new NpgsqlParameter($"@Columna{j}", batch[j].Columna));
                    parameters.Add(new NpgsqlParameter($"@Valor{j}", batch[j].Valor ?? ""));
                    parameters.Add(new NpgsqlParameter($"@Codigo{j}", codigo));
                }

                using (var cmd = new NpgsqlCommand(sb.ToString(), conn, transaction))
                {
                    cmd.Parameters.AddRange(parameters.ToArray());
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        /// <summary>
        /// Guarda comerciales con reporte de progreso (para UI)
        /// </summary>
        public async Task GuardarComercialesAsignadosConProgresoAsync(string codigo, List<CeldaAsignada> celdasAsignadas, IProgress<(int porcentaje, string mensaje)> progress)
        {
            if (celdasAsignadas == null || celdasAsignadas.Count == 0)
                return;

            progress?.Report((0, "Conectando a base de datos..."));

            using (NpgsqlConnection conn = new NpgsqlConnection(ConnectionString))
            {
                await conn.OpenAsync();

                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        progress?.Report((10, "Eliminando registros anteriores..."));

                        // Eliminar asignaciones anteriores
                        using (var deleteCmd = new NpgsqlCommand("DELETE FROM ComercialesAsignados WHERE Codigo = @Codigo", conn, transaction))
                        {
                            deleteCmd.Parameters.AddWithValue("@Codigo", codigo);
                            await deleteCmd.ExecuteNonQueryAsync();
                        }

                        progress?.Report((20, $"Insertando {celdasAsignadas.Count} registros..."));

                        // Usar COPY para insercion masiva
                        await InsertarMasivoCopyConProgresoAsync(conn, codigo, celdasAsignadas, progress);

                        progress?.Report((90, "Confirmando transaccion..."));
                        await transaction.CommitAsync();

                        progress?.Report((100, "Completado"));
                    }
                    catch
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// COPY con reporte de progreso
        /// </summary>
        private async Task InsertarMasivoCopyConProgresoAsync(NpgsqlConnection conn, string codigo, List<CeldaAsignada> celdasAsignadas, IProgress<(int porcentaje, string mensaje)> progress)
        {
            int total = celdasAsignadas.Count;
            int procesados = 0;
            int ultimoReporte = 0;

            using (var writer = conn.BeginBinaryImport("COPY ComercialesAsignados (Fila, Columna, ComercialAsignado, Codigo) FROM STDIN (FORMAT BINARY)"))
            {
                foreach (var celda in celdasAsignadas)
                {
                    writer.StartRow();
                    writer.Write(celda.Fila, NpgsqlTypes.NpgsqlDbType.Integer);
                    writer.Write(celda.Columna, NpgsqlTypes.NpgsqlDbType.Integer);
                    writer.Write(celda.Valor ?? "", NpgsqlTypes.NpgsqlDbType.Text);
                    writer.Write(codigo, NpgsqlTypes.NpgsqlDbType.Text);

                    procesados++;

                    // Reportar progreso cada 5%
                    int porcentajeActual = 20 + (procesados * 70 / total);
                    if (porcentajeActual >= ultimoReporte + 5)
                    {
                        ultimoReporte = porcentajeActual;
                        progress?.Report((porcentajeActual, $"Insertando... {procesados}/{total}"));
                    }
                }
                await writer.CompleteAsync();
            }
        }

        public async Task<List<CeldaAsignada>> CargarComercialesAsignadosAsync(string codigo)
        {
            var resultado = new List<CeldaAsignada>();

            using (NpgsqlConnection conn = new NpgsqlConnection(ConnectionString))
            {
                await conn.OpenAsync();

                using (NpgsqlCommand cmd = new NpgsqlCommand(
                    "SELECT Fila, Columna, ComercialAsignado FROM ComercialesAsignados WHERE Codigo = @Codigo", conn))
                {
                    cmd.Parameters.AddWithValue("@Codigo", codigo);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            resultado.Add(new CeldaAsignada
                            {
                                Fila = Convert.ToInt32(reader["Fila"]),
                                Columna = Convert.ToInt32(reader["Columna"]),
                                Valor = reader["ComercialAsignado"].ToString()
                            });
                        }
                    }
                }
            }

            return resultado;
        }

        public async Task EliminarComercialesAsignadosAsync(string codigo)
        {
            using (NpgsqlConnection conn = new NpgsqlConnection(ConnectionString))
            {
                await conn.OpenAsync();

                using (NpgsqlCommand cmd = new NpgsqlCommand("DELETE FROM ComercialesAsignados WHERE Codigo = @Codigo", conn))
                {
                    cmd.Parameters.AddWithValue("@Codigo", codigo);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task<List<(string ComercialAsignado, string Posicion, string Codigo, string FilePath)>> ObtenerComercialesPorHoraYFechaAsync(
            int fila, DateTime fecha, string ciudad, string radio, string codigoExcluir)
        {
            var resultado = new List<(string ComercialAsignado, string Posicion, string Codigo, string FilePath)>();

            using (NpgsqlConnection conn = new NpgsqlConnection(ConnectionString))
            {
                await conn.OpenAsync();

                string query = @"SELECT ca.ComercialAsignado, c.Posicion, c.Codigo, c.FilePath
                                FROM ComercialesAsignados ca
                                INNER JOIN Comerciales c ON ca.Codigo = c.Codigo
                                WHERE ca.Fila = @Fila
                                  AND c.FechaInicio::date <= @Fecha::date
                                  AND c.FechaFinal::date >= @Fecha::date
                                  AND c.Ciudad = @Ciudad
                                  AND c.Radio = @Radio
                                  AND c.Estado = 'Activo'
                                  AND c.Codigo <> @CodigoExcluir
                                ORDER BY c.Posicion ASC";

                using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Fila", fila);
                    cmd.Parameters.AddWithValue("@Fecha", fecha);
                    cmd.Parameters.AddWithValue("@Ciudad", ciudad ?? "");
                    cmd.Parameters.AddWithValue("@Radio", radio ?? "");
                    cmd.Parameters.AddWithValue("@CodigoExcluir", codigoExcluir ?? "");

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            string comercialAsignado = reader["ComercialAsignado"].ToString();
                            string posicion = reader["Posicion"].ToString();
                            string codigoResult = reader["Codigo"].ToString();
                            string filePath = reader["FilePath"].ToString();

                            resultado.Add((comercialAsignado, posicion, codigoResult, filePath));
                        }
                    }
                }
            }

            return resultado;
        }
    }

    public class CeldaAsignada
    {
        public int Fila { get; set; }
        public int Columna { get; set; }
        public string Valor { get; set; }
    }
}

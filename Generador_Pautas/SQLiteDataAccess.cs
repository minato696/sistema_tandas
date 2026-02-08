using System;
using System.Data;
using System.Threading.Tasks;
using Npgsql;

namespace Generador_Pautas
{
    public static class PostgreSQLDataAccess
    {
        private static string _connectionString = ConfigManager.ObtenerPostgreSQLConnectionString();

        public static string ConnectionString
        {
            get => _connectionString;
            set => _connectionString = value;
        }

        public static async Task<DataTable> CargarDatosDesdeBaseDeDatosAsync(string connectionString, string tableName)
        {
            DataTable dt = new DataTable();
            using (NpgsqlConnection conn = new NpgsqlConnection(connectionString))
            {
                await conn.OpenAsync();
                using (NpgsqlCommand cmd = new NpgsqlCommand($"SELECT * FROM {tableName}", conn))
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
                        $"INSERT INTO {tableName} (Codigo, FilePath, FechaInicio, FechaFinal, Ciudad, Radio, Posicion, Estado) " +
                        $"VALUES (@Codigo, @FilePath, @FechaInicio, @FechaFinal, @Ciudad, @Radio, @Posicion, @Estado)";

                    command.Parameters.AddWithValue("@Codigo", comercialesData.Codigo);
                    command.Parameters.AddWithValue("@FilePath", filePath);
                    command.Parameters.AddWithValue("@FechaInicio", comercialesData.FechaInicio);
                    command.Parameters.AddWithValue("@FechaFinal", comercialesData.FechaFinal);
                    command.Parameters.AddWithValue("@Ciudad", comercialesData.Ciudad);
                    command.Parameters.AddWithValue("@Radio", comercialesData.Radio);
                    command.Parameters.AddWithValue("@Posicion", comercialesData.Posicion);
                    command.Parameters.AddWithValue("@Estado", comercialesData.Estado);

                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        public static async Task EliminarDatosDeBaseDeDatosAsync(string connectionString, string tableName, string codigo)
        {
            using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (NpgsqlCommand command = new NpgsqlCommand("", connection))
                {
                    command.CommandText = $"DELETE FROM {tableName} WHERE Codigo = @Codigo";
                    command.Parameters.AddWithValue("@Codigo", codigo);
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        public static async Task EliminarMultiplesDatosDeBaseDeDatosAsync(string connectionString, string tableName, string[] codigos)
        {
            using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
            {
                await connection.OpenAsync();
                foreach (string codigo in codigos)
                {
                    using (NpgsqlCommand command = new NpgsqlCommand("", connection))
                    {
                        command.CommandText = $"DELETE FROM {tableName} WHERE Codigo = @Codigo";
                        command.Parameters.AddWithValue("@Codigo", codigo);
                        await command.ExecuteNonQueryAsync();
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
        }

        public static async Task ActualizarEstadoMultiplesComercialesAsync(string connectionString, string tableName, string[] codigos, string nuevoEstado)
        {
            using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
            {
                await connection.OpenAsync();
                foreach (string codigo in codigos)
                {
                    using (NpgsqlCommand command = new NpgsqlCommand("", connection))
                    {
                        command.CommandText = $"UPDATE {tableName} SET Estado = @Estado WHERE Codigo = @Codigo";
                        command.Parameters.AddWithValue("@Estado", nuevoEstado);
                        command.Parameters.AddWithValue("@Codigo", codigo);
                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
        }

        public static async Task<bool> ExisteCodigoEnBaseDeDatosAsync(string connectionString, string tableName, string codigo)
        {
            using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (NpgsqlCommand command = new NpgsqlCommand("", connection))
                {
                    command.CommandText = $"SELECT COUNT(*) FROM {tableName} WHERE Codigo = @Codigo";
                    command.Parameters.AddWithValue("@Codigo", codigo);
                    long count = Convert.ToInt64(await command.ExecuteScalarAsync());
                    return count > 0;
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
                                         $"Ciudad = @Ciudad, Radio = @Radio, Posicion = @Posicion, Estado = @Estado WHERE Codigo = @Codigo";

                    command.Parameters.AddWithValue("@FilePath", filePath);
                    command.Parameters.AddWithValue("@FechaInicio", comercialesData.FechaInicio);
                    command.Parameters.AddWithValue("@FechaFinal", comercialesData.FechaFinal);
                    command.Parameters.AddWithValue("@Ciudad", comercialesData.Ciudad);
                    command.Parameters.AddWithValue("@Radio", comercialesData.Radio);
                    command.Parameters.AddWithValue("@Posicion", comercialesData.Posicion);
                    command.Parameters.AddWithValue("@Estado", comercialesData.Estado);
                    command.Parameters.AddWithValue("@Codigo", comercialesData.Codigo);

                    await command.ExecuteNonQueryAsync();
                }
            }
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
                    command.CommandText = $"SELECT Codigo FROM {tableName} WHERE Codigo LIKE 'CU-%' ORDER BY Codigo DESC LIMIT 1";

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            string ultimoCodigo = reader["Codigo"].ToString();
                            string numeroStr = ultimoCodigo.Replace("CU-", "");
                            if (int.TryParse(numeroStr, out int numero))
                            {
                                return numero;
                            }
                        }
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
    }
}

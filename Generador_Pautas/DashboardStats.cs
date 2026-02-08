using System;
using System.Collections.Generic;
using Npgsql;
using System.Threading.Tasks;

namespace Generador_Pautas
{
    public class DashboardStats
    {
        public int TotalComerciales { get; set; }
        public int ComercialesActivos { get; set; }
        public int ComercialesInactivos { get; set; }
        public int ComercialesPorVencer { get; set; } // Vencen en 7 dias
        public int ComercialesVencidos { get; set; }
        public List<ComercialAlerta> AlertasProximosAVencer { get; set; }
        public Dictionary<string, int> ComercialesPorRadio { get; set; }
        public Dictionary<string, int> ComercialesPorCiudad { get; set; }

        public DashboardStats()
        {
            AlertasProximosAVencer = new List<ComercialAlerta>();
            ComercialesPorRadio = new Dictionary<string, int>();
            ComercialesPorCiudad = new Dictionary<string, int>();
        }

        public static async Task<DashboardStats> CargarEstadisticasAsync(string connectionString)
        {
            var stats = new DashboardStats();
            DateTime hoy = DateTime.Today;
            DateTime enSieteDias = hoy.AddDays(7);

            using (NpgsqlConnection conn = new NpgsqlConnection(connectionString))
            {
                await conn.OpenAsync();

                // Total de comerciales
                stats.TotalComerciales = await ContarRegistrosAsync(conn, "SELECT COUNT(*) FROM Comerciales");

                // Comerciales activos
                stats.ComercialesActivos = await ContarRegistrosAsync(conn,
                    "SELECT COUNT(*) FROM Comerciales WHERE Estado = 'Activo'");

                // Comerciales inactivos
                stats.ComercialesInactivos = await ContarRegistrosAsync(conn,
                    "SELECT COUNT(*) FROM Comerciales WHERE Estado = 'Inactivo'");

                // Comerciales por vencer (en los proximos 7 dias)
                string queryPorVencer = @"SELECT COUNT(*) FROM Comerciales
                    WHERE Estado = 'Activo'
                    AND FechaFinal::date >= @Hoy::date
                    AND FechaFinal::date <= @EnSieteDias::date";
                stats.ComercialesPorVencer = await ContarRegistrosConParametrosAsync(conn, queryPorVencer,
                    new[] { ("@Hoy", hoy.ToString("yyyy-MM-dd")), ("@EnSieteDias", enSieteDias.ToString("yyyy-MM-dd")) });

                // Comerciales vencidos (fecha final < hoy y estado activo)
                string queryVencidos = @"SELECT COUNT(*) FROM Comerciales
                    WHERE Estado = 'Activo'
                    AND FechaFinal::date < @Hoy::date";
                stats.ComercialesVencidos = await ContarRegistrosConParametrosAsync(conn, queryVencidos,
                    new[] { ("@Hoy", hoy.ToString("yyyy-MM-dd")) });

                // Alertas de comerciales proximos a vencer
                stats.AlertasProximosAVencer = await ObtenerAlertasProximosAVencerAsync(conn, hoy, enSieteDias);

                // Comerciales por radio
                stats.ComercialesPorRadio = await ObtenerConteoAgrupadoAsync(conn,
                    "SELECT Radio, COUNT(*) as Total FROM Comerciales WHERE Estado = 'Activo' GROUP BY Radio");

                // Comerciales por ciudad
                stats.ComercialesPorCiudad = await ObtenerConteoAgrupadoAsync(conn,
                    "SELECT Ciudad, COUNT(*) as Total FROM Comerciales WHERE Estado = 'Activo' GROUP BY Ciudad");
            }

            return stats;
        }

        private static async Task<int> ContarRegistrosAsync(NpgsqlConnection conn, string query)
        {
            using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn))
            {
                object result = await cmd.ExecuteScalarAsync();
                return Convert.ToInt32(result);
            }
        }

        private static async Task<int> ContarRegistrosConParametrosAsync(NpgsqlConnection conn, string query,
            (string nombre, string valor)[] parametros)
        {
            using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn))
            {
                foreach (var param in parametros)
                {
                    cmd.Parameters.AddWithValue(param.nombre, param.valor);
                }
                object result = await cmd.ExecuteScalarAsync();
                return Convert.ToInt32(result);
            }
        }

        private static async Task<List<ComercialAlerta>> ObtenerAlertasProximosAVencerAsync(
            NpgsqlConnection conn, DateTime hoy, DateTime enSieteDias)
        {
            var alertas = new List<ComercialAlerta>();

            string query = @"SELECT Codigo, FilePath, FechaFinal, Ciudad, Radio
                FROM Comerciales
                WHERE Estado = 'Activo'
                AND FechaFinal::date >= @Hoy::date
                AND FechaFinal::date <= @EnSieteDias::date
                ORDER BY FechaFinal::date ASC";

            using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@Hoy", hoy.ToString("yyyy-MM-dd"));
                cmd.Parameters.AddWithValue("@EnSieteDias", enSieteDias.ToString("yyyy-MM-dd"));

                using (NpgsqlDataReader reader = (NpgsqlDataReader)await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        DateTime fechaFinal;
                        DateTime.TryParse(reader["FechaFinal"].ToString(), out fechaFinal);

                        alertas.Add(new ComercialAlerta
                        {
                            Codigo = reader["Codigo"].ToString(),
                            NombreArchivo = System.IO.Path.GetFileNameWithoutExtension(reader["FilePath"].ToString()),
                            FechaVencimiento = fechaFinal,
                            DiasRestantes = (fechaFinal - hoy).Days,
                            Ciudad = reader["Ciudad"].ToString(),
                            Radio = reader["Radio"].ToString()
                        });
                    }
                }
            }

            return alertas;
        }

        private static async Task<Dictionary<string, int>> ObtenerConteoAgrupadoAsync(NpgsqlConnection conn, string query)
        {
            var resultado = new Dictionary<string, int>();

            using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn))
            {
                using (NpgsqlDataReader reader = (NpgsqlDataReader)await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        string clave = reader.GetString(0);
                        int total = reader.GetInt32(1);
                        resultado[clave] = total;
                    }
                }
            }

            return resultado;
        }
    }

    public class ComercialAlerta
    {
        public string Codigo { get; set; }
        public string NombreArchivo { get; set; }
        public DateTime FechaVencimiento { get; set; }
        public int DiasRestantes { get; set; }
        public string Ciudad { get; set; }
        public string Radio { get; set; }

        public string MensajeAlerta
        {
            get
            {
                if (DiasRestantes == 0)
                    return $"VENCE HOY: {NombreArchivo} ({Radio})";
                else if (DiasRestantes == 1)
                    return $"Vence MANANA: {NombreArchivo} ({Radio})";
                else
                    return $"Vence en {DiasRestantes} dias: {NombreArchivo} ({Radio})";
            }
        }
    }
}

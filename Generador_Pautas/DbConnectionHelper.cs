using System;
using System.Threading.Tasks;
using Npgsql;

namespace Generador_Pautas
{
    /// <summary>
    /// Helper para conexiones a base de datos con retry automático y timeout.
    /// </summary>
    public static class DbConnectionHelper
    {
        private const int MAX_RETRIES = 3;
        private const int RETRY_DELAY_MS = 500;
        private const int CONNECTION_TIMEOUT_SECONDS = 30;

        /// <summary>
        /// Ejecuta una operación de base de datos con retry automático.
        /// </summary>
        public static async Task<T> EjecutarConRetryAsync<T>(Func<NpgsqlConnection, Task<T>> operacion)
        {
            Exception ultimaExcepcion = null;

            for (int intento = 1; intento <= MAX_RETRIES; intento++)
            {
                try
                {
                    using (var conn = new NpgsqlConnection(DatabaseConfig.ConnectionString))
                    {
                        await conn.OpenAsync();
                        return await operacion(conn);
                    }
                }
                catch (NpgsqlException ex) when (EsErrorRecuperable(ex) && intento < MAX_RETRIES)
                {
                    ultimaExcepcion = ex;
                    System.Diagnostics.Debug.WriteLine($"[DbHelper] Intento {intento}/{MAX_RETRIES} fallido: {ex.Message}");
                    await Task.Delay(RETRY_DELAY_MS * intento);
                }
                catch (TimeoutException ex) when (intento < MAX_RETRIES)
                {
                    ultimaExcepcion = ex;
                    System.Diagnostics.Debug.WriteLine($"[DbHelper] Timeout en intento {intento}/{MAX_RETRIES}");
                    await Task.Delay(RETRY_DELAY_MS * intento);
                }
            }

            throw ultimaExcepcion ?? new Exception("Error desconocido en conexión a BD");
        }

        /// <summary>
        /// Ejecuta una operación rápida (alias para compatibilidad).
        /// </summary>
        public static async Task<T> EjecutarRapidoAsync<T>(Func<NpgsqlConnection, Task<T>> operacion)
        {
            using (var conn = new NpgsqlConnection(DatabaseConfig.ConnectionString))
            {
                await conn.OpenAsync();
                return await operacion(conn);
            }
        }

        /// <summary>
        /// Ejecuta una operación void con retry automático.
        /// </summary>
        public static async Task EjecutarConRetryAsync(Func<NpgsqlConnection, Task> operacion)
        {
            await EjecutarConRetryAsync<object>(async conn =>
            {
                await operacion(conn);
                return null;
            });
        }

        /// <summary>
        /// Verifica si el error es recuperable (problemas de red temporales).
        /// </summary>
        private static bool EsErrorRecuperable(NpgsqlException ex)
        {
            string mensaje = ex.Message?.ToLower() ?? "";

            if (mensaje.Contains("connection") ||
                mensaje.Contains("timeout") ||
                mensaje.Contains("network") ||
                mensaje.Contains("socket") ||
                mensaje.Contains("broken pipe") ||
                mensaje.Contains("connection refused") ||
                mensaje.Contains("host") ||
                mensaje.Contains("server closed"))
            {
                return true;
            }

            if (ex.InnerException != null)
            {
                var innerMessage = ex.InnerException.Message?.ToLower() ?? "";
                if (innerMessage.Contains("connection") ||
                    innerMessage.Contains("timeout") ||
                    innerMessage.Contains("network") ||
                    innerMessage.Contains("socket"))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Verifica la conectividad con la base de datos.
        /// </summary>
        public static async Task<(bool exito, string mensaje, int latenciaMs)> VerificarConectividadAsync()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                using (var conn = new NpgsqlConnection(DatabaseConfig.ConnectionString))
                {
                    await conn.OpenAsync();

                    using (var cmd = new NpgsqlCommand("SELECT 1", conn))
                    {
                        await cmd.ExecuteScalarAsync();
                    }

                    sw.Stop();
                    return (true, "Conexión OK", (int)sw.ElapsedMilliseconds);
                }
            }
            catch (Exception ex)
            {
                sw.Stop();
                return (false, ex.Message, (int)sw.ElapsedMilliseconds);
            }
        }

        /// <summary>
        /// Obtiene estadísticas del pool de conexiones.
        /// </summary>
        public static string ObtenerEstadisticasPool()
        {
            return "Pool: Usando pool nativo de Npgsql";
        }

        /// <summary>
        /// Limpia las conexiones del pool.
        /// </summary>
        public static void LimpiarPool()
        {
            try
            {
                NpgsqlConnection.ClearAllPools();
                System.Diagnostics.Debug.WriteLine("[DbHelper] Pool de conexiones limpiado");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DbHelper] Error limpiando pool: {ex.Message}");
            }
        }
    }
}

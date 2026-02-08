using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace Generador_Pautas
{
    /// <summary>
    /// Servicio de caché centralizado para optimizar consultas frecuentes.
    /// </summary>
    public static class CacheService
    {
        private static readonly ConcurrentDictionary<string, CacheEntry> _cache = new ConcurrentDictionary<string, CacheEntry>();

        private const int EXPIRACION_DEFECTO = 60;
        private const int EXPIRACION_CIUDADES = 300; // 5 minutos
        private const int EXPIRACION_ESTACIONES = 300;
        private const int EXPIRACION_COMERCIALES = 60;

        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new ConcurrentDictionary<string, SemaphoreSlim>();

        private class CacheEntry
        {
            public object Data { get; set; }
            public DateTime Expiracion { get; set; }
            public bool EsValido => DateTime.Now < Expiracion;
        }

        /// <summary>
        /// Obtiene datos del caché o los carga si no existen/expiraron
        /// </summary>
        public static async Task<T> ObtenerOCargarAsync<T>(string clave, Func<Task<T>> cargarDatos, int expiracionSegundos = EXPIRACION_DEFECTO)
        {
            if (_cache.TryGetValue(clave, out CacheEntry entry) && entry.EsValido)
            {
                return (T)entry.Data;
            }

            var lockObj = _locks.GetOrAdd(clave, _ => new SemaphoreSlim(1, 1));

            await lockObj.WaitAsync();
            try
            {
                if (_cache.TryGetValue(clave, out entry) && entry.EsValido)
                {
                    return (T)entry.Data;
                }

                T datos = await cargarDatos();

                _cache[clave] = new CacheEntry
                {
                    Data = datos,
                    Expiracion = DateTime.Now.AddSeconds(expiracionSegundos)
                };

                return datos;
            }
            finally
            {
                lockObj.Release();
            }
        }

        /// <summary>
        /// Invalida una entrada específica del caché
        /// </summary>
        public static void Invalidar(string clave)
        {
            _cache.TryRemove(clave, out _);
        }

        /// <summary>
        /// Invalida todas las entradas que comienzan con el prefijo
        /// </summary>
        public static void InvalidarPorPrefijo(string prefijo)
        {
            foreach (var clave in _cache.Keys)
            {
                if (clave.StartsWith(prefijo, StringComparison.OrdinalIgnoreCase))
                {
                    _cache.TryRemove(clave, out _);
                }
            }
        }

        /// <summary>
        /// Invalida todo el caché
        /// </summary>
        public static void InvalidarTodo()
        {
            _cache.Clear();
        }

        /// <summary>
        /// Invalida caché relacionado con comerciales
        /// </summary>
        public static void InvalidarComerciales()
        {
            InvalidarPorPrefijo("comerciales_");
            InvalidarPorPrefijo("spots_");
            InvalidarPorPrefijo("agrupados_");
        }

        /// <summary>
        /// Invalida caché relacionado con asignaciones
        /// </summary>
        public static void InvalidarAsignaciones()
        {
            InvalidarPorPrefijo("asignaciones_");
            InvalidarPorPrefijo("spots_");
        }

        /// <summary>
        /// Obtiene las ciudades (con caché)
        /// </summary>
        public static async Task<List<string>> ObtenerCiudadesCacheadasAsync()
        {
            return await ObtenerOCargarAsync("ciudades_todas", async () =>
            {
                var ciudades = new List<string>();
                using (var conn = new Npgsql.NpgsqlConnection(DatabaseConfig.ConnectionString))
                {
                    await conn.OpenAsync();
                    using (var cmd = new Npgsql.NpgsqlCommand(
                        "SELECT Nombre FROM Ciudades WHERE Activa = true ORDER BY Nombre", conn))
                    {
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                ciudades.Add(reader.GetString(0));
                            }
                        }
                    }
                }
                return ciudades;
            }, EXPIRACION_CIUDADES);
        }

        /// <summary>
        /// Obtiene las estaciones/radios (con caché)
        /// </summary>
        public static async Task<List<string>> ObtenerEstacionesCacheadasAsync()
        {
            return await ObtenerOCargarAsync("estaciones_todas", async () =>
            {
                var estaciones = new List<string>();
                using (var conn = new Npgsql.NpgsqlConnection(DatabaseConfig.ConnectionString))
                {
                    await conn.OpenAsync();
                    using (var cmd = new Npgsql.NpgsqlCommand(
                        "SELECT Nombre FROM Radios WHERE Activo = true ORDER BY Nombre", conn))
                    {
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                estaciones.Add(reader.GetString(0));
                            }
                        }
                    }
                }
                return estaciones;
            }, EXPIRACION_ESTACIONES);
        }

        /// <summary>
        /// Obtiene ciudades por estación (con caché)
        /// </summary>
        public static async Task<List<string>> ObtenerCiudadesPorEstacionCacheadasAsync(string estacion)
        {
            string clave = $"ciudades_estacion_{estacion}";
            return await ObtenerOCargarAsync(clave, async () =>
            {
                var ciudades = new List<string>();
                using (var conn = new Npgsql.NpgsqlConnection(DatabaseConfig.ConnectionString))
                {
                    await conn.OpenAsync();
                    using (var cmd = new Npgsql.NpgsqlCommand(@"
                        SELECT DISTINCT c.Nombre
                        FROM Ciudades c
                        INNER JOIN RadiosCiudades rc ON c.Nombre = rc.Ciudad
                        WHERE rc.Radio = @Radio AND c.Activa = true
                        ORDER BY c.Nombre", conn))
                    {
                        cmd.Parameters.AddWithValue("@Radio", estacion);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                ciudades.Add(reader.GetString(0));
                            }
                        }
                    }
                }
                return ciudades;
            }, EXPIRACION_CIUDADES);
        }

        /// <summary>
        /// Precarga datos estáticos (ciudades, estaciones) - llamar al iniciar
        /// </summary>
        public static async Task PrecargarDatosEstaticosAsync()
        {
            try
            {
                var taskCiudades = ObtenerCiudadesCacheadasAsync();
                var taskEstaciones = ObtenerEstacionesCacheadasAsync();
                await Task.WhenAll(taskCiudades, taskEstaciones);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CacheService] Error precargando: {ex.Message}");
            }
        }

        /// <summary>
        /// Obtiene spots para una hora específica (con caché)
        /// Busca tanto en ComercialesAsignados como en comerciales ACC (Access)
        /// </summary>
        public static async Task<List<string>> ObtenerSpotsPorHoraCacheadosAsync(
            int fila, int columna, DateTime fecha, string ciudad, string radio)
        {
            string clave = $"spots_{fila}_{columna}_{fecha:yyyyMMdd}_{ciudad}_{radio}";
            return await ObtenerOCargarAsync(clave, async () =>
            {
                var spots = new List<string>();
                using (var conn = new Npgsql.NpgsqlConnection(DatabaseConfig.ConnectionString))
                {
                    await conn.OpenAsync();

                    // 1. Buscar en ComercialesAsignados (comerciales CU- pauteados normalmente)
                    using (var cmd = new Npgsql.NpgsqlCommand(@"
                        SELECT ca.ComercialAsignado, c.Posicion
                        FROM ComercialesAsignados ca
                        INNER JOIN Comerciales c ON ca.Codigo = c.Codigo
                        WHERE ca.Fila = @Fila
                          AND ca.Columna = @Columna
                          AND ca.Fecha = @Fecha
                          AND c.Estado = 'Activo'
                        ORDER BY c.Posicion
                        LIMIT 30", conn))
                    {
                        cmd.Parameters.AddWithValue("@Fila", fila);
                        cmd.Parameters.AddWithValue("@Columna", columna);
                        cmd.Parameters.AddWithValue("@Fecha", fecha.Date);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                string comercial = reader.GetString(0);
                                string posicion = reader.IsDBNull(1) ? "" : reader.GetString(1);
                                string nombre = System.IO.Path.GetFileNameWithoutExtension(comercial);
                                spots.Add($"P{posicion} {nombre}");
                            }
                        }
                    }

                    // 2. Buscar comerciales ACC (importados de Access) que tienen la hora en el código
                    // Formato: ACC-numero-ciudad-radio-HHMM donde HHMM es la hora (ej: 1430 = 14:30)
                    // Convertir fila a hora: fila 0 = 00:00, fila 1 = 00:30, etc. para tandas 00-30
                    int horaInt = fila / 2;
                    int minutos = (fila % 2) * 30;
                    string horaCodigoACC = $"{horaInt:D2}{minutos:D2}"; // Formato HHMM

                    // Normalizar ciudad y radio para el patrón de búsqueda
                    string ciudadAbrev = ciudad?.Length >= 3 ? ciudad.Substring(0, 3).ToUpper() : ciudad?.ToUpper() ?? "";
                    string radioAbrev = radio?.Length >= 3 ? radio.Substring(0, 3).ToUpper() : radio?.ToUpper() ?? "";

                    using (var cmdACC = new Npgsql.NpgsqlCommand(@"
                        SELECT FilePath, Posicion
                        FROM Comerciales
                        WHERE Codigo LIKE 'ACC-%'
                          AND Codigo LIKE @PatronHora
                          AND Ciudad = @Ciudad
                          AND Radio = @Radio
                          AND Estado = 'Activo'
                          AND FechaInicio <= @Fecha
                          AND FechaFinal >= @Fecha
                        ORDER BY Posicion
                        LIMIT 30", conn))
                    {
                        cmdACC.Parameters.AddWithValue("@PatronHora", $"%-{horaCodigoACC}");
                        cmdACC.Parameters.AddWithValue("@Ciudad", ciudad ?? "");
                        cmdACC.Parameters.AddWithValue("@Radio", radio ?? "");
                        cmdACC.Parameters.AddWithValue("@Fecha", fecha.Date);

                        using (var reader = await cmdACC.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                string filePath = reader.GetString(0);
                                string posicion = reader.IsDBNull(1) ? "" : reader.GetString(1);
                                string nombre = System.IO.Path.GetFileNameWithoutExtension(filePath);
                                string spotInfo = $"P{posicion} {nombre}";
                                if (!spots.Contains(spotInfo))
                                {
                                    spots.Add(spotInfo);
                                }
                            }
                        }
                    }
                }
                return spots;
            }, 30); // 30 segundos de caché para spots
        }

        /// <summary>
        /// Obtiene estadísticas del caché (para debug)
        /// </summary>
        public static string ObtenerEstadisticas()
        {
            int total = _cache.Count;
            int validos = 0;
            foreach (var entry in _cache.Values)
            {
                if (entry.EsValido) validos++;
            }
            return $"Caché: {validos}/{total} entradas válidas";
        }
    }
}

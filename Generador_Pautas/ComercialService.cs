using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Npgsql;

namespace Generador_Pautas
{
    /// <summary>
    /// Servicio para consultas relacionadas a comerciales.
    /// Centraliza todas las operaciones de lectura de datos de comerciales.
    /// </summary>
    public class ComercialService
    {
        private readonly string _connectionString;

        public ComercialService()
        {
            _connectionString = DatabaseConfig.ConnectionString;
        }

        public ComercialService(string connectionString)
        {
            _connectionString = connectionString;
        }

        #region Obtener Fechas

        /// <summary>
        /// Obtiene las fechas de inicio y final de un comercial.
        /// Busca por código (numérico o completo), ciudad y radio.
        /// </summary>
        public async Task<(DateTime FechaInicio, DateTime FechaFinal)?> ObtenerFechasComercialAsync(
            string codigo, string ciudad, string radio)
        {
            try
            {
                using (var conn = new NpgsqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    string codigoNumerico = ExtraerCodigoNumerico(codigo);

                    string query = @"
                        SELECT FechaInicio, FechaFinal
                        FROM Comerciales
                        WHERE (Codigo = @Codigo OR Codigo = @CodigoNumerico)
                          AND Ciudad = @Ciudad AND Radio = @Radio
                        LIMIT 1";

                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Codigo", codigo ?? "");
                        cmd.Parameters.AddWithValue("@CodigoNumerico", codigoNumerico);
                        cmd.Parameters.AddWithValue("@Ciudad", ciudad ?? "");
                        cmd.Parameters.AddWithValue("@Radio", radio ?? "");

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return (
                                    Convert.ToDateTime(reader["FechaInicio"]),
                                    Convert.ToDateTime(reader["FechaFinal"])
                                );
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ComercialService] Error en ObtenerFechasComercialAsync: {ex.Message}");
            }

            return null;
        }

        #endregion

        #region Obtener Datos Comercial

        /// <summary>
        /// Obtiene los datos completos de un comercial.
        /// Busca primero en Comerciales y si no encuentra, busca en ComercialesAsignados.
        /// </summary>
        public async Task<AgregarComercialesData> ObtenerDatosComercialAsync(
            string filePath, string ciudad, string radio, string codigoNumerico = null)
        {
            try
            {
                using (var conn = new NpgsqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    // 1. Buscar en tabla Comerciales por código numérico
                    if (!string.IsNullOrEmpty(codigoNumerico))
                    {
                        var resultado = await BuscarEnComercialesAsync(conn, codigoNumerico, ciudad, radio);
                        if (resultado != null) return resultado;
                    }

                    // 2. Buscar en ComercialesAsignados por FilePath
                    if (!string.IsNullOrEmpty(filePath))
                    {
                        var resultado = await BuscarEnComercialesAsignadosAsync(conn, filePath, ciudad, radio);
                        if (resultado != null) return resultado;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ComercialService] Error en ObtenerDatosComercialAsync: {ex.Message}");
            }

            // 3. Crear datos por defecto si tenemos información básica
            return CrearDatosPorDefecto(filePath, ciudad, radio, codigoNumerico);
        }

        private async Task<AgregarComercialesData> BuscarEnComercialesAsync(
            NpgsqlConnection conn, string codigoNumerico, string ciudad, string radio)
        {
            string query = @"
                SELECT Codigo, FilePath, FechaInicio, FechaFinal, Ciudad, Radio,
                       Posicion, Estado, TipoProgramacion
                FROM Comerciales
                WHERE split_part(Codigo, '-', 2) = @CodigoNumerico
                  AND Ciudad = @Ciudad AND Radio = @Radio
                ORDER BY Codigo
                LIMIT 1";

            using (var cmd = new NpgsqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@CodigoNumerico", codigoNumerico);
                cmd.Parameters.AddWithValue("@Ciudad", ciudad ?? "");
                cmd.Parameters.AddWithValue("@Radio", radio ?? "");

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        return MapearComercial(reader);
                    }
                }
            }

            return null;
        }

        private async Task<AgregarComercialesData> BuscarEnComercialesAsignadosAsync(
            NpgsqlConnection conn, string filePath, string ciudad, string radio)
        {
            string nombreArchivo = System.IO.Path.GetFileName(filePath);
            string nombreSinExtension = System.IO.Path.GetFileNameWithoutExtension(filePath);

            string query = @"
                SELECT DISTINCT
                    ca.Codigo,
                    ca.ComercialAsignado as FilePath,
                    MIN(ca.Fecha) as FechaInicio,
                    MAX(ca.Fecha) as FechaFinal,
                    COALESCE(c.Ciudad, @Ciudad) as Ciudad,
                    COALESCE(c.Radio, @Radio) as Radio,
                    COALESCE(c.Posicion, '1') as Posicion,
                    COALESCE(c.Estado, 'Activo') as Estado,
                    COALESCE(c.TipoProgramacion, 'Cada 15-45') as TipoProgramacion
                FROM ComercialesAsignados ca
                LEFT JOIN Comerciales c ON ca.Codigo = c.Codigo
                WHERE (LOWER(ca.ComercialAsignado) = LOWER(@FilePath)
                       OR LOWER(ca.ComercialAsignado) = LOWER(@NombreArchivo)
                       OR LOWER(ca.ComercialAsignado) = LOWER(@NombreSinExt)
                       OR LOWER(ca.ComercialAsignado) LIKE '%' || LOWER(@NombreArchivo)
                       OR LOWER(ca.ComercialAsignado) LIKE '%' || LOWER(@NombreSinExt))
                GROUP BY ca.Codigo, ca.ComercialAsignado, c.Ciudad, c.Radio,
                         c.Posicion, c.Estado, c.TipoProgramacion
                LIMIT 1";

            using (var cmd = new NpgsqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@FilePath", filePath);
                cmd.Parameters.AddWithValue("@NombreArchivo", nombreArchivo);
                cmd.Parameters.AddWithValue("@NombreSinExt", nombreSinExtension);
                cmd.Parameters.AddWithValue("@Ciudad", ciudad ?? "");
                cmd.Parameters.AddWithValue("@Radio", radio ?? "");

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        return MapearComercial(reader);
                    }
                }
            }

            return null;
        }

        private AgregarComercialesData CrearDatosPorDefecto(
            string filePath, string ciudad, string radio, string codigoNumerico)
        {
            if (string.IsNullOrEmpty(filePath) || string.IsNullOrEmpty(ciudad) || string.IsNullOrEmpty(radio))
                return null;

            string codigoCompleto = !string.IsNullOrEmpty(codigoNumerico)
                ? $"ACC-{codigoNumerico}"
                : $"ACC-{DateTime.Now.Ticks}";

            return new AgregarComercialesData
            {
                Codigo = codigoCompleto,
                FilePath = filePath,
                FechaInicio = DateTime.Today,
                FechaFinal = DateTime.Today.AddMonths(1),
                Ciudad = ciudad,
                Radio = radio,
                Posicion = "1",
                Estado = "Activo",
                TipoProgramacion = "Cada 00-30 (48 tandas)"
            };
        }

        private AgregarComercialesData MapearComercial(NpgsqlDataReader reader)
        {
            return new AgregarComercialesData
            {
                Codigo = reader["Codigo"].ToString(),
                FilePath = reader["FilePath"].ToString(),
                FechaInicio = Convert.ToDateTime(reader["FechaInicio"]),
                FechaFinal = Convert.ToDateTime(reader["FechaFinal"]),
                Ciudad = reader["Ciudad"].ToString(),
                Radio = reader["Radio"].ToString(),
                Posicion = reader["Posicion"].ToString(),
                Estado = reader["Estado"].ToString(),
                TipoProgramacion = reader["TipoProgramacion"]?.ToString() ?? "Cada 00-30 (48 tandas)"
            };
        }

        #endregion

        #region Obtener Tandas Asignadas

        /// <summary>
        /// Obtiene las tandas (horas) asignadas para un comercial.
        /// Convierte los índices de fila a formato de hora (HH:mm).
        /// </summary>
        public async Task<List<string>> ObtenerTandasAsignadasAsync(string codigo, string tipoProgramacion)
        {
            try
            {
                TipoTanda tipo = DeterminarTipoTanda(tipoProgramacion);
                var tandasHoras = new List<string>();

                if (codigo.StartsWith("ACC-", StringComparison.OrdinalIgnoreCase))
                {
                    // Comerciales Access: horas en el código
                    tandasHoras = await ObtenerHorasDeCodigosACCAsync(codigo);
                }
                else
                {
                    // Comerciales nuevos: buscar en ComercialesAsignados
                    tandasHoras = await ObtenerHorasDeComercialesAsignadosAsync(codigo, tipo);
                }

                return tandasHoras;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ComercialService] Error en ObtenerTandasAsignadasAsync: {ex.Message}");
                return new List<string>();
            }
        }

        private async Task<List<string>> ObtenerHorasDeCodigosACCAsync(string codigo)
        {
            var tandasHoras = new List<string>();
            string codigoNumerico = ExtraerCodigoNumerico(codigo);

            using (var conn = new NpgsqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                string query = @"
                    SELECT DISTINCT Codigo
                    FROM Comerciales
                    WHERE split_part(Codigo, '-', 2) = @CodigoNumerico
                    ORDER BY Codigo";

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@CodigoNumerico", codigoNumerico);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            string codigoCompleto = reader.GetString(0);
                            string hora = ExtraerHoraDeCodigoACC(codigoCompleto);

                            if (!string.IsNullOrEmpty(hora) && !tandasHoras.Contains(hora))
                            {
                                tandasHoras.Add(hora);
                            }
                        }
                    }
                }
            }

            return tandasHoras;
        }

        private async Task<List<string>> ObtenerHorasDeComercialesAsignadosAsync(string codigo, TipoTanda tipo)
        {
            var tandasHoras = new List<string>();
            var filasAsignadas = await DataAccess.ObtenerTandasAsignadasAsync(_connectionString, codigo);
            var horarios = TandasHorarias.GetHorarios(tipo);

            foreach (var filaStr in filasAsignadas)
            {
                if (int.TryParse(filaStr, out int fila) && fila >= 0 && fila < horarios.Length)
                {
                    string hora = horarios[fila];
                    if (!tandasHoras.Contains(hora))
                    {
                        tandasHoras.Add(hora);
                    }
                }
            }

            return tandasHoras;
        }

        #endregion

        #region Obtener Días Asignados

        /// <summary>
        /// Obtiene los días de la semana que tienen pautas asignadas para un comercial.
        /// Primero intenta leer de la columna DiasSeleccionados en la tabla Comerciales.
        /// Si no existe o está vacía, usa todos los días del rango como fallback.
        /// </summary>
        public async Task<List<DayOfWeek>> ObtenerDiasAsignadosAsync(
            string codigo, DateTime fechaInicio, DateTime fechaFinal)
        {
            try
            {
                // Primero intentar leer DiasSeleccionados de la tabla Comerciales
                string diasGuardados = await ObtenerDiasSeleccionadosDeComercialAsync(codigo);
                if (!string.IsNullOrEmpty(diasGuardados))
                {
                    var dias = PauteoRapidoPanel.ConvertirStringADias(diasGuardados);
                    if (dias.Count > 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"[ComercialService] Días leídos de BD: {diasGuardados} -> {string.Join(", ", dias)}");
                        return dias;
                    }
                }

                // Fallback: usar todos los días del rango de fechas
                // Esto es mejor que inferir de ComercialesAsignados porque el rango de fechas
                // podría ser corto (ej: 2 días) pero el usuario seleccionó L-D
                System.Diagnostics.Debug.WriteLine($"[ComercialService] DiasSeleccionados no disponible, usando días del rango");
                var diasUnicos = ObtenerDiasDelRango(fechaInicio, fechaFinal);

                return diasUnicos.ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ComercialService] Error en ObtenerDiasAsignadosAsync: {ex.Message}");
                return new List<DayOfWeek>();
            }
        }

        /// <summary>
        /// Lee la columna DiasSeleccionados de la tabla Comerciales
        /// </summary>
        private async Task<string> ObtenerDiasSeleccionadosDeComercialAsync(string codigo)
        {
            try
            {
                string codigoNumerico = ExtraerCodigoNumerico(codigo);

                using (var conn = new NpgsqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    string query = @"
                        SELECT DiasSeleccionados
                        FROM Comerciales
                        WHERE Codigo = @Codigo
                           OR Codigo = @CodigoNumerico
                           OR split_part(Codigo, '-', 2) = @CodigoNumerico
                        LIMIT 1";

                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Codigo", codigo);
                        cmd.Parameters.AddWithValue("@CodigoNumerico", codigoNumerico);

                        var result = await cmd.ExecuteScalarAsync();
                        return result?.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ComercialService] Error leyendo DiasSeleccionados: {ex.Message}");
                return null;
            }
        }

        private HashSet<DayOfWeek> ObtenerDiasDelRango(DateTime fechaInicio, DateTime fechaFinal)
        {
            var diasUnicos = new HashSet<DayOfWeek>();

            for (DateTime fecha = fechaInicio; fecha <= fechaFinal; fecha = fecha.AddDays(1))
            {
                diasUnicos.Add(fecha.DayOfWeek);
                if (diasUnicos.Count == 7) break;
            }

            return diasUnicos;
        }

        private async Task<HashSet<DayOfWeek>> ObtenerDiasDeComercialesAsignadosAsync(string codigo)
        {
            var diasUnicos = new HashSet<DayOfWeek>();
            string codigoNumerico = ExtraerCodigoNumerico(codigo);

            using (var conn = new NpgsqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                string query = @"
                    SELECT DISTINCT Fecha
                    FROM ComercialesAsignados
                    WHERE Codigo = @Codigo
                       OR Codigo = @CodigoNumerico
                       OR split_part(Codigo, '-', 2) = @CodigoNumerico
                    ORDER BY Fecha";

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Codigo", codigo);
                    cmd.Parameters.AddWithValue("@CodigoNumerico", codigoNumerico);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            if (!reader.IsDBNull(0))
                            {
                                diasUnicos.Add(reader.GetDateTime(0).DayOfWeek);
                            }
                        }
                    }
                }
            }

            return diasUnicos;
        }

        #endregion

        #region Métodos Auxiliares

        /// <summary>
        /// Extrae el código numérico de un código completo (ej: "ACC-42358-ABA-EXI-0015" -> "42358")
        /// </summary>
        private string ExtraerCodigoNumerico(string codigo)
        {
            if (string.IsNullOrEmpty(codigo)) return "";

            if (codigo.Contains("-"))
            {
                var partes = codigo.Split('-');
                if (partes.Length >= 2 && int.TryParse(partes[1], out _))
                {
                    return partes[1];
                }
            }

            return codigo;
        }

        /// <summary>
        /// Extrae la hora del último segmento de un código ACC (ej: "ACC-957-ABA-EXI-0015" -> "00:15")
        /// </summary>
        private string ExtraerHoraDeCodigoACC(string codigoCompleto)
        {
            var partes = codigoCompleto.Split('-');
            if (partes.Length >= 5)
            {
                string horaStr = partes[partes.Length - 1];
                if (horaStr.Length == 4 && int.TryParse(horaStr, out _))
                {
                    return $"{horaStr.Substring(0, 2)}:{horaStr.Substring(2, 2)}";
                }
            }

            return null;
        }

        /// <summary>
        /// Determina el tipo de tanda basado en la cadena de TipoProgramacion
        /// </summary>
        private TipoTanda DeterminarTipoTanda(string tipoProgramacion)
        {
            if (string.IsNullOrEmpty(tipoProgramacion))
                return TipoTanda.Tandas_00_30;

            if (tipoProgramacion.Contains("10-40")) return TipoTanda.Tandas_10_40;
            if (tipoProgramacion.Contains("15-45")) return TipoTanda.Tandas_15_45;
            if (tipoProgramacion.Contains("20-50")) return TipoTanda.Tandas_20_50;
            if (tipoProgramacion.Contains("00-20-30-50")) return TipoTanda.Tandas_00_20_30_50;

            return TipoTanda.Tandas_00_30;
        }

        #endregion
    }
}

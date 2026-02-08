using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Npgsql;
using System.IO;
using System.Threading.Tasks;
using ClosedXML.Excel;

namespace Generador_Pautas
{
    public class ReportesService
    {
        private readonly string connectionString;

        public ReportesService()
        {
            connectionString = PostgreSQLMigration.ConnectionString;
        }

        #region Reporte: Comerciales por Fecha

        /// <summary>
        /// Genera un reporte Excel de comerciales filtrados por fecha, ciudad y radio
        /// </summary>
        public async Task<string> GenerarReporteComercialesPorFechaAsync(
            DateTime fechaInicio,
            DateTime fechaFin,
            string ciudad = null,
            string radio = null,
            string rutaDestino = null)
        {
            // Reutilizar la misma logica de horarios de transmision para generar FECHA | TANDA | COMERCIAL
            var registros = await ObtenerHorariosTransmisionAsync(fechaInicio, fechaFin, ciudad, radio);

            using (var workbook = new XLWorkbook())
            {
                var ws = workbook.Worksheets.Add("Comerciales");

                // Fila 2: Titulo
                ws.Cell(2, 2).Value = "HORARIOS DE TRANSMISION";
                ws.Cell(2, 2).Style.Font.Bold = true;
                ws.Cell(2, 2).Style.Font.FontSize = 12;

                // Fila 3: Ciudad
                ws.Cell(3, 1).Value = "CIUDAD:";
                ws.Cell(3, 1).Style.Font.Bold = true;
                ws.Cell(3, 2).Value = ciudad ?? "(Todas)";

                // Fila 4: Radio
                ws.Cell(4, 1).Value = "MEDIO :";
                ws.Cell(4, 1).Style.Font.Bold = true;
                ws.Cell(4, 2).Value = radio ?? "(Todas)";

                // Fila 5: Headers
                int headerRow = 5;
                ws.Cell(headerRow, 1).Value = "FECHA";
                ws.Cell(headerRow, 2).Value = "TANDA";
                ws.Cell(headerRow, 3).Value = "MOTIVO";
                ws.Range(headerRow, 1, headerRow, 3).Style.Font.Bold = true;

                // Filas 6+: Datos
                int row = headerRow + 1;
                foreach (var reg in registros)
                {
                    ws.Cell(row, 1).Value = reg.Fecha.ToString("dd/MM/yyyy");
                    ws.Cell(row, 2).Value = reg.Hora;
                    ws.Cell(row, 3).Value = reg.NombreComercial;
                    row++;
                }

                // Ajustar columnas
                ws.Column(1).Width = 14;
                ws.Column(2).Width = 10;
                ws.Column(3).Width = 50;

                // Guardar
                if (string.IsNullOrEmpty(rutaDestino))
                {
                    string carpetaReportes = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "REPORTES");
                    Directory.CreateDirectory(carpetaReportes);
                    rutaDestino = Path.Combine(carpetaReportes,
                        string.Format("Comerciales_{0}_{1}_{2}_{3}.xlsx",
                            ciudad ?? "Todas", radio ?? "Todas",
                            fechaInicio.ToString("yyyyMMdd"), fechaFin.ToString("yyyyMMdd")));
                }

                workbook.SaveAs(rutaDestino);
                return rutaDestino;
            }
        }

        #endregion

        #region Reporte: Resumen Diario/Semanal

        /// <summary>
        /// Genera un reporte resumen con estadisticas por ciudad y radio
        /// </summary>
        public async Task<string> GenerarReporteResumenAsync(
            DateTime fecha,
            bool semanal = false,
            string rutaDestino = null)
        {
            DateTime fechaInicio = fecha;
            DateTime fechaFin = semanal ? fecha.AddDays(6) : fecha;

            var estadisticas = await ObtenerEstadisticasAsync(fechaInicio, fechaFin);

            using (var workbook = new XLWorkbook())
            {
                // Hoja 1: Resumen General
                var wsResumen = workbook.Worksheets.Add("Resumen");

                wsResumen.Cell(1, 1).Value = semanal ? "RESUMEN SEMANAL" : "RESUMEN DIARIO";
                wsResumen.Range(1, 1, 1, 4).Merge();
                wsResumen.Cell(1, 1).Style.Font.Bold = true;
                wsResumen.Cell(1, 1).Style.Font.FontSize = 16;
                wsResumen.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                wsResumen.Cell(2, 1).Value = semanal
                    ? string.Format("Semana del {0} al {1}", fechaInicio.ToString("dd/MM/yyyy"), fechaFin.ToString("dd/MM/yyyy"))
                    : string.Format("Fecha: {0}", fecha.ToString("dd/MM/yyyy"));

                wsResumen.Cell(3, 1).Value = string.Format("Generado: {0}", DateTime.Now.ToString("dd/MM/yyyy HH:mm"));

                // Estadisticas generales
                int row = 5;
                wsResumen.Cell(row, 1).Value = "ESTADISTICAS GENERALES";
                wsResumen.Cell(row, 1).Style.Font.Bold = true;
                row++;

                wsResumen.Cell(row, 1).Value = "Total de comerciales activos:";
                wsResumen.Cell(row, 2).Value = estadisticas.TotalActivos;
                row++;

                wsResumen.Cell(row, 1).Value = "Comerciales por vencer (3 dias):";
                wsResumen.Cell(row, 2).Value = estadisticas.PorVencer;
                wsResumen.Cell(row, 2).Style.Font.FontColor = XLColor.Orange;
                row++;

                wsResumen.Cell(row, 1).Value = "Comerciales vencidos:";
                wsResumen.Cell(row, 2).Value = estadisticas.Vencidos;
                wsResumen.Cell(row, 2).Style.Font.FontColor = XLColor.Red;
                row++;

                wsResumen.Cell(row, 1).Value = "Comerciales inactivos:";
                wsResumen.Cell(row, 2).Value = estadisticas.Inactivos;
                row += 2;

                // Por ciudad
                wsResumen.Cell(row, 1).Value = "POR CIUDAD";
                wsResumen.Cell(row, 1).Style.Font.Bold = true;
                row++;

                wsResumen.Cell(row, 1).Value = "Ciudad";
                wsResumen.Cell(row, 2).Value = "Activos";
                wsResumen.Cell(row, 3).Value = "Por Vencer";
                wsResumen.Cell(row, 4).Value = "Vencidos";
                wsResumen.Range(row, 1, row, 4).Style.Font.Bold = true;
                wsResumen.Range(row, 1, row, 4).Style.Fill.BackgroundColor = XLColor.FromArgb(0, 120, 215);
                wsResumen.Range(row, 1, row, 4).Style.Font.FontColor = XLColor.White;
                row++;

                foreach (var ciudad in estadisticas.PorCiudad)
                {
                    wsResumen.Cell(row, 1).Value = ciudad.Key;
                    wsResumen.Cell(row, 2).Value = ciudad.Value.Activos;
                    wsResumen.Cell(row, 3).Value = ciudad.Value.PorVencer;
                    wsResumen.Cell(row, 4).Value = ciudad.Value.Vencidos;
                    row++;
                }

                row += 2;

                // Por radio
                wsResumen.Cell(row, 1).Value = "POR RADIO";
                wsResumen.Cell(row, 1).Style.Font.Bold = true;
                row++;

                wsResumen.Cell(row, 1).Value = "Radio";
                wsResumen.Cell(row, 2).Value = "Activos";
                wsResumen.Cell(row, 3).Value = "Por Vencer";
                wsResumen.Cell(row, 4).Value = "Vencidos";
                wsResumen.Range(row, 1, row, 4).Style.Font.Bold = true;
                wsResumen.Range(row, 1, row, 4).Style.Fill.BackgroundColor = XLColor.FromArgb(0, 150, 136);
                wsResumen.Range(row, 1, row, 4).Style.Font.FontColor = XLColor.White;
                row++;

                foreach (var radio in estadisticas.PorRadio)
                {
                    wsResumen.Cell(row, 1).Value = radio.Key;
                    wsResumen.Cell(row, 2).Value = radio.Value.Activos;
                    wsResumen.Cell(row, 3).Value = radio.Value.PorVencer;
                    wsResumen.Cell(row, 4).Value = radio.Value.Vencidos;
                    row++;
                }

                wsResumen.Columns().AdjustToContents();

                // Hoja 2: Detalle de comerciales por vencer
                if (estadisticas.ListaPorVencer.Count > 0)
                {
                    var wsPorVencer = workbook.Worksheets.Add("Por Vencer");

                    wsPorVencer.Cell(1, 1).Value = "COMERCIALES POR VENCER";
                    wsPorVencer.Range(1, 1, 1, 6).Merge();
                    wsPorVencer.Cell(1, 1).Style.Font.Bold = true;
                    wsPorVencer.Cell(1, 1).Style.Font.FontSize = 14;

                    string[] headers = { "Codigo", "Archivo", "Ciudad", "Radio", "Vence", "Dias Restantes" };
                    for (int i = 0; i < headers.Length; i++)
                    {
                        wsPorVencer.Cell(3, i + 1).Value = headers[i];
                        wsPorVencer.Cell(3, i + 1).Style.Font.Bold = true;
                        wsPorVencer.Cell(3, i + 1).Style.Fill.BackgroundColor = XLColor.Orange;
                    }

                    row = 4;
                    foreach (var c in estadisticas.ListaPorVencer)
                    {
                        wsPorVencer.Cell(row, 1).Value = c.Codigo;
                        wsPorVencer.Cell(row, 2).Value = Path.GetFileName(c.FilePath);
                        wsPorVencer.Cell(row, 3).Value = c.Ciudad;
                        wsPorVencer.Cell(row, 4).Value = c.Radio;
                        wsPorVencer.Cell(row, 5).Value = c.FechaFinal.ToString("dd/MM/yyyy");
                        wsPorVencer.Cell(row, 6).Value = (c.FechaFinal - DateTime.Today).Days;
                        row++;
                    }

                    wsPorVencer.Columns().AdjustToContents();
                }

                // Guardar
                if (string.IsNullOrEmpty(rutaDestino))
                {
                    string tipoReporte = semanal ? "Semanal" : "Diario";
                    rutaDestino = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                        string.Format("Resumen{0}_{1}.xlsx", tipoReporte, fecha.ToString("yyyyMMdd")));
                }

                workbook.SaveAs(rutaDestino);
                return rutaDestino;
            }
        }

        private async Task<EstadisticasReporte> ObtenerEstadisticasAsync(DateTime fechaInicio, DateTime fechaFin)
        {
            var stats = new EstadisticasReporte();
            var hoy = DateTime.Today;
            var limitePorVencer = hoy.AddDays(3);

            using (var conn = new NpgsqlConnection(connectionString))
            {
                await conn.OpenAsync();

                string query = @"
                    SELECT Codigo, FilePath, FechaInicio, FechaFinal, Ciudad, Radio, Posicion, Estado
                    FROM Comerciales
                    ORDER BY Ciudad, Radio, FechaFinal";

                using (var cmd = new NpgsqlCommand(query, conn))
                using (var reader = (NpgsqlDataReader)await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var comercial = new ComercialReporte
                        {
                            Codigo = reader["Codigo"].ToString(),
                            FilePath = reader["FilePath"].ToString(),
                            FechaInicio = DateTime.Parse(reader["FechaInicio"].ToString()),
                            FechaFinal = DateTime.Parse(reader["FechaFinal"].ToString()),
                            Ciudad = reader["Ciudad"].ToString(),
                            Radio = reader["Radio"].ToString(),
                            Posicion = reader["Posicion"].ToString(),
                            Estado = reader["Estado"].ToString()
                        };

                        bool esActivo = comercial.Estado == "Activo";
                        bool estaVigente = comercial.FechaFinal >= hoy;
                        bool porVencer = comercial.FechaFinal <= limitePorVencer && comercial.FechaFinal >= hoy;
                        bool vencido = comercial.FechaFinal < hoy;

                        // Totales
                        if (esActivo && estaVigente && !porVencer)
                            stats.TotalActivos++;
                        else if (esActivo && porVencer)
                        {
                            stats.PorVencer++;
                            stats.ListaPorVencer.Add(comercial);
                        }
                        else if (vencido)
                            stats.Vencidos++;

                        if (!esActivo)
                            stats.Inactivos++;

                        // Por ciudad
                        if (!stats.PorCiudad.ContainsKey(comercial.Ciudad))
                            stats.PorCiudad[comercial.Ciudad] = new ContadorEstadisticas();

                        if (esActivo && estaVigente && !porVencer)
                            stats.PorCiudad[comercial.Ciudad].Activos++;
                        else if (esActivo && porVencer)
                            stats.PorCiudad[comercial.Ciudad].PorVencer++;
                        else if (vencido)
                            stats.PorCiudad[comercial.Ciudad].Vencidos++;

                        // Por radio
                        if (!stats.PorRadio.ContainsKey(comercial.Radio))
                            stats.PorRadio[comercial.Radio] = new ContadorEstadisticas();

                        if (esActivo && estaVigente && !porVencer)
                            stats.PorRadio[comercial.Radio].Activos++;
                        else if (esActivo && porVencer)
                            stats.PorRadio[comercial.Radio].PorVencer++;
                        else if (vencido)
                            stats.PorRadio[comercial.Radio].Vencidos++;
                    }
                }
            }

            return stats;
        }

        #endregion

        #region Reporte: Historial de Pautas

        /// <summary>
        /// Genera un reporte del historial de pautas generadas
        /// </summary>
        public async Task<string> GenerarReporteHistorialPautasAsync(
            DateTime fechaInicio,
            DateTime fechaFin,
            string ciudad = null,
            string radio = null,
            string rutaDestino = null)
        {
            var historial = await ObtenerHistorialPautasAsync(fechaInicio, fechaFin, ciudad, radio);

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Historial Pautas");

                // Titulo
                worksheet.Cell(1, 1).Value = "HISTORIAL DE PAUTAS";
                worksheet.Range(1, 1, 1, 7).Merge();
                worksheet.Cell(1, 1).Style.Font.Bold = true;
                worksheet.Cell(1, 1).Style.Font.FontSize = 16;
                worksheet.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                worksheet.Cell(2, 1).Value = string.Format("Periodo: {0} al {1}",
                    fechaInicio.ToString("dd/MM/yyyy"), fechaFin.ToString("dd/MM/yyyy"));
                worksheet.Cell(3, 1).Value = string.Format("Generado: {0}", DateTime.Now.ToString("dd/MM/yyyy HH:mm"));

                // Encabezados
                int headerRow = 5;
                string[] headers = { "Fecha", "Hora", "Ciudad", "Radio", "Codigo", "Comercial", "Posicion" };
                for (int i = 0; i < headers.Length; i++)
                {
                    worksheet.Cell(headerRow, i + 1).Value = headers[i];
                    worksheet.Cell(headerRow, i + 1).Style.Font.Bold = true;
                    worksheet.Cell(headerRow, i + 1).Style.Fill.BackgroundColor = XLColor.FromArgb(100, 100, 100);
                    worksheet.Cell(headerRow, i + 1).Style.Font.FontColor = XLColor.White;
                }

                // Datos
                int row = headerRow + 1;
                string ultimaFecha = "";
                string ultimaCiudad = "";

                foreach (var item in historial)
                {
                    // Separador visual por fecha/ciudad
                    string fechaActual = item.Fecha.ToString("dd/MM/yyyy");
                    if (fechaActual != ultimaFecha || item.Ciudad != ultimaCiudad)
                    {
                        if (row > headerRow + 1)
                        {
                            row++; // Espacio entre grupos
                        }
                        ultimaFecha = fechaActual;
                        ultimaCiudad = item.Ciudad;
                    }

                    worksheet.Cell(row, 1).Value = item.Fecha.ToString("dd/MM/yyyy");
                    worksheet.Cell(row, 2).Value = item.Hora;
                    worksheet.Cell(row, 3).Value = item.Ciudad;
                    worksheet.Cell(row, 4).Value = item.Radio;
                    worksheet.Cell(row, 5).Value = item.Codigo;
                    worksheet.Cell(row, 6).Value = item.NombreComercial;
                    worksheet.Cell(row, 7).Value = item.Posicion;
                    row++;
                }

                worksheet.Columns().AdjustToContents();

                // Resumen
                worksheet.Cell(row + 2, 1).Value = "Total de registros: " + historial.Count;
                worksheet.Cell(row + 2, 1).Style.Font.Bold = true;

                // Guardar
                if (string.IsNullOrEmpty(rutaDestino))
                {
                    rutaDestino = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                        string.Format("HistorialPautas_{0}_{1}.xlsx", fechaInicio.ToString("yyyyMMdd"), fechaFin.ToString("yyyyMMdd")));
                }

                workbook.SaveAs(rutaDestino);
                return rutaDestino;
            }
        }

        private async Task<List<HistorialPautaItem>> ObtenerHistorialPautasAsync(
            DateTime fechaInicio, DateTime fechaFin, string ciudad, string radio)
        {
            var lista = new List<HistorialPautaItem>();
            var horasPorFila = GenerarMapaHoras();

            using (var conn = new NpgsqlConnection(connectionString))
            {
                await conn.OpenAsync();

                string query = @"
                    SELECT ca.Fila, ca.Columna, ca.ComercialAsignado, ca.Codigo,
                           c.Ciudad, c.Radio, c.Posicion, c.FechaInicio, c.FechaFinal
                    FROM ComercialesAsignados ca
                    INNER JOIN Comerciales c ON ca.Codigo = c.Codigo
                    WHERE c.FechaInicio::date <= @FechaFin::date
                      AND c.FechaFinal::date >= @FechaInicio::date";

                if (!string.IsNullOrEmpty(ciudad))
                    query += " AND c.Ciudad = @Ciudad";
                if (!string.IsNullOrEmpty(radio))
                    query += " AND c.Radio = @Radio";

                query += " ORDER BY c.FechaInicio, c.Ciudad, c.Radio, ca.Fila";

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@FechaInicio", fechaInicio.ToString("yyyy-MM-dd"));
                    cmd.Parameters.AddWithValue("@FechaFin", fechaFin.ToString("yyyy-MM-dd"));
                    if (!string.IsNullOrEmpty(ciudad))
                        cmd.Parameters.AddWithValue("@Ciudad", ciudad);
                    if (!string.IsNullOrEmpty(radio))
                        cmd.Parameters.AddWithValue("@Radio", radio);

                    using (var reader = (NpgsqlDataReader)await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            int fila = Convert.ToInt32(reader["Fila"]);
                            string hora = horasPorFila.ContainsKey(fila) ? horasPorFila[fila] : string.Format("Fila {0}", fila);

                            lista.Add(new HistorialPautaItem
                            {
                                Fecha = DateTime.Parse(reader["FechaInicio"].ToString()),
                                Hora = hora,
                                Ciudad = reader["Ciudad"].ToString(),
                                Radio = reader["Radio"].ToString(),
                                Codigo = reader["Codigo"].ToString(),
                                NombreComercial = reader["ComercialAsignado"].ToString(),
                                Posicion = reader["Posicion"].ToString()
                            });
                        }
                    }
                }
            }

            return lista;
        }

        private Dictionary<int, string> GenerarMapaHoras()
        {
            var mapa = new Dictionary<int, string>();
            int fila = 0;
            for (int hora = 0; hora < 24; hora++)
            {
                mapa[fila] = string.Format("{0:D2}:00", hora);
                fila++;
                mapa[fila] = string.Format("{0:D2}:30", hora);
                fila++;
            }
            return mapa;
        }

        #endregion

        #region Reporte: Pauta Mensual

        /// <summary>
        /// Genera un reporte tipo grilla mensual: filas = comerciales, columnas = dias, valores = tandas/dia.
        /// Similar al formato del archivo 1.xlsx de ejemplo.
        /// </summary>
        public async Task<string> GenerarReportePautaMensualAsync(
            DateTime fechaInicio, DateTime fechaFin, string ciudad, string radio, string rutaDestino = null)
        {
            // Obtener datos de la BD
            var datosPorComercial = await ObtenerTandasPorComercialYDiaAsync(fechaInicio, fechaFin, ciudad, radio);

            // Determinar si es mes completo para el titulo
            bool esMesCompleto = fechaInicio.Day == 1 &&
                                  fechaFin.Day == DateTime.DaysInMonth(fechaFin.Year, fechaFin.Month) &&
                                  fechaInicio.Month == fechaFin.Month;

            int totalDias = (fechaFin - fechaInicio).Days + 1;

            using (var workbook = new XLWorkbook())
            {
                var ws = workbook.Worksheets.Add("Pauta");

                // Fila 1: vacia (separador)
                // Fila 2: Titulo
                string titulo = esMesCompleto
                    ? string.Format("REPORTE DE PAUTA MES {0}", fechaInicio.ToString("MMMM", new CultureInfo("es-ES")))
                    : string.Format("REPORTE DE PAUTA {0} - {1}", fechaInicio.ToString("dd/MM/yyyy"), fechaFin.ToString("dd/MM/yyyy"));

                ws.Cell(2, 2).Value = titulo.ToUpper();
                ws.Cell(2, 2).Style.Font.Bold = true;
                ws.Cell(2, 2).Style.Font.FontSize = 12;

                // Fila 3: Ciudad
                ws.Cell(3, 1).Value = "CIUDAD:";
                ws.Cell(3, 1).Style.Font.Bold = true;
                ws.Cell(3, 2).Value = ciudad;

                // Fila 4: Radio
                ws.Cell(4, 1).Value = "MEDIO :";
                ws.Cell(4, 1).Style.Font.Bold = true;
                ws.Cell(4, 2).Value = radio;

                // Fila 5: Headers - Columna A vacia, luego dias
                int headerRow = 5;
                for (int d = 0; d < totalDias; d++)
                {
                    DateTime dia = fechaInicio.AddDays(d);
                    ws.Cell(headerRow, d + 2).Value = dia.Day;
                    ws.Cell(headerRow, d + 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    ws.Cell(headerRow, d + 2).Style.Fill.BackgroundColor = XLColor.White;
                    ws.Cell(headerRow, d + 2).Style.Font.Bold = true;
                }

                // Filas 6+: Datos por comercial
                int row = headerRow + 1;
                foreach (var kvp in datosPorComercial.OrderBy(x => x.Key))
                {
                    string nombreArchivo = kvp.Key;
                    var tandasPorDia = kvp.Value; // Dictionary<DateTime, int>

                    ws.Cell(row, 1).Value = nombreArchivo;
                    ws.Cell(row, 1).Style.Font.FontSize = 9;

                    for (int d = 0; d < totalDias; d++)
                    {
                        DateTime dia = fechaInicio.AddDays(d);
                        if (tandasPorDia.ContainsKey(dia.Date))
                        {
                            int tandas = tandasPorDia[dia.Date];
                            ws.Cell(row, d + 2).Value = tandas;
                            ws.Cell(row, d + 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        }
                    }
                    row++;
                }

                // Ajustar columnas
                ws.Column(1).Width = 40;
                for (int d = 0; d < totalDias; d++)
                {
                    ws.Column(d + 2).Width = 5;
                }

                // Guardar
                if (string.IsNullOrEmpty(rutaDestino))
                {
                    string carpetaReportes = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "REPORTES");
                    Directory.CreateDirectory(carpetaReportes);
                    rutaDestino = Path.Combine(carpetaReportes,
                        string.Format("Pauta_{0}_{1}_{2}_{3}.xlsx", ciudad, radio,
                            fechaInicio.ToString("yyyyMMdd"), fechaFin.ToString("yyyyMMdd")));
                }

                workbook.SaveAs(rutaDestino);
                return rutaDestino;
            }
        }

        /// <summary>
        /// Obtiene las tandas por comercial y por dia desde la BD.
        /// Retorna: Dictionary[NombreArchivo, Dictionary[Fecha, CantidadTandas]]
        /// </summary>
        private async Task<Dictionary<string, Dictionary<DateTime, int>>> ObtenerTandasPorComercialYDiaAsync(
            DateTime fechaInicio, DateTime fechaFin, string ciudad, string radio)
        {
            var resultado = new Dictionary<string, Dictionary<DateTime, int>>();

            using (var conn = new NpgsqlConnection(connectionString))
            {
                await conn.OpenAsync();

                // 1. Comerciales con ComercialesAsignados (CU y ACC que tengan asignaciones)
                string queryAsignados = @"
                    SELECT c.FilePath, ca.Fila, ca.Columna, c.FechaInicio, c.FechaFinal
                    FROM ComercialesAsignados ca
                    INNER JOIN Comerciales c ON ca.Codigo = c.Codigo
                    WHERE c.Ciudad = @Ciudad AND c.Radio = @Radio
                      AND c.Estado = 'Activo'
                      AND c.FechaInicio::date <= @FechaFin::date
                      AND c.FechaFinal::date >= @FechaInicio::date
                    ORDER BY c.FilePath, ca.Columna, ca.Fila";

                var filePathsConAsignaciones = new HashSet<string>();

                using (var cmd = new NpgsqlCommand(queryAsignados, conn))
                {
                    cmd.Parameters.AddWithValue("@Ciudad", ciudad);
                    cmd.Parameters.AddWithValue("@Radio", radio);
                    cmd.Parameters.AddWithValue("@FechaInicio", fechaInicio.ToString("yyyy-MM-dd"));
                    cmd.Parameters.AddWithValue("@FechaFin", fechaFin.ToString("yyyy-MM-dd"));

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        // Agrupar asignaciones por FilePath + Columna (dia de semana)
                        // Para cada fecha en el rango, si el dia de semana coincide con la columna, sumar 1 tanda
                        var asignaciones = new List<(string FilePath, int Fila, int Columna, DateTime FechaIni, DateTime FechaFinal)>();
                        while (await reader.ReadAsync())
                        {
                            string filePath = reader["FilePath"].ToString();
                            filePathsConAsignaciones.Add(filePath);
                            asignaciones.Add((
                                filePath,
                                Convert.ToInt32(reader["Fila"]),
                                Convert.ToInt32(reader["Columna"]),
                                DateTime.Parse(reader["FechaInicio"].ToString()),
                                DateTime.Parse(reader["FechaFinal"].ToString())
                            ));
                        }

                        // Procesar: para cada asignacion, mapear Columna a fechas concretas
                        foreach (var asig in asignaciones)
                        {
                            string nombre = Path.GetFileNameWithoutExtension(asig.FilePath);
                            if (!resultado.ContainsKey(nombre))
                                resultado[nombre] = new Dictionary<DateTime, int>();

                            // Columna 2=Lunes, 3=Martes, ..., 8=Domingo
                            DayOfWeek diaSemana;
                            switch (asig.Columna)
                            {
                                case 2: diaSemana = DayOfWeek.Monday; break;
                                case 3: diaSemana = DayOfWeek.Tuesday; break;
                                case 4: diaSemana = DayOfWeek.Wednesday; break;
                                case 5: diaSemana = DayOfWeek.Thursday; break;
                                case 6: diaSemana = DayOfWeek.Friday; break;
                                case 7: diaSemana = DayOfWeek.Saturday; break;
                                case 8: diaSemana = DayOfWeek.Sunday; break;
                                default: continue;
                            }

                            // Buscar fechas en el rango que coincidan con este dia de semana
                            for (DateTime fecha = fechaInicio; fecha <= fechaFin; fecha = fecha.AddDays(1))
                            {
                                if (fecha.DayOfWeek == diaSemana &&
                                    fecha.Date >= asig.FechaIni.Date && fecha.Date <= asig.FechaFinal.Date)
                                {
                                    if (!resultado[nombre].ContainsKey(fecha.Date))
                                        resultado[nombre][fecha.Date] = 0;
                                    resultado[nombre][fecha.Date]++;
                                }
                            }
                        }
                    }
                }

                // 2. Comerciales ACC que NO tienen ComercialesAsignados
                // Estos tienen una fila por hora en Comerciales, cada uno aplica a todos los dias
                string queryAcc = @"
                    SELECT FilePath, COUNT(*) as TandasPorDia, FechaInicio, FechaFinal
                    FROM Comerciales
                    WHERE Ciudad = @Ciudad AND Radio = @Radio
                      AND Estado = 'Activo'
                      AND Codigo LIKE 'ACC-%'
                      AND FechaInicio::date <= @FechaFin::date
                      AND FechaFinal::date >= @FechaInicio::date
                      AND FilePath NOT IN (
                          SELECT DISTINCT c2.FilePath FROM ComercialesAsignados ca2
                          INNER JOIN Comerciales c2 ON ca2.Codigo = c2.Codigo
                          WHERE c2.Ciudad = @Ciudad AND c2.Radio = @Radio
                      )
                    GROUP BY FilePath, FechaInicio, FechaFinal";

                using (var cmd = new NpgsqlCommand(queryAcc, conn))
                {
                    cmd.Parameters.AddWithValue("@Ciudad", ciudad);
                    cmd.Parameters.AddWithValue("@Radio", radio);
                    cmd.Parameters.AddWithValue("@FechaInicio", fechaInicio.ToString("yyyy-MM-dd"));
                    cmd.Parameters.AddWithValue("@FechaFin", fechaFin.ToString("yyyy-MM-dd"));

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            string filePath = reader["FilePath"].ToString();
                            string nombre = Path.GetFileNameWithoutExtension(filePath);
                            int tandasPorDia = Convert.ToInt32(reader["TandasPorDia"]);
                            DateTime fIni = DateTime.Parse(reader["FechaInicio"].ToString());
                            DateTime fFin = DateTime.Parse(reader["FechaFinal"].ToString());

                            if (!resultado.ContainsKey(nombre))
                                resultado[nombre] = new Dictionary<DateTime, int>();

                            // Aplica a todos los dias del rango que esten dentro del rango del comercial
                            for (DateTime fecha = fechaInicio; fecha <= fechaFin; fecha = fecha.AddDays(1))
                            {
                                if (fecha.Date >= fIni.Date && fecha.Date <= fFin.Date)
                                {
                                    resultado[nombre][fecha.Date] = tandasPorDia;
                                }
                            }
                        }
                    }
                }
            }

            return resultado;
        }

        #endregion

        #region Reporte: Horarios de Transmision

        /// <summary>
        /// Genera un reporte detallado de horarios de transmision: FECHA | TANDA | MOTIVO.
        /// Similar al formato del archivo 2.xlsx de ejemplo.
        /// </summary>
        public async Task<string> GenerarReporteHorariosTransmisionAsync(
            DateTime fechaInicio, DateTime fechaFin, string ciudad, string radio, string rutaDestino = null)
        {
            var registros = await ObtenerHorariosTransmisionAsync(fechaInicio, fechaFin, ciudad, radio);

            using (var workbook = new XLWorkbook())
            {
                var ws = workbook.Worksheets.Add("AVISOS");

                // Fila 1: vacia
                // Fila 2: Titulo
                ws.Cell(2, 2).Value = "HORARIOS DE TRANSMISION";
                ws.Cell(2, 2).Style.Font.Bold = true;
                ws.Cell(2, 2).Style.Font.FontSize = 12;

                // Fila 3: Ciudad
                ws.Cell(3, 1).Value = "CIUDAD:";
                ws.Cell(3, 1).Style.Font.Bold = true;
                ws.Cell(3, 2).Value = ciudad;

                // Fila 4: Radio
                ws.Cell(4, 1).Value = "MEDIO :";
                ws.Cell(4, 1).Style.Font.Bold = true;
                ws.Cell(4, 2).Value = radio;

                // Fila 5: Headers
                int headerRow = 5;
                ws.Cell(headerRow, 1).Value = "FECHA";
                ws.Cell(headerRow, 2).Value = "TANDA";
                ws.Cell(headerRow, 3).Value = "MOTIVO";
                ws.Range(headerRow, 1, headerRow, 3).Style.Font.Bold = true;

                // Filas 6+: Datos
                int row = headerRow + 1;
                foreach (var reg in registros)
                {
                    ws.Cell(row, 1).Value = reg.Fecha.ToString("dd/MM/yyyy");
                    ws.Cell(row, 2).Value = reg.Hora;
                    ws.Cell(row, 3).Value = reg.NombreComercial;
                    row++;
                }

                // Ajustar columnas
                ws.Column(1).Width = 14;
                ws.Column(2).Width = 10;
                ws.Column(3).Width = 50;

                // Guardar
                if (string.IsNullOrEmpty(rutaDestino))
                {
                    string carpetaReportes = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "REPORTES");
                    Directory.CreateDirectory(carpetaReportes);
                    rutaDestino = Path.Combine(carpetaReportes,
                        string.Format("Horarios_{0}_{1}_{2}_{3}.xlsx", ciudad, radio,
                            fechaInicio.ToString("yyyyMMdd"), fechaFin.ToString("yyyyMMdd")));
                }

                workbook.SaveAs(rutaDestino);
                return rutaDestino;
            }
        }

        /// <summary>
        /// Obtiene la lista de transmisiones expandida: para cada fecha, cada hora, cada comercial.
        /// Ordenado por Fecha ASC, Hora ASC, NombreComercial ASC.
        /// </summary>
        private async Task<List<RegistroTransmision>> ObtenerHorariosTransmisionAsync(
            DateTime fechaInicio, DateTime fechaFin, string ciudad, string radio)
        {
            var registros = new List<RegistroTransmision>();
            var horasPorFila = GenerarMapaHoras();

            // Detectar tipo de tanda segun la radio
            TipoTanda tipoTanda = TipoTanda.Tandas_00_30;
            string radioUpper = (radio ?? "").ToUpper();
            if (radioUpper.Contains("KARIBE") || radioUpper.Contains("KALLE"))
                tipoTanda = TipoTanda.Tandas_00_20_30_50;

            string[] horarios = TandasHorarias.GetHorarios(tipoTanda);

            using (var conn = new NpgsqlConnection(connectionString))
            {
                await conn.OpenAsync();

                // 1. Comerciales con ComercialesAsignados
                string queryAsignados = @"
                    SELECT ca.Fila, ca.Columna, c.FilePath, c.FechaInicio, c.FechaFinal
                    FROM ComercialesAsignados ca
                    INNER JOIN Comerciales c ON ca.Codigo = c.Codigo
                    WHERE c.Estado = 'Activo'
                      AND c.FechaInicio::date <= @FechaFin::date
                      AND c.FechaFinal::date >= @FechaInicio::date";
                if (!string.IsNullOrEmpty(ciudad))
                    queryAsignados += " AND c.Ciudad = @Ciudad";
                if (!string.IsNullOrEmpty(radio))
                    queryAsignados += " AND c.Radio = @Radio";
                queryAsignados += " ORDER BY ca.Fila, c.FilePath";

                var filePathsConAsignaciones = new HashSet<string>();

                using (var cmd = new NpgsqlCommand(queryAsignados, conn))
                {
                    if (!string.IsNullOrEmpty(ciudad))
                        cmd.Parameters.AddWithValue("@Ciudad", ciudad);
                    if (!string.IsNullOrEmpty(radio))
                        cmd.Parameters.AddWithValue("@Radio", radio);
                    cmd.Parameters.AddWithValue("@FechaInicio", fechaInicio.ToString("yyyy-MM-dd"));
                    cmd.Parameters.AddWithValue("@FechaFin", fechaFin.ToString("yyyy-MM-dd"));

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            int fila = Convert.ToInt32(reader["Fila"]);
                            int columna = Convert.ToInt32(reader["Columna"]);
                            string filePath = reader["FilePath"].ToString();
                            DateTime fIni = DateTime.Parse(reader["FechaInicio"].ToString());
                            DateTime fFin = DateTime.Parse(reader["FechaFinal"].ToString());
                            filePathsConAsignaciones.Add(filePath);

                            string hora = fila >= 0 && fila < horarios.Length
                                ? horarios[fila]
                                : (horasPorFila.ContainsKey(fila) ? horasPorFila[fila] : string.Format("Fila {0}", fila));

                            string nombre = Path.GetFileNameWithoutExtension(filePath);

                            // Columna a DayOfWeek
                            DayOfWeek diaSemana;
                            switch (columna)
                            {
                                case 2: diaSemana = DayOfWeek.Monday; break;
                                case 3: diaSemana = DayOfWeek.Tuesday; break;
                                case 4: diaSemana = DayOfWeek.Wednesday; break;
                                case 5: diaSemana = DayOfWeek.Thursday; break;
                                case 6: diaSemana = DayOfWeek.Friday; break;
                                case 7: diaSemana = DayOfWeek.Saturday; break;
                                case 8: diaSemana = DayOfWeek.Sunday; break;
                                default: continue;
                            }

                            // Expandir a fechas concretas
                            for (DateTime fecha = fechaInicio; fecha <= fechaFin; fecha = fecha.AddDays(1))
                            {
                                if (fecha.DayOfWeek == diaSemana &&
                                    fecha.Date >= fIni.Date && fecha.Date <= fFin.Date)
                                {
                                    registros.Add(new RegistroTransmision
                                    {
                                        Fecha = fecha.Date,
                                        Hora = hora,
                                        NombreComercial = nombre
                                    });
                                }
                            }
                        }
                    }
                }

                // 2. Comerciales ACC sin ComercialesAsignados
                string queryAcc = @"
                    SELECT Codigo, FilePath, FechaInicio, FechaFinal
                    FROM Comerciales
                    WHERE Estado = 'Activo'
                      AND Codigo LIKE 'ACC-%'
                      AND FechaInicio::date <= @FechaFin::date
                      AND FechaFinal::date >= @FechaInicio::date";
                if (!string.IsNullOrEmpty(ciudad))
                    queryAcc += " AND Ciudad = @Ciudad";
                if (!string.IsNullOrEmpty(radio))
                    queryAcc += " AND Radio = @Radio";

                using (var cmd = new NpgsqlCommand(queryAcc, conn))
                {
                    if (!string.IsNullOrEmpty(ciudad))
                        cmd.Parameters.AddWithValue("@Ciudad", ciudad);
                    if (!string.IsNullOrEmpty(radio))
                        cmd.Parameters.AddWithValue("@Radio", radio);
                    cmd.Parameters.AddWithValue("@FechaInicio", fechaInicio.ToString("yyyy-MM-dd"));
                    cmd.Parameters.AddWithValue("@FechaFin", fechaFin.ToString("yyyy-MM-dd"));

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            string codigo = reader["Codigo"].ToString();
                            string filePath = reader["FilePath"].ToString();
                            DateTime fIni = DateTime.Parse(reader["FechaInicio"].ToString());
                            DateTime fFin = DateTime.Parse(reader["FechaFinal"].ToString());

                            // Si ya tiene asignaciones, no duplicar
                            if (filePathsConAsignaciones.Contains(filePath)) continue;

                            string nombre = Path.GetFileNameWithoutExtension(filePath);

                            // Extraer hora del codigo ACC-550-ABA-EXI-0600
                            string[] partes = codigo.Split('-');
                            if (partes.Length >= 5)
                            {
                                string horaStr = partes[partes.Length - 1];
                                if (horaStr.Length == 4)
                                {
                                    string hora = horaStr.Substring(0, 2) + ":" + horaStr.Substring(2, 2);

                                    for (DateTime fecha = fechaInicio; fecha <= fechaFin; fecha = fecha.AddDays(1))
                                    {
                                        if (fecha.Date >= fIni.Date && fecha.Date <= fFin.Date)
                                        {
                                            registros.Add(new RegistroTransmision
                                            {
                                                Fecha = fecha.Date,
                                                Hora = hora,
                                                NombreComercial = nombre
                                            });
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // Ordenar por fecha, hora, nombre
            return registros
                .OrderBy(r => r.Fecha)
                .ThenBy(r => r.Hora)
                .ThenBy(r => r.NombreComercial)
                .ToList();
        }

        #endregion

        #region Clases auxiliares

        public class RegistroTransmision
        {
            public DateTime Fecha { get; set; }
            public string Hora { get; set; }
            public string NombreComercial { get; set; }
        }

        public class ComercialReporte
        {
            public string Codigo { get; set; }
            public string FilePath { get; set; }
            public DateTime FechaInicio { get; set; }
            public DateTime FechaFinal { get; set; }
            public string Ciudad { get; set; }
            public string Radio { get; set; }
            public string Posicion { get; set; }
            public string Estado { get; set; }
            public string TipoProgramacion { get; set; }
        }

        public class EstadisticasReporte
        {
            public int TotalActivos { get; set; }
            public int PorVencer { get; set; }
            public int Vencidos { get; set; }
            public int Inactivos { get; set; }
            public Dictionary<string, ContadorEstadisticas> PorCiudad { get; set; } = new Dictionary<string, ContadorEstadisticas>();
            public Dictionary<string, ContadorEstadisticas> PorRadio { get; set; } = new Dictionary<string, ContadorEstadisticas>();
            public List<ComercialReporte> ListaPorVencer { get; set; } = new List<ComercialReporte>();
        }

        public class ContadorEstadisticas
        {
            public int Activos { get; set; }
            public int PorVencer { get; set; }
            public int Vencidos { get; set; }
        }

        public class HistorialPautaItem
        {
            public DateTime Fecha { get; set; }
            public string Hora { get; set; }
            public string Ciudad { get; set; }
            public string Radio { get; set; }
            public string Codigo { get; set; }
            public string NombreComercial { get; set; }
            public string Posicion { get; set; }
        }

        #endregion
    }
}

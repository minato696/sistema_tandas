using System;
using System.Collections.Generic;
using Npgsql;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Generador_Pautas
{
    public class GenerarPauta
    {
        private HashSet<string> entradasAgregadas = new HashSet<string>();

        /// <summary>
        /// Clase para almacenar datos extraidos del DataGridView
        /// </summary>
        public class DatosColumna
        {
            public int ColumnaIndex { get; set; }
            public DateTime Fecha { get; set; }
            public string NombreArchivo { get; set; }
            public List<(int Fila, string Horario)> Filas { get; set; } = new List<(int, string)>();
            // Nuevo: almacenar los valores de las celdas con comerciales
            public Dictionary<int, string> CeldasConComerciales { get; set; } = new Dictionary<int, string>();
        }

        /// <summary>
        /// Extrae los datos necesarios del DataGridView (debe llamarse desde el hilo de UI)
        /// Incluye los valores de las celdas que tienen comerciales asignados.
        /// </summary>
        public List<DatosColumna> ExtraerDatosDelGrid(DataGridView dataGridView, string ciudad, string radio)
        {
            var datosColumnas = new List<DatosColumna>();

            // Iterar sobre cada columna de fecha (desde la columna 2 en adelante)
            for (int col = 2; col < dataGridView.Columns.Count; col++)
            {
                string headerText = dataGridView.Columns[col].HeaderText;
                DateTime fechaColumna = ExtraerFechaDeHeader(headerText);
                string nombreArchivo = $"{fechaColumna:dd-MM-yy}{ciudad}{radio}.txt";

                var datosCol = new DatosColumna
                {
                    ColumnaIndex = col,
                    Fecha = fechaColumna,
                    NombreArchivo = nombreArchivo
                };

                // Recorrer todas las filas (horarios) y capturar valores de celdas con comerciales
                foreach (DataGridViewRow row in dataGridView.Rows)
                {
                    if (row.IsNewRow) continue;
                    string horario = row.Cells[1].Value?.ToString();
                    if (!string.IsNullOrEmpty(horario))
                    {
                        datosCol.Filas.Add((row.Index, horario));

                        // IMPORTANTE: Capturar el valor de la celda si tiene un comercial asignado
                        string valorCelda = row.Cells[col].Value?.ToString();
                        if (!string.IsNullOrEmpty(valorCelda))
                        {
                            datosCol.CeldasConComerciales[row.Index] = valorCelda;
                        }
                    }
                }

                datosColumnas.Add(datosCol);
            }

            return datosColumnas;
        }

        /// <summary>
        /// Genera los archivos de pauta consultando TODOS los comerciales de la BD
        /// que coincidan con la hora, fecha, ciudad y radio especificados.
        /// </summary>
        public async Task<string> GenerarPautaArchivoAsync(DataGridView dataGridView, string ciudad, string radio, string tipoProgramacion, IProgress<int> progress = null)
        {
            System.Diagnostics.Debug.WriteLine($"[PAUTA] Generando: {ciudad}/{radio}, {dataGridView.Columns.Count - 2} días");

            // Extraer datos del DataGridView en el hilo de UI
            var datosColumnas = ExtraerDatosDelGrid(dataGridView, ciudad, radio);

            // Contar cuántas celdas tienen comerciales asignados
            int totalCeldasConComerciales = datosColumnas.Sum(d => d.CeldasConComerciales.Count);


            // Ejecutar el procesamiento en un hilo de fondo
            return await Task.Run(async () =>
            {
                return await ProcesarPautasAsync(datosColumnas, ciudad, radio, progress);
            });
        }

        /// <summary>
        /// Genera los archivos de pauta SOLO para las columnas (fechas) especificadas.
        /// Usado cuando se guarda un comercial para regenerar solo las pautas de las fechas modificadas.
        /// </summary>
        /// <param name="filePathActual">FilePath del registro actual para excluirlo de BD y usar datos del grid</param>
        public async Task<string> GenerarPautaParaColumnasAsync(DataGridView dataGridView, string ciudad, string radio, HashSet<int> columnasModificadas, IProgress<int> progress = null, string filePathActual = null)
        {
            if (columnasModificadas == null || columnasModificadas.Count == 0)
            {
                return "|No hay fechas modificadas para generar pautas.";
            }

            // Extraer datos solo de las columnas modificadas
            var datosColumnas = ExtraerDatosDelGridPorColumnas(dataGridView, ciudad, radio, columnasModificadas);

            // Ejecutar el procesamiento en un hilo de fondo
            return await Task.Run(async () =>
            {
                return await ProcesarPautasAsync(datosColumnas, ciudad, radio, progress, filePathActual);
            });
        }

        /// <summary>
        /// Extrae los datos del DataGridView solo para las columnas especificadas.
        /// Incluye los valores de las celdas que tienen comerciales asignados.
        /// </summary>
        public List<DatosColumna> ExtraerDatosDelGridPorColumnas(DataGridView dataGridView, string ciudad, string radio, HashSet<int> columnas)
        {
            var datosColumnas = new List<DatosColumna>();

            foreach (int col in columnas.OrderBy(c => c))
            {
                if (col < 2 || col >= dataGridView.Columns.Count) continue;

                string headerText = dataGridView.Columns[col].HeaderText;
                DateTime fechaColumna = ExtraerFechaDeHeader(headerText);
                string nombreArchivo = $"{fechaColumna:dd-MM-yy}{ciudad}{radio}.txt";

                var datosCol = new DatosColumna
                {
                    ColumnaIndex = col,
                    Fecha = fechaColumna,
                    NombreArchivo = nombreArchivo
                };

                // Recorrer todas las filas (horarios) y capturar valores de celdas con comerciales
                foreach (DataGridViewRow row in dataGridView.Rows)
                {
                    if (row.IsNewRow) continue;
                    string horario = row.Cells[1].Value?.ToString();
                    if (!string.IsNullOrEmpty(horario))
                    {
                        datosCol.Filas.Add((row.Index, horario));

                        // Capturar el valor de la celda si tiene un comercial asignado
                        string valorCelda = row.Cells[col].Value?.ToString();
                        if (!string.IsNullOrEmpty(valorCelda))
                        {
                            datosCol.CeldasConComerciales[row.Index] = valorCelda;
                        }
                    }
                }

                datosColumnas.Add(datosCol);
            }

            return datosColumnas;
        }

        /// <summary>
        /// Procesa las pautas usando los datos ya extraidos (puede ejecutarse en hilo de fondo)
        /// Genera el archivo con TODOS los comerciales de la BD para esa fecha/ciudad/radio
        /// OPTIMIZADO: Para muchas fechas, obtiene todos los datos en una sola consulta
        /// </summary>
        private async Task<string> ProcesarPautasAsync(List<DatosColumna> datosColumnas, string ciudad, string radio, IProgress<int> progress, string filePathActual = null)
        {
            string carpetaBase = ConfigManager.ObtenerCarpetaRadio(radio);
            List<string> archivosGenerados = new List<string>();
            int totalColumnas = datosColumnas.Count;
            int columnaProcesada = 0;

            TipoTanda tipoTandaActual = await ObtenerTipoTandaDesdeBDAsync(ciudad, radio);
            var horarios = TandasHorarias.GetHorarios(tipoTandaActual);

            Dictionary<string, List<(string NombreArchivo, string Posicion, string Hora)>> comercialesPorFecha = null;

            // SIEMPRE usar caché para garantizar que se procesen TODAS las horas
            // (incluyendo comerciales con TipoProgramacion diferente al de la radio)
            if (totalColumnas > 0)
            {
                DateTime fechaInicio = datosColumnas.Min(d => d.Fecha);
                DateTime fechaFin = datosColumnas.Max(d => d.Fecha);
                comercialesPorFecha = await ObtenerComercialesParaRangoFechasAsync(
                    fechaInicio, fechaFin, ciudad, radio, tipoTandaActual, horarios);
                progress?.Report(5);
            }

            // Crear directorio una sola vez
            string rutaBasePautas = ConfigManager.ObtenerRutaBasePautas();
            string directorioPautas = System.IO.Path.Combine(rutaBasePautas, ciudad.ToUpper());
            if (!System.IO.Directory.Exists(directorioPautas))
            {
                System.IO.Directory.CreateDirectory(directorioPautas);
            }

            string separator = new string('-', 80);

            foreach (var datosCol in datosColumnas)
            {
                StringBuilder sb = new StringBuilder();
                string horarioAnterior = "";

                // Determinar qué horas procesar:
                // - Si tenemos caché, usar TODAS las horas que aparecen en él (incluye diferentes TipoProgramacion)
                // - Si no hay caché, usar los horarios del TipoTanda de la radio
                string claveFecha = datosCol.Fecha.ToString("yyyy-MM-dd");
                List<string> horasAProcesar;

                if (comercialesPorFecha != null && comercialesPorFecha.ContainsKey(claveFecha))
                {
                    // Obtener todas las horas únicas del caché para esta fecha
                    horasAProcesar = comercialesPorFecha[claveFecha]
                        .Select(c => c.Hora)
                        .Distinct()
                        .OrderBy(h => h)
                        .ToList();

                    // También incluir las horas del grid que podrían no estar en el caché
                    foreach (var (filaG, horarioG) in datosCol.Filas)
                    {
                        if (!horasAProcesar.Contains(horarioG))
                        {
                            horasAProcesar.Add(horarioG);
                        }
                    }
                    horasAProcesar = horasAProcesar.OrderBy(h => h).ToList();
                }
                else
                {
                    // Sin caché, usar los horarios del tipo de tanda de la radio
                    horasAProcesar = horarios.ToList();
                }

                foreach (string horario in horasAProcesar)
                {
                    List<(string NombreArchivo, string Posicion)> comercialesBD;

                    // Usar caché si está disponible, sino consultar individual
                    if (comercialesPorFecha != null)
                    {
                        comercialesBD = comercialesPorFecha.ContainsKey(claveFecha)
                            ? comercialesPorFecha[claveFecha]
                                .Where(c => c.Hora == horario)
                                .Select(c => (c.NombreArchivo, c.Posicion))
                                .ToList()
                            : new List<(string, string)>();
                    }
                    else
                    {
                        // Buscar comerciales por hora directamente (string "HH:MM")
                        comercialesBD = await ObtenerComercialesPorHoraStringAsync(
                            horario, 0, datosCol.Fecha, ciudad, radio, filePathActual);
                    }

                    // Combinar con el valor del grid actual
                    // NOTA: El grid puede tener diferente número de filas (48) que los horarios (96)
                    // Debemos buscar por HORA, no por fila. Buscar si hay alguna fila del grid con este horario
                    int? filaGrid = null;
                    foreach (var (filaG, horarioG) in datosCol.Filas)
                    {
                        if (horarioG == horario)
                        {
                            filaGrid = filaG;
                            break;
                        }
                    }

                    if (filaGrid.HasValue && datosCol.CeldasConComerciales.TryGetValue(filaGrid.Value, out string valorCeldaGrid) && !string.IsNullOrEmpty(valorCeldaGrid))
                    {
                        string nombreComercialGrid = ExtraerNombreComercial(valorCeldaGrid);
                        bool yaExiste = comercialesBD.Any(c =>
                            c.NombreArchivo.Equals(nombreComercialGrid, StringComparison.OrdinalIgnoreCase));

                        if (!yaExiste)
                        {
                            string claveSecuenciaGrid = ExtraerClaveSecuencia(nombreComercialGrid);
                            if (claveSecuenciaGrid != null)
                            {
                                yaExiste = comercialesBD.Any(c =>
                                {
                                    string claveBD = ExtraerClaveSecuencia(c.NombreArchivo);
                                    return claveBD != null && claveBD.Equals(claveSecuenciaGrid, StringComparison.OrdinalIgnoreCase);
                                });
                            }
                        }

                        if (!yaExiste)
                        {
                            string posicion = "01"; // Posición por defecto para comerciales del grid
                            if (valorCeldaGrid.StartsWith("P") && valorCeldaGrid.Length > 3)
                            {
                                int espacio = valorCeldaGrid.IndexOf(' ');
                                if (espacio > 0)
                                    posicion = valorCeldaGrid.Substring(1, espacio - 1);
                            }
                            comercialesBD.Add((nombreComercialGrid, posicion));
                        }
                    }
                    // Si no hay comerciales de BD ni del grid para esta celda, verificar si hay en el diccionario
                    else if (comercialesBD.Count == 0 && filaGrid.HasValue && datosCol.CeldasConComerciales.ContainsKey(filaGrid.Value))
                    {
                        // Forzar obtener del grid aunque esté vacío en TryGetValue
                        string valorForzado = datosCol.CeldasConComerciales[filaGrid.Value];
                        if (!string.IsNullOrEmpty(valorForzado))
                        {
                            string nombreComercial = ExtraerNombreComercial(valorForzado);
                            string posicion = "01";
                            if (valorForzado.StartsWith("P") && valorForzado.Length > 3)
                            {
                                int espacio = valorForzado.IndexOf(' ');
                                if (espacio > 0)
                                    posicion = valorForzado.Substring(1, espacio - 1);
                            }
                            comercialesBD.Add((nombreComercial, posicion));
                        }
                    }

                    var comercialesOrdenados = comercialesBD.OrderBy(c => ParsearPosicion(c.Posicion)).ToList();

                    foreach (var (nombreArchivo, posicion) in comercialesOrdenados)
                    {
                        if (horario != horarioAnterior)
                        {
                            sb.AppendLine(separator);
                            horarioAnterior = horario;
                        }
                        sb.AppendLine($"{horario}|{carpetaBase}{nombreArchivo}.mp3");
                    }
                }

                // Guardar archivo
                string rutaCompleta = System.IO.Path.Combine(directorioPautas, datosCol.NombreArchivo);

                try
                {
                    if (sb.Length > 0)
                    {
                        System.IO.File.WriteAllText(rutaCompleta, sb.ToString().TrimEnd('\r', '\n'));
                        archivosGenerados.Add(rutaCompleta);
                    }
                    else if (System.IO.File.Exists(rutaCompleta))
                    {
                        System.IO.File.Delete(rutaCompleta);
                    }
                }
                catch (Exception exArchivo)
                {
                    System.Diagnostics.Debug.WriteLine($"[PROCESAR] ERROR guardando {datosCol.NombreArchivo}: {exArchivo.Message}");
                }

                // Reportar progreso (5-100%)
                columnaProcesada++;
                if (progress != null && totalColumnas > 0)
                {
                    int porcentaje = 5 + (int)((columnaProcesada * 95.0) / totalColumnas);
                    progress.Report(porcentaje);
                }
            }

            System.Diagnostics.Debug.WriteLine($"[PROCESAR] FIN - Se generaron {archivosGenerados.Count} archivos de {totalColumnas} días");

            string carpetaCiudad = System.IO.Path.Combine(rutaBasePautas, ciudad.ToUpper(), radio.ToUpper());
            return $"{carpetaCiudad}|Se generaron {archivosGenerados.Count} archivos de pautas correctamente.";
        }

        /// <summary>
        /// Obtiene TODOS los comerciales para un rango de fechas en UNA sola consulta.
        /// Retorna un diccionario organizado por fecha para acceso rápido.
        /// IMPORTANTE: Cada comercial puede tener diferente TipoProgramacion, por lo que
        /// la conversión fila->hora se hace usando el TipoProgramacion específico de cada uno.
        /// </summary>
        private async Task<Dictionary<string, List<(string NombreArchivo, string Posicion, string Hora)>>> ObtenerComercialesParaRangoFechasAsync(
            DateTime fechaInicio, DateTime fechaFin, string ciudad, string radio, TipoTanda tipoTandaPorDefecto, string[] horariosPorDefecto)
        {
            var resultado = new Dictionary<string, List<(string NombreArchivo, string Posicion, string Hora)>>();

            // Reset contador de logs de conversión
            _logConversionCount = 0;

            try
            {
                using (var conn = new NpgsqlConnection(DatabaseConfig.ConnectionString))
                {
                    await conn.OpenAsync();

                    // Consulta optimizada: obtener TODOS los comerciales ASIGNADOS en el rango
                    // IMPORTANTE: Incluir TipoProgramacion para convertir fila->hora correctamente
                    string query = @"
                        SELECT c.FilePath, c.Posicion, ca.Fila, ca.Fecha, ca.Columna, c.FechaInicio, c.FechaFinal,
                               COALESCE(c.TipoProgramacion, 'Cada 00-30') as TipoProgramacion
                        FROM ComercialesAsignados ca
                        INNER JOIN Comerciales c ON ca.Codigo = c.Codigo
                        WHERE c.FechaInicio::date <= @FechaFin::date
                          AND c.FechaFinal::date >= @FechaInicio::date
                          AND LOWER(c.Ciudad) = LOWER(@Ciudad)
                          AND LOWER(c.Radio) = LOWER(@Radio)
                          AND c.Estado = 'Activo'
                        ORDER BY ca.Fecha, ca.Fila, c.Posicion ASC";

                    // Estructura: FilePath, Posicion, Fila, Fecha (puede ser null), Columna, FechaInicio, FechaFinal, TipoProgramacion
                    var comercialesAsignados = new List<(string FilePath, string Posicion, int Fila, DateTime? FechaAsignacion, int Columna, DateTime FechaInicio, DateTime FechaFinal, string TipoProgramacion)>();

                    Logger.Log($"[PAUTA-BATCH] Consultando comerciales para rango {fechaInicio:dd/MM/yyyy} - {fechaFin:dd/MM/yyyy}, ciudad={ciudad}, radio={radio}");

                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@FechaInicio", fechaInicio.Date);
                        cmd.Parameters.AddWithValue("@FechaFin", fechaFin.Date);
                        cmd.Parameters.AddWithValue("@Ciudad", ciudad);
                        cmd.Parameters.AddWithValue("@Radio", radio);
                        cmd.CommandTimeout = 120; // 2 minutos timeout

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                comercialesAsignados.Add((
                                    reader["FilePath"].ToString(),
                                    reader["Posicion"].ToString(),
                                    Convert.ToInt32(reader["Fila"]),
                                    reader["Fecha"] != DBNull.Value ? Convert.ToDateTime(reader["Fecha"]) : (DateTime?)null,
                                    Convert.ToInt32(reader["Columna"]),
                                    Convert.ToDateTime(reader["FechaInicio"]),
                                    Convert.ToDateTime(reader["FechaFinal"]),
                                    reader["TipoProgramacion"]?.ToString() ?? "Cada 00-30"
                                ));
                            }
                        }
                    }

                    // Log de depuración: cuántos comerciales encontró en ComercialesAsignados
                    Logger.Log($"[PAUTA-BATCH] Comerciales en ComercialesAsignados: {comercialesAsignados.Count}");
                    var filePathsUnicos = comercialesAsignados.Select(c => System.IO.Path.GetFileName(c.FilePath)).Distinct().ToList();
                    Logger.Log($"[PAUTA-BATCH] Archivos únicos de ComercialesAsignados: {filePathsUnicos.Count}");
                    foreach (var fp in filePathsUnicos.Take(5))
                        Logger.Log($"[PAUTA-BATCH]   - {fp}");

                    // 2. NUEVO: Buscar directamente en Comerciales (datos importados de Access sin ComercialesAsignados)
                    // Estos comerciales tienen la hora en el código: ACC-550-ABA-EXI-0600
                    string queryDirecto = @"
                        SELECT DISTINCT c.FilePath, c.Posicion, c.Codigo, c.FechaInicio, c.FechaFinal,
                               COALESCE(c.TipoProgramacion, 'Cada 00-30') as TipoProgramacion
                        FROM Comerciales c
                        WHERE c.Codigo LIKE 'ACC-%'
                          AND c.FechaInicio::date <= @FechaFin::date
                          AND c.FechaFinal::date >= @FechaInicio::date
                          AND LOWER(c.Ciudad) = LOWER(@Ciudad)
                          AND LOWER(c.Radio) = LOWER(@Radio)
                          AND c.Estado = 'Activo'
                        ORDER BY c.Posicion ASC, c.FilePath ASC";

                    var comercialesDirectos = new List<(string FilePath, string Posicion, string Codigo, DateTime FechaInicio, DateTime FechaFinal, string TipoProgramacion)>();

                    using (var cmd = new NpgsqlCommand(queryDirecto, conn))
                    {
                        cmd.Parameters.AddWithValue("@FechaInicio", fechaInicio.Date);
                        cmd.Parameters.AddWithValue("@FechaFin", fechaFin.Date);
                        cmd.Parameters.AddWithValue("@Ciudad", ciudad);
                        cmd.Parameters.AddWithValue("@Radio", radio);
                        cmd.CommandTimeout = 120;

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                comercialesDirectos.Add((
                                    reader["FilePath"].ToString(),
                                    reader["Posicion"].ToString(),
                                    reader["Codigo"].ToString(),
                                    Convert.ToDateTime(reader["FechaInicio"]),
                                    Convert.ToDateTime(reader["FechaFinal"]),
                                    reader["TipoProgramacion"]?.ToString() ?? "Cada 00-30"
                                ));
                            }
                        }
                    }

                    Logger.Log($"[PAUTA-BATCH] Comerciales directos (ACC-): {comercialesDirectos.Count}");
                    var filePathsDirectos = comercialesDirectos.Select(c => System.IO.Path.GetFileName(c.FilePath)).Distinct().ToList();
                    Logger.Log($"[PAUTA-BATCH] Archivos únicos directos: {filePathsDirectos.Count}");
                    foreach (var fp in filePathsDirectos.Take(5))
                        Logger.Log($"[PAUTA-BATCH]   - {fp}");

                    // DEBUG: Mostrar tipo de tanda detectado y horarios
                    Logger.Log($"[PAUTA-BATCH] TipoTanda detectado: {tipoTandaPorDefecto}");
                    Logger.Log($"[PAUTA-BATCH] Total horarios: {horariosPorDefecto.Length}");
                    if (horariosPorDefecto.Length > 0)
                        Logger.Log($"[PAUTA-BATCH] Primeras horas: {string.Join(", ", horariosPorDefecto.Take(6))}");

                    // DEBUG: Mostrar TipoProgramacion de los primeros comerciales
                    var tiposUnicos = comercialesAsignados.Select(c => c.TipoProgramacion).Distinct().ToList();
                    Logger.Log($"[PAUTA-BATCH] TipoProgramacion únicos en comerciales: {string.Join(", ", tiposUnicos)}");

                    // Set para evitar duplicados entre ambas fuentes
                    var filePathsYaProcesados = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                    // Procesar cada fecha en el rango
                    int diasProcesados = 0;
                    int totalDias = (int)(fechaFin - fechaInicio).TotalDays + 1;
                    for (DateTime fecha = fechaInicio; fecha <= fechaFin; fecha = fecha.AddDays(1))
                    {
                        string claveFecha = fecha.ToString("yyyy-MM-dd");
                        resultado[claveFecha] = new List<(string, string, string)>();

                        // Calcular columna y rotación para esta fecha
                        int diaSemana = (int)fecha.DayOfWeek;
                        int columnaParaDiaSemana = diaSemana == 0 ? 8 : diaSemana + 1;
                        int indiceRotacion = diaSemana == 0 ? 6 : diaSemana - 1;
                        bool esDiaLaboral = indiceRotacion >= 0 && indiceRotacion <= 4;

                        // Filtrar asignaciones válidas para esta fecha
                        // - Si tiene FechaAsignacion, debe coincidir con la fecha actual
                        // - Si no tiene FechaAsignacion (legacy), usar Columna para día de semana
                        var asignacionesDelDia = comercialesAsignados
                            .Where(c => fecha.Date >= c.FechaInicio.Date && fecha.Date <= c.FechaFinal.Date)
                            .Where(c =>
                                (c.FechaAsignacion.HasValue && c.FechaAsignacion.Value.Date == fecha.Date) ||
                                (!c.FechaAsignacion.HasValue && c.Columna == columnaParaDiaSemana))
                            .ToList();

                        // Log solo para el primer día
                        if (diasProcesados == 0)
                        {
                            Logger.Log($"[PAUTA-BATCH] Fecha {fecha:dd/MM/yyyy}: {asignacionesDelDia.Count} asignaciones del día");
                            Logger.Log($"[PAUTA-BATCH] DiaSemana={diaSemana}, ColumnaParaDia={columnaParaDiaSemana}");
                            var archivosDelDia = asignacionesDelDia.Select(a => System.IO.Path.GetFileName(a.FilePath)).Distinct().ToList();
                            foreach (var a in archivosDelDia.Take(10))
                                Logger.Log($"[PAUTA-BATCH]   - {a}");

                            // DEBUG: Mostrar info de la primera asignación
                            if (asignacionesDelDia.Count > 0)
                            {
                                var primera = asignacionesDelDia[0];
                                Logger.Log($"[PAUTA-BATCH] Primera asignación: Fila={primera.Fila}, TipoProg={primera.TipoProgramacion}, FechaAsig={primera.FechaAsignacion}, Col={primera.Columna}");
                            }
                        }

                        // Agrupar por secuencia para manejar rotación
                        // IMPORTANTE: Ahora guardamos también el TipoProgramacion para convertir fila->hora correctamente
                        var porSecuenciaYFila = new Dictionary<string, List<(string FilePath, string Posicion, int Fila, string TipoProgramacion)>>();
                        var individuales = new List<(string FilePath, string Posicion, int Fila, string TipoProgramacion)>();

                        // HashSet para evitar duplicados del mismo FilePath en la misma hora
                        var filePathFilaYaProcesados = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                        foreach (var asig in asignacionesDelDia)
                        {
                            string nombreArchivo = System.IO.Path.GetFileNameWithoutExtension(asig.FilePath);
                            string claveSecuencia = ExtraerClaveSecuencia(nombreArchivo);

                            // Clave para evitar duplicados: FilePath + Fila
                            string claveDuplicado = $"{asig.FilePath}|{asig.Fila}";
                            if (filePathFilaYaProcesados.Contains(claveDuplicado))
                                continue;
                            filePathFilaYaProcesados.Add(claveDuplicado);

                            if (claveSecuencia != null)
                            {
                                // Clave única: secuencia + fila (hora)
                                string claveCompleta = $"{claveSecuencia}|{asig.Fila}";
                                if (!porSecuenciaYFila.ContainsKey(claveCompleta))
                                    porSecuenciaYFila[claveCompleta] = new List<(string, string, int, string)>();
                                porSecuenciaYFila[claveCompleta].Add((asig.FilePath, asig.Posicion, asig.Fila, asig.TipoProgramacion));
                            }
                            else
                            {
                                individuales.Add((asig.FilePath, asig.Posicion, asig.Fila, asig.TipoProgramacion));
                            }
                        }

                        // HashSet unificado para evitar duplicados en el resultado final
                        // Formato: "NombreArchivo|Hora" - no importa de dónde venga el comercial
                        var comercialesYaAgregados = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                        // Agregar comerciales individuales
                        // IMPORTANTE: Usar la hora real del comercial según su TipoProgramacion,
                        // sin intentar convertir a otro formato (eso causaba pérdida de comerciales)
                        foreach (var com in individuales)
                        {
                            string nombreArchivo = System.IO.Path.GetFileNameWithoutExtension(com.FilePath);
                            string horaReal = ConvertirFilaAHora(com.Fila, com.TipoProgramacion);
                            string claveDuplicado = $"{nombreArchivo}|{horaReal}";
                            if (!string.IsNullOrEmpty(horaReal) && !comercialesYaAgregados.Contains(claveDuplicado))
                            {
                                resultado[claveFecha].Add((nombreArchivo, com.Posicion, horaReal));
                                comercialesYaAgregados.Add(claveDuplicado);
                            }
                        }

                        // Procesar secuencias
                        // - Si hay UN SOLO archivo en la secuencia: tratarlo como individual (aparece TODOS los días)
                        // - Si hay MÚLTIPLES archivos: rotar solo de lunes a viernes
                        foreach (var kvp in porSecuenciaYFila)
                        {
                            var archivos = kvp.Value.OrderBy(a => a.FilePath).ToList();
                            if (archivos.Count == 1)
                            {
                                // UN SOLO archivo: tratarlo como individual - aparece TODOS los días (incluyendo sábado y domingo)
                                var unico = archivos[0];
                                string nombreArchivo = System.IO.Path.GetFileNameWithoutExtension(unico.FilePath);
                                string horaReal = ConvertirFilaAHora(unico.Fila, unico.TipoProgramacion);
                                string claveDuplicado = $"{nombreArchivo}|{horaReal}";
                                if (!string.IsNullOrEmpty(horaReal) && !comercialesYaAgregados.Contains(claveDuplicado))
                                {
                                    resultado[claveFecha].Add((nombreArchivo, unico.Posicion, horaReal));
                                    comercialesYaAgregados.Add(claveDuplicado);
                                }
                            }
                            else if (archivos.Count > 1 && esDiaLaboral)
                            {
                                // MÚLTIPLES archivos: rotar solo de lunes a viernes
                                int indiceSeleccionado = indiceRotacion % archivos.Count;
                                var seleccionado = archivos[indiceSeleccionado];
                                string nombreArchivo = System.IO.Path.GetFileNameWithoutExtension(seleccionado.FilePath);

                                // Usar la hora real del comercial según su TipoProgramacion
                                string horaReal = ConvertirFilaAHora(seleccionado.Fila, seleccionado.TipoProgramacion);
                                string claveDuplicado = $"{nombreArchivo}|{horaReal}";
                                if (!string.IsNullOrEmpty(horaReal) && !comercialesYaAgregados.Contains(claveDuplicado))
                                {
                                    resultado[claveFecha].Add((nombreArchivo, seleccionado.Posicion, horaReal));
                                    comercialesYaAgregados.Add(claveDuplicado);
                                }
                            }
                            // Si hay múltiples archivos y NO es día laboral, no agregar (secuencias solo rotan L-V)
                        }

                        // 3. NUEVO: Procesar comerciales directos (ACC-) para esta fecha
                        // Estos tienen la hora en el código, no en ComercialesAsignados
                        // IMPORTANTE: Cada comercial puede tener MÚLTIPLES registros ACC (uno por cada hora)
                        // Debemos agregar el comercial a TODAS sus horas asignadas
                        // Usamos comercialesYaAgregados que ya contiene los comerciales agregados anteriormente
                        var comercialesDirectosDelDia = comercialesDirectos
                            .Where(c => fecha.Date >= c.FechaInicio.Date && fecha.Date <= c.FechaFinal.Date)
                            .ToList();

                        // Agrupar primero por FilePath para detectar secuencias
                        var comercialesPorFilePath = comercialesDirectosDelDia
                            .Where(c => !filePathsYaProcesados.Contains(c.FilePath))
                            .GroupBy(c => c.FilePath)
                            .ToDictionary(g => g.Key, g => g.ToList());

                        // Detectar secuencias (archivos que terminan en 01, 02, etc.)
                        var filePathsDirectosUnicos = comercialesPorFilePath.Keys.ToList();
                        var secuenciasDetectadas = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
                        var filePathsIndividuales = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                        foreach (var filePath in filePathsDirectosUnicos)
                        {
                            string nombreArchivo = System.IO.Path.GetFileNameWithoutExtension(filePath);
                            string claveSecuencia = ExtraerClaveSecuencia(nombreArchivo);

                            if (claveSecuencia != null)
                            {
                                if (!secuenciasDetectadas.ContainsKey(claveSecuencia))
                                    secuenciasDetectadas[claveSecuencia] = new List<string>();
                                secuenciasDetectadas[claveSecuencia].Add(filePath);
                            }
                            else
                            {
                                filePathsIndividuales.Add(filePath);
                            }
                        }

                        // Procesar comerciales individuales: agregar a TODAS sus horas
                        foreach (var filePath in filePathsIndividuales)
                        {
                            var registros = comercialesPorFilePath[filePath];
                            string nombreArchivo = System.IO.Path.GetFileNameWithoutExtension(filePath);

                            foreach (var reg in registros)
                            {
                                // Extraer la hora directamente del código ACC (ej: ACC-550-ABA-EXI-0600 -> "06:00")
                                string horaDirecta = ExtraerHoraDeCodigoACC(reg.Codigo);
                                // Usar NombreArchivo (sin extensión) para la clave, igual que en el HashSet inicial
                                string claveUnica = $"{nombreArchivo}|{horaDirecta}";

                                if (!string.IsNullOrEmpty(horaDirecta) && !comercialesYaAgregados.Contains(claveUnica))
                                {
                                    resultado[claveFecha].Add((nombreArchivo, reg.Posicion, horaDirecta));
                                    comercialesYaAgregados.Add(claveUnica);
                                }
                            }
                        }

                        // Procesar secuencias de comerciales directos
                        foreach (var kvp in secuenciasDetectadas)
                        {
                            var archivosSecuencia = kvp.Value.OrderBy(a => a).ToList();

                            if (archivosSecuencia.Count == 1)
                            {
                                // Solo un archivo: agregar a TODAS sus horas
                                var filePath = archivosSecuencia[0];
                                var registros = comercialesPorFilePath[filePath];
                                string nombreArchivo = System.IO.Path.GetFileNameWithoutExtension(filePath);

                                foreach (var reg in registros)
                                {
                                    string horaDirecta = ExtraerHoraDeCodigoACC(reg.Codigo);
                                    // Usar NombreArchivo para la clave, igual que en el HashSet inicial
                                    string claveUnica = $"{nombreArchivo}|{horaDirecta}";

                                    if (!string.IsNullOrEmpty(horaDirecta) && !comercialesYaAgregados.Contains(claveUnica))
                                    {
                                        resultado[claveFecha].Add((nombreArchivo, reg.Posicion, horaDirecta));
                                        comercialesYaAgregados.Add(claveUnica);
                                    }
                                }
                            }
                            else if (archivosSecuencia.Count > 1 && esDiaLaboral)
                            {
                                // Múltiples archivos: rotar y agregar a TODAS las horas del seleccionado
                                int indiceSeleccionado = indiceRotacion % archivosSecuencia.Count;
                                var filePathSeleccionado = archivosSecuencia[indiceSeleccionado];
                                var registros = comercialesPorFilePath[filePathSeleccionado];
                                string nombreArchivo = System.IO.Path.GetFileNameWithoutExtension(filePathSeleccionado);

                                foreach (var reg in registros)
                                {
                                    string horaDirecta = ExtraerHoraDeCodigoACC(reg.Codigo);
                                    // Usar NombreArchivo para la clave, igual que en el HashSet inicial
                                    string claveUnica = $"{nombreArchivo}|{horaDirecta}";

                                    if (!string.IsNullOrEmpty(horaDirecta) && !comercialesYaAgregados.Contains(claveUnica))
                                    {
                                        resultado[claveFecha].Add((nombreArchivo, reg.Posicion, horaDirecta));
                                        comercialesYaAgregados.Add(claveUnica);
                                    }
                                }
                            }
                        }

                        // Marcar FilePaths de ComercialesAsignados como ya procesados para la siguiente fecha
                        foreach (var asig in asignacionesDelDia)
                        {
                            filePathsYaProcesados.Add(asig.FilePath);
                        }

                        // Limpiar para la siguiente fecha
                        filePathsYaProcesados.Clear();

                        // DEBUG: Log del resultado del primer día
                        if (diasProcesados == 0)
                        {
                            Logger.Log($"[PAUTA-BATCH] Resultado para {claveFecha}: {resultado[claveFecha].Count} comerciales agregados");
                            foreach (var com in resultado[claveFecha].Take(5))
                                Logger.Log($"[PAUTA-BATCH]   - {com.NombreArchivo} | {com.Posicion} | Hora={com.Hora}");
                        }

                        diasProcesados++;
                    }

                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[BATCH] ERROR: {ex.Message}");
                Logger.Log($"[PAUTA-BATCH] ERROR: {ex.Message}");
            }

            return resultado;
        }

        /// <summary>
        /// Convierte una fila de un TipoProgramacion a otro.
        /// Ejemplo: Fila 0 en "Cada 00-20-30-50 (96 tandas)" = 00:00
        ///          En "Cada 20-50 (48 tandas)" esa hora no existe, retorna -1
        ///          Fila 2 en "Cada 00-20-30-50 (96 tandas)" = 00:30
        ///          En "Cada 00-30 (48 tandas)" corresponde a Fila 1
        /// </summary>
        // Variable estática para limitar logs
        private static int _logConversionCount = 0;

        private int ConvertirFilaEntreFormatos(int filaOrigen, string tipoProgramacionOrigen, TipoTanda tipoSalida, string[] horariosDestino)
        {
            // Obtener el TipoTanda del origen
            TipoTanda? tipoOrigen = ConvertirTipoProgramacionATipoTanda(tipoProgramacionOrigen);
            if (!tipoOrigen.HasValue)
            {
                // DEBUG: Solo loguear los primeros 5
                if (_logConversionCount < 5)
                {
                    Logger.Log($"[CONVERSION] TipoProgramacion no reconocido: '{tipoProgramacionOrigen}', usando fallback Tandas_00_20_30_50");
                    _logConversionCount++;
                }
                tipoOrigen = TipoTanda.Tandas_00_20_30_50;
            }

            if (tipoOrigen.Value == tipoSalida)
            {
                return filaOrigen;
            }

            string[] horariosOrigen = TandasHorarias.GetHorarios(tipoOrigen.Value);
            if (filaOrigen < 0 || filaOrigen >= horariosOrigen.Length)
            {
                return -1;
            }

            string horaReal = horariosOrigen[filaOrigen];

            for (int i = 0; i < horariosDestino.Length; i++)
            {
                if (horariosDestino[i] == horaReal)
                {
                    return i;
                }
            }

            // DEBUG: Loguear cuando no se encuentra la hora en el destino
            if (_logConversionCount < 10)
            {
                Logger.Log($"[CONVERSION] Hora {horaReal} (fila {filaOrigen}) de {tipoOrigen.Value} no existe en {tipoSalida}");
                _logConversionCount++;
            }

            return -1;
        }

        /// <summary>
        /// Extrae la fila (índice de horario) del código ACC (formato: ACC-XXX-XXX-XXX-HHMM)
        /// </summary>
        private int ExtraerFilaDeCodigoACC(string codigo, string[] horarios)
        {
            if (string.IsNullOrEmpty(codigo) || !codigo.StartsWith("ACC-", StringComparison.OrdinalIgnoreCase))
                return -1;

            // El código tiene formato: ACC-550-ABA-EXI-0600
            // La hora está en los últimos 4 dígitos: HHMM
            string[] partes = codigo.Split('-');
            if (partes.Length >= 5)
            {
                string horaStr = partes[partes.Length - 1]; // Último segmento: "0600"
                if (horaStr.Length == 4 && int.TryParse(horaStr.Substring(0, 2), out int hora) && int.TryParse(horaStr.Substring(2, 2), out int minuto))
                {
                    string horaFormateada = $"{hora:D2}:{minuto:D2}";
                    for (int i = 0; i < horarios.Length; i++)
                    {
                        if (horarios[i] == horaFormateada)
                            return i;
                    }
                }
            }
            return -1;
        }

        /// <summary>
        /// Extrae la hora directamente del código ACC (formato: ACC-XXX-XXX-XXX-HHMM)
        /// Retorna la hora formateada como "HH:MM" (ej: "06:00", "15:45")
        /// </summary>
        private string ExtraerHoraDeCodigoACC(string codigo)
        {
            if (string.IsNullOrEmpty(codigo) || !codigo.StartsWith("ACC-", StringComparison.OrdinalIgnoreCase))
                return null;

            // El código tiene formato: ACC-550-ABA-EXI-0600
            // La hora está en los últimos 4 dígitos: HHMM
            string[] partes = codigo.Split('-');
            if (partes.Length >= 5)
            {
                string horaStr = partes[partes.Length - 1]; // Último segmento: "0600"
                if (horaStr.Length == 4 && int.TryParse(horaStr.Substring(0, 2), out int hora) && int.TryParse(horaStr.Substring(2, 2), out int minuto))
                {
                    return $"{hora:D2}:{minuto:D2}";
                }
            }
            return null;
        }

        /// <summary>
        /// Extrae la fila (índice de horario) basándose en el código del comercial
        /// </summary>
        private int ExtraerFilaDeComercial(string filePath, string[] horarios)
        {
            // El código tiene formato: XXX-###-XXX-XXX-HHMM donde HHMM es la hora
            string nombreArchivo = System.IO.Path.GetFileNameWithoutExtension(filePath);

            // Intentar extraer hora del path o nombre
            // Buscar patrón de 4 dígitos al final que represente hora (0000, 0030, 0100, etc)
            for (int i = 0; i < horarios.Length; i++)
            {
                string[] partes = horarios[i].Split(':');
                if (partes.Length == 2)
                {
                    string horaStr = partes[0] + partes[1]; // "00:00" -> "0000"
                    if (filePath.Contains("-" + horaStr) || nombreArchivo.EndsWith(horaStr))
                    {
                        return i;
                    }
                }
            }

            return -1; // No encontrado
        }

        /// <summary>
        /// Metodo sincrono para compatibilidad con codigo existente
        /// </summary>
        public string GenerarPautaArchivo(DataGridView dataGridView, string ciudad, string radio, string filePath)
        {
            return Task.Run(() => GenerarPautaArchivoAsync(dataGridView, ciudad, radio, filePath)).Result;
        }

        /// <summary>
        /// Obtiene TODOS los comerciales de la BD que coinciden con la hora, fecha, ciudad y radio
        /// ordenados por posicion (P01, P02, P03...)
        /// Busca tanto en ComercialesAsignados como directamente en Comerciales (para datos importados de Access)
        /// </summary>
        private async Task<List<ComercialParaPauta>> ObtenerComercialesPorHoraFechaAsync(
            int fila, int columna, DateTime fecha, string ciudad, string radio)
        {
            var comerciales = new List<ComercialParaPauta>();
            var filePathsAgregados = new HashSet<string>(); // Para evitar duplicados

            // Obtener el TipoProgramacion desde la BD para este comercial específico
            // En lugar de detectar por nombre de radio, usamos el valor guardado por el usuario
            TipoTanda tipoTanda = await ObtenerTipoTandaDesdeBDAsync(ciudad, radio);
            var horarios = TandasHorarias.GetHorarios(tipoTanda);
            int hora, minuto;
            if (fila >= 0 && fila < horarios.Length)
            {
                string[] partes = horarios[fila].Split(':');
                hora = int.Parse(partes[0]);
                minuto = int.Parse(partes[1]);
            }
            else
            {
                hora = fila / 2;
                minuto = (fila % 2) * 30;
            }
            string horaStr = $"{hora:D2}{minuto:D2}";

            try
            {
                using (var conn = new NpgsqlConnection(DatabaseConfig.ConnectionString))
                {
                    await conn.OpenAsync();

                    // 1. Primero buscar en ComercialesAsignados (comerciales guardados con nuevo sistema)
                    // Nota: Removido filtro de fechas porque la fecha de la columna del grid ya representa la fecha deseada
                    //       y el comercial ya fue asignado a esa celda específica
                    string queryAsignados = @"
                        SELECT
                            ca.ComercialAsignado,
                            c.FilePath,
                            c.Posicion,
                            c.Codigo,
                            c.FechaInicio,
                            c.FechaFinal,
                            c.Ciudad as CiudadBD,
                            c.Radio as RadioBD
                        FROM ComercialesAsignados ca
                        INNER JOIN Comerciales c ON ca.Codigo = c.Codigo
                        WHERE ca.Fila = @Fila
                          AND ca.Columna = @Columna
                        ORDER BY c.Posicion ASC";

                    using (var cmd = new NpgsqlCommand(queryAsignados, conn))
                    {
                        cmd.Parameters.AddWithValue("@Fila", fila);
                        cmd.Parameters.AddWithValue("@Columna", columna);

                        using (var reader = (NpgsqlDataReader)await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                string filePath = reader["FilePath"].ToString();
                                string ciudadBD = reader["CiudadBD"].ToString();
                                string radioBD = reader["RadioBD"].ToString();
                                DateTime fechaInicioBD = reader.GetDateTime(reader.GetOrdinal("FechaInicio"));
                                DateTime fechaFinalBD = reader.GetDateTime(reader.GetOrdinal("FechaFinal"));

                                bool ciudadOk = ciudadBD.Equals(ciudad, StringComparison.OrdinalIgnoreCase);
                                bool radioOk = radioBD.Equals(radio, StringComparison.OrdinalIgnoreCase);
                                bool fechaOk = fecha.Date >= fechaInicioBD.Date && fecha.Date <= fechaFinalBD.Date;

                                if (ciudadOk && radioOk && fechaOk)
                                {
                                    comerciales.Add(new ComercialParaPauta
                                    {
                                        ComercialAsignado = reader["ComercialAsignado"].ToString(),
                                        FilePath = filePath,
                                        Posicion = reader["Posicion"].ToString(),
                                        Codigo = reader["Codigo"].ToString()
                                    });
                                    filePathsAgregados.Add(filePath);
                                }
                            }
                        }
                    }

                    // 2. Buscar directamente en Comerciales (datos importados de Access sin ComercialesAsignados)
                    // La hora esta en el ultimo segmento del codigo: ACC-550-ABA-EXI-0600
                    string queryDirecto = @"
                        SELECT DISTINCT
                            c.FilePath,
                            c.Posicion,
                            c.Codigo
                        FROM Comerciales c
                        WHERE c.Codigo LIKE '%-' || @HoraStr
                          AND LOWER(c.Ciudad) = LOWER(@Ciudad)
                          AND LOWER(c.Radio) = LOWER(@Radio)
                          AND c.Estado = 'Activo'
                          AND c.FechaInicio::date <= @Fecha::date
                          AND c.FechaFinal::date >= @Fecha::date
                        ORDER BY c.Posicion ASC";

                    using (var cmd = new NpgsqlCommand(queryDirecto, conn))
                    {
                        cmd.Parameters.AddWithValue("@HoraStr", horaStr);
                        cmd.Parameters.AddWithValue("@Ciudad", ciudad);
                        cmd.Parameters.AddWithValue("@Radio", radio);
                        cmd.Parameters.AddWithValue("@Fecha", fecha.Date);

                        using (var reader = (NpgsqlDataReader)await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                string filePath = reader["FilePath"].ToString();

                                if (filePathsAgregados.Contains(filePath))
                                    continue;

                                string nombreArchivo = System.IO.Path.GetFileNameWithoutExtension(filePath);
                                string posicion = reader["Posicion"].ToString();

                                comerciales.Add(new ComercialParaPauta
                                {
                                    ComercialAsignado = $"{posicion} {nombreArchivo}",
                                    FilePath = filePath,
                                    Posicion = posicion,
                                    Codigo = reader["Codigo"].ToString()
                                });
                                filePathsAgregados.Add(filePath);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GenerarPauta] Error: {ex.Message}");
            }

            return comerciales.OrderBy(c => c.Posicion).ToList();
        }

        private string construirRutaComercial(string filePath, string carpetaBase)
        {
            // Si el filePath ya es una ruta completa, extraer solo el nombre del archivo
            string nombreArchivo = System.IO.Path.GetFileName(filePath);

            // Verificar si ya tiene extensión .mp3
            if (!nombreArchivo.ToLower().EndsWith(".mp3"))
            {
                nombreArchivo += ".mp3";
            }

            return $"{carpetaBase}{nombreArchivo}";
        }

        /// <summary>
        /// Obtiene TODOS los comerciales de la BD para una hora (fila), fecha, ciudad y radio específicos.
        /// Busca tanto en ComercialesAsignados como directamente en Comerciales.
        /// Retorna una lista de tuplas (NombreArchivo, Posicion) ordenada por posición.
        /// </summary>
        /// <param name="filePathExcluir">FilePath del registro actual a excluir (se usará el grid para ese registro)</param>
        private async Task<List<(string NombreArchivo, string Posicion)>> ObtenerTodosLosComercialesPorHoraFechaAsync(
            int fila, int columna, DateTime fecha, string ciudad, string radio, string filePathExcluir = null)
        {
            return await ObtenerComercialesPorHoraStringAsync(null, fila, fecha, ciudad, radio, filePathExcluir);
        }

        /// <summary>
        /// Busca comerciales para una hora específica (string "HH:MM") o una fila.
        /// Si horaBuscada es null, usa fila para determinar la hora basándose en el tipo de tanda de la radio.
        /// </summary>
        private async Task<List<(string NombreArchivo, string Posicion)>> ObtenerComercialesPorHoraStringAsync(
            string horaBuscada, int fila, DateTime fecha, string ciudad, string radio, string filePathExcluir = null)
        {
            var resultado = new List<(string NombreArchivo, string Posicion)>();
            var filePathsAgregados = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Si hay un filePath a excluir, agregarlo al set para que no se incluya
            if (!string.IsNullOrEmpty(filePathExcluir))
            {
                filePathsAgregados.Add(filePathExcluir);
            }

            // Determinar la hora a buscar
            string horaFormateada;
            int hora, minuto;

            if (!string.IsNullOrEmpty(horaBuscada))
            {
                // Hora especificada directamente
                horaFormateada = horaBuscada;
                string[] partes = horaFormateada.Split(':');
                hora = int.Parse(partes[0]);
                minuto = int.Parse(partes[1]);
            }
            else
            {
                // Calcular hora desde fila usando el tipo de tanda de la radio
                TipoTanda tipoTanda = await ObtenerTipoTandaDesdeBDAsync(ciudad, radio);
                var horarios = TandasHorarias.GetHorarios(tipoTanda);

                if (fila >= 0 && fila < horarios.Length)
                {
                    horaFormateada = horarios[fila];
                    string[] partes = horaFormateada.Split(':');
                    hora = int.Parse(partes[0]);
                    minuto = int.Parse(partes[1]);
                }
                else
                {
                    // Fallback: calcular basado en 48 tandas
                    hora = fila / 2;
                    minuto = (fila % 2) * 30;
                    horaFormateada = $"{hora:D2}:{minuto:D2}";
                }
            }
            string horaStr = $"{hora:D2}{minuto:D2}"; // "0000", "0030", "0100", etc.

            // Log para depuración de generación de pautas
            if (fila == 0)
            {
                System.Diagnostics.Debug.WriteLine($"[PAUTA] Generando para fecha={fecha:dd/MM/yyyy}, ciudad={ciudad}, radio={radio}, hora={horaFormateada}");
                Logger.Log($"[PAUTA-DEBUG] Iniciando consulta para fecha={fecha:dd/MM/yyyy}, ciudad={ciudad}, radio={radio}");
            }

            try
            {
                using (var conn = new NpgsqlConnection(DatabaseConfig.ConnectionString))
                {
                    await conn.OpenAsync();

                    // 1. Buscar en ComercialesAsignados (comerciales con nuevo sistema de asignación)
                    // SOLUCION: Buscar por HORA, no por fila. Cada comercial tiene su propio TipoProgramacion
                    // que determina cómo convertir su Fila a hora.
                    // Usamos SQL para calcular la hora desde la Fila según el TipoProgramacion del comercial.

                    // Calcular la columna correspondiente al día de la semana de la fecha buscada
                    // Columnas: 2=Lunes, 3=Martes, 4=Miércoles, 5=Jueves, 6=Viernes, 7=Sábado, 8=Domingo
                    int diaSemana = (int)fecha.DayOfWeek;
                    int columnaParaDiaSemana = diaSemana == 0 ? 8 : diaSemana + 1; // Domingo=0 -> columna 8

                    // Para registros legacy (ca.Fecha IS NULL) con múltiples archivos en la misma posición,
                    // necesitamos rotar: seleccionar solo UN archivo por día usando el día de la semana
                    // Lunes=0, Martes=1, Miércoles=2, Jueves=3, Viernes=4, Sábado=5, Domingo=6
                    // Esto hace que: Lunes→archivo 1, Martes→archivo 2, etc.
                    int indiceRotacion = diaSemana == 0 ? 6 : diaSemana - 1; // Convertir: Lunes(1)→0, Martes(2)→1, ..., Domingo(0)→6

                    // NUEVO QUERY: Buscar por HORA calculada desde Fila según TipoProgramacion
                    // Para 96 tandas (00-20-30-50): cada 4 filas = 1 hora, patrón: 00,20,30,50
                    // Para 48 tandas (20-50): cada 2 filas = 1 hora, patrón: 20,50
                    // Para 48 tandas (00-30): cada 2 filas = 1 hora, patrón: 00,30
                    // Para 48 tandas (10-40): cada 2 filas = 1 hora, patrón: 10,40
                    // Para 48 tandas (15-45): cada 2 filas = 1 hora, patrón: 15,45
                    string queryAsignados = @"
                        SELECT ca.ComercialAsignado, c.FilePath, c.Posicion, ca.Fecha as FechaAsignacion, ca.Fila, c.TipoProgramacion
                        FROM ComercialesAsignados ca
                        INNER JOIN Comerciales c ON ca.Codigo = c.Codigo
                        WHERE (
                              -- Buscar por fecha exacta (registros nuevos)
                              ca.Fecha = @Fecha::date
                              OR
                              -- Buscar por columna/día de semana (registros antiguos sin fecha)
                              (ca.Fecha IS NULL AND ca.Columna = @ColumnaParaDiaSemana)
                          )
                          AND c.FechaInicio::date <= @Fecha::date
                          AND c.FechaFinal::date >= @Fecha::date
                          AND LOWER(c.Ciudad) = LOWER(@Ciudad)
                          AND LOWER(c.Radio) = LOWER(@Radio)
                          AND c.Estado = 'Activo'
                        ORDER BY c.Posicion ASC, c.FilePath ASC";

                    // Estructura para agrupar comerciales por "base del nombre" (para secuencias como PAPILLON 01, 02, 03)
                    // La clave es el nombre base sin el número final (ej: "SECUENCIA PAPILLON KARIBEÑA-2025")
                    // NOTA: Aplicamos la lógica de secuencias tanto a registros con fecha como sin fecha
                    var comercialesPorSecuencia = new Dictionary<string, List<(string FilePath, string Posicion)>>();
                    // Para comerciales que NO son parte de una secuencia
                    var comercialesIndividuales = new List<(string FilePath, string Posicion)>();


                    using (var cmd = new NpgsqlCommand(queryAsignados, conn))
                    {
                        cmd.Parameters.AddWithValue("@Fecha", fecha.Date);
                        cmd.Parameters.AddWithValue("@ColumnaParaDiaSemana", columnaParaDiaSemana);
                        cmd.Parameters.AddWithValue("@Ciudad", ciudad);
                        cmd.Parameters.AddWithValue("@Radio", radio);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                string filePath = reader["FilePath"].ToString();
                                string posicion = reader["Posicion"].ToString();
                                int filaComercial = Convert.ToInt32(reader["Fila"]);
                                string tipoProgramacion = reader["TipoProgramacion"]?.ToString() ?? "";
                                var fechaAsignacion = reader["FechaAsignacion"];
                                string fechaAsigStr = fechaAsignacion != DBNull.Value ? ((DateTime)fechaAsignacion).ToString("dd/MM/yyyy") : "NULL";

                                // NUEVO: Convertir la fila del comercial a hora según su TipoProgramacion
                                string horaComercial = ConvertirFilaAHora(filaComercial, tipoProgramacion);

                                if (horaComercial != horaFormateada)
                                {
                                    continue;
                                }

                                if (!filePathsAgregados.Contains(filePath))
                                {
                                    // Detectar si es parte de una secuencia (aplica tanto a registros con fecha como sin fecha)
                                    string nombreArchivo = System.IO.Path.GetFileNameWithoutExtension(filePath);
                                    string claveSecuencia = ExtraerClaveSecuencia(nombreArchivo);

                                    if (claveSecuencia != null)
                                    {
                                        // Es parte de una secuencia (termina en número como 01, 02, 03)
                                        if (!comercialesPorSecuencia.ContainsKey(claveSecuencia))
                                        {
                                            comercialesPorSecuencia[claveSecuencia] = new List<(string, string)>();
                                        }
                                        comercialesPorSecuencia[claveSecuencia].Add((filePath, posicion));
                                    }
                                    else
                                    {
                                        // No es secuencia: agregar como individual
                                        comercialesIndividuales.Add((filePath, posicion));
                                    }
                                }
                            }
                        }
                    }

                    // Para cada secuencia de archivos:
                    // - Si hay UN SOLO archivo: tratarlo como individual (aparece TODOS los días)
                    // - Si hay MÚLTIPLES archivos: rotar solo de lunes a viernes
                    bool esDiaLaboral = indiceRotacion >= 0 && indiceRotacion <= 4; // Lunes=0 a Viernes=4

                    // Set para rastrear secuencias ya procesadas (evitar que queryDirecto las procese de nuevo)
                    var secuenciasProcesadas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                    foreach (var kvp in comercialesPorSecuencia)
                    {
                        string claveSecuencia = kvp.Key;
                        var archivos = kvp.Value.OrderBy(a => a.FilePath).ToList();

                        // Marcar esta secuencia como procesada
                        secuenciasProcesadas.Add(claveSecuencia);

                        // Agregar TODOS los archivos de la secuencia al set para evitar duplicados
                        foreach (var archivo in archivos)
                        {
                            filePathsAgregados.Add(archivo.FilePath);
                        }

                        if (archivos.Count == 1)
                        {
                            // UN SOLO archivo: tratarlo como individual - aparece TODOS los días
                            var unico = archivos[0];
                            string nombreArchivo = System.IO.Path.GetFileNameWithoutExtension(unico.FilePath);
                            resultado.Add((nombreArchivo, unico.Posicion));
                        }
                        else if (archivos.Count > 1 && esDiaLaboral)
                        {
                            // MÚLTIPLES archivos: rotar solo de lunes a viernes
                            int indiceSeleccionado = indiceRotacion % archivos.Count;
                            var seleccionado = archivos[indiceSeleccionado];

                            string nombreArchivo = System.IO.Path.GetFileNameWithoutExtension(seleccionado.FilePath);
                            resultado.Add((nombreArchivo, seleccionado.Posicion));
                        }
                        // Si hay múltiples archivos y NO es día laboral, no agregar
                    }

                    // Agregar comerciales individuales (no son secuencias)
                    foreach (var (filePath, posicion) in comercialesIndividuales)
                    {
                        if (!filePathsAgregados.Contains(filePath))
                        {
                            string nombreArchivo = System.IO.Path.GetFileNameWithoutExtension(filePath);
                            resultado.Add((nombreArchivo, posicion));
                            filePathsAgregados.Add(filePath);
                        }
                    }

                    // Log de depuración para la primera hora del día
                    if (fila == 0)
                    {
                        Logger.Log($"[PAUTA-DEBUG] Query ComercialesAsignados - Secuencias: {comercialesPorSecuencia.Count}, Individuales: {comercialesIndividuales.Count}");
                        Logger.Log($"[PAUTA-DEBUG] Resultado parcial tras query1: {resultado.Count} comerciales");
                        foreach (var r in resultado.Take(5))
                            Logger.Log($"[PAUTA-DEBUG]   - {r.NombreArchivo} (P{r.Posicion})");
                    }

                    // 2. Buscar directamente en Comerciales (datos importados de Access sin ComercialesAsignados)
                    // La hora está en el último segmento del código: ACC-550-ABA-EXI-0600
                    // NOTA: Este query también puede traer secuencias, así que aplicamos la misma lógica de rotación
                    string queryDirecto = @"
                        SELECT DISTINCT c.FilePath, c.Posicion, c.Codigo
                        FROM Comerciales c
                        WHERE c.Codigo LIKE '%-' || @HoraStr
                          AND c.FechaInicio::date <= @Fecha::date
                          AND c.FechaFinal::date >= @Fecha::date
                          AND LOWER(c.Ciudad) = LOWER(@Ciudad)
                          AND LOWER(c.Radio) = LOWER(@Radio)
                          AND c.Estado = 'Activo'
                        ORDER BY c.Posicion ASC, c.FilePath ASC";


                    // Estructuras para agrupar comerciales directos por secuencia
                    var comercialesDirectosPorSecuencia = new Dictionary<string, List<(string FilePath, string Posicion)>>();
                    var comercialesDirectosIndividuales = new List<(string FilePath, string Posicion)>();

                    using (var cmd = new NpgsqlCommand(queryDirecto, conn))
                    {
                        cmd.Parameters.AddWithValue("@HoraStr", horaStr);
                        cmd.Parameters.AddWithValue("@Fecha", fecha.Date);
                        cmd.Parameters.AddWithValue("@Ciudad", ciudad);
                        cmd.Parameters.AddWithValue("@Radio", radio);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                string filePath = reader["FilePath"].ToString();
                                string codigoDirecto = reader["Codigo"]?.ToString() ?? "";


                                if (!filePathsAgregados.Contains(filePath))
                                {
                                    string nombreArchivo = System.IO.Path.GetFileNameWithoutExtension(filePath);
                                    string posicion = reader["Posicion"].ToString();
                                    string claveSecuencia = ExtraerClaveSecuencia(nombreArchivo);

                                    if (claveSecuencia != null)
                                    {
                                        // Es parte de una secuencia - verificar si ya fue procesada
                                        if (!secuenciasProcesadas.Contains(claveSecuencia))
                                        {
                                            if (!comercialesDirectosPorSecuencia.ContainsKey(claveSecuencia))
                                            {
                                                comercialesDirectosPorSecuencia[claveSecuencia] = new List<(string, string)>();
                                            }
                                            comercialesDirectosPorSecuencia[claveSecuencia].Add((filePath, posicion));
                                        }
                                        // Si la secuencia ya fue procesada, ignorar este archivo
                                    }
                                    else
                                    {
                                        // No es secuencia
                                        comercialesDirectosIndividuales.Add((filePath, posicion));
                                    }
                                }
                            }
                        }
                    }

                    // Para cada secuencia de comerciales directos:
                    // - Si hay UN SOLO archivo: tratarlo como individual (aparece TODOS los días)
                    // - Si hay MÚLTIPLES archivos: rotar solo de lunes a viernes
                    foreach (var kvp in comercialesDirectosPorSecuencia)
                    {
                        string claveSecuencia = kvp.Key;
                        var archivos = kvp.Value.OrderBy(a => a.FilePath).ToList();

                        // Marcar como procesada
                        secuenciasProcesadas.Add(claveSecuencia);

                        // Agregar TODOS los archivos de la secuencia al set para evitar duplicados
                        foreach (var archivo in archivos)
                        {
                            filePathsAgregados.Add(archivo.FilePath);
                        }

                        if (archivos.Count == 1)
                        {
                            // UN SOLO archivo: tratarlo como individual - aparece TODOS los días
                            var unico = archivos[0];
                            string nombreArchivo = System.IO.Path.GetFileNameWithoutExtension(unico.FilePath);
                            resultado.Add((nombreArchivo, unico.Posicion));
                        }
                        else if (archivos.Count > 1 && esDiaLaboral)
                        {
                            // MÚLTIPLES archivos: rotar solo de lunes a viernes
                            int indiceSeleccionado = indiceRotacion % archivos.Count;
                            var seleccionado = archivos[indiceSeleccionado];

                            string nombreArchivo = System.IO.Path.GetFileNameWithoutExtension(seleccionado.FilePath);
                            resultado.Add((nombreArchivo, seleccionado.Posicion));
                        }
                        // Si hay múltiples archivos y NO es día laboral, no agregar
                    }

                    // Agregar comerciales directos individuales
                    foreach (var (filePath, posicion) in comercialesDirectosIndividuales)
                    {
                        if (!filePathsAgregados.Contains(filePath))
                        {
                            string nombreArchivo = System.IO.Path.GetFileNameWithoutExtension(filePath);
                            resultado.Add((nombreArchivo, posicion));
                            filePathsAgregados.Add(filePath);
                        }
                    }

                    // Log de depuración para la primera hora del día
                    if (fila == 0)
                    {
                        Logger.Log($"[PAUTA-DEBUG] Query Directos - Secuencias: {comercialesDirectosPorSecuencia.Count}, Individuales: {comercialesDirectosIndividuales.Count}");
                        Logger.Log($"[PAUTA-DEBUG] RESULTADO FINAL hora {horaFormateada}: {resultado.Count} comerciales");
                        foreach (var r in resultado)
                            Logger.Log($"[PAUTA-DEBUG]   - {r.NombreArchivo} (P{r.Posicion})");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[OBTENER_POR_HORA] Error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[GenerarPauta] Error: {ex.Message}");
                Logger.Log($"[PAUTA-ERROR] Error en ObtenerTodosLosComercialesPorHoraFechaAsync: {ex.Message}");
            }


            // Ordenar por posición NUMÉRICA (maneja tanto "P00" como "00")
            return resultado.OrderBy(r => ParsearPosicion(r.Posicion)).ToList();
        }

        /// <summary>
        /// Regenera el archivo TXT para una fecha específica después de eliminar pautas.
        /// Obtiene todos los comerciales de la BD para esa fecha/ciudad/radio y genera el archivo.
        /// </summary>
        public async Task RegenerarArchivoPorFechaAsync(DateTime fecha, string ciudad, string radio)
        {
            try
            {
                // Obtener TipoTanda para la radio
                TipoTanda tipoTanda = await ObtenerTipoTandaDesdeBDAsync(ciudad, radio);
                var horarios = TandasHorarias.GetHorarios(tipoTanda);

                // Obtener la ruta base de pautas
                string rutaBasePautas = ConfigManager.ObtenerRutaBasePautas();
                string directorioPautas = System.IO.Path.Combine(rutaBasePautas, ciudad.ToUpper());

                if (!System.IO.Directory.Exists(directorioPautas))
                {
                    System.IO.Directory.CreateDirectory(directorioPautas);
                }

                // Obtener carpeta base de comerciales
                string carpetaBase = ConfigManager.ObtenerCarpetaRadio(radio);

                // Construir contenido del archivo
                var sb = new StringBuilder();
                string separator = new string('-', 80);
                string horarioAnterior = "";

                // Iterar sobre TODOS los horarios
                for (int fila = 0; fila < horarios.Length; fila++)
                {
                    string horario = horarios[fila];

                    // Obtener comerciales para esta hora/fecha desde la BD
                    var comercialesBD = await ObtenerTodosLosComercialesPorHoraFechaAsync(
                        fila, 0, fecha, ciudad, radio, null);

                    // Ordenar por posición
                    var comercialesOrdenados = comercialesBD.OrderBy(c => ParsearPosicion(c.Posicion)).ToList();

                    foreach (var (nombreArchivo, posicion) in comercialesOrdenados)
                    {
                        if (horario != horarioAnterior)
                        {
                            sb.AppendLine(separator);
                            horarioAnterior = horario;
                        }
                        sb.AppendLine($"{horario}|{carpetaBase}{nombreArchivo}.mp3");
                    }
                }

                // Guardar archivo
                string nombreArchivo2 = $"{fecha:dd-MM-yy}{ciudad}{radio}.txt";
                string rutaCompleta = System.IO.Path.Combine(directorioPautas, nombreArchivo2);

                if (sb.Length > 0)
                {
                    System.IO.File.WriteAllText(rutaCompleta, sb.ToString(), Encoding.Default);
                    System.Diagnostics.Debug.WriteLine($"[REGENERAR] Archivo regenerado: {rutaCompleta}");
                }
                else
                {
                    // Si no hay comerciales, eliminar el archivo si existe
                    if (System.IO.File.Exists(rutaCompleta))
                    {
                        System.IO.File.Delete(rutaCompleta);
                        System.Diagnostics.Debug.WriteLine($"[REGENERAR] Archivo eliminado (sin comerciales): {rutaCompleta}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[REGENERAR] Error al regenerar archivo para {fecha:dd/MM/yyyy}: {ex.Message}");
            }
        }

        /// <summary>
        /// Regenera TODOS los archivos TXT para un rango de fechas completo.
        /// OPTIMIZADO: Hace UNA SOLA consulta masiva y genera todos los archivos en memoria.
        /// </summary>
        public async Task<int> RegenerarArchivosParaRangoAsync(
            DateTime fechaInicio, DateTime fechaFin,
            string ciudad, string radio,
            IProgress<(int porcentaje, string mensaje)> progress = null)
        {
            Logger.Log($"[REGENERAR-RANGO-OPT] Iniciando regeneración optimizada: {ciudad}/{radio} desde {fechaInicio:dd/MM/yyyy} hasta {fechaFin:dd/MM/yyyy}");

            int totalDias = (int)(fechaFin - fechaInicio).TotalDays + 1;
            int archivosGenerados = 0;

            try
            {
                progress?.Report((5, "Consultando base de datos..."));

                // 1. Obtener configuración
                TipoTanda tipoTanda = await ObtenerTipoTandaDesdeBDAsync(ciudad, radio);
                var horarios = TandasHorarias.GetHorarios(tipoTanda);
                string rutaBasePautas = ConfigManager.ObtenerRutaBasePautas();
                string directorioPautas = System.IO.Path.Combine(rutaBasePautas, ciudad.ToUpper());
                string carpetaBase = ConfigManager.ObtenerCarpetaRadio(radio);

                if (!System.IO.Directory.Exists(directorioPautas))
                {
                    System.IO.Directory.CreateDirectory(directorioPautas);
                }

                progress?.Report((10, "Obteniendo comerciales..."));

                // 2. Hacer UNA SOLA consulta masiva para obtener TODOS los comerciales del rango
                var comercialesPorFecha = await ObtenerComercialesParaRangoFechasAsync(
                    fechaInicio, fechaFin, ciudad, radio, tipoTanda, horarios);

                progress?.Report((30, $"Generando {totalDias} archivos..."));

                // 3. Generar archivos en paralelo (sin consultas adicionales)
                string separator = new string('-', 80);
                int procesados = 0;
                object lockObj = new object();

                var fechas = new List<DateTime>();
                for (int i = 0; i < totalDias; i++)
                {
                    fechas.Add(fechaInicio.AddDays(i));
                }

                // Procesar en lotes de 20 archivos simultáneos
                var semaphore = new System.Threading.SemaphoreSlim(20);

                var tasks = fechas.Select(async fecha =>
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        string claveFecha = fecha.ToString("yyyy-MM-dd");
                        var sb = new StringBuilder();
                        string horarioAnterior = "";

                        if (comercialesPorFecha.TryGetValue(claveFecha, out var comercialesDelDia))
                        {
                            // Agrupar por hora (string) y ordenar por hora cronológicamente
                            var porHora = comercialesDelDia
                                .GroupBy(c => c.Hora)
                                .OrderBy(g => g.Key); // Las horas "HH:MM" se ordenan correctamente alfabéticamente

                            foreach (var grupoHora in porHora)
                            {
                                string horario = grupoHora.Key;
                                if (!string.IsNullOrEmpty(horario))
                                {
                                    var comercialesOrdenados = grupoHora.OrderBy(c => ParsearPosicion(c.Posicion)).ToList();

                                    foreach (var com in comercialesOrdenados)
                                    {
                                        if (horario != horarioAnterior)
                                        {
                                            sb.AppendLine(separator);
                                            horarioAnterior = horario;
                                        }
                                        sb.AppendLine($"{horario}|{carpetaBase}{com.NombreArchivo}.mp3");
                                    }
                                }
                            }
                        }

                        // Guardar archivo
                        string nombreArchivo = $"{fecha:dd-MM-yy}{ciudad}{radio}.txt";
                        string rutaCompleta = System.IO.Path.Combine(directorioPautas, nombreArchivo);

                        if (sb.Length > 0)
                        {
                            System.IO.File.WriteAllText(rutaCompleta, sb.ToString(), Encoding.Default);
                            lock (lockObj) { archivosGenerados++; }
                        }
                        else if (System.IO.File.Exists(rutaCompleta))
                        {
                            System.IO.File.Delete(rutaCompleta);
                        }

                        lock (lockObj)
                        {
                            procesados++;
                            int porcentaje = 30 + (int)(procesados * 70.0 / totalDias);
                            progress?.Report((porcentaje, $"Escribiendo archivos... ({procesados}/{totalDias})"));
                        }
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });

                await Task.WhenAll(tasks);

                Logger.Log($"[REGENERAR-RANGO-OPT] Completado: {archivosGenerados} archivos generados");
            }
            catch (Exception ex)
            {
                Logger.Log($"[REGENERAR-RANGO-OPT] Error: {ex.Message}");
            }

            return archivosGenerados;
        }

        private DateTime ExtraerFechaDeHeader(string headerText)
        {
            // El headerText tiene formato: "Lunes\n02/01/2026"
            // Extraer la línea con la fecha
            string[] lineas = headerText.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            if (lineas.Length >= 2)
            {
                string fechaStr = lineas[1]; // "02/01/2026"
                if (DateTime.TryParseExact(fechaStr, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime fecha))
                {
                    return fecha;
                }
            }

            // Si falla, retornar fecha actual
            return DateTime.Now;
        }

        private string ExtraerNombreComercial(string valorCelda)
        {
            // El valorCelda puede tener formato:
            // - "P01 CHINCHA-PROMO LA KALLE OCT 2025" (cargado de BD)
            // - "P04 CLARO SUR ENE 2026.mp3" (asignado manualmente, incluye extension)
            // Necesitamos quitar "P01 " del inicio y la extension ".mp3" si existe

            string resultado = valorCelda;

            // 1. Quitar prefijo "P01 " si existe
            if (resultado.Length > 4 && resultado.StartsWith("P"))
            {
                // Buscar el primer espacio después de "P01"
                int indiceEspacio = resultado.IndexOf(' ');
                if (indiceEspacio >= 0 && indiceEspacio < resultado.Length - 1)
                {
                    resultado = resultado.Substring(indiceEspacio + 1).Trim();
                }
            }

            // 2. Quitar extension ".mp3" si existe (case-insensitive)
            if (resultado.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase))
            {
                resultado = resultado.Substring(0, resultado.Length - 4);
            }

            return resultado;
        }

        private int ObtenerRepeticiones(string nombreComercial)
        {
            // Cada comercial aparece solo 1 vez en la pauta
            // La cantidad de veces que se reproduce se controla desde Jazler
            return 1;
        }

        /// <summary>
        /// Extrae la clave de secuencia de un nombre de archivo.
        /// Si el nombre termina en un número (01, 02, 03, etc.), retorna el nombre base sin ese número.
        /// Ejemplos:
        /// - "SECUENCIA PAPILLON KARIBEÑA-2025 03" -> "SECUENCIA PAPILLON KARIBEÑA-2025"
        /// - "TACNA-DAYPER FEB 2025 01" -> "TACNA-DAYPER FEB 2025"
        /// - "CONECTOR SATELITE - TACNA" -> null (no es secuencia)
        /// </summary>
        private string ExtraerClaveSecuencia(string nombreArchivo)
        {
            if (string.IsNullOrEmpty(nombreArchivo))
                return null;

            // Buscar patrón: termina en espacio + número de 1-2 dígitos
            // Regex: termina en " 01", " 02", " 1", " 2", etc.
            string trimmed = nombreArchivo.Trim();

            // Verificar si termina en espacio + número
            int ultimoEspacio = trimmed.LastIndexOf(' ');
            if (ultimoEspacio > 0 && ultimoEspacio < trimmed.Length - 1)
            {
                string posibleNumero = trimmed.Substring(ultimoEspacio + 1);

                // Verificar si es un número de 1 o 2 dígitos
                if (posibleNumero.Length <= 2 && int.TryParse(posibleNumero, out int num) && num >= 1 && num <= 99)
                {
                    // Es una secuencia: retornar el nombre base
                    return trimmed.Substring(0, ultimoEspacio).Trim();
                }
            }

            // No es parte de una secuencia
            return null;
        }

        /// <summary>
        /// Parsea la posición de un comercial a número entero.
        /// Maneja tanto formato "P00" (con prefijo P) como "00" (sin prefijo).
        /// </summary>
        private int ParsearPosicion(string posicion)
        {
            if (string.IsNullOrEmpty(posicion))
                return 99;

            // Si empieza con "P", quitar el prefijo
            string posicionLimpia = posicion;
            if (posicion.StartsWith("P", StringComparison.OrdinalIgnoreCase))
            {
                posicionLimpia = posicion.Substring(1);
            }

            // Intentar parsear como número
            if (int.TryParse(posicionLimpia, out int pos))
            {
                return pos;
            }

            // Si no se puede parsear, retornar 99 (al final)
            return 99;
        }

        /// <summary>
        /// Genera pautas directamente desde la BD para una ciudad y radio específicos.
        /// Consulta todos los comerciales activos y genera los archivos de tanda.
        /// </summary>
        public async Task<string> GenerarPautaDesdeBaseDeDatosAsync(string ciudad, string radio, IProgress<int> progress = null)
        {
            entradasAgregadas.Clear();

            // Obtener carpeta base según la radio (desde config.ini)
            string carpetaBase = ConfigManager.ObtenerCarpetaRadio(radio);

            // Crear carpeta de pautas si no existe: RUTA_BASE/CIUDAD/RADIO/
            // Usa la ruta de red si esta configurada en config.ini, sino la carpeta local
            string rutaBasePautas = ConfigManager.ObtenerRutaBasePautas();
            string carpetaPautas = System.IO.Path.Combine(rutaBasePautas, ciudad.ToUpper());
            if (!System.IO.Directory.Exists(carpetaPautas))
            {
                System.IO.Directory.CreateDirectory(carpetaPautas);
            }

            List<string> archivosGenerados = new List<string>();
            var fechasConComerciales = new Dictionary<DateTime, List<(string Hora, string FilePath, string Posicion)>>();

            // Obtener el TipoProgramacion desde la BD para convertir fila a hora correctamente
            TipoTanda tipoTanda = await ObtenerTipoTandaDesdeBDAsync(ciudad, radio);
            var horarios = TandasHorarias.GetHorarios(tipoTanda);

            try
            {
                using (var conn = new NpgsqlConnection(DatabaseConfig.ConnectionString))
                {
                    await conn.OpenAsync();

                    // Obtener todas las fechas y horas con comerciales para esta ciudad y radio
                    string query = @"
                        SELECT DISTINCT
                            c.FechaInicio,
                            c.FechaFinal,
                            ca.Fila,
                            c.FilePath,
                            c.Posicion
                        FROM ComercialesAsignados ca
                        INNER JOIN Comerciales c ON ca.Codigo = c.Codigo
                        WHERE c.Ciudad = @Ciudad
                          AND c.Radio = @Radio
                          AND c.Estado = 'Activo'
                        ORDER BY c.Posicion ASC";

                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Ciudad", ciudad);
                        cmd.Parameters.AddWithValue("@Radio", radio);

                        using (var reader = (NpgsqlDataReader)await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                DateTime fechaInicio = reader.GetDateTime(0);
                                DateTime fechaFinal = reader.GetDateTime(1);
                                int fila = reader.GetInt32(2);
                                string filePath = reader["FilePath"].ToString();
                                string posicion = reader["Posicion"].ToString();

                                // Obtener la hora según la fila usando el TipoProgramacion de la BD
                                string horaStr;
                                if (fila >= 0 && fila < horarios.Length)
                                {
                                    horaStr = horarios[fila]; // Ya viene en formato "00:00", "00:30", etc.
                                }
                                else
                                {
                                    // Fallback: calcular basado en 48 tandas (00-30)
                                    int hora = fila / 2;
                                    int minuto = (fila % 2) * 30;
                                    horaStr = $"{hora:D2}:{minuto:D2}";
                                }

                                // Agregar para cada fecha en el rango
                                for (DateTime fecha = fechaInicio.Date; fecha <= fechaFinal.Date; fecha = fecha.AddDays(1))
                                {
                                    if (!fechasConComerciales.ContainsKey(fecha))
                                    {
                                        fechasConComerciales[fecha] = new List<(string, string, string)>();
                                    }
                                    fechasConComerciales[fecha].Add((horaStr, filePath, posicion));
                                }
                            }
                        }
                    }
                }

                // Generar un archivo por cada fecha
                int totalFechas = fechasConComerciales.Count;
                int fechaProcesada = 0;

                foreach (var kvp in fechasConComerciales.OrderBy(x => x.Key))
                {
                    DateTime fecha = kvp.Key;
                    var comerciales = kvp.Value;

                    string nombreArchivo = $"{fecha:dd-MM-yy}{ciudad}{radio}.txt";
                    string rutaArchivo = System.IO.Path.Combine(carpetaPautas, nombreArchivo);

                    // Construir contenido del archivo
                    var contenido = new StringBuilder();
                    var comercialesUnicos = new HashSet<string>();

                    // Agrupar por hora y ordenar por posición
                    var comercialesPorHora = comerciales
                        .GroupBy(c => c.Hora)
                        .OrderBy(g => g.Key);

                    foreach (var grupo in comercialesPorHora)
                    {
                        string hora = grupo.Key;
                        foreach (var comercial in grupo.OrderBy(c => c.Posicion))
                        {
                            string clave = $"{hora}|{comercial.FilePath}";
                            if (!comercialesUnicos.Contains(clave))
                            {
                                comercialesUnicos.Add(clave);
                                string rutaCompleta = construirRutaComercial(comercial.FilePath, carpetaBase);
                                contenido.AppendLine($"{hora}|{rutaCompleta}");
                            }
                        }
                    }

                    // Guardar archivo
                    if (contenido.Length > 0)
                    {
                        System.IO.File.WriteAllText(rutaArchivo, contenido.ToString(), Encoding.Default);
                        archivosGenerados.Add(rutaArchivo);
                    }

                    fechaProcesada++;
                    progress?.Report((int)((fechaProcesada * 100.0) / totalFechas));
                }
            }
            catch (Exception ex)
            {
                return $"{carpetaPautas}|Error al generar pautas: {ex.Message}";
            }

            return $"{carpetaPautas}|Se generaron {archivosGenerados.Count} archivos de pautas para {ciudad} - {radio}.";
        }

        /// <summary>
        /// Genera pautas SOLO para un comercial específico (por su código).
        /// Genera archivos para cada fecha dentro del rango de vigencia del comercial.
        /// </summary>
        public async Task<string> GenerarPautaPorCodigoAsync(string codigo, string ciudad, string radio, IProgress<int> progress = null)
        {
            entradasAgregadas.Clear();

            // Obtener carpeta base según la radio (desde config.ini)
            string carpetaBase = ConfigManager.ObtenerCarpetaRadio(radio);

            // Crear carpeta de pautas si no existe: RUTA_BASE/CIUDAD/RADIO/
            string rutaBasePautas = ConfigManager.ObtenerRutaBasePautas();
            string carpetaPautas = System.IO.Path.Combine(rutaBasePautas, ciudad.ToUpper());
            if (!System.IO.Directory.Exists(carpetaPautas))
            {
                System.IO.Directory.CreateDirectory(carpetaPautas);
            }

            List<string> archivosGenerados = new List<string>();
            var fechasConComerciales = new Dictionary<DateTime, List<(string Hora, string FilePath, string Posicion)>>();

            // Obtener el TipoProgramacion desde la BD para convertir fila a hora correctamente
            TipoTanda tipoTanda = await ObtenerTipoTandaDesdeBDAsync(ciudad, radio);
            var horarios = TandasHorarias.GetHorarios(tipoTanda);

            try
            {
                using (var conn = new NpgsqlConnection(DatabaseConfig.ConnectionString))
                {
                    await conn.OpenAsync();

                    // Obtener datos SOLO del comercial específico por su código
                    string query = @"
                        SELECT
                            c.FechaInicio,
                            c.FechaFinal,
                            ca.Fila,
                            c.FilePath,
                            c.Posicion
                        FROM ComercialesAsignados ca
                        INNER JOIN Comerciales c ON ca.Codigo = c.Codigo
                        WHERE c.Codigo = @Codigo
                        ORDER BY c.Posicion ASC";

                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Codigo", codigo);

                        using (var reader = (NpgsqlDataReader)await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                DateTime fechaInicio = reader.GetDateTime(0);
                                DateTime fechaFinal = reader.GetDateTime(1);
                                int fila = reader.GetInt32(2);
                                string filePath = reader["FilePath"].ToString();
                                string posicion = reader["Posicion"].ToString();

                                // Obtener la hora según la fila usando el TipoProgramacion de la BD
                                string horaStr;
                                if (fila >= 0 && fila < horarios.Length)
                                {
                                    horaStr = horarios[fila]; // Ya viene en formato "00:00", "00:30", etc.
                                }
                                else
                                {
                                    // Fallback: calcular basado en 48 tandas (00-30)
                                    int hora = fila / 2;
                                    int minuto = (fila % 2) * 30;
                                    horaStr = $"{hora:D2}:{minuto:D2}";
                                }

                                // Agregar para cada fecha en el rango
                                for (DateTime fecha = fechaInicio.Date; fecha <= fechaFinal.Date; fecha = fecha.AddDays(1))
                                {
                                    if (!fechasConComerciales.ContainsKey(fecha))
                                    {
                                        fechasConComerciales[fecha] = new List<(string, string, string)>();
                                    }
                                    fechasConComerciales[fecha].Add((horaStr, filePath, posicion));
                                }
                            }
                        }
                    }
                }

                if (fechasConComerciales.Count == 0)
                {
                    return $"{carpetaPautas}|No se encontraron asignaciones horarias para el comercial {codigo}.";
                }

                // Generar un archivo por cada fecha
                int totalFechas = fechasConComerciales.Count;
                int fechaProcesada = 0;

                foreach (var kvp in fechasConComerciales.OrderBy(x => x.Key))
                {
                    DateTime fecha = kvp.Key;
                    var comerciales = kvp.Value;

                    string nombreArchivo = $"{fecha:dd-MM-yy}{ciudad}{radio}.txt";
                    string rutaArchivo = System.IO.Path.Combine(carpetaPautas, nombreArchivo);

                    // Construir contenido del archivo
                    var contenido = new StringBuilder();
                    var comercialesUnicos = new HashSet<string>();

                    // Agrupar por hora y ordenar por posición
                    var comercialesPorHora = comerciales
                        .GroupBy(c => c.Hora)
                        .OrderBy(g => g.Key);

                    foreach (var grupo in comercialesPorHora)
                    {
                        string hora = grupo.Key;
                        foreach (var comercial in grupo.OrderBy(c => c.Posicion))
                        {
                            string clave = $"{hora}|{comercial.FilePath}";
                            if (!comercialesUnicos.Contains(clave))
                            {
                                comercialesUnicos.Add(clave);
                                string rutaCompleta = construirRutaComercial(comercial.FilePath, carpetaBase);
                                contenido.AppendLine($"{hora}|{rutaCompleta}");
                            }
                        }
                    }

                    // Guardar archivo
                    if (contenido.Length > 0)
                    {
                        System.IO.File.WriteAllText(rutaArchivo, contenido.ToString(), Encoding.Default);
                        archivosGenerados.Add(rutaArchivo);
                    }

                    fechaProcesada++;
                    progress?.Report((int)((fechaProcesada * 100.0) / totalFechas));
                }
            }
            catch (Exception ex)
            {
                return $"{carpetaPautas}|Error al generar pautas: {ex.Message}";
            }

            return $"{carpetaPautas}|Se generaron {archivosGenerados.Count} archivos de pautas para el comercial {codigo}.";
        }

        /// <summary>
        /// Obtiene el TipoTanda desde la BD consultando el TipoProgramacion guardado para la ciudad y radio.
        /// IMPORTANTE: Si hay comerciales con diferentes TipoProgramacion (48 y 96 tandas),
        /// se usa el de 96 tandas para que el TXT contenga TODAS las horas posibles.
        /// IMPORTANTE: Si hay comerciales con "Importado Access", usa detección por radio.
        /// </summary>
        private async Task<TipoTanda> ObtenerTipoTandaDesdeBDAsync(string ciudad, string radio)
        {
            TipoTanda tipoRadio = DetectarTipoProgramacionPorRadio(radio);
            if (tipoRadio == TipoTanda.Tandas_00_20_30_50)
            {
                return TipoTanda.Tandas_00_20_30_50;
            }

            try
            {
                using (var conn = new NpgsqlConnection(DatabaseConfig.ConnectionString))
                {
                    await conn.OpenAsync();

                    // Obtener TODOS los TipoProgramacion distintos para esta ciudad y radio
                    string query = @"
                        SELECT DISTINCT TipoProgramacion
                        FROM Comerciales
                        WHERE LOWER(Ciudad) = LOWER(@Ciudad)
                          AND LOWER(Radio) = LOWER(@Radio)
                          AND Estado = 'Activo'
                          AND TipoProgramacion IS NOT NULL
                          AND TipoProgramacion <> ''";

                    var tiposEncontrados = new List<string>();

                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Ciudad", ciudad);
                        cmd.Parameters.AddWithValue("@Radio", radio);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                tiposEncontrados.Add(reader.GetString(0));
                            }
                        }
                    }

                    var tiposTanda = new HashSet<TipoTanda>();
                    foreach (string tipoProgramacion in tiposEncontrados)
                    {
                        TipoTanda? tipo = ConvertirTipoProgramacionATipoTanda(tipoProgramacion);
                        if (tipo.HasValue)
                        {
                            tiposTanda.Add(tipo.Value);
                        }
                    }

                    if (tiposTanda.Contains(TipoTanda.Tandas_00_20_30_50))
                    {
                        return TipoTanda.Tandas_00_20_30_50;
                    }

                    if (tiposTanda.Count > 1)
                    {
                        return TipoTanda.Tandas_00_20_30_50;
                    }

                    if (tiposTanda.Count == 1)
                    {
                        return tiposTanda.First();
                    }
                }
            }
            catch (Exception)
            {
                // Usar fallback por radio
            }

            return tipoRadio;
        }

        /// <summary>
        /// Convierte el string de TipoProgramacion (guardado en BD) a TipoTanda enum.
        /// Retorna null si no reconoce el formato para que se use el fallback por radio.
        /// </summary>
        private TipoTanda? ConvertirTipoProgramacionATipoTanda(string tipoProgramacion)
        {
            if (string.IsNullOrEmpty(tipoProgramacion))
                return null;

            // Manejar diferentes formatos posibles
            // IMPORTANTE: El orden importa - patrones más específicos primero
            string tipoUpper = tipoProgramacion.ToUpper();

            // 96 tandas: "Cada 00-20-30-50 (96 tandas)" o similar
            if (tipoUpper.Contains("00-20-30-50") || tipoUpper.Contains("96 TANDAS"))
            {
                return TipoTanda.Tandas_00_20_30_50;
            }
            // 48 tandas formato 20-50: "Cada 20-50 (48 tandas)" o similar
            // NOTA: Debe evaluarse ANTES de 00-30 porque ambos son 48 tandas
            else if (tipoUpper.Contains("20-50"))
            {
                return TipoTanda.Tandas_20_50;
            }
            // 48 tandas formato 10-40
            else if (tipoUpper.Contains("10-40"))
            {
                return TipoTanda.Tandas_10_40;
            }
            // 48 tandas formato 15-45
            else if (tipoUpper.Contains("15-45"))
            {
                return TipoTanda.Tandas_15_45;
            }
            // 48 tandas formato 00-30: "Cada 00-30 (48 tandas)" o similar (formato por defecto)
            else if (tipoUpper.Contains("00-30"))
            {
                return TipoTanda.Tandas_00_30;
            }

            // No reconocido - retornar null para usar fallback por radio
            return null;
        }

        /// <summary>
        /// Convierte una fila (índice) a hora formateada "HH:MM" según el TipoProgramacion del comercial.
        /// Esto es necesario porque diferentes comerciales pueden tener diferentes TipoProgramacion
        /// y sus filas representan diferentes horas.
        /// </summary>
        private string ConvertirFilaAHora(int fila, string tipoProgramacion)
        {
            // Obtener el TipoTanda del comercial
            TipoTanda? tipoTanda = ConvertirTipoProgramacionATipoTanda(tipoProgramacion);

            // Si no se reconoce el TipoProgramacion, asumir 96 tandas para radios tipo KARIBEÑA
            // ya que "Importado Access" suele ser de estas radios
            if (!tipoTanda.HasValue)
            {
                // Si el TipoProgramacion no es reconocido (como "Importado Access"),
                // usar 96 tandas como fallback más seguro para no perder horas
                tipoTanda = TipoTanda.Tandas_00_20_30_50;
            }

            // Obtener el array de horarios para este tipo de tanda
            string[] horarios = TandasHorarias.GetHorarios(tipoTanda.Value);

            if (fila >= 0 && fila < horarios.Length)
            {
                return horarios[fila]; // Retorna "HH:MM"
            }

            return "00:00";
        }

        /// <summary>
        /// Detecta el tipo de tanda basándose en el nombre de la radio.
        /// KARIBEÑA y LA KALLE usan 4 tandas por hora (00, 20, 30, 50).
        /// Las demás radios usan 2 tandas por hora (00, 30).
        /// NOTA: Este método es solo un fallback. Se prefiere usar ObtenerTipoTandaDesdeBDAsync.
        /// </summary>
        private TipoTanda DetectarTipoProgramacionPorRadio(string radio)
        {
            if (string.IsNullOrEmpty(radio))
                return TipoTanda.Tandas_00_30;

            string radioUpper = radio.ToUpper();

            // KARIBEÑA y LA KALLE usan las 4 tandas: 00, 20, 30, 50
            // Incluir variantes de codificación: KARIBEÑA, KARIBENA, KARIBEÃA (UTF-8 mal interpretado)
            if (radioUpper.Contains("KARIBEÑA") || radioUpper.Contains("KARIBENA") ||
                radioUpper.Contains("KARIBEÃ") || radioUpper.Contains("KARIBE") ||
                radioUpper.Contains("LAKALLE") || radioUpper.Contains("LA KALLE") || radioUpper.Contains("KALLE"))
            {
                return TipoTanda.Tandas_00_20_30_50;
            }

            // EXITOSA y otros usan 00-30 por defecto
            return TipoTanda.Tandas_00_30;
        }
    }

    /// <summary>
    /// Clase auxiliar para almacenar datos de comerciales para la pauta
    /// </summary>
    public class ComercialParaPauta
    {
        public string ComercialAsignado { get; set; }
        public string FilePath { get; set; }
        public string Posicion { get; set; }
        public string Codigo { get; set; }

        public string NombreComercial
        {
            get
            {
                // Extraer nombre del archivo sin ruta
                return System.IO.Path.GetFileNameWithoutExtension(FilePath);
            }
        }
    }
}

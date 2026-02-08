using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Generador_Pautas
{
    /// <summary>
    /// Clase para analizar y mostrar la estructura de una base de datos Access
    /// </summary>
    public class AccessAnalyzer
    {
        private string _connectionString;
        private string _dbPath;

        public AccessAnalyzer(string dbPath)
        {
            _dbPath = dbPath;
            // Conexión para Access 2003 (.mdb)
            _connectionString = $"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={dbPath};Persist Security Info=False;";
        }

        /// <summary>
        /// Obtiene la lista de todas las tablas en la base de datos
        /// </summary>
        public List<string> ObtenerTablas()
        {
            var tablas = new List<string>();

            using (OleDbConnection conn = new OleDbConnection(_connectionString))
            {
                conn.Open();

                // Obtener esquema de tablas
                DataTable schema = conn.GetOleDbSchemaTable(OleDbSchemaGuid.Tables,
                    new object[] { null, null, null, "TABLE" });

                foreach (DataRow row in schema.Rows)
                {
                    string tableName = row["TABLE_NAME"].ToString();
                    // Ignorar tablas del sistema
                    if (!tableName.StartsWith("MSys"))
                    {
                        tablas.Add(tableName);
                    }
                }
            }

            return tablas;
        }

        /// <summary>
        /// Obtiene la estructura (columnas) de una tabla específica
        /// </summary>
        public DataTable ObtenerEstructuraTabla(string tableName)
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("Columna", typeof(string));
            dt.Columns.Add("Tipo", typeof(string));
            dt.Columns.Add("Tamaño", typeof(int));
            dt.Columns.Add("Nullable", typeof(bool));

            using (OleDbConnection conn = new OleDbConnection(_connectionString))
            {
                conn.Open();

                DataTable schema = conn.GetOleDbSchemaTable(OleDbSchemaGuid.Columns,
                    new object[] { null, null, tableName, null });

                foreach (DataRow row in schema.Rows)
                {
                    dt.Rows.Add(
                        row["COLUMN_NAME"].ToString(),
                        ConvertirTipoOleDb((int)row["DATA_TYPE"]),
                        row["CHARACTER_MAXIMUM_LENGTH"] != DBNull.Value ? Convert.ToInt32(row["CHARACTER_MAXIMUM_LENGTH"]) : 0,
                        row["IS_NULLABLE"].ToString() == "YES"
                    );
                }
            }

            return dt;
        }

        /// <summary>
        /// Obtiene una muestra de datos de una tabla
        /// </summary>
        public DataTable ObtenerMuestraDatos(string tableName, int limite = 100)
        {
            DataTable dt = new DataTable();

            using (OleDbConnection conn = new OleDbConnection(_connectionString))
            {
                conn.Open();

                string query = $"SELECT TOP {limite} * FROM [{tableName}]";
                using (OleDbCommand cmd = new OleDbCommand(query, conn))
                using (OleDbDataAdapter adapter = new OleDbDataAdapter(cmd))
                {
                    adapter.Fill(dt);
                }
            }

            return dt;
        }

        /// <summary>
        /// Cuenta el total de registros en una tabla
        /// </summary>
        public int ContarRegistros(string tableName)
        {
            using (OleDbConnection conn = new OleDbConnection(_connectionString))
            {
                conn.Open();

                string query = $"SELECT COUNT(*) FROM [{tableName}]";
                using (OleDbCommand cmd = new OleDbCommand(query, conn))
                {
                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }

        /// <summary>
        /// Genera un reporte completo de la base de datos
        /// </summary>
        public string GenerarReporteCompleto()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine($"  ANÁLISIS DE BASE DE DATOS ACCESS");
            sb.AppendLine($"  Archivo: {Path.GetFileName(_dbPath)}");
            sb.AppendLine($"  Fecha: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine();

            try
            {
                var tablas = ObtenerTablas();
                sb.AppendLine($"TABLAS ENCONTRADAS: {tablas.Count}");
                sb.AppendLine("───────────────────────────────────────────────────────────────");

                foreach (string tabla in tablas)
                {
                    int registros = ContarRegistros(tabla);
                    sb.AppendLine($"\n▶ TABLA: {tabla} ({registros:N0} registros)");
                    sb.AppendLine("  ┌─────────────────────────────────────────────────────────┐");

                    var estructura = ObtenerEstructuraTabla(tabla);
                    foreach (DataRow row in estructura.Rows)
                    {
                        string columna = row["Columna"].ToString().PadRight(25);
                        string tipo = row["Tipo"].ToString().PadRight(15);
                        int tamaño = Convert.ToInt32(row["Tamaño"]);
                        bool nullable = Convert.ToBoolean(row["Nullable"]);

                        string tamañoStr = tamaño > 0 ? $"({tamaño})" : "";
                        string nullStr = nullable ? "NULL" : "NOT NULL";

                        sb.AppendLine($"  │ {columna} {tipo}{tamañoStr.PadRight(8)} {nullStr}");
                    }
                    sb.AppendLine("  └─────────────────────────────────────────────────────────┘");

                    // Mostrar muestra de datos (primeras 3 filas)
                    if (registros > 0)
                    {
                        var muestra = ObtenerMuestraDatos(tabla, 3);
                        sb.AppendLine("  Muestra de datos:");
                        foreach (DataRow dataRow in muestra.Rows)
                        {
                            sb.Append("    → ");
                            for (int i = 0; i < Math.Min(4, muestra.Columns.Count); i++)
                            {
                                string valor = dataRow[i]?.ToString() ?? "NULL";
                                if (valor.Length > 30) valor = valor.Substring(0, 27) + "...";
                                sb.Append($"{muestra.Columns[i].ColumnName}={valor}; ");
                            }
                            sb.AppendLine();
                        }
                    }
                }

                sb.AppendLine("\n═══════════════════════════════════════════════════════════════");
                sb.AppendLine("  FIN DEL ANÁLISIS");
                sb.AppendLine("═══════════════════════════════════════════════════════════════");
            }
            catch (Exception ex)
            {
                sb.AppendLine($"ERROR: {ex.Message}");
                sb.AppendLine($"Stack: {ex.StackTrace}");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Genera un análisis detallado del flujo del sistema PAUTA
        /// </summary>
        public string GenerarAnalisisDetallado()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("═══════════════════════════════════════════════════════════════════════════");
            sb.AppendLine("  ANÁLISIS DETALLADO DEL SISTEMA PAUTA (Access)");
            sb.AppendLine("═══════════════════════════════════════════════════════════════════════════");
            sb.AppendLine();

            try
            {
                using (OleDbConnection conn = new OleDbConnection(_connectionString))
                {
                    conn.Open();

                    // 1. Estadísticas generales de AVISOS
                    sb.AppendLine("▶ ESTADÍSTICAS GENERALES DE LA TABLA AVISOS");
                    sb.AppendLine("───────────────────────────────────────────────────────────────────────────");

                    // Total de registros
                    using (var cmd = new OleDbCommand("SELECT COUNT(*) FROM AVISOS", conn))
                    {
                        int total = Convert.ToInt32(cmd.ExecuteScalar());
                        sb.AppendLine($"  Total de registros: {total:N0}");
                    }

                    // Ciudades únicas con conteo de registros
                    sb.AppendLine("\n▶ CIUDADES EN EL SISTEMA (con conteo de registros)");
                    sb.AppendLine("───────────────────────────────────────────────────────────────────────────");
                    var ciudadesEnAccess = new List<(string Ciudad, int Registros)>();
                    using (var cmd = new OleDbCommand("SELECT Ciudad, COUNT(*) as Total FROM AVISOS GROUP BY Ciudad ORDER BY Ciudad", conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string ciudad = reader["Ciudad"]?.ToString()?.Trim() ?? "";
                            int total = Convert.ToInt32(reader["Total"]);
                            ciudadesEnAccess.Add((ciudad, total));
                            sb.AppendLine($"  • {ciudad,-30} ({total:N0} registros)");
                        }
                    }
                    sb.AppendLine($"  Total: {ciudadesEnAccess.Count} ciudades");

                    // Buscar ciudades problemáticas específicas
                    sb.AppendLine("\n▶ BÚSQUEDA DE CIUDADES ESPECÍFICAS");
                    sb.AppendLine("───────────────────────────────────────────────────────────────────────────");
                    string[] ciudadesBuscar = { "JAUJA", "HUANCABAMBA", "HUANCAVELICA", "LOS ORGANOS", "MOLLENDO", "PAITA" };
                    foreach (string ciudadBuscar in ciudadesBuscar)
                    {
                        // Buscar coincidencia exacta
                        var exacta = ciudadesEnAccess.FirstOrDefault(c => c.Ciudad.Equals(ciudadBuscar, StringComparison.OrdinalIgnoreCase));
                        // Buscar coincidencia parcial
                        var parciales = ciudadesEnAccess.Where(c => c.Ciudad.ToUpper().Contains(ciudadBuscar.ToUpper().Substring(0, Math.Min(4, ciudadBuscar.Length)))).ToList();

                        if (exacta.Ciudad != null)
                        {
                            sb.AppendLine($"  ✓ {ciudadBuscar}: ENCONTRADA como '{exacta.Ciudad}' ({exacta.Registros:N0} registros)");
                        }
                        else if (parciales.Count > 0)
                        {
                            sb.AppendLine($"  ⚠ {ciudadBuscar}: NO EXACTA, pero similar a:");
                            foreach (var p in parciales)
                            {
                                sb.AppendLine($"      → '{p.Ciudad}' ({p.Registros:N0} registros)");
                            }
                        }
                        else
                        {
                            sb.AppendLine($"  ✗ {ciudadBuscar}: NO ENCONTRADA en Access");
                        }
                    }

                    // Medios (Radios) únicos
                    sb.AppendLine("\n▶ MEDIOS/RADIOS EN EL SISTEMA");
                    sb.AppendLine("───────────────────────────────────────────────────────────────────────────");
                    using (var cmd = new OleDbCommand("SELECT DISTINCT Medio FROM AVISOS ORDER BY Medio", conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        int count = 0;
                        while (reader.Read())
                        {
                            sb.AppendLine($"  • {reader["Medio"]}");
                            count++;
                        }
                        sb.AppendLine($"  Total: {count} medios/radios");
                    }

                    // Horas disponibles - agrupadas por tipo de tanda
                    sb.AppendLine("\n▶ HORAS DE PROGRAMACIÓN");
                    sb.AppendLine("───────────────────────────────────────────────────────────────────────────");

                    var horas00_30 = new List<string>();
                    var horas20_50 = new List<string>();
                    var horas10_40 = new List<string>();
                    var horas15_45 = new List<string>();
                    var horasOtras = new List<string>();

                    using (var cmd = new OleDbCommand("SELECT DISTINCT Hora FROM AVISOS ORDER BY Hora", conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string hora = reader["Hora"]?.ToString() ?? "";
                            if (hora.Contains(":"))
                            {
                                string[] partes = hora.Split(':');
                                if (partes.Length >= 2 && int.TryParse(partes[1], out int minutos))
                                {
                                    if (minutos == 0 || minutos == 30)
                                        horas00_30.Add(hora);
                                    else if (minutos == 20 || minutos == 50)
                                        horas20_50.Add(hora);
                                    else if (minutos == 10 || minutos == 40)
                                        horas10_40.Add(hora);
                                    else if (minutos == 15 || minutos == 45)
                                        horas15_45.Add(hora);
                                    else
                                        horasOtras.Add(hora);
                                }
                                else
                                    horasOtras.Add(hora);
                            }
                            else
                                horasOtras.Add(hora);
                        }
                    }

                    sb.AppendLine($"  TIPO 00-30 ({horas00_30.Count} horas): {string.Join(", ", horas00_30.Take(12))}...");
                    sb.AppendLine($"  TIPO 20-50 ({horas20_50.Count} horas): {string.Join(", ", horas20_50.Take(12))}...");
                    sb.AppendLine($"  TIPO 10-40 ({horas10_40.Count} horas): {string.Join(", ", horas10_40.Take(12))}...");
                    sb.AppendLine($"  TIPO 15-45 ({horas15_45.Count} horas): {string.Join(", ", horas15_45.Take(12))}...");
                    if (horasOtras.Count > 0)
                        sb.AppendLine($"  OTRAS ({horasOtras.Count} horas): {string.Join(", ", horasOtras)}");

                    int totalHoras = horas00_30.Count + horas20_50.Count + horas10_40.Count + horas15_45.Count + horasOtras.Count;
                    sb.AppendLine($"\n  Total: {totalHoras} horas únicas");

                    // Posiciones (prioridades)
                    sb.AppendLine("\n▶ POSICIONES/PRIORIDADES");
                    sb.AppendLine("───────────────────────────────────────────────────────────────────────────");
                    using (var cmd = new OleDbCommand("SELECT Pos, COUNT(*) as Total FROM AVISOS GROUP BY Pos ORDER BY Pos", conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            sb.AppendLine($"  Posición {reader["Pos"]}: {Convert.ToInt32(reader["Total"]):N0} registros");
                        }
                    }

                    // Rango de fechas
                    sb.AppendLine("\n▶ RANGO DE FECHAS");
                    sb.AppendLine("───────────────────────────────────────────────────────────────────────────");
                    using (var cmd = new OleDbCommand("SELECT MIN(FechaI) as MinI, MAX(FechaI) as MaxI, MIN(FechaF) as MinF, MAX(FechaF) as MaxF FROM AVISOS", conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            sb.AppendLine($"  FechaInicio: desde {reader["MinI"]:dd/MM/yyyy} hasta {reader["MaxI"]:dd/MM/yyyy}");
                            sb.AppendLine($"  FechaFinal:  desde {reader["MinF"]:dd/MM/yyyy} hasta {reader["MaxF"]:dd/MM/yyyy}");
                        }
                    }

                    // Muestra de rutas de archivos
                    sb.AppendLine("\n▶ EJEMPLOS DE RUTAS DE ARCHIVOS (MP3)");
                    sb.AppendLine("───────────────────────────────────────────────────────────────────────────");
                    using (var cmd = new OleDbCommand("SELECT DISTINCT TOP 15 Ruta FROM AVISOS", conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string ruta = reader["Ruta"]?.ToString() ?? "";
                            sb.AppendLine($"  {ruta}");
                        }
                    }

                    // Archivos únicos (Access no soporta COUNT(DISTINCT), usamos subquery)
                    sb.AppendLine("\n▶ ESTADÍSTICAS DE ARCHIVOS");
                    sb.AppendLine("───────────────────────────────────────────────────────────────────────────");
                    try
                    {
                        using (var cmd = new OleDbCommand("SELECT COUNT(*) FROM (SELECT DISTINCT Ruta FROM AVISOS)", conn))
                        {
                            int archivosUnicos = Convert.ToInt32(cmd.ExecuteScalar());
                            sb.AppendLine($"  Archivos MP3 únicos: {archivosUnicos:N0}");
                        }
                    }
                    catch
                    {
                        sb.AppendLine("  (No se pudo contar archivos únicos)");
                    }

                    // Días de la semana
                    sb.AppendLine("\n▶ PROGRAMACIÓN POR DÍAS DE LA SEMANA");
                    sb.AppendLine("───────────────────────────────────────────────────────────────────────────");
                    string[] dias = { "lun", "mar", "mie", "jue", "vie", "sab", "dom" };
                    string[] nombresD = { "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado", "Domingo" };
                    for (int i = 0; i < dias.Length; i++)
                    {
                        using (var cmd = new OleDbCommand($"SELECT COUNT(*) FROM AVISOS WHERE [{dias[i]}] = True", conn))
                        {
                            int count = Convert.ToInt32(cmd.ExecuteScalar());
                            sb.AppendLine($"  {nombresD[i].PadRight(12)}: {count:N0} registros activos");
                        }
                    }

                    // Estado CADUCO
                    sb.AppendLine("\n▶ ESTADO DE COMERCIALES (CADUCO)");
                    sb.AppendLine("───────────────────────────────────────────────────────────────────────────");
                    using (var cmd = new OleDbCommand("SELECT CADUCO, COUNT(*) as Total FROM AVISOS GROUP BY CADUCO", conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            bool caduco = Convert.ToBoolean(reader["CADUCO"]);
                            int total = Convert.ToInt32(reader["Total"]);
                            sb.AppendLine($"  {(caduco ? "Caducados" : "Vigentes")}: {total:N0} registros");
                        }
                    }

                    // Muestra completa de registros
                    sb.AppendLine("\n▶ MUESTRA DE REGISTROS COMPLETOS (10 registros)");
                    sb.AppendLine("───────────────────────────────────────────────────────────────────────────");
                    using (var cmd = new OleDbCommand("SELECT TOP 10 * FROM AVISOS ORDER BY codigo DESC", conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        int num = 1;
                        while (reader.Read())
                        {
                            sb.AppendLine($"\n  [{num}] Código: {reader["codigo"]} | Código_avi: {reader["Codigo_avi"]}");
                            sb.AppendLine($"      Ciudad: {reader["Ciudad"]} | Medio: {reader["Medio"]}");
                            sb.AppendLine($"      Hora: {reader["Hora"]} | Posición: {reader["Pos"]}");
                            sb.AppendLine($"      Fechas: {Convert.ToDateTime(reader["FechaI"]):dd/MM/yyyy} - {Convert.ToDateTime(reader["FechaF"]):dd/MM/yyyy}");
                            sb.AppendLine($"      Ruta: {reader["Ruta"]}");
                            sb.AppendLine($"      Días: L={reader["lun"]} M={reader["mar"]} X={reader["mie"]} J={reader["jue"]} V={reader["vie"]} S={reader["sab"]} D={reader["dom"]}");
                            sb.AppendLine($"      Caduco: {reader["CADUCO"]} | Motivo: {reader["MOTIVO"]}");
                            num++;
                        }
                    }

                    // Relación entre Ciudad y Medio
                    sb.AppendLine("\n▶ RELACIÓN CIUDAD - MEDIO (Top 20)");
                    sb.AppendLine("───────────────────────────────────────────────────────────────────────────");
                    using (var cmd = new OleDbCommand("SELECT TOP 20 Ciudad, Medio, COUNT(*) as Total FROM AVISOS GROUP BY Ciudad, Medio ORDER BY Ciudad, Medio", conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            sb.AppendLine($"  {reader["Ciudad"],-20} → {reader["Medio"],-25} ({Convert.ToInt32(reader["Total"]):N0} registros)");
                        }
                    }
                }

                sb.AppendLine("\n═══════════════════════════════════════════════════════════════════════════");
                sb.AppendLine("  MAPEO SUGERIDO PARA IMPORTACIÓN");
                sb.AppendLine("═══════════════════════════════════════════════════════════════════════════");
                sb.AppendLine();
                sb.AppendLine("  Access (AVISOS)          →  PostgreSQL (Comerciales)");
                sb.AppendLine("  ─────────────────────────────────────────────────────────────");
                sb.AppendLine("  codigo                   →  Codigo (generar nuevo o usar)");
                sb.AppendLine("  Ruta                     →  FilePath");
                sb.AppendLine("  FechaI                   →  FechaInicio");
                sb.AppendLine("  FechaF                   →  FechaFinal");
                sb.AppendLine("  Ciudad                   →  Ciudad");
                sb.AppendLine("  Medio                    →  Radio");
                sb.AppendLine("  Pos                      →  Posicion (convertir a P01, P02...)");
                sb.AppendLine("  CADUCO                   →  Estado (False→Activo, True→Inactivo)");
                sb.AppendLine("  Hora                     →  (para ComercialesAsignados.Fila)");
                sb.AppendLine("  lun,mar,mie,jue,vie...   →  (para generar pautas por día)");
                sb.AppendLine();
                sb.AppendLine("═══════════════════════════════════════════════════════════════════════════");
            }
            catch (Exception ex)
            {
                sb.AppendLine($"ERROR: {ex.Message}");
                sb.AppendLine($"Stack: {ex.StackTrace}");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Convierte código de tipo OleDb a nombre legible
        /// </summary>
        private string ConvertirTipoOleDb(int oleDbType)
        {
            switch (oleDbType)
            {
                case 2: return "SmallInt";
                case 3: return "Integer";
                case 4: return "Single";
                case 5: return "Double";
                case 6: return "Currency";
                case 7: return "Date";
                case 11: return "Boolean";
                case 17: return "Byte";
                case 72: return "GUID";
                case 128: return "Binary";
                case 129: return "Char";
                case 130: return "WChar/Text";
                case 131: return "Decimal";
                case 200: return "VarChar";
                case 201: return "LongVarChar";
                case 202: return "VarWChar";
                case 203: return "Memo";
                default: return $"Type({oleDbType})";
            }
        }
    }
}

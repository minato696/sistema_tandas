using System;
using System.IO;

namespace Generador_Pautas
{
    public static class Logger
    {
        // Carpeta Logs dentro de donde se ejecuta la aplicación
        private static string logFolder = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Logs");

        // Archivo de log del día actual
        private static string logPath = Path.Combine(
            logFolder,
            $"Log_{DateTime.Now:yyyy-MM-dd}.txt");

        public static string LogPath => logPath;

        public static void Log(string mensaje)
        {
            try
            {
                // Crear carpeta Logs si no existe
                if (!Directory.Exists(logFolder))
                    Directory.CreateDirectory(logFolder);

                string linea = $"[{DateTime.Now:HH:mm:ss}] {mensaje}";
                File.AppendAllText(logPath, linea + Environment.NewLine);
            }
            catch { }
        }

        public static void LogSeparador()
        {
            Log("========================================");
        }

        public static void LimpiarLog()
        {
            try
            {
                if (File.Exists(logPath))
                    File.Delete(logPath);
            }
            catch { }
        }

        /// <summary>
        /// Abre la carpeta de logs en el explorador de Windows
        /// </summary>
        public static void AbrirCarpetaLogs()
        {
            try
            {
                if (!Directory.Exists(logFolder))
                    Directory.CreateDirectory(logFolder);
                System.Diagnostics.Process.Start("explorer.exe", logFolder);
            }
            catch { }
        }

        /// <summary>
        /// Abre el archivo de log actual en el bloc de notas
        /// </summary>
        public static void AbrirLogActual()
        {
            try
            {
                if (File.Exists(logPath))
                    System.Diagnostics.Process.Start("notepad.exe", logPath);
            }
            catch { }
        }

        /// <summary>
        /// Log específico para debug de generación de horarios.
        /// Muestra la conversión fila→hora y el tipo de tanda usado.
        /// </summary>
        public static void LogHorario(string contexto, int fila, string horaCalculada, TipoTanda tipoTanda, string archivo = null)
        {
            string info = $"[HORARIO] {contexto}: Fila={fila} → Hora={horaCalculada} (TipoTanda={tipoTanda})";
            if (!string.IsNullOrEmpty(archivo))
                info += $" | Archivo: {Path.GetFileName(archivo)}";
            Log(info);
        }

        /// <summary>
        /// Log de error en conversión de horarios
        /// </summary>
        public static void LogHorarioError(string contexto, int fila, TipoTanda tipoTanda, int totalHorarios)
        {
            Log($"[HORARIO ERROR] {contexto}: Fila={fila} FUERA DE RANGO (TipoTanda={tipoTanda}, MaxFilas={totalHorarios})");
        }

        /// <summary>
        /// Log resumen de generación de pauta
        /// </summary>
        public static void LogPautaGenerada(string archivo, DateTime fecha, string ciudad, string radio,
            TipoTanda tipoTanda, int comercialesEncontrados)
        {
            Log($"[PAUTA] Generada: {Path.GetFileName(archivo)}");
            Log($"[PAUTA]   Fecha={fecha:dd/MM/yyyy}, Ciudad={ciudad}, Radio={radio}");
            Log($"[PAUTA]   TipoTanda={tipoTanda}, Comerciales={comercialesEncontrados}");
        }

        /// <summary>
        /// Log de inicio de generación de pautas
        /// </summary>
        public static void LogInicioGeneracion(string ciudad, string radio, TipoTanda tipoTanda)
        {
            LogSeparador();
            Log($"[GENERACION INICIADA] Ciudad={ciudad}, Radio={radio}, TipoTanda={tipoTanda}");
            Log($"[GENERACION] Hora del sistema: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
        }
    }
}

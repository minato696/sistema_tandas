using System;
using System.Drawing;
using System.IO;
using System.Reflection;

namespace Generador_Pautas
{
    /// <summary>
    /// Clase estatica para proporcionar el icono de la aplicacion a todos los formularios
    /// </summary>
    public static class AppIcon
    {
        private static Icon _appIcon;
        private static readonly object _lock = new object();

        /// <summary>
        /// Obtiene el icono de la aplicacion desde los recursos embebidos de Form1
        /// </summary>
        public static Icon GetIcon()
        {
            if (_appIcon == null)
            {
                lock (_lock)
                {
                    if (_appIcon == null)
                    {
                        try
                        {
                            // Intentar cargar el icono desde los recursos de Form1
                            var assembly = Assembly.GetExecutingAssembly();
                            var resourceName = "Generador_Pautas.Form1.resources";

                            using (var stream = assembly.GetManifestResourceStream(resourceName))
                            {
                                if (stream != null)
                                {
                                    using (var reader = new System.Resources.ResourceReader(stream))
                                    {
                                        foreach (System.Collections.DictionaryEntry entry in reader)
                                        {
                                            if (entry.Key.ToString() == "$this.Icon" && entry.Value is Icon icon)
                                            {
                                                _appIcon = (Icon)icon.Clone();
                                                break;
                                            }
                                        }
                                    }
                                }
                            }

                            // Si no se pudo cargar, intentar desde el archivo ico en el directorio de la aplicacion
                            if (_appIcon == null)
                            {
                                string exePath = Assembly.GetExecutingAssembly().Location;
                                string exeDir = Path.GetDirectoryName(exePath);
                                string icoPath = Path.Combine(exeDir, "app.ico");

                                if (File.Exists(icoPath))
                                {
                                    _appIcon = new Icon(icoPath);
                                }
                            }

                            // Si aun no hay icono, usar el icono del ejecutable
                            if (_appIcon == null)
                            {
                                string exePath = Assembly.GetExecutingAssembly().Location;
                                _appIcon = Icon.ExtractAssociatedIcon(exePath);
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error al cargar icono de aplicacion: {ex.Message}");
                        }
                    }
                }
            }

            return _appIcon;
        }

        /// <summary>
        /// Aplica el icono de la aplicacion a un formulario
        /// </summary>
        public static void ApplyTo(System.Windows.Forms.Form form)
        {
            try
            {
                var icon = GetIcon();
                if (icon != null)
                {
                    form.Icon = icon;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al aplicar icono a formulario: {ex.Message}");
            }
        }
    }
}

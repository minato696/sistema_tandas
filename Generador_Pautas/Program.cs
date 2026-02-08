using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Generador_Pautas
{
    internal static class Program
    {
        /// <summary>
        /// Punto de entrada principal para la aplicacion.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Capturar excepciones no manejadas
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (sender, e) =>
            {
                MostrarError("Error de aplicacion", e.Exception);
            };
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                MostrarError("Error critico", e.ExceptionObject as Exception);
            };

            try
            {
                // Mostrar info de configuracion para debug
                string dbPath = ConfigManager.ObtenerRutaBaseDeDatos();
                string configInfo = $"Ruta BD: {dbPath}\nExiste: {System.IO.File.Exists(dbPath)}";
                System.Diagnostics.Debug.WriteLine(configInfo);

                // Si es ruta de red, sincronizar desde el servidor
                if (ConfigManager.EsRutaDeRed)
                {
                    System.Diagnostics.Debug.WriteLine($"Modo RED detectado. Sincronizando desde: {ConfigManager.RutaRedOriginal}");
                    if (!ConfigManager.SincronizarDesdeRed())
                    {
                        // Si falla la sincronizacion, preguntar si continuar con cache local
                        var resultado = MessageBox.Show(
                            "No se pudo conectar al servidor de red.\n\n" +
                            "¿Desea continuar con la copia local?\n" +
                            "(Los cambios se guardaran localmente y se sincronizaran cuando el servidor este disponible)",
                            "Conexion de Red",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Warning);

                        if (resultado == DialogResult.No)
                        {
                            return;
                        }
                    }
                }

                // Inicializar base de datos SQLite (crear tablas si no existen)
                SQLiteMigration.InicializarBaseDeDatos();

                // Intentar auto-login si hay sesion guardada
                bool autoLoginExitoso = false;
                if (UserManager.HaySesionGuardada())
                {
                    // Ejecutar auto-login de forma sincrona
                    autoLoginExitoso = Task.Run(async () => await UserManager.IntentarAutoLoginAsync()).Result;
                }

                if (autoLoginExitoso)
                {
                    // Auto-login exitoso, ir directo al formulario principal
                    Application.Run(new Form1());
                }
                else
                {
                    // Mostrar formulario de Login
                    LoginForm loginForm = new LoginForm();
                    DialogResult result = loginForm.ShowDialog();

                    // Si el login fue exitoso, mostrar el formulario principal
                    if (result == DialogResult.OK && loginForm.LoginExitoso)
                    {
                        Application.Run(new Form1());
                    }
                    // Si el usuario cerro el login sin autenticarse, salir de la aplicacion
                }
            }
            catch (Exception ex)
            {
                MostrarError("Error al iniciar la aplicacion", ex);
            }
        }

        private static void MostrarError(string titulo, Exception ex)
        {
            string mensaje = ex != null
                ? $"{ex.Message}\n\nDetalle:\n{ex.ToString()}"
                : "Error desconocido";

            string dbPath = "";
            try
            {
                dbPath = ConfigManager.ObtenerRutaBaseDeDatos();
            }
            catch { dbPath = "No disponible"; }

            string infoCompleta = $"{mensaje}\n\n--- Info de configuracion ---\nRuta BD: {dbPath}";

            MessageBox.Show(infoCompleta, titulo, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}

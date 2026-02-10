using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Generador_Pautas
{
    /// <summary>
    /// Tipo de base de datos soportado
    /// </summary>
    public enum DatabaseType
    {
        SQLite,
        PostgreSQL
    }

    /// <summary>
    /// Gestiona la configuracion del sistema desde el archivo config.ini
    /// Permite configurar la base de datos en red local o servidor
    /// </summary>
    public static class ConfigManager
    {
        private static readonly string ConfigFileName = "config.ini";
        private static string _configFilePath;
        private static Dictionary<string, Dictionary<string, string>> _config;

        /// <summary>
        /// Verifica si una ruta de LocalApplicationData es válida y accesible
        /// Excluye carpetas de sistema como TEMP, UMFD-*, Font Driver Host, etc.
        /// No genera excepciones - solo verifica patrones de texto.
        /// </summary>
        private static bool EsRutaLocalAppDataValida(string ruta)
        {
            if (string.IsNullOrEmpty(ruta))
                return false;

            // Excluir rutas de usuarios de servicio/sistema (solo verificación de texto, sin I/O)
            string rutaUpper = ruta.ToUpperInvariant();
            string[] patronesExcluir = {
                "\\TEMP",
                "\\UMFD-",
                "\\FONT DRIVER HOST",
                "\\DWMADMIN",
                "\\DEFAULTAPPPOOL",
                "\\SYSTEM32\\CONFIG\\SYSTEMPROFILE",
                "\\TEMP.",
                ".FONT DRIVER HOST"
            };

            foreach (var patron in patronesExcluir)
            {
                if (rutaUpper.Contains(patron))
                    return false;
            }

            // Solo aceptar rutas que contengan el nombre de usuario actual de Windows
            try
            {
                string currentUser = Environment.UserName;
                if (!string.IsNullOrEmpty(currentUser) && !rutaUpper.Contains(currentUser.ToUpperInvariant()))
                {
                    // La ruta no contiene el nombre de usuario actual, probablemente es de otro usuario del sistema
                    return false;
                }
            }
            catch
            {
                // Si no podemos obtener el nombre de usuario, aceptar la ruta
            }

            return true;
        }

        static ConfigManager()
        {
            // Buscar config.ini en el directorio de la aplicacion
            _configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigFileName);
            _config = new Dictionary<string, Dictionary<string, string>>();
            CargarConfiguracion();
        }

        /// <summary>
        /// Carga la configuracion desde el archivo config.ini
        /// </summary>
        private static void CargarConfiguracion()
        {
            if (!File.Exists(_configFilePath))
            {
                // Crear archivo de configuracion por defecto
                CrearConfiguracionPorDefecto();
            }

            try
            {
                string[] lineas = File.ReadAllLines(_configFilePath, Encoding.UTF8);
                string seccionActual = "";

                foreach (string linea in lineas)
                {
                    string lineaTrimmed = linea.Trim();

                    // Ignorar lineas vacias y comentarios
                    if (string.IsNullOrEmpty(lineaTrimmed) || lineaTrimmed.StartsWith(";") || lineaTrimmed.StartsWith("#"))
                        continue;

                    // Detectar seccion [Seccion]
                    if (lineaTrimmed.StartsWith("[") && lineaTrimmed.EndsWith("]"))
                    {
                        seccionActual = lineaTrimmed.Substring(1, lineaTrimmed.Length - 2);
                        if (!_config.ContainsKey(seccionActual))
                        {
                            _config[seccionActual] = new Dictionary<string, string>();
                        }
                        continue;
                    }

                    // Leer clave=valor
                    int separadorIndex = lineaTrimmed.IndexOf('=');
                    if (separadorIndex > 0 && !string.IsNullOrEmpty(seccionActual))
                    {
                        string clave = lineaTrimmed.Substring(0, separadorIndex).Trim();
                        string valor = lineaTrimmed.Substring(separadorIndex + 1).Trim();
                        _config[seccionActual][clave] = valor;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error cargando configuracion: {ex.Message}");
                // Usar valores por defecto si hay error
                CrearConfiguracionPorDefecto();
            }
        }

        /// <summary>
        /// Crea un archivo de configuracion con valores por defecto
        /// </summary>
        private static void CrearConfiguracionPorDefecto()
        {
            _config.Clear();
            _config["Database"] = new Dictionary<string, string>
            {
                { "Type", "SQLite" },
                { "DatabasePath", "Data.db" }
            };
            _config["PostgreSQL"] = new Dictionary<string, string>
            {
                { "Host", "192.168.10.188" },
                { "Port", "9134" },
                { "Database", "generador_pautas" },
                { "Username", "pautas_user" },
                { "Password", "Pautas2024!" }
            };
            _config["CarpetasRadios"] = new Dictionary<string, string>
            {
                { "LAKALLE", @"C:\LA KALLE\Comerciales\" },
                { "LA KALLE", @"C:\LA KALLE\Comerciales\" },
                { "KARIBEÑA", @"C:\KARIBEÑA\Comerciales\" },
                { "KARIBENA", @"C:\KARIBEÑA\Comerciales\" },
                { "EXITOSA", @"C:\EXITOSA\Comerciales\" },
                { "LA HOT", @"C:\LA HOT\Comerciales\" },
                { "LAHOT", @"C:\LA HOT\Comerciales\" },
                { "RADIO Z", @"C:\RADIO Z\Comerciales\" },
                { "RADIOZ", @"C:\RADIO Z\Comerciales\" }
            };

            try
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("[Database]");
                sb.AppendLine("; Tipo de base de datos: SQLite o PostgreSQL");
                sb.AppendLine("; SQLite: Base de datos local o en carpeta compartida");
                sb.AppendLine("; PostgreSQL: Servidor de base de datos (recomendado para red empresarial)");
                sb.AppendLine("Type=SQLite");
                sb.AppendLine("");
                sb.AppendLine("; Ruta de la base de datos SQLite (solo si Type=SQLite)");
                sb.AppendLine("; Para uso LOCAL: usar ruta relativa o absoluta local");
                sb.AppendLine("; Ejemplo local: Data.db");
                sb.AppendLine("; Para uso en RED: usar ruta UNC del servidor");
                sb.AppendLine("; Ejemplo red: \\\\192.168.10.188\\DB-SISTEMA-PAUTAS\\Data.db");
                sb.AppendLine("DatabasePath=Data.db");
                sb.AppendLine("");
                sb.AppendLine("[PostgreSQL]");
                sb.AppendLine("; Configuracion de PostgreSQL (solo si Type=PostgreSQL)");
                sb.AppendLine("; RECOMENDADO para uso en red empresarial - mejor rendimiento");
                sb.AppendLine("Host=192.168.10.188");
                sb.AppendLine("Port=9134");
                sb.AppendLine("Database=generador_pautas");
                sb.AppendLine("Username=pautas_user");
                sb.AppendLine("Password=Pautas2024!");
                sb.AppendLine("");
                sb.AppendLine("[CarpetasRadios]");
                sb.AppendLine("; Carpetas donde se encuentran los comerciales de cada radio");
                sb.AppendLine("; Formato: NombreRadio=RutaCarpeta");
                sb.AppendLine("; Para agregar una nueva radio, simplemente agregue una linea nueva");
                sb.AppendLine(@"LAKALLE=C:\LA KALLE\Comerciales\");
                sb.AppendLine(@"LA KALLE=C:\LA KALLE\Comerciales\");
                sb.AppendLine(@"KARIBEÑA=C:\KARIBEÑA\Comerciales\");
                sb.AppendLine(@"KARIBENA=C:\KARIBEÑA\Comerciales\");
                sb.AppendLine(@"EXITOSA=C:\EXITOSA\Comerciales\");
                sb.AppendLine(@"LA HOT=C:\LA HOT\Comerciales\");
                sb.AppendLine(@"LAHOT=C:\LA HOT\Comerciales\");
                sb.AppendLine(@"RADIO Z=C:\RADIO Z\Comerciales\");
                sb.AppendLine(@"RADIOZ=C:\RADIO Z\Comerciales\");

                File.WriteAllText(_configFilePath, sb.ToString(), Encoding.UTF8);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creando config por defecto: {ex.Message}");
            }
        }

        /// <summary>
        /// Obtiene un valor de configuracion
        /// </summary>
        public static string ObtenerValor(string seccion, string clave, string valorPorDefecto = "")
        {
            if (_config.ContainsKey(seccion) && _config[seccion].ContainsKey(clave))
            {
                return _config[seccion][clave];
            }
            return valorPorDefecto;
        }

        /// <summary>
        /// Establece un valor de configuracion y lo guarda en el archivo
        /// </summary>
        public static void EstablecerValor(string seccion, string clave, string valor)
        {
            if (!_config.ContainsKey(seccion))
            {
                _config[seccion] = new Dictionary<string, string>();
            }
            _config[seccion][clave] = valor;
            GuardarConfiguracion();
        }

        /// <summary>
        /// Obtiene la carpeta de comerciales para una radio específica.
        /// Busca primero en config.ini, si no existe genera una ruta por defecto.
        /// Detecta automáticamente si la carpeta usa "Comerciales" o "COMERCIALES".
        /// </summary>
        public static string ObtenerCarpetaRadio(string radio)
        {
            if (string.IsNullOrEmpty(radio))
                return @"C:\COMERCIALES\";

            string rutaConfigurada = null;

            // Buscar en la configuración (case-insensitive)
            if (_config.ContainsKey("CarpetasRadios"))
            {
                foreach (var kvp in _config["CarpetasRadios"])
                {
                    if (kvp.Key.Equals(radio, StringComparison.OrdinalIgnoreCase))
                    {
                        rutaConfigurada = kvp.Value;
                        break;
                    }
                }
            }

            // Si no está en la configuración, generar ruta por defecto
            if (string.IsNullOrEmpty(rutaConfigurada))
            {
                rutaConfigurada = $@"C:\{radio.ToUpper()}\Comerciales\";
            }

            // Verificar si la ruta configurada existe
            if (Directory.Exists(rutaConfigurada))
            {
                return rutaConfigurada;
            }

            // Si no existe, probar con variaciones de "Comerciales" / "COMERCIALES"
            string rutaAlternativa = ObtenerRutaAlternativa(rutaConfigurada);
            if (!string.IsNullOrEmpty(rutaAlternativa) && Directory.Exists(rutaAlternativa))
            {
                return rutaAlternativa;
            }

            // Si ninguna existe, retornar la configurada (puede que se cree después)
            return rutaConfigurada;
        }

        /// <summary>
        /// Genera una ruta alternativa intercambiando "Comerciales" por "COMERCIALES" o viceversa.
        /// </summary>
        private static string ObtenerRutaAlternativa(string rutaOriginal)
        {
            if (string.IsNullOrEmpty(rutaOriginal))
                return null;

            // Variaciones a buscar (case-sensitive)
            string[] variacionesMinusculas = { "Comerciales", "comerciales" };
            string[] variacionesMayusculas = { "COMERCIALES" };

            // Si contiene alguna variación en minúsculas, probar con mayúsculas
            foreach (var variacion in variacionesMinusculas)
            {
                if (rutaOriginal.Contains(variacion))
                {
                    return rutaOriginal.Replace(variacion, "COMERCIALES");
                }
            }

            // Si contiene COMERCIALES, probar con Comerciales
            foreach (var variacion in variacionesMayusculas)
            {
                if (rutaOriginal.Contains(variacion))
                {
                    return rutaOriginal.Replace(variacion, "Comerciales");
                }
            }

            return null;
        }

        /// <summary>
        /// Obtiene todas las carpetas de radios configuradas
        /// </summary>
        public static Dictionary<string, string> ObtenerTodasLasCarpetasRadios()
        {
            var resultado = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (_config.ContainsKey("CarpetasRadios"))
            {
                foreach (var kvp in _config["CarpetasRadios"])
                {
                    resultado[kvp.Key] = kvp.Value;
                }
            }

            return resultado;
        }

        /// <summary>
        /// Guarda la configuracion actual en el archivo
        /// </summary>
        private static void GuardarConfiguracion()
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                foreach (var seccion in _config)
                {
                    sb.AppendLine($"[{seccion.Key}]");
                    foreach (var kvp in seccion.Value)
                    {
                        sb.AppendLine($"{kvp.Key}={kvp.Value}");
                    }
                    sb.AppendLine();
                }
                File.WriteAllText(_configFilePath, sb.ToString(), Encoding.UTF8);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error guardando configuracion: {ex.Message}");
            }
        }

        private static string _rutaLocalCache = null;
        private static string _rutaRedOriginal = null;
        private static bool _esRutaRed = false;
        private static DateTime _ultimaFechaModificacionRed = DateTime.MinValue;

        /// <summary>
        /// Obtiene la ruta completa de la base de datos
        /// Soporta rutas locales y rutas de red UNC
        /// Para rutas UNC, crea una copia local y trabaja con ella
        /// </summary>
        public static string ObtenerRutaBaseDeDatos()
        {
            string dbPath = ObtenerValor("Database", "DatabasePath", "Data.db");

            // Si es una ruta UNC (red)
            if (dbPath.StartsWith("\\\\"))
            {
                _esRutaRed = true;
                _rutaRedOriginal = dbPath;

                // Crear ruta local en AppData para cache
                if (_rutaLocalCache == null)
                {
                    string appDataFolder;
                    try
                    {
                        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                        if (!string.IsNullOrEmpty(localAppData) && EsRutaLocalAppDataValida(localAppData))
                        {
                            appDataFolder = Path.Combine(localAppData, "GeneradorPautas");
                        }
                        else
                        {
                            // Fallback: usar carpeta de la aplicación
                            appDataFolder = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                        }
                    }
                    catch
                    {
                        appDataFolder = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                    }

                    if (!Directory.Exists(appDataFolder))
                    {
                        try
                        {
                            Directory.CreateDirectory(appDataFolder);
                        }
                        catch
                        {
                            // Si no se puede crear, usar la carpeta de la aplicación
                            appDataFolder = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                        }
                    }

                    _rutaLocalCache = Path.Combine(appDataFolder, "Data_Cache.db");
                }

                return _rutaLocalCache;
            }

            _esRutaRed = false;

            // Si es ruta absoluta, usarla directamente
            if (Path.IsPathRooted(dbPath))
            {
                return dbPath;
            }

            // Si es ruta relativa, combinar con el directorio de la aplicacion
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dbPath);
        }

        /// <summary>
        /// Indica si se esta usando una ruta de red
        /// </summary>
        public static bool EsRutaDeRed => _esRutaRed;

        /// <summary>
        /// Obtiene la ruta original de red (si aplica)
        /// </summary>
        public static string RutaRedOriginal => _rutaRedOriginal;

        /// <summary>
        /// Sincroniza la base de datos local con la de red (descarga)
        /// NOTA: Con PostgreSQL no se necesita sincronizacion de archivos
        /// </summary>
        public static bool SincronizarDesdeRed()
        {
            // Con PostgreSQL no se necesita sincronizacion de archivos
            // El servidor maneja todo directamente
            return true;
        }

        /// <summary>
        /// Sincroniza la base de datos local con la de red (subida)
        /// NOTA: Con PostgreSQL no se necesita sincronizacion de archivos
        /// </summary>
        public static bool SincronizarHaciaRed(bool mostrarError = true)
        {
            // Con PostgreSQL no se necesita sincronizacion de archivos
            // El servidor maneja todo directamente
            return true;
        }

        /// <summary>
        /// Sincroniza en ambas direcciones: primero descarga, luego sube
        /// Util para operaciones criticas donde se necesita tener la ultima version
        /// </summary>
        public static bool SincronizarBidireccional()
        {
            if (!_esRutaRed)
                return true;

            // Primero subir cambios locales pendientes
            SincronizarHaciaRed(mostrarError: false);

            // Luego descargar la version mas reciente del servidor
            return SincronizarDesdeRed();
        }

        /// <summary>
        /// Evento que se dispara cuando hay cambios en la base de datos
        /// Los formularios pueden suscribirse para refrescarse automaticamente
        /// </summary>
        public static event Action OnDatosActualizados;

        /// <summary>
        /// Notifica que se realizo un cambio en la BD y debe sincronizarse
        /// Llamar despues de INSERT, UPDATE, DELETE
        /// </summary>
        public static void NotificarCambioEnBD()
        {
            if (_esRutaRed)
            {
                // Sincronizar hacia red inmediatamente (sin mostrar error para no molestar)
                SincronizarHaciaRed(mostrarError: false);
            }

            // Notificar a todos los formularios suscritos que deben actualizarse
            OnDatosActualizados?.Invoke();
        }

        /// <summary>
        /// Actualiza la cache local antes de una operacion de lectura importante
        /// Llamar antes de cargar listas de comerciales, usuarios, etc.
        /// </summary>
        public static void ActualizarCacheAntesDeLectura()
        {
            if (_esRutaRed)
            {
                SincronizarDesdeRed();
            }
        }

        /// <summary>
        /// Verifica si el archivo de red fue modificado desde la ultima sincronizacion
        /// Retorna true si hay cambios pendientes de descargar
        /// </summary>
        public static bool HayCambiosEnRed()
        {
            if (!_esRutaRed || string.IsNullOrEmpty(_rutaRedOriginal))
                return false;

            try
            {
                if (File.Exists(_rutaRedOriginal))
                {
                    DateTime fechaModificacionRed = File.GetLastWriteTime(_rutaRedOriginal);

                    // Si es la primera verificacion, guardar la fecha actual
                    if (_ultimaFechaModificacionRed == DateTime.MinValue)
                    {
                        _ultimaFechaModificacionRed = fechaModificacionRed;
                        return false;
                    }

                    // Si la fecha de modificacion cambio, hay actualizaciones
                    if (fechaModificacionRed > _ultimaFechaModificacionRed)
                    {
                        return true;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error verificando cambios en red: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Sincroniza desde red si hay cambios y actualiza la fecha de ultima modificacion
        /// Retorna true si se sincronizaron cambios
        /// </summary>
        public static bool SincronizarSiHayCambios()
        {
            if (!_esRutaRed)
                return false;

            if (HayCambiosEnRed())
            {
                bool exito = SincronizarDesdeRed();
                if (exito)
                {
                    // Actualizar la fecha de ultima modificacion conocida
                    try
                    {
                        _ultimaFechaModificacionRed = File.GetLastWriteTime(_rutaRedOriginal);
                    }
                    catch { }
                }
                return exito;
            }
            return false;
        }

        /// <summary>
        /// Obtiene el connection string para SQLite optimizado para rendimiento
        /// </summary>
        public static string ObtenerConnectionString()
        {
            string dbPath = ObtenerRutaBaseDeDatos();
            // Parametros de optimizacion:
            // - Cache=Shared: Comparte cache entre conexiones (mejor rendimiento)
            // - Journal Mode=WAL: Write-Ahead Logging (mejor concurrencia y rendimiento)
            // - Synchronous=Normal: Balance entre seguridad y velocidad
            // - Cache Size: -20000 = 20MB de cache en memoria
            // - Page Size: 4096 bytes (optimo para la mayoria de sistemas)
            return $"Data Source={dbPath};Version=3;Cache=Shared;Journal Mode=WAL;Synchronous=Normal;Cache Size=-20000;Page Size=4096;";
        }

        /// <summary>
        /// Verifica si la base de datos es accesible (util para conexiones de red)
        /// </summary>
        public static bool VerificarConexion()
        {
            try
            {
                string dbPath = ObtenerRutaBaseDeDatos();

                // Verificar si es ruta de red
                if (dbPath.StartsWith("\\\\"))
                {
                    string directorio = Path.GetDirectoryName(dbPath);
                    if (!Directory.Exists(directorio))
                    {
                        return false;
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Obtiene informacion sobre la configuracion actual
        /// </summary>
        public static string ObtenerInfoConfiguracion()
        {
            string dbPath = ObtenerRutaBaseDeDatos();
            bool esRed = dbPath.StartsWith("\\\\");

            StringBuilder info = new StringBuilder();
            info.AppendLine($"Tipo: {(esRed ? "Red" : "Local")}");
            info.AppendLine($"Ruta: {dbPath}");
            info.AppendLine($"Accesible: {(VerificarConexion() ? "Si" : "No")}");

            return info.ToString();
        }

        /// <summary>
        /// Recarga la configuracion desde el archivo
        /// </summary>
        public static void RecargarConfiguracion()
        {
            _config.Clear();
            CargarConfiguracion();
        }

        /// <summary>
        /// Obtiene la ruta de red para guardar las pautas.
        /// Si no esta configurada, retorna null y se usara la carpeta local.
        /// </summary>
        public static string ObtenerRutaPautasRed()
        {
            string ruta = ObtenerValor("Pautas", "RutaRed", "");
            if (string.IsNullOrWhiteSpace(ruta))
                return null;
            return ruta;
        }

        /// <summary>
        /// Obtiene la ruta base para guardar archivos de pautas.
        /// Usa la ruta de red si esta configurada, sino la carpeta local.
        /// </summary>
        public static string ObtenerRutaBasePautas()
        {
            string rutaRed = ObtenerRutaPautasRed();
            if (!string.IsNullOrEmpty(rutaRed))
            {
                return rutaRed;
            }
            // Carpeta local por defecto
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pautas");
        }

        /// <summary>
        /// Verifica si la ruta de pautas de red esta accesible
        /// </summary>
        public static bool VerificarRutaPautasRed()
        {
            string rutaRed = ObtenerRutaPautasRed();
            if (string.IsNullOrEmpty(rutaRed))
                return true; // No hay ruta de red configurada, usar local

            try
            {
                // Verificar si la carpeta existe o se puede crear
                if (!Directory.Exists(rutaRed))
                {
                    Directory.CreateDirectory(rutaRed);
                }
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error accediendo a ruta de pautas: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Obtiene el tipo de base de datos configurado
        /// </summary>
        public static DatabaseType ObtenerTipoBaseDatos()
        {
            string tipo = ObtenerValor("Database", "Type", "SQLite");
            if (tipo.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
            {
                return DatabaseType.PostgreSQL;
            }
            return DatabaseType.SQLite;
        }

        /// <summary>
        /// Verifica si se esta usando PostgreSQL
        /// </summary>
        public static bool EsPostgreSQL => ObtenerTipoBaseDatos() == DatabaseType.PostgreSQL;

        /// <summary>
        /// Obtiene el connection string para PostgreSQL
        /// </summary>
        public static string ObtenerPostgreSQLConnectionString()
        {
            string host = ObtenerValor("PostgreSQL", "Host", "192.168.10.188");
            string port = ObtenerValor("PostgreSQL", "Port", "9134");
            string database = ObtenerValor("PostgreSQL", "Database", "generador_pautas");
            string username = ObtenerValor("PostgreSQL", "Username", "pautas_user");
            string password = ObtenerValor("PostgreSQL", "Password", "Pautas2024!");

            // Optimizaciones de conexión para red local
            return $"Host={host};Port={port};Database={database};Username={username};Password={password};" +
                   "Pooling=true;MinPoolSize=5;MaxPoolSize=20;" +
                   "ConnectionIdleLifetime=300;ConnectionPruningInterval=10;" +
                   "Timeout=30;CommandTimeout=60;";
        }

        /// <summary>
        /// Obtiene el connection string segun el tipo de BD configurado
        /// </summary>
        public static string ObtenerConnectionStringActual()
        {
            if (EsPostgreSQL)
            {
                return ObtenerPostgreSQLConnectionString();
            }
            return ObtenerConnectionString();
        }
    }
}

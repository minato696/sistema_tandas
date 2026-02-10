using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Npgsql;

namespace Generador_Pautas
{
    /// <summary>
    /// Gestiona la autenticacion y sesion de usuarios
    /// </summary>
    public static class UserManager
    {
        // Usuario actualmente logueado
        private static Usuario _usuarioActual;

        // Archivo para guardar sesion
        private static readonly string SessionFile = ObtenerRutaSessionFile();

        private static string ObtenerRutaSessionFile()
        {
            try
            {
                // Intentar usar LocalApplicationData
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                if (!string.IsNullOrEmpty(localAppData) && EsRutaValida(localAppData))
                {
                    return Path.Combine(localAppData, "GeneradorPautas", "session.dat");
                }
            }
            catch { }

            // Fallback: usar carpeta de la aplicación
            string appFolder = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            return Path.Combine(appFolder, "session.dat");
        }

        /// <summary>
        /// Verifica si una ruta de usuario es válida y accesible
        /// Excluye carpetas de sistema como TEMP, UMFD-*, Font Driver Host, etc.
        /// No genera excepciones - solo verifica patrones de texto.
        /// </summary>
        private static bool EsRutaValida(string ruta)
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

        // Clave para encriptar (en produccion usar DPAPI o similar)
        private static readonly byte[] Key = Encoding.UTF8.GetBytes("GP_SecretKey2024");

        /// <summary>
        /// Obtiene el usuario actualmente logueado
        /// </summary>
        public static Usuario UsuarioActual => _usuarioActual;

        /// <summary>
        /// Verifica si hay un usuario logueado
        /// </summary>
        public static bool HayUsuarioLogueado => _usuarioActual != null;

        /// <summary>
        /// Verifica si el usuario actual es administrador
        /// </summary>
        public static bool EsAdministrador => _usuarioActual?.EsAdministrador ?? false;

        /// <summary>
        /// Intenta autenticar un usuario con sus credenciales
        /// </summary>
        public static async Task<(bool exito, string mensaje)> LoginAsync(string usuario, string contrasena)
        {
            try
            {
                string connectionString = PostgreSQLMigration.ConnectionString;
                string hashedPassword = HashPassword(contrasena);

                using (NpgsqlConnection conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    string query = @"SELECT Id, Usuario, Rol, NombreCompleto, Estado, FechaCreacion
                                    FROM Usuarios
                                    WHERE Usuario = @Usuario AND Contrasena = @Contrasena";

                    using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Usuario", usuario);
                        cmd.Parameters.AddWithValue("@Contrasena", hashedPassword);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                string estado = reader["Estado"].ToString();
                                if (estado != "Activo")
                                {
                                    return (false, "El usuario esta desactivado. Contacte al administrador.");
                                }

                                _usuarioActual = new Usuario
                                {
                                    Id = Convert.ToInt32(reader["Id"]),
                                    NombreUsuario = reader["Usuario"].ToString(),
                                    Rol = reader["Rol"].ToString(),
                                    NombreCompleto = reader["NombreCompleto"].ToString(),
                                    Estado = estado,
                                    FechaCreacion = Convert.ToDateTime(reader["FechaCreacion"])
                                };

                                return (true, $"Bienvenido, {_usuarioActual.NombreCompleto}");
                            }
                            else
                            {
                                return (false, "Usuario o contrasena incorrectos.");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return (false, $"Error al iniciar sesion: {ex.Message}");
            }
        }

        /// <summary>
        /// Cierra la sesion del usuario actual
        /// </summary>
        public static void Logout()
        {
            _usuarioActual = null;
            EliminarSesionGuardada();
        }

        /// <summary>
        /// Guarda la sesion para auto-login
        /// </summary>
        public static void GuardarSesion(string usuario, string contrasena)
        {
            try
            {
                string directorio = Path.GetDirectoryName(SessionFile);
                if (!Directory.Exists(directorio))
                {
                    Directory.CreateDirectory(directorio);
                }

                // Encriptar credenciales
                string datos = $"{usuario}|{contrasena}";
                byte[] datosBytes = Encoding.UTF8.GetBytes(datos);
                byte[] encriptado = ProtectedData.Protect(datosBytes, Key, DataProtectionScope.CurrentUser);

                File.WriteAllBytes(SessionFile, encriptado);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al guardar sesion: {ex.Message}");
            }
        }

        /// <summary>
        /// Intenta cargar sesion guardada y hacer auto-login
        /// </summary>
        public static async Task<bool> IntentarAutoLoginAsync()
        {
            try
            {
                if (!File.Exists(SessionFile))
                    return false;

                byte[] encriptado = File.ReadAllBytes(SessionFile);
                byte[] desencriptado = ProtectedData.Unprotect(encriptado, Key, DataProtectionScope.CurrentUser);
                string datos = Encoding.UTF8.GetString(desencriptado);

                string[] partes = datos.Split('|');
                if (partes.Length != 2)
                    return false;

                string usuario = partes[0];
                string contrasena = partes[1];

                var (exito, _) = await LoginAsync(usuario, contrasena);
                return exito;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en auto-login: {ex.Message}");
                EliminarSesionGuardada();
                return false;
            }
        }

        /// <summary>
        /// Verifica si hay una sesion guardada
        /// </summary>
        public static bool HaySesionGuardada()
        {
            return File.Exists(SessionFile);
        }

        /// <summary>
        /// Elimina la sesion guardada
        /// </summary>
        public static void EliminarSesionGuardada()
        {
            try
            {
                if (File.Exists(SessionFile))
                {
                    File.Delete(SessionFile);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al eliminar sesion: {ex.Message}");
            }
        }

        /// <summary>
        /// Obtiene la lista de todos los usuarios (solo admin)
        /// </summary>
        public static async Task<List<Usuario>> ObtenerUsuariosAsync()
        {
            var usuarios = new List<Usuario>();

            try
            {
                string connectionString = PostgreSQLMigration.ConnectionString;

                using (NpgsqlConnection conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    string query = "SELECT Id, Usuario, Rol, NombreCompleto, Estado, FechaCreacion FROM Usuarios ORDER BY Usuario";

                    using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            usuarios.Add(new Usuario
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                NombreUsuario = reader["Usuario"].ToString(),
                                Rol = reader["Rol"].ToString(),
                                NombreCompleto = reader["NombreCompleto"].ToString(),
                                Estado = reader["Estado"].ToString(),
                                FechaCreacion = Convert.ToDateTime(reader["FechaCreacion"])
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al obtener usuarios: {ex.Message}");
            }

            return usuarios;
        }

        /// <summary>
        /// Crea un nuevo usuario (solo admin)
        /// </summary>
        public static async Task<(bool exito, string mensaje)> CrearUsuarioAsync(string usuario, string contrasena, string rol, string nombreCompleto)
        {
            try
            {
                string connectionString = PostgreSQLMigration.ConnectionString;
                string hashedPassword = HashPassword(contrasena);

                using (NpgsqlConnection conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    string query = @"INSERT INTO Usuarios (Usuario, Contrasena, Rol, NombreCompleto, Estado, FechaCreacion)
                                    VALUES (@Usuario, @Contrasena, @Rol, @NombreCompleto, 'Activo', @Fecha)";

                    using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Usuario", usuario);
                        cmd.Parameters.AddWithValue("@Contrasena", hashedPassword);
                        cmd.Parameters.AddWithValue("@Rol", rol);
                        cmd.Parameters.AddWithValue("@NombreCompleto", nombreCompleto);
                        cmd.Parameters.AddWithValue("@Fecha", DateTime.Now);

                        await cmd.ExecuteNonQueryAsync();

                        return (true, "Usuario creado correctamente.");
                    }
                }
            }
            catch (PostgresException ex) when (ex.SqlState == "23505")
            {
                return (false, "Ya existe un usuario con ese nombre.");
            }
            catch (Exception ex)
            {
                return (false, $"Error al crear usuario: {ex.Message}");
            }
        }

        /// <summary>
        /// Actualiza un usuario existente (solo admin)
        /// </summary>
        public static async Task<(bool exito, string mensaje)> ActualizarUsuarioAsync(int id, string rol, string nombreCompleto, string estado)
        {
            try
            {
                string connectionString = PostgreSQLMigration.ConnectionString;

                using (NpgsqlConnection conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    string query = @"UPDATE Usuarios SET Rol = @Rol, NombreCompleto = @NombreCompleto, Estado = @Estado
                                    WHERE Id = @Id";

                    using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", id);
                        cmd.Parameters.AddWithValue("@Rol", rol);
                        cmd.Parameters.AddWithValue("@NombreCompleto", nombreCompleto);
                        cmd.Parameters.AddWithValue("@Estado", estado);

                        await cmd.ExecuteNonQueryAsync();

                        return (true, "Usuario actualizado correctamente.");
                    }
                }
            }
            catch (Exception ex)
            {
                return (false, $"Error al actualizar usuario: {ex.Message}");
            }
        }

        /// <summary>
        /// Cambia la contrasena de un usuario
        /// </summary>
        public static async Task<(bool exito, string mensaje)> CambiarContrasenaAsync(int id, string nuevaContrasena)
        {
            try
            {
                string connectionString = PostgreSQLMigration.ConnectionString;
                string hashedPassword = HashPassword(nuevaContrasena);

                using (NpgsqlConnection conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    string query = "UPDATE Usuarios SET Contrasena = @Contrasena WHERE Id = @Id";

                    using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", id);
                        cmd.Parameters.AddWithValue("@Contrasena", hashedPassword);

                        await cmd.ExecuteNonQueryAsync();

                        return (true, "Contrasena actualizada correctamente.");
                    }
                }
            }
            catch (Exception ex)
            {
                return (false, $"Error al cambiar contrasena: {ex.Message}");
            }
        }

        /// <summary>
        /// Hash SHA256 para contrasenas
        /// </summary>
        private static string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                var builder = new StringBuilder();
                foreach (byte b in bytes)
                {
                    builder.Append(b.ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}

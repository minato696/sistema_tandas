using System;
using System.Collections.Generic;
using Npgsql;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Generador_Pautas
{
    public partial class AdminRadiosForm : Form
    {
        private DataGridView dgvRadios;
        private TextBox txtNuevaRadio;
        private TextBox txtDescripcion;
        private Button btnAgregar;
        private Button btnEliminar;
        private Button btnGuardar;
        private CheckedListBox clbCiudades;
        private Label lblCiudadesAsignadas;

        public AdminRadiosForm()
        {
            InitializeComponent();
            AppIcon.ApplyTo(this); // Aplicar icono de la aplicacion
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // Configuracion del formulario
            this.Text = "Administrar Radios";
            this.Size = new Size(650, 550);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(240, 240, 245);

            // Titulo
            Label lblTitulo = new Label
            {
                Text = "Administrar Radios",
                Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 120, 215),
                AutoSize = true,
                Location = new Point(20, 15)
            };

            // DataGridView
            dgvRadios = new DataGridView
            {
                Location = new Point(20, 50),
                Size = new Size(400, 250),
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                ReadOnly = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            // Columnas del DataGridView
            dgvRadios.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", HeaderText = "ID", Width = 40, ReadOnly = true });
            dgvRadios.Columns.Add(new DataGridViewTextBoxColumn { Name = "Nombre", HeaderText = "Radio", Width = 150 });
            dgvRadios.Columns.Add(new DataGridViewTextBoxColumn { Name = "Descripcion", HeaderText = "Descripcion", Width = 150 });
            dgvRadios.Columns.Add(new DataGridViewComboBoxColumn
            {
                Name = "Estado",
                HeaderText = "Estado",
                Width = 80,
                Items = { "Activo", "Inactivo" }
            });

            dgvRadios.SelectionChanged += DgvRadios_SelectionChanged;

            // Panel de ciudades asignadas
            lblCiudadesAsignadas = new Label
            {
                Text = "Ciudades asignadas:",
                Font = new Font("Segoe UI", 10F),
                AutoSize = true,
                Location = new Point(440, 50)
            };

            clbCiudades = new CheckedListBox
            {
                Location = new Point(440, 75),
                Size = new Size(170, 225),
                Font = new Font("Segoe UI", 10F),
                CheckOnClick = true
            };

            // Panel para agregar nueva radio
            Label lblNueva = new Label
            {
                Text = "Nueva Radio:",
                Font = new Font("Segoe UI", 10F),
                AutoSize = true,
                Location = new Point(20, 315)
            };

            txtNuevaRadio = new TextBox
            {
                Font = new Font("Segoe UI", 10F),
                Size = new Size(150, 25),
                Location = new Point(20, 340)
            };

            Label lblDesc = new Label
            {
                Text = "Descripcion:",
                Font = new Font("Segoe UI", 10F),
                AutoSize = true,
                Location = new Point(180, 315)
            };

            txtDescripcion = new TextBox
            {
                Font = new Font("Segoe UI", 10F),
                Size = new Size(200, 25),
                Location = new Point(180, 340)
            };

            btnAgregar = new Button
            {
                Text = "Agregar",
                Font = new Font("Segoe UI", 9F),
                Size = new Size(80, 28),
                Location = new Point(390, 338),
                BackColor = Color.FromArgb(0, 150, 136),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnAgregar.FlatAppearance.BorderSize = 0;
            btnAgregar.Click += BtnAgregar_Click;

            btnEliminar = new Button
            {
                Text = "Eliminar",
                Font = new Font("Segoe UI", 9F),
                Size = new Size(80, 28),
                Location = new Point(480, 338),
                BackColor = Color.FromArgb(200, 50, 50),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnEliminar.FlatAppearance.BorderSize = 0;
            btnEliminar.Click += BtnEliminar_Click;

            btnGuardar = new Button
            {
                Text = "Guardar Cambios",
                Font = new Font("Segoe UI Semibold", 10F),
                Size = new Size(590, 35),
                Location = new Point(20, 470),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnGuardar.FlatAppearance.BorderSize = 0;
            btnGuardar.Click += BtnGuardar_Click;

            // Nota informativa
            Label lblNota = new Label
            {
                Text = "Nota: Marque las ciudades donde opera cada radio. Los cambios se guardan al presionar 'Guardar Cambios'.",
                Font = new Font("Segoe UI", 8F, FontStyle.Italic),
                ForeColor = Color.Gray,
                AutoSize = true,
                Location = new Point(20, 385)
            };

            Button btnGuardarCiudades = new Button
            {
                Text = "Aplicar Ciudades",
                Font = new Font("Segoe UI", 9F),
                Size = new Size(170, 28),
                Location = new Point(440, 310),
                BackColor = Color.FromArgb(255, 152, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnGuardarCiudades.FlatAppearance.BorderSize = 0;
            btnGuardarCiudades.Click += BtnGuardarCiudades_Click;

            // Agregar controles
            this.Controls.Add(lblTitulo);
            this.Controls.Add(dgvRadios);
            this.Controls.Add(lblCiudadesAsignadas);
            this.Controls.Add(clbCiudades);
            this.Controls.Add(lblNueva);
            this.Controls.Add(txtNuevaRadio);
            this.Controls.Add(lblDesc);
            this.Controls.Add(txtDescripcion);
            this.Controls.Add(btnAgregar);
            this.Controls.Add(btnEliminar);
            this.Controls.Add(btnGuardarCiudades);
            this.Controls.Add(lblNota);
            this.Controls.Add(btnGuardar);

            this.Load += AdminRadiosForm_Load;

            this.ResumeLayout(false);
        }

        private async void AdminRadiosForm_Load(object sender, EventArgs e)
        {
            await CargarCiudadesAsync();
            await CargarRadiosAsync();
        }

        private async Task CargarCiudadesAsync()
        {
            clbCiudades.Items.Clear();

            try
            {
                var ciudades = await AdminCiudadesForm.ObtenerCiudadesActivasAsync();
                foreach (string ciudad in ciudades)
                {
                    clbCiudades.Items.Add(ciudad);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al cargar ciudades: {ex.Message}");
            }
        }

        private async Task CargarRadiosAsync()
        {
            dgvRadios.Rows.Clear();

            try
            {
                string connectionString = PostgreSQLMigration.ConnectionString;

                using (NpgsqlConnection conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    string query = "SELECT Id, Nombre, Descripcion, Estado FROM Radios ORDER BY Nombre";

                    using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn))
                    using (NpgsqlDataReader reader = (NpgsqlDataReader)await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            int rowIndex = dgvRadios.Rows.Add();
                            dgvRadios.Rows[rowIndex].Cells["Id"].Value = reader["Id"].ToString();
                            dgvRadios.Rows[rowIndex].Cells["Nombre"].Value = reader["Nombre"].ToString();
                            dgvRadios.Rows[rowIndex].Cells["Descripcion"].Value = reader["Descripcion"]?.ToString() ?? "";
                            dgvRadios.Rows[rowIndex].Cells["Estado"].Value = reader["Estado"].ToString();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar radios: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void DgvRadios_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvRadios.SelectedRows.Count > 0)
            {
                string radioId = dgvRadios.SelectedRows[0].Cells["Id"].Value?.ToString();
                if (!string.IsNullOrEmpty(radioId))
                {
                    await CargarCiudadesDeRadioAsync(int.Parse(radioId));
                }
            }
        }

        private async Task CargarCiudadesDeRadioAsync(int radioId)
        {
            // Desmarcar todas
            for (int i = 0; i < clbCiudades.Items.Count; i++)
            {
                clbCiudades.SetItemChecked(i, false);
            }

            try
            {
                string connectionString = PostgreSQLMigration.ConnectionString;

                using (NpgsqlConnection conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    string query = @"
                        SELECT c.Nombre
                        FROM RadiosCiudades rc
                        INNER JOIN Ciudades c ON rc.CiudadId = c.Id
                        WHERE rc.RadioId = @RadioId AND rc.Estado = 'Activo'";

                    using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@RadioId", radioId);

                        using (NpgsqlDataReader reader = (NpgsqlDataReader)await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                string ciudadNombre = reader["Nombre"].ToString();
                                int index = clbCiudades.Items.IndexOf(ciudadNombre);
                                if (index >= 0)
                                {
                                    clbCiudades.SetItemChecked(index, true);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al cargar ciudades de radio: {ex.Message}");
            }
        }

        private async void BtnAgregar_Click(object sender, EventArgs e)
        {
            string nombreRadio = txtNuevaRadio.Text.Trim().ToUpper();
            string descripcion = txtDescripcion.Text.Trim();

            if (string.IsNullOrEmpty(nombreRadio))
            {
                MessageBox.Show("Ingrese el nombre de la radio.", "Advertencia",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                string connectionString = PostgreSQLMigration.ConnectionString;

                using (NpgsqlConnection conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    string query = "INSERT INTO Radios (Nombre, Descripcion, Estado) VALUES (@Nombre, @Descripcion, 'Activo')";

                    using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Nombre", nombreRadio);
                        cmd.Parameters.AddWithValue("@Descripcion", descripcion);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                // Sincronizar cambios a la red
                ConfigManager.NotificarCambioEnBD();

                txtNuevaRadio.Text = "";
                txtDescripcion.Text = "";
                await CargarRadiosAsync();
                MessageBox.Show("Radio agregada correctamente.", "Exito",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (PostgresException ex) when (ex.Message.Contains("UNIQUE"))
            {
                MessageBox.Show("Ya existe una radio con ese nombre.", "Advertencia",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al agregar radio: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void BtnEliminar_Click(object sender, EventArgs e)
        {
            if (dgvRadios.SelectedRows.Count == 0)
            {
                MessageBox.Show("Seleccione una radio para eliminar.", "Advertencia",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string id = dgvRadios.SelectedRows[0].Cells["Id"].Value.ToString();
            string nombre = dgvRadios.SelectedRows[0].Cells["Nombre"].Value.ToString();

            DialogResult result = MessageBox.Show(
                $"Esta seguro de eliminar la radio '{nombre}'?\n\nEsto tambien eliminara las asignaciones de ciudades.",
                "Confirmar eliminacion",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                try
                {
                    string connectionString = PostgreSQLMigration.ConnectionString;

                    using (NpgsqlConnection conn = new NpgsqlConnection(connectionString))
                    {
                        await conn.OpenAsync();

                        // PostgreSQL maneja foreign keys automaticamente si se definen con ON DELETE CASCADE
                        string query = "DELETE FROM Radios WHERE Id = @Id";

                        using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@Id", id);
                            await cmd.ExecuteNonQueryAsync();
                        }
                    }

                    // Sincronizar cambios a la red
                    ConfigManager.NotificarCambioEnBD();

                    await CargarRadiosAsync();
                    MessageBox.Show("Radio eliminada correctamente.", "Exito",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al eliminar radio: {ex.Message}", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private async void BtnGuardarCiudades_Click(object sender, EventArgs e)
        {
            if (dgvRadios.SelectedRows.Count == 0)
            {
                MessageBox.Show("Seleccione una radio para asignar ciudades.", "Advertencia",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int radioId = int.Parse(dgvRadios.SelectedRows[0].Cells["Id"].Value.ToString());

            try
            {
                string connectionString = PostgreSQLMigration.ConnectionString;

                using (NpgsqlConnection conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    // Eliminar asignaciones existentes
                    string deleteQuery = "DELETE FROM RadiosCiudades WHERE RadioId = @RadioId";
                    using (NpgsqlCommand cmd = new NpgsqlCommand(deleteQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@RadioId", radioId);
                        await cmd.ExecuteNonQueryAsync();
                    }

                    // Insertar nuevas asignaciones
                    foreach (var item in clbCiudades.CheckedItems)
                    {
                        string ciudadNombre = item.ToString();

                        string insertQuery = @"
                            INSERT INTO RadiosCiudades (RadioId, CiudadId, Estado)
                            SELECT @RadioId, Id, 'Activo' FROM Ciudades WHERE Nombre = @CiudadNombre";

                        using (NpgsqlCommand cmd = new NpgsqlCommand(insertQuery, conn))
                        {
                            cmd.Parameters.AddWithValue("@RadioId", radioId);
                            cmd.Parameters.AddWithValue("@CiudadNombre", ciudadNombre);
                            await cmd.ExecuteNonQueryAsync();
                        }
                    }
                }

                // Sincronizar cambios a la red
                ConfigManager.NotificarCambioEnBD();

                MessageBox.Show("Ciudades asignadas correctamente.", "Exito",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al asignar ciudades: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void BtnGuardar_Click(object sender, EventArgs e)
        {
            try
            {
                string connectionString = PostgreSQLMigration.ConnectionString;

                using (NpgsqlConnection conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    foreach (DataGridViewRow row in dgvRadios.Rows)
                    {
                        string id = row.Cells["Id"].Value?.ToString();
                        string nombre = row.Cells["Nombre"].Value?.ToString()?.ToUpper();
                        string descripcion = row.Cells["Descripcion"].Value?.ToString() ?? "";
                        string estado = row.Cells["Estado"].Value?.ToString();

                        if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(nombre))
                        {
                            string query = "UPDATE Radios SET Nombre = @Nombre, Descripcion = @Descripcion, Estado = @Estado WHERE Id = @Id";

                            using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn))
                            {
                                cmd.Parameters.AddWithValue("@Id", id);
                                cmd.Parameters.AddWithValue("@Nombre", nombre);
                                cmd.Parameters.AddWithValue("@Descripcion", descripcion);
                                cmd.Parameters.AddWithValue("@Estado", estado ?? "Activo");
                                await cmd.ExecuteNonQueryAsync();
                            }
                        }
                    }
                }

                // Sincronizar cambios a la red
                ConfigManager.NotificarCambioEnBD();

                MessageBox.Show("Cambios guardados correctamente.", "Exito",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar cambios: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Obtiene la lista de radios activas para usar en ComboBoxes
        /// Filtra duplicados como "LA KALLE" y "LAKALLE" (preferencia al nombre con espacios)
        /// </summary>
        public static async Task<List<string>> ObtenerRadiosActivasAsync()
        {
            var radios = new List<string>();

            try
            {
                string connectionString = PostgreSQLMigration.ConnectionString;

                using (NpgsqlConnection conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    string query = "SELECT Nombre FROM Radios WHERE Estado = 'Activo' ORDER BY Nombre";

                    using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn))
                    using (NpgsqlDataReader reader = (NpgsqlDataReader)await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            radios.Add(reader["Nombre"].ToString());
                        }
                    }
                }

                // Filtrar duplicados (nombres que son iguales sin espacios)
                // Ejemplo: "LA KALLE" y "LAKALLE" -> solo mantener "LA KALLE"
                radios = FiltrarRadiosDuplicadas(radios);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al obtener radios: {ex.Message}");
            }

            // Si no hay radios, devolver las por defecto
            if (radios.Count == 0)
            {
                radios.AddRange(new[] { "EXITOSA", "KARIBEÑA", "LA KALLE" });
            }

            return radios;
        }

        /// <summary>
        /// Filtra radios duplicadas que son iguales sin espacios
        /// Ejemplo: "LA KALLE" y "LAKALLE" -> solo mantener "LA KALLE" (el que tiene espacios)
        /// </summary>
        private static List<string> FiltrarRadiosDuplicadas(List<string> radios)
        {
            var resultado = new List<string>();
            var nombresNormalizados = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var radio in radios)
            {
                // Normalizar: quitar espacios y convertir a mayúsculas
                string normalizado = radio.Replace(" ", "").ToUpperInvariant();

                if (!nombresNormalizados.Contains(normalizado))
                {
                    nombresNormalizados.Add(normalizado);
                    resultado.Add(radio);
                }
                else
                {
                    // Si ya existe, preferir el que tiene espacios
                    if (radio.Contains(" "))
                    {
                        // Buscar y reemplazar el existente sin espacios
                        for (int i = 0; i < resultado.Count; i++)
                        {
                            string existenteNorm = resultado[i].Replace(" ", "").ToUpperInvariant();
                            if (existenteNorm == normalizado && !resultado[i].Contains(" "))
                            {
                                resultado[i] = radio;
                                break;
                            }
                        }
                    }
                    // Si el nuevo no tiene espacios, ignorarlo (el existente es mejor)
                }
            }

            return resultado;
        }

        /// <summary>
        /// Obtiene las radios activas que operan en una ciudad especifica
        /// </summary>
        public static async Task<List<string>> ObtenerRadiosPorCiudadAsync(string ciudadNombre)
        {
            var radios = new List<string>();

            try
            {
                string connectionString = PostgreSQLMigration.ConnectionString;

                using (NpgsqlConnection conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    string query = @"
                        SELECT r.Nombre
                        FROM Radios r
                        INNER JOIN RadiosCiudades rc ON r.Id = rc.RadioId
                        INNER JOIN Ciudades c ON rc.CiudadId = c.Id
                        WHERE c.Nombre = @CiudadNombre
                        AND r.Estado = 'Activo'
                        AND rc.Estado = 'Activo'
                        ORDER BY r.Nombre";

                    using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@CiudadNombre", ciudadNombre);

                        using (NpgsqlDataReader reader = (NpgsqlDataReader)await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                radios.Add(reader["Nombre"].ToString());
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al obtener radios por ciudad: {ex.Message}");
            }

            // Si no hay radios para esa ciudad, devolver todas las activas
            if (radios.Count == 0)
            {
                radios = await ObtenerRadiosActivasAsync();
            }

            return radios;
        }
    }
}

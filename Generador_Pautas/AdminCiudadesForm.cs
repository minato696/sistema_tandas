using System;
using System.Collections.Generic;
using Npgsql;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Generador_Pautas
{
    public partial class AdminCiudadesForm : Form
    {
        private DataGridView dgvCiudades;
        private TextBox txtNuevaCiudad;
        private Button btnAgregar;
        private Button btnEliminar;
        private Button btnGuardar;
        private Button btnResetearIds;

        public AdminCiudadesForm()
        {
            InitializeComponent();
            AppIcon.ApplyTo(this); // Aplicar icono de la aplicacion
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // Configuracion del formulario
            this.Text = "Administrar Ciudades";
            this.Size = new Size(500, 450);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(240, 240, 245);

            // Titulo
            Label lblTitulo = new Label
            {
                Text = "Administrar Ciudades",
                Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 120, 215),
                AutoSize = true,
                Location = new Point(20, 15)
            };

            // DataGridView
            dgvCiudades = new DataGridView
            {
                Location = new Point(20, 50),
                Size = new Size(440, 250),
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
            dgvCiudades.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", HeaderText = "ID", Width = 50, ReadOnly = true });
            dgvCiudades.Columns.Add(new DataGridViewTextBoxColumn { Name = "Nombre", HeaderText = "Ciudad", Width = 200 });
            dgvCiudades.Columns.Add(new DataGridViewComboBoxColumn
            {
                Name = "Estado",
                HeaderText = "Estado",
                Width = 100,
                Items = { "Activo", "Inactivo" }
            });

            // Panel para agregar nueva ciudad
            Label lblNueva = new Label
            {
                Text = "Nueva Ciudad:",
                Font = new Font("Segoe UI", 10F),
                AutoSize = true,
                Location = new Point(20, 315)
            };

            txtNuevaCiudad = new TextBox
            {
                Font = new Font("Segoe UI", 10F),
                Size = new Size(250, 25),
                Location = new Point(20, 340)
            };

            btnAgregar = new Button
            {
                Text = "Agregar",
                Font = new Font("Segoe UI", 9F),
                Size = new Size(80, 28),
                Location = new Point(280, 338),
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
                Location = new Point(370, 338),
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
                Size = new Size(320, 35),
                Location = new Point(20, 380),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnGuardar.FlatAppearance.BorderSize = 0;
            btnGuardar.Click += BtnGuardar_Click;

            btnResetearIds = new Button
            {
                Text = "Reset IDs",
                Font = new Font("Segoe UI", 9F),
                Size = new Size(110, 35),
                Location = new Point(350, 380),
                BackColor = Color.FromArgb(100, 100, 100),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnResetearIds.FlatAppearance.BorderSize = 0;
            btnResetearIds.Click += BtnResetearIds_Click;

            // Agregar controles
            this.Controls.Add(lblTitulo);
            this.Controls.Add(dgvCiudades);
            this.Controls.Add(lblNueva);
            this.Controls.Add(txtNuevaCiudad);
            this.Controls.Add(btnAgregar);
            this.Controls.Add(btnEliminar);
            this.Controls.Add(btnGuardar);
            this.Controls.Add(btnResetearIds);

            this.Load += AdminCiudadesForm_Load;

            this.ResumeLayout(false);
        }

        private async void AdminCiudadesForm_Load(object sender, EventArgs e)
        {
            await InsertarCiudadesPorDefectoAsync();
            await CargarCiudadesAsync();
        }

        private async Task InsertarCiudadesPorDefectoAsync()
        {
            string[] ciudadesPorDefecto = {
                "ABANCAY", "ANDAHUAYLAS", "AYACUCHO", "BARRANCA", "CAJAMARCA",
                "CAÑETE", "CERRO DE PASCO", "CHACHAPOYAS", "CHICLAYO", "CHIMBOTE",
                "CHINCHA", "CHULUCANAS", "CUSCO", "HUACHO", "HUANCABAMBA",
                "HUANCAVELICA", "HUANUCO", "HUARAL", "HUARAZ", "HUARMEY",
                "ILO", "JAEN", "JAUJA", "JULIACA", "LIMA",
                "LOS ORGANOS", "MOLLENDO", "MOQUEGUA", "MOYOBAMBA", "PACASMAYO",
                "PAITA", "PISCO", "PIURA", "PUCALLPA", "PUNO",
                "PUERTO MALDONADO", "SULLANA", "TACNA", "TALARA", "TARAPOTO",
                "TINGO MARIA", "TRUJILLO", "TUMBES", "VENTANILLA", "YURIMAGUAS"
            };

            try
            {
                string connectionString = PostgreSQLMigration.ConnectionString;
                using (NpgsqlConnection conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();
                    foreach (string ciudad in ciudadesPorDefecto)
                    {
                        string insertQuery = "INSERT INTO Ciudades (Nombre, Estado) VALUES (@Nombre, 'Activo') ON CONFLICT (Nombre) DO NOTHING";
                        using (NpgsqlCommand cmd = new NpgsqlCommand(insertQuery, conn))
                        {
                            cmd.Parameters.AddWithValue("@Nombre", ciudad);
                            await cmd.ExecuteNonQueryAsync();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error insertando ciudades: {ex.Message}");
            }
        }

        private async Task CargarCiudadesAsync()
        {
            dgvCiudades.Rows.Clear();

            try
            {
                string connectionString = PostgreSQLMigration.ConnectionString;

                using (NpgsqlConnection conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    string query = "SELECT Id, Nombre, Estado FROM Ciudades ORDER BY Nombre";

                    using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn))
                    using (NpgsqlDataReader reader = (NpgsqlDataReader)await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            int rowIndex = dgvCiudades.Rows.Add();
                            dgvCiudades.Rows[rowIndex].Cells["Id"].Value = reader["Id"].ToString();
                            dgvCiudades.Rows[rowIndex].Cells["Nombre"].Value = reader["Nombre"].ToString();
                            dgvCiudades.Rows[rowIndex].Cells["Estado"].Value = reader["Estado"].ToString();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar ciudades: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void BtnAgregar_Click(object sender, EventArgs e)
        {
            string nombreCiudad = txtNuevaCiudad.Text.Trim().ToUpper();

            if (string.IsNullOrEmpty(nombreCiudad))
            {
                MessageBox.Show("Ingrese el nombre de la ciudad.", "Advertencia",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                string connectionString = PostgreSQLMigration.ConnectionString;

                using (NpgsqlConnection conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    string query = "INSERT INTO Ciudades (Nombre, Estado) VALUES (@Nombre, 'Activo')";

                    using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Nombre", nombreCiudad);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                // Sincronizar cambios a la red
                ConfigManager.NotificarCambioEnBD();

                txtNuevaCiudad.Text = "";
                await CargarCiudadesAsync();
                MessageBox.Show("Ciudad agregada correctamente.", "Exito",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (PostgresException ex) when (ex.Message.Contains("UNIQUE"))
            {
                MessageBox.Show("Ya existe una ciudad con ese nombre.", "Advertencia",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al agregar ciudad: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void BtnEliminar_Click(object sender, EventArgs e)
        {
            if (dgvCiudades.SelectedRows.Count == 0)
            {
                MessageBox.Show("Seleccione una ciudad para eliminar.", "Advertencia",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string id = dgvCiudades.SelectedRows[0].Cells["Id"].Value.ToString();
            string nombre = dgvCiudades.SelectedRows[0].Cells["Nombre"].Value.ToString();

            DialogResult result = MessageBox.Show(
                $"Esta seguro de eliminar la ciudad '{nombre}'?",
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

                        string query = "DELETE FROM Ciudades WHERE Id = @Id";

                        using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@Id", id);
                            await cmd.ExecuteNonQueryAsync();
                        }
                    }

                    // Sincronizar cambios a la red
                    ConfigManager.NotificarCambioEnBD();

                    await CargarCiudadesAsync();
                    MessageBox.Show("Ciudad eliminada correctamente.", "Exito",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al eliminar ciudad: {ex.Message}", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
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

                    foreach (DataGridViewRow row in dgvCiudades.Rows)
                    {
                        string id = row.Cells["Id"].Value?.ToString();
                        string nombre = row.Cells["Nombre"].Value?.ToString()?.ToUpper();
                        string estado = row.Cells["Estado"].Value?.ToString();

                        if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(nombre))
                        {
                            string query = "UPDATE Ciudades SET Nombre = @Nombre, Estado = @Estado WHERE Id = @Id";

                            using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn))
                            {
                                cmd.Parameters.AddWithValue("@Id", id);
                                cmd.Parameters.AddWithValue("@Nombre", nombre);
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

                // Cerrar el formulario despues de guardar
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar cambios: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void BtnResetearIds_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show(
                "Esto recreara la tabla de ciudades con IDs desde 1.\n" +
                "Los datos se mantendran, solo cambiaran los IDs.\n\n" +
                "¿Desea continuar?",
                "Resetear IDs",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
                return;

            var ciudades = new List<(string Nombre, string Estado)>();

            try
            {
                string connectionString = PostgreSQLMigration.ConnectionString;

                using (NpgsqlConnection conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    // 1. Obtener todas las ciudades actuales
                    string selectQuery = "SELECT Nombre, Estado FROM Ciudades ORDER BY Nombre";
                    using (NpgsqlCommand cmd = new NpgsqlCommand(selectQuery, conn))
                    using (NpgsqlDataReader reader = (NpgsqlDataReader)await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            ciudades.Add((reader["Nombre"].ToString(), reader["Estado"].ToString()));
                        }
                    }

                    // 2. Eliminar la tabla actual
                    using (NpgsqlCommand cmd = new NpgsqlCommand("DROP TABLE IF EXISTS Ciudades", conn))
                    {
                        await cmd.ExecuteNonQueryAsync();
                    }

                    // 3. Recrear la tabla (esto resetea el SERIAL)
                    string createTableQuery = @"
                        CREATE TABLE Ciudades (
                            Id SERIAL PRIMARY KEY,
                            Nombre TEXT NOT NULL UNIQUE,
                            Estado TEXT NOT NULL DEFAULT 'Activo'
                        )";
                    using (NpgsqlCommand cmd = new NpgsqlCommand(createTableQuery, conn))
                    {
                        await cmd.ExecuteNonQueryAsync();
                    }

                    // 4. Reinsertar todas las ciudades (ahora tendran IDs 1, 2, 3, etc.)
                    foreach (var ciudad in ciudades)
                    {
                        string insertQuery = "INSERT INTO Ciudades (Nombre, Estado) VALUES (@Nombre, @Estado)";
                        using (NpgsqlCommand cmd = new NpgsqlCommand(insertQuery, conn))
                        {
                            cmd.Parameters.AddWithValue("@Nombre", ciudad.Nombre);
                            cmd.Parameters.AddWithValue("@Estado", ciudad.Estado);
                            await cmd.ExecuteNonQueryAsync();
                        }
                    }
                }

                // Sincronizar cambios a la red
                ConfigManager.NotificarCambioEnBD();

                await CargarCiudadesAsync();
                MessageBox.Show($"IDs reseteados correctamente.\n{ciudades.Count} ciudades con IDs del 1 al {ciudades.Count}.",
                    "Exito", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al resetear IDs: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Obtiene la lista de ciudades activas para usar en ComboBoxes
        /// </summary>
        public static async Task<List<string>> ObtenerCiudadesActivasAsync()
        {
            var ciudades = new List<string>();

            try
            {
                string connectionString = PostgreSQLMigration.ConnectionString;

                using (NpgsqlConnection conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    string query = "SELECT Nombre FROM Ciudades WHERE Estado = 'Activo' ORDER BY Nombre";

                    using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn))
                    using (NpgsqlDataReader reader = (NpgsqlDataReader)await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            ciudades.Add(reader["Nombre"].ToString());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al obtener ciudades: {ex.Message}");
            }

            // Si no hay ciudades, devolver las por defecto
            if (ciudades.Count == 0)
            {
                ciudades.AddRange(new[] { "LIMA", "PROVINCIAS" });
            }

            return ciudades;
        }
    }
}

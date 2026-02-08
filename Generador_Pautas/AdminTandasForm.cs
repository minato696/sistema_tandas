using System;
using System.Collections.Generic;
using Npgsql;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Generador_Pautas
{
    public partial class AdminTandasForm : Form
    {
        private DataGridView dgvTandas;
        private TextBox txtNombreTanda;
        private TextBox txtHorarios;
        private Button btnAgregar;
        private Button btnEliminar;
        private Button btnGuardar;
        private Label lblInfo;

        public AdminTandasForm()
        {
            InitializeComponent();
            AppIcon.ApplyTo(this); // Aplicar icono de la aplicacion
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // Configuracion del formulario
            this.Text = "Administrar Tandas de Programacion";
            this.Size = new Size(700, 550);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(240, 240, 245);

            // Titulo
            Label lblTitulo = new Label
            {
                Text = "Administrar Tandas de Programacion",
                Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 120, 215),
                AutoSize = true,
                Location = new Point(20, 15)
            };

            // DataGridView
            dgvTandas = new DataGridView
            {
                Location = new Point(20, 50),
                Size = new Size(640, 220),
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
            dgvTandas.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", HeaderText = "ID", Width = 40, ReadOnly = true });
            dgvTandas.Columns.Add(new DataGridViewTextBoxColumn { Name = "Nombre", HeaderText = "Nombre de Tanda", Width = 150 });
            dgvTandas.Columns.Add(new DataGridViewTextBoxColumn { Name = "Horarios", HeaderText = "Horarios (separados por coma)", Width = 350 });
            dgvTandas.Columns.Add(new DataGridViewComboBoxColumn
            {
                Name = "Estado",
                HeaderText = "Estado",
                Width = 80,
                Items = { "Activo", "Inactivo" }
            });

            dgvTandas.SelectionChanged += DgvTandas_SelectionChanged;

            // Panel para agregar nueva tanda
            Label lblNueva = new Label
            {
                Text = "Nueva Tanda:",
                Font = new Font("Segoe UI Semibold", 10F),
                AutoSize = true,
                Location = new Point(20, 285)
            };

            Label lblNombre = new Label
            {
                Text = "Nombre:",
                Font = new Font("Segoe UI", 9F),
                AutoSize = true,
                Location = new Point(20, 315)
            };

            txtNombreTanda = new TextBox
            {
                Font = new Font("Segoe UI", 10F),
                Size = new Size(200, 25),
                Location = new Point(20, 335)
            };

            Label lblHorarios = new Label
            {
                Text = "Horarios (separados por coma, ej: 06:00,06:30,07:00):",
                Font = new Font("Segoe UI", 9F),
                AutoSize = true,
                Location = new Point(20, 370)
            };

            txtHorarios = new TextBox
            {
                Font = new Font("Segoe UI", 10F),
                Size = new Size(640, 25),
                Location = new Point(20, 390),
                Multiline = false
            };

            lblInfo = new Label
            {
                Text = "Ejemplo: 06:00,06:30,07:00,07:30,08:00,08:30...",
                Font = new Font("Segoe UI", 8F, FontStyle.Italic),
                ForeColor = Color.Gray,
                AutoSize = true,
                Location = new Point(20, 420)
            };

            btnAgregar = new Button
            {
                Text = "Agregar Tanda",
                Font = new Font("Segoe UI", 9F),
                Size = new Size(120, 30),
                Location = new Point(20, 450),
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
                Size = new Size(100, 30),
                Location = new Point(150, 450),
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
                Size = new Size(200, 35),
                Location = new Point(460, 450),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnGuardar.FlatAppearance.BorderSize = 0;
            btnGuardar.Click += BtnGuardar_Click;

            // Agregar controles
            this.Controls.Add(lblTitulo);
            this.Controls.Add(dgvTandas);
            this.Controls.Add(lblNueva);
            this.Controls.Add(lblNombre);
            this.Controls.Add(txtNombreTanda);
            this.Controls.Add(lblHorarios);
            this.Controls.Add(txtHorarios);
            this.Controls.Add(lblInfo);
            this.Controls.Add(btnAgregar);
            this.Controls.Add(btnEliminar);
            this.Controls.Add(btnGuardar);

            this.Load += AdminTandasForm_Load;

            this.ResumeLayout(false);
        }

        private async void AdminTandasForm_Load(object sender, EventArgs e)
        {
            await CargarTandasAsync();
        }

        private void DgvTandas_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvTandas.SelectedRows.Count > 0)
            {
                txtNombreTanda.Text = dgvTandas.SelectedRows[0].Cells["Nombre"].Value?.ToString() ?? "";
                txtHorarios.Text = dgvTandas.SelectedRows[0].Cells["Horarios"].Value?.ToString() ?? "";
            }
        }

        private async Task CargarTandasAsync()
        {
            dgvTandas.Rows.Clear();

            try
            {
                string connectionString = PostgreSQLMigration.ConnectionString;

                using (NpgsqlConnection conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    string query = "SELECT Id, Nombre, Horarios, Estado FROM TandasProgramacion ORDER BY Nombre";

                    using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn))
                    using (NpgsqlDataReader reader = (NpgsqlDataReader)await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            int rowIndex = dgvTandas.Rows.Add();
                            dgvTandas.Rows[rowIndex].Cells["Id"].Value = reader["Id"].ToString();
                            dgvTandas.Rows[rowIndex].Cells["Nombre"].Value = reader["Nombre"].ToString();
                            dgvTandas.Rows[rowIndex].Cells["Horarios"].Value = reader["Horarios"].ToString();
                            dgvTandas.Rows[rowIndex].Cells["Estado"].Value = reader["Estado"].ToString();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar tandas: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void BtnAgregar_Click(object sender, EventArgs e)
        {
            string nombre = txtNombreTanda.Text.Trim();
            string horarios = txtHorarios.Text.Trim();

            if (string.IsNullOrEmpty(nombre))
            {
                MessageBox.Show("Ingrese el nombre de la tanda.", "Advertencia",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrEmpty(horarios))
            {
                MessageBox.Show("Ingrese los horarios de la tanda.", "Advertencia",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Validar formato de horarios
            if (!ValidarFormatoHorarios(horarios))
            {
                MessageBox.Show("Formato de horarios invalido. Use el formato HH:MM separado por comas.\nEjemplo: 06:00,06:30,07:00",
                    "Advertencia", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                string connectionString = PostgreSQLMigration.ConnectionString;

                using (NpgsqlConnection conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    string query = "INSERT INTO TandasProgramacion (Nombre, Horarios, Estado) VALUES (@Nombre, @Horarios, 'Activo')";

                    using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Nombre", nombre);
                        cmd.Parameters.AddWithValue("@Horarios", horarios);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                // Sincronizar cambios a la red
                ConfigManager.NotificarCambioEnBD();

                txtNombreTanda.Text = "";
                txtHorarios.Text = "";
                await CargarTandasAsync();
                MessageBox.Show("Tanda agregada correctamente.", "Exito",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (PostgresException ex) when (ex.Message.Contains("UNIQUE"))
            {
                MessageBox.Show("Ya existe una tanda con ese nombre.", "Advertencia",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al agregar tanda: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool ValidarFormatoHorarios(string horarios)
        {
            try
            {
                string[] partes = horarios.Split(',');
                foreach (string parte in partes)
                {
                    string hora = parte.Trim();
                    if (hora.Length != 5 || hora[2] != ':')
                        return false;

                    if (!int.TryParse(hora.Substring(0, 2), out int hh) || hh < 0 || hh > 23)
                        return false;

                    if (!int.TryParse(hora.Substring(3, 2), out int mm) || mm < 0 || mm > 59)
                        return false;
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        private async void BtnEliminar_Click(object sender, EventArgs e)
        {
            if (dgvTandas.SelectedRows.Count == 0)
            {
                MessageBox.Show("Seleccione una tanda para eliminar.", "Advertencia",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string id = dgvTandas.SelectedRows[0].Cells["Id"].Value.ToString();
            string nombre = dgvTandas.SelectedRows[0].Cells["Nombre"].Value.ToString();

            DialogResult result = MessageBox.Show(
                $"Esta seguro de eliminar la tanda '{nombre}'?",
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

                        string query = "DELETE FROM TandasProgramacion WHERE Id = @Id";

                        using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@Id", id);
                            await cmd.ExecuteNonQueryAsync();
                        }
                    }

                    // Sincronizar cambios a la red
                    ConfigManager.NotificarCambioEnBD();

                    txtNombreTanda.Text = "";
                    txtHorarios.Text = "";
                    await CargarTandasAsync();
                    MessageBox.Show("Tanda eliminada correctamente.", "Exito",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al eliminar tanda: {ex.Message}", "Error",
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

                    foreach (DataGridViewRow row in dgvTandas.Rows)
                    {
                        string id = row.Cells["Id"].Value?.ToString();
                        string nombre = row.Cells["Nombre"].Value?.ToString();
                        string horarios = row.Cells["Horarios"].Value?.ToString();
                        string estado = row.Cells["Estado"].Value?.ToString();

                        if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(nombre) && !string.IsNullOrEmpty(horarios))
                        {
                            string query = "UPDATE TandasProgramacion SET Nombre = @Nombre, Horarios = @Horarios, Estado = @Estado WHERE Id = @Id";

                            using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn))
                            {
                                cmd.Parameters.AddWithValue("@Id", id);
                                cmd.Parameters.AddWithValue("@Nombre", nombre);
                                cmd.Parameters.AddWithValue("@Horarios", horarios);
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
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar cambios: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Obtiene la lista de tandas activas para usar en ComboBoxes
        /// </summary>
        public static async Task<List<(string Nombre, string[] Horarios)>> ObtenerTandasActivasAsync()
        {
            var tandas = new List<(string Nombre, string[] Horarios)>();

            try
            {
                string connectionString = PostgreSQLMigration.ConnectionString;

                using (NpgsqlConnection conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    string query = "SELECT Nombre, Horarios FROM TandasProgramacion WHERE Estado = 'Activo' ORDER BY Nombre";

                    using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn))
                    using (NpgsqlDataReader reader = (NpgsqlDataReader)await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            string nombre = reader["Nombre"].ToString();
                            string[] horarios = reader["Horarios"].ToString().Split(',').Select(h => h.Trim()).ToArray();
                            tandas.Add((nombre, horarios));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al obtener tandas: {ex.Message}");
            }

            return tandas;
        }
    }
}

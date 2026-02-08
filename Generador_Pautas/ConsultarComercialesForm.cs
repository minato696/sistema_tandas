using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using Npgsql;

namespace Generador_Pautas
{
    /// <summary>
    /// Formulario para consultar comerciales importados - estilo sistema antiguo
    /// Flujo: Seleccionar Radio + Ciudad → Ver lista de comerciales → Ver fechas/horas pauteadas
    /// </summary>
    public class ConsultarComercialesForm : Form
    {
        // Controles
        private ComboBox cboRadio;
        private ComboBox cboCiudad;
        private DataGridView dgvComerciales;
        private DataGridView dgvPautas;
        private Label lblInfo;
        private Label lblTituloComerciales;
        private Label lblTituloPautas;
        private TextBox txtBuscar;
        private Button btnBuscar;
        private Button btnEliminar;
        private Button btnEliminarPorFecha;
        private DateTimePicker dtpFechaInicio;
        private DateTimePicker dtpFechaFin;
        private Label lblEstado;

        private string _connectionString;

        public ConsultarComercialesForm()
        {
            _connectionString = ConfigManager.ObtenerPostgreSQLConnectionString();
            InitializeComponent();
            CargarRadios();
        }

        private void InitializeComponent()
        {
            this.Text = "Consultar Comerciales - Sistema de Pautas";
            this.Size = new Size(1200, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(240, 240, 245);

            // Panel superior - Filtros
            Panel pnlFiltros = new Panel
            {
                Dock = DockStyle.Top,
                Height = 100,
                BackColor = Color.FromArgb(63, 81, 181),
                Padding = new Padding(15)
            };

            Label lblTitulo = new Label
            {
                Text = "Consultar Comerciales",
                Font = new Font("Segoe UI Semibold", 16F, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(15, 10)
            };

            // Radio
            Label lblRadio = new Label
            {
                Text = "ESTACION:",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Location = new Point(15, 50),
                AutoSize = true
            };

            cboRadio = new ComboBox
            {
                Location = new Point(90, 47),
                Width = 150,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9F)
            };
            cboRadio.SelectedIndexChanged += CboRadio_SelectedIndexChanged;

            // Ciudad
            Label lblCiudad = new Label
            {
                Text = "CIUDAD:",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Location = new Point(260, 50),
                AutoSize = true
            };

            cboCiudad = new ComboBox
            {
                Location = new Point(325, 47),
                Width = 180,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9F)
            };
            cboCiudad.SelectedIndexChanged += CboCiudad_SelectedIndexChanged;

            // Buscar
            Label lblBuscar = new Label
            {
                Text = "BUSCAR:",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Location = new Point(530, 50),
                AutoSize = true
            };

            txtBuscar = new TextBox
            {
                Location = new Point(590, 47),
                Width = 200,
                Font = new Font("Segoe UI", 9F)
            };

            btnBuscar = new Button
            {
                Text = "Buscar",
                Location = new Point(800, 45),
                Width = 80,
                Height = 28,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F),
                Cursor = Cursors.Hand
            };
            btnBuscar.FlatAppearance.BorderSize = 0;
            btnBuscar.Click += BtnBuscar_Click;

            pnlFiltros.Controls.AddRange(new Control[] {
                lblTitulo, lblRadio, cboRadio, lblCiudad, cboCiudad,
                lblBuscar, txtBuscar, btnBuscar
            });
            this.Controls.Add(pnlFiltros);

            // Panel principal con split
            SplitContainer splitMain = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 450,
                Panel1MinSize = 300,
                Panel2MinSize = 300
            };

            // Panel izquierdo - Lista de comerciales
            Panel pnlComerciales = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };

            lblTituloComerciales = new Label
            {
                Text = "COMERCIALES (Codigo_avi)",
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(63, 81, 181),
                Dock = DockStyle.Top,
                Height = 30
            };

            dgvComerciales = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                Font = new Font("Segoe UI", 9F)
            };
            dgvComerciales.SelectionChanged += DgvComerciales_SelectionChanged;

            // Configurar estilo del DataGridView
            dgvComerciales.EnableHeadersVisualStyles = false;
            dgvComerciales.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(63, 81, 181);
            dgvComerciales.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvComerciales.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9F);
            dgvComerciales.ColumnHeadersHeight = 35;
            dgvComerciales.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(245, 245, 250);

            pnlComerciales.Controls.Add(dgvComerciales);
            pnlComerciales.Controls.Add(lblTituloComerciales);
            splitMain.Panel1.Controls.Add(pnlComerciales);

            // Panel derecho - Pautas del comercial seleccionado
            Panel pnlPautas = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };

            lblTituloPautas = new Label
            {
                Text = "FECHAS Y HORAS PAUTEADAS",
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(76, 175, 80),
                Dock = DockStyle.Top,
                Height = 30
            };

            dgvPautas = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                Font = new Font("Segoe UI", 9F)
            };

            dgvPautas.EnableHeadersVisualStyles = false;
            dgvPautas.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(76, 175, 80);
            dgvPautas.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvPautas.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9F);
            dgvPautas.ColumnHeadersHeight = 35;
            dgvPautas.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(245, 250, 245);

            pnlPautas.Controls.Add(dgvPautas);
            pnlPautas.Controls.Add(lblTituloPautas);
            splitMain.Panel2.Controls.Add(pnlPautas);

            this.Controls.Add(splitMain);

            // Panel inferior - Acciones y estado
            Panel pnlAcciones = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 80,
                BackColor = Color.FromArgb(235, 235, 240),
                Padding = new Padding(15)
            };

            Label lblFechaI = new Label
            {
                Text = "Fecha I:",
                AutoSize = true,
                Location = new Point(15, 15),
                Font = new Font("Segoe UI", 9F)
            };

            dtpFechaInicio = new DateTimePicker
            {
                Location = new Point(70, 12),
                Width = 120,
                Format = DateTimePickerFormat.Short
            };

            Label lblFechaF = new Label
            {
                Text = "Fecha F:",
                AutoSize = true,
                Location = new Point(200, 15),
                Font = new Font("Segoe UI", 9F)
            };

            dtpFechaFin = new DateTimePicker
            {
                Location = new Point(255, 12),
                Width = 120,
                Format = DateTimePickerFormat.Short
            };

            btnEliminarPorFecha = new Button
            {
                Text = "Eliminar por Fecha",
                Location = new Point(390, 10),
                Width = 130,
                Height = 30,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(255, 152, 0),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F),
                Cursor = Cursors.Hand
            };
            btnEliminarPorFecha.FlatAppearance.BorderSize = 0;
            btnEliminarPorFecha.Click += BtnEliminarPorFecha_Click;

            btnEliminar = new Button
            {
                Text = "Eliminar Seleccionado",
                Location = new Point(530, 10),
                Width = 150,
                Height = 30,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(244, 67, 54),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F),
                Cursor = Cursors.Hand
            };
            btnEliminar.FlatAppearance.BorderSize = 0;
            btnEliminar.Click += BtnEliminar_Click;

            lblEstado = new Label
            {
                Text = "Seleccione una estación y ciudad para ver los comerciales",
                AutoSize = true,
                Location = new Point(15, 50),
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.Gray
            };

            pnlAcciones.Controls.AddRange(new Control[] {
                lblFechaI, dtpFechaInicio, lblFechaF, dtpFechaFin,
                btnEliminarPorFecha, btnEliminar, lblEstado
            });
            this.Controls.Add(pnlAcciones);
        }

        private async void CargarRadios()
        {
            try
            {
                using (var conn = new NpgsqlConnection(_connectionString))
                {
                    await conn.OpenAsync();
                    string query = "SELECT DISTINCT Radio FROM Comerciales WHERE Radio IS NOT NULL AND Radio != '' ORDER BY Radio";
                    using (var cmd = new NpgsqlCommand(query, conn))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        cboRadio.Items.Clear();
                        cboRadio.Items.Add("-- Seleccionar --");
                        while (await reader.ReadAsync())
                        {
                            cboRadio.Items.Add(reader["Radio"].ToString());
                        }
                        cboRadio.SelectedIndex = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar radios: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void CboRadio_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboRadio.SelectedIndex <= 0)
            {
                cboCiudad.Items.Clear();
                cboCiudad.Items.Add("-- Seleccionar --");
                cboCiudad.SelectedIndex = 0;
                dgvComerciales.DataSource = null;
                dgvPautas.DataSource = null;
                return;
            }

            string radio = cboRadio.SelectedItem.ToString();
            await CargarCiudadesPorRadio(radio);
        }

        private async Task CargarCiudadesPorRadio(string radio)
        {
            try
            {
                using (var conn = new NpgsqlConnection(_connectionString))
                {
                    await conn.OpenAsync();
                    string query = @"SELECT DISTINCT Ciudad FROM Comerciales
                                     WHERE Radio = @Radio AND Ciudad IS NOT NULL AND Ciudad != ''
                                     ORDER BY Ciudad";
                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Radio", radio);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            cboCiudad.Items.Clear();
                            cboCiudad.Items.Add("-- Seleccionar --");
                            while (await reader.ReadAsync())
                            {
                                cboCiudad.Items.Add(reader["Ciudad"].ToString());
                            }
                            cboCiudad.SelectedIndex = 0;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar ciudades: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void CboCiudad_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboRadio.SelectedIndex <= 0 || cboCiudad.SelectedIndex <= 0)
            {
                dgvComerciales.DataSource = null;
                dgvPautas.DataSource = null;
                return;
            }

            string radio = cboRadio.SelectedItem.ToString();
            string ciudad = cboCiudad.SelectedItem.ToString();
            await CargarComerciales(radio, ciudad);
        }

        private async void BtnBuscar_Click(object sender, EventArgs e)
        {
            if (cboRadio.SelectedIndex <= 0 || cboCiudad.SelectedIndex <= 0)
            {
                MessageBox.Show("Seleccione primero una estación y ciudad", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string radio = cboRadio.SelectedItem.ToString();
            string ciudad = cboCiudad.SelectedItem.ToString();
            await CargarComerciales(radio, ciudad, txtBuscar.Text);
        }

        private async Task CargarComerciales(string radio, string ciudad, string busqueda = null)
        {
            try
            {
                lblEstado.Text = "Cargando comerciales...";
                Application.DoEvents();

                using (var conn = new NpgsqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    // Extraer Codigo_avi del código PostgreSQL (formato: ACC-{codigoAvi}-...)
                    // Agrupar por Codigo_avi para mostrar como el sistema antiguo
                    string query = @"
                        SELECT
                            SPLIT_PART(Codigo, '-', 2) as Codigo_avi,
                            MIN(Posicion) as POS,
                            MIN(FilePath) as RUTA,
                            Ciudad as CIUDAD,
                            COUNT(*) as Total_Horas,
                            MIN(FechaInicio) as Fecha_Inicio,
                            MAX(FechaFinal) as Fecha_Final,
                            CASE WHEN COUNT(*) FILTER (WHERE Estado = 'Activo') > 0 THEN 'Activo' ELSE 'Inactivo' END as Estado
                        FROM Comerciales
                        WHERE Radio = @Radio AND Ciudad = @Ciudad";

                    if (!string.IsNullOrWhiteSpace(busqueda))
                    {
                        query += " AND (FilePath ILIKE @Busqueda OR Codigo ILIKE @Busqueda)";
                    }

                    query += @" GROUP BY SPLIT_PART(Codigo, '-', 2), Ciudad
                                ORDER BY Codigo_avi DESC";

                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Radio", radio);
                        cmd.Parameters.AddWithValue("@Ciudad", ciudad);
                        if (!string.IsNullOrWhiteSpace(busqueda))
                        {
                            cmd.Parameters.AddWithValue("@Busqueda", $"%{busqueda}%");
                        }

                        DataTable dt = new DataTable();
                        using (var adapter = new NpgsqlDataAdapter(cmd))
                        {
                            adapter.Fill(dt);
                        }

                        dgvComerciales.DataSource = dt;

                        // Ajustar anchos de columnas
                        if (dgvComerciales.Columns.Contains("Codigo_avi"))
                            dgvComerciales.Columns["Codigo_avi"].Width = 80;
                        if (dgvComerciales.Columns.Contains("POS"))
                            dgvComerciales.Columns["POS"].Width = 50;
                        if (dgvComerciales.Columns.Contains("RUTA"))
                            dgvComerciales.Columns["RUTA"].Width = 300;
                        if (dgvComerciales.Columns.Contains("Total_Horas"))
                            dgvComerciales.Columns["Total_Horas"].Width = 80;

                        lblTituloComerciales.Text = $"{ciudad} => {radio} ({dt.Rows.Count} comerciales)";
                        lblEstado.Text = $"Se encontraron {dt.Rows.Count} comerciales";
                    }
                }
            }
            catch (Exception ex)
            {
                lblEstado.Text = "Error al cargar comerciales";
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void DgvComerciales_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvComerciales.SelectedRows.Count == 0)
            {
                dgvPautas.DataSource = null;
                return;
            }

            var row = dgvComerciales.SelectedRows[0];
            string codigoAvi = row.Cells["Codigo_avi"].Value?.ToString();
            string ciudad = row.Cells["CIUDAD"].Value?.ToString();
            string radio = cboRadio.SelectedItem?.ToString();

            if (string.IsNullOrEmpty(codigoAvi)) return;

            await CargarPautasDelComercial(codigoAvi, ciudad, radio);
        }

        private async Task CargarPautasDelComercial(string codigoAvi, string ciudad, string radio)
        {
            try
            {
                using (var conn = new NpgsqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    // Obtener todas las fechas y horas para este comercial
                    // La hora se extrae del código (formato: ACC-{codigoAvi}-{ciudad}-{radio}-{hora})
                    string query = @"
                        SELECT
                            FechaInicio as FECHA,
                            SUBSTRING(Codigo FROM '[0-9]{4}$') as HORA_RAW,
                            CASE
                                WHEN LENGTH(SUBSTRING(Codigo FROM '[0-9]{4}$')) = 4
                                THEN SUBSTRING(SUBSTRING(Codigo FROM '[0-9]{4}$'), 1, 2) || ':' || SUBSTRING(SUBSTRING(Codigo FROM '[0-9]{4}$'), 3, 2)
                                ELSE 'N/A'
                            END as HORA,
                            Posicion as POS,
                            Estado,
                            Codigo
                        FROM Comerciales
                        WHERE SPLIT_PART(Codigo, '-', 2) = @CodigoAvi
                          AND Ciudad = @Ciudad
                          AND Radio = @Radio
                        ORDER BY FechaInicio, HORA";

                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@CodigoAvi", codigoAvi);
                        cmd.Parameters.AddWithValue("@Ciudad", ciudad);
                        cmd.Parameters.AddWithValue("@Radio", radio);

                        DataTable dt = new DataTable();
                        using (var adapter = new NpgsqlDataAdapter(cmd))
                        {
                            adapter.Fill(dt);
                        }

                        dgvPautas.DataSource = dt;

                        // Ocultar columnas innecesarias
                        if (dgvPautas.Columns.Contains("HORA_RAW"))
                            dgvPautas.Columns["HORA_RAW"].Visible = false;
                        if (dgvPautas.Columns.Contains("Codigo"))
                            dgvPautas.Columns["Codigo"].Visible = false;

                        // Obtener nombre del archivo para el título
                        if (dgvComerciales.SelectedRows.Count > 0)
                        {
                            string ruta = dgvComerciales.SelectedRows[0].Cells["RUTA"].Value?.ToString() ?? "";
                            string nombreArchivo = System.IO.Path.GetFileNameWithoutExtension(ruta);
                            lblTituloPautas.Text = $"{nombreArchivo} ({dt.Rows.Count} pautas)";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar pautas: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void BtnEliminar_Click(object sender, EventArgs e)
        {
            if (dgvPautas.SelectedRows.Count == 0)
            {
                MessageBox.Show("Seleccione una pauta para eliminar", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var row = dgvPautas.SelectedRows[0];
            string codigo = row.Cells["Codigo"].Value?.ToString();

            if (string.IsNullOrEmpty(codigo)) return;

            var result = MessageBox.Show(
                $"¿Está seguro de eliminar este registro?\n\nCódigo: {codigo}",
                "Confirmar eliminación",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result != DialogResult.Yes) return;

            try
            {
                using (var conn = new NpgsqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    // Primero eliminar de ComercialesAsignados
                    using (var cmd = new NpgsqlCommand("DELETE FROM ComercialesAsignados WHERE Codigo = @Codigo", conn))
                    {
                        cmd.Parameters.AddWithValue("@Codigo", codigo);
                        await cmd.ExecuteNonQueryAsync();
                    }

                    // Luego eliminar de Comerciales
                    using (var cmd = new NpgsqlCommand("DELETE FROM Comerciales WHERE Codigo = @Codigo", conn))
                    {
                        cmd.Parameters.AddWithValue("@Codigo", codigo);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                lblEstado.Text = "Registro eliminado correctamente";

                // Recargar datos
                string radio = cboRadio.SelectedItem?.ToString();
                string ciudad = cboCiudad.SelectedItem?.ToString();
                if (!string.IsNullOrEmpty(radio) && !string.IsNullOrEmpty(ciudad))
                {
                    await CargarComerciales(radio, ciudad);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al eliminar: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void BtnEliminarPorFecha_Click(object sender, EventArgs e)
        {
            if (dgvComerciales.SelectedRows.Count == 0)
            {
                MessageBox.Show("Seleccione un comercial primero", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var row = dgvComerciales.SelectedRows[0];
            string codigoAvi = row.Cells["Codigo_avi"].Value?.ToString();
            string ciudad = row.Cells["CIUDAD"].Value?.ToString();
            string radio = cboRadio.SelectedItem?.ToString();

            DateTime fechaI = dtpFechaInicio.Value.Date;
            DateTime fechaF = dtpFechaFin.Value.Date;

            if (fechaI > fechaF)
            {
                MessageBox.Show("La fecha inicial no puede ser mayor a la fecha final", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"¿Eliminar todas las pautas del comercial {codigoAvi}\nentre {fechaI:dd/MM/yyyy} y {fechaF:dd/MM/yyyy}?",
                "Confirmar eliminación por fecha",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result != DialogResult.Yes) return;

            try
            {
                int eliminados = 0;
                using (var conn = new NpgsqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    // Obtener códigos a eliminar
                    string selectQuery = @"
                        SELECT Codigo FROM Comerciales
                        WHERE SPLIT_PART(Codigo, '-', 2) = @CodigoAvi
                          AND Ciudad = @Ciudad
                          AND Radio = @Radio
                          AND FechaInicio >= @FechaI
                          AND FechaFinal <= @FechaF";

                    var codigos = new List<string>();
                    using (var cmd = new NpgsqlCommand(selectQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@CodigoAvi", codigoAvi);
                        cmd.Parameters.AddWithValue("@Ciudad", ciudad);
                        cmd.Parameters.AddWithValue("@Radio", radio);
                        cmd.Parameters.AddWithValue("@FechaI", fechaI);
                        cmd.Parameters.AddWithValue("@FechaF", fechaF);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                codigos.Add(reader["Codigo"].ToString());
                            }
                        }
                    }

                    // Eliminar cada código
                    foreach (var codigo in codigos)
                    {
                        using (var cmd = new NpgsqlCommand("DELETE FROM ComercialesAsignados WHERE Codigo = @Codigo", conn))
                        {
                            cmd.Parameters.AddWithValue("@Codigo", codigo);
                            await cmd.ExecuteNonQueryAsync();
                        }

                        using (var cmd = new NpgsqlCommand("DELETE FROM Comerciales WHERE Codigo = @Codigo", conn))
                        {
                            cmd.Parameters.AddWithValue("@Codigo", codigo);
                            eliminados += await cmd.ExecuteNonQueryAsync();
                        }
                    }
                }

                lblEstado.Text = $"Se eliminaron {eliminados} registros";
                MessageBox.Show($"Se eliminaron {eliminados} registros", "Eliminación completada", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Recargar datos
                await CargarComerciales(radio, ciudad);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al eliminar: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Npgsql;

namespace Generador_Pautas
{
    public class VerPautasComercialForm : Form
    {
        private DataGridView dgvPautas;
        private TextBox txtCodigo;
        private Button btnBuscar;
        private Label lblTitulo;
        private Label lblComercialInfo;
        private GroupBox grpEliminar;
        private GroupBox grpEliminarPorHora;
        private DateTimePicker dtpFechaInicio;
        private DateTimePicker dtpFechaFinal;
        private Button btnEliminarPorFecha;
        private ComboBox cboHoras;
        private Button btnEliminarPorHora;
        private Label lblTotalPautas;

        private string codigoActual = "";
        private string comercialNombre = "";
        private string ciudadActual = "";
        private string radioActual = "";
        private DateTime fechaInicioActual = DateTime.Today;
        private DateTime fechaFinalActual = DateTime.Today;
        private List<string> horasPauteadas = new List<string>();

        public VerPautasComercialForm()
        {
            InitializeComponent();
        }

        public VerPautasComercialForm(string codigo) : this()
        {
            txtCodigo.Text = codigo;
            // Cargar automáticamente al abrir
            this.Shown += async (s, e) => await BuscarPautasAsync();
        }

        private void InitializeComponent()
        {
            this.Text = "Ver Pautas por Comercial";
            this.Size = new Size(700, 680);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(245, 245, 250);
            AppIcon.ApplyTo(this);

            // Panel superior
            Panel pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Color.FromArgb(63, 81, 181),
                Padding = new Padding(15)
            };

            lblTitulo = new Label
            {
                Text = "Ver Pautas por Comercial",
                Font = new Font("Segoe UI Semibold", 16F),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(15, 15)
            };
            pnlTop.Controls.Add(lblTitulo);

            // Panel de búsqueda
            Panel pnlBusqueda = new Panel
            {
                Location = new Point(15, 75),
                Size = new Size(655, 50)
            };

            Label lblCodigo = new Label
            {
                Text = "Código:",
                Font = new Font("Segoe UI", 10F),
                Location = new Point(0, 12),
                AutoSize = true
            };

            txtCodigo = new TextBox
            {
                Location = new Point(70, 10),
                Size = new Size(150, 30),
                Font = new Font("Segoe UI", 10F)
            };
            txtCodigo.KeyDown += (s, e) => {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    BuscarPautas();
                }
            };

            btnBuscar = new Button
            {
                Text = "Buscar",
                Location = new Point(230, 8),
                Size = new Size(100, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(63, 81, 181),
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 9.5F),
                Cursor = Cursors.Hand
            };
            btnBuscar.FlatAppearance.BorderSize = 0;
            btnBuscar.Click += (s, e) => BuscarPautas();

            pnlBusqueda.Controls.Add(lblCodigo);
            pnlBusqueda.Controls.Add(txtCodigo);
            pnlBusqueda.Controls.Add(btnBuscar);

            // Label info del comercial
            lblComercialInfo = new Label
            {
                Location = new Point(15, 130),
                Size = new Size(655, 25),
                Font = new Font("Segoe UI Semibold", 10F),
                ForeColor = Color.FromArgb(50, 50, 50),
                Text = ""
            };

            // DataGridView de pautas
            dgvPautas = new DataGridView
            {
                Location = new Point(15, 160),
                Size = new Size(655, 250),
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = true,
                RowHeadersVisible = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            // Configurar columnas
            dgvPautas.Columns.Add("Fecha", "Fecha");
            dgvPautas.Columns.Add("Hora", "Hora");
            dgvPautas.Columns.Add("DiaSemana", "Día");

            dgvPautas.Columns["Fecha"].Width = 120;
            dgvPautas.Columns["Hora"].Width = 80;
            dgvPautas.Columns["DiaSemana"].Width = 100;

            // Estilo del DataGridView
            dgvPautas.EnableHeadersVisualStyles = false;
            dgvPautas.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(63, 81, 181);
            dgvPautas.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvPautas.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9.5F);
            dgvPautas.ColumnHeadersHeight = 35;
            dgvPautas.DefaultCellStyle.Font = new Font("Segoe UI", 9F);
            dgvPautas.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(240, 240, 250);

            // Label total de pautas
            lblTotalPautas = new Label
            {
                Location = new Point(15, 415),
                Size = new Size(300, 25),
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(100, 100, 100),
                Text = ""
            };

            // GroupBox eliminar por fecha
            grpEliminar = new GroupBox
            {
                Text = "Eliminar Pautas por Rango de Fechas",
                Location = new Point(15, 445),
                Size = new Size(655, 100),
                Font = new Font("Segoe UI Semibold", 9.5F)
            };

            Label lblFechaInicio = new Label
            {
                Text = "Fecha Inicio:",
                Location = new Point(15, 35),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F)
            };

            dtpFechaInicio = new DateTimePicker
            {
                Location = new Point(100, 32),
                Size = new Size(130, 25),
                Format = DateTimePickerFormat.Short,
                Font = new Font("Segoe UI", 9F)
            };

            Label lblFechaFinal = new Label
            {
                Text = "Fecha Final:",
                Location = new Point(250, 35),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F)
            };

            dtpFechaFinal = new DateTimePicker
            {
                Location = new Point(335, 32),
                Size = new Size(130, 25),
                Format = DateTimePickerFormat.Short,
                Font = new Font("Segoe UI", 9F)
            };

            btnEliminarPorFecha = new Button
            {
                Text = "Eliminar por Fecha",
                Location = new Point(485, 28),
                Size = new Size(150, 35),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(244, 67, 54),
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 9.5F),
                Cursor = Cursors.Hand
            };
            btnEliminarPorFecha.FlatAppearance.BorderSize = 0;
            btnEliminarPorFecha.Click += BtnEliminarPorFecha_Click;

            // Botón eliminar seleccionados
            Button btnEliminarSeleccionados = new Button
            {
                Text = "Eliminar Seleccionados",
                Location = new Point(15, 68),
                Size = new Size(180, 28),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(255, 152, 0),
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 9F),
                Cursor = Cursors.Hand
            };
            btnEliminarSeleccionados.FlatAppearance.BorderSize = 0;
            btnEliminarSeleccionados.Click += BtnEliminarSeleccionados_Click;

            grpEliminar.Controls.Add(lblFechaInicio);
            grpEliminar.Controls.Add(dtpFechaInicio);
            grpEliminar.Controls.Add(lblFechaFinal);
            grpEliminar.Controls.Add(dtpFechaFinal);
            grpEliminar.Controls.Add(btnEliminarPorFecha);
            grpEliminar.Controls.Add(btnEliminarSeleccionados);

            // GroupBox eliminar por hora
            grpEliminarPorHora = new GroupBox
            {
                Text = "Eliminar Pautas por Hora (todas las fechas)",
                Location = new Point(15, 550),
                Size = new Size(655, 70),
                Font = new Font("Segoe UI Semibold", 9.5F)
            };

            Label lblHora = new Label
            {
                Text = "Hora:",
                Location = new Point(15, 30),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F)
            };

            cboHoras = new ComboBox
            {
                Location = new Point(60, 27),
                Size = new Size(100, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9F)
            };

            Label lblInfoHora = new Label
            {
                Text = "Elimina todas las pautas de esta hora en todas las fechas",
                Location = new Point(175, 30),
                AutoSize = true,
                Font = new Font("Segoe UI", 8F),
                ForeColor = Color.Gray
            };

            btnEliminarPorHora = new Button
            {
                Text = "Eliminar por Hora",
                Location = new Point(485, 23),
                Size = new Size(150, 35),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(156, 39, 176),
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 9.5F),
                Cursor = Cursors.Hand
            };
            btnEliminarPorHora.FlatAppearance.BorderSize = 0;
            btnEliminarPorHora.Click += BtnEliminarPorHora_Click;

            grpEliminarPorHora.Controls.Add(lblHora);
            grpEliminarPorHora.Controls.Add(cboHoras);
            grpEliminarPorHora.Controls.Add(lblInfoHora);
            grpEliminarPorHora.Controls.Add(btnEliminarPorHora);

            // Agregar controles al formulario
            this.Controls.Add(pnlTop);
            this.Controls.Add(pnlBusqueda);
            this.Controls.Add(lblComercialInfo);
            this.Controls.Add(dgvPautas);
            this.Controls.Add(lblTotalPautas);
            this.Controls.Add(grpEliminar);
            this.Controls.Add(grpEliminarPorHora);
        }

        private async void BuscarPautas()
        {
            await BuscarPautasAsync();
        }

        private async Task BuscarPautasAsync()
        {
            string codigo = txtCodigo.Text.Trim();
            if (string.IsNullOrEmpty(codigo))
            {
                MessageBox.Show("Por favor, ingrese un código de comercial.", "Advertencia",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            codigoActual = codigo;
            dgvPautas.Rows.Clear();

            try
            {
                this.Cursor = Cursors.WaitCursor;

                using (var conn = new NpgsqlConnection(DatabaseConfig.ConnectionString))
                {
                    await conn.OpenAsync();

                    // Obtener info del comercial
                    string ciudad = "";
                    string radio = "";
                    string tipoProgramacion = "";
                    DateTime fechaInicioComercial = DateTime.Today;
                    DateTime fechaFinalComercial = DateTime.Today;

                    string queryComercial = @"
                        SELECT FilePath, Ciudad, Radio, FechaInicio, FechaFinal,
                               COALESCE(TipoProgramacion, 'Cada 00-30') as TipoProgramacion
                        FROM Comerciales
                        WHERE Codigo = @Codigo";

                    using (var cmd = new NpgsqlCommand(queryComercial, conn))
                    {
                        cmd.Parameters.AddWithValue("@Codigo", codigo);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                comercialNombre = System.IO.Path.GetFileName(reader["FilePath"].ToString());
                                ciudad = reader["Ciudad"].ToString();
                                radio = reader["Radio"].ToString();
                                fechaInicioComercial = reader.GetDateTime(3);
                                fechaFinalComercial = reader.GetDateTime(4);
                                tipoProgramacion = reader["TipoProgramacion"]?.ToString() ?? "Cada 00-30";

                                // Guardar para uso en eliminación/regeneración
                                ciudadActual = ciudad;
                                radioActual = radio;
                                fechaInicioActual = fechaInicioComercial;
                                fechaFinalActual = fechaFinalComercial;

                                lblComercialInfo.Text = $"{comercialNombre} | {ciudad} - {radio} | Vigencia: {fechaInicioComercial:dd/MM/yyyy} - {fechaFinalComercial:dd/MM/yyyy}";

                                // Establecer fechas en los DateTimePicker
                                dtpFechaInicio.Value = fechaInicioComercial;
                                dtpFechaFinal.Value = fechaFinalComercial;
                            }
                            else
                            {
                                lblComercialInfo.Text = "Comercial no encontrado";
                                lblTotalPautas.Text = "";
                                return;
                            }
                        }
                    }

                    // Obtener pautas asignadas
                    string queryPautas = @"
                        SELECT ca.Fila, ca.Columna
                        FROM ComercialesAsignados ca
                        WHERE ca.Codigo = @Codigo
                        ORDER BY ca.Fila, ca.Columna";

                    var pautas = new List<(DateTime Fecha, string Hora, string DiaSemana, int Fila)>();

                    // Determinar el tipo de tanda:
                    // 1. Primero usar el campo TipoProgramacion de la BD si tiene valor significativo
                    // 2. Si es el valor por defecto, detectar basandose en la radio
                    TipoTanda tipoTandaDetectado;
                    if (string.IsNullOrEmpty(tipoProgramacion) || tipoProgramacion == "Cada 00-30")
                    {
                        // Detectar automaticamente basandose en la radio
                        tipoTandaDetectado = DetectarTipoTandaPorRadio(radio);
                    }
                    else
                    {
                        tipoTandaDetectado = ObtenerTipoTandaDesdeProgramacion(tipoProgramacion);
                    }
                    string[] horarios = TandasHorarias.GetHorarios(tipoTandaDetectado);

                    // Ahora obtener los datos completos
                    using (var cmd = new NpgsqlCommand(queryPautas, conn))
                    {
                        cmd.Parameters.AddWithValue("@Codigo", codigo);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                int fila = reader.GetInt32(0);
                                int columna = reader.GetInt32(1);

                                // Convertir fila a hora usando el array de horarios del tipo de tanda detectado
                                string horaStr = (fila >= 0 && fila < horarios.Length) ? horarios[fila] : $"Fila {fila}";

                                // La columna indica el día (columna 2 = primer día, etc.)
                                // Calcular la fecha real basándose en FechaInicio + (columna - 2)
                                int diasDesdeInicio = columna - 2;
                                DateTime fechaPauta = fechaInicioComercial.AddDays(diasDesdeInicio);

                                // Obtener día de la semana en español
                                string diaSemana = fechaPauta.ToString("dddd", new System.Globalization.CultureInfo("es-ES"));
                                diaSemana = char.ToUpper(diaSemana[0]) + diaSemana.Substring(1);

                                pautas.Add((fechaPauta, horaStr, diaSemana, fila));
                            }
                        }
                    }

                    // Ordenar por fecha y hora
                    var pautasOrdenadas = pautas.OrderBy(p => p.Fecha).ThenBy(p => p.Hora).ToList();

                    // Agregar al DataGridView
                    foreach (var pauta in pautasOrdenadas)
                    {
                        int rowIndex = dgvPautas.Rows.Add(
                            pauta.Fecha.ToString("dd/MM/yyyy"),
                            pauta.Hora,
                            pauta.DiaSemana
                        );
                        // Guardar la fila real en el Tag de la fila para uso posterior
                        dgvPautas.Rows[rowIndex].Tag = pauta.Fila;
                    }

                    // Cargar horas únicas en el ComboBox (con su fila correspondiente)
                    var horasConFila = pautasOrdenadas
                        .Select(p => new { p.Hora, p.Fila })
                        .Distinct()
                        .OrderBy(h => h.Hora)
                        .ToList();

                    horasPauteadas = horasConFila.Select(h => h.Hora).ToList();
                    cboHoras.Items.Clear();
                    cboHoras.Tag = new Dictionary<string, int>(); // Guardar mapeo hora -> fila
                    var mapeoHoraFila = (Dictionary<string, int>)cboHoras.Tag;

                    foreach (var item in horasConFila)
                    {
                        cboHoras.Items.Add(item.Hora);
                        if (!mapeoHoraFila.ContainsKey(item.Hora))
                            mapeoHoraFila[item.Hora] = item.Fila;
                    }
                    if (cboHoras.Items.Count > 0)
                        cboHoras.SelectedIndex = 0;

                    // Mostrar info del tipo de tanda detectado
                    string tipoTandaTexto = ObtenerNombreTipoTanda(tipoTandaDetectado);
                    lblTotalPautas.Text = $"Total: {pautasOrdenadas.Count} pautas ({horasPauteadas.Count} horas) - Tipo: {tipoTandaTexto}";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al buscar pautas: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }

        private async void BtnEliminarPorFecha_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(codigoActual))
            {
                MessageBox.Show("Primero busque un comercial.", "Advertencia",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DateTime fechaInicio = dtpFechaInicio.Value.Date;
            DateTime fechaFinal = dtpFechaFinal.Value.Date;

            if (fechaFinal < fechaInicio)
            {
                MessageBox.Show("La fecha final debe ser mayor o igual a la fecha de inicio.", "Advertencia",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Contar cuántas pautas se eliminarán
            int pautasAEliminar = 0;
            foreach (DataGridViewRow row in dgvPautas.Rows)
            {
                string fechaStr = row.Cells["Fecha"].Value?.ToString();
                if (DateTime.TryParseExact(fechaStr, "dd/MM/yyyy", null,
                    System.Globalization.DateTimeStyles.None, out DateTime fecha))
                {
                    if (fecha >= fechaInicio && fecha <= fechaFinal)
                    {
                        pautasAEliminar++;
                    }
                }
            }

            if (pautasAEliminar == 0)
            {
                MessageBox.Show("No hay pautas en el rango de fechas seleccionado.", "Información",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DialogResult result = MessageBox.Show(
                $"¿Está seguro de eliminar {pautasAEliminar} pautas del comercial {codigoActual}\n" +
                $"en el rango {fechaInicio:dd/MM/yyyy} - {fechaFinal:dd/MM/yyyy}?\n\n" +
                "Esta acción no se puede deshacer.",
                "Confirmar Eliminación",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                await EliminarPautasPorFechaAsync(fechaInicio, fechaFinal);
            }
        }

        private async Task EliminarPautasPorFechaAsync(DateTime fechaInicio, DateTime fechaFinal)
        {
            try
            {
                this.Cursor = Cursors.WaitCursor;
                int eliminadas = 0;

                using (var conn = new NpgsqlConnection(DatabaseConfig.ConnectionString))
                {
                    await conn.OpenAsync();

                    // Eliminar las pautas en el rango de fechas (usando el campo Fecha directamente)
                    string queryDelete = @"
                        DELETE FROM ComercialesAsignados
                        WHERE Codigo = @Codigo
                        AND Fecha >= @FechaInicio::date
                        AND Fecha <= @FechaFinal::date";

                    using (var cmd = new NpgsqlCommand(queryDelete, conn))
                    {
                        cmd.Parameters.AddWithValue("@Codigo", codigoActual);
                        cmd.Parameters.AddWithValue("@FechaInicio", fechaInicio.Date);
                        cmd.Parameters.AddWithValue("@FechaFinal", fechaFinal.Date);
                        eliminadas = await cmd.ExecuteNonQueryAsync();
                    }

                    // Si no eliminó nada con Fecha, intentar con columnas (registros legacy)
                    if (eliminadas == 0)
                    {
                        // Obtener FechaInicio del comercial para calcular las columnas
                        string queryFechaInicioComercial = "SELECT FechaInicio FROM Comerciales WHERE Codigo = @Codigo";
                        using (var cmd = new NpgsqlCommand(queryFechaInicioComercial, conn))
                        {
                            cmd.Parameters.AddWithValue("@Codigo", codigoActual);
                            var result = await cmd.ExecuteScalarAsync();
                            if (result != null && result != DBNull.Value)
                            {
                                DateTime fechaInicioComercial = (DateTime)result;
                                int columnaInicio = (fechaInicio - fechaInicioComercial).Days + 2;
                                int columnaFinal = (fechaFinal - fechaInicioComercial).Days + 2;

                                string queryDeleteLegacy = @"
                                    DELETE FROM ComercialesAsignados
                                    WHERE Codigo = @Codigo
                                    AND Columna >= @ColumnaInicio
                                    AND Columna <= @ColumnaFinal
                                    AND Fecha IS NULL";

                                using (var cmdLegacy = new NpgsqlCommand(queryDeleteLegacy, conn))
                                {
                                    cmdLegacy.Parameters.AddWithValue("@Codigo", codigoActual);
                                    cmdLegacy.Parameters.AddWithValue("@ColumnaInicio", columnaInicio);
                                    cmdLegacy.Parameters.AddWithValue("@ColumnaFinal", columnaFinal);
                                    eliminadas = await cmdLegacy.ExecuteNonQueryAsync();
                                }
                            }
                        }
                    }
                }

                // Regenerar los archivos TXT para las fechas afectadas
                if (eliminadas > 0 && !string.IsNullOrEmpty(ciudadActual) && !string.IsNullOrEmpty(radioActual))
                {
                    await RegenerarArchivosTXTAsync(fechaInicio, fechaFinal);
                }

                MessageBox.Show($"Se eliminaron {eliminadas} pautas y se regeneraron los archivos TXT.", "Éxito",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Recargar la lista
                await BuscarPautasAsync();

                // Notificar cambio en BD
                ConfigManager.NotificarCambioEnBD();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al eliminar pautas: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }

        private async void BtnEliminarSeleccionados_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(codigoActual))
            {
                MessageBox.Show("Primero busque un comercial.", "Advertencia",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (dgvPautas.SelectedRows.Count == 0)
            {
                MessageBox.Show("Seleccione al menos una pauta para eliminar.", "Advertencia",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DialogResult result = MessageBox.Show(
                $"¿Está seguro de eliminar {dgvPautas.SelectedRows.Count} pautas seleccionadas?\n\n" +
                "Esta acción no se puede deshacer.",
                "Confirmar Eliminación",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                await EliminarPautasSeleccionadasAsync();
            }
        }

        private async Task EliminarPautasSeleccionadasAsync()
        {
            try
            {
                this.Cursor = Cursors.WaitCursor;
                int eliminadas = 0;

                using (var conn = new NpgsqlConnection(DatabaseConfig.ConnectionString))
                {
                    await conn.OpenAsync();

                    // Obtener FechaInicio del comercial para calcular las columnas
                    DateTime fechaInicioComercial;
                    string queryFechaInicio = "SELECT FechaInicio FROM Comerciales WHERE Codigo = @Codigo";
                    using (var cmd = new NpgsqlCommand(queryFechaInicio, conn))
                    {
                        cmd.Parameters.AddWithValue("@Codigo", codigoActual);
                        fechaInicioComercial = (DateTime)await cmd.ExecuteScalarAsync();
                    }

                    foreach (DataGridViewRow row in dgvPautas.SelectedRows)
                    {
                        string fechaStr = row.Cells["Fecha"].Value?.ToString();

                        if (DateTime.TryParseExact(fechaStr, "dd/MM/yyyy", null,
                            System.Globalization.DateTimeStyles.None, out DateTime fecha))
                        {
                            // Calcular columna
                            int columna = (fecha - fechaInicioComercial).Days + 2;

                            // Obtener fila desde el Tag de la fila del DataGridView
                            int fila = row.Tag != null ? (int)row.Tag : -1;
                            if (fila < 0) continue;

                            // Eliminar la pauta específica
                            string queryDelete = @"
                                DELETE FROM ComercialesAsignados
                                WHERE Codigo = @Codigo
                                AND Fila = @Fila
                                AND Columna = @Columna";

                            using (var cmd = new NpgsqlCommand(queryDelete, conn))
                            {
                                cmd.Parameters.AddWithValue("@Codigo", codigoActual);
                                cmd.Parameters.AddWithValue("@Fila", fila);
                                cmd.Parameters.AddWithValue("@Columna", columna);
                                eliminadas += await cmd.ExecuteNonQueryAsync();
                            }
                        }
                    }
                }

                MessageBox.Show($"Se eliminaron {eliminadas} pautas correctamente.", "Éxito",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Recargar la lista
                await BuscarPautasAsync();

                // Notificar cambio en BD
                ConfigManager.NotificarCambioEnBD();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al eliminar pautas: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }

        private async void BtnEliminarPorHora_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(codigoActual))
            {
                MessageBox.Show("Primero busque un comercial.", "Advertencia",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (cboHoras.SelectedItem == null)
            {
                MessageBox.Show("Seleccione una hora para eliminar.", "Advertencia",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string horaSeleccionada = cboHoras.SelectedItem.ToString();

            // Contar cuántas pautas se eliminarán
            int pautasAEliminar = 0;
            foreach (DataGridViewRow row in dgvPautas.Rows)
            {
                string horaStr = row.Cells["Hora"].Value?.ToString();
                if (horaStr == horaSeleccionada)
                {
                    pautasAEliminar++;
                }
            }

            if (pautasAEliminar == 0)
            {
                MessageBox.Show("No hay pautas para la hora seleccionada.", "Información",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DialogResult result = MessageBox.Show(
                $"¿Está seguro de eliminar {pautasAEliminar} pautas del comercial {codigoActual}\n" +
                $"a las {horaSeleccionada} en TODAS las fechas?\n\n" +
                "Esta acción no se puede deshacer.",
                "Confirmar Eliminación por Hora",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                await EliminarPautasPorHoraAsync(horaSeleccionada);
            }
        }

        private async Task EliminarPautasPorHoraAsync(string horaStr)
        {
            try
            {
                this.Cursor = Cursors.WaitCursor;

                // Obtener la fila real desde el mapeo guardado en el ComboBox
                int fila = -1;
                if (cboHoras.Tag is Dictionary<string, int> mapeo && mapeo.ContainsKey(horaStr))
                {
                    fila = mapeo[horaStr];
                }
                else
                {
                    MessageBox.Show("No se pudo determinar la fila para esta hora.", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                int eliminadas = 0;

                using (var conn = new NpgsqlConnection(DatabaseConfig.ConnectionString))
                {
                    await conn.OpenAsync();

                    // Eliminar todas las pautas de esa hora (fila) para el comercial
                    string queryDelete = @"
                        DELETE FROM ComercialesAsignados
                        WHERE Codigo = @Codigo
                        AND Fila = @Fila";

                    using (var cmd = new NpgsqlCommand(queryDelete, conn))
                    {
                        cmd.Parameters.AddWithValue("@Codigo", codigoActual);
                        cmd.Parameters.AddWithValue("@Fila", fila);
                        eliminadas = await cmd.ExecuteNonQueryAsync();
                    }
                }

                // Regenerar los archivos TXT para todo el rango de fechas del comercial
                if (eliminadas > 0 && !string.IsNullOrEmpty(ciudadActual) && !string.IsNullOrEmpty(radioActual))
                {
                    await RegenerarArchivosTXTAsync(fechaInicioActual, fechaFinalActual);
                }

                MessageBox.Show($"Se eliminaron {eliminadas} pautas de las {horaStr} y se regeneraron los archivos TXT.", "Éxito",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Recargar la lista
                await BuscarPautasAsync();

                // Notificar cambio en BD
                ConfigManager.NotificarCambioEnBD();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al eliminar pautas por hora: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }

        /// <summary>
        /// Convierte el texto de TipoProgramacion de la BD al enum TipoTanda
        /// </summary>
        private TipoTanda ObtenerTipoTandaDesdeProgramacion(string tipoProgramacion)
        {
            if (string.IsNullOrEmpty(tipoProgramacion))
                return TipoTanda.Tandas_00_30;

            // Usar el método existente de TandasHorarias
            return TandasHorarias.GetTipoTandaFromString(tipoProgramacion);
        }

        /// <summary>
        /// Detecta el tipo de tanda basandose en el nombre de la radio
        /// </summary>
        private TipoTanda DetectarTipoTandaPorRadio(string radio)
        {
            if (string.IsNullOrEmpty(radio))
                return TipoTanda.Tandas_00_30;

            string radioUpper = radio.ToUpper();

            // KARIBEÑA usa las 4 tandas: 00, 20, 30, 50 (96 tandas)
            // Incluir variantes de codificación: KARIBEÑA, KARIBENA, KARIBEÃA (UTF-8 mal interpretado)
            if (radioUpper.Contains("KARIBEÑA") || radioUpper.Contains("KARIBENA") ||
                radioUpper.Contains("KARIBEÃ") || radioUpper.Contains("KARIBE") || radioUpper.Contains("KARIBEAN"))
            {
                return TipoTanda.Tandas_00_20_30_50;
            }
            // LAKALLE usa las 4 tandas: 00, 20, 30, 50 (96 tandas)
            else if (radioUpper.Contains("LAKALLE") || radioUpper.Contains("LA KALLE") || radioUpper.Contains("KALLE"))
            {
                return TipoTanda.Tandas_00_20_30_50;
            }
            // EXITOSA usa tandas 00-30 (por defecto)
            else
            {
                return TipoTanda.Tandas_00_30;
            }
        }

        /// <summary>
        /// Obtiene el nombre legible del tipo de tanda
        /// </summary>
        private string ObtenerNombreTipoTanda(TipoTanda tipo)
        {
            switch (tipo)
            {
                case TipoTanda.Tandas_00_30:
                    return "00-30";
                case TipoTanda.Tandas_20_50:
                    return "20-50";
                case TipoTanda.Tandas_10_40:
                    return "10-40";
                case TipoTanda.Tandas_15_45:
                    return "15-45";
                case TipoTanda.Tandas_00_20_30_50:
                    return "00-20-30-50";
                default:
                    return "Desconocido";
            }
        }

        /// <summary>
        /// Regenera los archivos TXT para un rango de fechas después de eliminar pautas
        /// </summary>
        private async Task RegenerarArchivosTXTAsync(DateTime fechaInicio, DateTime fechaFinal)
        {
            try
            {
                // Obtener la ruta base de pautas
                string rutaBasePautas = ConfigManager.ObtenerRutaBasePautas();
                string directorioPautas = System.IO.Path.Combine(rutaBasePautas, ciudadActual.ToUpper(), radioActual.ToUpper());

                if (!System.IO.Directory.Exists(directorioPautas))
                {
                    System.IO.Directory.CreateDirectory(directorioPautas);
                }

                // Usar el generador de pautas existente
                var generador = new GenerarPauta();

                // Regenerar archivo por cada fecha en el rango
                for (DateTime fecha = fechaInicio.Date; fecha <= fechaFinal.Date; fecha = fecha.AddDays(1))
                {
                    await generador.RegenerarArchivoPorFechaAsync(fecha, ciudadActual, radioActual);
                }

            }
            catch (Exception ex)
            {
                // No mostrar error al usuario - la eliminación ya fue exitosa
                System.Diagnostics.Debug.WriteLine($"[REGENERAR_TXT] Error: {ex.Message}");
            }
        }

    }
}

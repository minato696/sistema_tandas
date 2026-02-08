using System;
using System.Collections.Generic;
using Npgsql;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Generador_Pautas
{
    public class ReportesForm : Form
    {
        private TabControl tabControl;
        private TabPage tabComerciales;
        private TabPage tabResumen;
        private TabPage tabHistorial;
        private TabPage tabPautaMensual;
        private TabPage tabHorarios;

        // Tab Comerciales por Fecha
        private DateTimePicker dtpComFechaInicio;
        private DateTimePicker dtpComFechaFin;
        private ComboBox cboComCiudad;
        private ComboBox cboComRadio;
        private Button btnGenerarComerciales;

        // Tab Resumen
        private DateTimePicker dtpResumenFecha;
        private RadioButton rbDiario;
        private RadioButton rbSemanal;
        private Button btnGenerarResumen;

        // Tab Historial
        private DateTimePicker dtpHistFechaInicio;
        private DateTimePicker dtpHistFechaFin;
        private ComboBox cboHistCiudad;
        private ComboBox cboHistRadio;
        private Button btnGenerarHistorial;

        // Tab Pauta Mensual
        private RadioButton rbPautaRango;
        private RadioButton rbPautaMes;
        private DateTimePicker dtpPautaFechaInicio;
        private DateTimePicker dtpPautaFechaFin;
        private ComboBox cboPautaMes;
        private NumericUpDown nudPautaAnio;
        private ComboBox cboPautaCiudad;
        private ComboBox cboPautaRadio;
        private Button btnGenerarPautaMensual;

        // Tab Horarios Transmision
        private RadioButton rbHorariosRango;
        private RadioButton rbHorariosMes;
        private DateTimePicker dtpHorariosFechaInicio;
        private DateTimePicker dtpHorariosFechaFin;
        private ComboBox cboHorariosMes;
        private NumericUpDown nudHorariosAnio;
        private ComboBox cboHorariosCiudad;
        private ComboBox cboHorariosRadio;
        private Button btnGenerarHorarios;

        private Label lblEstado;
        private ProgressBar progressBar;

        private ReportesService reportesService;

        public ReportesForm()
        {
            reportesService = new ReportesService();
            InitializeComponent();
            AppIcon.ApplyTo(this);
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            this.Text = "Generador de Reportes";
            this.Size = new Size(650, 500);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(240, 240, 245);

            // Titulo
            Label lblTitulo = new Label
            {
                Text = "Generador de Reportes",
                Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 120, 215),
                AutoSize = true,
                Location = new Point(20, 15)
            };

            // TabControl
            tabControl = new TabControl
            {
                Location = new Point(20, 50),
                Size = new Size(590, 350),
                Font = new Font("Segoe UI", 9F)
            };

            // Tab 1: Comerciales por Fecha
            tabComerciales = new TabPage("Comerciales");
            CrearTabComerciales();
            tabControl.TabPages.Add(tabComerciales);

            // Tab 2: Resumen
            tabResumen = new TabPage("Resumen");
            CrearTabResumen();
            tabControl.TabPages.Add(tabResumen);

            // Tab 3: Historial
            tabHistorial = new TabPage("Historial");
            CrearTabHistorial();
            tabControl.TabPages.Add(tabHistorial);

            // Tab 4: Pauta Mensual
            tabPautaMensual = new TabPage("Pauta Mensual");
            CrearTabPautaMensual();
            tabControl.TabPages.Add(tabPautaMensual);

            // Tab 5: Horarios Transmision
            tabHorarios = new TabPage("Horarios");
            CrearTabHorarios();
            tabControl.TabPages.Add(tabHorarios);

            // Barra de estado
            progressBar = new ProgressBar
            {
                Location = new Point(20, 410),
                Size = new Size(590, 10),
                Style = ProgressBarStyle.Marquee,
                Visible = false
            };

            lblEstado = new Label
            {
                Location = new Point(20, 425),
                Size = new Size(590, 25),
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.Gray,
                Text = "Seleccione un tipo de reporte y configure los filtros."
            };

            this.Controls.Add(lblTitulo);
            this.Controls.Add(tabControl);
            this.Controls.Add(progressBar);
            this.Controls.Add(lblEstado);

            this.Load += ReportesForm_Load;

            this.ResumeLayout(false);
        }

        private void CrearTabComerciales()
        {
            tabComerciales.BackColor = Color.White;
            tabComerciales.Padding = new Padding(15);

            Label lblDesc = new Label
            {
                Text = "Genera un listado de comerciales filtrado por rango de fechas,\nciudad y/o radio. Incluye estado y alertas de vencimiento.",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.Gray,
                AutoSize = true,
                Location = new Point(15, 15)
            };

            Label lblFechaInicio = new Label
            {
                Text = "Fecha Inicio:",
                Font = new Font("Segoe UI", 10F),
                AutoSize = true,
                Location = new Point(15, 60)
            };

            dtpComFechaInicio = new DateTimePicker
            {
                Font = new Font("Segoe UI", 10F),
                Format = DateTimePickerFormat.Short,
                Location = new Point(120, 57),
                Size = new Size(130, 25),
                Value = DateTime.Today.AddDays(-7)
            };

            Label lblFechaFin = new Label
            {
                Text = "Fecha Fin:",
                Font = new Font("Segoe UI", 10F),
                AutoSize = true,
                Location = new Point(270, 60)
            };

            dtpComFechaFin = new DateTimePicker
            {
                Font = new Font("Segoe UI", 10F),
                Format = DateTimePickerFormat.Short,
                Location = new Point(350, 57),
                Size = new Size(130, 25),
                Value = DateTime.Today
            };

            Label lblCiudad = new Label
            {
                Text = "Ciudad:",
                Font = new Font("Segoe UI", 10F),
                AutoSize = true,
                Location = new Point(15, 100)
            };

            cboComCiudad = new ComboBox
            {
                Font = new Font("Segoe UI", 10F),
                Location = new Point(120, 97),
                Size = new Size(150, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            Label lblRadio = new Label
            {
                Text = "Radio:",
                Font = new Font("Segoe UI", 10F),
                AutoSize = true,
                Location = new Point(290, 100)
            };

            cboComRadio = new ComboBox
            {
                Font = new Font("Segoe UI", 10F),
                Location = new Point(350, 97),
                Size = new Size(130, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            btnGenerarComerciales = new Button
            {
                Text = "Generar Reporte Excel",
                Font = new Font("Segoe UI Semibold", 11F),
                Size = new Size(200, 40),
                Location = new Point(145, 180),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnGenerarComerciales.FlatAppearance.BorderSize = 0;
            btnGenerarComerciales.Click += BtnGenerarComerciales_Click;

            tabComerciales.Controls.AddRange(new Control[] {
                lblDesc, lblFechaInicio, dtpComFechaInicio, lblFechaFin, dtpComFechaFin,
                lblCiudad, cboComCiudad, lblRadio, cboComRadio, btnGenerarComerciales
            });
        }

        private void CrearTabResumen()
        {
            tabResumen.BackColor = Color.White;
            tabResumen.Padding = new Padding(15);

            Label lblDesc = new Label
            {
                Text = "Genera un resumen estadistico con totales por ciudad y radio.\nIncluye comerciales activos, por vencer y vencidos.",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.Gray,
                AutoSize = true,
                Location = new Point(15, 15)
            };

            Label lblFecha = new Label
            {
                Text = "Fecha Base:",
                Font = new Font("Segoe UI", 10F),
                AutoSize = true,
                Location = new Point(15, 60)
            };

            dtpResumenFecha = new DateTimePicker
            {
                Font = new Font("Segoe UI", 10F),
                Format = DateTimePickerFormat.Short,
                Location = new Point(120, 57),
                Size = new Size(130, 25),
                Value = DateTime.Today
            };

            rbDiario = new RadioButton
            {
                Text = "Reporte Diario",
                Font = new Font("Segoe UI", 10F),
                AutoSize = true,
                Location = new Point(120, 100),
                Checked = true
            };

            rbSemanal = new RadioButton
            {
                Text = "Reporte Semanal",
                Font = new Font("Segoe UI", 10F),
                AutoSize = true,
                Location = new Point(270, 100)
            };

            btnGenerarResumen = new Button
            {
                Text = "Generar Resumen Excel",
                Font = new Font("Segoe UI Semibold", 11F),
                Size = new Size(200, 40),
                Location = new Point(145, 180),
                BackColor = Color.FromArgb(0, 150, 136),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnGenerarResumen.FlatAppearance.BorderSize = 0;
            btnGenerarResumen.Click += BtnGenerarResumen_Click;

            tabResumen.Controls.AddRange(new Control[] {
                lblDesc, lblFecha, dtpResumenFecha, rbDiario, rbSemanal, btnGenerarResumen
            });
        }

        private void CrearTabHistorial()
        {
            tabHistorial.BackColor = Color.White;
            tabHistorial.Padding = new Padding(15);

            Label lblDesc = new Label
            {
                Text = "Genera un historial detallado de todas las pautas programadas\ncon horarios, comerciales y posiciones.",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.Gray,
                AutoSize = true,
                Location = new Point(15, 15)
            };

            Label lblFechaInicio = new Label
            {
                Text = "Fecha Inicio:",
                Font = new Font("Segoe UI", 10F),
                AutoSize = true,
                Location = new Point(15, 60)
            };

            dtpHistFechaInicio = new DateTimePicker
            {
                Font = new Font("Segoe UI", 10F),
                Format = DateTimePickerFormat.Short,
                Location = new Point(120, 57),
                Size = new Size(130, 25),
                Value = DateTime.Today.AddDays(-7)
            };

            Label lblFechaFin = new Label
            {
                Text = "Fecha Fin:",
                Font = new Font("Segoe UI", 10F),
                AutoSize = true,
                Location = new Point(270, 60)
            };

            dtpHistFechaFin = new DateTimePicker
            {
                Font = new Font("Segoe UI", 10F),
                Format = DateTimePickerFormat.Short,
                Location = new Point(350, 57),
                Size = new Size(130, 25),
                Value = DateTime.Today
            };

            Label lblCiudad = new Label
            {
                Text = "Ciudad:",
                Font = new Font("Segoe UI", 10F),
                AutoSize = true,
                Location = new Point(15, 100)
            };

            cboHistCiudad = new ComboBox
            {
                Font = new Font("Segoe UI", 10F),
                Location = new Point(120, 97),
                Size = new Size(150, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            Label lblRadio = new Label
            {
                Text = "Radio:",
                Font = new Font("Segoe UI", 10F),
                AutoSize = true,
                Location = new Point(290, 100)
            };

            cboHistRadio = new ComboBox
            {
                Font = new Font("Segoe UI", 10F),
                Location = new Point(350, 97),
                Size = new Size(130, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            btnGenerarHistorial = new Button
            {
                Text = "Generar Historial Excel",
                Font = new Font("Segoe UI Semibold", 11F),
                Size = new Size(200, 40),
                Location = new Point(145, 180),
                BackColor = Color.FromArgb(100, 100, 100),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnGenerarHistorial.FlatAppearance.BorderSize = 0;
            btnGenerarHistorial.Click += BtnGenerarHistorial_Click;

            tabHistorial.Controls.AddRange(new Control[] {
                lblDesc, lblFechaInicio, dtpHistFechaInicio, lblFechaFin, dtpHistFechaFin,
                lblCiudad, cboHistCiudad, lblRadio, cboHistRadio, btnGenerarHistorial
            });
        }

        private void CrearTabPautaMensual()
        {
            tabPautaMensual.BackColor = Color.White;
            tabPautaMensual.Padding = new Padding(15);

            Label lblDesc = new Label
            {
                Text = "Genera un reporte tipo grilla con tandas por dia para cada comercial.\nFilas = comerciales, Columnas = dias del mes/rango.",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.Gray,
                AutoSize = true,
                Location = new Point(15, 10)
            };

            // Radio buttons: Rango o Mes
            rbPautaRango = new RadioButton
            {
                Text = "Por Rango de Fechas",
                Font = new Font("Segoe UI", 9F),
                AutoSize = true,
                Location = new Point(15, 50),
                Checked = true
            };
            rbPautaRango.CheckedChanged += (s, e) => ActualizarVisibilidadPauta();

            rbPautaMes = new RadioButton
            {
                Text = "Mes Completo",
                Font = new Font("Segoe UI", 9F),
                AutoSize = true,
                Location = new Point(200, 50)
            };

            // Controles de rango
            Label lblPautaInicio = new Label { Text = "Inicio:", Font = new Font("Segoe UI", 9F), AutoSize = true, Location = new Point(15, 80) };
            dtpPautaFechaInicio = new DateTimePicker { Font = new Font("Segoe UI", 9F), Format = DateTimePickerFormat.Short, Location = new Point(70, 77), Size = new Size(120, 25), Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1) };
            Label lblPautaFin = new Label { Text = "Fin:", Font = new Font("Segoe UI", 9F), AutoSize = true, Location = new Point(200, 80) };
            dtpPautaFechaFin = new DateTimePicker { Font = new Font("Segoe UI", 9F), Format = DateTimePickerFormat.Short, Location = new Point(235, 77), Size = new Size(120, 25), Value = DateTime.Today };

            // Controles de mes
            Label lblPautaMes = new Label { Text = "Mes:", Font = new Font("Segoe UI", 9F), AutoSize = true, Location = new Point(15, 80), Visible = false, Name = "lblPautaMes" };
            string[] meses = { "Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio", "Julio", "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre" };
            cboPautaMes = new ComboBox { Font = new Font("Segoe UI", 9F), Location = new Point(70, 77), Size = new Size(120, 25), DropDownStyle = ComboBoxStyle.DropDownList, Visible = false };
            cboPautaMes.Items.AddRange(meses);
            cboPautaMes.SelectedIndex = DateTime.Today.Month - 1;
            Label lblPautaAnio = new Label { Text = "Anio:", Font = new Font("Segoe UI", 9F), AutoSize = true, Location = new Point(200, 80), Visible = false, Name = "lblPautaAnio" };
            nudPautaAnio = new NumericUpDown { Font = new Font("Segoe UI", 9F), Location = new Point(245, 77), Size = new Size(80, 25), Minimum = 2020, Maximum = 2035, Value = DateTime.Today.Year, Visible = false };

            // Ciudad y Radio (obligatorios)
            Label lblCiudad = new Label { Text = "Ciudad:", Font = new Font("Segoe UI", 9F), AutoSize = true, Location = new Point(15, 115) };
            cboPautaCiudad = new ComboBox { Font = new Font("Segoe UI", 9F), Location = new Point(70, 112), Size = new Size(150, 25), DropDownStyle = ComboBoxStyle.DropDownList };
            Label lblRadio = new Label { Text = "Radio:", Font = new Font("Segoe UI", 9F), AutoSize = true, Location = new Point(235, 115) };
            cboPautaRadio = new ComboBox { Font = new Font("Segoe UI", 9F), Location = new Point(285, 112), Size = new Size(130, 25), DropDownStyle = ComboBoxStyle.DropDownList };

            btnGenerarPautaMensual = new Button
            {
                Text = "Generar Reporte Pauta",
                Font = new Font("Segoe UI Semibold", 11F),
                Size = new Size(220, 40),
                Location = new Point(140, 160),
                BackColor = Color.FromArgb(255, 152, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnGenerarPautaMensual.FlatAppearance.BorderSize = 0;
            btnGenerarPautaMensual.Click += BtnGenerarPautaMensual_Click;

            tabPautaMensual.Controls.AddRange(new Control[] {
                lblDesc, rbPautaRango, rbPautaMes,
                lblPautaInicio, dtpPautaFechaInicio, lblPautaFin, dtpPautaFechaFin,
                lblPautaMes, cboPautaMes, lblPautaAnio, nudPautaAnio,
                lblCiudad, cboPautaCiudad, lblRadio, cboPautaRadio,
                btnGenerarPautaMensual
            });
        }

        private void CrearTabHorarios()
        {
            tabHorarios.BackColor = Color.White;
            tabHorarios.Padding = new Padding(15);

            Label lblDesc = new Label
            {
                Text = "Genera un listado detallado de horarios de transmision.\nFECHA | TANDA | MOTIVO para cada comercial programado.",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.Gray,
                AutoSize = true,
                Location = new Point(15, 10)
            };

            // Radio buttons: Rango o Mes
            rbHorariosRango = new RadioButton
            {
                Text = "Por Rango de Fechas",
                Font = new Font("Segoe UI", 9F),
                AutoSize = true,
                Location = new Point(15, 50),
                Checked = true
            };
            rbHorariosRango.CheckedChanged += (s, e) => ActualizarVisibilidadHorarios();

            rbHorariosMes = new RadioButton
            {
                Text = "Mes Completo",
                Font = new Font("Segoe UI", 9F),
                AutoSize = true,
                Location = new Point(200, 50)
            };

            // Controles de rango
            Label lblHorariosInicio = new Label { Text = "Inicio:", Font = new Font("Segoe UI", 9F), AutoSize = true, Location = new Point(15, 80) };
            dtpHorariosFechaInicio = new DateTimePicker { Font = new Font("Segoe UI", 9F), Format = DateTimePickerFormat.Short, Location = new Point(70, 77), Size = new Size(120, 25), Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1) };
            Label lblHorariosFin = new Label { Text = "Fin:", Font = new Font("Segoe UI", 9F), AutoSize = true, Location = new Point(200, 80) };
            dtpHorariosFechaFin = new DateTimePicker { Font = new Font("Segoe UI", 9F), Format = DateTimePickerFormat.Short, Location = new Point(235, 77), Size = new Size(120, 25), Value = DateTime.Today };

            // Controles de mes
            Label lblHorariosMes = new Label { Text = "Mes:", Font = new Font("Segoe UI", 9F), AutoSize = true, Location = new Point(15, 80), Visible = false, Name = "lblHorariosMes" };
            string[] meses = { "Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio", "Julio", "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre" };
            cboHorariosMes = new ComboBox { Font = new Font("Segoe UI", 9F), Location = new Point(70, 77), Size = new Size(120, 25), DropDownStyle = ComboBoxStyle.DropDownList, Visible = false };
            cboHorariosMes.Items.AddRange(meses);
            cboHorariosMes.SelectedIndex = DateTime.Today.Month - 1;
            Label lblHorariosAnio = new Label { Text = "Anio:", Font = new Font("Segoe UI", 9F), AutoSize = true, Location = new Point(200, 80), Visible = false, Name = "lblHorariosAnio" };
            nudHorariosAnio = new NumericUpDown { Font = new Font("Segoe UI", 9F), Location = new Point(245, 77), Size = new Size(80, 25), Minimum = 2020, Maximum = 2035, Value = DateTime.Today.Year, Visible = false };

            // Ciudad y Radio (obligatorios)
            Label lblCiudad = new Label { Text = "Ciudad:", Font = new Font("Segoe UI", 9F), AutoSize = true, Location = new Point(15, 115) };
            cboHorariosCiudad = new ComboBox { Font = new Font("Segoe UI", 9F), Location = new Point(70, 112), Size = new Size(150, 25), DropDownStyle = ComboBoxStyle.DropDownList };
            Label lblRadio = new Label { Text = "Radio:", Font = new Font("Segoe UI", 9F), AutoSize = true, Location = new Point(235, 115) };
            cboHorariosRadio = new ComboBox { Font = new Font("Segoe UI", 9F), Location = new Point(285, 112), Size = new Size(130, 25), DropDownStyle = ComboBoxStyle.DropDownList };

            btnGenerarHorarios = new Button
            {
                Text = "Generar Horarios",
                Font = new Font("Segoe UI Semibold", 11F),
                Size = new Size(220, 40),
                Location = new Point(140, 160),
                BackColor = Color.FromArgb(156, 39, 176),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnGenerarHorarios.FlatAppearance.BorderSize = 0;
            btnGenerarHorarios.Click += BtnGenerarHorarios_Click;

            tabHorarios.Controls.AddRange(new Control[] {
                lblDesc, rbHorariosRango, rbHorariosMes,
                lblHorariosInicio, dtpHorariosFechaInicio, lblHorariosFin, dtpHorariosFechaFin,
                lblHorariosMes, cboHorariosMes, lblHorariosAnio, nudHorariosAnio,
                lblCiudad, cboHorariosCiudad, lblRadio, cboHorariosRadio,
                btnGenerarHorarios
            });
        }

        private void ActualizarVisibilidadPauta()
        {
            bool esRango = rbPautaRango.Checked;
            dtpPautaFechaInicio.Visible = esRango;
            dtpPautaFechaFin.Visible = esRango;
            // Show/hide labels for rango
            foreach (Control c in tabPautaMensual.Controls)
            {
                if (c is Label lbl && (lbl.Text == "Inicio:" || lbl.Text == "Fin:"))
                    lbl.Visible = esRango;
            }
            cboPautaMes.Visible = !esRango;
            nudPautaAnio.Visible = !esRango;
            foreach (Control c in tabPautaMensual.Controls)
            {
                if (c.Name == "lblPautaMes" || c.Name == "lblPautaAnio")
                    c.Visible = !esRango;
            }
        }

        private void ActualizarVisibilidadHorarios()
        {
            bool esRango = rbHorariosRango.Checked;
            dtpHorariosFechaInicio.Visible = esRango;
            dtpHorariosFechaFin.Visible = esRango;
            foreach (Control c in tabHorarios.Controls)
            {
                if (c is Label lbl && (lbl.Text == "Inicio:" || lbl.Text == "Fin:"))
                    lbl.Visible = esRango;
            }
            cboHorariosMes.Visible = !esRango;
            nudHorariosAnio.Visible = !esRango;
            foreach (Control c in tabHorarios.Controls)
            {
                if (c.Name == "lblHorariosMes" || c.Name == "lblHorariosAnio")
                    c.Visible = !esRango;
            }
        }

        private void ObtenerFechasPauta(out DateTime fechaInicio, out DateTime fechaFin)
        {
            if (rbPautaRango.Checked)
            {
                fechaInicio = dtpPautaFechaInicio.Value.Date;
                fechaFin = dtpPautaFechaFin.Value.Date;
            }
            else
            {
                int mes = cboPautaMes.SelectedIndex + 1;
                int anio = (int)nudPautaAnio.Value;
                fechaInicio = new DateTime(anio, mes, 1);
                fechaFin = new DateTime(anio, mes, DateTime.DaysInMonth(anio, mes));
            }
        }

        private void ObtenerFechasHorarios(out DateTime fechaInicio, out DateTime fechaFin)
        {
            if (rbHorariosRango.Checked)
            {
                fechaInicio = dtpHorariosFechaInicio.Value.Date;
                fechaFin = dtpHorariosFechaFin.Value.Date;
            }
            else
            {
                int mes = cboHorariosMes.SelectedIndex + 1;
                int anio = (int)nudHorariosAnio.Value;
                fechaInicio = new DateTime(anio, mes, 1);
                fechaFin = new DateTime(anio, mes, DateTime.DaysInMonth(anio, mes));
            }
        }

        private async void ReportesForm_Load(object sender, EventArgs e)
        {
            await CargarCombosAsync();
        }

        private async Task CargarCombosAsync()
        {
            try
            {
                // Cargar ciudades
                var ciudades = await AdminCiudadesForm.ObtenerCiudadesActivasAsync();
                ciudades.Insert(0, "(Todas)");

                cboComCiudad.Items.Clear();
                cboComCiudad.Items.AddRange(ciudades.ToArray());
                cboComCiudad.SelectedIndex = 0;

                cboHistCiudad.Items.Clear();
                cboHistCiudad.Items.AddRange(ciudades.ToArray());
                cboHistCiudad.SelectedIndex = 0;

                // Cargar ciudades para tabs nuevas (sin "(Todas)" - obligatorio)
                var ciudadesSinTodas = await AdminCiudadesForm.ObtenerCiudadesActivasAsync();

                cboPautaCiudad.Items.Clear();
                cboPautaCiudad.Items.AddRange(ciudadesSinTodas.ToArray());
                if (cboPautaCiudad.Items.Count > 0) cboPautaCiudad.SelectedIndex = 0;

                cboHorariosCiudad.Items.Clear();
                cboHorariosCiudad.Items.AddRange(ciudadesSinTodas.ToArray());
                if (cboHorariosCiudad.Items.Count > 0) cboHorariosCiudad.SelectedIndex = 0;

                // Cargar radios
                var radios = await ObtenerRadiosAsync();
                radios.Insert(0, "(Todas)");

                cboComRadio.Items.Clear();
                cboComRadio.Items.AddRange(radios.ToArray());
                cboComRadio.SelectedIndex = 0;

                cboHistRadio.Items.Clear();
                cboHistRadio.Items.AddRange(radios.ToArray());
                cboHistRadio.SelectedIndex = 0;

                // Cargar radios para tabs nuevas (sin "(Todas)" - obligatorio)
                var radiosSinTodas = await ObtenerRadiosAsync();

                cboPautaRadio.Items.Clear();
                cboPautaRadio.Items.AddRange(radiosSinTodas.ToArray());
                if (cboPautaRadio.Items.Count > 0) cboPautaRadio.SelectedIndex = 0;

                cboHorariosRadio.Items.Clear();
                cboHorariosRadio.Items.AddRange(radiosSinTodas.ToArray());
                if (cboHorariosRadio.Items.Count > 0) cboHorariosRadio.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error cargando combos: " + ex.Message);
            }
        }

        private async Task<List<string>> ObtenerRadiosAsync()
        {
            var radios = new List<string>();

            try
            {
                using (var conn = new NpgsqlConnection(PostgreSQLMigration.ConnectionString))
                {
                    await conn.OpenAsync();
                    using (var cmd = new NpgsqlCommand("SELECT DISTINCT Radio FROM Comerciales WHERE Radio IS NOT NULL ORDER BY Radio", conn))
                    using (var reader = (NpgsqlDataReader)await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            radios.Add(reader[0].ToString());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ObtenerRadiosDisponibles] Error: {ex.Message}");
            }

            if (radios.Count == 0)
            {
                radios.AddRange(new[] { "EXITOSA", "KARIBENA", "LAKALLE" });
            }

            return radios;
        }

        private async void BtnGenerarComerciales_Click(object sender, EventArgs e)
        {
            try
            {
                SetLoading(true, "Generando reporte de comerciales...");

                string ciudad = cboComCiudad.SelectedItem?.ToString();
                string radio = cboComRadio.SelectedItem?.ToString();

                if (ciudad == "(Todas)") ciudad = null;
                if (radio == "(Todas)") radio = null;

                string archivo = await reportesService.GenerarReporteComercialesPorFechaAsync(
                    dtpComFechaInicio.Value,
                    dtpComFechaFin.Value,
                    ciudad,
                    radio);

                SetLoading(false, "Reporte generado: " + archivo);

                var result = MessageBox.Show(
                    "Reporte generado exitosamente.\n\n" + archivo + "\n\n¿Desea abrir el archivo?",
                    "Reporte Generado",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information);

                if (result == DialogResult.Yes)
                {
                    Process.Start(archivo);
                }
            }
            catch (Exception ex)
            {
                SetLoading(false, "Error al generar reporte");
                MessageBox.Show("Error al generar el reporte:\n" + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void BtnGenerarResumen_Click(object sender, EventArgs e)
        {
            try
            {
                SetLoading(true, "Generando resumen...");

                string archivo = await reportesService.GenerarReporteResumenAsync(
                    dtpResumenFecha.Value,
                    rbSemanal.Checked);

                SetLoading(false, "Resumen generado: " + archivo);

                var result = MessageBox.Show(
                    "Resumen generado exitosamente.\n\n" + archivo + "\n\n¿Desea abrir el archivo?",
                    "Resumen Generado",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information);

                if (result == DialogResult.Yes)
                {
                    Process.Start(archivo);
                }
            }
            catch (Exception ex)
            {
                SetLoading(false, "Error al generar resumen");
                MessageBox.Show("Error al generar el resumen:\n" + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void BtnGenerarHistorial_Click(object sender, EventArgs e)
        {
            try
            {
                SetLoading(true, "Generando historial de pautas...");

                string ciudad = cboHistCiudad.SelectedItem?.ToString();
                string radio = cboHistRadio.SelectedItem?.ToString();

                if (ciudad == "(Todas)") ciudad = null;
                if (radio == "(Todas)") radio = null;

                string archivo = await reportesService.GenerarReporteHistorialPautasAsync(
                    dtpHistFechaInicio.Value,
                    dtpHistFechaFin.Value,
                    ciudad,
                    radio);

                SetLoading(false, "Historial generado: " + archivo);

                var result = MessageBox.Show(
                    "Historial generado exitosamente.\n\n" + archivo + "\n\n¿Desea abrir el archivo?",
                    "Historial Generado",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information);

                if (result == DialogResult.Yes)
                {
                    Process.Start(archivo);
                }
            }
            catch (Exception ex)
            {
                SetLoading(false, "Error al generar historial");
                MessageBox.Show("Error al generar el historial:\n" + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void BtnGenerarPautaMensual_Click(object sender, EventArgs e)
        {
            try
            {
                string ciudad = cboPautaCiudad.SelectedItem?.ToString();
                string radio = cboPautaRadio.SelectedItem?.ToString();

                if (string.IsNullOrEmpty(ciudad))
                {
                    MessageBox.Show("Debe seleccionar una ciudad.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (string.IsNullOrEmpty(radio))
                {
                    MessageBox.Show("Debe seleccionar una radio.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                ObtenerFechasPauta(out DateTime fechaInicio, out DateTime fechaFin);

                SetLoading(true, "Generando reporte de pauta mensual...");

                string archivo = await reportesService.GenerarReportePautaMensualAsync(
                    fechaInicio, fechaFin, ciudad, radio);

                SetLoading(false, "Reporte generado: " + archivo);

                var result = MessageBox.Show(
                    "Reporte de pauta generado exitosamente.\n\n" + archivo + "\n\n¿Desea abrir el archivo?",
                    "Reporte Generado",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information);

                if (result == DialogResult.Yes)
                {
                    Process.Start(archivo);
                }
            }
            catch (Exception ex)
            {
                SetLoading(false, "Error al generar reporte de pauta");
                MessageBox.Show("Error al generar el reporte:\n" + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void BtnGenerarHorarios_Click(object sender, EventArgs e)
        {
            try
            {
                string ciudad = cboHorariosCiudad.SelectedItem?.ToString();
                string radio = cboHorariosRadio.SelectedItem?.ToString();

                if (string.IsNullOrEmpty(ciudad))
                {
                    MessageBox.Show("Debe seleccionar una ciudad.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (string.IsNullOrEmpty(radio))
                {
                    MessageBox.Show("Debe seleccionar una radio.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                ObtenerFechasHorarios(out DateTime fechaInicio, out DateTime fechaFin);

                SetLoading(true, "Generando horarios de transmision...");

                string archivo = await reportesService.GenerarReporteHorariosTransmisionAsync(
                    fechaInicio, fechaFin, ciudad, radio);

                SetLoading(false, "Horarios generados: " + archivo);

                var result = MessageBox.Show(
                    "Horarios de transmision generados exitosamente.\n\n" + archivo + "\n\n¿Desea abrir el archivo?",
                    "Reporte Generado",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information);

                if (result == DialogResult.Yes)
                {
                    Process.Start(archivo);
                }
            }
            catch (Exception ex)
            {
                SetLoading(false, "Error al generar horarios");
                MessageBox.Show("Error al generar los horarios:\n" + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SetLoading(bool loading, string mensaje)
        {
            progressBar.Visible = loading;
            lblEstado.Text = mensaje;

            btnGenerarComerciales.Enabled = !loading;
            btnGenerarResumen.Enabled = !loading;
            btnGenerarHistorial.Enabled = !loading;
            btnGenerarPautaMensual.Enabled = !loading;
            btnGenerarHorarios.Enabled = !loading;

            Application.DoEvents();
        }
    }
}

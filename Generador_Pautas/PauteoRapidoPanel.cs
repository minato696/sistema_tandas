using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Generador_Pautas
{
    /// <summary>
    /// Panel de Pauteo Rápido para agregar comerciales sin abrir formularios adicionales.
    /// Permite configurar fechas, posición, tandas y días directamente.
    /// </summary>
    public class PauteoRapidoPanel
    {
        // Panel principal
        public Panel Panel { get; private set; }

        // Controles
        private Label lblTitulo;
        private Label lblAudioSeleccionado;
        private TextBox txtAudioPath;
        private Label lblFechaInicio;
        private DateTimePicker dtpFechaInicio;
        private Label lblFechaFin;
        private DateTimePicker dtpFechaFin;
        private Label lblPosicion;
        private ComboBox cboPosicion;
        private Label lblTandas;
        private Panel pnlTandas;
        private List<CheckBox> checksTandas = new List<CheckBox>();
        private Label lblDias;
        private Panel pnlDias;
        private CheckBox chkLunes, chkMartes, chkMiercoles, chkJueves, chkViernes, chkSabado, chkDomingo;
        private Button btnMarcarTodo;
        private Button btnLimpiarTandas;
        private Button btnGenerarRapido;
        private Button btnLimpiar;
        private Label lblContadorTandas;
        private Label lblEstado;
        private Label lblProgramacion;
        private ComboBox cboProgramacion;
        private TipoTanda _tipoTandaActual = TipoTanda.Tandas_00_30;

        // Datos
        private string _audioPathActual = null;
        private string _ciudadActual = null;
        private string _radioActual = null;

        // Modo edición
        private bool _modoEdicion = false;
        private string _codigoEdicion = null;
        private AgregarComercialesData _datosEdicion = null;


        // Evento cuando se genera una pauta
        public event EventHandler<PautaGeneradaEventArgs> PautaGenerada;

        // Evento cuando se hace click en una tanda (para vista previa)
        public event EventHandler<TandaClickedEventArgs> TandaClicked;

        public class TandaClickedEventArgs : EventArgs
        {
            public string Hora { get; set; }
            public DateTime Fecha { get; set; }
            public string Ciudad { get; set; }
            public string Radio { get; set; }
        }

        public class PautaGeneradaEventArgs : EventArgs
        {
            public string AudioPath { get; set; }
            public string Ciudad { get; set; }
            public string Radio { get; set; }
            public DateTime FechaInicio { get; set; }
            public DateTime FechaFin { get; set; }
            public string Posicion { get; set; }
            public List<string> TandasSeleccionadas { get; set; }
            public List<DayOfWeek> DiasSeleccionados { get; set; }
            public bool Exito { get; set; }
            public string Mensaje { get; set; }
        }

        public PauteoRapidoPanel()
        {
            CrearPanel();
        }

        private void CrearPanel()
        {
            Panel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(250, 252, 255),
                Padding = new Padding(5),
                AutoScroll = true
            };
            Panel.Resize += Panel_Resize;

            int y = 5;
            int margen = 5;

            // Título
            lblTitulo = new Label
            {
                Text = "PAUTEO RAPIDO",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 150, 243),
                Location = new Point(margen, y),
                AutoSize = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                TextAlign = ContentAlignment.MiddleCenter
            };
            Panel.Controls.Add(lblTitulo);
            y += 25;

            // Audio seleccionado (en una línea, responsive)
            lblAudioSeleccionado = new Label
            {
                Text = "Audio:",
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                Location = new Point(margen, y),
                Size = new Size(40, 18)
            };
            Panel.Controls.Add(lblAudioSeleccionado);

            txtAudioPath = new TextBox
            {
                Location = new Point(48, y),
                ReadOnly = true,
                BackColor = Color.White,
                Font = new Font("Segoe UI", 7.5F),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            Panel.Controls.Add(txtAudioPath);
            y += 22;

            // Fila 1: Fecha Inicio y Fin en la misma línea
            lblFechaInicio = new Label
            {
                Text = "Desde:",
                Font = new Font("Segoe UI", 8F),
                Location = new Point(margen, y + 2),
                Size = new Size(42, 18)
            };
            Panel.Controls.Add(lblFechaInicio);

            dtpFechaInicio = new DateTimePicker
            {
                Location = new Point(47, y),
                Size = new Size(100, 22),
                Format = DateTimePickerFormat.Short,
                Font = new Font("Segoe UI", 8F),
                Value = DateTime.Today
            };
            Panel.Controls.Add(dtpFechaInicio);

            lblFechaFin = new Label
            {
                Text = "Hasta:",
                Font = new Font("Segoe UI", 8F),
                Location = new Point(152, y + 2),
                Size = new Size(42, 18)
            };
            Panel.Controls.Add(lblFechaFin);

            dtpFechaFin = new DateTimePicker
            {
                Location = new Point(194, y),
                Size = new Size(100, 22),
                Format = DateTimePickerFormat.Short,
                Font = new Font("Segoe UI", 8F),
                Value = DateTime.Today.AddMonths(1),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            Panel.Controls.Add(dtpFechaFin);
            y += 26;

            // Fila: Posición y Programación (en la misma línea)
            lblPosicion = new Label
            {
                Text = "Pos:",
                Font = new Font("Segoe UI", 8F),
                Location = new Point(margen, y),
                Size = new Size(28, 18)
            };
            Panel.Controls.Add(lblPosicion);

            cboPosicion = new ComboBox
            {
                Location = new Point(35, y),
                Size = new Size(50, 20),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 8F)
            };
            for (int i = 1; i <= 20; i++)
            {
                cboPosicion.Items.Add($"P{i}");
            }
            cboPosicion.SelectedIndex = 0;
            Panel.Controls.Add(cboPosicion);

            lblProgramacion = new Label
            {
                Text = "Prog:",
                Font = new Font("Segoe UI", 8F),
                Location = new Point(90, y),
                Size = new Size(35, 18)
            };
            Panel.Controls.Add(lblProgramacion);

            cboProgramacion = new ComboBox
            {
                Location = new Point(125, y),
                Size = new Size(160, 20),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 7.5F),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            cboProgramacion.Items.Add("Cada 00-30 (48 tandas)");
            cboProgramacion.Items.Add("Cada 10-40 (48 tandas)");
            cboProgramacion.Items.Add("Cada 15-45 (48 tandas)");
            cboProgramacion.Items.Add("Cada 20-50 (48 tandas)");
            cboProgramacion.Items.Add("Cada 00-20-30-50 (96)");
            cboProgramacion.SelectedIndex = 0;
            cboProgramacion.SelectedIndexChanged += CboProgramacion_SelectedIndexChanged;
            Panel.Controls.Add(cboProgramacion);
            y += 24;

            // Días de la semana + Tandas controles (todo en una fila para ganar espacio)
            lblDias = new Label
            {
                Text = "Días:",
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                Location = new Point(margen, y + 3),
                Size = new Size(32, 18)
            };
            Panel.Controls.Add(lblDias);

            pnlDias = new Panel
            {
                Location = new Point(37, y),
                Size = new Size(260, 26),
                BackColor = Color.Transparent
            };
            Panel.Controls.Add(pnlDias);

            int xDia = 0;
            chkLunes = CrearCheckDia("L", ref xDia);
            chkMartes = CrearCheckDia("M", ref xDia);
            chkMiercoles = CrearCheckDia("X", ref xDia);
            chkJueves = CrearCheckDia("J", ref xDia);
            chkViernes = CrearCheckDia("V", ref xDia);
            chkSabado = CrearCheckDia("S", ref xDia);
            chkDomingo = CrearCheckDia("D", ref xDia);

            chkLunes.Checked = chkMartes.Checked = chkMiercoles.Checked = chkJueves.Checked = chkViernes.Checked = true;

            // Tandas label, contador y botones en la misma fila que los días
            lblTandas = new Label
            {
                Text = "Tandas:",
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                Location = new Point(xDia + 8, 3),
                Size = new Size(45, 18)
            };
            pnlDias.Controls.Add(lblTandas);

            lblContadorTandas = new Label
            {
                Text = "(0)",
                Font = new Font("Segoe UI", 7F),
                ForeColor = Color.Gray,
                Location = new Point(xDia + 52, 3),
                Size = new Size(28, 18)
            };
            pnlDias.Controls.Add(lblContadorTandas);

            btnMarcarTodo = new Button
            {
                Text = "Todo",
                Location = new Point(xDia + 80, 1),
                Size = new Size(40, 22),
                Font = new Font("Segoe UI", 7F),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White
            };
            btnMarcarTodo.FlatAppearance.BorderSize = 0;
            btnMarcarTodo.Click += BtnMarcarTodo_Click;
            pnlDias.Controls.Add(btnMarcarTodo);

            btnLimpiarTandas = new Button
            {
                Text = "Nada",
                Location = new Point(xDia + 122, 1),
                Size = new Size(38, 22),
                Font = new Font("Segoe UI", 7F),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(244, 67, 54),
                ForeColor = Color.White
            };
            btnLimpiarTandas.FlatAppearance.BorderSize = 0;
            btnLimpiarTandas.Click += BtnLimpiarTandas_Click;
            pnlDias.Controls.Add(btnLimpiarTandas);
            y += 28;

            // Panel de tandas (responsive - se ajusta al tamaño disponible)
            pnlTandas = new Panel
            {
                Location = new Point(margen, y),
                BackColor = Color.FromArgb(220, 240, 220),
                BorderStyle = BorderStyle.FixedSingle,
                AutoScroll = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };
            Panel.Controls.Add(pnlTandas);

            CrearGridTandas();

            // Botones de acción (en la parte inferior)
            btnGenerarRapido = new Button
            {
                Text = "GENERAR PAUTA",
                Size = new Size(180, 30),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                Enabled = false,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            btnGenerarRapido.FlatAppearance.BorderSize = 0;
            btnGenerarRapido.Click += BtnGenerarRapido_Click;
            Panel.Controls.Add(btnGenerarRapido);

            btnLimpiar = new Button
            {
                Text = "Limpiar",
                Size = new Size(55, 30),
                Font = new Font("Segoe UI", 8F),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(158, 158, 158),
                ForeColor = Color.White,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };
            btnLimpiar.FlatAppearance.BorderSize = 0;
            btnLimpiar.Click += BtnLimpiar_Click;
            Panel.Controls.Add(btnLimpiar);

            // Estado (debajo de los botones)
            lblEstado = new Label
            {
                Text = "Seleccione un audio para comenzar",
                Font = new Font("Segoe UI", 7.5F, FontStyle.Italic),
                ForeColor = Color.Gray,
                TextAlign = ContentAlignment.MiddleCenter,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            Panel.Controls.Add(lblEstado);

            // Ajustar posiciones iniciales
            AjustarLayoutResponsive();
        }

        /// <summary>
        /// Ajusta el layout de los controles según el tamaño del panel
        /// </summary>
        private void AjustarLayoutResponsive()
        {
            if (Panel == null || Panel.Width < 50) return;

            int margen = 5;
            int anchoDisponible = Panel.Width - (margen * 2) - 2;
            int altoDisponible = Panel.Height;

            // Ajustar título
            lblTitulo.Size = new Size(anchoDisponible, 22);

            // Ajustar textbox de audio
            txtAudioPath.Size = new Size(anchoDisponible - 48, 20);

            // Ajustar fechas de forma responsive
            int anchoFechaMin = 95;
            int espacioTotal = anchoDisponible - 42 - 42 - 10; // menos labels y espacios
            int anchoFecha = Math.Max(anchoFechaMin, espacioTotal / 2);

            // Posicionar fechas
            dtpFechaInicio.Size = new Size(anchoFecha, 22);
            int xHasta = 47 + anchoFecha + 5;
            lblFechaFin.Location = new Point(xHasta, lblFechaInicio.Location.Y);
            dtpFechaFin.Location = new Point(xHasta + 42, dtpFechaInicio.Location.Y);
            dtpFechaFin.Size = new Size(anchoDisponible - xHasta - 42, 22);

            // Ajustar panel de días (contiene días + tandas controles)
            pnlDias.Size = new Size(anchoDisponible - 37, 26);

            // Calcular espacio para el grid de tandas
            int altoFijoArriba = 125; // Reducido porque quitamos una fila
            int altoFijoAbajo = 55;
            int altoGridTandas = altoDisponible - altoFijoArriba - altoFijoAbajo;
            if (altoGridTandas < 150) altoGridTandas = 150;

            // Para 96 tandas, permitir más altura para mejor scroll
            string[] horasTandas = TandasHorarias.GetHorarios(_tipoTandaActual);
            int maxAlto = horasTandas.Length > 48 ? 450 : 350;
            if (altoGridTandas > maxAlto) altoGridTandas = maxAlto;

            // Ajustar panel de tandas
            pnlTandas.Size = new Size(anchoDisponible, altoGridTandas);

            // Posicionar botones al final
            int yBotones = altoFijoArriba + altoGridTandas + 5;
            btnGenerarRapido.Location = new Point(margen, yBotones);
            btnGenerarRapido.Size = new Size(anchoDisponible - 65, 32);
            btnLimpiar.Location = new Point(anchoDisponible - 55, yBotones);
            btnLimpiar.Size = new Size(60, 32);

            // Posicionar estado
            int yEstado = yBotones + 35;
            lblEstado.Location = new Point(margen, yEstado);
            lblEstado.Size = new Size(anchoDisponible, 18);

            // Recrear grid de tandas con el nuevo tamaño
            CrearGridTandasResponsive();
        }

        private void Panel_Resize(object sender, EventArgs e)
        {
            AjustarLayoutResponsive();
        }

        /// <summary>
        /// Crea el grid de tandas adaptándose al tamaño disponible con horas más grandes y visibles
        /// </summary>
        private void CrearGridTandasResponsive()
        {
            if (pnlTandas == null || pnlTandas.Width < 50) return;

            pnlTandas.Controls.Clear();
            checksTandas.Clear();

            string[] horasTandas = TandasHorarias.GetHorarios(_tipoTandaActual);
            int totalTandas = horasTandas.Length;

            // Calcular columnas según el ancho disponible
            int anchoPanel = pnlTandas.Width - 4;
            int altoPanel = pnlTandas.Height - 4;

            // Calcular columnas dinámicamente según el ancho
            int anchoMinCheck = 70; // Ancho mínimo para que se vea bien la hora
            int cols = Math.Max(4, anchoPanel / anchoMinCheck);

            // Limitar columnas según el tipo de tandas
            if (totalTandas <= 48)
            {
                if (cols > 8) cols = 8;
                if (cols < 4) cols = 4;
            }
            else
            {
                if (cols > 6) cols = 6;
                if (cols < 4) cols = 4;
            }

            int rows = (int)Math.Ceiling((double)totalTandas / cols);

            // Tamaños para mejor legibilidad - horas MÁS GRANDES
            int anchoCheck = (anchoPanel - (totalTandas > 48 ? 18 : 0)) / cols;
            int altoCheck;
            float fontSize;

            if (totalTandas <= 48)
            {
                // 48 tandas: tamaño más grande
                altoCheck = altoPanel / rows;
                if (altoCheck < 28) altoCheck = 28;
                if (altoCheck > 38) altoCheck = 38;
                fontSize = 9F; // Fuente más grande
                pnlTandas.AutoScroll = false;
            }
            else
            {
                // 96 tandas: usar scroll vertical con botones más grandes
                altoCheck = 32; // Altura fija más grande
                fontSize = 8.5F; // Fuente más grande
                pnlTandas.AutoScroll = true;
            }

            // Asegurar tamaño mínimo del check
            if (anchoCheck < 60) anchoCheck = 60;

            int col = 0;
            int row = 0;

            foreach (string hora in horasTandas)
            {
                var chk = new CheckBox
                {
                    Text = hora,
                    Location = new Point(col * anchoCheck + 1, row * altoCheck + 1),
                    Size = new Size(anchoCheck - 2, altoCheck - 2),
                    Font = new Font("Consolas", fontSize, FontStyle.Bold),
                    Appearance = Appearance.Button,
                    FlatStyle = FlatStyle.Flat,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Tag = hora
                };
                chk.FlatAppearance.CheckedBackColor = Color.FromArgb(255, 193, 7);
                chk.FlatAppearance.BorderSize = 1;
                chk.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);
                chk.CheckedChanged += (s, e) =>
                {
                    chk.BackColor = chk.Checked ? Color.FromArgb(255, 193, 7) : Color.FromArgb(235, 250, 235);
                    chk.ForeColor = chk.Checked ? Color.Black : Color.FromArgb(60, 60, 60);
                    ActualizarContadorTandas();
                };
                // Evento para mostrar vista previa con click derecho (MouseUp es más confiable en CheckBox Button)
                chk.MouseUp += (s, me) =>
                {
                    if (me.Button == MouseButtons.Right)
                    {
                        System.Diagnostics.Debug.WriteLine($"[PAUTEO RAPIDO] Click derecho en hora: {hora}");
                        MostrarVistaPreviaHora(hora);
                    }
                };
                chk.BackColor = Color.FromArgb(235, 250, 235);
                chk.ForeColor = Color.FromArgb(60, 60, 60);

                pnlTandas.Controls.Add(chk);
                checksTandas.Add(chk);

                col++;
                if (col >= cols)
                {
                    col = 0;
                    row++;
                }
            }

            ActualizarContadorTandas();
        }

        private CheckBox CrearCheckDia(string texto, ref int x)
        {
            var chk = new CheckBox
            {
                Text = texto,
                Location = new Point(x, 0),
                Size = new Size(32, 24),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Appearance = Appearance.Button,
                FlatStyle = FlatStyle.Flat,
                TextAlign = ContentAlignment.MiddleCenter
            };
            chk.FlatAppearance.CheckedBackColor = Color.FromArgb(76, 175, 80);
            chk.FlatAppearance.BorderSize = 1;
            chk.CheckedChanged += (s, e) =>
            {
                chk.ForeColor = chk.Checked ? Color.White : Color.Black;
                chk.BackColor = chk.Checked ? Color.FromArgb(76, 175, 80) : Color.White;
            };
            pnlDias.Controls.Add(chk);
            x += 33;
            return chk;
        }

        private void CrearGridTandas()
        {
            CrearGridTandasResponsive();
        }

        public void ActualizarTandasPorRadio(string radio)
        {
            TipoTanda tipo = TipoTanda.Tandas_00_30;
            int comboIndex = 0;

            if (!string.IsNullOrEmpty(radio))
            {
                string radioUpper = radio.ToUpper();
                if (radioUpper.Contains("KARIBE"))
                {
                    tipo = TipoTanda.Tandas_20_50;
                    comboIndex = 3;
                }
                else if (radioUpper.Contains("KALLE") || radioUpper.Contains("LAKALLE"))
                {
                    tipo = TipoTanda.Tandas_10_40;
                    comboIndex = 1;
                }
            }

            cboProgramacion.SelectedIndexChanged -= CboProgramacion_SelectedIndexChanged;
            cboProgramacion.SelectedIndex = comboIndex;
            cboProgramacion.SelectedIndexChanged += CboProgramacion_SelectedIndexChanged;

            _tipoTandaActual = tipo;
            CrearGridTandasPorTipo(tipo);
        }

        private void CboProgramacion_SelectedIndexChanged(object sender, EventArgs e)
        {
            TipoTanda tipo;
            switch (cboProgramacion.SelectedIndex)
            {
                case 0: tipo = TipoTanda.Tandas_00_30; break;
                case 1: tipo = TipoTanda.Tandas_10_40; break;
                case 2: tipo = TipoTanda.Tandas_15_45; break;
                case 3: tipo = TipoTanda.Tandas_20_50; break;
                case 4: tipo = TipoTanda.Tandas_00_20_30_50; break;
                default: tipo = TipoTanda.Tandas_00_30; break;
            }
            _tipoTandaActual = tipo;
            CrearGridTandasPorTipo(tipo);
        }

        private void CrearGridTandasPorTipo(TipoTanda tipo)
        {
            _tipoTandaActual = tipo;
            CrearGridTandasResponsive();
        }

        private void ActualizarContadorTandas()
        {
            int seleccionadas = checksTandas.Count(c => c.Checked);
            lblContadorTandas.Text = $"({seleccionadas})";
        }

        /// <summary>
        /// Dispara evento para mostrar vista previa de spots en una hora específica
        /// </summary>
        private void MostrarVistaPreviaHora(string hora)
        {
            System.Diagnostics.Debug.WriteLine($"[PAUTEO RAPIDO] MostrarVistaPreviaHora - Hora: {hora}, Ciudad: {_ciudadActual}, Radio: {_radioActual}");

            // Disparar evento para que Form1 cargue los spots en el panel derecho
            TandaClicked?.Invoke(this, new TandaClickedEventArgs
            {
                Hora = hora,
                Fecha = dtpFechaInicio.Value.Date,
                Ciudad = _ciudadActual,
                Radio = _radioActual
            });
        }

        private void BtnMarcarTodo_Click(object sender, EventArgs e)
        {
            foreach (var chk in checksTandas)
            {
                chk.Checked = true;
            }
        }

        private void BtnLimpiarTandas_Click(object sender, EventArgs e)
        {
            foreach (var chk in checksTandas)
            {
                chk.Checked = false;
            }
        }

        private void BtnLimpiar_Click(object sender, EventArgs e)
        {
            Limpiar();
        }

        public void Limpiar()
        {
            _audioPathActual = null;
            txtAudioPath.Text = "";
            dtpFechaInicio.Value = DateTime.Today;
            dtpFechaFin.Value = DateTime.Today.AddMonths(1);
            cboPosicion.SelectedIndex = 0;

            foreach (var chk in checksTandas)
            {
                chk.Checked = false;
            }

            chkLunes.Checked = chkMartes.Checked = chkMiercoles.Checked = chkJueves.Checked = chkViernes.Checked = true;
            chkSabado.Checked = chkDomingo.Checked = false;

            btnGenerarRapido.Enabled = false;
            lblEstado.Text = "Seleccione un audio para comenzar";
            lblEstado.ForeColor = Color.Gray;
        }

        public void SetAudioSeleccionado(string audioPath)
        {
            _audioPathActual = audioPath;
            txtAudioPath.Text = Path.GetFileName(audioPath);
            btnGenerarRapido.Enabled = true;
            lblEstado.Text = "Listo para generar";
            lblEstado.ForeColor = Color.FromArgb(76, 175, 80);
        }

        public void SetCiudadRadio(string ciudad, string radio)
        {
            _ciudadActual = ciudad;
            _radioActual = radio;
            ActualizarTandasPorRadio(radio);
        }

        public void CargarComercialParaEdicion(AgregarComercialesData datos)
        {
            if (datos == null) return;

            _modoEdicion = true;
            _codigoEdicion = datos.Codigo;
            _datosEdicion = datos;

            _audioPathActual = datos.FilePath;
            txtAudioPath.Text = !string.IsNullOrEmpty(datos.FilePath) ? Path.GetFileName(datos.FilePath) : datos.Codigo;
            _ciudadActual = datos.Ciudad;
            _radioActual = datos.Radio;

            dtpFechaInicio.Value = datos.FechaInicio;
            dtpFechaFin.Value = datos.FechaFinal;

            if (!string.IsNullOrEmpty(datos.Posicion))
            {
                string posicion = datos.Posicion.StartsWith("P") ? datos.Posicion : $"P{datos.Posicion}";
                int index = cboPosicion.Items.IndexOf(posicion);
                if (index >= 0) cboPosicion.SelectedIndex = index;
            }

            TipoTanda tipo = TipoTanda.Tandas_00_30;
            int comboIndex = 0;
            if (!string.IsNullOrEmpty(datos.TipoProgramacion))
            {
                if (datos.TipoProgramacion.Contains("10-40")) { tipo = TipoTanda.Tandas_10_40; comboIndex = 1; }
                else if (datos.TipoProgramacion.Contains("15-45")) { tipo = TipoTanda.Tandas_15_45; comboIndex = 2; }
                else if (datos.TipoProgramacion.Contains("20-50")) { tipo = TipoTanda.Tandas_20_50; comboIndex = 3; }
                else if (datos.TipoProgramacion.Contains("00-20-30-50")) { tipo = TipoTanda.Tandas_00_20_30_50; comboIndex = 4; }
            }
            cboProgramacion.SelectedIndexChanged -= CboProgramacion_SelectedIndexChanged;
            cboProgramacion.SelectedIndex = comboIndex;
            cboProgramacion.SelectedIndexChanged += CboProgramacion_SelectedIndexChanged;
            _tipoTandaActual = tipo;
            CrearGridTandasPorTipo(tipo);

            lblTitulo.Text = "EDITAR COMERCIAL";
            lblTitulo.ForeColor = Color.FromArgb(255, 152, 0);
            btnGenerarRapido.Text = "ACTUALIZAR";
            btnGenerarRapido.BackColor = Color.FromArgb(255, 152, 0);
            btnGenerarRapido.Enabled = true;

            string codigoMostrar = datos.Codigo;
            if (datos.Codigo.Contains("-"))
            {
                var partes = datos.Codigo.Split('-');
                if (partes.Length >= 2 && int.TryParse(partes[1], out _))
                {
                    codigoMostrar = partes[1];
                }
            }
            lblEstado.Text = $"Editando: {codigoMostrar}";
            lblEstado.ForeColor = Color.FromArgb(255, 152, 0);

            System.Diagnostics.Debug.WriteLine($"[PAUTEO RAPIDO] Cargado para edición: {datos.Codigo} (mostrado: {codigoMostrar})");
        }

        public void MarcarDiasAsignados(List<DayOfWeek> dias)
        {
            System.Diagnostics.Debug.WriteLine($"[PAUTEO RAPIDO] MarcarDiasAsignados - Recibidos: {dias?.Count ?? 0} días");

            chkLunes.Checked = false;
            chkMartes.Checked = false;
            chkMiercoles.Checked = false;
            chkJueves.Checked = false;
            chkViernes.Checked = false;
            chkSabado.Checked = false;
            chkDomingo.Checked = false;

            if (dias != null && dias.Count > 0)
            {
                System.Diagnostics.Debug.WriteLine($"[PAUTEO RAPIDO] Días a marcar: {string.Join(", ", dias)}");

                foreach (var dia in dias)
                {
                    switch (dia)
                    {
                        case DayOfWeek.Monday: chkLunes.Checked = true; break;
                        case DayOfWeek.Tuesday: chkMartes.Checked = true; break;
                        case DayOfWeek.Wednesday: chkMiercoles.Checked = true; break;
                        case DayOfWeek.Thursday: chkJueves.Checked = true; break;
                        case DayOfWeek.Friday: chkViernes.Checked = true; break;
                        case DayOfWeek.Saturday: chkSabado.Checked = true; break;
                        case DayOfWeek.Sunday: chkDomingo.Checked = true; break;
                    }
                }
            }
            else
            {
                chkLunes.Checked = true;
                chkMartes.Checked = true;
                chkMiercoles.Checked = true;
                chkJueves.Checked = true;
                chkViernes.Checked = true;
                chkSabado.Checked = true;
                chkDomingo.Checked = true;
                System.Diagnostics.Debug.WriteLine($"[PAUTEO RAPIDO] Sin info de días, marcando todos por defecto");
            }
        }

        public void MarcarTandasAsignadas(List<string> tandas)
        {
            System.Diagnostics.Debug.WriteLine($"[PAUTEO RAPIDO] MarcarTandasAsignadas - Recibidas: {tandas?.Count ?? 0} tandas");
            if (tandas != null && tandas.Count > 0)
            {
                System.Diagnostics.Debug.WriteLine($"[PAUTEO RAPIDO] Tandas a marcar: {string.Join(", ", tandas.Take(10))}");
            }

            foreach (var chk in checksTandas)
            {
                chk.Checked = false;
            }

            int marcadas = 0;
            if (tandas != null)
            {
                System.Diagnostics.Debug.WriteLine($"[PAUTEO RAPIDO] ChecksTandas disponibles: {checksTandas.Count}");
                if (checksTandas.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[PAUTEO RAPIDO] Ejemplo Tags: {string.Join(", ", checksTandas.Take(5).Select(c => c.Tag?.ToString() ?? "null"))}");
                }

                foreach (var tanda in tandas)
                {
                    var chk = checksTandas.FirstOrDefault(c => c.Tag?.ToString() == tanda);
                    if (chk != null)
                    {
                        chk.Checked = true;
                        marcadas++;
                        System.Diagnostics.Debug.WriteLine($"[PAUTEO RAPIDO] Marcada: {tanda}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[PAUTEO RAPIDO] NO encontrada: {tanda}");
                    }
                }
            }

            System.Diagnostics.Debug.WriteLine($"[PAUTEO RAPIDO] Total marcadas: {marcadas}");
            ActualizarContadorTandas();
        }

        public void SalirModoEdicion()
        {
            _modoEdicion = false;
            _codigoEdicion = null;
            _datosEdicion = null;

            lblTitulo.Text = "PAUTEO RAPIDO";
            lblTitulo.ForeColor = Color.FromArgb(33, 150, 243);
            btnGenerarRapido.Text = "GENERAR PAUTA";
            btnGenerarRapido.BackColor = Color.FromArgb(76, 175, 80);

            Limpiar();
        }

        private async void BtnGenerarRapido_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_audioPathActual))
            {
                MessageBox.Show("Seleccione un audio primero.", "Audio requerido",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrEmpty(_ciudadActual) || string.IsNullOrEmpty(_radioActual))
            {
                MessageBox.Show("Seleccione una ciudad y estación primero.", "Ciudad/Estación requerida",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var tandasSeleccionadas = checksTandas.Where(c => c.Checked).Select(c => c.Tag.ToString()).ToList();
            if (tandasSeleccionadas.Count == 0)
            {
                MessageBox.Show("Seleccione al menos una tanda.", "Tandas requeridas",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var diasSeleccionados = ObtenerDiasSeleccionados();
            if (diasSeleccionados.Count == 0)
            {
                MessageBox.Show("Seleccione al menos un día.", "Días requeridos",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnGenerarRapido.Enabled = false;
            btnGenerarRapido.Text = _modoEdicion ? "Actualizando..." : "Generando...";
            lblEstado.Text = "Procesando...";
            lblEstado.ForeColor = Color.Orange;

            try
            {
                var args = new PautaGeneradaEventArgs
                {
                    AudioPath = _audioPathActual,
                    Ciudad = _ciudadActual,
                    Radio = _radioActual,
                    FechaInicio = dtpFechaInicio.Value.Date,
                    FechaFin = dtpFechaFin.Value.Date,
                    Posicion = cboPosicion.SelectedItem.ToString().Replace("P", ""),
                    TandasSeleccionadas = tandasSeleccionadas,
                    DiasSeleccionados = diasSeleccionados
                };

                if (_modoEdicion && !string.IsNullOrEmpty(_codigoEdicion))
                {
                    await ActualizarComercialAsync(args);
                    lblEstado.Text = $"Comercial actualizado correctamente";
                }
                else
                {
                    await GenerarPautaRapidaAsync(args);
                    lblEstado.Text = $"Pauta generada correctamente";
                }

                PautaGenerada?.Invoke(this, args);
                lblEstado.ForeColor = Color.FromArgb(76, 175, 80);

                if (_modoEdicion)
                {
                    SalirModoEdicion();
                }
                else
                {
                    _audioPathActual = null;
                    txtAudioPath.Text = "";
                    lblEstado.Text = "Seleccione el siguiente audio";
                }
            }
            catch (Exception ex)
            {
                lblEstado.Text = $"Error: {ex.Message}";
                lblEstado.ForeColor = Color.Red;
                MessageBox.Show($"Error al {(_modoEdicion ? "actualizar" : "generar")} pauta: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnGenerarRapido.Enabled = !string.IsNullOrEmpty(_audioPathActual) || _modoEdicion;
                btnGenerarRapido.Text = _modoEdicion ? "ACTUALIZAR" : "GENERAR PAUTA";
            }
        }

        private List<DayOfWeek> ObtenerDiasSeleccionados()
        {
            var dias = new List<DayOfWeek>();
            if (chkLunes.Checked) dias.Add(DayOfWeek.Monday);
            if (chkMartes.Checked) dias.Add(DayOfWeek.Tuesday);
            if (chkMiercoles.Checked) dias.Add(DayOfWeek.Wednesday);
            if (chkJueves.Checked) dias.Add(DayOfWeek.Thursday);
            if (chkViernes.Checked) dias.Add(DayOfWeek.Friday);
            if (chkSabado.Checked) dias.Add(DayOfWeek.Saturday);
            if (chkDomingo.Checked) dias.Add(DayOfWeek.Sunday);
            return dias;
        }

        private async Task GenerarPautaRapidaAsync(PautaGeneradaEventArgs args)
        {
            int ultimoNumero = await DataAccess.ObtenerUltimoNumeroCodigoAsync(
                DatabaseConfig.ConnectionString,
                DatabaseConfig.TableName);
            int nuevoNumero = ultimoNumero + 1;

            TipoTanda tipoTanda = _tipoTandaActual;
            string tipoProgramacion = ObtenerNombreProgramacion(tipoTanda);

            string codigoPrincipal = nuevoNumero.ToString();

            var comercialData = new AgregarComercialesData
            {
                Codigo = codigoPrincipal,
                FechaInicio = args.FechaInicio,
                FechaFinal = args.FechaFin,
                Ciudad = args.Ciudad,
                Radio = args.Radio,
                Posicion = args.Posicion,
                Estado = "Activo",
                TipoProgramacion = tipoProgramacion
            };

            await DataAccess.InsertarDatosEnBaseDeDatosAsync(
                DatabaseConfig.ConnectionString,
                DatabaseConfig.TableName,
                comercialData,
                args.AudioPath);

            var horariosTanda = TandasHorarias.GetHorarios(tipoTanda);
            var asignaciones = new List<(int fila, int columna, string comercial, string codigo, DateTime fecha)>();

            for (DateTime fecha = args.FechaInicio; fecha <= args.FechaFin; fecha = fecha.AddDays(1))
            {
                if (!args.DiasSeleccionados.Contains(fecha.DayOfWeek))
                    continue;

                int columna = (int)fecha.DayOfWeek;
                if (columna == 0) columna = 7;

                foreach (string tandaHora in args.TandasSeleccionadas)
                {
                    int fila = TandasHorarias.GetFilaParaHora(tandaHora, tipoTanda);
                    if (fila >= 0)
                    {
                        asignaciones.Add((fila, columna, args.AudioPath, codigoPrincipal, fecha));
                    }
                }
            }

            if (asignaciones.Count > 0)
            {
                await DataAccess.InsertarAsignacionesMasivasAsync(
                    DatabaseConfig.ConnectionString,
                    asignaciones);
            }

            var generador = new GenerarPauta();
            await generador.RegenerarArchivosParaRangoAsync(
                args.FechaInicio,
                args.FechaFin,
                args.Ciudad,
                args.Radio,
                null);

            args.Exito = true;
            args.Mensaje = $"Pauta generada: {asignaciones.Count} asignaciones";

            System.Diagnostics.Debug.WriteLine($"[PAUTEO RAPIDO] Generado: {Path.GetFileName(args.AudioPath)} - {asignaciones.Count} asignaciones");
        }

        private async Task ActualizarComercialAsync(PautaGeneradaEventArgs args)
        {
            TipoTanda tipoTanda = _tipoTandaActual;
            string tipoProgramacion = ObtenerNombreProgramacion(tipoTanda);

            var comercialData = new AgregarComercialesData
            {
                Codigo = _codigoEdicion,
                FechaInicio = args.FechaInicio,
                FechaFinal = args.FechaFin,
                Ciudad = args.Ciudad,
                Radio = args.Radio,
                Posicion = args.Posicion,
                Estado = "Activo",
                TipoProgramacion = tipoProgramacion,
                FilePath = args.AudioPath
            };

            await DataAccess.ActualizarComercialAsync(
                DatabaseConfig.ConnectionString,
                DatabaseConfig.TableName,
                comercialData);

            await DataAccess.EliminarAsignacionesPorCodigoAsync(
                DatabaseConfig.ConnectionString,
                _codigoEdicion);

            var horariosTanda = TandasHorarias.GetHorarios(tipoTanda);
            var asignaciones = new List<(int fila, int columna, string comercial, string codigo, DateTime fecha)>();

            for (DateTime fecha = args.FechaInicio; fecha <= args.FechaFin; fecha = fecha.AddDays(1))
            {
                if (!args.DiasSeleccionados.Contains(fecha.DayOfWeek))
                    continue;

                int columna = (int)fecha.DayOfWeek;
                if (columna == 0) columna = 7;

                foreach (string tandaHora in args.TandasSeleccionadas)
                {
                    int fila = TandasHorarias.GetFilaParaHora(tandaHora, tipoTanda);
                    if (fila >= 0)
                    {
                        asignaciones.Add((fila, columna, args.AudioPath, _codigoEdicion, fecha));
                    }
                }
            }

            if (asignaciones.Count > 0)
            {
                await DataAccess.InsertarAsignacionesMasivasAsync(
                    DatabaseConfig.ConnectionString,
                    asignaciones);
            }

            var generador = new GenerarPauta();
            await generador.RegenerarArchivosParaRangoAsync(
                args.FechaInicio,
                args.FechaFin,
                args.Ciudad,
                args.Radio,
                null);

            args.Exito = true;
            args.Mensaje = $"Comercial actualizado: {asignaciones.Count} asignaciones";

            System.Diagnostics.Debug.WriteLine($"[PAUTEO RAPIDO] Actualizado: {_codigoEdicion} - {asignaciones.Count} asignaciones");
        }

        private string ObtenerNombreProgramacion(TipoTanda tipo)
        {
            switch (tipo)
            {
                case TipoTanda.Tandas_00_30: return "Cada 00-30";
                case TipoTanda.Tandas_10_40: return "Cada 10-40";
                case TipoTanda.Tandas_15_45: return "Cada 15-45";
                case TipoTanda.Tandas_20_50: return "Cada 20-50";
                case TipoTanda.Tandas_00_20_30_50: return "Cada 00-20-30-50";
                default: return "Cada 00-30";
            }
        }
    }
}

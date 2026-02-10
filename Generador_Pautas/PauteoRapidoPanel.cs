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

        /// <summary>
        /// Indica si el panel está actualmente en modo edición
        /// </summary>
        public bool EstaModoEdicion => _modoEdicion;

        /// <summary>
        /// Obtiene la fecha de inicio actual del panel
        /// </summary>
        public DateTime FechaInicioActual => dtpFechaInicio?.Value.Date ?? DateTime.Today;

        /// <summary>
        /// Obtiene la fecha final actual del panel
        /// </summary>
        public DateTime FechaFinalActual => dtpFechaFin?.Value.Date ?? DateTime.Today.AddMonths(1);

        // Evento cuando se genera una pauta
        public event EventHandler<PautaGeneradaEventArgs> PautaGenerada;

        // Evento cuando cambian las fechas del panel
        public event EventHandler FechasModificadas;

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
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 150, 243),
                Location = new Point(margen, y),
                AutoSize = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                TextAlign = ContentAlignment.MiddleCenter
            };
            Panel.Controls.Add(lblTitulo);
            y += 22;

            // Audio seleccionado (en una línea, responsive)
            lblAudioSeleccionado = new Label
            {
                Text = "Audio:",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Location = new Point(margen, y),
                Size = new Size(45, 22)
            };
            Panel.Controls.Add(lblAudioSeleccionado);

            txtAudioPath = new TextBox
            {
                Location = new Point(50, y),
                ReadOnly = true,
                BackColor = Color.White,
                Font = new Font("Segoe UI", 9F),
                Height = 22,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            Panel.Controls.Add(txtAudioPath);
            y += 26;

            // Fila 1: Fecha Inicio y Fin en la misma línea
            lblFechaInicio = new Label
            {
                Text = "Desde:",
                Font = new Font("Segoe UI", 9F),
                Location = new Point(margen, y + 2),
                Size = new Size(48, 20)
            };
            Panel.Controls.Add(lblFechaInicio);

            dtpFechaInicio = new DateTimePicker
            {
                Location = new Point(53, y),
                Size = new Size(110, 24),
                Format = DateTimePickerFormat.Short,
                Font = new Font("Segoe UI", 9F),
                Value = DateTime.Today
            };
            dtpFechaInicio.ValueChanged += (s, e) => FechasModificadas?.Invoke(this, EventArgs.Empty);
            Panel.Controls.Add(dtpFechaInicio);

            lblFechaFin = new Label
            {
                Text = "Hasta:",
                Font = new Font("Segoe UI", 9F),
                Location = new Point(168, y + 2),
                Size = new Size(45, 20)
            };
            Panel.Controls.Add(lblFechaFin);

            dtpFechaFin = new DateTimePicker
            {
                Location = new Point(213, y),
                Size = new Size(110, 24),
                Format = DateTimePickerFormat.Short,
                Font = new Font("Segoe UI", 9F),
                Value = DateTime.Today.AddMonths(1),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            dtpFechaFin.ValueChanged += (s, e) => FechasModificadas?.Invoke(this, EventArgs.Empty);
            Panel.Controls.Add(dtpFechaFin);
            y += 28;

            // Fila: Posición y Programación (en la misma línea)
            lblPosicion = new Label
            {
                Text = "Pos:",
                Font = new Font("Segoe UI", 9F),
                Location = new Point(margen, y + 2),
                Size = new Size(32, 20)
            };
            Panel.Controls.Add(lblPosicion);

            cboPosicion = new ComboBox
            {
                Location = new Point(37, y),
                Size = new Size(55, 24),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9F)
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
                Font = new Font("Segoe UI", 9F),
                Location = new Point(97, y + 2),
                Size = new Size(38, 20)
            };
            Panel.Controls.Add(lblProgramacion);

            cboProgramacion = new ComboBox
            {
                Location = new Point(135, y),
                Size = new Size(180, 24),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9F),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            cboProgramacion.Items.Add("Cada 00-30 (48 tandas)");
            cboProgramacion.Items.Add("Cada 10-40 (48 tandas)");
            cboProgramacion.Items.Add("Cada 15-45 (48 tandas)");
            cboProgramacion.Items.Add("Cada 20-50 (48 tandas)");
            cboProgramacion.SelectedIndex = 0;
            cboProgramacion.SelectedIndexChanged += CboProgramacion_SelectedIndexChanged;
            Panel.Controls.Add(cboProgramacion);
            y += 28;

            // Días de la semana + Tandas controles (todo en una fila para ganar espacio)
            lblDias = new Label
            {
                Text = "Días:",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Location = new Point(margen, y + 4),
                Size = new Size(38, 20)
            };
            Panel.Controls.Add(lblDias);

            pnlDias = new Panel
            {
                Location = new Point(43, y),
                Size = new Size(380, 30),
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
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Location = new Point(xDia + 8, 5),
                Size = new Size(52, 20)
            };
            pnlDias.Controls.Add(lblTandas);

            lblContadorTandas = new Label
            {
                Text = "(0)",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.Gray,
                Location = new Point(xDia + 60, 5),
                Size = new Size(35, 20)
            };
            pnlDias.Controls.Add(lblContadorTandas);

            btnMarcarTodo = new Button
            {
                Text = "Todo",
                Location = new Point(xDia + 98, 2),
                Size = new Size(48, 26),
                Font = new Font("Segoe UI", 9F),
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
                Location = new Point(xDia + 150, 2),
                Size = new Size(48, 26),
                Font = new Font("Segoe UI", 9F),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(244, 67, 54),
                ForeColor = Color.White
            };
            btnLimpiarTandas.FlatAppearance.BorderSize = 0;
            btnLimpiarTandas.Click += BtnLimpiarTandas_Click;
            pnlDias.Controls.Add(btnLimpiarTandas);
            y += 34;

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
                Text = "VISTA PREVIA",
                Size = new Size(180, 36),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
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
                Size = new Size(70, 36),
                Font = new Font("Segoe UI", 9F),
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
                Font = new Font("Segoe UI", 9F, FontStyle.Italic),
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

            int margen = 10;
            int anchoDisponible = Panel.Width - (margen * 2) - 2;
            int altoDisponible = Panel.Height;

            // Ajustar título
            lblTitulo.Size = new Size(anchoDisponible, 24);

            // Ajustar textbox de audio
            txtAudioPath.Size = new Size(anchoDisponible - 50, 22);

            // Ajustar fechas de forma responsive
            int anchoFechaMin = 100;
            int espacioTotal = anchoDisponible - 48 - 45 - 15; // menos labels y espacios
            int anchoFecha = Math.Max(anchoFechaMin, espacioTotal / 2);

            // Posicionar fechas
            dtpFechaInicio.Size = new Size(anchoFecha, 24);
            int xHasta = 53 + anchoFecha + 10;
            lblFechaFin.Location = new Point(xHasta, lblFechaInicio.Location.Y);
            dtpFechaFin.Location = new Point(xHasta + 45, dtpFechaInicio.Location.Y);
            dtpFechaFin.Size = new Size(anchoDisponible - xHasta - 45, 24);

            // Ajustar panel de días (contiene días + tandas controles)
            pnlDias.Size = new Size(anchoDisponible - 43, 30);

            // Calcular espacio para el grid de tandas
            int altoFijoArriba = 138; // Espacio mínimo arriba
            int altoBotonera = 42;    // Espacio para botones
            int altoGridTandas = altoDisponible - altoFijoArriba - altoBotonera;
            if (altoGridTandas < 200) altoGridTandas = 200;

            // Ajustar panel de tandas (sin límite máximo para que use todo el espacio)
            pnlTandas.Size = new Size(anchoDisponible, altoGridTandas);

            // Posicionar botones al final - visibles
            int yBotones = altoFijoArriba + altoGridTandas + 2;
            btnGenerarRapido.Location = new Point(margen, yBotones);
            btnGenerarRapido.Size = new Size(anchoDisponible - 75, 34);
            btnLimpiar.Location = new Point(anchoDisponible - 70, yBotones);
            btnLimpiar.Size = new Size(80, 34);

            // Posicionar estado (oculto si no hay espacio)
            int yEstado = yBotones + 36;
            lblEstado.Location = new Point(margen, yEstado);
            lblEstado.Size = new Size(anchoDisponible, 16);
            lblEstado.Visible = (yEstado + 16) < altoDisponible;

            // Recrear grid de tandas con el nuevo tamaño
            CrearGridTandasResponsive();
        }

        private void Panel_Resize(object sender, EventArgs e)
        {
            AjustarLayoutResponsive();
        }

        /// <summary>
        /// Crea el grid de tandas adaptándose al tamaño disponible
        /// Layout: 8 filas x 6 columnas (como la imagen de referencia)
        /// Columnas: bloques de 4 horas (00-03, 04-07, 08-11, 12-15, 16-19, 20-23)
        /// </summary>
        private void CrearGridTandasResponsive()
        {
            if (pnlTandas == null || pnlTandas.Width < 50) return;

            pnlTandas.Controls.Clear();
            checksTandas.Clear();

            string[] horasTandas = TandasHorarias.GetHorarios(_tipoTandaActual);
            int totalTandas = horasTandas.Length;

            int anchoPanel = pnlTandas.Width - 4;
            int altoPanel = pnlTandas.Height - 4;

            int rows, cols;
            int altoCheck;
            float fontSize;

            if (totalTandas <= 48)
            {
                // 48 tandas: 8 filas x 6 columnas
                rows = 8;
                cols = 6;
                altoCheck = (altoPanel - 4) / rows;
                if (altoCheck < 28) altoCheck = 28;
                fontSize = 14F;
                pnlTandas.AutoScroll = false;
            }
            else
            {
                // 96 tandas: 16 filas x 6 columnas con scroll
                rows = 16;
                cols = 6;
                altoCheck = 30;
                fontSize = 13F;
                pnlTandas.AutoScroll = true;
            }

            int anchoCheck = (anchoPanel - (totalTandas > 48 ? 18 : 0)) / cols;
            if (anchoCheck < 50) anchoCheck = 50;

            // Reorganizar las horas para mostrar en formato correcto
            string[] horasOrdenadas = OrdenarHorasParesImpares(horasTandas, totalTandas);

            int col = 0;
            int row = 0;

            foreach (string hora in horasOrdenadas)
            {
                // Mostrar sin dos puntos (0000 en vez de 00:00)
                string horaDisplay = hora.Replace(":", "");

                var chk = new CheckBox
                {
                    Text = horaDisplay,
                    Location = new Point(col * anchoCheck + 1, row * altoCheck + 1),
                    Size = new Size(anchoCheck - 2, altoCheck - 2),
                    Font = new Font("Consolas", fontSize, FontStyle.Bold),
                    Appearance = Appearance.Button,
                    FlatStyle = FlatStyle.Flat,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Tag = hora  // Guardar el formato original con dos puntos
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

        /// <summary>
        /// Ordena las horas para mostrar en formato 6x8 como la imagen de referencia:
        /// Columnas: bloques de 4 horas (00-03, 04-07, 08-11, 12-15, 16-19, 20-23)
        /// Filas: primera tanda de cada bloque, luego segunda tanda
        /// Ejemplo para 00-30:
        /// 0000 0400 0800 1200 1600 2000
        /// 0030 0430 0830 1230 1630 2030
        /// 0100 0500 0900 1300 1700 2100
        /// 0130 0530 0930 1330 1730 2130
        /// 0200 0600 1000 1400 1800 2200
        /// 0230 0630 1030 1430 1830 2230
        /// 0300 0700 1100 1500 1900 2300
        /// 0330 0730 1130 1530 1930 2330
        /// </summary>
        private string[] OrdenarHorasParesImpares(string[] horasOriginales, int totalTandas)
        {
            var resultado = new List<string>();

            if (totalTandas == 48)
            {
                // 48 tandas = 2 por hora
                // Agrupar por hora
                var porHora = new Dictionary<int, List<string>>();
                foreach (string h in horasOriginales)
                {
                    int hora = int.Parse(h.Substring(0, 2));
                    if (!porHora.ContainsKey(hora))
                        porHora[hora] = new List<string>();
                    porHora[hora].Add(h);
                }

                // 8 filas x 6 columnas
                // Cada fila tiene las horas 0,4,8,12,16,20 + offset
                // Fila 0: 00:XX, 04:XX, 08:XX, 12:XX, 16:XX, 20:XX (primera tanda)
                // Fila 1: 00:YY, 04:YY, 08:YY, 12:YY, 16:YY, 20:YY (segunda tanda)
                // Fila 2: 01:XX, 05:XX, 09:XX, 13:XX, 17:XX, 21:XX (primera tanda)
                // ...

                int[] baseHoras = { 0, 4, 8, 12, 16, 20 };

                for (int offset = 0; offset < 4; offset++)
                {
                    // Primera tanda de este offset
                    foreach (int bh in baseHoras)
                    {
                        int h = bh + offset;
                        if (porHora.ContainsKey(h) && porHora[h].Count > 0)
                            resultado.Add(porHora[h][0]);
                    }
                    // Segunda tanda de este offset
                    foreach (int bh in baseHoras)
                    {
                        int h = bh + offset;
                        if (porHora.ContainsKey(h) && porHora[h].Count > 1)
                            resultado.Add(porHora[h][1]);
                    }
                }
            }
            else if (totalTandas == 96)
            {
                // 96 tandas = 4 por hora
                var porHora = new Dictionary<int, List<string>>();
                foreach (string h in horasOriginales)
                {
                    int hora = int.Parse(h.Substring(0, 2));
                    if (!porHora.ContainsKey(hora))
                        porHora[hora] = new List<string>();
                    porHora[hora].Add(h);
                }

                int[] baseHoras = { 0, 4, 8, 12, 16, 20 };

                // Para 96 tandas: 4 tandas por hora, misma estructura pero más filas
                for (int offset = 0; offset < 4; offset++)
                {
                    for (int tanda = 0; tanda < 4; tanda++)
                    {
                        foreach (int bh in baseHoras)
                        {
                            int h = bh + offset;
                            if (porHora.ContainsKey(h) && porHora[h].Count > tanda)
                                resultado.Add(porHora[h][tanda]);
                        }
                    }
                }
            }
            else
            {
                return horasOriginales;
            }

            return resultado.ToArray();
        }

        private CheckBox CrearCheckDia(string texto, ref int x)
        {
            var chk = new CheckBox
            {
                Text = texto,
                Location = new Point(x, 0),
                Size = new Size(32, 28),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
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
                if (radioUpper.Contains("KARIBE") || radioUpper.Contains("KALLE") || radioUpper.Contains("LAKALLE") || radioUpper.Contains("HOT") || radioUpper.Contains("RADIO Z"))
                {
                    // KARIBEÑA, LA KALLE, LA HOT y RADIO Z = Cada 20-50
                    tipo = TipoTanda.Tandas_20_50;
                    comboIndex = 3;
                }
                else if (radioUpper.Contains("EXITOSA"))
                {
                    // EXITOSA = Cada 00-30
                    tipo = TipoTanda.Tandas_00_30;
                    comboIndex = 0;
                }
                // Por defecto (otras radios) = Cada 00-30
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
            // NO resetear fechas, posición ni días - el usuario los controla manualmente
            // Solo limpiar las tandas seleccionadas
            foreach (var chk in checksTandas)
            {
                chk.Checked = false;
            }

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

        /// <summary>
        /// Establece un nuevo audio manteniendo la configuración actual (fechas, días, tandas)
        /// La posición NO se avanza aquí - solo se avanza después de generar la pauta
        /// Si estábamos en modo edición, sale del modo edición pero mantiene la configuración
        /// </summary>
        public void SetAudioManteniendo(string audioPath)
        {
            // Si estamos en modo edición, salir pero mantener configuración
            if (_modoEdicion)
            {
                SalirModoEdicionSinLimpiar();
            }

            _audioPathActual = audioPath;
            txtAudioPath.Text = Path.GetFileName(audioPath);
            btnGenerarRapido.Enabled = true;
            lblEstado.Text = "Listo para generar";
            lblEstado.ForeColor = Color.FromArgb(76, 175, 80);
            // La posición se mantiene - solo avanza después de generar
        }

        /// <summary>
        /// Sale del modo edición manteniendo TODA la configuración actual
        /// Se usa cuando se selecciona un nuevo archivo desde el explorador
        /// </summary>
        private void SalirModoEdicionSinLimpiar()
        {
            _modoEdicion = false;
            _codigoEdicion = null;
            _datosEdicion = null;

            lblTitulo.Text = "PAUTEO RAPIDO";
            lblTitulo.ForeColor = Color.FromArgb(33, 150, 243);
            btnGenerarRapido.Text = "VISTA PREVIA";
            btnGenerarRapido.BackColor = Color.FromArgb(76, 175, 80);

            // NO resetear nada - mantener fechas, días, posición y tandas
            // El usuario controla todo manualmente
        }

        /// <summary>
        /// Avanza la posición al siguiente valor disponible
        /// </summary>
        private void AvanzarPosicion()
        {
            if (cboPosicion.Items.Count > 0 && cboPosicion.SelectedIndex < cboPosicion.Items.Count - 1)
            {
                cboPosicion.SelectedIndex++;
            }
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

            // Solo mostrar fechas desde hoy en adelante
            DateTime fechaHoy = DateTime.Today;
            dtpFechaInicio.Value = datos.FechaInicio < fechaHoy ? fechaHoy : datos.FechaInicio;
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
            btnGenerarRapido.Text = "VISTA PREVIA";
            btnGenerarRapido.BackColor = Color.FromArgb(76, 175, 80);

            Limpiar();
        }

        private async void BtnGenerarRapido_Click(object sender, EventArgs e)
        {
            Logger.Log($"[PAUTEO RAPIDO] === CLICK EN GENERAR/ACTUALIZAR ===");
            Logger.Log($"[PAUTEO RAPIDO] ModoEdicion: {_modoEdicion}, CodigoEdicion: {_codigoEdicion}");
            System.Diagnostics.Debug.WriteLine($"[PAUTEO RAPIDO] === CLICK EN GENERAR/ACTUALIZAR ===");
            System.Diagnostics.Debug.WriteLine($"[PAUTEO RAPIDO] ModoEdicion: {_modoEdicion}, CodigoEdicion: {_codigoEdicion}");

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
            Logger.Log($"[PAUTEO RAPIDO] Tandas seleccionadas: {tandasSeleccionadas.Count}");

            if (tandasSeleccionadas.Count == 0)
            {
                MessageBox.Show("Seleccione al menos una tanda.", "Tandas requeridas",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var diasSeleccionados = ObtenerDiasSeleccionados();
            Logger.Log($"[PAUTEO RAPIDO] Días seleccionados: {diasSeleccionados.Count} - {string.Join(", ", diasSeleccionados)}");
            Logger.Log($"[PAUTEO RAPIDO] CheckBoxes: L={chkLunes.Checked}, M={chkMartes.Checked}, X={chkMiercoles.Checked}, J={chkJueves.Checked}, V={chkViernes.Checked}, S={chkSabado.Checked}, D={chkDomingo.Checked}");

            if (diasSeleccionados.Count == 0)
            {
                MessageBox.Show("Seleccione al menos un día.", "Días requeridos",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

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

            // Mostrar vista previa antes de generar
            if (!_modoEdicion)
            {
                var vistaPrevia = GenerarVistaPrevia(args);
                if (!MostrarVistaPreviaYConfirmar(vistaPrevia, args))
                {
                    return; // Usuario canceló
                }
            }

            btnGenerarRapido.Enabled = false;
            btnGenerarRapido.Text = _modoEdicion ? "Actualizando..." : "Generando...";
            lblEstado.Text = "Procesando...";
            lblEstado.ForeColor = Color.Orange;

            try
            {
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
                    // Mantener TODO: fechas, días, posición y tandas - solo limpiar audio
                    _audioPathActual = null;
                    txtAudioPath.Text = "";
                    // NO resetear posición, fechas, días ni tandas - el usuario controla todo manualmente
                    lblEstado.Text = "Configuración mantenida. Seleccione siguiente audio";
                    btnGenerarRapido.Enabled = false;
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
                btnGenerarRapido.Text = _modoEdicion ? "ACTUALIZAR" : "VISTA PREVIA";
            }
        }

        /// <summary>
        /// Genera una lista con la vista previa de todas las tandas que se van a crear
        /// </summary>
        private List<(DateTime fecha, string hora, string posicion, string rutaComercial)> GenerarVistaPrevia(PautaGeneradaEventArgs args)
        {
            var resultado = new List<(DateTime fecha, string hora, string posicion, string rutaComercial)>();

            // Obtener la ruta de destino según la radio (la que se escribe en el .txt)
            string carpetaRadio = ConfigManager.ObtenerCarpetaRadio(args.Radio);
            string nombreArchivo = Path.GetFileName(args.AudioPath);
            string rutaDestino = Path.Combine(carpetaRadio, nombreArchivo);

            for (DateTime fecha = args.FechaInicio; fecha <= args.FechaFin; fecha = fecha.AddDays(1))
            {
                if (!args.DiasSeleccionados.Contains(fecha.DayOfWeek))
                    continue;

                foreach (string hora in args.TandasSeleccionadas)
                {
                    resultado.Add((fecha, hora, args.Posicion, rutaDestino));
                }
            }

            // Ordenar por fecha y hora
            return resultado.OrderBy(x => x.fecha).ThenBy(x => x.hora).ToList();
        }

        /// <summary>
        /// Muestra el formulario de vista previa y retorna true si el usuario confirma
        /// </summary>
        private bool MostrarVistaPreviaYConfirmar(List<(DateTime fecha, string hora, string posicion, string rutaComercial)> vistaPrevia, PautaGeneradaEventArgs args)
        {
            using (var form = new Form())
            {
                form.Text = "Vista Previa - Tandas a Generar";
                form.Size = new Size(700, 500);
                form.StartPosition = FormStartPosition.CenterParent;
                form.MinimizeBox = false;
                form.MaximizeBox = false;
                form.FormBorderStyle = FormBorderStyle.FixedDialog;

                // Label de información
                var lblInfo = new Label
                {
                    Text = $"Comercial: {Path.GetFileName(args.AudioPath)}\n" +
                           $"Ciudad: {args.Ciudad} | Radio: {args.Radio} | Posición: P{args.Posicion}\n" +
                           $"Total de tandas a generar: {vistaPrevia.Count}",
                    Location = new Point(10, 10),
                    Size = new Size(680, 50),
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold)
                };
                form.Controls.Add(lblInfo);

                // DataGridView con la vista previa
                var dgv = new DataGridView
                {
                    Location = new Point(10, 65),
                    Size = new Size(665, 340),
                    AllowUserToAddRows = false,
                    AllowUserToDeleteRows = false,
                    ReadOnly = true,
                    SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                    AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                    BackgroundColor = Color.White,
                    RowHeadersVisible = false
                };

                dgv.Columns.Add("Fecha", "Fecha");
                dgv.Columns.Add("Hora", "Hora");
                dgv.Columns.Add("Pos", "POS");
                dgv.Columns.Add("Ruta", "Ruta de Comercial");

                dgv.Columns["Fecha"].FillWeight = 15;
                dgv.Columns["Hora"].FillWeight = 10;
                dgv.Columns["Pos"].FillWeight = 8;
                dgv.Columns["Ruta"].FillWeight = 67;

                // Agregar filas
                foreach (var item in vistaPrevia)
                {
                    dgv.Rows.Add(
                        item.fecha.ToString("dd/MM/yyyy"),
                        item.hora,
                        item.posicion.PadLeft(2, '0'),
                        item.rutaComercial
                    );
                }

                // Estilo del grid
                dgv.EnableHeadersVisualStyles = false;
                dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(0, 120, 0);
                dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
                dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(240, 255, 240);

                form.Controls.Add(dgv);

                // Botón Generar
                var btnGenerar = new Button
                {
                    Text = "GENERAR PAUTA",
                    Location = new Point(10, 415),
                    Size = new Size(320, 40),
                    Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                    BackColor = Color.FromArgb(76, 175, 80),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    DialogResult = DialogResult.OK
                };
                btnGenerar.FlatAppearance.BorderSize = 0;
                form.Controls.Add(btnGenerar);

                // Botón Cancelar
                var btnCancelar = new Button
                {
                    Text = "Cancelar",
                    Location = new Point(355, 415),
                    Size = new Size(320, 40),
                    Font = new Font("Segoe UI", 11F),
                    BackColor = Color.FromArgb(158, 158, 158),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    DialogResult = DialogResult.Cancel
                };
                btnCancelar.FlatAppearance.BorderSize = 0;
                form.Controls.Add(btnCancelar);

                form.AcceptButton = btnGenerar;
                form.CancelButton = btnCancelar;

                return form.ShowDialog() == DialogResult.OK;
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

            // Convertir días seleccionados a string para guardar en BD
            string diasStr = ConvertirDiasAString(args.DiasSeleccionados);

            var comercialData = new AgregarComercialesData
            {
                Codigo = codigoPrincipal,
                FechaInicio = args.FechaInicio,
                FechaFinal = args.FechaFin,
                Ciudad = args.Ciudad,
                Radio = args.Radio,
                Posicion = args.Posicion,
                Estado = "Activo",
                TipoProgramacion = tipoProgramacion,
                DiasSeleccionados = diasStr
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
            Logger.Log($"[PAUTEO RAPIDO] === INICIO ActualizarComercialAsync ===");
            Logger.Log($"[PAUTEO RAPIDO] Código edición: {_codigoEdicion}");

            TipoTanda tipoTanda = _tipoTandaActual;
            string tipoProgramacion = ObtenerNombreProgramacion(tipoTanda);

            // Convertir días seleccionados a string (1=Lun, 2=Mar, ..., 0=Dom)
            string diasStr = ConvertirDiasAString(args.DiasSeleccionados);

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
                FilePath = args.AudioPath,
                DiasSeleccionados = diasStr
            };

            // Actualizar datos del comercial en la tabla Comerciales
            await DataAccess.ActualizarComercialAsync(
                DatabaseConfig.ConnectionString,
                DatabaseConfig.TableName,
                comercialData);

            // Los comerciales ACC tienen las horas embebidas en el código (ej: ACC-830-ABA-EXI-0800)
            bool esComercialACC = _codigoEdicion.StartsWith("ACC-", StringComparison.OrdinalIgnoreCase);
            Logger.Log($"[PAUTEO RAPIDO] Es comercial ACC: {esComercialACC}");

            if (!esComercialACC)
            {
                // Para comerciales nuevos (no ACC), actualizar las asignaciones
                Logger.Log($"[PAUTEO RAPIDO] Actualizar - Código: {_codigoEdicion}");
                Logger.Log($"[PAUTEO RAPIDO] Actualizar - Días seleccionados: {string.Join(", ", args.DiasSeleccionados)}");
                Logger.Log($"[PAUTEO RAPIDO] Actualizar - Tandas: {args.TandasSeleccionadas.Count}");
                Logger.Log($"[PAUTEO RAPIDO] Actualizar - Rango fechas: {args.FechaInicio:dd/MM/yyyy} - {args.FechaFin:dd/MM/yyyy}");

                await DataAccess.EliminarAsignacionesPorCodigoAsync(
                    DatabaseConfig.ConnectionString,
                    _codigoEdicion);

                var horariosTanda = TandasHorarias.GetHorarios(tipoTanda);
                var asignaciones = new List<(int fila, int columna, string comercial, string codigo, DateTime fecha)>();

                for (DateTime fecha = args.FechaInicio; fecha <= args.FechaFin; fecha = fecha.AddDays(1))
                {
                    System.Diagnostics.Debug.WriteLine($"[PAUTEO RAPIDO] Verificando fecha {fecha:dd/MM/yyyy} ({fecha.DayOfWeek})");

                    if (!args.DiasSeleccionados.Contains(fecha.DayOfWeek))
                    {
                        System.Diagnostics.Debug.WriteLine($"[PAUTEO RAPIDO]   -> Día {fecha.DayOfWeek} NO está en seleccionados, saltando");
                        continue;
                    }

                    System.Diagnostics.Debug.WriteLine($"[PAUTEO RAPIDO]   -> Día {fecha.DayOfWeek} SÍ está en seleccionados");
                    int columna = (int)fecha.DayOfWeek;
                    if (columna == 0) columna = 7;

                    foreach (string tandaHora in args.TandasSeleccionadas)
                    {
                        int fila = TandasHorarias.GetFilaParaHora(tandaHora, tipoTanda);
                        if (fila >= 0)
                        {
                            asignaciones.Add((fila, columna, args.AudioPath, _codigoEdicion, fecha));
                            System.Diagnostics.Debug.WriteLine($"[PAUTEO RAPIDO]   -> Agregada asignación: {fecha:dd/MM/yyyy} {tandaHora} col={columna}");
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[PAUTEO RAPIDO] Total asignaciones a insertar: {asignaciones.Count}");

                if (asignaciones.Count > 0)
                {
                    await DataAccess.InsertarAsignacionesMasivasAsync(
                        DatabaseConfig.ConnectionString,
                        asignaciones);
                }

                args.Mensaje = $"Comercial actualizado: {asignaciones.Count} asignaciones";
                Logger.Log($"[PAUTEO RAPIDO] Actualizado: {_codigoEdicion} - {asignaciones.Count} asignaciones insertadas");
            }
            else
            {
                // Para comerciales ACC, necesitamos crear/eliminar códigos según las horas seleccionadas
                Logger.Log($"[PAUTEO RAPIDO] Comercial ACC - ejecutando ActualizarHorasComercialACCAsync");
                Logger.Log($"[PAUTEO RAPIDO] ACC - Días seleccionados: {string.Join(", ", args.DiasSeleccionados)}");
                await ActualizarHorasComercialACCAsync(args, comercialData);
            }

            // Regenerar archivos de pautas para el rango de fechas
            Logger.Log($"[PAUTEO RAPIDO] Iniciando regeneración TXT...");
            var generador = new GenerarPauta();
            await generador.RegenerarArchivosParaRangoAsync(
                args.FechaInicio,
                args.FechaFin,
                args.Ciudad,
                args.Radio,
                null);

            Logger.Log($"[PAUTEO RAPIDO] === FIN ActualizarComercialAsync - ÉXITO ===");
            args.Exito = true;
        }

        private string ObtenerNombreProgramacion(TipoTanda tipo)
        {
            switch (tipo)
            {
                case TipoTanda.Tandas_00_30: return "Cada 00-30";
                case TipoTanda.Tandas_10_40: return "Cada 10-40";
                case TipoTanda.Tandas_15_45: return "Cada 15-45";
                case TipoTanda.Tandas_20_50: return "Cada 20-50";
                default: return "Cada 00-30";
            }
        }

        /// <summary>
        /// Actualiza las horas de un comercial ACC creando/eliminando códigos según las horas seleccionadas.
        /// Los comerciales ACC tienen formato: ACC-830-ABA-EXI-0800 donde el último segmento es la hora (HHMM).
        /// </summary>
        private async Task ActualizarHorasComercialACCAsync(PautaGeneradaEventArgs args, AgregarComercialesData datosBase)
        {
            // Extraer el código numérico del código de edición (ACC-830-ABA-EXI-0800 -> 830)
            string codigoNumerico = ExtraerCodigoNumericoACC(_codigoEdicion);
            if (string.IsNullOrEmpty(codigoNumerico))
            {
                args.Mensaje = "Error: No se pudo extraer el código numérico";
                System.Diagnostics.Debug.WriteLine($"[PAUTEO RAPIDO] Error ACC: No se pudo extraer código numérico de {_codigoEdicion}");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"[PAUTEO RAPIDO] Actualizando horas ACC para código numérico: {codigoNumerico}");

            // Obtener las horas actuales de los códigos ACC existentes
            var horasActuales = await DataAccess.ObtenerHorasACCPorCodigoNumericoAsync(
                DatabaseConfig.ConnectionString,
                DatabaseConfig.TableName,
                codigoNumerico);

            System.Diagnostics.Debug.WriteLine($"[PAUTEO RAPIDO] Horas actuales: {string.Join(", ", horasActuales)}");
            System.Diagnostics.Debug.WriteLine($"[PAUTEO RAPIDO] Horas seleccionadas: {string.Join(", ", args.TandasSeleccionadas)}");

            // Determinar qué horas agregar (están seleccionadas pero no existen)
            var horasAAgregar = args.TandasSeleccionadas.Except(horasActuales).ToList();

            // Determinar qué horas eliminar (existen pero no están seleccionadas)
            var horasAEliminar = horasActuales.Except(args.TandasSeleccionadas).ToList();

            System.Diagnostics.Debug.WriteLine($"[PAUTEO RAPIDO] Horas a agregar: {string.Join(", ", horasAAgregar)}");
            System.Diagnostics.Debug.WriteLine($"[PAUTEO RAPIDO] Horas a eliminar: {string.Join(", ", horasAEliminar)}");

            int agregadas = 0;
            int eliminadas = 0;

            // Crear nuevos códigos ACC para las horas agregadas
            foreach (string hora in horasAAgregar)
            {
                await DataAccess.CrearCodigoACCParaHoraAsync(
                    DatabaseConfig.ConnectionString,
                    DatabaseConfig.TableName,
                    _codigoEdicion,
                    hora,
                    datosBase);
                agregadas++;
            }

            // Eliminar códigos ACC para las horas removidas
            // Obtener todos los códigos completos para encontrar cuáles eliminar
            var todosLosCodigos = await DataAccess.ObtenerCodigosACCCompletosAsync(
                DatabaseConfig.ConnectionString,
                DatabaseConfig.TableName,
                codigoNumerico);

            foreach (string codigoCompleto in todosLosCodigos)
            {
                // Extraer la hora del código (ACC-830-ABA-EXI-0800 -> 08:00)
                string horaDelCodigo = ExtraerHoraDeCodigoACC(codigoCompleto);
                if (!string.IsNullOrEmpty(horaDelCodigo) && horasAEliminar.Contains(horaDelCodigo))
                {
                    await DataAccess.EliminarCodigoACCAsync(
                        DatabaseConfig.ConnectionString,
                        DatabaseConfig.TableName,
                        codigoCompleto);
                    eliminadas++;
                }
            }

            args.Mensaje = $"Comercial ACC actualizado: {agregadas} horas agregadas, {eliminadas} horas eliminadas";
            System.Diagnostics.Debug.WriteLine($"[PAUTEO RAPIDO] Actualizado ACC: {_codigoEdicion} - {agregadas} agregadas, {eliminadas} eliminadas");
        }

        /// <summary>
        /// Extrae el código numérico de un código ACC (ACC-830-ABA-EXI-0800 -> 830)
        /// </summary>
        private string ExtraerCodigoNumericoACC(string codigo)
        {
            if (string.IsNullOrEmpty(codigo)) return "";

            if (codigo.StartsWith("ACC-", StringComparison.OrdinalIgnoreCase))
            {
                string[] partes = codigo.Split('-');
                if (partes.Length >= 2)
                {
                    return partes[1];
                }
            }
            return "";
        }

        /// <summary>
        /// Extrae la hora de un código ACC (ACC-830-ABA-EXI-0800 -> 08:00)
        /// </summary>
        private string ExtraerHoraDeCodigoACC(string codigo)
        {
            if (string.IsNullOrEmpty(codigo)) return "";

            string[] partes = codigo.Split('-');
            if (partes.Length >= 5)
            {
                string horaStr = partes[partes.Length - 1];
                if (horaStr.Length == 4 && int.TryParse(horaStr, out _))
                {
                    return $"{horaStr.Substring(0, 2)}:{horaStr.Substring(2, 2)}";
                }
            }
            return "";
        }

        /// <summary>
        /// Convierte una lista de DayOfWeek a string para guardar en BD.
        /// Formato: "1,2,3,4,5,6,0" donde 1=Lunes, 2=Martes, ..., 0=Domingo
        /// </summary>
        private string ConvertirDiasAString(List<DayOfWeek> dias)
        {
            if (dias == null || dias.Count == 0) return "1,2,3,4,5"; // L-V por defecto

            var numeros = dias.Select(d => d == DayOfWeek.Sunday ? 0 : (int)d).OrderBy(n => n == 0 ? 7 : n);
            return string.Join(",", numeros);
        }

        /// <summary>
        /// Convierte un string de días guardado en BD a lista de DayOfWeek.
        /// Formato esperado: "1,2,3,4,5,6,0" donde 1=Lunes, 2=Martes, ..., 0=Domingo
        /// </summary>
        public static List<DayOfWeek> ConvertirStringADias(string diasStr)
        {
            var dias = new List<DayOfWeek>();
            if (string.IsNullOrEmpty(diasStr)) return dias;

            foreach (var num in diasStr.Split(','))
            {
                if (int.TryParse(num.Trim(), out int n))
                {
                    dias.Add(n == 0 ? DayOfWeek.Sunday : (DayOfWeek)n);
                }
            }
            return dias;
        }
    }
}

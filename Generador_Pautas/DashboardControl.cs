using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Generador_Pautas
{
    [ToolboxItem(true)]
    [DesignTimeVisible(true)]
    public partial class DashboardControl : UserControl
    {
        private DashboardStats stats;
        private Timer refreshTimer;

        // Colores del tema
        private readonly Color colorActivo = Color.FromArgb(76, 175, 80);      // Verde
        private readonly Color colorInactivo = Color.FromArgb(158, 158, 158);  // Gris
        private readonly Color colorPorVencer = Color.FromArgb(255, 152, 0);   // Naranja
        private readonly Color colorVencido = Color.FromArgb(244, 67, 54);     // Rojo
        private readonly Color colorFondo = Color.FromArgb(250, 250, 255);
        private readonly Color colorTitulo = Color.FromArgb(63, 81, 181);      // Azul indigo

        // Rectangulos de las tarjetas para detectar clics
        private Rectangle rectTarjetaPorVencer;
        private Rectangle rectTarjetaVencidos;

        public DashboardControl()
        {
            InitializeComponent();
            this.DoubleBuffered = true;

            // No inicializar timer en modo diseño
            if (!DesignMode && LicenseManager.UsageMode != LicenseUsageMode.Designtime)
            {
                SetupRefreshTimer();
                this.MouseClick += DashboardControl_MouseClick;
                this.MouseMove += DashboardControl_MouseMove;
            }
            this.Cursor = Cursors.Default;
        }

        private void DashboardControl_MouseMove(object sender, MouseEventArgs e)
        {
            // Cambiar cursor a mano cuando pase sobre tarjetas clickeables
            if (rectTarjetaPorVencer.Contains(e.Location) || rectTarjetaVencidos.Contains(e.Location))
            {
                this.Cursor = Cursors.Hand;
            }
            else
            {
                this.Cursor = Cursors.Default;
            }
        }

        private void SetupRefreshTimer()
        {
            refreshTimer = new Timer();
            refreshTimer.Interval = 60000; // Refrescar cada minuto
            refreshTimer.Tick += RefreshTimer_Tick;
        }

        private async void RefreshTimer_Tick(object sender, EventArgs e)
        {
            // Verificar que el control no haya sido destruido antes de refrescar
            if (this.IsDisposed || this.Disposing)
            {
                refreshTimer?.Stop();
                return;
            }
            await CargarEstadisticasAsync();
        }

        protected override async void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            // No ejecutar en modo de diseño
            if (DesignMode || LicenseManager.UsageMode == LicenseUsageMode.Designtime)
                return;

            // Verificar que el control no haya sido destruido
            if (this.IsDisposed || this.Disposing) return;

            try
            {
                await CargarEstadisticasAsync();
                // Solo iniciar el timer si el control sigue activo
                if (!this.IsDisposed && !this.Disposing)
                {
                    refreshTimer.Start();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en OnLoad: {ex.Message}");
            }
        }

        public async Task CargarEstadisticasAsync()
        {
            // Verificar que el control no haya sido destruido
            if (this.IsDisposed || this.Disposing) return;

            try
            {
                stats = await DashboardStats.CargarEstadisticasAsync(
                    DatabaseConfig.ConnectionString);

                // Verificar de nuevo despues de la operacion async
                if (this.IsDisposed || this.Disposing) return;

                this.Invalidate(); // Redibujar
                ActualizarAlertas();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error cargando estadisticas: {ex.Message}");
            }
        }

        private void ActualizarAlertas()
        {
            // Liberar recursos de controles anteriores antes de limpiar
            foreach (Control ctrl in panelAlertas.Controls)
            {
                ctrl.Dispose();
            }
            panelAlertas.Controls.Clear();

            if (stats?.AlertasProximosAVencer == null || stats.AlertasProximosAVencer.Count == 0)
            {
                Label lblSinAlertas = new Label
                {
                    Text = "No hay comerciales proximos a vencer",
                    ForeColor = Color.Gray,
                    Font = new Font("Segoe UI", 9F, FontStyle.Italic),
                    AutoSize = false,
                    Dock = DockStyle.Top,
                    Height = 30,
                    TextAlign = ContentAlignment.MiddleCenter
                };
                panelAlertas.Controls.Add(lblSinAlertas);
                return;
            }

            int yPos = 5;
            // Limitar a maximo 10 alertas para evitar crear demasiados controles
            int maxAlertas = Math.Min(stats.AlertasProximosAVencer.Count, 10);
            for (int i = 0; i < maxAlertas; i++)
            {
                var alerta = stats.AlertasProximosAVencer[i];
                Panel alertaPanel = CrearPanelAlerta(alerta);
                alertaPanel.Location = new Point(5, yPos);
                panelAlertas.Controls.Add(alertaPanel);
                yPos += alertaPanel.Height + 5;
            }
        }

        private Panel CrearPanelAlerta(ComercialAlerta alerta)
        {
            Panel panel = new Panel
            {
                Width = panelAlertas.Width - 30,
                Height = 50,
                BackColor = alerta.DiasRestantes <= 2 ? Color.FromArgb(255, 235, 238) : Color.FromArgb(255, 243, 224),
                Padding = new Padding(8)
            };

            // Icono de alerta
            Label iconLabel = new Label
            {
                Text = alerta.DiasRestantes <= 2 ? "!" : "i",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = alerta.DiasRestantes <= 2 ? colorVencido : colorPorVencer,
                Size = new Size(25, 25),
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(5, 12)
            };

            // Mensaje
            Label msgLabel = new Label
            {
                Text = alerta.MensajeAlerta,
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(60, 60, 60),
                AutoSize = false,
                Location = new Point(35, 5),
                Size = new Size(panel.Width - 45, 20)
            };

            // Detalles
            Label detailLabel = new Label
            {
                Text = $"{alerta.NombreArchivo} - {alerta.Ciudad}/{alerta.Radio}",
                Font = new Font("Segoe UI", 7.5F),
                ForeColor = Color.Gray,
                AutoSize = false,
                Location = new Point(35, 25),
                Size = new Size(panel.Width - 45, 18)
            };

            panel.Controls.AddRange(new Control[] { iconLabel, msgLabel, detailLabel });
            return panel;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            // En modo diseño, mostrar un rectangulo de preview
            if (DesignMode || LicenseManager.UsageMode == LicenseUsageMode.Designtime)
            {
                using (var brush = new SolidBrush(Color.FromArgb(250, 250, 255)))
                using (var pen = new Pen(Color.FromArgb(63, 81, 181), 2))
                using (var font = new Font("Segoe UI Semibold", 11F))
                {
                    e.Graphics.FillRectangle(brush, ClientRectangle);
                    e.Graphics.DrawRectangle(pen, 1, 1, Width - 3, Height - 3);
                    e.Graphics.DrawString("Dashboard Control (Preview)", font, Brushes.Gray, 10, 10);
                }
                return;
            }

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            if (stats == null) return;

            // Dibujar tarjetas de estadisticas - version toolbar horizontal ultra compacta
            int cardWidth = 55;
            int cardHeight = 36;
            int spacing = 4;
            int startX = 5;  // Sin titulo, directo las tarjetas
            int startY = 4;

            // Tarjeta Total
            DibujarTarjetaCompacta(g, startX, startY, cardWidth, cardHeight,
                stats.TotalComerciales.ToString(), "Total", colorTitulo);

            // Tarjeta Activos
            DibujarTarjetaCompacta(g, startX + cardWidth + spacing, startY, cardWidth, cardHeight,
                stats.ComercialesActivos.ToString(), "Activos", colorActivo);

            // Tarjeta Por Vencer - Guardar rectangulo para detectar clic
            int xPorVencer = startX + (cardWidth + spacing) * 2;
            rectTarjetaPorVencer = new Rectangle(xPorVencer, startY, cardWidth, cardHeight);
            DibujarTarjetaCompacta(g, xPorVencer, startY, cardWidth, cardHeight,
                stats.ComercialesPorVencer.ToString(), "Por Vencer", colorPorVencer);

            // Tarjeta Vencidos - Guardar rectangulo para detectar clic
            int xVencidos = startX + (cardWidth + spacing) * 3;
            rectTarjetaVencidos = new Rectangle(xVencidos, startY, cardWidth, cardHeight);
            DibujarTarjetaCompacta(g, xVencidos, startY, cardWidth, cardHeight,
                stats.ComercialesVencidos.ToString(), "Vencidos", colorVencido);
        }

        private void DibujarTarjeta(Graphics g, int x, int y, int width, int height,
            string valor, string titulo, Color color)
        {
            // Fondo de la tarjeta con sombra suave
            using (GraphicsPath path = RoundedRect(new Rectangle(x, y, width, height), 8))
            {
                // Sombra
                using (SolidBrush shadowBrush = new SolidBrush(Color.FromArgb(20, 0, 0, 0)))
                {
                    g.TranslateTransform(2, 2);
                    g.FillPath(shadowBrush, path);
                    g.TranslateTransform(-2, -2);
                }

                // Fondo blanco
                using (SolidBrush bgBrush = new SolidBrush(Color.White))
                {
                    g.FillPath(bgBrush, path);
                }

                // Borde superior coloreado
                using (Pen borderPen = new Pen(color, 3))
                {
                    g.DrawLine(borderPen, x + 8, y, x + width - 8, y);
                }
            }

            // Valor grande - version compacta
            using (Font valorFont = new Font("Segoe UI Semibold", 14F))
            using (SolidBrush valorBrush = new SolidBrush(color))
            {
                SizeF valorSize = g.MeasureString(valor, valorFont);
                g.DrawString(valor, valorFont, valorBrush,
                    x + (width - valorSize.Width) / 2, y + 8);
            }

            // Titulo - version compacta
            using (Font tituloFont = new Font("Segoe UI", 7F))
            using (SolidBrush tituloBrush = new SolidBrush(Color.Gray))
            {
                SizeF tituloSize = g.MeasureString(titulo, tituloFont);
                g.DrawString(titulo, tituloFont, tituloBrush,
                    x + (width - tituloSize.Width) / 2, y + height - 18);
            }
        }

        private void DibujarTarjetaCompacta(Graphics g, int x, int y, int width, int height,
            string valor, string titulo, Color color)
        {
            // Fondo de la tarjeta con bordes redondeados - version ultra compacta para toolbar
            using (GraphicsPath path = RoundedRect(new Rectangle(x, y, width, height), 4))
            {
                // Fondo blanco
                using (SolidBrush bgBrush = new SolidBrush(Color.White))
                {
                    g.FillPath(bgBrush, path);
                }

                // Borde izquierdo coloreado (en vez de superior)
                using (Pen borderPen = new Pen(color, 2))
                {
                    g.DrawLine(borderPen, x + 1, y + 4, x + 1, y + height - 4);
                }
            }

            // Valor centrado
            using (Font valorFont = new Font("Segoe UI Semibold", 10F))
            using (SolidBrush valorBrush = new SolidBrush(color))
            {
                SizeF valorSize = g.MeasureString(valor, valorFont);
                g.DrawString(valor, valorFont, valorBrush, x + (width - valorSize.Width) / 2, y + 2);
            }

            // Titulo debajo del valor centrado
            using (Font tituloFont = new Font("Segoe UI", 6F))
            using (SolidBrush tituloBrush = new SolidBrush(Color.Gray))
            {
                SizeF tituloSize = g.MeasureString(titulo, tituloFont);
                g.DrawString(titulo, tituloFont, tituloBrush, x + (width - tituloSize.Width) / 2, y + height - 13);
            }
        }

        private GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            int diameter = radius * 2;
            GraphicsPath path = new GraphicsPath();

            path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();

            return path;
        }

        public async void RefrescarDashboard()
        {
            // Verificar que el control no haya sido destruido
            if (this.IsDisposed || this.Disposing) return;
            await CargarEstadisticasAsync();
        }

        private void DashboardControl_MouseClick(object sender, MouseEventArgs e)
        {
            // Verificar si se hizo clic en la tarjeta "Por Vencer"
            if (rectTarjetaPorVencer.Contains(e.Location))
            {
                MostrarComercialesPorVencer();
            }
            // Verificar si se hizo clic en la tarjeta "Vencidos"
            else if (rectTarjetaVencidos.Contains(e.Location))
            {
                MostrarComercialesVencidos();
            }
        }

        private void MostrarComercialesPorVencer()
        {
            if (stats?.AlertasProximosAVencer == null || stats.AlertasProximosAVencer.Count == 0)
            {
                MessageBox.Show("No hay comerciales proximos a vencer.", "Informacion",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            MostrarListaComerciales("Comerciales Por Vencer (proximos 7 dias)",
                stats.AlertasProximosAVencer, colorPorVencer);
        }

        private async void MostrarComercialesVencidos()
        {
            // Obtener lista de comerciales vencidos desde la base de datos
            var comercialesVencidos = await ObtenerComercialesVencidosAsync();

            if (comercialesVencidos == null || comercialesVencidos.Count == 0)
            {
                MessageBox.Show("No hay comerciales vencidos.", "Informacion",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            MostrarListaComerciales("Comerciales Vencidos", comercialesVencidos, colorVencido);
        }

        private async Task<System.Collections.Generic.List<ComercialAlerta>> ObtenerComercialesVencidosAsync()
        {
            var vencidos = new System.Collections.Generic.List<ComercialAlerta>();
            DateTime hoy = DateTime.Today;

            try
            {
                using (var conn = new Npgsql.NpgsqlConnection(DatabaseConfig.ConnectionString))
                {
                    await conn.OpenAsync();

                    string query = @"SELECT Codigo, FilePath, FechaFinal, Ciudad, Radio
                        FROM Comerciales
                        WHERE Estado = 'Activo'
                        AND FechaFinal::date < @Hoy::date
                        ORDER BY FechaFinal::date DESC";

                    using (var cmd = new Npgsql.NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Hoy", hoy.ToString("yyyy-MM-dd"));

                        using (var reader = (Npgsql.NpgsqlDataReader)await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                DateTime fechaFinal;
                                fechaFinal = Convert.ToDateTime(reader["FechaFinal"]);

                                vencidos.Add(new ComercialAlerta
                                {
                                    Codigo = reader["Codigo"].ToString(),
                                    NombreArchivo = System.IO.Path.GetFileNameWithoutExtension(reader["FilePath"].ToString()),
                                    FechaVencimiento = fechaFinal,
                                    DiasRestantes = (fechaFinal - hoy).Days,
                                    Ciudad = reader["Ciudad"].ToString(),
                                    Radio = reader["Radio"].ToString()
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error obteniendo comerciales vencidos: {ex.Message}");
            }

            return vencidos;
        }

        private void MostrarListaComerciales(string titulo, System.Collections.Generic.List<ComercialAlerta> comerciales, Color colorTema)
        {
            // Crear formulario de dialogo
            Form formLista = new Form
            {
                Text = titulo,
                Size = new Size(700, 450),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.FromArgb(250, 250, 250)
            };
            AppIcon.ApplyTo(formLista); // Aplicar icono de la aplicacion

            // Panel de titulo
            Panel pnlTitulo = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = colorTema
            };

            Label lblTitulo = new Label
            {
                Text = $"{titulo} ({comerciales.Count})",
                Font = new Font("Segoe UI Semibold", 14F),
                ForeColor = Color.White,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            pnlTitulo.Controls.Add(lblTitulo);

            // DataGridView para mostrar la lista
            DataGridView dgv = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                GridColor = Color.FromArgb(230, 230, 230),
                Font = new Font("Segoe UI", 9F)
            };

            // Estilo de encabezados
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(240, 240, 240);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(60, 60, 60);
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9F);
            dgv.ColumnHeadersHeight = 35;
            dgv.EnableHeadersVisualStyles = false;

            // Configurar columnas
            dgv.Columns.Add("Codigo", "Codigo");
            dgv.Columns.Add("NombreArchivo", "Nombre del Comercial");
            dgv.Columns.Add("Ciudad", "Ciudad");
            dgv.Columns.Add("Radio", "Radio");
            dgv.Columns.Add("FechaVencimiento", "Fecha Vencimiento");
            dgv.Columns.Add("Estado", "Estado");

            dgv.Columns["Codigo"].Width = 80;
            dgv.Columns["NombreArchivo"].FillWeight = 150;
            dgv.Columns["Ciudad"].Width = 90;
            dgv.Columns["Radio"].Width = 90;
            dgv.Columns["FechaVencimiento"].Width = 110;
            dgv.Columns["Estado"].Width = 100;

            // Agregar filas
            foreach (var comercial in comerciales)
            {
                string estado;
                if (comercial.DiasRestantes < 0)
                    estado = $"Vencido hace {Math.Abs(comercial.DiasRestantes)} dias";
                else if (comercial.DiasRestantes == 0)
                    estado = "Vence HOY";
                else if (comercial.DiasRestantes == 1)
                    estado = "Vence MANANA";
                else
                    estado = $"Vence en {comercial.DiasRestantes} dias";

                int rowIndex = dgv.Rows.Add(
                    comercial.Codigo,
                    comercial.NombreArchivo,
                    comercial.Ciudad,
                    comercial.Radio,
                    comercial.FechaVencimiento.ToString("dd/MM/yyyy"),
                    estado
                );

                // Colorear segun urgencia
                if (comercial.DiasRestantes < 0)
                {
                    dgv.Rows[rowIndex].DefaultCellStyle.BackColor = Color.FromArgb(255, 235, 238);
                    dgv.Rows[rowIndex].DefaultCellStyle.ForeColor = colorVencido;
                }
                else if (comercial.DiasRestantes <= 2)
                {
                    dgv.Rows[rowIndex].DefaultCellStyle.BackColor = Color.FromArgb(255, 243, 224);
                    dgv.Rows[rowIndex].DefaultCellStyle.ForeColor = Color.FromArgb(200, 100, 0);
                }
            }

            // Boton cerrar
            Button btnCerrar = new Button
            {
                Text = "Cerrar",
                Size = new Size(100, 35),
                FlatStyle = FlatStyle.Flat,
                BackColor = colorTema,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };
            btnCerrar.FlatAppearance.BorderSize = 0;
            btnCerrar.Click += (s, e) => formLista.Close();

            Panel pnlBotones = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                Padding = new Padding(10)
            };
            btnCerrar.Location = new Point(pnlBotones.Width - 120, 8);
            pnlBotones.Controls.Add(btnCerrar);

            // Agregar controles al formulario
            formLista.Controls.Add(dgv);
            formLista.Controls.Add(pnlBotones);
            formLista.Controls.Add(pnlTitulo);

            // Ajustar posicion del boton cuando cambie el tamano
            pnlBotones.Resize += (s, e) => btnCerrar.Location = new Point(pnlBotones.Width - 120, 8);

            formLista.ShowDialog();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Detener y liberar el timer
                if (refreshTimer != null)
                {
                    refreshTimer.Stop();
                    refreshTimer.Tick -= RefreshTimer_Tick;
                    refreshTimer.Dispose();
                    refreshTimer = null;
                }

                // Liberar controles del panel de alertas
                if (panelAlertas != null)
                {
                    foreach (Control ctrl in panelAlertas.Controls)
                    {
                        ctrl.Dispose();
                    }
                    panelAlertas.Controls.Clear();
                }

                components?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}

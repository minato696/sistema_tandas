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
    /// Formulario para comparar archivos TXT de pautas entre el sistema actual y el sistema antiguo.
    /// </summary>
    public class ComparadorPautasForm : Form
    {
        private Label lblTitulo;
        private GroupBox grpSistemaActual;
        private GroupBox grpSistemaAntiguo;
        private TextBox txtRutaActual;
        private TextBox txtRutaAntiguo;
        private Button btnSeleccionarActual;
        private Button btnSeleccionarAntiguo;
        private Button btnComparar;
        private Button btnLimpiar;
        private ListBox lstArchivosActual;
        private ListBox lstArchivosAntiguo;
        private DataGridView dgvResultados;
        private Label lblEstadisticas;
        private ProgressBar progressBar;
        private Label lblProgreso;
        private Panel pnlResumen;
        private Label lblResumenTotal;
        private Label lblResumenIguales;
        private Label lblResumenDiferentes;
        private Label lblResumenSoloActual;
        private Label lblResumenSoloAntiguo;

        public ComparadorPautasForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // Configuracion del formulario
            this.Text = "Comparador de Pautas - Sistema Actual vs Sistema Antiguo";
            this.Size = new Size(1200, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(245, 245, 250);
            this.MinimumSize = new Size(1000, 700);

            // Titulo
            lblTitulo = new Label
            {
                Text = "Comparador de Archivos de Pautas (.txt)",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = Color.FromArgb(63, 81, 181),
                Location = new Point(20, 15),
                AutoSize = true
            };
            this.Controls.Add(lblTitulo);

            // Panel superior para seleccion de carpetas
            Panel pnlSeleccion = new Panel
            {
                Location = new Point(20, 55),
                Size = new Size(1140, 180),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(pnlSeleccion);

            // Grupo Sistema Actual
            grpSistemaActual = new GroupBox
            {
                Text = "Sistema Actual (Nuevo)",
                Font = new Font("Segoe UI Semibold", 10F),
                ForeColor = Color.FromArgb(76, 175, 80),
                Location = new Point(10, 10),
                Size = new Size(550, 160)
            };
            pnlSeleccion.Controls.Add(grpSistemaActual);

            Label lblRutaActual = new Label
            {
                Text = "Carpeta de pautas:",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(60, 60, 60),
                Location = new Point(15, 30),
                AutoSize = true
            };
            grpSistemaActual.Controls.Add(lblRutaActual);

            txtRutaActual = new TextBox
            {
                Location = new Point(15, 50),
                Size = new Size(440, 25),
                Font = new Font("Segoe UI", 9F),
                ReadOnly = true,
                BackColor = Color.FromArgb(250, 250, 250)
            };
            grpSistemaActual.Controls.Add(txtRutaActual);

            btnSeleccionarActual = new Button
            {
                Text = "...",
                Location = new Point(460, 49),
                Size = new Size(35, 27),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnSeleccionarActual.FlatAppearance.BorderSize = 0;
            btnSeleccionarActual.Click += BtnSeleccionarActual_Click;
            grpSistemaActual.Controls.Add(btnSeleccionarActual);

            lstArchivosActual = new ListBox
            {
                Location = new Point(15, 85),
                Size = new Size(480, 65),
                Font = new Font("Consolas", 8F),
                HorizontalScrollbar = true
            };
            grpSistemaActual.Controls.Add(lstArchivosActual);

            // Grupo Sistema Antiguo
            grpSistemaAntiguo = new GroupBox
            {
                Text = "Sistema Antiguo (Referencia)",
                Font = new Font("Segoe UI Semibold", 10F),
                ForeColor = Color.FromArgb(255, 152, 0),
                Location = new Point(570, 10),
                Size = new Size(550, 160)
            };
            pnlSeleccion.Controls.Add(grpSistemaAntiguo);

            Label lblRutaAntiguo = new Label
            {
                Text = "Carpeta de pautas:",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(60, 60, 60),
                Location = new Point(15, 30),
                AutoSize = true
            };
            grpSistemaAntiguo.Controls.Add(lblRutaAntiguo);

            txtRutaAntiguo = new TextBox
            {
                Location = new Point(15, 50),
                Size = new Size(440, 25),
                Font = new Font("Segoe UI", 9F),
                ReadOnly = true,
                BackColor = Color.FromArgb(250, 250, 250)
            };
            grpSistemaAntiguo.Controls.Add(txtRutaAntiguo);

            btnSeleccionarAntiguo = new Button
            {
                Text = "...",
                Location = new Point(460, 49),
                Size = new Size(35, 27),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(255, 152, 0),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnSeleccionarAntiguo.FlatAppearance.BorderSize = 0;
            btnSeleccionarAntiguo.Click += BtnSeleccionarAntiguo_Click;
            grpSistemaAntiguo.Controls.Add(btnSeleccionarAntiguo);

            lstArchivosAntiguo = new ListBox
            {
                Location = new Point(15, 85),
                Size = new Size(480, 65),
                Font = new Font("Consolas", 8F),
                HorizontalScrollbar = true
            };
            grpSistemaAntiguo.Controls.Add(lstArchivosAntiguo);

            // Botones de accion
            btnComparar = new Button
            {
                Text = "Comparar Archivos",
                Location = new Point(20, 245),
                Size = new Size(150, 35),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(63, 81, 181),
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 10F),
                Cursor = Cursors.Hand
            };
            btnComparar.FlatAppearance.BorderSize = 0;
            btnComparar.Click += BtnComparar_Click;
            this.Controls.Add(btnComparar);

            btnLimpiar = new Button
            {
                Text = "Limpiar",
                Location = new Point(180, 245),
                Size = new Size(100, 35),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(158, 158, 158),
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 10F),
                Cursor = Cursors.Hand
            };
            btnLimpiar.FlatAppearance.BorderSize = 0;
            btnLimpiar.Click += BtnLimpiar_Click;
            this.Controls.Add(btnLimpiar);

            // Barra de progreso
            progressBar = new ProgressBar
            {
                Location = new Point(300, 250),
                Size = new Size(400, 25),
                Style = ProgressBarStyle.Continuous,
                Visible = false
            };
            this.Controls.Add(progressBar);

            lblProgreso = new Label
            {
                Text = "",
                Location = new Point(710, 253),
                Size = new Size(300, 20),
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(100, 100, 100),
                Visible = false
            };
            this.Controls.Add(lblProgreso);

            // Panel de resumen
            pnlResumen = new Panel
            {
                Location = new Point(20, 290),
                Size = new Size(1140, 50),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(pnlResumen);

            lblResumenTotal = new Label
            {
                Text = "Total: 0",
                Font = new Font("Segoe UI Semibold", 10F),
                ForeColor = Color.FromArgb(63, 81, 181),
                Location = new Point(20, 15),
                AutoSize = true
            };
            pnlResumen.Controls.Add(lblResumenTotal);

            lblResumenIguales = new Label
            {
                Text = "Iguales: 0",
                Font = new Font("Segoe UI Semibold", 10F),
                ForeColor = Color.FromArgb(76, 175, 80),
                Location = new Point(150, 15),
                AutoSize = true
            };
            pnlResumen.Controls.Add(lblResumenIguales);

            lblResumenDiferentes = new Label
            {
                Text = "Diferentes: 0",
                Font = new Font("Segoe UI Semibold", 10F),
                ForeColor = Color.FromArgb(244, 67, 54),
                Location = new Point(300, 15),
                AutoSize = true
            };
            pnlResumen.Controls.Add(lblResumenDiferentes);

            lblResumenSoloActual = new Label
            {
                Text = "Solo en Actual: 0",
                Font = new Font("Segoe UI Semibold", 10F),
                ForeColor = Color.FromArgb(33, 150, 243),
                Location = new Point(480, 15),
                AutoSize = true
            };
            pnlResumen.Controls.Add(lblResumenSoloActual);

            lblResumenSoloAntiguo = new Label
            {
                Text = "Solo en Antiguo: 0",
                Font = new Font("Segoe UI Semibold", 10F),
                ForeColor = Color.FromArgb(255, 152, 0),
                Location = new Point(680, 15),
                AutoSize = true
            };
            pnlResumen.Controls.Add(lblResumenSoloAntiguo);

            // DataGridView para resultados
            dgvResultados = new DataGridView
            {
                Location = new Point(20, 350),
                Size = new Size(1140, 390),
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
                Font = new Font("Segoe UI", 9F)
            };
            dgvResultados.DefaultCellStyle.SelectionBackColor = Color.FromArgb(63, 81, 181);
            dgvResultados.DefaultCellStyle.SelectionForeColor = Color.White;
            dgvResultados.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(63, 81, 181);
            dgvResultados.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvResultados.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9F);
            dgvResultados.EnableHeadersVisualStyles = false;
            dgvResultados.CellDoubleClick += DgvResultados_CellDoubleClick;
            this.Controls.Add(dgvResultados);

            ConfigurarColumnasResultados();

            // Ajustar controles cuando cambie el tamaño del formulario
            this.Resize += ComparadorPautasForm_Resize;

            this.ResumeLayout(false);
        }

        private void ConfigurarColumnasResultados()
        {
            dgvResultados.Columns.Clear();

            dgvResultados.Columns.Add("Archivo", "Archivo");
            dgvResultados.Columns.Add("Estado", "Estado");
            dgvResultados.Columns.Add("LineasActual", "Lineas Actual");
            dgvResultados.Columns.Add("LineasAntiguo", "Lineas Antiguo");
            dgvResultados.Columns.Add("Diferencias", "Diferencias");
            dgvResultados.Columns.Add("Detalle", "Detalle");

            dgvResultados.Columns["Archivo"].Width = 200;
            dgvResultados.Columns["Estado"].Width = 120;
            dgvResultados.Columns["LineasActual"].Width = 100;
            dgvResultados.Columns["LineasAntiguo"].Width = 100;
            dgvResultados.Columns["Diferencias"].Width = 100;
            dgvResultados.Columns["Detalle"].Width = 400;
        }

        private void ComparadorPautasForm_Resize(object sender, EventArgs e)
        {
            // Ajustar tamaño del DataGridView
            dgvResultados.Width = this.ClientSize.Width - 40;
            dgvResultados.Height = this.ClientSize.Height - dgvResultados.Top - 20;
            pnlResumen.Width = this.ClientSize.Width - 40;
        }

        private void BtnSeleccionarActual_Click(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Seleccione la carpeta de pautas del Sistema Actual";
                dialog.SelectedPath = ConfigManager.ObtenerRutaBasePautas();

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    txtRutaActual.Text = dialog.SelectedPath;
                    CargarArchivos(dialog.SelectedPath, lstArchivosActual);
                }
            }
        }

        private void BtnSeleccionarAntiguo_Click(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Seleccione la carpeta de pautas del Sistema Antiguo";

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    txtRutaAntiguo.Text = dialog.SelectedPath;
                    CargarArchivos(dialog.SelectedPath, lstArchivosAntiguo);
                }
            }
        }

        private void CargarArchivos(string ruta, ListBox listBox)
        {
            listBox.Items.Clear();

            try
            {
                var archivos = Directory.GetFiles(ruta, "*.txt", SearchOption.AllDirectories)
                    .Select(f => f.Substring(ruta.Length).TrimStart('\\', '/'))
                    .OrderBy(f => f)
                    .ToList();

                foreach (var archivo in archivos)
                {
                    listBox.Items.Add(archivo);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar archivos: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void BtnComparar_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtRutaActual.Text) || string.IsNullOrEmpty(txtRutaAntiguo.Text))
            {
                MessageBox.Show("Por favor seleccione ambas carpetas para comparar.",
                    "Carpetas requeridas", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            await CompararArchivosAsync();
        }

        // Clase para almacenar resultados de comparación
        private class ResultadoFila
        {
            public string Archivo { get; set; }
            public string Estado { get; set; }
            public string LineasActual { get; set; }
            public string LineasAntiguo { get; set; }
            public string Diferencias { get; set; }
            public string Detalle { get; set; }
            public Color ColorFila { get; set; }
            public string RutaActual { get; set; }
            public string RutaAntiguo { get; set; }
        }

        private async Task CompararArchivosAsync()
        {
            dgvResultados.Rows.Clear();

            try
            {
                btnComparar.Enabled = false;
                progressBar.Visible = true;
                lblProgreso.Visible = true;
                this.Cursor = Cursors.WaitCursor;

                string rutaActual = txtRutaActual.Text;
                string rutaAntiguo = txtRutaAntiguo.Text;

                lblProgreso.Text = "Escaneando carpetas...";
                Application.DoEvents();

                // Ejecutar comparación en paralelo en segundo plano
                var (resultados, iguales, diferentes, soloActual, soloAntiguo) = await Task.Run(() =>
                {
                    // Obtener archivos de ambas carpetas
                    var archivosActual = ObtenerArchivosRelativos(rutaActual);
                    var archivosAntiguo = ObtenerArchivosRelativos(rutaAntiguo);

                    // Combinar todos los nombres de archivos unicos
                    var todosLosArchivos = archivosActual.Keys
                        .Union(archivosAntiguo.Keys, StringComparer.OrdinalIgnoreCase)
                        .OrderBy(f => f)
                        .ToList();

                    int total = todosLosArchivos.Count;
                    int contIguales = 0;
                    int contDiferentes = 0;
                    int contSoloActual = 0;
                    int contSoloAntiguo = 0;

                    // Procesar en paralelo con Parallel.ForEach
                    var listaResultados = new System.Collections.Concurrent.ConcurrentBag<ResultadoFila>();

                    // Usar paralelismo con límite de threads para no saturar
                    var opciones = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount };

                    Parallel.ForEach(todosLosArchivos, opciones, archivo =>
                    {
                        bool existeEnActual = archivosActual.ContainsKey(archivo);
                        bool existeEnAntiguo = archivosAntiguo.ContainsKey(archivo);

                        var fila = new ResultadoFila
                        {
                            Archivo = archivo,
                            LineasActual = "-",
                            LineasAntiguo = "-",
                            Diferencias = "-",
                            RutaActual = existeEnActual ? archivosActual[archivo] : null,
                            RutaAntiguo = existeEnAntiguo ? archivosAntiguo[archivo] : null
                        };

                        if (existeEnActual && existeEnAntiguo)
                        {
                            // El archivo existe en ambos - comparar contenido
                            var resultado = CompararContenidoRapido(
                                archivosActual[archivo],
                                archivosAntiguo[archivo]);

                            fila.LineasActual = resultado.LineasActual.ToString();
                            fila.LineasAntiguo = resultado.LineasAntiguo.ToString();

                            if (resultado.SonIguales)
                            {
                                fila.Estado = "IGUAL";
                                fila.Diferencias = "0";
                                fila.Detalle = "Contenido identico";
                                fila.ColorFila = Color.FromArgb(232, 245, 233);
                                System.Threading.Interlocked.Increment(ref contIguales);
                            }
                            else
                            {
                                fila.Estado = "DIFERENTE";
                                fila.Diferencias = resultado.TotalDiferencias.ToString();
                                fila.Detalle = resultado.ResumenDiferencias;
                                fila.ColorFila = Color.FromArgb(255, 235, 238);
                                System.Threading.Interlocked.Increment(ref contDiferentes);
                            }
                        }
                        else if (existeEnActual)
                        {
                            fila.Estado = "SOLO ACTUAL";
                            fila.LineasActual = ContarLineasRapido(archivosActual[archivo]).ToString();
                            fila.Detalle = "Archivo solo existe en el sistema actual";
                            fila.ColorFila = Color.FromArgb(227, 242, 253);
                            System.Threading.Interlocked.Increment(ref contSoloActual);
                        }
                        else
                        {
                            fila.Estado = "SOLO ANTIGUO";
                            fila.LineasAntiguo = ContarLineasRapido(archivosAntiguo[archivo]).ToString();
                            fila.Detalle = "Archivo solo existe en el sistema antiguo";
                            fila.ColorFila = Color.FromArgb(255, 243, 224);
                            System.Threading.Interlocked.Increment(ref contSoloAntiguo);
                        }

                        listaResultados.Add(fila);
                    });

                    // Ordenar resultados por nombre de archivo
                    var resultadosOrdenados = listaResultados.OrderBy(r => r.Archivo).ToList();

                    return (resultadosOrdenados, contIguales, contDiferentes, contSoloActual, contSoloAntiguo);
                });

                // Actualizar UI - deshabilitar redibujado durante la carga
                lblProgreso.Text = "Cargando resultados...";
                Application.DoEvents();

                dgvResultados.SuspendLayout();
                try
                {
                    progressBar.Maximum = resultados.Count;
                    progressBar.Value = 0;

                    // Agregar filas en lotes para mejor rendimiento
                    int batchSize = 100;
                    for (int i = 0; i < resultados.Count; i += batchSize)
                    {
                        var batch = resultados.Skip(i).Take(batchSize);
                        foreach (var fila in batch)
                        {
                            int rowIndex = dgvResultados.Rows.Add(
                                fila.Archivo, fila.Estado, fila.LineasActual,
                                fila.LineasAntiguo, fila.Diferencias, fila.Detalle);
                            dgvResultados.Rows[rowIndex].DefaultCellStyle.BackColor = fila.ColorFila;
                            dgvResultados.Rows[rowIndex].Tag = new string[] { fila.RutaActual, fila.RutaAntiguo };
                        }
                        progressBar.Value = Math.Min(i + batchSize, resultados.Count);
                        Application.DoEvents();
                    }
                }
                finally
                {
                    dgvResultados.ResumeLayout();
                }

                // Actualizar resumen
                lblResumenTotal.Text = $"Total: {resultados.Count}";
                lblResumenIguales.Text = $"Iguales: {iguales}";
                lblResumenDiferentes.Text = $"Diferentes: {diferentes}";
                lblResumenSoloActual.Text = $"Solo en Actual: {soloActual}";
                lblResumenSoloAntiguo.Text = $"Solo en Antiguo: {soloAntiguo}";

                lblProgreso.Text = "Comparacion completada";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error durante la comparacion: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnComparar.Enabled = true;
                progressBar.Visible = false;
                lblProgreso.Visible = false;
                this.Cursor = Cursors.Default;
            }
        }

        private Dictionary<string, string> ObtenerArchivosRelativos(string rutaBase)
        {
            var archivos = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var todosTxt = Directory.GetFiles(rutaBase, "*.txt", SearchOption.AllDirectories);

            foreach (var archivo in todosTxt)
            {
                string relativo = archivo.Substring(rutaBase.Length).TrimStart('\\', '/');
                archivos[relativo] = archivo;
            }

            return archivos;
        }

        private class ResultadoComparacion
        {
            public bool SonIguales { get; set; }
            public int LineasActual { get; set; }
            public int LineasAntiguo { get; set; }
            public int TotalDiferencias { get; set; }
            public string ResumenDiferencias { get; set; }
            public List<string> LineasSoloEnActual { get; set; } = new List<string>();
            public List<string> LineasSoloEnAntiguo { get; set; } = new List<string>();
        }

        private ResultadoComparacion CompararContenido(string archivoActual, string archivoAntiguo)
        {
            var resultado = new ResultadoComparacion();

            try
            {
                var lineasActual = File.ReadAllLines(archivoActual)
                    .Where(l => !string.IsNullOrWhiteSpace(l))
                    .Select(l => l.Trim())
                    .ToList();

                var lineasAntiguo = File.ReadAllLines(archivoAntiguo)
                    .Where(l => !string.IsNullOrWhiteSpace(l))
                    .Select(l => l.Trim())
                    .ToList();

                resultado.LineasActual = lineasActual.Count;
                resultado.LineasAntiguo = lineasAntiguo.Count;

                // Comparar como conjuntos (ignorando orden)
                var setActual = new HashSet<string>(lineasActual, StringComparer.OrdinalIgnoreCase);
                var setAntiguo = new HashSet<string>(lineasAntiguo, StringComparer.OrdinalIgnoreCase);

                resultado.LineasSoloEnActual = setActual.Except(setAntiguo, StringComparer.OrdinalIgnoreCase).ToList();
                resultado.LineasSoloEnAntiguo = setAntiguo.Except(setActual, StringComparer.OrdinalIgnoreCase).ToList();

                resultado.TotalDiferencias = resultado.LineasSoloEnActual.Count + resultado.LineasSoloEnAntiguo.Count;
                resultado.SonIguales = resultado.TotalDiferencias == 0;

                if (!resultado.SonIguales)
                {
                    var detalles = new List<string>();
                    if (resultado.LineasSoloEnActual.Count > 0)
                        detalles.Add($"+{resultado.LineasSoloEnActual.Count} en actual");
                    if (resultado.LineasSoloEnAntiguo.Count > 0)
                        detalles.Add($"-{resultado.LineasSoloEnAntiguo.Count} en antiguo");
                    resultado.ResumenDiferencias = string.Join(", ", detalles);
                }
            }
            catch (Exception ex)
            {
                resultado.SonIguales = false;
                resultado.ResumenDiferencias = $"Error: {ex.Message}";
            }

            return resultado;
        }

        /// <summary>
        /// Versión optimizada de comparación para procesamiento paralelo
        /// </summary>
        private ResultadoComparacion CompararContenidoRapido(string archivoActual, string archivoAntiguo)
        {
            var resultado = new ResultadoComparacion();

            try
            {
                // Leer archivos de forma más eficiente
                var setActual = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var setAntiguo = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                // Leer archivo actual
                using (var reader = new StreamReader(archivoActual))
                {
                    string linea;
                    while ((linea = reader.ReadLine()) != null)
                    {
                        if (!string.IsNullOrWhiteSpace(linea))
                        {
                            setActual.Add(linea.Trim());
                        }
                    }
                }

                // Leer archivo antiguo
                using (var reader = new StreamReader(archivoAntiguo))
                {
                    string linea;
                    while ((linea = reader.ReadLine()) != null)
                    {
                        if (!string.IsNullOrWhiteSpace(linea))
                        {
                            setAntiguo.Add(linea.Trim());
                        }
                    }
                }

                resultado.LineasActual = setActual.Count;
                resultado.LineasAntiguo = setAntiguo.Count;

                // Contar diferencias sin crear listas intermedias
                int soloEnActual = 0;
                int soloEnAntiguo = 0;

                foreach (var linea in setActual)
                {
                    if (!setAntiguo.Contains(linea))
                        soloEnActual++;
                }

                foreach (var linea in setAntiguo)
                {
                    if (!setActual.Contains(linea))
                        soloEnAntiguo++;
                }

                resultado.TotalDiferencias = soloEnActual + soloEnAntiguo;
                resultado.SonIguales = resultado.TotalDiferencias == 0;

                if (!resultado.SonIguales)
                {
                    var detalles = new List<string>(2);
                    if (soloEnActual > 0)
                        detalles.Add($"+{soloEnActual} en actual");
                    if (soloEnAntiguo > 0)
                        detalles.Add($"-{soloEnAntiguo} en antiguo");
                    resultado.ResumenDiferencias = string.Join(", ", detalles);
                }
            }
            catch (Exception ex)
            {
                resultado.SonIguales = false;
                resultado.ResumenDiferencias = $"Error: {ex.Message}";
            }

            return resultado;
        }

        /// <summary>
        /// Cuenta líneas de un archivo de forma rápida
        /// </summary>
        private static int ContarLineasRapido(string archivo)
        {
            int count = 0;
            using (var reader = new StreamReader(archivo))
            {
                while (reader.ReadLine() != null)
                    count++;
            }
            return count;
        }

        private void DgvResultados_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            var rutas = dgvResultados.Rows[e.RowIndex].Tag as string[];
            if (rutas == null) return;

            string archivoActual = rutas[0];
            string archivoAntiguo = rutas[1];
            string nombreArchivo = dgvResultados.Rows[e.RowIndex].Cells["Archivo"].Value?.ToString();

            // Mostrar formulario de detalle
            using (var formDetalle = new FormDetalleComparacion(nombreArchivo, archivoActual, archivoAntiguo))
            {
                formDetalle.ShowDialog(this);
            }
        }

        private void BtnLimpiar_Click(object sender, EventArgs e)
        {
            txtRutaActual.Text = "";
            txtRutaAntiguo.Text = "";
            lstArchivosActual.Items.Clear();
            lstArchivosAntiguo.Items.Clear();
            dgvResultados.Rows.Clear();
            lblResumenTotal.Text = "Total: 0";
            lblResumenIguales.Text = "Iguales: 0";
            lblResumenDiferentes.Text = "Diferentes: 0";
            lblResumenSoloActual.Text = "Solo en Actual: 0";
            lblResumenSoloAntiguo.Text = "Solo en Antiguo: 0";
        }
    }

    /// <summary>
    /// Formulario para mostrar el detalle de la comparacion de un archivo especifico.
    /// </summary>
    public class FormDetalleComparacion : Form
    {
        private RichTextBox rtbActual;
        private RichTextBox rtbAntiguo;
        private Label lblTituloActual;
        private Label lblTituloAntiguo;

        public FormDetalleComparacion(string nombreArchivo, string archivoActual, string archivoAntiguo)
        {
            InitializeComponent(nombreArchivo, archivoActual, archivoAntiguo);
        }

        private void InitializeComponent(string nombreArchivo, string archivoActual, string archivoAntiguo)
        {
            this.SuspendLayout();

            this.Text = $"Detalle: {nombreArchivo}";
            this.Size = new Size(1000, 700);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(245, 245, 250);

            // Panel izquierdo - Sistema Actual
            Panel pnlActual = new Panel
            {
                Location = new Point(10, 10),
                Size = new Size(480, 640),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(pnlActual);

            lblTituloActual = new Label
            {
                Text = "Sistema Actual (Nuevo)",
                Font = new Font("Segoe UI Semibold", 11F),
                ForeColor = Color.FromArgb(76, 175, 80),
                Location = new Point(10, 10),
                AutoSize = true
            };
            pnlActual.Controls.Add(lblTituloActual);

            rtbActual = new RichTextBox
            {
                Location = new Point(10, 40),
                Size = new Size(455, 590),
                Font = new Font("Consolas", 9F),
                ReadOnly = true,
                WordWrap = false,
                ScrollBars = RichTextBoxScrollBars.Both
            };
            pnlActual.Controls.Add(rtbActual);

            // Panel derecho - Sistema Antiguo
            Panel pnlAntiguo = new Panel
            {
                Location = new Point(500, 10),
                Size = new Size(480, 640),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(pnlAntiguo);

            lblTituloAntiguo = new Label
            {
                Text = "Sistema Antiguo (Referencia)",
                Font = new Font("Segoe UI Semibold", 11F),
                ForeColor = Color.FromArgb(255, 152, 0),
                Location = new Point(10, 10),
                AutoSize = true
            };
            pnlAntiguo.Controls.Add(lblTituloAntiguo);

            rtbAntiguo = new RichTextBox
            {
                Location = new Point(10, 40),
                Size = new Size(455, 590),
                Font = new Font("Consolas", 9F),
                ReadOnly = true,
                WordWrap = false,
                ScrollBars = RichTextBoxScrollBars.Both
            };
            pnlAntiguo.Controls.Add(rtbAntiguo);

            // Cargar contenido
            CargarContenido(archivoActual, archivoAntiguo);

            this.ResumeLayout(false);
        }

        private void CargarContenido(string archivoActual, string archivoAntiguo)
        {
            HashSet<string> lineasActual = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            HashSet<string> lineasAntiguo = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Cargar contenido del sistema actual
            if (!string.IsNullOrEmpty(archivoActual) && File.Exists(archivoActual))
            {
                var lineas = File.ReadAllLines(archivoActual);
                foreach (var linea in lineas)
                {
                    if (!string.IsNullOrWhiteSpace(linea))
                        lineasActual.Add(linea.Trim());
                }
            }

            // Cargar contenido del sistema antiguo
            if (!string.IsNullOrEmpty(archivoAntiguo) && File.Exists(archivoAntiguo))
            {
                var lineas = File.ReadAllLines(archivoAntiguo);
                foreach (var linea in lineas)
                {
                    if (!string.IsNullOrWhiteSpace(linea))
                        lineasAntiguo.Add(linea.Trim());
                }
            }

            // Mostrar contenido con resaltado
            MostrarContenidoResaltado(rtbActual, archivoActual, lineasAntiguo, true);
            MostrarContenidoResaltado(rtbAntiguo, archivoAntiguo, lineasActual, false);
        }

        private void MostrarContenidoResaltado(RichTextBox rtb, string archivo, HashSet<string> lineasOtro, bool esActual)
        {
            rtb.Clear();

            if (string.IsNullOrEmpty(archivo) || !File.Exists(archivo))
            {
                rtb.Text = "(Archivo no existe)";
                rtb.ForeColor = Color.Gray;
                return;
            }

            var lineas = File.ReadAllLines(archivo);

            foreach (var linea in lineas)
            {
                string lineaTrimmed = linea.Trim();

                if (string.IsNullOrWhiteSpace(lineaTrimmed))
                {
                    rtb.AppendText(linea + "\n");
                    continue;
                }

                bool existeEnOtro = lineasOtro.Contains(lineaTrimmed);

                int start = rtb.TextLength;
                rtb.AppendText(linea + "\n");
                int end = rtb.TextLength;

                rtb.Select(start, end - start);

                if (!existeEnOtro)
                {
                    // Linea que no existe en el otro archivo
                    if (esActual)
                    {
                        rtb.SelectionBackColor = Color.FromArgb(200, 255, 200); // Verde - linea adicional
                    }
                    else
                    {
                        rtb.SelectionBackColor = Color.FromArgb(255, 200, 200); // Rojo - linea faltante
                    }
                }
                else
                {
                    rtb.SelectionBackColor = Color.White;
                }
            }

            rtb.Select(0, 0);
        }
    }
}

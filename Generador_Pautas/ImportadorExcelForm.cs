using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using Npgsql;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Generador_Pautas
{
    public partial class ImportadorExcelForm : Form
    {
        private string archivoExcel;
        private List<TandaImportada> tandasEncontradas = new List<TandaImportada>();
        private DataGridView dgvPreview;
        private Label lblEstado;
        private ProgressBar progressBar;
        private Button btnSeleccionar;
        private Button btnImportar;
        private Button btnCerrar;
        private CheckedListBox chkTandas;
        private Label lblResumen;

        public ImportadorExcelForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Importar Pautas desde Excel/TXT";
            this.Size = new Size(900, 650);
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

            Label lblTitulo = new Label
            {
                Text = "Importador de Pautas desde Excel/TXT",
                Font = new Font("Segoe UI Semibold", 16F),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(15, 15)
            };
            pnlTop.Controls.Add(lblTitulo);

            // Panel de seleccion de archivo
            Panel pnlArchivo = new Panel
            {
                Location = new Point(15, 75),
                Size = new Size(855, 50)
            };

            Label lblArchivo = new Label
            {
                Text = "Archivos:",
                Font = new Font("Segoe UI", 10F),
                Location = new Point(0, 8),
                AutoSize = true
            };

            TextBox txtArchivo = new TextBox
            {
                Name = "txtArchivo",
                Location = new Point(100, 5),
                Size = new Size(600, 30),
                Font = new Font("Segoe UI", 10F),
                ReadOnly = true
            };

            btnSeleccionar = new Button
            {
                Text = "Seleccionar...",
                Location = new Point(710, 3),
                Size = new Size(130, 32),
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            btnSeleccionar.FlatAppearance.BorderSize = 0;
            btnSeleccionar.Click += BtnSeleccionar_Click;

            pnlArchivo.Controls.AddRange(new Control[] { lblArchivo, txtArchivo, btnSeleccionar });

            // Label de resumen
            lblResumen = new Label
            {
                Location = new Point(15, 130),
                Size = new Size(855, 25),
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(100, 100, 100),
                Text = "Seleccione archivos Excel o TXT para analizar..."
            };

            // Lista de tandas encontradas
            Label lblTandas = new Label
            {
                Text = "Tandas encontradas:",
                Location = new Point(15, 160),
                Font = new Font("Segoe UI Semibold", 10F),
                AutoSize = true
            };

            chkTandas = new CheckedListBox
            {
                Location = new Point(15, 185),
                Size = new Size(300, 200),
                Font = new Font("Segoe UI", 9F),
                CheckOnClick = true
            };

            // Preview de datos
            Label lblPreview = new Label
            {
                Text = "Vista previa de comerciales:",
                Location = new Point(330, 160),
                Font = new Font("Segoe UI Semibold", 10F),
                AutoSize = true
            };

            dgvPreview = new DataGridView
            {
                Location = new Point(330, 185),
                Size = new Size(540, 200),
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                RowHeadersVisible = false,
                Font = new Font("Segoe UI", 8.5F)
            };
            dgvPreview.Columns.Add("Hora", "Hora");
            dgvPreview.Columns.Add("Archivo", "Archivo de Audio");
            dgvPreview.Columns["Hora"].Width = 60;
            dgvPreview.Columns["Archivo"].Width = 400;

            chkTandas.SelectedIndexChanged += ChkTandas_SelectedIndexChanged;

            // Progress bar
            progressBar = new ProgressBar
            {
                Location = new Point(15, 400),
                Size = new Size(855, 25),
                Style = ProgressBarStyle.Continuous,
                Visible = false
            };

            // Label de estado
            lblEstado = new Label
            {
                Location = new Point(15, 430),
                Size = new Size(855, 50),
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(80, 80, 80),
                Text = ""
            };

            // Panel de botones
            Panel pnlBotones = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Color.FromArgb(240, 240, 245)
            };

            btnImportar = new Button
            {
                Text = "Importar Seleccionados",
                Size = new Size(180, 40),
                Location = new Point(500, 10),
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Enabled = false
            };
            btnImportar.FlatAppearance.BorderSize = 0;
            btnImportar.Click += BtnImportar_Click;

            btnCerrar = new Button
            {
                Text = "Cerrar",
                Size = new Size(100, 40),
                Location = new Point(690, 10),
                BackColor = Color.FromArgb(158, 158, 158),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            btnCerrar.FlatAppearance.BorderSize = 0;
            btnCerrar.Click += (s, e) => this.Close();

            Button btnSeleccionarTodos = new Button
            {
                Text = "Seleccionar Todos",
                Size = new Size(140, 40),
                Location = new Point(15, 10),
                BackColor = Color.FromArgb(100, 100, 100),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            btnSeleccionarTodos.FlatAppearance.BorderSize = 0;
            btnSeleccionarTodos.Click += (s, e) =>
            {
                for (int i = 0; i < chkTandas.Items.Count; i++)
                    chkTandas.SetItemChecked(i, true);
                ActualizarEstadoBotonImportar();
            };

            Button btnLimpiarImportados = new Button
            {
                Text = "Limpiar Importados",
                Size = new Size(150, 40),
                Location = new Point(165, 10),
                BackColor = Color.FromArgb(244, 67, 54),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            btnLimpiarImportados.FlatAppearance.BorderSize = 0;
            btnLimpiarImportados.Click += BtnLimpiarImportados_Click;

            pnlBotones.Controls.AddRange(new Control[] { btnSeleccionarTodos, btnLimpiarImportados, btnImportar, btnCerrar });

            // Agregar controles al form
            this.Controls.Add(pnlTop);
            this.Controls.Add(pnlArchivo);
            this.Controls.Add(lblResumen);
            this.Controls.Add(lblTandas);
            this.Controls.Add(chkTandas);
            this.Controls.Add(lblPreview);
            this.Controls.Add(dgvPreview);
            this.Controls.Add(progressBar);
            this.Controls.Add(lblEstado);
            this.Controls.Add(pnlBotones);
        }

        private void BtnSeleccionar_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Archivos de Pautas|*.xlsx;*.xlsm;*.xls;*.txt|Archivos Excel|*.xlsx;*.xlsm;*.xls|Archivos TXT|*.txt|Todos los archivos|*.*";
                ofd.Title = "Seleccionar archivos de pautas (puede seleccionar varios)";
                ofd.Multiselect = true; // Permitir seleccion multiple

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    if (ofd.FileNames.Length == 1)
                    {
                        archivoExcel = ofd.FileName;
                        ((TextBox)this.Controls.Find("txtArchivo", true)[0]).Text = archivoExcel;

                        // Detectar tipo de archivo
                        string extension = Path.GetExtension(archivoExcel).ToLower();
                        if (extension == ".txt")
                        {
                            AnalizarArchivosTXT(new[] { archivoExcel });
                        }
                        else
                        {
                            AnalizarArchivoExcel();
                        }
                    }
                    else
                    {
                        // Multiples archivos seleccionados
                        ((TextBox)this.Controls.Find("txtArchivo", true)[0]).Text = $"{ofd.FileNames.Length} archivos seleccionados";

                        // Separar archivos TXT de Excel
                        var archivosTXT = ofd.FileNames.Where(f => Path.GetExtension(f).ToLower() == ".txt").ToArray();
                        var archivosExcel = ofd.FileNames.Where(f => Path.GetExtension(f).ToLower() != ".txt").ToArray();

                        tandasEncontradas.Clear();
                        chkTandas.Items.Clear();
                        dgvPreview.Rows.Clear();

                        // Procesar archivos TXT
                        if (archivosTXT.Length > 0)
                        {
                            AnalizarArchivosTXT(archivosTXT, false); // false = no limpiar lista
                        }

                        // Procesar archivos Excel
                        if (archivosExcel.Length > 0)
                        {
                            AnalizarMultiplesArchivosExcel(archivosExcel, false); // false = no limpiar lista
                        }

                        // Actualizar UI
                        ActualizarListaTandas();
                    }
                }
            }
        }

        private void AnalizarMultiplesArchivosExcel(string[] archivos, bool limpiarLista = true)
        {
            if (limpiarLista)
            {
                tandasEncontradas.Clear();
                chkTandas.Items.Clear();
                dgvPreview.Rows.Clear();
            }

            int archivosLeidos = 0;
            int errores = 0;

            try
            {
                lblEstado.Text = $"Analizando {archivos.Length} archivos Excel...";
                lblEstado.ForeColor = Color.FromArgb(0, 100, 180);
                Application.DoEvents();

                foreach (string archivo in archivos)
                {
                    try
                    {
                        lblEstado.Text = $"Analizando: {Path.GetFileName(archivo)} ({archivosLeidos + 1}/{archivos.Length})";
                        Application.DoEvents();

                        archivoExcel = archivo;
                        AnalizarArchivoExcelInterno(archivo);
                        archivosLeidos++;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error analizando {archivo}: {ex.Message}");
                        errores++;
                    }
                }

                if (limpiarLista)
                {
                    ActualizarListaTandas();
                }

                if (errores > 0)
                {
                    lblEstado.Text = $"Analisis completado con {errores} errores. Seleccione las tandas a importar.";
                    lblEstado.ForeColor = Color.Orange;
                }
                else
                {
                    lblEstado.Text = "Analisis completado. Seleccione las tandas a importar.";
                    lblEstado.ForeColor = Color.FromArgb(76, 175, 80);
                }

                ActualizarEstadoBotonImportar();
            }
            catch (Exception ex)
            {
                lblEstado.Text = $"Error al analizar: {ex.Message}";
                lblEstado.ForeColor = Color.Red;
                System.Diagnostics.Debug.WriteLine($"Error importando multiples Excel: {ex}");
            }
        }

        /// <summary>
        /// Analiza archivos TXT con formato:
        /// Nombre archivo: DD-MM-YYCIUDADRADIO.txt (ej: 06-04-26BARRANCALA KALLE.txt)
        /// Contenido: HH:MM|RutaArchivo (ej: 00:20|C:\LA KALLE\COMERCIALES\archivo.mp3)
        /// </summary>
        private void AnalizarArchivosTXT(string[] archivos, bool limpiarLista = true)
        {
            if (limpiarLista)
            {
                tandasEncontradas.Clear();
                chkTandas.Items.Clear();
                dgvPreview.Rows.Clear();
            }

            int archivosLeidos = 0;
            int errores = 0;

            try
            {
                lblEstado.Text = $"Analizando {archivos.Length} archivos TXT...";
                lblEstado.ForeColor = Color.FromArgb(0, 100, 180);
                Application.DoEvents();

                foreach (string archivo in archivos)
                {
                    try
                    {
                        lblEstado.Text = $"Analizando TXT: {Path.GetFileName(archivo)} ({archivosLeidos + 1}/{archivos.Length})";
                        Application.DoEvents();

                        AnalizarArchivoTXTInterno(archivo);
                        archivosLeidos++;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error analizando TXT {archivo}: {ex.Message}");
                        errores++;
                    }
                }

                if (limpiarLista)
                {
                    ActualizarListaTandas();
                }

                if (errores > 0)
                {
                    lblEstado.Text = $"Analisis TXT completado con {errores} errores. Seleccione las tandas a importar.";
                    lblEstado.ForeColor = Color.Orange;
                }
                else
                {
                    lblEstado.Text = $"Analisis completado: {archivosLeidos} archivos TXT. Seleccione las tandas a importar.";
                    lblEstado.ForeColor = Color.FromArgb(76, 175, 80);
                }

                ActualizarEstadoBotonImportar();
            }
            catch (Exception ex)
            {
                lblEstado.Text = $"Error al analizar TXT: {ex.Message}";
                lblEstado.ForeColor = Color.Red;
                System.Diagnostics.Debug.WriteLine($"Error importando TXT: {ex}");
            }
        }

        /// <summary>
        /// Analiza un archivo TXT individual.
        /// Nombre: DD-MM-YYCIUDADRADIO.txt
        /// Contenido: lineas con formato HH:MM|RutaArchivo
        /// </summary>
        private void AnalizarArchivoTXTInterno(string archivo)
        {
            string nombreArchivo = Path.GetFileNameWithoutExtension(archivo);

            // Parsear el nombre del archivo para obtener fecha, ciudad y radio
            var (fecha, ciudad, radio) = ParsearNombreArchivoTXT(nombreArchivo);

            if (string.IsNullOrEmpty(fecha) || string.IsNullOrEmpty(ciudad))
            {
                System.Diagnostics.Debug.WriteLine($"No se pudo parsear nombre de archivo TXT: {nombreArchivo}");
                throw new Exception($"Formato de nombre incorrecto: {nombreArchivo}. Esperado: DD-MM-YYCIUDADRADIO");
            }

            var tanda = new TandaImportada
            {
                Fecha = fecha,
                Ciudad = ciudad,
                Radio = radio,
                Comerciales = new List<ComercialImportado>()
            };

            // Leer contenido del archivo
            string[] lineas = File.ReadAllLines(archivo, System.Text.Encoding.Default);

            foreach (string linea in lineas)
            {
                // Ignorar lineas vacias o separadores
                if (string.IsNullOrWhiteSpace(linea) || linea.StartsWith("-"))
                    continue;

                // Formato esperado: HH:MM|RutaArchivo
                if (linea.Contains("|"))
                {
                    string[] partes = linea.Split('|');
                    if (partes.Length >= 2)
                    {
                        string hora = partes[0].Trim();
                        string rutaArchivo = partes[1].Trim();

                        if (!string.IsNullOrEmpty(hora) && !string.IsNullOrEmpty(rutaArchivo))
                        {
                            tanda.Comerciales.Add(new ComercialImportado
                            {
                                Hora = hora,
                                RutaArchivo = rutaArchivo,
                                NombreArchivo = Path.GetFileNameWithoutExtension(rutaArchivo)
                            });
                        }
                    }
                }
            }

            if (tanda.Comerciales.Count > 0)
            {
                // Detectar tipo de programacion
                tanda.TipoProgramacion = DetectarTipoProgramacion(tanda.Comerciales);
                System.Diagnostics.Debug.WriteLine($"TXT TANDA: {tanda.Fecha} - {tanda.Ciudad} - {tanda.Radio} ({tanda.Comerciales.Count} comerciales) TipoProg: {tanda.TipoProgramacion}");
                tandasEncontradas.Add(tanda);
            }
        }

        /// <summary>
        /// Parsea el nombre de un archivo TXT para extraer fecha, ciudad y radio.
        /// Formato esperado: DD-MM-YYCIUDADRADIO (ej: 06-04-26BARRANCALA KALLE)
        /// </summary>
        private (string fecha, string ciudad, string radio) ParsearNombreArchivoTXT(string nombreArchivo)
        {
            // Patron: DD-MM-YY seguido de texto
            var match = Regex.Match(nombreArchivo, @"^(\d{2}-\d{2}-\d{2})(.+)$");
            if (!match.Success)
            {
                return ("", "", "");
            }

            string fecha = match.Groups[1].Value;
            string resto = match.Groups[2].Value.Trim();

            // Lista de radios conocidas (ordenadas de mas larga a mas corta para mejor coincidencia)
            string[] radiosConocidas = {
                "RITMO ROMANTICA", "PANAMERICANA", "LA KALLE", "KARIBEÑA", "EXITOSA",
                "NUEVA Q", "LAKALLE", "MODA", "OASIS", "RPP"
            };

            string ciudad = "";
            string radio = "";

            foreach (var r in radiosConocidas)
            {
                if (resto.EndsWith(r, StringComparison.OrdinalIgnoreCase))
                {
                    radio = r;
                    ciudad = resto.Substring(0, resto.Length - r.Length).Trim();
                    break;
                }
            }

            // Si no encontro radio conocida, intentar separar por ultimo espacio
            if (string.IsNullOrEmpty(radio))
            {
                int ultimoEspacio = resto.LastIndexOf(' ');
                if (ultimoEspacio > 0)
                {
                    ciudad = resto.Substring(0, ultimoEspacio).Trim();
                    radio = resto.Substring(ultimoEspacio + 1).Trim();
                }
                else
                {
                    ciudad = resto;
                    radio = "";
                }
            }

            return (fecha, ciudad.ToUpper(), radio.ToUpper());
        }

        /// <summary>
        /// Actualiza la lista de tandas en el CheckedListBox y el resumen
        /// </summary>
        private void ActualizarListaTandas()
        {
            chkTandas.Items.Clear();
            foreach (var tanda in tandasEncontradas)
            {
                string item = $"{tanda.Fecha} - {tanda.Ciudad} - {tanda.Radio} ({tanda.Comerciales.Count} comerciales)";
                chkTandas.Items.Add(item);
            }

            int totalComerciales = tandasEncontradas.Sum(t => t.Comerciales.Count);
            lblResumen.Text = $"Se encontraron {tandasEncontradas.Count} tandas con {totalComerciales} comerciales en total.";
        }

        private void AnalizarArchivoExcelInterno(string archivo)
        {
            // Leer Excel usando OleDb
            DataTable dt = LeerExcelConOleDb(archivo);

            if (dt == null || dt.Rows.Count == 0)
            {
                return;
            }

            TandaImportada tandaActual = null;
            string fechaActual = "";
            string ciudadActual = "";
            string radioActual = "";

            foreach (DataRow row in dt.Rows)
            {
                string celda1 = row[0]?.ToString()?.Trim() ?? "";
                string celda2 = dt.Columns.Count > 1 ? row[1]?.ToString()?.Trim() ?? "" : "";

                // Detectar linea de encabezado de tanda (ej: "01-01-16BARRANCAKARIBEÑA")
                if (EsEncabezadoTanda(celda1))
                {
                    // Guardar tanda anterior si existe
                    if (tandaActual != null && tandaActual.Comerciales.Count > 0)
                    {
                        tandasEncontradas.Add(tandaActual);
                    }

                    // Parsear encabezado
                    var (fecha, ciudad, radio) = ParsearEncabezadoTanda(celda1);
                    fechaActual = fecha;
                    ciudadActual = ciudad;
                    radioActual = radio;

                    tandaActual = new TandaImportada
                    {
                        Fecha = fecha,
                        Ciudad = ciudad,
                        Radio = radio,
                        Comerciales = new List<ComercialImportado>()
                    };
                }
                // Detectar lineas de FECHA:, CIUDAD:, RADIO:
                else if (celda1.StartsWith("FECHA", StringComparison.OrdinalIgnoreCase))
                {
                    var match = Regex.Match(celda1, @"FECHA\s*:\s*(.+)", RegexOptions.IgnoreCase);
                    if (match.Success) fechaActual = match.Groups[1].Value.Trim();
                    if (tandaActual != null) tandaActual.Fecha = fechaActual;
                }
                else if (celda1.StartsWith("CIUDAD", StringComparison.OrdinalIgnoreCase))
                {
                    var match = Regex.Match(celda1, @"CIUDAD\s*:\s*(.+)", RegexOptions.IgnoreCase);
                    if (match.Success) ciudadActual = match.Groups[1].Value.Trim();
                    if (tandaActual != null) tandaActual.Ciudad = ciudadActual;
                }
                else if (celda1.StartsWith("RADIO", StringComparison.OrdinalIgnoreCase))
                {
                    var match = Regex.Match(celda1, @"RADIO\s*:\s*(.+)", RegexOptions.IgnoreCase);
                    if (match.Success) radioActual = match.Groups[1].Value.Trim();
                    if (tandaActual != null) tandaActual.Radio = radioActual;
                }
                // Detectar linea de comercial (hora + ruta)
                else if (EsLineaComercial(celda1, celda2))
                {
                    if (tandaActual == null)
                    {
                        tandaActual = new TandaImportada
                        {
                            Fecha = fechaActual,
                            Ciudad = ciudadActual,
                            Radio = radioActual,
                            Comerciales = new List<ComercialImportado>()
                        };
                    }

                    string hora = "";
                    string rutaArchivo = "";

                    // El formato puede ser: Hora en col1, Ruta en col2
                    if (!string.IsNullOrEmpty(celda2) && celda2.Contains(":\\"))
                    {
                        hora = celda1;
                        rutaArchivo = celda2;
                    }
                    else if (celda1.Contains("\t"))
                    {
                        var partes = celda1.Split('\t');
                        hora = partes[0].Trim();
                        rutaArchivo = partes.Length > 1 ? partes[1].Trim() : "";
                    }
                    else if (Regex.IsMatch(celda1, @"^\d{1,2}:\d{2}"))
                    {
                        var match = Regex.Match(celda1, @"^(\d{1,2}:\d{2})\s+(.+)$");
                        if (match.Success)
                        {
                            hora = match.Groups[1].Value;
                            rutaArchivo = match.Groups[2].Value;
                        }
                        else
                        {
                            hora = celda1;
                            rutaArchivo = celda2;
                        }
                    }

                    if (!string.IsNullOrEmpty(hora) && !string.IsNullOrEmpty(rutaArchivo))
                    {
                        tandaActual.Comerciales.Add(new ComercialImportado
                        {
                            Hora = hora,
                            RutaArchivo = rutaArchivo,
                            NombreArchivo = Path.GetFileNameWithoutExtension(rutaArchivo)
                        });
                    }
                }
            }

            // Agregar ultima tanda
            if (tandaActual != null && tandaActual.Comerciales.Count > 0)
            {
                tandasEncontradas.Add(tandaActual);
            }

            // Detectar tipo de programacion para las tandas recien agregadas
            foreach (var tanda in tandasEncontradas.Where(t => string.IsNullOrEmpty(t.TipoProgramacion) || t.TipoProgramacion == "Cada 00-30"))
            {
                tanda.TipoProgramacion = DetectarTipoProgramacion(tanda.Comerciales);
                System.Diagnostics.Debug.WriteLine($"TANDA DETECTADA: {tanda.Ciudad} - {tanda.Radio} -> TipoProgramacion: {tanda.TipoProgramacion}");
            }
        }

        private void AnalizarArchivoExcel()
        {
            tandasEncontradas.Clear();
            chkTandas.Items.Clear();
            dgvPreview.Rows.Clear();

            try
            {
                lblEstado.Text = "Analizando archivo Excel...";
                lblEstado.ForeColor = Color.FromArgb(0, 100, 180);
                Application.DoEvents();

                // Usar el metodo interno para analizar
                AnalizarArchivoExcelInterno(archivoExcel);

                if (tandasEncontradas.Count == 0)
                {
                    lblEstado.Text = "El archivo esta vacio o no se pudo leer.";
                    lblEstado.ForeColor = Color.Red;
                    return;
                }

                // Mostrar tandas encontradas
                foreach (var tanda in tandasEncontradas)
                {
                    string item = $"{tanda.Fecha} - {tanda.Ciudad} - {tanda.Radio} ({tanda.Comerciales.Count} comerciales)";
                    chkTandas.Items.Add(item);
                }

                int totalComerciales = tandasEncontradas.Sum(t => t.Comerciales.Count);
                lblResumen.Text = $"Se encontraron {tandasEncontradas.Count} tandas con {totalComerciales} comerciales en total.";
                lblEstado.Text = "Analisis completado. Seleccione las tandas a importar.";
                lblEstado.ForeColor = Color.FromArgb(76, 175, 80);

                ActualizarEstadoBotonImportar();
            }
            catch (Exception ex)
            {
                lblEstado.Text = $"Error al analizar: {ex.Message}";
                lblEstado.ForeColor = Color.Red;
                System.Diagnostics.Debug.WriteLine($"Error importando Excel: {ex}");
            }
        }

        private DataTable LeerExcelConOleDb(string archivo)
        {
            DataTable dt = new DataTable();

            // Determinar el proveedor segun la extension
            string extension = Path.GetExtension(archivo).ToLower();
            string connectionString;

            if (extension == ".xlsx" || extension == ".xlsm")
            {
                // Excel 2007+ (archivos .xlsx/.xlsm)
                connectionString = $"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={archivo};Extended Properties=\"Excel 12.0 Xml;HDR=NO;IMEX=1\"";
            }
            else
            {
                // Excel 97-2003 (archivos .xls)
                connectionString = $"Provider=Microsoft.Jet.OLEDB.4.0;Data Source={archivo};Extended Properties=\"Excel 8.0;HDR=NO;IMEX=1\"";
            }

            try
            {
                using (OleDbConnection conn = new OleDbConnection(connectionString))
                {
                    conn.Open();

                    // Obtener el nombre de la primera hoja
                    DataTable schemaTable = conn.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null);
                    if (schemaTable == null || schemaTable.Rows.Count == 0)
                    {
                        throw new Exception("No se encontraron hojas en el archivo Excel.");
                    }

                    string nombreHoja = schemaTable.Rows[0]["TABLE_NAME"].ToString();

                    // Leer datos de la hoja
                    string query = $"SELECT * FROM [{nombreHoja}]";
                    using (OleDbDataAdapter adapter = new OleDbDataAdapter(query, conn))
                    {
                        adapter.Fill(dt);
                    }
                }
            }
            catch (Exception ex)
            {
                // Si falla OleDb, intentar con el otro proveedor
                System.Diagnostics.Debug.WriteLine($"Error con OleDb: {ex.Message}. Intentando proveedor alternativo...");

                try
                {
                    // Intentar con el proveedor alternativo
                    if (extension == ".xlsx" || extension == ".xlsm")
                    {
                        connectionString = $"Provider=Microsoft.Jet.OLEDB.4.0;Data Source={archivo};Extended Properties=\"Excel 8.0;HDR=NO;IMEX=1\"";
                    }
                    else
                    {
                        connectionString = $"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={archivo};Extended Properties=\"Excel 12.0;HDR=NO;IMEX=1\"";
                    }

                    using (OleDbConnection conn = new OleDbConnection(connectionString))
                    {
                        conn.Open();
                        DataTable schemaTable = conn.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null);
                        string nombreHoja = schemaTable.Rows[0]["TABLE_NAME"].ToString();
                        string query = $"SELECT * FROM [{nombreHoja}]";
                        using (OleDbDataAdapter adapter = new OleDbDataAdapter(query, conn))
                        {
                            adapter.Fill(dt);
                        }
                    }
                }
                catch (Exception ex2)
                {
                    throw new Exception($"No se pudo leer el archivo Excel. Asegurese de tener instalado Microsoft Access Database Engine.\n\nError: {ex2.Message}");
                }
            }

            return dt;
        }

        private bool EsEncabezadoTanda(string texto)
        {
            if (string.IsNullOrEmpty(texto)) return false;
            // Detectar patron como "01-01-16BARRANCAKARIBEÑA"
            return Regex.IsMatch(texto, @"^\d{2}-\d{2}-\d{2}[A-Z]", RegexOptions.IgnoreCase);
        }

        private (string fecha, string ciudad, string radio) ParsearEncabezadoTanda(string texto)
        {
            var match = Regex.Match(texto, @"^(\d{2}-\d{2}-\d{2})(.+)$");
            if (!match.Success) return ("", "", "");

            string fecha = match.Groups[1].Value;
            string resto = match.Groups[2].Value;

            // Buscar radios conocidas
            string[] radiosConocidas = { "KARIBEÑA", "LA KALLE", "EXITOSA", "NUEVA Q", "MODA", "RITMO ROMANTICA", "OASIS", "PANAMERICANA", "RPP" };
            string ciudad = "";
            string radio = "";

            foreach (var r in radiosConocidas)
            {
                if (resto.EndsWith(r, StringComparison.OrdinalIgnoreCase))
                {
                    radio = r;
                    ciudad = resto.Substring(0, resto.Length - r.Length).Trim();
                    break;
                }
            }

            if (string.IsNullOrEmpty(radio))
            {
                ciudad = resto;
            }

            return (fecha, ciudad.ToUpper(), radio.ToUpper());
        }

        private bool EsLineaComercial(string celda1, string celda2)
        {
            if (string.IsNullOrEmpty(celda1)) return false;
            bool tieneHora = Regex.IsMatch(celda1, @"^\d{1,2}:\d{2}");
            bool tieneRuta = celda1.Contains(":\\") || celda2?.Contains(":\\") == true;
            return tieneHora || tieneRuta;
        }

        private void ChkTandas_SelectedIndexChanged(object sender, EventArgs e)
        {
            dgvPreview.Rows.Clear();

            if (chkTandas.SelectedIndex >= 0 && chkTandas.SelectedIndex < tandasEncontradas.Count)
            {
                var tanda = tandasEncontradas[chkTandas.SelectedIndex];
                foreach (var comercial in tanda.Comerciales.Take(50))
                {
                    dgvPreview.Rows.Add(comercial.Hora, comercial.RutaArchivo);
                }
            }

            ActualizarEstadoBotonImportar();
        }

        private void ActualizarEstadoBotonImportar()
        {
            btnImportar.Enabled = chkTandas.CheckedItems.Count > 0;
        }

        private async void BtnImportar_Click(object sender, EventArgs e)
        {
            if (chkTandas.CheckedItems.Count == 0)
            {
                MessageBox.Show("Seleccione al menos una tanda para importar.", "Aviso",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var resultado = MessageBox.Show(
                $"Se importaran {chkTandas.CheckedItems.Count} tandas a la base de datos.\n\n" +
                "Esto creara nuevos registros de comerciales.\n\n" +
                "¿Desea continuar?",
                "Confirmar Importacion",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (resultado != DialogResult.Yes) return;

            btnImportar.Enabled = false;
            btnSeleccionar.Enabled = false;
            progressBar.Visible = true;
            progressBar.Value = 0;

            int comercialesCreados = 0;  // Registros en tabla Comerciales
            int asignacionesCreadas = 0; // Registros en tabla ComercialesAsignados (por hora)
            int errores = 0;

            try
            {
                var tandasSeleccionadas = new List<TandaImportada>();
                for (int i = 0; i < chkTandas.Items.Count; i++)
                {
                    if (chkTandas.GetItemChecked(i))
                    {
                        tandasSeleccionadas.Add(tandasEncontradas[i]);
                    }
                }

                int totalComerciales = tandasSeleccionadas.Sum(t => t.Comerciales.Count);
                int procesados = 0;

                using (var conn = new NpgsqlConnection(DatabaseConfig.ConnectionString))
                {
                    await conn.OpenAsync();

                    // Asegurar que la columna TipoProgramacion exista (migracion)
                    try
                    {
                        using (var cmdMigration = new NpgsqlCommand(
                            "ALTER TABLE Comerciales ADD COLUMN TipoProgramacion TEXT DEFAULT 'Cada 00-30'", conn))
                        {
                            await cmdMigration.ExecuteNonQueryAsync();
                            System.Diagnostics.Debug.WriteLine("Columna TipoProgramacion agregada a la tabla Comerciales");
                        }
                    }
                    catch (PostgresException)
                    {
                        // La columna ya existe, ignorar
                    }

                    foreach (var tanda in tandasSeleccionadas)
                    {
                        lblEstado.Text = $"Importando: {tanda.Ciudad} - {tanda.Radio}...";
                        Application.DoEvents();

                        DateTime fechaTanda;
                        if (!DateTime.TryParseExact(tanda.Fecha, new[] { "dd-MM-yy", "dd/MM/yy", "dd-MM-yyyy", "dd/MM/yyyy" },
                            System.Globalization.CultureInfo.InvariantCulture,
                            System.Globalization.DateTimeStyles.None, out fechaTanda))
                        {
                            fechaTanda = DateTime.Today;
                        }

                        // Agrupar comerciales por archivo
                        var comercialesAgrupados = tanda.Comerciales
                            .GroupBy(c => c.RutaArchivo.ToLower())
                            .ToList();

                        int posicion = 1;
                        foreach (var grupo in comercialesAgrupados)
                        {
                            var primerComercial = grupo.First();
                            string codigo = await GenerarCodigoUnicoAsync(conn);

                            try
                            {
                                string insertComercial = @"INSERT INTO Comerciales
                                    (Codigo, FilePath, FechaInicio, FechaFinal, Ciudad, Radio, Posicion, Estado, TipoProgramacion)
                                    VALUES (@Codigo, @FilePath, @FechaInicio, @FechaFinal, @Ciudad, @Radio, @Posicion, 'Activo', @TipoProgramacion)";

                                using (var cmd = new NpgsqlCommand(insertComercial, conn))
                                {
                                    cmd.Parameters.AddWithValue("@Codigo", codigo);
                                    cmd.Parameters.AddWithValue("@FilePath", primerComercial.RutaArchivo);
                                    cmd.Parameters.AddWithValue("@FechaInicio", NpgsqlTypes.NpgsqlDbType.Timestamp, fechaTanda);
                                    cmd.Parameters.AddWithValue("@FechaFinal", NpgsqlTypes.NpgsqlDbType.Timestamp, fechaTanda);
                                    cmd.Parameters.AddWithValue("@Ciudad", tanda.Ciudad);
                                    cmd.Parameters.AddWithValue("@Radio", tanda.Radio);
                                    cmd.Parameters.AddWithValue("@Posicion", $"{posicion:D2}");
                                    cmd.Parameters.AddWithValue("@TipoProgramacion", tanda.TipoProgramacion);

                                    await cmd.ExecuteNonQueryAsync();
                                }

                                // Agrupar por hora para evitar duplicados del mismo archivo en la misma hora
                                var horasUnicas = grupo.Select(c => c.Hora).Distinct().ToList();

                                foreach (var hora in horasUnicas)
                                {
                                    int fila = ObtenerFilaDesdeHora(hora, tanda.TipoProgramacion);
                                    System.Diagnostics.Debug.WriteLine($"IMPORT - Hora: {hora} -> Fila calculada: {fila} (TipoProgramacion: {tanda.TipoProgramacion})");

                                    if (fila >= 0)
                                    {
                                        string nombreMostrar = $"P{posicion:D2} {primerComercial.NombreArchivo}";

                                        string insertAsignacion = @"INSERT INTO ComercialesAsignados
                                            (Codigo, Fila, Columna, ComercialAsignado)
                                            VALUES (@Codigo, @Fila, @Columna, @ComercialAsignado)";

                                        using (var cmdAsig = new NpgsqlCommand(insertAsignacion, conn))
                                        {
                                            cmdAsig.Parameters.AddWithValue("@Codigo", codigo);
                                            cmdAsig.Parameters.AddWithValue("@Fila", fila);
                                            cmdAsig.Parameters.AddWithValue("@Columna", 2);
                                            cmdAsig.Parameters.AddWithValue("@ComercialAsignado", nombreMostrar);

                                            await cmdAsig.ExecuteNonQueryAsync();
                                            asignacionesCreadas++;
                                            System.Diagnostics.Debug.WriteLine($"IMPORT - Insertado: Codigo={codigo}, Fila={fila}, Comercial={nombreMostrar}");
                                        }
                                    }
                                }

                                comercialesCreados++;
                                posicion++;
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Error importando comercial {primerComercial.NombreArchivo}: {ex.Message}");
                                errores++;
                            }

                            procesados += grupo.Count();
                            progressBar.Value = (int)((procesados * 100.0) / totalComerciales);
                            Application.DoEvents();
                        }
                    }
                }

                progressBar.Value = 100;
                lblEstado.Text = $"Importacion completada: {comercialesCreados} comerciales, {asignacionesCreadas} asignaciones horarias.";
                lblEstado.ForeColor = errores > 0 ? Color.Orange : Color.FromArgb(76, 175, 80);

                MessageBox.Show(
                    $"Importacion completada:\n\n" +
                    $"- Archivos de audio (comerciales): {comercialesCreados}\n" +
                    $"- Asignaciones horarias: {asignacionesCreadas}\n" +
                    $"- Errores: {errores}",
                    "Resultado",
                    MessageBoxButtons.OK,
                    errores > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);

                ConfigManager.NotificarCambioEnBD();
            }
            catch (Exception ex)
            {
                lblEstado.Text = $"Error durante importacion: {ex.Message}";
                lblEstado.ForeColor = Color.Red;
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnImportar.Enabled = true;
                btnSeleccionar.Enabled = true;
                progressBar.Visible = false;
            }
        }

        private async System.Threading.Tasks.Task<string> GenerarCodigoUnicoAsync(NpgsqlConnection conn)
        {
            string query = "SELECT MAX(CAST(Codigo AS INTEGER)) FROM Comerciales WHERE Codigo ~ '^[0-9]+$'";
            using (var cmd = new NpgsqlCommand(query, conn))
            {
                var result = await cmd.ExecuteScalarAsync();
                int ultimo = result != DBNull.Value && result != null ? Convert.ToInt32(result) : 0;
                return (ultimo + 1).ToString("D4");
            }
        }

        /// <summary>
        /// Convierte una hora del Excel a la fila correspondiente en el DataGridView.
        /// Usa el TipoProgramacion de la tanda para determinar el mapeo correcto.
        /// Maneja tanto formato texto "07:20" como numero decimal de Excel (0.30555...)
        /// </summary>
        private int ObtenerFilaDesdeHora(string hora, string tipoProgramacion = null)
        {
            if (string.IsNullOrEmpty(hora)) return -1;

            int horas, minutos;

            // Primero intentar parsear como formato HH:MM
            var match = Regex.Match(hora, @"(\d{1,2}):(\d{2})");
            if (match.Success)
            {
                horas = int.Parse(match.Groups[1].Value);
                minutos = int.Parse(match.Groups[2].Value);
                System.Diagnostics.Debug.WriteLine($"HORA PARSED (HH:MM): {hora} -> {horas:D2}:{minutos:D2}");
            }
            // Si no es formato HH:MM, intentar como numero decimal de Excel
            else if (double.TryParse(hora, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out double valorDecimal))
            {
                // Excel almacena horas como fraccion de dia (0.0 a 1.0)
                // Convertir a horas y minutos
                double totalHoras = valorDecimal * 24;
                horas = (int)totalHoras;
                minutos = (int)Math.Round((totalHoras - horas) * 60);

                // Ajustar si los minutos redondean a 60
                if (minutos >= 60)
                {
                    minutos = 0;
                    horas++;
                }
                System.Diagnostics.Debug.WriteLine($"HORA PARSED (DECIMAL): {hora} -> {valorDecimal} * 24 = {totalHoras} -> {horas:D2}:{minutos:D2}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"HORA PARSE FAILED: {hora}");
                return -1;
            }

            // Usar TandasHorarias para obtener la fila correcta segun el tipo de programacion
            TipoTanda tipoTanda = TandasHorarias.GetTipoTandaFromString(tipoProgramacion ?? "Cada 00-30");
            int fila = TandasHorarias.GetFilaParaHoraMinutos(horas, minutos, tipoTanda);

            if (fila >= 0)
            {
                System.Diagnostics.Debug.WriteLine($"HORA -> FILA: {horas:D2}:{minutos:D2} -> Fila {fila} (TipoProgramacion: {tipoProgramacion})");
                return fila;
            }

            // Fallback: calcular fila manualmente si no se encuentra en el tipo de tanda
            // Para tandas de 4 por hora (00-20-30-50)
            if (tipoProgramacion != null && tipoProgramacion.Contains("00-20-30-50"))
            {
                // 4 filas por hora: 00=0, 20=1, 30=2, 50=3
                int offset = 0;
                if (minutos == 0) offset = 0;
                else if (minutos == 20) offset = 1;
                else if (minutos == 30) offset = 2;
                else if (minutos == 50) offset = 3;
                fila = (horas * 4) + offset;
            }
            else
            {
                // 2 filas por hora (comportamiento original)
                if (minutos == 50)
                    fila = (horas * 2) + 1;
                else if (minutos >= 30)
                    fila = (horas * 2) + 1;
                else
                    fila = (horas * 2);
            }

            System.Diagnostics.Debug.WriteLine($"HORA -> FILA (fallback): {horas:D2}:{minutos:D2} -> Fila {fila}");
            return fila;
        }

        /// <summary>
        /// Detecta el tipo de programacion basandose en los minutos de las horas de los comerciales.
        /// </summary>
        private string DetectarTipoProgramacion(List<ComercialImportado> comerciales)
        {
            // Contar los minutos mas frecuentes
            var contadorMinutos = new Dictionary<int, int>();

            foreach (var comercial in comerciales)
            {
                int minutos = -1;
                string hora = comercial.Hora ?? "";

                // Primero intentar formato HH:MM
                var match = Regex.Match(hora, @"\d{1,2}:(\d{2})");
                if (match.Success)
                {
                    minutos = int.Parse(match.Groups[1].Value);
                }
                // Si no es HH:MM, intentar formato decimal de Excel
                else if (double.TryParse(hora, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out double valorDecimal))
                {
                    double totalHoras = valorDecimal * 24;
                    int horaEntera = (int)totalHoras;
                    minutos = (int)Math.Round((totalHoras - horaEntera) * 60);
                    if (minutos >= 60) minutos = 0;
                }

                if (minutos >= 0)
                {
                    if (!contadorMinutos.ContainsKey(minutos))
                        contadorMinutos[minutos] = 0;
                    contadorMinutos[minutos]++;
                }
            }

            if (contadorMinutos.Count == 0)
                return "Cada 00-30";

            // Detectar el patron basandose en los minutos encontrados
            bool tiene00 = contadorMinutos.ContainsKey(0);
            bool tiene10 = contadorMinutos.ContainsKey(10);
            bool tiene15 = contadorMinutos.ContainsKey(15);
            bool tiene20 = contadorMinutos.ContainsKey(20);
            bool tiene30 = contadorMinutos.ContainsKey(30);
            bool tiene40 = contadorMinutos.ContainsKey(40);
            bool tiene45 = contadorMinutos.ContainsKey(45);
            bool tiene50 = contadorMinutos.ContainsKey(50);

            // Tandas 00-20-30-50 (4 por hora) - detectar si tiene los 4 tipos
            if ((tiene00 || tiene30) && (tiene20 || tiene50))
            {
                // Tiene minutos de ambos tipos (00-30 y 20-50), es formato mixto
                return "Cada 00-20-30-50";
            }

            // Tandas 20-50
            if ((tiene20 || tiene50) && !tiene00 && !tiene30)
                return "Cada 20-50";

            // Tandas 10-40
            if ((tiene10 || tiene40) && !tiene00 && !tiene30 && !tiene20 && !tiene50)
                return "Cada 10-40";

            // Tandas 15-45
            if ((tiene15 || tiene45) && !tiene00 && !tiene30)
                return "Cada 15-45";

            // Por defecto: Tandas 00-30
            return "Cada 00-30";
        }

        private async void BtnLimpiarImportados_Click(object sender, EventArgs e)
        {
            var resultado = MessageBox.Show(
                "¿Qué desea hacer?\n\n" +
                "SI = Eliminar TODOS los comerciales\n" +
                "NO = Solo eliminar DUPLICADOS (mantiene 1 de cada uno)",
                "Limpiar Base de Datos",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question);

            if (resultado == DialogResult.Cancel) return;

            try
            {
                using (var conn = new NpgsqlConnection(DatabaseConfig.ConnectionString))
                {
                    await conn.OpenAsync();

                    if (resultado == DialogResult.Yes)
                    {
                        // Eliminar TODO
                        lblEstado.Text = "Eliminando todos los comerciales...";
                        lblEstado.ForeColor = Color.FromArgb(244, 67, 54);
                        Application.DoEvents();

                        using (var cmd = new NpgsqlCommand("DELETE FROM ComercialesAsignados", conn))
                        {
                            await cmd.ExecuteNonQueryAsync();
                        }

                        using (var cmd = new NpgsqlCommand("DELETE FROM Comerciales", conn))
                        {
                            await cmd.ExecuteNonQueryAsync();
                        }

                        lblEstado.Text = "Todos los comerciales han sido eliminados.";
                        lblEstado.ForeColor = Color.FromArgb(76, 175, 80);

                        MessageBox.Show(
                            "Todos los comerciales han sido eliminados de la base de datos.",
                            "Eliminacion Completada",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                    }
                    else
                    {
                        // Solo eliminar DUPLICADOS
                        lblEstado.Text = "Eliminando duplicados...";
                        lblEstado.ForeColor = Color.FromArgb(255, 152, 0);
                        Application.DoEvents();

                        // Eliminar duplicados en ComercialesAsignados manteniendo solo el primer registro
                        string deleteDuplicados = @"
                            DELETE FROM ComercialesAsignados
                            WHERE ctid NOT IN (
                                SELECT MIN(ctid)
                                FROM ComercialesAsignados
                                GROUP BY Codigo, Fila, Columna
                            )";

                        int eliminados = 0;
                        using (var cmd = new NpgsqlCommand(deleteDuplicados, conn))
                        {
                            eliminados = await cmd.ExecuteNonQueryAsync();
                        }

                        lblEstado.Text = $"Se eliminaron {eliminados} registros duplicados.";
                        lblEstado.ForeColor = Color.FromArgb(76, 175, 80);

                        MessageBox.Show(
                            $"Se eliminaron {eliminados} registros duplicados.\n\n" +
                            "Ahora cada comercial aparece solo 1 vez por hora.",
                            "Duplicados Eliminados",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                    }
                }

                ConfigManager.NotificarCambioEnBD();
            }
            catch (Exception ex)
            {
                lblEstado.Text = $"Error: {ex.Message}";
                lblEstado.ForeColor = Color.Red;
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    public class TandaImportada
    {
        public string Fecha { get; set; }
        public string Ciudad { get; set; }
        public string Radio { get; set; }
        public string TipoProgramacion { get; set; } = "Cada 00-30";
        public List<ComercialImportado> Comerciales { get; set; } = new List<ComercialImportado>();
    }

    public class ComercialImportado
    {
        public string Hora { get; set; }
        public string RutaArchivo { get; set; }
        public string NombreArchivo { get; set; }
    }
}

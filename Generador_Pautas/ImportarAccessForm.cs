using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Npgsql;

namespace Generador_Pautas
{
    public class ImportarAccessForm : Form
    {
        private TextBox txtRutaAccess;
        private Button btnExaminar;
        private Button btnAnalizar;
        private Button btnImportar;
        private RichTextBox txtReporte;
        private ProgressBar progressBar;
        private Label lblEstado;
        private DataGridView dgvTablas;
        private CheckedListBox chkTablas;
        private Label lblInfo;

        private string _rutaAccess;
        private AccessAnalyzer _analyzer;
        private Dictionary<string, int> _tablasRegistros = new Dictionary<string, int>();

        public ImportarAccessForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Importar Base de Datos Access";
            this.Size = new Size(1000, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(240, 240, 245);

            // Panel superior - Selección de archivo
            Panel pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Color.FromArgb(63, 81, 181),
                Padding = new Padding(15)
            };

            Label lblTitulo = new Label
            {
                Text = "Importar desde Microsoft Access",
                Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(15, 18)
            };
            pnlTop.Controls.Add(lblTitulo);
            this.Controls.Add(pnlTop);

            // Panel de selección de archivo
            Panel pnlArchivo = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                Padding = new Padding(15, 10, 15, 5)
            };

            Label lblRuta = new Label
            {
                Text = "Archivo Access (.mdb/.accdb):",
                AutoSize = true,
                Location = new Point(15, 18),
                Font = new Font("Segoe UI", 9F)
            };

            txtRutaAccess = new TextBox
            {
                Location = new Point(200, 15),
                Width = 550,
                Font = new Font("Segoe UI", 9F),
                ReadOnly = true
            };

            btnExaminar = new Button
            {
                Text = "Examinar...",
                Location = new Point(760, 13),
                Width = 90,
                Height = 28,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(63, 81, 181),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F),
                Cursor = Cursors.Hand
            };
            btnExaminar.FlatAppearance.BorderSize = 0;
            btnExaminar.Click += BtnExaminar_Click;

            btnAnalizar = new Button
            {
                Text = "Analizar",
                Location = new Point(860, 13),
                Width = 100,
                Height = 28,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Enabled = false
            };
            btnAnalizar.FlatAppearance.BorderSize = 0;
            btnAnalizar.Click += BtnAnalizar_Click;

            Button btnAnalisisDetallado = new Button
            {
                Text = "Análisis Detallado",
                Location = new Point(760, 13),
                Width = 90,
                Height = 28,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(156, 39, 176),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 8F),
                Cursor = Cursors.Hand,
                Enabled = false
            };
            btnAnalisisDetallado.FlatAppearance.BorderSize = 0;
            btnAnalisisDetallado.Click += async (s, ev) =>
            {
                if (_analyzer == null) return;
                btnAnalisisDetallado.Enabled = false;
                lblEstado.Text = "Generando análisis detallado...";
                progressBar.Style = ProgressBarStyle.Marquee;
                try
                {
                    string reporte = await Task.Run(() => _analyzer.GenerarAnalisisDetallado());
                    txtReporte.Text = reporte;
                    lblEstado.Text = "Análisis detallado completado.";
                }
                catch (Exception ex)
                {
                    txtReporte.Text = $"ERROR: {ex.Message}";
                }
                finally
                {
                    btnAnalisisDetallado.Enabled = true;
                    progressBar.Style = ProgressBarStyle.Continuous;
                    progressBar.Value = 0;
                }
            };

            // Ajustar posiciones de botones
            btnExaminar.Location = new Point(660, 13);
            btnAnalizar.Location = new Point(860, 13);

            // Habilitar botón de análisis detallado cuando se seleccione archivo
            txtRutaAccess.TextChanged += (s, ev) =>
            {
                btnAnalisisDetallado.Enabled = !string.IsNullOrEmpty(txtRutaAccess.Text) && File.Exists(txtRutaAccess.Text);
            };

            pnlArchivo.Controls.AddRange(new Control[] { lblRuta, txtRutaAccess, btnExaminar, btnAnalisisDetallado, btnAnalizar });
            this.Controls.Add(pnlArchivo);

            // Panel principal - Split (usamos TableLayoutPanel para evitar problemas de SplitterDistance)
            TableLayoutPanel splitMain = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(5)
            };
            splitMain.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 250F));
            splitMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            // Panel izquierdo - Lista de tablas
            Label lblTablas = new Label
            {
                Text = "Tablas a importar:",
                Dock = DockStyle.Top,
                Height = 25,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Padding = new Padding(5, 5, 0, 0)
            };

            chkTablas = new CheckedListBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9F),
                CheckOnClick = true,
                BorderStyle = BorderStyle.FixedSingle
            };

            // Panel izquierdo contenedor
            Panel pnlIzquierdo = new Panel { Dock = DockStyle.Fill };
            pnlIzquierdo.Controls.Add(chkTablas);
            pnlIzquierdo.Controls.Add(lblTablas);
            splitMain.Controls.Add(pnlIzquierdo, 0, 0);

            // Panel derecho - Reporte
            Label lblReporte = new Label
            {
                Text = "Análisis de estructura:",
                Dock = DockStyle.Top,
                Height = 25,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Padding = new Padding(5, 5, 0, 0)
            };

            txtReporte = new RichTextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 9F),
                ReadOnly = true,
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.FromArgb(220, 220, 220),
                BorderStyle = BorderStyle.None,
                WordWrap = false
            };

            // Panel derecho contenedor
            Panel pnlDerecho = new Panel { Dock = DockStyle.Fill };
            pnlDerecho.Controls.Add(txtReporte);
            pnlDerecho.Controls.Add(lblReporte);
            splitMain.Controls.Add(pnlDerecho, 1, 0);

            this.Controls.Add(splitMain);

            // Panel inferior - Progreso y botones
            Panel pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 80,
                BackColor = Color.FromArgb(235, 235, 240),
                Padding = new Padding(15)
            };

            lblEstado = new Label
            {
                Text = "Seleccione un archivo Access para comenzar...",
                AutoSize = true,
                Location = new Point(15, 10),
                Font = new Font("Segoe UI", 9F)
            };

            progressBar = new ProgressBar
            {
                Location = new Point(15, 35),
                Width = 750,
                Height = 25,
                Style = ProgressBarStyle.Continuous
            };

            btnImportar = new Button
            {
                Text = "Importar Seleccionados",
                Location = new Point(780, 30),
                Width = 180,
                Height = 35,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(255, 152, 0),
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Enabled = false
            };
            btnImportar.FlatAppearance.BorderSize = 0;
            btnImportar.Click += BtnImportar_Click;

            pnlBottom.Controls.AddRange(new Control[] { lblEstado, progressBar, btnImportar });
            this.Controls.Add(pnlBottom);

            // Precargar ruta conocida
            string rutaConocida = @"C:\Users\USUARIO\Desktop\sistema pauta\data\PAUTA.mdb";
            if (File.Exists(rutaConocida))
            {
                txtRutaAccess.Text = rutaConocida;
                _rutaAccess = rutaConocida;
                btnAnalizar.Enabled = true;
            }
        }

        private void BtnExaminar_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Access Database|*.mdb;*.accdb|All files|*.*";
                ofd.Title = "Seleccionar base de datos Access";

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    txtRutaAccess.Text = ofd.FileName;
                    _rutaAccess = ofd.FileName;
                    btnAnalizar.Enabled = true;
                    chkTablas.Items.Clear();
                    txtReporte.Clear();
                    btnImportar.Enabled = false;
                }
            }
        }

        private async void BtnAnalizar_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_rutaAccess) || !File.Exists(_rutaAccess))
            {
                MessageBox.Show("Seleccione un archivo Access válido.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                btnAnalizar.Enabled = false;
                lblEstado.Text = "Analizando base de datos...";
                progressBar.Style = ProgressBarStyle.Marquee;
                Application.DoEvents();

                _analyzer = new AccessAnalyzer(_rutaAccess);

                // Ejecutar análisis en segundo plano
                string reporte = await Task.Run(() => _analyzer.GenerarReporteCompleto());

                // Mostrar reporte
                txtReporte.Text = reporte;

                // Cargar lista de tablas
                chkTablas.Items.Clear();
                _tablasRegistros.Clear();

                var tablas = await Task.Run(() => _analyzer.ObtenerTablas());
                foreach (string tabla in tablas)
                {
                    int registros = await Task.Run(() => _analyzer.ContarRegistros(tabla));
                    _tablasRegistros[tabla] = registros;
                    chkTablas.Items.Add($"{tabla} ({registros:N0} registros)", true);
                }

                lblEstado.Text = $"Análisis completado. {tablas.Count} tablas encontradas.";
                btnImportar.Enabled = chkTablas.Items.Count > 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al analizar la base de datos:\n\n{ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtReporte.Text = $"ERROR: {ex.Message}\n\n{ex.StackTrace}";
                lblEstado.Text = "Error en el análisis.";
            }
            finally
            {
                btnAnalizar.Enabled = true;
                progressBar.Style = ProgressBarStyle.Continuous;
                progressBar.Value = 0;
            }
        }

        private async void BtnImportar_Click(object sender, EventArgs e)
        {
            // Preguntar si quiere limpiar la base de datos primero
            DialogResult limpiar = MessageBox.Show(
                "¿Desea LIMPIAR la base de datos PostgreSQL antes de importar?\n\n" +
                "• SI - Eliminará TODOS los comerciales y pautas existentes\n" +
                "• NO - Agregará los nuevos registros sin borrar los existentes\n" +
                "• Cancelar - No hace nada",
                "Limpiar Base de Datos",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Warning);

            if (limpiar == DialogResult.Cancel)
                return;

            try
            {
                btnImportar.Enabled = false;
                btnAnalizar.Enabled = false;
                progressBar.Value = 0;
                progressBar.Style = ProgressBarStyle.Continuous;

                // Limpiar base de datos si el usuario lo pidió
                if (limpiar == DialogResult.Yes)
                {
                    lblEstado.Text = "Limpiando base de datos PostgreSQL...";
                    Application.DoEvents();
                    await LimpiarBaseDatosAsync();
                    txtReporte.AppendText("\n✓ Base de datos limpiada correctamente\n");
                }

                // Importar directamente la tabla AVISOS
                lblEstado.Text = "Iniciando importación de AVISOS...";
                Application.DoEvents();

                int totalImportados = await ImportarAvisosAccessAsync();

                progressBar.Value = 100;
                lblEstado.Text = $"Importación completada. {totalImportados:N0} registros importados.";

                MessageBox.Show(
                    $"Importación completada exitosamente.\n\n" +
                    $"Total de registros importados: {totalImportados:N0}",
                    "Importación Completa",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error durante la importación:\n\n{ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtReporte.AppendText($"\n\n❌ ERROR: {ex.Message}");
                lblEstado.Text = "Error en la importación.";
            }
            finally
            {
                btnImportar.Enabled = true;
                btnAnalizar.Enabled = true;
            }
        }

        /// <summary>
        /// Limpia todas las tablas de la base de datos PostgreSQL
        /// </summary>
        private async Task LimpiarBaseDatosAsync()
        {
            string connString = ConfigManager.ObtenerPostgreSQLConnectionString();

            using (var conn = new NpgsqlConnection(connString))
            {
                await conn.OpenAsync();

                // Primero eliminar ComercialesAsignados (tiene FK a Comerciales)
                using (var cmd = new NpgsqlCommand("DELETE FROM ComercialesAsignados", conn))
                {
                    await cmd.ExecuteNonQueryAsync();
                }

                // Luego eliminar Comerciales
                using (var cmd = new NpgsqlCommand("DELETE FROM Comerciales", conn))
                {
                    await cmd.ExecuteNonQueryAsync();
                }

                txtReporte.AppendText("  - ComercialesAsignados: limpiado\n");
                txtReporte.AppendText("  - Comerciales: limpiado\n");
            }
        }

        /// <summary>
        /// Importa la tabla AVISOS de Access directamente a PostgreSQL
        /// con progreso en tiempo real
        /// </summary>
        private async Task<int> ImportarAvisosAccessAsync()
        {
            int importados = 0;
            int errores = 0;
            string connStringPg = ConfigManager.ObtenerPostgreSQLConnectionString();
            string connStringAccess = $"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={_rutaAccess};Persist Security Info=False;";

            // Primero contar total de registros
            int totalRegistros = 0;
            using (var connAccess = new OleDbConnection(connStringAccess))
            {
                await Task.Run(() => connAccess.Open());
                using (var cmd = new OleDbCommand("SELECT COUNT(*) FROM AVISOS", connAccess))
                {
                    totalRegistros = Convert.ToInt32(cmd.ExecuteScalar());
                }
            }

            txtReporte.AppendText($"\n▶ Iniciando importación de {totalRegistros:N0} registros...\n");

            // Diccionario para agrupar por Codigo_avi (un comercial único)
            var comercialesImportados = new Dictionary<string, string>(); // Codigo_avi -> Codigo PostgreSQL

            using (var connAccess = new OleDbConnection(connStringAccess))
            using (var connPg = new NpgsqlConnection(connStringPg))
            {
                await Task.Run(() => connAccess.Open());
                await connPg.OpenAsync();

                // PASO 1: Importar todas las ciudades únicas de Access a la tabla Ciudades
                lblEstado.Text = "Importando ciudades...";
                Application.DoEvents();
                var ciudadesImportadas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                using (var cmdCiudades = new OleDbCommand("SELECT DISTINCT Ciudad FROM AVISOS WHERE Ciudad IS NOT NULL", connAccess))
                using (var readerCiudades = cmdCiudades.ExecuteReader())
                {
                    while (readerCiudades.Read())
                    {
                        string ciudad = readerCiudades["Ciudad"]?.ToString()?.Trim() ?? "";
                        if (!string.IsNullOrEmpty(ciudad) && !ciudadesImportadas.Contains(ciudad))
                        {
                            ciudadesImportadas.Add(ciudad);
                            // Insertar en PostgreSQL si no existe
                            string insertCiudad = "INSERT INTO Ciudades (Nombre, Estado) VALUES (@Nombre, 'Activo') ON CONFLICT (Nombre) DO NOTHING";
                            using (var cmdInsertCiudad = new NpgsqlCommand(insertCiudad, connPg))
                            {
                                cmdInsertCiudad.Parameters.AddWithValue("@Nombre", ciudad);
                                await cmdInsertCiudad.ExecuteNonQueryAsync();
                            }
                        }
                    }
                }
                txtReporte.AppendText($"  ✓ Ciudades importadas: {ciudadesImportadas.Count}\n");

                // PASO 2: Importar todas las radios únicas de Access a la tabla Radios
                lblEstado.Text = "Importando radios...";
                Application.DoEvents();
                var radiosImportadas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                using (var cmdRadios = new OleDbCommand("SELECT DISTINCT Medio FROM AVISOS WHERE Medio IS NOT NULL", connAccess))
                using (var readerRadios = cmdRadios.ExecuteReader())
                {
                    while (readerRadios.Read())
                    {
                        string radio = readerRadios["Medio"]?.ToString()?.Trim() ?? "";
                        if (!string.IsNullOrEmpty(radio) && !radiosImportadas.Contains(radio))
                        {
                            radiosImportadas.Add(radio);
                            // Insertar en PostgreSQL si no existe
                            string insertRadio = "INSERT INTO Radios (Nombre, Estado) VALUES (@Nombre, 'Activo') ON CONFLICT (Nombre) DO NOTHING";
                            using (var cmdInsertRadio = new NpgsqlCommand(insertRadio, connPg))
                            {
                                cmdInsertRadio.Parameters.AddWithValue("@Nombre", radio);
                                await cmdInsertRadio.ExecuteNonQueryAsync();
                            }
                        }
                    }
                }
                txtReporte.AppendText($"  ✓ Radios importadas: {radiosImportadas.Count}\n");

                // PASO 3: Leer todos los registros de Access
                string query = "SELECT * FROM AVISOS ORDER BY Codigo_avi, Ciudad, Medio, FechaI";
                using (var cmdAccess = new OleDbCommand(query, connAccess))
                using (var reader = cmdAccess.ExecuteReader())
                {
                    int contador = 0;
                    int batchSize = 1000;
                    DateTime ultimaActualizacion = DateTime.Now;

                    while (reader.Read())
                    {
                        try
                        {
                            // Extraer datos de Access
                            string codigoAvi = reader["Codigo_avi"]?.ToString() ?? "";
                            string ciudad = reader["Ciudad"]?.ToString()?.Trim() ?? "";
                            string medio = reader["Medio"]?.ToString()?.Trim() ?? "";
                            string ruta = reader["Ruta"]?.ToString()?.Trim() ?? "";
                            string hora = reader["Hora"]?.ToString()?.Trim() ?? "";
                            int pos = Convert.ToInt32(reader["Pos"]);
                            DateTime fechaI = Convert.ToDateTime(reader["FechaI"]);
                            DateTime fechaF = Convert.ToDateTime(reader["FechaF"]);
                            bool caduco = Convert.ToBoolean(reader["CADUCO"]);

                            // Días de la semana
                            bool lun = Convert.ToBoolean(reader["lun"]);
                            bool mar = Convert.ToBoolean(reader["mar"]);
                            bool mie = Convert.ToBoolean(reader["mie"]);
                            bool jue = Convert.ToBoolean(reader["jue"]);
                            bool vie = Convert.ToBoolean(reader["vie"]);
                            bool sab = Convert.ToBoolean(reader["sab"]);
                            bool dom = Convert.ToBoolean(reader["dom"]);

                            if (string.IsNullOrEmpty(ruta))
                            {
                                contador++;
                                continue;
                            }

                            // Crear clave única para el comercial - INCLUIR HORA para que cada hora sea un registro separado
                            string claveComercial = $"{codigoAvi}|{ciudad}|{medio}|{hora}|{fechaI:yyyyMMdd}|{fechaF:yyyyMMdd}";
                            string codigoPg;

                            // Detectar tipo de tanda - primero por hora, luego por radio
                            TipoTanda tipoTanda = DetectarTipoTandaPorHora(hora);
                            // Si la hora es genérica (00, 30), verificar si la radio usa 4 tandas
                            if (tipoTanda == TipoTanda.Tandas_00_30)
                            {
                                TipoTanda tandaPorRadio = DetectarTipoTandaPorRadio(medio);
                                if (tandaPorRadio == TipoTanda.Tandas_00_20_30_50)
                                {
                                    tipoTanda = tandaPorRadio;
                                }
                            }

                            // Si este comercial ya fue importado, usar el mismo código
                            if (!comercialesImportados.TryGetValue(claveComercial, out codigoPg))
                            {
                                // Crear nuevo comercial en PostgreSQL - incluir hora en el código para hacerlo único
                                string horaLimpia = hora.Replace(":", "");
                                codigoPg = $"ACC-{codigoAvi}-{ciudad.Replace(" ", "").Substring(0, Math.Min(3, ciudad.Length))}-{medio.Replace(" ", "").Substring(0, Math.Min(3, medio.Length))}-{horaLimpia}";

                                // Normalizar posición
                                string posicion = $"P{pos.ToString().PadLeft(2, '0')}";
                                string estado = caduco ? "Inactivo" : "Activo";

                                // Obtener el texto del tipo de programación
                                string tipoProgramacion = ObtenerTipoProgramacionTexto(tipoTanda);

                                // Insertar comercial
                                string insertComercial = @"
                                    INSERT INTO Comerciales (Codigo, FilePath, FechaInicio, FechaFinal, Ciudad, Radio, Posicion, Estado, TipoProgramacion)
                                    VALUES (@Codigo, @FilePath, @FechaInicio, @FechaFinal, @Ciudad, @Radio, @Posicion, @Estado, @TipoProgramacion)
                                    ON CONFLICT (Codigo) DO NOTHING";

                                using (var cmdPg = new NpgsqlCommand(insertComercial, connPg))
                                {
                                    cmdPg.Parameters.AddWithValue("@Codigo", codigoPg);
                                    cmdPg.Parameters.AddWithValue("@FilePath", ruta);
                                    cmdPg.Parameters.AddWithValue("@FechaInicio", fechaI);
                                    cmdPg.Parameters.AddWithValue("@FechaFinal", fechaF);
                                    cmdPg.Parameters.AddWithValue("@Ciudad", ciudad);
                                    cmdPg.Parameters.AddWithValue("@Radio", medio);
                                    cmdPg.Parameters.AddWithValue("@Posicion", posicion);
                                    cmdPg.Parameters.AddWithValue("@Estado", estado);
                                    cmdPg.Parameters.AddWithValue("@TipoProgramacion", tipoProgramacion);

                                    await cmdPg.ExecuteNonQueryAsync();
                                }

                                comercialesImportados[claveComercial] = codigoPg;
                                importados++;
                            }

                            // NO insertamos en ComercialesAsignados durante la importación inicial
                            // porque los comerciales ACC tienen la hora embebida en el código
                            // y las fechas se generan dinámicamente desde FechaInicio-FechaFinal
                            // Esto es más eficiente y consistente con el resto del sistema

                            contador++;

                            // Actualizar progreso cada batchSize registros
                            if (contador % batchSize == 0)
                            {
                                int porcentaje = (contador * 100) / totalRegistros;
                                progressBar.Value = Math.Min(porcentaje, 99);
                                lblEstado.Text = $"Importando... {contador:N0}/{totalRegistros:N0} ({porcentaje}%) - {importados:N0} comerciales creados";
                                Application.DoEvents();
                            }
                        }
                        catch (Exception ex)
                        {
                            errores++;
                            if (errores <= 10)
                            {
                                txtReporte.AppendText($"\n⚠ Error fila {importados}: {ex.Message}");
                            }
                        }
                    }
                }
            }

            txtReporte.AppendText($"\n\n═══════════════════════════════════════════════════════════════");
            txtReporte.AppendText($"\n✓ Comerciales importados: {importados:N0}");
            txtReporte.AppendText($"\n✓ Total registros procesados: {totalRegistros:N0}");
            if (errores > 0)
                txtReporte.AppendText($"\n⚠ Errores: {errores:N0}");
            txtReporte.AppendText($"\n═══════════════════════════════════════════════════════════════\n");

            return importados;
        }

        /// <summary>
        /// Convierte una hora string (ej: "06:00") a índice de fila según el tipo de tanda
        /// </summary>
        private int ConvertirHoraAFila(string hora, TipoTanda tipoTanda)
        {
            // Usar TandasHorarias para obtener el índice correcto
            int fila = TandasHorarias.GetFilaParaHora(hora, tipoTanda);
            if (fila >= 0)
                return fila;

            // Si no se encuentra en el tipo de tanda, intentar calcularlo
            if (TimeSpan.TryParse(hora, out TimeSpan ts))
            {
                int horas = ts.Hours;
                int minutos = ts.Minutes;

                // Determinar la fila según el tipo de tanda
                switch (tipoTanda)
                {
                    case TipoTanda.Tandas_00_30:
                        // 2 tandas por hora: 00 y 30
                        return horas * 2 + (minutos >= 30 ? 1 : 0);

                    case TipoTanda.Tandas_20_50:
                        // 2 tandas por hora: 20 y 50
                        return horas * 2 + (minutos >= 50 ? 1 : 0);

                    case TipoTanda.Tandas_10_40:
                        // 2 tandas por hora: 10 y 40
                        return horas * 2 + (minutos >= 40 ? 1 : 0);

                    case TipoTanda.Tandas_15_45:
                        // 2 tandas por hora: 15 y 45
                        return horas * 2 + (minutos >= 45 ? 1 : 0);

                    case TipoTanda.Tandas_00_20_30_50:
                        // 4 tandas por hora: 00, 20, 30, 50
                        if (minutos < 20) return horas * 4 + 0;
                        else if (minutos < 30) return horas * 4 + 1;
                        else if (minutos < 50) return horas * 4 + 2;
                        else return horas * 4 + 3;
                }
            }

            return 0;
        }

        /// <summary>
        /// Detecta el tipo de tanda según la radio/medio
        /// </summary>
        private TipoTanda DetectarTipoTandaPorRadio(string radio)
        {
            if (string.IsNullOrEmpty(radio))
                return TipoTanda.Tandas_00_30;

            string radioUpper = radio.ToUpper();

            // KARIBEÑA y LA KALLE usan 4 tandas: 00, 20, 30, 50
            if (radioUpper.Contains("KARIBEÑA") || radioUpper.Contains("KARIBENA") ||
                radioUpper.Contains("KARIBEÃ") || radioUpper.Contains("KARIBE") ||
                radioUpper.Contains("LAKALLE") || radioUpper.Contains("LA KALLE") || radioUpper.Contains("KALLE"))
            {
                return TipoTanda.Tandas_00_20_30_50;
            }

            // Por defecto usar 00-30 (EXITOSA y otras)
            return TipoTanda.Tandas_00_30;
        }

        /// <summary>
        /// Detecta el tipo de tanda según la hora exacta del comercial
        /// </summary>
        private TipoTanda DetectarTipoTandaPorHora(string hora)
        {
            if (string.IsNullOrEmpty(hora) || !hora.Contains(":"))
                return TipoTanda.Tandas_00_30;

            string[] partes = hora.Split(':');
            if (partes.Length < 2 || !int.TryParse(partes[1], out int minutos))
                return TipoTanda.Tandas_00_30;

            // Detectar por los minutos
            switch (minutos)
            {
                case 0:
                case 30:
                    return TipoTanda.Tandas_00_30;
                case 20:
                case 50:
                    return TipoTanda.Tandas_20_50;
                case 10:
                case 40:
                    return TipoTanda.Tandas_10_40;
                case 15:
                case 45:
                    return TipoTanda.Tandas_15_45;
                default:
                    return TipoTanda.Tandas_00_30;
            }
        }

        /// <summary>
        /// Obtiene el texto del tipo de programación para guardar en la BD
        /// </summary>
        private string ObtenerTipoProgramacionTexto(TipoTanda tipoTanda)
        {
            switch (tipoTanda)
            {
                case TipoTanda.Tandas_00_30:
                    return "Cada 00-30";
                case TipoTanda.Tandas_20_50:
                    return "Cada 20-50";
                case TipoTanda.Tandas_10_40:
                    return "Cada 10-40";
                case TipoTanda.Tandas_15_45:
                    return "Cada 15-45";
                case TipoTanda.Tandas_00_20_30_50:
                    return "Cada 00-20-30-50";
                default:
                    return "Cada 00-30";
            }
        }

        /// <summary>
        /// Importa una tabla de Access a PostgreSQL
        /// </summary>
        private async Task<int> ImportarTablaAsync(string tableName)
        {
            int importados = 0;

            // Obtener datos de Access
            DataTable datos = await Task.Run(() => _analyzer.ObtenerMuestraDatos(tableName, int.MaxValue));

            if (datos.Rows.Count == 0)
                return 0;

            // Mapear según el nombre de la tabla
            switch (tableName.ToUpper())
            {
                case "SPOT":
                case "SPOTS":
                case "COMERCIALES":
                    importados = await ImportarSpotAsync(datos);
                    break;
                case "PAUTAS":
                case "PAUTA":
                case "PROGRAMACION":
                    importados = await ImportarPautasAsync(datos);
                    break;
                case "CIUDAD":
                case "CIUDADES":
                    importados = await ImportarCiudadesAsync(datos);
                    break;
                case "RADIO":
                case "RADIOS":
                case "ESTACION":
                case "ESTACIONES":
                    importados = await ImportarRadiosAsync(datos);
                    break;
                default:
                    // Intentar detectar por columnas
                    if (datos.Columns.Contains("FilePath") || datos.Columns.Contains("ARCHIVO") || datos.Columns.Contains("RUTA"))
                    {
                        importados = await ImportarSpotGenericoAsync(datos);
                    }
                    break;
            }

            return importados;
        }

        /// <summary>
        /// Importa spots/comerciales desde Access
        /// </summary>
        private async Task<int> ImportarSpotAsync(DataTable datos)
        {
            int importados = 0;
            string connString = PostgreSQLMigration.ConnectionString;

            using (var conn = new NpgsqlConnection(connString))
            {
                await conn.OpenAsync();

                foreach (DataRow row in datos.Rows)
                {
                    try
                    {
                        // Mapear columnas (intentar varios nombres posibles)
                        string codigo = ObtenerValorColumna(row, "Codigo", "CODIGO", "ID", "COD");
                        string filePath = ObtenerValorColumna(row, "FilePath", "FILEPATH", "ARCHIVO", "RUTA", "PATH");
                        string ciudad = ObtenerValorColumna(row, "Ciudad", "CIUDAD", "CITY");
                        string radio = ObtenerValorColumna(row, "Radio", "RADIO", "ESTACION", "STATION");
                        string posicion = ObtenerValorColumna(row, "Posicion", "POSICION", "POS", "PRIORIDAD");
                        string estado = ObtenerValorColumna(row, "Estado", "ESTADO", "STATUS", "ACTIVO");

                        DateTime fechaInicio = ObtenerFechaColumna(row, "FechaInicio", "FECHAINICIO", "FECHA_INICIO", "INICIO", "DESDE");
                        DateTime fechaFinal = ObtenerFechaColumna(row, "FechaFinal", "FECHAFINAL", "FECHA_FINAL", "FIN", "HASTA");

                        // Generar código si no existe
                        if (string.IsNullOrEmpty(codigo))
                        {
                            codigo = $"IMP-{DateTime.Now:yyyyMMddHHmmss}-{importados}";
                        }

                        // Validar FilePath
                        if (string.IsNullOrEmpty(filePath))
                            continue;

                        // Normalizar estado
                        if (string.IsNullOrEmpty(estado) || estado == "1" || estado.ToUpper() == "TRUE" || estado.ToUpper() == "SI")
                            estado = "Activo";
                        else if (estado == "0" || estado.ToUpper() == "FALSE" || estado.ToUpper() == "NO")
                            estado = "Inactivo";

                        // Normalizar posición
                        if (string.IsNullOrEmpty(posicion))
                            posicion = "P01";
                        else if (!posicion.StartsWith("P"))
                            posicion = $"P{posicion.PadLeft(2, '0')}";

                        // Verificar si ya existe
                        string checkQuery = "SELECT COUNT(*) FROM Comerciales WHERE Codigo = @Codigo";
                        using (var checkCmd = new NpgsqlCommand(checkQuery, conn))
                        {
                            checkCmd.Parameters.AddWithValue("@Codigo", codigo);
                            long count = (long)await checkCmd.ExecuteScalarAsync();
                            if (count > 0)
                                continue; // Ya existe, saltar
                        }

                        // Insertar en PostgreSQL
                        string insertQuery = @"
                            INSERT INTO Comerciales (Codigo, FilePath, FechaInicio, FechaFinal, Ciudad, Radio, Posicion, Estado, TipoProgramacion)
                            VALUES (@Codigo, @FilePath, @FechaInicio, @FechaFinal, @Ciudad, @Radio, @Posicion, @Estado, @TipoProgramacion)";

                        using (var cmd = new NpgsqlCommand(insertQuery, conn))
                        {
                            cmd.Parameters.AddWithValue("@Codigo", codigo);
                            cmd.Parameters.AddWithValue("@FilePath", filePath);
                            cmd.Parameters.AddWithValue("@FechaInicio", fechaInicio);
                            cmd.Parameters.AddWithValue("@FechaFinal", fechaFinal);
                            cmd.Parameters.AddWithValue("@Ciudad", ciudad ?? "");
                            cmd.Parameters.AddWithValue("@Radio", radio ?? "");
                            cmd.Parameters.AddWithValue("@Posicion", posicion);
                            cmd.Parameters.AddWithValue("@Estado", estado);
                            cmd.Parameters.AddWithValue("@TipoProgramacion", "Cada 00-30");

                            await cmd.ExecuteNonQueryAsync();
                            importados++;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error importando fila: {ex.Message}");
                    }
                }
            }

            return importados;
        }

        /// <summary>
        /// Importa spots con detección genérica de columnas
        /// </summary>
        private async Task<int> ImportarSpotGenericoAsync(DataTable datos)
        {
            return await ImportarSpotAsync(datos);
        }

        /// <summary>
        /// Importa pautas/programación
        /// </summary>
        private async Task<int> ImportarPautasAsync(DataTable datos)
        {
            int importados = 0;
            string connString = PostgreSQLMigration.ConnectionString;

            using (var conn = new NpgsqlConnection(connString))
            {
                await conn.OpenAsync();

                foreach (DataRow row in datos.Rows)
                {
                    try
                    {
                        string codigo = ObtenerValorColumna(row, "Codigo", "CODIGO", "ID_SPOT", "SPOT_ID");
                        int fila = ObtenerIntColumna(row, "Fila", "FILA", "ROW", "HORA_INDEX");
                        int columna = ObtenerIntColumna(row, "Columna", "COLUMNA", "COL", "DIA_INDEX");
                        string comercialAsignado = ObtenerValorColumna(row, "ComercialAsignado", "COMERCIAL", "SPOT", "NOMBRE");

                        if (string.IsNullOrEmpty(codigo))
                            continue;

                        // Verificar si ya existe
                        string checkQuery = "SELECT COUNT(*) FROM ComercialesAsignados WHERE Codigo = @Codigo AND Fila = @Fila AND Columna = @Columna";
                        using (var checkCmd = new NpgsqlCommand(checkQuery, conn))
                        {
                            checkCmd.Parameters.AddWithValue("@Codigo", codigo);
                            checkCmd.Parameters.AddWithValue("@Fila", fila);
                            checkCmd.Parameters.AddWithValue("@Columna", columna);
                            long count = (long)await checkCmd.ExecuteScalarAsync();
                            if (count > 0)
                                continue;
                        }

                        string insertQuery = @"
                            INSERT INTO ComercialesAsignados (Codigo, Fila, Columna, ComercialAsignado)
                            VALUES (@Codigo, @Fila, @Columna, @ComercialAsignado)";

                        using (var cmd = new NpgsqlCommand(insertQuery, conn))
                        {
                            cmd.Parameters.AddWithValue("@Codigo", codigo);
                            cmd.Parameters.AddWithValue("@Fila", fila);
                            cmd.Parameters.AddWithValue("@Columna", columna);
                            cmd.Parameters.AddWithValue("@ComercialAsignado", comercialAsignado ?? "");

                            await cmd.ExecuteNonQueryAsync();
                            importados++;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error importando pauta: {ex.Message}");
                    }
                }
            }

            return importados;
        }

        /// <summary>
        /// Importa ciudades
        /// </summary>
        private async Task<int> ImportarCiudadesAsync(DataTable datos)
        {
            int importados = 0;
            string connString = PostgreSQLMigration.ConnectionString;

            using (var conn = new NpgsqlConnection(connString))
            {
                await conn.OpenAsync();

                foreach (DataRow row in datos.Rows)
                {
                    try
                    {
                        string nombre = ObtenerValorColumna(row, "Nombre", "NOMBRE", "CIUDAD", "NAME");

                        if (string.IsNullOrEmpty(nombre))
                            continue;

                        // Verificar si ya existe
                        string checkQuery = "SELECT COUNT(*) FROM Ciudades WHERE Nombre = @Nombre";
                        using (var checkCmd = new NpgsqlCommand(checkQuery, conn))
                        {
                            checkCmd.Parameters.AddWithValue("@Nombre", nombre);
                            long count = (long)await checkCmd.ExecuteScalarAsync();
                            if (count > 0)
                                continue;
                        }

                        string insertQuery = "INSERT INTO Ciudades (Nombre, Activo) VALUES (@Nombre, true)";
                        using (var cmd = new NpgsqlCommand(insertQuery, conn))
                        {
                            cmd.Parameters.AddWithValue("@Nombre", nombre);
                            await cmd.ExecuteNonQueryAsync();
                            importados++;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[ImportarCiudades] Error: {ex.Message}");
                    }
                }
            }

            return importados;
        }

        /// <summary>
        /// Importa radios/estaciones
        /// </summary>
        private async Task<int> ImportarRadiosAsync(DataTable datos)
        {
            int importados = 0;
            string connString = PostgreSQLMigration.ConnectionString;

            using (var conn = new NpgsqlConnection(connString))
            {
                await conn.OpenAsync();

                foreach (DataRow row in datos.Rows)
                {
                    try
                    {
                        string nombre = ObtenerValorColumna(row, "Nombre", "NOMBRE", "RADIO", "ESTACION", "NAME");

                        if (string.IsNullOrEmpty(nombre))
                            continue;

                        // Verificar si ya existe
                        string checkQuery = "SELECT COUNT(*) FROM Radios WHERE Nombre = @Nombre";
                        using (var checkCmd = new NpgsqlCommand(checkQuery, conn))
                        {
                            checkCmd.Parameters.AddWithValue("@Nombre", nombre);
                            long count = (long)await checkCmd.ExecuteScalarAsync();
                            if (count > 0)
                                continue;
                        }

                        string insertQuery = "INSERT INTO Radios (Nombre, Activo) VALUES (@Nombre, true)";
                        using (var cmd = new NpgsqlCommand(insertQuery, conn))
                        {
                            cmd.Parameters.AddWithValue("@Nombre", nombre);
                            await cmd.ExecuteNonQueryAsync();
                            importados++;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[ImportarRadios] Error: {ex.Message}");
                    }
                }
            }

            return importados;
        }

        // Métodos auxiliares para obtener valores de columnas con nombres alternativos

        private string ObtenerValorColumna(DataRow row, params string[] posiblesNombres)
        {
            foreach (string nombre in posiblesNombres)
            {
                if (row.Table.Columns.Contains(nombre) && row[nombre] != DBNull.Value)
                {
                    return row[nombre].ToString().Trim();
                }
            }
            return null;
        }

        private int ObtenerIntColumna(DataRow row, params string[] posiblesNombres)
        {
            foreach (string nombre in posiblesNombres)
            {
                if (row.Table.Columns.Contains(nombre) && row[nombre] != DBNull.Value)
                {
                    if (int.TryParse(row[nombre].ToString(), out int valor))
                        return valor;
                }
            }
            return 0;
        }

        private DateTime ObtenerFechaColumna(DataRow row, params string[] posiblesNombres)
        {
            foreach (string nombre in posiblesNombres)
            {
                if (row.Table.Columns.Contains(nombre) && row[nombre] != DBNull.Value)
                {
                    if (row[nombre] is DateTime fecha)
                        return fecha;
                    if (DateTime.TryParse(row[nombre].ToString(), out DateTime fechaParsed))
                        return fechaParsed;
                }
            }
            return DateTime.Today;
        }
    }
}

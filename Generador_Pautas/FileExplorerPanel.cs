using Generador_Pautas.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Generador_Pautas
{
    /// <summary>
    /// Panel de explorador de archivos integrado directamente en Form1
    /// </summary>
    public class FileExplorerPanel
    {
        // Constantes
        private const int COLUMN_ICON = 0;
        private const int COLUMN_NAME = 1;
        private const string DIRECTORY = "Directory";
        private const string FILE = "File";
        private const int ICON_PADDING = 2;
        private const int ICON_SIZE = 16;
        private const int TEXT_OFFSET = 20;

        // Controles
        private Panel containerPanel;
        private PictureBox btnAtras;
        private ComboBox comboBoxDrives;
        private PictureBox picBuscar;
        private TextBox txtBuscarAudio;
        private Button btnCancelarBusqueda;
        private Label lblEstadoBusqueda;
        private DataGridView dgvExplorador;

        // Iconos
        private Image diskIcon;
        private Image folderIcon;
        private Image audioFileIcon;

        // Estado
        private DirectoryInfo currentDirectory;
        private bool actualizandoComboBox = false;
        private System.Windows.Forms.Timer timerBusqueda;

        // Eventos
        public event EventHandler<string> AudioFileDoubleClicked;
        public event EventHandler<List<string>> AudioFilesLoaded;
        public event EventHandler<string> SearchTextChanged;

        /// <summary>
        /// Obtiene el panel contenedor para agregar al formulario
        /// </summary>
        public Panel Panel => containerPanel;

        public FileExplorerPanel()
        {
            CreateControls();
            LoadIcons();
            SetupEvents();

            // Timer para debounce de busqueda
            timerBusqueda = new System.Windows.Forms.Timer();
            timerBusqueda.Interval = 100;
            timerBusqueda.Tick += TimerBusqueda_Tick;
        }

        private void CreateControls()
        {
            // Panel contenedor principal - altura compacta
            containerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 160,
                BackColor = Color.FromArgb(250, 250, 252)
            };

            // Boton atras
            btnAtras = new PictureBox
            {
                Location = new Point(2, 3),
                Size = new Size(18, 18),
                SizeMode = PictureBoxSizeMode.Zoom,
                Cursor = Cursors.Hand,
                Image = Resources.atras
            };

            // ComboBox de drives
            comboBoxDrives = new ComboBox
            {
                Location = new Point(22, 2),
                Size = new Size(350, 20),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 7.5F),
                DrawMode = DrawMode.OwnerDrawFixed,
                DropDownWidth = 400
            };

            // DataGridView explorador - ahora va arriba, justo despues del combobox
            dgvExplorador = new DataGridView
            {
                Location = new Point(2, 24),
                Size = new Size(370, 100),  // Ancho ajustado
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeColumns = false,
                AllowUserToResizeRows = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.FromArgb(250, 250, 255),
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.None,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
                EnableHeadersVisualStyles = false,
                Cursor = Cursors.Hand
            };

            // Icono de busqueda - ahora debajo del DataGridView
            picBuscar = new PictureBox
            {
                Location = new Point(4, 128),
                Size = new Size(15, 15),
                SizeMode = PictureBoxSizeMode.Zoom,
                Cursor = Cursors.Default
            };

            // TextBox de busqueda - debajo del DataGridView
            txtBuscarAudio = new TextBox
            {
                Location = new Point(22, 126),
                Size = new Size(328, 19),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Font = new Font("Segoe UI", 7.5F),
                ForeColor = Color.Black,
                Text = "",
                BorderStyle = BorderStyle.FixedSingle
            };

            // Boton cancelar busqueda
            btnCancelarBusqueda = new Button
            {
                Location = new Point(352, 126),
                Size = new Size(22, 19),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Text = "X",
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(244, 67, 54),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 6F, FontStyle.Bold),
                Visible = false,
                Cursor = Cursors.Hand
            };
            btnCancelarBusqueda.FlatAppearance.BorderSize = 0;

            // Label estado busqueda - debajo del textbox
            lblEstadoBusqueda = new Label
            {
                Location = new Point(22, 147),
                Size = new Size(350, 12),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Font = new Font("Segoe UI", 6.5F),
                ForeColor = Color.FromArgb(100, 100, 100)
            };

            // Estilos del DataGridView
            dgvExplorador.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(63, 81, 181),
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 8F),
                SelectionBackColor = Color.FromArgb(63, 81, 181),
                SelectionForeColor = Color.White
            };
            dgvExplorador.ColumnHeadersHeight = 22;
            dgvExplorador.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

            dgvExplorador.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(250, 250, 255),
                ForeColor = Color.FromArgb(40, 40, 40),
                SelectionBackColor = Color.FromArgb(0, 120, 215),
                SelectionForeColor = Color.White,
                Font = new Font("Segoe UI", 8F)
            };

            dgvExplorador.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(232, 234, 246)
            };

            dgvExplorador.RowTemplate.Height = 22;

            // Agregar columnas
            var imageColumn = new DataGridViewImageColumn
            {
                Name = "Icon",
                HeaderText = "",
                Width = 24,
                ImageLayout = DataGridViewImageCellLayout.Zoom
            };
            dgvExplorador.Columns.Add(imageColumn);
            dgvExplorador.Columns.Add("Name", "Explorador de Archivos");
            dgvExplorador.Columns[1].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            dgvExplorador.Columns[1].MinimumWidth = 100;

            // Agregar controles al panel
            containerPanel.Controls.Add(dgvExplorador);
            containerPanel.Controls.Add(lblEstadoBusqueda);
            containerPanel.Controls.Add(btnCancelarBusqueda);
            containerPanel.Controls.Add(txtBuscarAudio);
            containerPanel.Controls.Add(picBuscar);
            containerPanel.Controls.Add(comboBoxDrives);
            containerPanel.Controls.Add(btnAtras);

            // Menu contextual
            var contextMenu = new ContextMenuStrip();
            var menuUbicar = new ToolStripMenuItem("Ubicar Archivo");
            menuUbicar.Click += OpenFileMenuItem_Click;
            contextMenu.Items.Add(menuUbicar);
            dgvExplorador.ContextMenuStrip = contextMenu;
        }

        private void LoadIcons()
        {
            diskIcon = Resources.diskdrive;
            folderIcon = Resources.folder;
            audioFileIcon = Resources.audio;
            picBuscar.Image = CrearIconoBusqueda(16, 16);
        }

        private Image CrearIconoBusqueda(int width, int height)
        {
            Bitmap bmp = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);
                using (Pen pen = new Pen(Color.FromArgb(100, 100, 100), 1.5f))
                {
                    g.DrawEllipse(pen, 1, 1, 8, 8);
                    g.DrawLine(pen, 8, 8, 13, 13);
                }
            }
            return bmp;
        }

        private void SetupEvents()
        {
            btnAtras.Click += BtnAtras_Click;
            comboBoxDrives.DrawItem += ComboBoxDrives_DrawItem;
            comboBoxDrives.SelectedIndexChanged += ComboBoxDrives_SelectedIndexChanged;
            txtBuscarAudio.TextChanged += TxtBuscarAudio_TextChanged;
            txtBuscarAudio.KeyDown += TxtBuscarAudio_KeyDown;
            btnCancelarBusqueda.Click += BtnCancelarBusqueda_Click;
            dgvExplorador.CellDoubleClick += DgvExplorador_CellDoubleClick;
            dgvExplorador.MouseUp += DgvExplorador_MouseUp;
        }

        /// <summary>
        /// Inicializa el explorador (llamar despues de agregar al formulario)
        /// </summary>
        public void Initialize()
        {
            // Cargar drives
            comboBoxDrives.Items.Clear();
            foreach (var drive in Environment.GetLogicalDrives())
            {
                comboBoxDrives.Items.Add(drive);
            }
            if (comboBoxDrives.Items.Count > 0)
            {
                comboBoxDrives.SelectedIndex = 0;
                currentDirectory = new DirectoryInfo(comboBoxDrives.Items[0].ToString());
                LoadDirectoryContent(currentDirectory);
            }
        }

        private void LoadDirectoryContent(DirectoryInfo directory)
        {
            dgvExplorador.Rows.Clear();
            var audioFiles = new List<string>();

            try
            {
                // Recolectar archivos de audio primero
                foreach (var file in directory.GetFiles().Where(IsAudioFile))
                {
                    audioFiles.Add(file.FullName);
                }

                // Si la carpeta actual tiene archivos de audio, mostrarla en el explorador
                if (audioFiles.Count > 0)
                {
                    AddRowToExplorer(folderIcon, directory.Name, "CURRENT");
                }

                // Cargar subdirectorios (solo los que contienen archivos MP3)
                foreach (var subDirectory in directory.GetDirectories())
                {
                    if (ShouldSkipDirectory(subDirectory)) continue;
                    if (DirectoryContainsMp3Files(subDirectory))
                    {
                        AddRowToExplorer(folderIcon, subDirectory.Name, DIRECTORY);
                    }
                }

                // Disparar el evento para cargar los archivos en dgv_archivos
                if (audioFiles.Count > 0)
                {
                    AudioFilesLoaded?.Invoke(this, audioFiles);
                }
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show("No tienes permisos para acceder a esta carpeta.", "Acceso denegado", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar el directorio: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Verifica si un directorio contiene archivos MP3 (busqueda recursiva con limite de profundidad)
        /// </summary>
        private bool DirectoryContainsMp3Files(DirectoryInfo directory, int depth = 0)
        {
            const int MAX_DEPTH = 5; // Aumentar profundidad para encontrar carpetas anidadas

            try
            {
                // Verificar archivos MP3 en el directorio actual
                if (directory.GetFiles("*.mp3", SearchOption.TopDirectoryOnly).Any())
                    return true;

                // Tambien verificar archivos WAV
                if (directory.GetFiles("*.wav", SearchOption.TopDirectoryOnly).Any())
                    return true;

                // Si no hemos llegado al limite de profundidad, buscar en subdirectorios
                if (depth < MAX_DEPTH)
                {
                    foreach (var subDir in directory.GetDirectories())
                    {
                        if (ShouldSkipDirectory(subDir)) continue;
                        if (DirectoryContainsMp3Files(subDir, depth + 1))
                            return true;
                    }
                }

                return false;
            }
            catch
            {
                return false; // En caso de error de acceso, asumir que no tiene MP3
            }
        }

        private void AddRowToExplorer(Image icon, string name, string tag)
        {
            var row = new DataGridViewRow();
            row.Cells.Add(new DataGridViewImageCell { Value = icon });
            row.Cells.Add(new DataGridViewTextBoxCell { Value = name });
            row.Tag = tag;
            dgvExplorador.Rows.Add(row);
        }

        private bool ShouldSkipDirectory(DirectoryInfo directory)
        {
            if ((directory.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden) return true;
            if ((directory.Attributes & FileAttributes.System) == FileAttributes.System) return true;

            string[] excludedFolders = {
                "$RECYCLE.BIN", "System Volume Information", "Recovery", "ProgramData",
                "Windows", "Program Files", "Program Files (x86)", "AppData",
                "Config.Msi", "Documents and Settings", "PerfLogs",
                "$Windows.~BT", "$Windows.~WS", "MSOCache"
            };

            return excludedFolders.Any(excluded =>
                directory.Name.Equals(excluded, StringComparison.OrdinalIgnoreCase));
        }

        private bool IsAudioFile(FileInfo file)
        {
            string ext = file.Extension.ToLowerInvariant();
            return ext == ".mp3" || ext == ".wav";
        }

        #region Event Handlers

        private void BtnAtras_Click(object sender, EventArgs e)
        {
            if (currentDirectory?.Parent != null)
            {
                currentDirectory = currentDirectory.Parent;
                LoadDirectoryContent(currentDirectory);
                ActualizarRutaEnComboBox();
            }
        }

        private void ComboBoxDrives_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (actualizandoComboBox) return;

            string selectedPath = comboBoxDrives.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(selectedPath)) return;

            var newDir = new DirectoryInfo(selectedPath);
            if (newDir.Exists && !newDir.FullName.Equals(currentDirectory?.FullName, StringComparison.OrdinalIgnoreCase))
            {
                currentDirectory = newDir;
                LoadDirectoryContent(currentDirectory);
            }
        }

        private void ComboBoxDrives_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;

            string text = comboBoxDrives.GetItemText(comboBoxDrives.Items[e.Index]);
            e.DrawBackground();

            bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            Brush textBrush = isSelected ? Brushes.White : Brushes.Black;
            bool isDrive = Environment.GetLogicalDrives().Contains(text);

            Image icon = isDrive ? diskIcon : folderIcon;
            e.Graphics.DrawImage(icon, e.Bounds.Left + ICON_PADDING, e.Bounds.Top + ICON_PADDING, ICON_SIZE, ICON_SIZE);
            e.Graphics.DrawString(text, comboBoxDrives.Font, textBrush, e.Bounds.Left + TEXT_OFFSET, e.Bounds.Top + ICON_PADDING);
            e.DrawFocusRectangle();
        }

        private void ActualizarRutaEnComboBox()
        {
            actualizandoComboBox = true;
            try
            {
                string rutaActual = currentDirectory.FullName;
                int index = -1;
                for (int i = 0; i < comboBoxDrives.Items.Count; i++)
                {
                    if (comboBoxDrives.Items[i].ToString().Equals(rutaActual, StringComparison.OrdinalIgnoreCase))
                    {
                        index = i;
                        break;
                    }
                }

                if (index >= 0)
                    comboBoxDrives.SelectedIndex = index;
                else
                {
                    comboBoxDrives.Items.Add(rutaActual);
                    comboBoxDrives.SelectedIndex = comboBoxDrives.Items.Count - 1;
                }
            }
            finally
            {
                actualizandoComboBox = false;
            }
        }

        private void DgvExplorador_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            var row = dgvExplorador.Rows[e.RowIndex];
            string name = row.Cells[COLUMN_NAME].Value?.ToString();
            string tag = row.Tag?.ToString();

            if (string.IsNullOrEmpty(tag)) return;

            if (tag == DIRECTORY)
            {
                currentDirectory = new DirectoryInfo(Path.Combine(currentDirectory.FullName, name));
                LoadDirectoryContent(currentDirectory);
                ActualizarRutaEnComboBox();
            }
        }

        private void DgvExplorador_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                var hitTest = dgvExplorador.HitTest(e.X, e.Y);
                if (hitTest.Type == DataGridViewHitTestType.Cell &&
                    dgvExplorador.Rows[hitTest.RowIndex].Tag?.ToString() == FILE)
                {
                    dgvExplorador.ClearSelection();
                    dgvExplorador.Rows[hitTest.RowIndex].Selected = true;
                }
                else
                {
                    dgvExplorador.ClearSelection();
                }
            }
        }

        private void OpenFileMenuItem_Click(object sender, EventArgs e)
        {
            if (dgvExplorador.SelectedRows.Count > 0 && dgvExplorador.SelectedRows[0].Tag?.ToString() == FILE)
            {
                string fileName = dgvExplorador.SelectedRows[0].Cells[COLUMN_NAME].Value.ToString();
                string filePath = Path.Combine(currentDirectory.FullName, fileName);
                Process.Start("explorer.exe", $"/select,\"{filePath}\"");
            }
        }

        #endregion

        #region Busqueda en dgv_archivos

        private void TxtBuscarAudio_TextChanged(object sender, EventArgs e)
        {
            timerBusqueda.Stop();
            timerBusqueda.Start();
        }

        private void TxtBuscarAudio_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                e.SuppressKeyPress = true;
                txtBuscarAudio.Text = "";
                SearchTextChanged?.Invoke(this, "");
            }
        }

        private void TimerBusqueda_Tick(object sender, EventArgs e)
        {
            timerBusqueda.Stop();
            string termino = txtBuscarAudio.Text.Trim();

            // Disparar evento para filtrar en dgv_archivos
            SearchTextChanged?.Invoke(this, termino);

            // Actualizar label de estado
            if (string.IsNullOrEmpty(termino))
            {
                lblEstadoBusqueda.Text = "";
            }
            else
            {
                lblEstadoBusqueda.Text = $"Filtrando: {termino}";
                lblEstadoBusqueda.ForeColor = Color.FromArgb(0, 120, 215);
            }
        }

        private void BtnCancelarBusqueda_Click(object sender, EventArgs e)
        {
            txtBuscarAudio.Text = "";
            lblEstadoBusqueda.Text = "";
            SearchTextChanged?.Invoke(this, "");
        }

        /// <summary>
        /// Actualiza el label de estado con la cantidad de resultados encontrados
        /// </summary>
        public void ActualizarEstadoBusqueda(int encontrados, int total)
        {
            if (string.IsNullOrEmpty(txtBuscarAudio.Text.Trim()))
            {
                lblEstadoBusqueda.Text = "";
            }
            else
            {
                lblEstadoBusqueda.Text = $"{encontrados} de {total} archivos";
                lblEstadoBusqueda.ForeColor = encontrados > 0
                    ? Color.FromArgb(76, 175, 80)
                    : Color.FromArgb(244, 67, 54);
            }
        }

        /// <summary>
        /// Muestra el progreso de carga de archivos
        /// </summary>
        public void MostrarProgresoCarga(int cargados, int total)
        {
            if (cargados < total)
            {
                lblEstadoBusqueda.Text = $"Cargando {cargados}/{total}...";
                lblEstadoBusqueda.ForeColor = Color.FromArgb(33, 150, 243); // Azul
            }
            else
            {
                lblEstadoBusqueda.Text = $"{total} archivos";
                lblEstadoBusqueda.ForeColor = Color.FromArgb(76, 175, 80); // Verde
            }
        }

        #endregion
    }
}

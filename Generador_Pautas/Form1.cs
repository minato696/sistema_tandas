using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Un4seen.Bass;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
namespace Generador_Pautas
{
    public partial class Form1 : Form
    {
        public int index = 1;
        private int contador = 1;

        public List<string> songPaths = new List<string>();
        public const string SongTimeFormat = @"mm\:ss";
        public TimeSpan totalTime = new TimeSpan();
        private Timer playbackTimer = new Timer();
        public int currentPlayingRowIndex = -1; // -1 indica que ninguna fila está siendo reproducida
        private bool isDragging = false;
        private DGV_Form1 dgvForm;
        private DatabaseManager dbManager = new DatabaseManager(); // Crea una instancia de DatabaseManager
        private DashboardControl _dashboardControl; // Dashboard real
        public BassPlayer BassPlayer { get; private set; }
        private AudioPlayer audioPlayer = new AudioPlayer();
        public List<AgregarComercialesData> agregarComercialesDataList = new List<AgregarComercialesData>();

        // Filtros actuales para dgv_base
        private string _filtroRadioActual = null;
        private string _filtroCiudadActual = null;

        // Flag para evitar multiples disparos de SelectionChanged durante busquedas
        private bool _cargandoDatos = false;

        // Flag para evitar cargas dobles durante la inicializacion
        private bool _inicializando = true;

        // FileSystemWatcher para sincronizacion en tiempo real
        private FileSystemWatcher _watcherRed = null;

        // Menu de administracion (solo visible para admin)
        private MenuStrip menuPrincipal;
        private ToolStripMenuItem menuAdministracion;
        private ToolStripMenuItem menuAdminCiudades;
        private ToolStripMenuItem menuAdminRadios;
        private ToolStripMenuItem menuAdminTandas;
        private ToolStripMenuItem menuAdminUsuarios;
        private ToolStripMenuItem menuCerrarSesion;
        private Label lblUsuarioActual;

        // Controles de paginacion (en toolbar)
        private System.Windows.Forms.Button btnPrimero;
        private System.Windows.Forms.Button btnAnterior;
        private System.Windows.Forms.Button btnSiguiente;
        private System.Windows.Forms.Button btnUltimo;
        private Label lblPaginaActual;
        private System.Windows.Forms.Button btnGenerarTanda;
        private System.Windows.Forms.Button btnGenerarGlobal;
        private System.Windows.Forms.Button btnEliminarSpot;
        private int _paginaActual = 1;
        private int _registrosPorPagina = 500;
        private int _totalRegistros = 0;

        private PlayerState playerState = new PlayerState();

        // Nuevo explorador de archivos integrado
        private FileExplorerPanel fileExplorerPanel;

        // Panel de Pauteo Rápido
        private PauteoRapidoPanel pauteoRapidoPanel;

        // Panel de Vista Previa de Spots (debajo de Eliminar Pautas)
        private GroupBox grpVistaPrevia;
        private Label lblVistaPreviaHora;
        private ListBox lstVistaPreviaSpots;

        // Vista siempre agrupada por archivo único (como sistema antiguo)
        private int _totalArchivosUnicos = 0;

        // Caché de datos para acelerar cambios de ciudad/radio
        private Dictionary<string, DataTable> _cacheDatosCiudadRadio = new Dictionary<string, DataTable>();
        private DateTime _ultimaActualizacionCache = DateTime.MinValue;
        private const int CACHE_EXPIRACION_MINUTOS = 5;

        public class PlayerState
        {
            public int CurrentStream { get; set; } = 0;
            public int CurrentPlayingRowIndex { get; set; } = -1;
        }
        public Form1()
        {
            // Habilitar DoubleBuffered en el formulario para evitar parpadeo
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer |
                          ControlStyles.AllPaintingInWmPaint |
                          ControlStyles.UserPaint, true);
            this.DoubleBuffered = true;
            this.UpdateStyles();

            InitializeComponent();

            // Inicializar el DashboardControl real y agregarlo al panel
            _dashboardControl = new DashboardControl();
            _dashboardControl.Dock = DockStyle.Fill;
            dashboardControl1.Controls.Add(_dashboardControl);

            elegantProgressBar1.MouseDown += ProgressBar1_MouseDown;
            elegantProgressBar1.MouseUp += ProgressBar1_MouseUp;
            elegantProgressBar1.MouseMove += ProgressBar1_MouseMove;

            // Inicializar el nuevo explorador de archivos integrado
            fileExplorerPanel = new FileExplorerPanel();
            fileExplorerPanel.AudioFileDoubleClicked += FileExplorerControl_AudioFileDoubleClicked;
            fileExplorerPanel.AudioFilesLoaded += FileExplorerPanel_AudioFilesLoaded;
            fileExplorerPanel.SearchTextChanged += FileExplorerPanel_SearchTextChanged;

            // Reorganizar el panel izquierdo
            pnlLeft.SuspendLayout();

            // Reducir altura del pnlPlayer y ajustar posiciones de botones
            pnlPlayer.Height = 75;
            elegantProgressBar1.Location = new Point(8, 8);
            elegantProgressBar1.Size = new Size(elegantProgressBar1.Width, 25);
            progressBarLeft.Location = new Point(progressBarLeft.Location.X, 8);
            progressBarLeft.Size = new Size(progressBarLeft.Width, 25);
            progressBarRight.Location = new Point(progressBarRight.Location.X, 8);
            progressBarRight.Size = new Size(progressBarRight.Width, 25);
            btn_play.Location = new Point(8, 38);
            btn_play.Size = new Size(70, 30);
            btn_stop.Location = new Point(83, 38);
            btn_stop.Size = new Size(70, 30);
            btn_pause.Location = new Point(158, 38);
            btn_pause.Size = new Size(70, 30);
            btn_limpiar.Location = new Point(233, 38);
            btn_limpiar.Size = new Size(70, 30);

            // Agregar todos los paneles a pnlLeft (sin estaciones/ciudades, se moveran a panel derecho)
            pnlLeft.Controls.Add(fileExplorerPanel.Panel);

            // Orden de Dock: (Mayor a menor indice)
            // - dgv_archivos (Fill) = indice 0
            // - fileExplorerPanel (Top) = indice 1
            // - pnlPlayer (Bottom) = indice 2
            pnlLeft.Controls.SetChildIndex(dgv_archivos, 0);
            pnlLeft.Controls.SetChildIndex(fileExplorerPanel.Panel, 1);
            pnlLeft.Controls.SetChildIndex(pnlPlayer, 2);

            pnlLeft.ResumeLayout(true);

            fileExplorerPanel.Initialize();

            // Inicializar el panel de Pauteo Rápido
            InicializarPauteoRapido();

            BassPlayer = new BassPlayer();
            dgvForm = new DGV_Form1(dgv_archivos, dgv_base, dgv_estaciones, dgv_ciudades);
            // Configurar el temporizador
            playbackTimer.Interval = 100; // Actualizar cada segundo
            playbackTimer.Tick += PlaybackTimer_Tick;
            this.Shown += Form1_Shown;
            audioPlayer = new AudioPlayer();
            audioPlayer.AudioFinished += AudioPlayer_AudioFinished;
            audioPlayer.AudioProgressUpdated += AudioPlayer_AudioProgressUpdated;

            // Configurar eventos para dgv_archivos
            dgv_archivos.CellDoubleClick += dgv_archivos_CellDoubleClick;
            dgv_archivos.SelectionChanged += dgv_archivos_SelectionChanged;
            ConfigurarMenuContextualDGVArchivos();

            // Configurar evento para dgv_base
            dgv_base.SelectionChanged += dgv_base_SelectionChanged;

            // Configurar eventos de filtro para dgv_estaciones y dgv_ciudades
            dgv_estaciones.SelectionChanged += dgv_estaciones_SelectionChanged;
            dgv_ciudades.SelectionChanged += dgv_ciudades_SelectionChanged;

            // Configurar FileSystemWatcher para sincronizacion en tiempo real
            if (ConfigManager.EsRutaDeRed)
            {
                ConfigurarWatcherRed();
            }

            // Crear menu de administracion
            CrearMenuAdministracion();
        }

        private void CrearMenuAdministracion()
        {
            // Crear el menu principal
            menuPrincipal = new MenuStrip();
            menuPrincipal.BackColor = Color.FromArgb(240, 240, 245);
            menuPrincipal.Font = new Font("Segoe UI", 9.5F);

            // Menu Administracion (solo visible para admin)
            menuAdministracion = new ToolStripMenuItem("Administracion");
            menuAdministracion.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);

            // Submenu Ciudades
            menuAdminCiudades = new ToolStripMenuItem("Administrar Ciudades");
            menuAdminCiudades.Click += async (s, e) => {
                var form = new AdminCiudadesForm();
                form.ShowDialog();
                // Refrescar las ciudades despues de cerrar el formulario
                await CargarCiudadesAsync();
            };

            // Submenu Radios
            menuAdminRadios = new ToolStripMenuItem("Administrar Radios");
            menuAdminRadios.Click += async (s, e) => {
                var form = new AdminRadiosForm();
                form.ShowDialog();
                // Refrescar las estaciones despues de cerrar el formulario
                await CargarEstacionesAsync();
            };

            // Submenu Tandas de Programacion
            menuAdminTandas = new ToolStripMenuItem("Administrar Tandas de Programacion");
            menuAdminTandas.Click += (s, e) => {
                var form = new AdminTandasForm();
                form.ShowDialog();
            };

            // Submenu Usuarios
            menuAdminUsuarios = new ToolStripMenuItem("Administrar Usuarios");
            menuAdminUsuarios.Click += (s, e) => {
                var form = new AdminUsuariosForm();
                form.ShowDialog();
            };

            // Menu Importar desde Excel
            var menuImportarExcel = new ToolStripMenuItem("Importar Pautas desde Excel");
            menuImportarExcel.Click += (s, e) => {
                var form = new ImportadorExcelForm();
                form.ShowDialog();
            };

            // Menu Importar desde Access
            var menuImportarAccess = new ToolStripMenuItem("Importar desde Access (.mdb)");
            menuImportarAccess.Click += (s, e) => {
                var form = new ImportarAccessForm();
                form.ShowDialog();
            };

            // Menu Consultar Comerciales (estilo sistema antiguo)
            var menuConsultarComerciales = new ToolStripMenuItem("Consultar Comerciales (Sistema Antiguo)");
            menuConsultarComerciales.Click += (s, e) => {
                var form = new ConsultarComercialesForm();
                form.ShowDialog();
            };

            // Menu Optimizar Base de Datos
            var menuOptimizarBD = new ToolStripMenuItem("Optimizar Base de Datos");
            menuOptimizarBD.Click += (s, e) => {
                DialogResult confirmar = MessageBox.Show(
                    "Esta accion optimizara la base de datos:\n\n" +
                    "- Compactara el archivo\n" +
                    "- Reconstruira los indices\n" +
                    "- Actualizara las estadisticas\n\n" +
                    "Esto puede mejorar el rendimiento de las busquedas.\n\nDesea continuar?",
                    "Optimizar Base de Datos",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (confirmar == DialogResult.Yes)
                {
                    this.Cursor = Cursors.WaitCursor;
                    try
                    {
                        string resultado = SQLiteMigration.OptimizarBaseDeDatos();
                        string estadisticas = SQLiteMigration.ObtenerEstadisticas();
                        MessageBox.Show(
                            resultado + "\n\n--- Estadisticas ---\n" + estadisticas,
                            "Resultado",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                    }
                    finally
                    {
                        this.Cursor = Cursors.Default;
                    }
                }
            };

            // Menu Ver Estadisticas BD
            var menuEstadisticasBD = new ToolStripMenuItem("Ver Estadisticas BD");
            menuEstadisticasBD.Click += (s, e) => {
                string estadisticas = SQLiteMigration.ObtenerEstadisticas();
                string infoConexion = SQLiteMigration.ObtenerInfoConexion();
                MessageBox.Show(
                    "--- Estadisticas de la Base de Datos ---\n" + estadisticas +
                    "\n--- Informacion de Conexion ---\n" + infoConexion,
                    "Estadisticas",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            };

            // Agregar submenus
            menuAdministracion.DropDownItems.Add(menuAdminCiudades);
            menuAdministracion.DropDownItems.Add(menuAdminRadios);
            menuAdministracion.DropDownItems.Add(menuAdminTandas);
            menuAdministracion.DropDownItems.Add(new ToolStripSeparator());
            menuAdministracion.DropDownItems.Add(menuImportarExcel);
            menuAdministracion.DropDownItems.Add(menuImportarAccess);
            menuAdministracion.DropDownItems.Add(menuConsultarComerciales);
            menuAdministracion.DropDownItems.Add(new ToolStripSeparator());
            menuAdministracion.DropDownItems.Add(menuOptimizarBD);
            menuAdministracion.DropDownItems.Add(menuEstadisticasBD);
            menuAdministracion.DropDownItems.Add(new ToolStripSeparator());
            menuAdministracion.DropDownItems.Add(menuAdminUsuarios);

            // Menu Sesion
            var menuSesion = new ToolStripMenuItem("Sesion");
            menuSesion.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);

            menuCerrarSesion = new ToolStripMenuItem("Cerrar Sesion");
            menuCerrarSesion.Click += (s, e) => {
                DialogResult result = MessageBox.Show("Desea cerrar sesion?", "Confirmar",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    UserManager.Logout();
                    this.Close();
                }
            };

            menuSesion.DropDownItems.Add(menuCerrarSesion);

            // Menu Reportes
            var menuReportes = new ToolStripMenuItem("Reportes");
            menuReportes.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            menuReportes.Click += (s, e) => {
                var form = new ReportesForm();
                form.ShowDialog();
            };

            // Menu Comparar Pautas
            var menuComparar = new ToolStripMenuItem("Comparar");
            menuComparar.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            menuComparar.Click += (s, e) => {
                var form = new ComparadorPautasForm();
                form.ShowDialog();
            };

            // Agregar menus al MenuStrip
            menuPrincipal.Items.Add(menuAdministracion);
            menuPrincipal.Items.Add(menuReportes);
            menuPrincipal.Items.Add(menuComparar);
            menuPrincipal.Items.Add(menuSesion);

            // Label para mostrar usuario actual
            lblUsuarioActual = new Label();
            lblUsuarioActual.AutoSize = true;
            lblUsuarioActual.Font = new Font("Segoe UI", 9F);
            lblUsuarioActual.ForeColor = Color.FromArgb(0, 120, 215);
            lblUsuarioActual.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblUsuarioActual.Location = new Point(this.ClientSize.Width - 250, 5);

            // Agregar al formulario
            this.MainMenuStrip = menuPrincipal;
            this.Controls.Add(menuPrincipal);
            this.Controls.Add(lblUsuarioActual);

            // Crear controles de paginacion
            CrearControlesPaginacion();
        }

        private void CrearControlesPaginacion()
        {
            // Agregar controles al pnlToolbar - Reorganizado sin botones de agregar/eliminar/desactivar

            // Boton Generar Todo (color verde oscuro) - Genera todas las pautas de ciudad/radio
            btnGenerarTanda = new System.Windows.Forms.Button
            {
                Text = "Generar Todo",
                Width = 95,
                Height = 34,
                Location = new Point(10, 8),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(56, 142, 60), // Verde oscuro
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnGenerarTanda.FlatAppearance.BorderSize = 0;
            btnGenerarTanda.Click += BtnGenerarTodo_Click;

            // Boton Generar Global (color azul) - Genera pautas para TODAS las ciudades y radios
            btnGenerarGlobal = new System.Windows.Forms.Button
            {
                Text = "Generar Global",
                Width = 105,
                Height = 34,
                Location = new Point(110, 8),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(33, 150, 243), // Azul
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnGenerarGlobal.FlatAppearance.BorderSize = 0;
            btnGenerarGlobal.Click += BtnGenerarGlobal_Click;

            // Boton Eliminar Spot (color rojo)
            btnEliminarSpot = new System.Windows.Forms.Button
            {
                Text = "Eliminar",
                Width = 80,
                Height = 34,
                Location = new Point(220, 8),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(244, 67, 54), // Rojo
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnEliminarSpot.FlatAppearance.BorderSize = 0;
            btnEliminarSpot.Click += BtnEliminarSpot_Click;

            int startX = 305; // Posicion inicial para paginacion (despues del boton Eliminar)

            // Boton Primera pagina
            btnPrimero = new System.Windows.Forms.Button
            {
                Text = "|<",
                Width = 35,
                Height = 34,
                Location = new Point(startX, 8),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(63, 81, 181),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnPrimero.FlatAppearance.BorderSize = 0;
            btnPrimero.Click += async (s, e) => await IrAPaginaAsync(1);

            // Boton Anterior
            btnAnterior = new System.Windows.Forms.Button
            {
                Text = "<",
                Width = 35,
                Height = 34,
                Location = new Point(startX + 38, 8),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(63, 81, 181),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnAnterior.FlatAppearance.BorderSize = 0;
            btnAnterior.Click += async (s, e) => await IrAPaginaAsync(_paginaActual - 1);

            // Label de pagina actual
            lblPaginaActual = new Label
            {
                AutoSize = false,
                Width = 120,
                Height = 34,
                Location = new Point(startX + 76, 8),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(50, 50, 50),
                BackColor = Color.FromArgb(240, 240, 245),
                Text = "1/1 (0)"
            };

            // Boton Siguiente
            btnSiguiente = new System.Windows.Forms.Button
            {
                Text = ">",
                Width = 35,
                Height = 34,
                Location = new Point(startX + 199, 8),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(63, 81, 181),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnSiguiente.FlatAppearance.BorderSize = 0;
            btnSiguiente.Click += async (s, e) => await IrAPaginaAsync(_paginaActual + 1);

            // Boton Ultima pagina
            btnUltimo = new System.Windows.Forms.Button
            {
                Text = ">|",
                Width = 35,
                Height = 34,
                Location = new Point(startX + 237, 8),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(63, 81, 181),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnUltimo.FlatAppearance.BorderSize = 0;
            btnUltimo.Click += async (s, e) => {
                int totalPaginas = (int)Math.Ceiling((double)_totalRegistros / _registrosPorPagina);
                await IrAPaginaAsync(totalPaginas);
            };

            // Agregar controles al pnlToolbar
            if (pnlToolbar != null)
            {
                pnlToolbar.Controls.Add(btnGenerarTanda);
                pnlToolbar.Controls.Add(btnGenerarGlobal);
                pnlToolbar.Controls.Add(btnEliminarSpot);
                pnlToolbar.Controls.Add(btnPrimero);
                pnlToolbar.Controls.Add(btnAnterior);
                pnlToolbar.Controls.Add(lblPaginaActual);
                pnlToolbar.Controls.Add(btnSiguiente);
                pnlToolbar.Controls.Add(btnUltimo);
            }

            // Configurar menu contextual para dgv_base
            ConfigurarMenuContextualDGVBase();
        }

        private void ConfigurarColumnasDgvBase()
        {
            dgv_base.Columns.Clear();

            // Vista siempre agrupada (por archivo + ciudad + radio, como sistema antiguo)
            dgv_base.Columns.Add("FilePath", "Ruta");
            dgv_base.Columns.Add("Codigo", "Código");

            // Columna Archivo con alineación a la izquierda
            var colArchivo = new DataGridViewTextBoxColumn();
            colArchivo.Name = "NombreArchivo";
            colArchivo.HeaderText = "Archivo";
            colArchivo.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dgv_base.Columns.Add(colArchivo);

            dgv_base.Columns.Add("TotalRegistros", "Tandas");
            dgv_base.Columns.Add("Ciudad", "Ciudad");
            dgv_base.Columns.Add("Radio", "Radio");
            dgv_base.Columns.Add("FechaMinima", "Desde");
            dgv_base.Columns.Add("FechaMaxima", "Hasta");
            dgv_base.Columns.Add("EstadoGeneral", "Estado");
            dgv_base.Columns.Add("Posicion", "Pos");

            // Ocultar columna FilePath (solo para referencia interna)
            dgv_base.Columns["FilePath"].Visible = false;

            // Usar AutoSizeColumnsMode para que sea responsive
            dgv_base.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            // Configurar FillWeight para proporciones relativas
            dgv_base.Columns["Codigo"].FillWeight = 8;
            dgv_base.Columns["NombreArchivo"].FillWeight = 40;
            dgv_base.Columns["TotalRegistros"].FillWeight = 8;
            dgv_base.Columns["Ciudad"].FillWeight = 12;
            dgv_base.Columns["Radio"].FillWeight = 12;
            dgv_base.Columns["FechaMinima"].FillWeight = 12;
            dgv_base.Columns["FechaMaxima"].FillWeight = 12;
            dgv_base.Columns["EstadoGeneral"].FillWeight = 8;
            dgv_base.Columns["Posicion"].FillWeight = 6;

            // Anchos minimos para que no se vean muy pequenas
            dgv_base.Columns["Codigo"].MinimumWidth = 50;
            dgv_base.Columns["NombreArchivo"].MinimumWidth = 150;
            dgv_base.Columns["TotalRegistros"].MinimumWidth = 50;
            dgv_base.Columns["Ciudad"].MinimumWidth = 70;
            dgv_base.Columns["Radio"].MinimumWidth = 70;
            dgv_base.Columns["FechaMinima"].MinimumWidth = 75;
            dgv_base.Columns["FechaMaxima"].MinimumWidth = 75;
            dgv_base.Columns["EstadoGeneral"].MinimumWidth = 50;
            dgv_base.Columns["Posicion"].MinimumWidth = 35;
        }

        private async Task CargarDatosAgrupadosAsync(bool forzarRecarga = false)
        {
            // Iniciar medición de tiempo total
            var swTotal = System.Diagnostics.Stopwatch.StartNew();
            var swParcial = new System.Diagnostics.Stopwatch();

            try
            {
                this.Cursor = Cursors.WaitCursor;
                lblUsuarioActual.Text = "Cargando...";
                Application.DoEvents();

                int offset = (_paginaActual - 1) * _registrosPorPagina;

                // Obtener filtros actuales
                string textoBusqueda = txtBusqueda.Text.Trim();
                string estadoFiltro = cboFiltroEstado.SelectedItem?.ToString() ?? "Todos";

                // Obtener filtros de ciudad y radio (siempre activos si estan seleccionados)
                string ciudadFiltro = _filtroCiudadActual;
                string radioFiltro = _filtroRadioActual;

                // Generar clave de caché
                string claveCache = $"{ciudadFiltro ?? ""}|{radioFiltro ?? ""}|{estadoFiltro}|{textoBusqueda ?? ""}|{offset}|{_registrosPorPagina}";

                System.Diagnostics.Debug.WriteLine($"[TIMING] === Iniciando carga: Ciudad={ciudadFiltro ?? "(todas)"}, Radio={radioFiltro ?? "(todas)"}, Estado={estadoFiltro} ===");

                DataTable tableData = null;
                bool usarCache = !forzarRecarga &&
                                 _cacheDatosCiudadRadio.ContainsKey(claveCache) &&
                                 (DateTime.Now - _ultimaActualizacionCache).TotalMinutes < CACHE_EXPIRACION_MINUTOS;

                swParcial.Restart();
                if (usarCache)
                {
                    // Usar datos en caché
                    tableData = _cacheDatosCiudadRadio[claveCache];
                    swParcial.Stop();
                    System.Diagnostics.Debug.WriteLine($"[TIMING] Datos obtenidos de CACHÉ: {swParcial.ElapsedMilliseconds} ms ({tableData.Rows.Count} filas)");
                }
                else
                {
                    // Cargar datos agrupados desde la base de datos
                    tableData = await DataAccess.CargarComercialesAgrupadosAsync(
                        DatabaseConfig.ConnectionString,
                        DatabaseConfig.TableName,
                        _registrosPorPagina,
                        offset,
                        string.IsNullOrEmpty(textoBusqueda) ? null : textoBusqueda,
                        estadoFiltro,
                        ciudadFiltro,
                        radioFiltro);
                    swParcial.Stop();
                    System.Diagnostics.Debug.WriteLine($"[TIMING] Consulta BD: {swParcial.ElapsedMilliseconds} ms ({tableData.Rows.Count} filas)");

                    // Guardar en caché
                    _cacheDatosCiudadRadio[claveCache] = tableData;
                    _ultimaActualizacionCache = DateTime.Now;
                }

                // Suspender layout
                _cargandoDatos = true;
                dgv_base.SuspendLayout();
                dgv_base.Rows.Clear();

                swParcial.Restart();
                foreach (DataRow row in tableData.Rows)
                {
                    // Formatear posición con prefijo P si es solo números
                    string posicion = row["Posicion"].ToString();
                    if (!string.IsNullOrEmpty(posicion) && !posicion.StartsWith("P"))
                    {
                        posicion = "P" + posicion;
                    }

                    dgv_base.Rows.Add(
                        row["FilePath"].ToString(),
                        row["CodigoNumerico"].ToString(),
                        row["NombreArchivo"].ToString(),
                        row["TotalRegistros"].ToString(),
                        row["Ciudad"].ToString(),
                        row["Radio"].ToString(),
                        Convert.ToDateTime(row["FechaMinima"]).ToString("dd/MM/yyyy"),
                        Convert.ToDateTime(row["FechaMaxima"]).ToString("dd/MM/yyyy"),
                        row["EstadoGeneral"].ToString(),
                        posicion
                    );
                }

                dgv_base.ResumeLayout();
                swParcial.Stop();
                System.Diagnostics.Debug.WriteLine($"[TIMING] Renderizar grid principal: {swParcial.ElapsedMilliseconds} ms");

                ActualizarControlesPaginacion();

                // Si hay filas, seleccionar la primera y cargar sus pautas
                // Mantenemos _cargandoDatos = true para evitar que SelectionChanged se dispare
                if (dgv_base.Rows.Count > 0)
                {
                    dgv_base.ClearSelection();
                    dgv_base.Rows[0].Selected = true;
                    if (dgv_base.Columns.Contains("Codigo"))
                        dgv_base.CurrentCell = dgv_base.Rows[0].Cells["Codigo"];
                }

                // Ahora sí permitimos que SelectionChanged funcione
                _cargandoDatos = false;

                // Cargar las pautas de la fila seleccionada
                if (dgv_base.Rows.Count > 0)
                {
                    swParcial.Restart();
                    await CargarRegistrosDelArchivoSeleccionadoAsync();
                    swParcial.Stop();
                    System.Diagnostics.Debug.WriteLine($"[TIMING] Cargar pautas del archivo seleccionado: {swParcial.ElapsedMilliseconds} ms");
                }

                // Actualizar info de filtros si están activos
                if (!string.IsNullOrEmpty(ciudadFiltro) || !string.IsNullOrEmpty(radioFiltro))
                {
                    string filtroInfo = "";
                    if (!string.IsNullOrEmpty(radioFiltro)) filtroInfo += radioFiltro;
                    if (!string.IsNullOrEmpty(ciudadFiltro)) filtroInfo += (filtroInfo.Length > 0 ? " - " : "") + ciudadFiltro;
                    lblUsuarioActual.Text = $"Filtro: {filtroInfo} ({dgv_base.Rows.Count} archivos)";
                }

                swTotal.Stop();
                System.Diagnostics.Debug.WriteLine($"[TIMING] === TIEMPO TOTAL de carga: {swTotal.ElapsedMilliseconds} ms (Fuente: {(usarCache ? "CACHÉ" : "BD")}) ===");
            }
            catch (Exception ex)
            {
                swTotal.Stop();
                System.Diagnostics.Debug.WriteLine($"[TIMING] Error después de {swTotal.ElapsedMilliseconds} ms: {ex.Message}");
                MessageBox.Show($"Error al cargar datos: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }

        /// <summary>
        /// Invalida la caché de datos, forzando recarga en la próxima consulta
        /// </summary>
        private void InvalidarCache()
        {
            _cacheDatosCiudadRadio.Clear();
            _ultimaActualizacionCache = DateTime.MinValue;
            System.Diagnostics.Debug.WriteLine("Caché de datos invalidada");
        }

        private void ConfigurarMenuContextualDGVBase()
        {
            ContextMenuStrip menuContextual = new ContextMenuStrip();

            // Opción: Generar todo el año de la ciudad/radio
            ToolStripMenuItem generarTodoMenuItem = new ToolStripMenuItem("Generar Todo (Ciudad/Radio completa)");
            generarTodoMenuItem.Click += BtnGenerarTodo_Click;
            menuContextual.Items.Add(generarTodoMenuItem);

            menuContextual.Items.Add(new ToolStripSeparator());

            ToolStripMenuItem editarMenuItem = new ToolStripMenuItem("Editar Comercial");
            editarMenuItem.Click += (s, e) => {
                if (dgv_base.SelectedRows.Count > 0)
                {
                    dgv_base_CellDoubleClick(dgv_base, new DataGridViewCellEventArgs(0, dgv_base.SelectedRows[0].Index));
                }
            };
            menuContextual.Items.Add(editarMenuItem);

            dgv_base.ContextMenuStrip = menuContextual;
        }

        private async void BtnEliminarSpot_Click(object sender, EventArgs e)
        {
            if (dgv_base.SelectedRows.Count == 0)
            {
                MessageBox.Show("Seleccione un comercial para eliminar.", "Aviso",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Obtener datos del comercial seleccionado
            var selectedRow = dgv_base.SelectedRows[0];
            int rowIndex = selectedRow.Index;
            string filePath = selectedRow.Cells["FilePath"].Value?.ToString();
            string codigoNumerico = selectedRow.Cells["Codigo"].Value?.ToString();
            string nombreArchivo = selectedRow.Cells["NombreArchivo"].Value?.ToString();
            string ciudad = selectedRow.Cells["Ciudad"].Value?.ToString();
            string radio = selectedRow.Cells["Radio"].Value?.ToString();
            string tandas = selectedRow.Cells["TotalRegistros"].Value?.ToString();

            if (string.IsNullOrEmpty(filePath))
            {
                MessageBox.Show("No se pudo obtener la información del comercial.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Confirmar eliminación
            var result = MessageBox.Show(
                $"¿Está seguro que desea eliminar el comercial?\n\nArchivo: {nombreArchivo}\nCódigo: {codigoNumerico}\nCiudad: {ciudad}\nRadio: {radio}\nTandas: {tandas}\n\nEsta acción eliminará TODAS las tandas de este comercial y regenerará los archivos de pautas.",
                "Confirmar Eliminación",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result != DialogResult.Yes)
                return;

            // Crear formulario de progreso
            var formProgreso = new FormProgresoGeneracion();
            formProgreso.CambiarTitulo("Eliminando Comercial");
            formProgreso.CambiarColorTema(Color.FromArgb(244, 67, 54)); // Rojo para eliminación
            formProgreso.ActualizarInfo($"{nombreArchivo} - {ciudad} / {radio}");
            formProgreso.Show(this);

            try
            {
                this.Cursor = Cursors.WaitCursor;

                // Paso 1: Obtener fechas (10%)
                formProgreso.ActualizarProgreso(10, "Obteniendo información del comercial...");

                DateTime? fechaInicio = null;
                DateTime? fechaFinal = null;

                using (var conn = new Npgsql.NpgsqlConnection(DatabaseConfig.ConnectionString))
                {
                    await conn.OpenAsync();
                    string queryFechas = @"SELECT FechaInicio, FechaFinal FROM Comerciales
                                          WHERE FilePath = @FilePath AND Ciudad = @Ciudad AND Radio = @Radio
                                          LIMIT 1";
                    using (var cmd = new Npgsql.NpgsqlCommand(queryFechas, conn))
                    {
                        cmd.Parameters.AddWithValue("@FilePath", filePath);
                        cmd.Parameters.AddWithValue("@Ciudad", ciudad);
                        cmd.Parameters.AddWithValue("@Radio", radio);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                fechaInicio = reader.GetDateTime(0);
                                fechaFinal = reader.GetDateTime(1);
                            }
                        }
                    }
                }

                // Paso 2: Eliminar de BD (30%)
                formProgreso.ActualizarProgreso(30, "Eliminando de la base de datos...");

                await DataAccess.EliminarComercialesPorFilePathAsync(
                    DatabaseConfig.ConnectionString,
                    filePath,
                    ciudad,
                    radio);

                // Paso 3: Actualizar grid inmediatamente (40%)
                formProgreso.ActualizarProgreso(40, "Actualizando lista de comerciales...");

                // Remover la fila del grid inmediatamente para feedback visual rápido
                if (rowIndex >= 0 && rowIndex < dgv_base.Rows.Count)
                {
                    dgv_base.Rows.RemoveAt(rowIndex);
                }
                _totalRegistros = Math.Max(0, _totalRegistros - 1);
                ActualizarControlesPaginacion();

                // Paso 4: Regenerar archivos TXT (40% - 95%)
                if (fechaInicio.HasValue && fechaFinal.HasValue && !string.IsNullOrEmpty(ciudad) && !string.IsNullOrEmpty(radio))
                {
                    Logger.Log($"[ELIMINAR] Iniciando regeneración TXT para {ciudad}/{radio} ({fechaInicio.Value:dd/MM/yyyy} - {fechaFinal.Value:dd/MM/yyyy})");
                    formProgreso.ActualizarProgreso(45, "Regenerando archivos de pautas...");

                    var generador = new GenerarPauta();
                    int totalDias = (int)(fechaFinal.Value - fechaInicio.Value).TotalDays + 1;

                    var progreso = new Progress<(int porcentaje, string mensaje)>(p => {
                        // Mapear el progreso de regeneración al rango 45-95%
                        int progresoMapeado = 45 + (int)(p.porcentaje * 0.5);
                        formProgreso.ActualizarProgreso(progresoMapeado, p.mensaje);
                    });

                    int archivosRegenerados = await generador.RegenerarArchivosParaRangoAsync(fechaInicio.Value, fechaFinal.Value, ciudad, radio, progreso);
                    Logger.Log($"[ELIMINAR] Regeneración completada: {archivosRegenerados} archivos");
                }
                else
                {
                    Logger.Log($"[ELIMINAR] No se regeneraron TXT - fechaInicio={fechaInicio}, fechaFinal={fechaFinal}, ciudad={ciudad}, radio={radio}");
                }

                // Paso 5: Completado (100%)
                formProgreso.ActualizarProgreso(100, "¡Eliminación completada!");
                formProgreso.CambiarTitulo("Eliminación Completada");
                formProgreso.CambiarColorTema(Color.FromArgb(76, 175, 80)); // Verde para éxito

                await Task.Delay(800); // Mostrar mensaje de éxito brevemente

                // Invalidar caché ya que los datos han cambiado
                InvalidarCache();

                // Recargar datos completos en segundo plano
                await MostrarInfoRegistrosAsync();
                await CargarDatosAgrupadosAsync(true); // Forzar recarga
            }
            catch (Exception ex)
            {
                formProgreso.Close();
                MessageBox.Show($"Error al eliminar el comercial: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                formProgreso.Close();
                this.Cursor = Cursors.Default;
            }
        }

        private async Task IrAPaginaAsync(int pagina)
        {
            int totalPaginas = (int)Math.Ceiling((double)_totalRegistros / _registrosPorPagina);
            if (totalPaginas == 0) totalPaginas = 1;

            // Validar limites
            if (pagina < 1) pagina = 1;
            if (pagina > totalPaginas) pagina = totalPaginas;

            _paginaActual = pagina;
            await CargarPaginaAsync();
        }

        private async Task CargarPaginaAsync()
        {
            try
            {
                this.Cursor = Cursors.WaitCursor;
                lblUsuarioActual.Text = "Cargando...";
                Application.DoEvents();

                int offset = (_paginaActual - 1) * _registrosPorPagina;

                // Obtener filtros actuales
                string textoBusqueda = txtBusqueda.Text.Trim();
                string estadoFiltro = cboFiltroEstado?.SelectedItem?.ToString() ?? "Todos";
                string ciudadFiltro = _filtroCiudadActual;
                string radioFiltro = _filtroRadioActual;

                // Cargar datos agrupados con paginacion (usando el mismo metodo que CargarDatosAgrupadosAsync)
                var tableData = await DataAccess.CargarComercialesAgrupadosAsync(
                    DatabaseConfig.ConnectionString,
                    DatabaseConfig.TableName,
                    _registrosPorPagina,
                    offset,
                    string.IsNullOrEmpty(textoBusqueda) ? null : textoBusqueda,
                    estadoFiltro,
                    ciudadFiltro,
                    radioFiltro);

                // Suspender layout y evitar disparos de SelectionChanged
                _cargandoDatos = true;
                dgv_base.SuspendLayout();
                dgv_base.Rows.Clear();

                foreach (DataRow row in tableData.Rows)
                {
                    // Formatear posición con prefijo P si es solo números
                    string posicion = row["Posicion"].ToString();
                    if (!string.IsNullOrEmpty(posicion) && !posicion.StartsWith("P"))
                    {
                        posicion = "P" + posicion;
                    }

                    dgv_base.Rows.Add(
                        row["FilePath"].ToString(),
                        row["CodigoNumerico"].ToString(),
                        row["NombreArchivo"].ToString(),
                        row["TotalRegistros"].ToString(),
                        row["Ciudad"].ToString(),
                        row["Radio"].ToString(),
                        Convert.ToDateTime(row["FechaMinima"]).ToString("dd/MM/yyyy"),
                        Convert.ToDateTime(row["FechaMaxima"]).ToString("dd/MM/yyyy"),
                        row["EstadoGeneral"].ToString(),
                        posicion
                    );
                }

                dgv_base.ResumeLayout();

                // Actualizar controles de paginacion
                ActualizarControlesPaginacion();

                // Si hay filas, asegurar que la primera esté seleccionada
                // Mantenemos _cargandoDatos = true para evitar duplicación
                if (dgv_base.Rows.Count > 0 && dgv_base.SelectedRows.Count == 0)
                {
                    dgv_base.Rows[0].Selected = true;
                    if (dgv_base.Columns.Contains("Codigo"))
                        dgv_base.CurrentCell = dgv_base.Rows[0].Cells["Codigo"];
                }

                // Ahora permitimos SelectionChanged
                _cargandoDatos = false;

                // Cargar pautas si hay filas seleccionadas
                if (dgv_base.SelectedRows.Count > 0)
                {
                    await CargarRegistrosDelArchivoSeleccionadoAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error cargando pagina: {ex.Message}");
                MessageBox.Show($"Error al cargar datos: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }

        private void ActualizarControlesPaginacion()
        {
            int totalPaginas = (int)Math.Ceiling((double)_totalRegistros / _registrosPorPagina);
            if (totalPaginas == 0) totalPaginas = 1;

            // Formato compacto para 1366x768
            lblPaginaActual.Text = $"{_paginaActual}/{totalPaginas} ({_totalRegistros:N0})";

            // Habilitar/deshabilitar botones
            btnPrimero.Enabled = _paginaActual > 1;
            btnAnterior.Enabled = _paginaActual > 1;
            btnSiguiente.Enabled = _paginaActual < totalPaginas;
            btnUltimo.Enabled = _paginaActual < totalPaginas;

            // Cambiar color segun estado
            btnPrimero.BackColor = btnPrimero.Enabled ? Color.FromArgb(63, 81, 181) : Color.Gray;
            btnAnterior.BackColor = btnAnterior.Enabled ? Color.FromArgb(63, 81, 181) : Color.Gray;
            btnSiguiente.BackColor = btnSiguiente.Enabled ? Color.FromArgb(63, 81, 181) : Color.Gray;
            btnUltimo.BackColor = btnUltimo.Enabled ? Color.FromArgb(63, 81, 181) : Color.Gray;

            // Actualizar label de usuario
            string infoUsuario = UserManager.HayUsuarioLogueado
                ? UserManager.UsuarioActual.NombreCompleto
                : "Sin sesion";
            lblUsuarioActual.Text = $"{infoUsuario} | Pag. {_paginaActual}/{totalPaginas}";
        }

        private void MostrarControlesPaginacion(bool visible)
        {
            if (btnPrimero != null) btnPrimero.Visible = visible;
            if (btnAnterior != null) btnAnterior.Visible = visible;
            if (lblPaginaActual != null) lblPaginaActual.Visible = visible;
            if (btnSiguiente != null) btnSiguiente.Visible = visible;
            if (btnUltimo != null) btnUltimo.Visible = visible;
        }

        private async void BtnGenerarTodo_Click(object sender, EventArgs e)
        {
            // Verificar que hay ciudad y radio seleccionadas
            string ciudad = _filtroCiudadActual;
            string radio = _filtroRadioActual;

            if (string.IsNullOrEmpty(ciudad) || string.IsNullOrEmpty(radio))
            {
                MessageBox.Show("Por favor, seleccione una ESTACIÓN y una CIUDAD en los paneles inferiores para generar todas las pautas.",
                    "Selección requerida", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Confirmar la acción
            var confirmResult = MessageBox.Show(
                $"Se generarán TODAS las pautas para:\n\n" +
                $"  Estación: {radio}\n" +
                $"  Ciudad: {ciudad}\n\n" +
                $"Esto incluirá todos los comerciales activos con sus rangos de fechas.\n\n" +
                $"¿Desea continuar?",
                "Confirmar Generación Masiva",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirmResult != DialogResult.Yes)
                return;

            try
            {
                btnGenerarTanda.Enabled = false;
                this.Cursor = Cursors.WaitCursor;

                // Crear y mostrar formulario de progreso
                var formProgreso = new FormProgresoGeneracion();
                formProgreso.Show(this);
                Application.DoEvents();

                // Obtener rango de fechas de todos los comerciales activos para esta ciudad/radio
                var (fechaMinima, fechaMaxima) = await ObtenerRangoFechasComercialesAsync(ciudad, radio);

                if (fechaMinima == DateTime.MinValue || fechaMaxima == DateTime.MinValue)
                {
                    formProgreso.Close();
                    MessageBox.Show($"No se encontraron comerciales activos para {ciudad} - {radio}.",
                        "Sin comerciales", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                formProgreso.ActualizarInfo($"Generando pautas del {fechaMinima:dd/MM/yyyy} al {fechaMaxima:dd/MM/yyyy}");

                // Crear instancia del generador
                var generador = new GenerarPauta();

                // Crear progress reporter
                var progress = new Progress<(int porcentaje, string mensaje)>(info =>
                {
                    formProgreso.ActualizarProgreso(info.porcentaje, info.mensaje);
                    Application.DoEvents();
                });

                // Generar todas las pautas
                int archivosGenerados = await generador.RegenerarArchivosParaRangoAsync(
                    fechaMinima,
                    fechaMaxima,
                    ciudad,
                    radio,
                    progress);

                formProgreso.Close();

                // Mostrar resultado
                string rutaPautas = System.IO.Path.Combine(
                    ConfigManager.ObtenerRutaBasePautas(),
                    ciudad.ToUpper(),
                    radio.ToUpper());

                MessageBox.Show(
                    $"Generación completada exitosamente.\n\n" +
                    $"Archivos generados: {archivosGenerados}\n" +
                    $"Rango: {fechaMinima:dd/MM/yyyy} - {fechaMaxima:dd/MM/yyyy}\n" +
                    $"Ubicación: {rutaPautas}",
                    "Generación Completa",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al generar las pautas: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnGenerarTanda.Enabled = true;
                this.Cursor = Cursors.Default;
                await MostrarInfoRegistrosAsync();
            }
        }

        /// <summary>
        /// Obtiene el rango de fechas (mínima y máxima) de todos los comerciales activos
        /// para una ciudad y radio específicas.
        /// </summary>
        private async Task<(DateTime fechaMinima, DateTime fechaMaxima)> ObtenerRangoFechasComercialesAsync(string ciudad, string radio)
        {
            DateTime fechaMinima = DateTime.MinValue;
            DateTime fechaMaxima = DateTime.MinValue;

            try
            {
                using (var conn = new Npgsql.NpgsqlConnection(DatabaseConfig.ConnectionString))
                {
                    await conn.OpenAsync();

                    string query = @"
                        SELECT MIN(FechaInicio) as FechaMin, MAX(FechaFinal) as FechaMax
                        FROM Comerciales
                        WHERE LOWER(Ciudad) = LOWER(@Ciudad)
                          AND LOWER(Radio) = LOWER(@Radio)
                          AND Estado = 'Activo'";

                    using (var cmd = new Npgsql.NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Ciudad", ciudad);
                        cmd.Parameters.AddWithValue("@Radio", radio);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                if (reader["FechaMin"] != DBNull.Value)
                                    fechaMinima = Convert.ToDateTime(reader["FechaMin"]);
                                if (reader["FechaMax"] != DBNull.Value)
                                    fechaMaxima = Convert.ToDateTime(reader["FechaMax"]);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error obteniendo rango de fechas: {ex.Message}");
            }

            return (fechaMinima, fechaMaxima);
        }

        /// <summary>
        /// Genera pautas para TODAS las ciudades y radios que tengan comerciales activos.
        /// </summary>
        private async void BtnGenerarGlobal_Click(object sender, EventArgs e)
        {
            // Confirmar la acción
            var confirmResult = MessageBox.Show(
                "Se generarán TODAS las pautas para TODAS las ciudades y radios con comerciales activos.\n\n" +
                "Este proceso puede tomar varios minutos dependiendo de la cantidad de datos.\n\n" +
                "¿Desea continuar?",
                "Confirmar Generación Global",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirmResult != DialogResult.Yes)
                return;

            try
            {
                btnGenerarTanda.Enabled = false;
                btnGenerarGlobal.Enabled = false;
                this.Cursor = Cursors.WaitCursor;

                // Crear y mostrar formulario de progreso
                var formProgreso = new FormProgresoGeneracion();
                formProgreso.CambiarTitulo("Generación Global de Pautas");
                formProgreso.CambiarColorTema(Color.FromArgb(33, 150, 243)); // Azul
                formProgreso.Show(this);
                Application.DoEvents();

                // Obtener todas las combinaciones únicas de ciudad/radio con comerciales activos
                var combinaciones = await ObtenerCombinacionesCiudadRadioAsync();

                if (combinaciones.Count == 0)
                {
                    formProgreso.Close();
                    MessageBox.Show("No se encontraron comerciales activos en ninguna ciudad/radio.",
                        "Sin comerciales", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                formProgreso.ActualizarInfo($"Procesando {combinaciones.Count} combinaciones de ciudad/radio");

                int totalArchivosGenerados = 0;
                int combinacionesProcesadas = 0;

                // Crear instancia del generador
                var generador = new GenerarPauta();

                foreach (var (ciudad, radio) in combinaciones)
                {
                    combinacionesProcesadas++;
                    int porcentaje = (int)((combinacionesProcesadas * 100.0) / combinaciones.Count);

                    formProgreso.ActualizarProgreso(porcentaje, $"Generando: {ciudad} - {radio}");
                    Application.DoEvents();

                    try
                    {
                        // Obtener rango de fechas para esta ciudad/radio
                        var (fechaMinima, fechaMaxima) = await ObtenerRangoFechasComercialesAsync(ciudad, radio);

                        if (fechaMinima != DateTime.MinValue && fechaMaxima != DateTime.MinValue)
                        {
                            // Generar pautas sin mostrar progreso individual
                            int archivos = await generador.RegenerarArchivosParaRangoAsync(
                                fechaMinima,
                                fechaMaxima,
                                ciudad,
                                radio,
                                null); // Sin progress individual para no interferir

                            totalArchivosGenerados += archivos;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error generando pautas para {ciudad}/{radio}: {ex.Message}");
                        // Continuar con la siguiente combinación
                    }
                }

                formProgreso.MostrarCompletadoGenerico($"Completado - {totalArchivosGenerados} archivos en {combinaciones.Count} radios");
                await Task.Delay(1500); // Mostrar mensaje de completado brevemente
                formProgreso.Close();

                // Mostrar resultado
                MessageBox.Show(
                    $"Generación global completada exitosamente.\n\n" +
                    $"Radios procesadas: {combinaciones.Count}\n" +
                    $"Archivos generados: {totalArchivosGenerados}\n" +
                    $"Ubicación: {ConfigManager.ObtenerRutaBasePautas()}",
                    "Generación Global Completa",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al generar las pautas globales: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnGenerarTanda.Enabled = true;
                btnGenerarGlobal.Enabled = true;
                this.Cursor = Cursors.Default;
            }
        }

        /// <summary>
        /// Obtiene todas las combinaciones únicas de Ciudad/Radio que tienen comerciales activos.
        /// </summary>
        private async Task<List<(string Ciudad, string Radio)>> ObtenerCombinacionesCiudadRadioAsync()
        {
            var combinaciones = new List<(string Ciudad, string Radio)>();

            try
            {
                using (var conn = new Npgsql.NpgsqlConnection(DatabaseConfig.ConnectionString))
                {
                    await conn.OpenAsync();

                    string query = @"
                        SELECT DISTINCT Ciudad, Radio
                        FROM Comerciales
                        WHERE Estado = 'Activo'
                          AND Ciudad IS NOT NULL AND Ciudad != ''
                          AND Radio IS NOT NULL AND Radio != ''
                        ORDER BY Ciudad, Radio";

                    using (var cmd = new Npgsql.NpgsqlCommand(query, conn))
                    {
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                string ciudad = reader["Ciudad"]?.ToString() ?? "";
                                string radio = reader["Radio"]?.ToString() ?? "";

                                if (!string.IsNullOrEmpty(ciudad) && !string.IsNullOrEmpty(radio))
                                {
                                    combinaciones.Add((ciudad, radio));
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error obteniendo combinaciones ciudad/radio: {ex.Message}");
            }

            return combinaciones;
        }

        private void ConfigurarPermisosPorRol()
        {
            if (UserManager.HayUsuarioLogueado)
            {
                // Actualizar label de usuario
                lblUsuarioActual.Text = $"Usuario: {UserManager.UsuarioActual.NombreCompleto} ({UserManager.UsuarioActual.Rol})";
                lblUsuarioActual.Location = new Point(this.ClientSize.Width - lblUsuarioActual.Width - 20, 5);

                // Mostrar/ocultar menu de administracion segun el rol
                menuAdministracion.Visible = UserManager.EsAdministrador;

                // Actualizar titulo del formulario
                this.Text = $"Generador de Pautas - {UserManager.UsuarioActual.NombreCompleto}";
            }
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x02000000; // WS_EX_COMPOSITED - evita parpadeo
                return cp;
            }
        }

        private async void dgv_base_SelectionChanged(object sender, EventArgs e)
        {
            // Evitar disparos multiples durante carga de datos
            if (_cargandoDatos) return;

            // Vista siempre agrupada - cargar registros del archivo seleccionado
            await CargarRegistrosDelArchivoSeleccionadoAsync();
        }

        /// <summary>
        /// Carga todas las combinaciones FECHA + HORA de un archivo seleccionado (como el sistema antiguo)
        /// </summary>
        private async Task CargarRegistrosDelArchivoSeleccionadoAsync()
        {
            try
            {
                // Limpiar pautas anteriores
                dgv_pautas.DataSource = null;
                dgv_pautas.Rows.Clear();
                dgv_pautas.Columns.Clear();

                if (dgv_base.SelectedRows.Count == 0)
                {
                    lblPautasTitulo.Text = "Seleccione un archivo para ver sus pauteos";
                    return;
                }

                // Obtener el FilePath del archivo seleccionado usando nombres de columna
                string filePath = dgv_base.SelectedRows[0].Cells["FilePath"]?.Value?.ToString();
                string nombreArchivo = dgv_base.SelectedRows[0].Cells["NombreArchivo"]?.Value?.ToString();

                if (string.IsNullOrEmpty(filePath))
                {
                    return;
                }

                // Configurar columnas para mostrar FECHA + HORA (como el sistema antiguo)
                dgv_pautas.Columns.Add("Fecha", "Fecha");
                dgv_pautas.Columns.Add("Hora", "Hora");

                dgv_pautas.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                dgv_pautas.Columns["Fecha"].FillWeight = 50;
                dgv_pautas.Columns["Hora"].FillWeight = 50;

                // Estilos
                dgv_pautas.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(63, 81, 181);
                dgv_pautas.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
                dgv_pautas.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold);
                dgv_pautas.DefaultCellStyle.Font = new Font("Segoe UI", 8.5F);
                dgv_pautas.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(245, 248, 255);

                // Cargar las combinaciones FECHA + HORA desde la BD
                // Usar la ciudad y radio del comercial seleccionado (no el filtro global)
                string ciudadDelComercial = dgv_base.SelectedRows[0].Cells["Ciudad"]?.Value?.ToString();
                string radioDelComercial = dgv_base.SelectedRows[0].Cells["Radio"]?.Value?.ToString();

                var tableData = await DataAccess.ObtenerFechasHorasPorFilePathAsync(
                    DatabaseConfig.ConnectionString,
                    filePath,
                    ciudadDelComercial,
                    radioDelComercial);

                // Si no hay datos, mostrar información de diagnóstico
                if (tableData.Rows.Count == 0)
                {
                    Logger.Log($"FORM1 - CargarRegistros - No hay pautas para FilePath={filePath}");
                    Logger.Log($"FORM1 - CargarRegistros - Filtros: Ciudad={ciudadDelComercial}, Radio={radioDelComercial}");

                    // Buscar cuántos comerciales hay para este FilePath (sin filtro de ciudad/radio)
                    int totalEnBD = await ContarComercialesParaFilePathAsync(filePath);
                    Logger.Log($"FORM1 - CargarRegistros - Total en BD sin filtros: {totalEnBD}");

                    string debugInfo = totalEnBD > 0
                        ? $"BD tiene {totalEnBD} comerciales pero no coinciden los filtros Ciudad='{ciudadDelComercial}' Radio='{radioDelComercial}'"
                        : $"No hay comerciales en BD para este archivo";

                    lblPautasTitulo.Text = debugInfo;
                    lblPautasTitulo.Visible = true;
                    lblTotalPautas.Text = "0 pautas programadas";
                    lblConteoHoras.Text = "(0 horarios)";
                    cboHoraElim.Items.Clear();
                    return;
                }

                Logger.Log($"FORM1 - CargarRegistros - Pautas encontradas: {tableData.Rows.Count}");

                // Recopilar horas únicas que se muestran en dgv_pautas para comparar
                var horasEnDgv = new SortedSet<string>();

                foreach (DataRow row in tableData.Rows)
                {
                    DateTime fecha = Convert.ToDateTime(row["Fecha"]);
                    string hora = row["Hora"].ToString();
                    horasEnDgv.Add(hora);

                    dgv_pautas.Rows.Add(
                        fecha.ToString("dd/MM/yyyy"),
                        hora
                    );
                }

                Logger.Log($"FORM1 - CargarRegistros - Horas únicas en dgv_pautas: {horasEnDgv.Count}");
                Logger.Log($"FORM1 - CargarRegistros - Horas en dgv: {string.Join(", ", horasEnDgv)}");

                // Truncar nombre si es muy largo para que quepa
                string nombreMostrar = nombreArchivo ?? "Sin nombre";
                if (nombreMostrar.Length > 45)
                {
                    nombreMostrar = nombreMostrar.Substring(0, 42) + "...";
                }
                lblPautasTitulo.Text = $"{nombreMostrar} ({tableData.Rows.Count})";
                lblPautasTitulo.Visible = true;

                // Llenar el código en el panel de eliminación y mostrar total
                string codigoComercial = dgv_base.SelectedRows[0].Cells["Codigo"]?.Value?.ToString();
                string ciudadSeleccionada = dgv_base.SelectedRows[0].Cells["Ciudad"]?.Value?.ToString() ?? "";
                string radioSeleccionada = dgv_base.SelectedRows[0].Cells["Radio"]?.Value?.ToString() ?? "";

                if (!string.IsNullOrEmpty(codigoComercial))
                {
                    txtCodigoEliminar.Text = codigoComercial;
                    lblTotalPautas.Text = $"{tableData.Rows.Count} pautas programadas";

                    // Extraer las fechas y horas únicas directamente de los datos ya cargados
                    cboHoraElim.Items.Clear();
                    cboHoraElim.Items.Add("(Todas)"); // Opción para eliminar todas las horas
                    var horasUnicas = new SortedSet<string>();
                    DateTime? fechaMin = null;
                    DateTime? fechaMax = null;

                    Logger.Log($"FORM1 - Panel Elim - Procesando {tableData.Rows.Count} filas para extraer horas");

                    foreach (DataRow row in tableData.Rows)
                    {
                        // Horas
                        string hora = row["Hora"].ToString();
                        if (!string.IsNullOrEmpty(hora))
                        {
                            horasUnicas.Add(hora);
                        }

                        // Fechas
                        DateTime fecha = Convert.ToDateTime(row["Fecha"]);
                        if (!fechaMin.HasValue || fecha < fechaMin.Value)
                            fechaMin = fecha;
                        if (!fechaMax.HasValue || fecha > fechaMax.Value)
                            fechaMax = fecha;
                    }

                    Logger.Log($"FORM1 - Panel Elim - Horas únicas encontradas: {horasUnicas.Count}");
                    Logger.Log($"FORM1 - Panel Elim - Horas: {string.Join(", ", horasUnicas)}");

                    // Actualizar DateTimePicker con las fechas de las pautas
                    if (fechaMin.HasValue && fechaMax.HasValue)
                    {
                        dtpFechaElimI.Value = fechaMin.Value;
                        dtpFechaElimF.Value = fechaMax.Value;
                    }

                    foreach (string hora in horasUnicas)
                    {
                        cboHoraElim.Items.Add(hora);
                    }
                    if (cboHoraElim.Items.Count > 0)
                        cboHoraElim.SelectedIndex = 0;
                    lblConteoHoras.Text = $"({horasUnicas.Count} horarios)";
                }
                else
                {
                    lblTotalPautas.Text = "";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error cargando registros del archivo: {ex.Message}");
                lblPautasTitulo.Text = "Error al cargar registros";
            }
        }
        private async void dgv_base_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            // Obtenemos el índice de la fila seleccionada
            int rowIndex = e.RowIndex;

            Logger.LogSeparador();
            Logger.Log("FORM1 - DOBLE CLICK - Usuario hizo doble click en una fila");
            Logger.Log($"FORM1 - DOBLE CLICK - Índice de fila: {rowIndex}");

            // Comprobamos que el índice sea válido
            if (rowIndex >= 0)
            {
                // Vista siempre agrupada - el doble click carga datos en Pauteo Rápido
                DataGridViewRow selectedRow = dgv_base.Rows[rowIndex];
                string filePath = selectedRow.Cells["FilePath"].Value?.ToString();
                string ciudad = selectedRow.Cells["Ciudad"].Value?.ToString();
                string radio = selectedRow.Cells["Radio"].Value?.ToString();
                string codigoNumerico = selectedRow.Cells["Codigo"].Value?.ToString();

                Logger.Log($"FORM1 - DOBLE CLICK - FilePath: {filePath}");
                Logger.Log($"FORM1 - DOBLE CLICK - Ciudad: {ciudad}");
                Logger.Log($"FORM1 - DOBLE CLICK - Radio: {radio}");
                Logger.Log($"FORM1 - DOBLE CLICK - CodigoNumerico: {codigoNumerico}");

                if (!string.IsNullOrEmpty(filePath) || !string.IsNullOrEmpty(codigoNumerico))
                {
                    // Buscar el comercial con este codigo numerico, Ciudad y Radio para obtener sus datos
                    var datosComercial = await ObtenerDatosComercialAsync(filePath, ciudad, radio, codigoNumerico);

                    if (datosComercial != null)
                    {
                        Logger.Log($"FORM1 - DOBLE CLICK - Datos encontrados: Codigo={datosComercial.Codigo}");

                        // Cargar datos en el panel de Pauteo Rápido para edición
                        if (pauteoRapidoPanel != null)
                        {
                            pauteoRapidoPanel.CargarComercialParaEdicion(datosComercial);

                            // Obtener las tandas asignadas y marcarlas
                            var tandasAsignadas = await ObtenerTandasAsignadasParaEdicionAsync(datosComercial.Codigo, datosComercial.TipoProgramacion);
                            pauteoRapidoPanel.MarcarTandasAsignadas(tandasAsignadas);

                            // Obtener los días asignados y marcarlos
                            var diasAsignados = await ObtenerDiasAsignadosParaEdicionAsync(datosComercial.Codigo, datosComercial.FechaInicio, datosComercial.FechaFinal);
                            pauteoRapidoPanel.MarcarDiasAsignados(diasAsignados);

                            System.Diagnostics.Debug.WriteLine($"[FORM1] Comercial cargado en Pauteo Rápido: {datosComercial.Codigo}");
                        }
                    }
                    else
                    {
                        MessageBox.Show("No se encontraron datos del comercial en la base de datos.",
                            "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                else
                {
                    MessageBox.Show("No se pudo obtener la información del comercial.",
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        /// <summary>
        /// Obtiene las tandas asignadas para un comercial y las convierte a formato de hora
        /// </summary>
        private async Task<List<string>> ObtenerTandasAsignadasParaEdicionAsync(string codigo, string tipoProgramacion)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[FORM1] ObtenerTandasAsignadasParaEdicionAsync - Código: {codigo}, TipoProgramacion: {tipoProgramacion}");

                // Determinar el tipo de tanda
                TipoTanda tipo = TipoTanda.Tandas_00_30;
                if (!string.IsNullOrEmpty(tipoProgramacion))
                {
                    if (tipoProgramacion.Contains("10-40")) tipo = TipoTanda.Tandas_10_40;
                    else if (tipoProgramacion.Contains("15-45")) tipo = TipoTanda.Tandas_15_45;
                    else if (tipoProgramacion.Contains("20-50")) tipo = TipoTanda.Tandas_20_50;
                    else if (tipoProgramacion.Contains("00-20-30-50")) tipo = TipoTanda.Tandas_00_20_30_50;
                }

                System.Diagnostics.Debug.WriteLine($"[FORM1] Tipo de tanda detectado: {tipo}");

                var tandasHoras = new List<string>();

                // Para comerciales importados de Access (ACC-XXX-...-HHMM), las horas están en el código
                if (codigo.StartsWith("ACC-", StringComparison.OrdinalIgnoreCase))
                {
                    System.Diagnostics.Debug.WriteLine($"[FORM1] Detectado comercial Access, buscando horas en códigos...");

                    // Extraer la parte numérica del código
                    string codigoNumerico = "";
                    var partes = codigo.Split('-');
                    if (partes.Length >= 2 && int.TryParse(partes[1], out _))
                    {
                        codigoNumerico = partes[1];
                    }

                    // Buscar todos los códigos con el mismo número para extraer las horas
                    using (var conn = new Npgsql.NpgsqlConnection(DatabaseConfig.ConnectionString))
                    {
                        await conn.OpenAsync();

                        string query = @"SELECT DISTINCT Codigo
                                         FROM Comerciales
                                         WHERE split_part(Codigo, '-', 2) = @CodigoNumerico
                                         ORDER BY Codigo";

                        using (var cmd = new Npgsql.NpgsqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@CodigoNumerico", codigoNumerico);

                            using (var reader = await cmd.ExecuteReaderAsync())
                            {
                                while (await reader.ReadAsync())
                                {
                                    string codigoCompleto = reader.GetString(0);
                                    // Extraer hora del último segmento (ej: ACC-957-ABA-EXI-0015 -> 0015 -> 00:15)
                                    var partesCompleto = codigoCompleto.Split('-');
                                    if (partesCompleto.Length >= 5)
                                    {
                                        string horaStr = partesCompleto[partesCompleto.Length - 1];
                                        if (horaStr.Length == 4 && int.TryParse(horaStr, out _))
                                        {
                                            string hora = $"{horaStr.Substring(0, 2)}:{horaStr.Substring(2, 2)}";
                                            if (!tandasHoras.Contains(hora))
                                            {
                                                tandasHoras.Add(hora);
                                                System.Diagnostics.Debug.WriteLine($"[FORM1] Código {codigoCompleto} -> Hora {hora}");
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    // Para comerciales nuevos (CU-XXX o solo número), buscar en ComercialesAsignados
                    var filasAsignadas = await DataAccess.ObtenerTandasAsignadasAsync(
                        DatabaseConfig.ConnectionString, codigo);

                    System.Diagnostics.Debug.WriteLine($"[FORM1] Filas asignadas obtenidas: {filasAsignadas.Count} - Valores: {string.Join(", ", filasAsignadas.Take(10))}");

                    // Convertir los índices de fila a horas
                    var horarios = TandasHorarias.GetHorarios(tipo);

                    System.Diagnostics.Debug.WriteLine($"[FORM1] Horarios disponibles: {horarios.Length}");

                    foreach (var filaStr in filasAsignadas)
                    {
                        if (int.TryParse(filaStr, out int fila) && fila >= 0 && fila < horarios.Length)
                        {
                            string hora = horarios[fila];
                            if (!tandasHoras.Contains(hora))
                            {
                                tandasHoras.Add(hora);
                                System.Diagnostics.Debug.WriteLine($"[FORM1] Fila {fila} -> Hora {hora}");
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"[FORM1] Fila inválida: {filaStr} (max: {horarios.Length - 1})");
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[FORM1] Tandas horas resultado: {tandasHoras.Count} - {string.Join(", ", tandasHoras.Take(10))}");

                return tandasHoras;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FORM1] Error obteniendo tandas asignadas: {ex.Message}");
                return new List<string>();
            }
        }

        /// <summary>
        /// Obtiene los días de la semana que tienen pautas asignadas para un comercial
        /// </summary>
        private async Task<List<DayOfWeek>> ObtenerDiasAsignadosParaEdicionAsync(string codigo, DateTime fechaInicio, DateTime fechaFinal)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[FORM1] ObtenerDiasAsignadosParaEdicionAsync - Código: {codigo}");

                var diasUnicos = new HashSet<DayOfWeek>();

                // Para comerciales ACC, los días se determinan por el rango de fechas
                // ya que no hay registros individuales en ComercialesAsignados con fecha
                if (codigo.StartsWith("ACC-", StringComparison.OrdinalIgnoreCase))
                {
                    // Para comerciales importados de Access, asumimos todos los días del rango
                    // ya que no tienen información específica de días
                    for (DateTime fecha = fechaInicio; fecha <= fechaFinal; fecha = fecha.AddDays(1))
                    {
                        diasUnicos.Add(fecha.DayOfWeek);
                        // Si ya tenemos todos los días, salir
                        if (diasUnicos.Count == 7) break;
                    }

                    System.Diagnostics.Debug.WriteLine($"[FORM1] Días ACC (rango completo): {string.Join(", ", diasUnicos)}");
                }
                else
                {
                    // Para comerciales nuevos, buscar las fechas en ComercialesAsignados
                    using (var conn = new Npgsql.NpgsqlConnection(DatabaseConfig.ConnectionString))
                    {
                        await conn.OpenAsync();

                        // Extraer código numérico si es necesario
                        string codigoNumerico = codigo;
                        if (codigo.Contains("-"))
                        {
                            var partes = codigo.Split('-');
                            if (partes.Length >= 2 && int.TryParse(partes[1], out _))
                            {
                                codigoNumerico = partes[1];
                            }
                        }

                        string query = @"
                            SELECT DISTINCT Fecha
                            FROM ComercialesAsignados
                            WHERE Codigo = @Codigo
                               OR Codigo = @CodigoNumerico
                               OR split_part(Codigo, '-', 2) = @CodigoNumerico
                            ORDER BY Fecha";

                        using (var cmd = new Npgsql.NpgsqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@Codigo", codigo);
                            cmd.Parameters.AddWithValue("@CodigoNumerico", codigoNumerico);

                            using (var reader = await cmd.ExecuteReaderAsync())
                            {
                                while (await reader.ReadAsync())
                                {
                                    if (!reader.IsDBNull(0))
                                    {
                                        DateTime fecha = reader.GetDateTime(0);
                                        diasUnicos.Add(fecha.DayOfWeek);
                                    }
                                }
                            }
                        }
                    }

                    System.Diagnostics.Debug.WriteLine($"[FORM1] Días desde ComercialesAsignados: {string.Join(", ", diasUnicos)}");
                }

                var resultado = diasUnicos.ToList();
                System.Diagnostics.Debug.WriteLine($"[FORM1] Días resultado: {resultado.Count} - {string.Join(", ", resultado)}");

                return resultado;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FORM1] Error obteniendo días asignados: {ex.Message}");
                return new List<DayOfWeek>();
            }
        }

        /// <summary>
        /// Obtiene los datos de un comercial desde la BD basandose en FilePath, Ciudad, Radio y codigo numerico
        /// Busca primero en Comerciales (tabla principal) y si no encuentra, busca en ComercialesAsignados
        /// </summary>
        private async Task<AgregarComercialesData> ObtenerDatosComercialAsync(string filePath, string ciudad, string radio, string codigoNumerico = null)
        {
            try
            {
                using (var conn = new Npgsql.NpgsqlConnection(DatabaseConfig.ConnectionString))
                {
                    await conn.OpenAsync();

                    // Primero buscar en la tabla Comerciales (comerciales con codigo ACC-)
                    if (!string.IsNullOrEmpty(codigoNumerico))
                    {
                        string queryComerciales = @"SELECT Codigo, FilePath, FechaInicio, FechaFinal, Ciudad, Radio, Posicion, Estado, TipoProgramacion
                                FROM Comerciales
                                WHERE split_part(Codigo, '-', 2) = @CodigoNumerico
                                  AND Ciudad = @Ciudad
                                  AND Radio = @Radio
                                ORDER BY Codigo
                                LIMIT 1";

                        using (var cmd = new Npgsql.NpgsqlCommand(queryComerciales, conn))
                        {
                            cmd.Parameters.AddWithValue("@CodigoNumerico", codigoNumerico);
                            cmd.Parameters.AddWithValue("@Ciudad", ciudad ?? "");
                            cmd.Parameters.AddWithValue("@Radio", radio ?? "");

                            using (var reader = await cmd.ExecuteReaderAsync())
                            {
                                if (await reader.ReadAsync())
                                {
                                    return new AgregarComercialesData
                                    {
                                        Codigo = reader["Codigo"].ToString(),
                                        FilePath = reader["FilePath"].ToString(),
                                        FechaInicio = Convert.ToDateTime(reader["FechaInicio"]),
                                        FechaFinal = Convert.ToDateTime(reader["FechaFinal"]),
                                        Ciudad = reader["Ciudad"].ToString(),
                                        Radio = reader["Radio"].ToString(),
                                        Posicion = reader["Posicion"].ToString(),
                                        Estado = reader["Estado"].ToString(),
                                        TipoProgramacion = reader["TipoProgramacion"]?.ToString() ?? "Cada 00-30 (48 tandas)"
                                    };
                                }
                            }
                        }
                    }

                    // Si no encontramos en Comerciales, buscar en ComercialesAsignados (comerciales importados)
                    // Buscar primero por FilePath (más confiable) ya que el código puede tener diferentes formatos
                    if (!string.IsNullOrEmpty(filePath))
                    {
                        string queryAsignados = @"SELECT DISTINCT Codigo, Comercial as FilePath,
                                MIN(Fecha) as FechaInicio, MAX(Fecha) as FechaFinal,
                                Ciudad, Radio,
                                COALESCE(MAX(Posicion)::text, '1') as Posicion,
                                'Activo' as Estado,
                                TipoProgramacion
                            FROM ComercialesAsignados
                            WHERE LOWER(Comercial) = LOWER(@FilePath)
                              AND Ciudad = @Ciudad
                              AND Radio = @Radio
                            GROUP BY Codigo, Comercial, Ciudad, Radio, TipoProgramacion
                            LIMIT 1";

                        using (var cmd = new Npgsql.NpgsqlCommand(queryAsignados, conn))
                        {
                            cmd.Parameters.AddWithValue("@FilePath", filePath);
                            cmd.Parameters.AddWithValue("@Ciudad", ciudad ?? "");
                            cmd.Parameters.AddWithValue("@Radio", radio ?? "");

                            using (var reader = await cmd.ExecuteReaderAsync())
                            {
                                if (await reader.ReadAsync())
                                {
                                    return new AgregarComercialesData
                                    {
                                        Codigo = reader["Codigo"].ToString(),
                                        FilePath = reader["FilePath"].ToString(),
                                        FechaInicio = Convert.ToDateTime(reader["FechaInicio"]),
                                        FechaFinal = Convert.ToDateTime(reader["FechaFinal"]),
                                        Ciudad = reader["Ciudad"].ToString(),
                                        Radio = reader["Radio"].ToString(),
                                        Posicion = reader["Posicion"].ToString(),
                                        Estado = reader["Estado"].ToString(),
                                        TipoProgramacion = reader["TipoProgramacion"]?.ToString() ?? "Cada 00-30 (48 tandas)"
                                    };
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al obtener datos del comercial: {ex.Message}");
            }

            return null;
        }
        private async void Form1_Shown(object sender, EventArgs e)
        {
            // Marcar que estamos inicializando para evitar cargas dobles
            _inicializando = true;

            // Configurar permisos segun el rol del usuario
            ConfigurarPermisosPorRol();

            // Inicializar el ComboBox de filtro de estado (sin disparar eventos)
            cboFiltroEstado.Items.AddRange(new string[] { "Todos", "Activo", "Inactivo" });
            cboFiltroEstado.SelectedIndex = 1; // Seleccionar "Activo" por defecto (no mostrar vencidos)

            // Inicializar el ComboBox de filtro por columna
            cboFiltroColumna.Items.AddRange(new string[] { "Código", "Spot", "Ciudad", "Radio", "Estado" });
            cboFiltroColumna.SelectedIndex = 1; // Seleccionar "Spot" por defecto

            // Mostrar indicador de carga
            lblUsuarioActual.Text = "Cargando datos...";
            this.Cursor = Cursors.WaitCursor;
            Application.DoEvents();

            // Desactivar automáticamente los comerciales vencidos (FechaFinal < hoy)
            try
            {
                await DataAccess.DesactivarComercialesVencidosAsync(
                    DatabaseConfig.ConnectionString,
                    DatabaseConfig.TableName);
            }
            catch (Exception)
            {
                // Error al desactivar comerciales vencidos
            }

            // Configurar columnas (vista siempre agrupada)
            ConfigurarColumnasDgvBase();

            // NO cargar datos al inicio - esperar a que el usuario seleccione ciudad o estación
            dgv_base.Rows.Clear();
            _totalRegistros = 0;
            _totalArchivosUnicos = 0;
            ActualizarControlesPaginacion();

            // Cargar ciudades y estaciones desde la base de datos
            await CargarCiudadesAsync();
            await CargarEstacionesAsync();

            // Finalizar inicializacion - ahora los eventos pueden funcionar normalmente
            _inicializando = false;

            this.Cursor = Cursors.Default;
            lblUsuarioActual.Text = $"Usuario: {UserManager.UsuarioActual?.NombreCompleto ?? "Desconocido"} | Seleccione una ciudad o estación para ver datos";

            // Pre-cargar datos en segundo plano para acelerar cambios de ciudad/radio
            _ = PrecargarDatosEnSegundoPlanoAsync();
        }

        /// <summary>
        /// Pre-carga los datos de las combinaciones más comunes ciudad/radio en segundo plano
        /// para acelerar los cambios de selección
        /// </summary>
        private async Task PrecargarDatosEnSegundoPlanoAsync()
        {
            var swTotal = System.Diagnostics.Stopwatch.StartNew();
            var swParcial = new System.Diagnostics.Stopwatch();

            try
            {
                System.Diagnostics.Debug.WriteLine("[TIMING] ========== INICIANDO PRE-CARGA EN SEGUNDO PLANO ==========");

                // Obtener todas las ciudades y radios disponibles
                var ciudades = await AdminCiudadesForm.ObtenerCiudadesActivasAsync();
                var radios = await AdminRadiosForm.ObtenerRadiosActivasAsync();

                System.Diagnostics.Debug.WriteLine($"[TIMING] Pre-cargando {Math.Min(10, radios.Count)} radios y {Math.Min(10, ciudades.Count)} ciudades...");

                // Pre-cargar datos para cada radio (las más usadas)
                int contadorRadios = 0;
                foreach (string radio in radios.Take(10)) // Limitar a las primeras 10 radios
                {
                    string claveCache = $"|{radio}|Todos||0|{_registrosPorPagina}";
                    if (!_cacheDatosCiudadRadio.ContainsKey(claveCache))
                    {
                        swParcial.Restart();
                        var tableData = await DataAccess.CargarComercialesAgrupadosAsync(
                            DatabaseConfig.ConnectionString,
                            DatabaseConfig.TableName,
                            _registrosPorPagina,
                            0,
                            null,
                            "Todos",
                            null,
                            radio);

                        _cacheDatosCiudadRadio[claveCache] = tableData;
                        swParcial.Stop();
                        contadorRadios++;
                        System.Diagnostics.Debug.WriteLine($"[TIMING] Pre-cargado radio '{radio}': {swParcial.ElapsedMilliseconds} ms ({tableData.Rows.Count} filas)");
                    }
                }

                // Pre-cargar datos para cada ciudad (las más usadas)
                int contadorCiudades = 0;
                foreach (string ciudad in ciudades.Take(10)) // Limitar a las primeras 10 ciudades
                {
                    string claveCache = $"{ciudad}||Todos||0|{_registrosPorPagina}";
                    if (!_cacheDatosCiudadRadio.ContainsKey(claveCache))
                    {
                        swParcial.Restart();
                        var tableData = await DataAccess.CargarComercialesAgrupadosAsync(
                            DatabaseConfig.ConnectionString,
                            DatabaseConfig.TableName,
                            _registrosPorPagina,
                            0,
                            null,
                            "Todos",
                            ciudad,
                            null);

                        _cacheDatosCiudadRadio[claveCache] = tableData;
                        swParcial.Stop();
                        contadorCiudades++;
                        System.Diagnostics.Debug.WriteLine($"[TIMING] Pre-cargado ciudad '{ciudad}': {swParcial.ElapsedMilliseconds} ms ({tableData.Rows.Count} filas)");
                    }
                }

                _ultimaActualizacionCache = DateTime.Now;
                swTotal.Stop();
                System.Diagnostics.Debug.WriteLine($"[TIMING] ========== PRE-CARGA COMPLETADA: {swTotal.ElapsedMilliseconds} ms total ({contadorRadios} radios, {contadorCiudades} ciudades, {_cacheDatosCiudadRadio.Count} items en caché) ==========");
            }
            catch (Exception ex)
            {
                swTotal.Stop();
                System.Diagnostics.Debug.WriteLine($"[TIMING] Error en pre-carga después de {swTotal.ElapsedMilliseconds} ms: {ex.Message}");
            }
        }

        /// <summary>
        /// Muestra informacion sobre el total de registros en la base de datos
        /// </summary>
        private async Task MostrarInfoRegistrosAsync()
        {
            try
            {
                // Vista siempre agrupada - contar archivos únicos
                string textoBusqueda = txtBusqueda.Text.Trim();
                string estadoFiltro = cboFiltroEstado.SelectedItem?.ToString() ?? "Todos";
                string ciudadFiltro = _filtroCiudadActual;
                string radioFiltro = _filtroRadioActual;

                _totalArchivosUnicos = await DataAccess.ContarArchivosUnicosAsync(
                    DatabaseConfig.ConnectionString,
                    DatabaseConfig.TableName,
                    string.IsNullOrEmpty(textoBusqueda) ? null : textoBusqueda,
                    estadoFiltro,
                    ciudadFiltro,
                    radioFiltro);
                _totalRegistros = _totalArchivosUnicos;

                ActualizarControlesPaginacion();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error contando registros: {ex.Message}");
            }
        }

        private async Task CargarCiudadesAsync()
        {
            dgv_ciudades.Rows.Clear();

            try
            {
                var ciudades = await AdminCiudadesForm.ObtenerCiudadesActivasAsync();

                foreach (string ciudad in ciudades)
                {
                    dgv_ciudades.Rows.Add(ciudad);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al cargar ciudades: {ex.Message}");
            }
        }

        private async Task CargarEstacionesAsync()
        {
            dgv_estaciones.Rows.Clear();

            // Solo mostrar estas estaciones específicas
            string[] estacionesPermitidas = { "EXITOSA", "KARIBEÑA", "LA KALLE", "LAKALLE" };

            try
            {
                var radios = await AdminRadiosForm.ObtenerRadiosActivasAsync();

                foreach (string radio in radios)
                {
                    // Filtrar solo las estaciones permitidas
                    string radioUpper = radio.ToUpper();
                    if (estacionesPermitidas.Any(e => radioUpper.Contains(e) || e.Contains(radioUpper)))
                    {
                        dgv_estaciones.Rows.Add(radio);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al cargar estaciones: {ex.Message}");
                // Si hay error, usar estaciones por defecto
                string[] estaciones = { "EXITOSA", "KARIBEÑA", "LA KALLE" };
                foreach (string estacion in estaciones)
                {
                    dgv_estaciones.Rows.Add(estacion);
                }
            }
        }

        private async Task CargarDBAsync(string tableName = null)
        {
            string connectionString = DatabaseConfig.ConnectionString;
            tableName = tableName ?? DatabaseConfig.TableName; // Si tableName es null, usar DatabaseConfig.TableName
            await CargarDatosDesdeBaseDeDatosAsync(connectionString, tableName);
        }

        private async Task CargarDatosDesdeBaseDeDatosAsync(string connectionString, string tableName)
        {
            await dbManager.LoadDataFromDatabaseAsync(dgv_base, connectionString, tableName);
            // Refrescar el dashboard despues de cargar datos
            _dashboardControl?.RefrescarDashboard();
        }

        private async Task CargarDatosFiltradosAsync(string estadoFiltro)
        {
            string connectionString = DatabaseConfig.ConnectionString;
            string tableName = DatabaseConfig.TableName;

            ComercialesDataHelper helper = new ComercialesDataHelper();
            await helper.CargarDatosFiltradosAsync(connectionString, tableName, dgv_base, estadoFiltro);
        }

        private async Task<string> ObtenerTipoProgramacionAsync(string codigo)
        {
            try
            {
                using (var conn = new Npgsql.NpgsqlConnection(PostgreSQLMigration.ConnectionString))
                {
                    await conn.OpenAsync();
                    string query = "SELECT TipoProgramacion FROM Comerciales WHERE Codigo = @Codigo";
                    using (var cmd = new Npgsql.NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Codigo", codigo);
                        var result = await cmd.ExecuteScalarAsync();
                        if (result != null && result != DBNull.Value)
                        {
                            return result.ToString();
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Error al obtener TipoProgramacion
            }
            return "Cada 00-30"; // Valor por defecto
        }

        private string GetSongName(string filePath)
        {
            return Path.GetFileName(filePath);
        }
        private void ProgressBar1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isDragging = true;
                ProgressBar1_MouseMove(sender, e); // Agrega esta línea
            }
        }
        private void ProgressBar1_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isDragging = false;
            }
        }
        private void ProgressBar1_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                double percentage = e.X / (double)elegantProgressBar1.Width;
                int newValue = (int)(percentage * (elegantProgressBar1.Maximum - elegantProgressBar1.Minimum));
                newValue = Math.Max(elegantProgressBar1.Minimum, Math.Min(elegantProgressBar1.Maximum, newValue));
                elegantProgressBar1.Value = newValue;
                UpdateAudioPosition(percentage);
            }
        }
        private void UpdatePlayingRow(Action<DataGridViewRow> updateAction)
        {
            if (currentPlayingRowIndex >= 0 && currentPlayingRowIndex < dgv_archivos.Rows.Count)
            {
                DataGridViewRow playingRow = dgv_archivos.Rows[currentPlayingRowIndex];
                updateAction(playingRow);
            }
        }
        private void AudioPlayer_AudioFinished(object sender, EventArgs e)
        {
            playbackTimer.Stop();
            elegantProgressBar1.Value = 0;
            progressBarLeft.Value = 0;
            progressBarRight.Value = 0;
        }
        private void AudioPlayer_AudioProgressUpdated(object sender, double progressPercentage)
        {
            elegantProgressBar1.Value = (int)progressPercentage;
            // Actualizar el tiempo transcurrido
            UpdatePlayingRow(playingRow => {
                double totalLengthSeconds = Bass.BASS_ChannelBytes2Seconds(audioPlayer.CurrentStream, Bass.BASS_ChannelGetLength(audioPlayer.CurrentStream));
                double elapsedSeconds = totalLengthSeconds * (progressPercentage / 100.0);
                TimeSpan elapsedTime = TimeSpan.FromSeconds(elapsedSeconds);
                playingRow.Cells["Column4"].Value = elapsedTime.ToString(SongTimeFormat);
            });
        }

        private void PlaybackTimer_Tick(object sender, EventArgs e)
        {
            if (audioPlayer.CurrentStream != 0)
            {
                long positionBytes = Bass.BASS_ChannelGetPosition(audioPlayer.CurrentStream);
                double positionSeconds = Bass.BASS_ChannelBytes2Seconds(audioPlayer.CurrentStream, positionBytes);
                TimeSpan position = TimeSpan.FromSeconds(positionSeconds);
                // Calcular el progreso actual
                double totalLengthSeconds = audioPlayer.GetTotalLengthSeconds();
                double progressPercentage = (positionSeconds / totalLengthSeconds) * 100;
                elegantProgressBar1.Value = (int)progressPercentage;
                // Actualizar el DataGridView
                UpdatePlayingRow(playingRow => playingRow.Cells["Column4"].Value = position.ToString(SongTimeFormat));
                // Actualiza los niveles de audio
                (uint leftLevel, uint rightLevel) = audioPlayer.UpdateAudioLevels();
                progressBarLeft.Value = (int)leftLevel;
                progressBarRight.Value = (int)rightLevel;
            }
        }
        private void UpdateAudioPosition(double percentage)
        {
            if (playerState.CurrentStream != 0)
            {
                long totalLengthBytes = Bass.BASS_ChannelGetLength(playerState.CurrentStream);
                double totalLengthSeconds = Bass.BASS_ChannelBytes2Seconds(playerState.CurrentStream, totalLengthBytes);
                double newPositionSeconds = totalLengthSeconds * percentage;
                long newPositionBytes = Bass.BASS_ChannelSeconds2Bytes(playerState.CurrentStream, newPositionSeconds);
                Bass.BASS_ChannelSetPosition(playerState.CurrentStream, newPositionBytes);
            }
        }
        public void FileExplorerControl_AudioFileDoubleClicked(object sender, string filePath)
        {
            // Extract the song name from the file path
            string songName = GetSongName(filePath);
            // Get the duration of the audio file
            TimeSpan time = BassPlayer.GetAudioDuration(filePath);
            // Add the audio file to the DataGridView
            AddSongToDataGridView(songName, filePath, time);
            // Update the list of song paths
            songPaths.Add(filePath);
        }

        /// <summary>
        /// Manejador del evento que carga automaticamente todos los archivos de audio
        /// cuando el usuario entra a una carpeta en el explorador
        /// </summary>
        private void FileExplorerPanel_AudioFilesLoaded(object sender, List<string> audioFilePaths)
        {
            // Limpiar la lista actual de archivos
            dgv_archivos.Rows.Clear();
            songPaths.Clear();
            index = 1;

            // Cargar todos los archivos de audio de la carpeta
            foreach (string filePath in audioFilePaths)
            {
                string songName = GetSongName(filePath);
                TimeSpan time = BassPlayer.GetAudioDuration(filePath);
                AddSongToDataGridView(songName, filePath, time);
                songPaths.Add(filePath);
            }
        }

        /// <summary>
        /// Manejador del evento de busqueda - filtra los archivos en dgv_archivos
        /// </summary>
        private void FileExplorerPanel_SearchTextChanged(object sender, string searchTerm)
        {
            if (string.IsNullOrEmpty(searchTerm))
            {
                // Mostrar todos los archivos
                foreach (DataGridViewRow row in dgv_archivos.Rows)
                {
                    row.Visible = true;
                }
                fileExplorerPanel.ActualizarEstadoBusqueda(dgv_archivos.Rows.Count, dgv_archivos.Rows.Count);
            }
            else
            {
                // Filtrar por nombre
                int visibles = 0;
                int total = dgv_archivos.Rows.Count;

                foreach (DataGridViewRow row in dgv_archivos.Rows)
                {
                    if (row.IsNewRow) continue;

                    string nombre = row.Cells[1].Value?.ToString() ?? "";
                    bool coincide = nombre.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0;
                    row.Visible = coincide;

                    if (coincide) visibles++;
                }

                fileExplorerPanel.ActualizarEstadoBusqueda(visibles, total);
            }
        }

        private void AddSongToDataGridView(string songName, string filePath, TimeSpan time)
        {
            int stream = Bass.BASS_StreamCreateFile(filePath, 0, 0, BASSFlag.BASS_STREAM_DECODE);
            float bitrate = 0;
            if (stream != 0)
            {
                Bass.BASS_ChannelGetAttribute(stream, BASSAttribute.BASS_ATTRIB_BITRATE, ref bitrate);
                Bass.BASS_StreamFree(stream);
            }

            int roundedBitrate = (int)Math.Round(bitrate);

            dgv_archivos.Rows.Add(index, songName, time.ToString(SongTimeFormat), "", $"{roundedBitrate} kbps");
            index++;

            // If there's a new row at the end, remove it
            if (dgv_archivos.Rows.Count > 0 && dgv_archivos.Rows[dgv_archivos.Rows.Count - 1].IsNewRow)
            {
                dgv_archivos.Rows.RemoveAt(dgv_archivos.Rows.Count - 1);
            }
        }
        private async void Form1_Load(object sender, EventArgs e)
        {
            // Aplicar estilos oscuros a los DataGridViews
            AplicarEstilosDataGridViews();

            // Suscribirse al evento de actualizacion de datos para refresh en tiempo real
            ConfigManager.OnDatosActualizados += OnDatosActualizadosHandler;

            // Inicializar combo de horas para eliminacion
            InicializarComboHorasEliminar();

            // OPTIMIZACIÓN: Precargar datos estáticos en caché (ciudades, estaciones)
            // Esto acelera las primeras consultas del usuario
            _ = CacheService.PrecargarDatosEstaticosAsync();

            // Crear índices de BD para mejorar rendimiento (se ejecuta en background)
            await DatabaseService.CrearIndicesAsync();
        }

        /// <summary>
        /// Manejador del evento de actualizacion de datos
        /// Refresca la grilla y el dashboard cuando hay cambios en la BD
        /// </summary>
        private async void OnDatosActualizadosHandler()
        {
            // Asegurar que se ejecute en el hilo de UI
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(OnDatosActualizadosHandler));
                return;
            }

            try
            {
                // Recargar los datos de la base de datos
                await CargarDBAsync();

                // Refrescar el dashboard
                _dashboardControl?.RefrescarDashboard();

                // Aplicar filtros si hay alguno activo
                if (!string.IsNullOrEmpty(_filtroCiudadActual) || !string.IsNullOrEmpty(_filtroRadioActual))
                {
                    AplicarFiltrosCombinados();
                }

                System.Diagnostics.Debug.WriteLine("Form1: Datos actualizados en tiempo real");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error actualizando Form1: {ex.Message}");
            }
        }

        private async Task CargarPautasDelComercialSeleccionadoAsync()
        {
            try
            {
                // Limpiar pautas anteriores
                dgv_pautas.Rows.Clear();

                if (dgv_base.SelectedRows.Count == 0)
                {
                    lblPautasTitulo.Text = "Pautas del Comercial Seleccionado";
                    return;
                }

                string codigo = dgv_base.SelectedRows[0].Cells["Codigo"]?.Value?.ToString();
                string radio = dgv_base.SelectedRows[0].Cells["Radio"]?.Value?.ToString();
                string fechaInicioStr = dgv_base.SelectedRows[0].Cells["FechaMinima"]?.Value?.ToString();

                if (string.IsNullOrEmpty(codigo))
                    return;

                // Parsear fecha de inicio
                DateTime fechaInicio = DateTime.Today;
                DateTime.TryParseExact(fechaInicioStr, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out fechaInicio);

                // Obtener tipo de programacion
                string tipoProgramacion = await ObtenerTipoProgramacionAsync(codigo);
                TipoTanda tipoTanda = DetectarTipoTanda(tipoProgramacion, radio);
                string[] horarios = TandasHorarias.GetHorarios(tipoTanda);

                // Configurar columnas - SIEMPRE limpiar para evitar duplicados
                dgv_pautas.Columns.Clear();
                dgv_pautas.Columns.Add("Fecha", "Fecha");
                dgv_pautas.Columns.Add("DiaSemana", "Día");
                dgv_pautas.Columns.Add("Hora", "Hora");

                // Usar FillWeight para columnas responsivas
                dgv_pautas.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                dgv_pautas.Columns["Fecha"].FillWeight = 40;
                dgv_pautas.Columns["DiaSemana"].FillWeight = 35;
                dgv_pautas.Columns["Hora"].FillWeight = 25;

                // Ancho minimo para evitar que se corten
                dgv_pautas.Columns["Fecha"].MinimumWidth = 75;
                dgv_pautas.Columns["DiaSemana"].MinimumWidth = 60;
                dgv_pautas.Columns["Hora"].MinimumWidth = 50;

                // Estilos del header
                dgv_pautas.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(63, 81, 181);
                dgv_pautas.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
                dgv_pautas.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold);
                dgv_pautas.DefaultCellStyle.Font = new Font("Segoe UI", 8.5F);
                dgv_pautas.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(245, 248, 255);

                // Cargar pautas desde la BD
                using (var conn = new Npgsql.NpgsqlConnection(DatabaseConfig.ConnectionString))
                {
                    await conn.OpenAsync();

                    string query = @"
                        SELECT Fila, Columna
                        FROM ComercialesAsignados
                        WHERE Codigo = @Codigo
                        ORDER BY Columna, Fila";

                    using (var cmd = new Npgsql.NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Codigo", codigo);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            int totalPautas = 0;
                            while (await reader.ReadAsync())
                            {
                                int fila = reader.GetInt32(0);
                                int columna = reader.GetInt32(1);

                                // Convertir fila a hora
                                string hora = (fila >= 0 && fila < horarios.Length) ? horarios[fila] : $"Fila {fila}";

                                // Calcular fecha (columna 2 = primer día)
                                int diasDesdeInicio = columna - 2;
                                DateTime fechaPauta = fechaInicio.AddDays(diasDesdeInicio);

                                // Obtener día de la semana
                                string diaSemana = ObtenerDiaSemanaEspanol(fechaPauta.DayOfWeek);

                                dgv_pautas.Rows.Add(
                                    fechaPauta.ToString("dd/MM/yyyy"),
                                    diaSemana,
                                    hora
                                );
                                totalPautas++;
                            }

                            lblPautasTitulo.Text = $"Pautas: {codigo} ({totalPautas} registros)";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error cargando pautas: {ex.Message}");
                lblPautasTitulo.Text = "Error al cargar pautas";
            }
        }

        private TipoTanda DetectarTipoTanda(string tipoProgramacion, string radio)
        {
            // Si tiene tipo de programacion valido y no es generico, usarlo
            if (!string.IsNullOrEmpty(tipoProgramacion) &&
                tipoProgramacion != "Cada 00-30" &&
                tipoProgramacion != "Importado Access")
            {
                return TandasHorarias.GetTipoTandaFromString(tipoProgramacion);
            }

            // Si no, detectar por nombre de radio
            if (string.IsNullOrEmpty(radio))
                return TipoTanda.Tandas_00_30;

            string radioUpper = radio.ToUpper();

            // KARIBEÑA y LA KALLE usan las 4 tandas: 00, 20, 30, 50 (96 tandas por dia)
            if (radioUpper.Contains("KARIBEÑA") || radioUpper.Contains("KARIBENA"))
                return TipoTanda.Tandas_00_20_30_50;
            else if (radioUpper.Contains("LAKALLE") || radioUpper.Contains("LA KALLE") || radioUpper.Contains("KALLE"))
                return TipoTanda.Tandas_00_20_30_50;
            else
                return TipoTanda.Tandas_00_30;
        }

        private string ObtenerDiaSemanaEspanol(DayOfWeek dia)
        {
            switch (dia)
            {
                case DayOfWeek.Monday: return "Lunes";
                case DayOfWeek.Tuesday: return "Martes";
                case DayOfWeek.Wednesday: return "Miércoles";
                case DayOfWeek.Thursday: return "Jueves";
                case DayOfWeek.Friday: return "Viernes";
                case DayOfWeek.Saturday: return "Sábado";
                case DayOfWeek.Sunday: return "Domingo";
                default: return "";
            }
        }

        private void AplicarEstilosDataGridViews()
        {
            // Habilitar DoubleBuffered en los DataGridViews para evitar parpadeo
            HabilitarDoubleBuffered(dgv_archivos);
            HabilitarDoubleBuffered(dgv_base);
            HabilitarDoubleBuffered(dgv_estaciones);
            HabilitarDoubleBuffered(dgv_ciudades);
            HabilitarDoubleBuffered(dgv_pautas);

            // =============================================
            // ESTILO PARA dgv_base (Grilla principal)
            // =============================================
            DataGridViewCellStyle headerStyleBase = new DataGridViewCellStyle();
            headerStyleBase.BackColor = Color.FromArgb(0, 120, 215); // Azul moderno
            headerStyleBase.ForeColor = Color.White;
            headerStyleBase.SelectionBackColor = Color.FromArgb(0, 120, 215);
            headerStyleBase.SelectionForeColor = Color.White;
            headerStyleBase.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            headerStyleBase.Alignment = DataGridViewContentAlignment.MiddleLeft;
            headerStyleBase.Padding = new Padding(8, 0, 0, 0);

            DataGridViewCellStyle cellStyleBase = new DataGridViewCellStyle();
            cellStyleBase.BackColor = Color.FromArgb(250, 250, 250); // Fondo claro
            cellStyleBase.ForeColor = Color.FromArgb(30, 30, 30); // Texto oscuro
            cellStyleBase.SelectionBackColor = Color.FromArgb(0, 120, 215); // Azul selección
            cellStyleBase.SelectionForeColor = Color.White;
            cellStyleBase.Font = new Font("Segoe UI", 8.5F);
            cellStyleBase.Alignment = DataGridViewContentAlignment.MiddleCenter; // Centrado por defecto

            DataGridViewCellStyle altRowStyleBase = new DataGridViewCellStyle();
            altRowStyleBase.BackColor = Color.FromArgb(235, 243, 255); // Azul muy claro para filas alternas
            altRowStyleBase.ForeColor = Color.FromArgb(30, 30, 30);
            altRowStyleBase.SelectionBackColor = Color.FromArgb(0, 120, 215);
            altRowStyleBase.SelectionForeColor = Color.White;

            dgv_base.EnableHeadersVisualStyles = false;
            dgv_base.ColumnHeadersDefaultCellStyle = headerStyleBase;
            dgv_base.DefaultCellStyle = cellStyleBase;
            dgv_base.AlternatingRowsDefaultCellStyle = altRowStyleBase;
            dgv_base.BackgroundColor = Color.FromArgb(240, 240, 240);
            dgv_base.GridColor = Color.FromArgb(200, 200, 200);
            dgv_base.RowTemplate.Height = 22; // Filas más compactas
            dgv_base.ColumnHeadersHeight = 28; // Encabezado más compacto

            // Columna Archivo alineada a la izquierda (aplicar despues de estilos globales)
            if (dgv_base.Columns.Contains("NombreArchivo"))
            {
                dgv_base.Columns["NombreArchivo"].DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleLeft,
                    BackColor = Color.FromArgb(250, 250, 250),
                    ForeColor = Color.FromArgb(30, 30, 30),
                    SelectionBackColor = Color.FromArgb(0, 120, 215),
                    SelectionForeColor = Color.White,
                    Font = new Font("Segoe UI", 8.5F)
                };
            }

            // =============================================
            // ESTILO PARA dgv_archivos (Lista de audio)
            // =============================================
            DataGridViewCellStyle headerStyleArchivos = new DataGridViewCellStyle();
            headerStyleArchivos.BackColor = Color.FromArgb(63, 81, 181); // Azul índigo
            headerStyleArchivos.ForeColor = Color.White;
            headerStyleArchivos.SelectionBackColor = Color.FromArgb(63, 81, 181);
            headerStyleArchivos.SelectionForeColor = Color.White;
            headerStyleArchivos.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            headerStyleArchivos.Alignment = DataGridViewContentAlignment.MiddleLeft;
            headerStyleArchivos.Padding = new Padding(5, 0, 0, 0);

            DataGridViewCellStyle cellStyleArchivos = new DataGridViewCellStyle();
            cellStyleArchivos.BackColor = Color.FromArgb(250, 250, 255);
            cellStyleArchivos.ForeColor = Color.FromArgb(40, 40, 40);
            cellStyleArchivos.SelectionBackColor = Color.FromArgb(0, 120, 215); // Azul Windows 0078D7
            cellStyleArchivos.SelectionForeColor = Color.White;
            cellStyleArchivos.Font = new Font("Segoe UI", 9F);
            cellStyleArchivos.Padding = new Padding(5, 0, 0, 0);

            DataGridViewCellStyle altRowStyleArchivos = new DataGridViewCellStyle();
            altRowStyleArchivos.BackColor = Color.FromArgb(232, 234, 246); // Azul muy claro
            altRowStyleArchivos.ForeColor = Color.FromArgb(40, 40, 40);
            altRowStyleArchivos.SelectionBackColor = Color.FromArgb(0, 120, 215); // Azul Windows 0078D7
            altRowStyleArchivos.SelectionForeColor = Color.White;

            dgv_archivos.EnableHeadersVisualStyles = false;
            dgv_archivos.ColumnHeadersDefaultCellStyle = headerStyleArchivos;
            dgv_archivos.DefaultCellStyle = cellStyleArchivos;
            dgv_archivos.AlternatingRowsDefaultCellStyle = altRowStyleArchivos;
            dgv_archivos.BackgroundColor = Color.FromArgb(245, 248, 252);
            dgv_archivos.GridColor = Color.FromArgb(200, 210, 220);

            // =============================================
            // ESTILO PARA dgv_estaciones
            // =============================================
            DataGridViewCellStyle headerStyleEstaciones = new DataGridViewCellStyle();
            headerStyleEstaciones.BackColor = Color.FromArgb(0, 150, 136); // Teal
            headerStyleEstaciones.ForeColor = Color.White;
            headerStyleEstaciones.SelectionBackColor = Color.FromArgb(0, 150, 136);
            headerStyleEstaciones.SelectionForeColor = Color.White;
            headerStyleEstaciones.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            headerStyleEstaciones.Alignment = DataGridViewContentAlignment.MiddleCenter;

            DataGridViewCellStyle cellStyleEstaciones = new DataGridViewCellStyle();
            cellStyleEstaciones.BackColor = Color.White;
            cellStyleEstaciones.ForeColor = Color.FromArgb(50, 50, 50);
            cellStyleEstaciones.SelectionBackColor = Color.FromArgb(255, 152, 0); // Naranja para seleccion
            cellStyleEstaciones.SelectionForeColor = Color.White;
            cellStyleEstaciones.Font = new Font("Segoe UI", 9F);
            cellStyleEstaciones.Alignment = DataGridViewContentAlignment.MiddleCenter;

            dgv_estaciones.EnableHeadersVisualStyles = false;
            dgv_estaciones.ColumnHeadersDefaultCellStyle = headerStyleEstaciones;
            dgv_estaciones.DefaultCellStyle = cellStyleEstaciones;
            dgv_estaciones.AlternatingRowsDefaultCellStyle = cellStyleEstaciones; // Mismo estilo para filas alternadas
            dgv_estaciones.BackgroundColor = Color.White;
            dgv_estaciones.GridColor = Color.FromArgb(220, 220, 220);
            dgv_estaciones.RowTemplate.DefaultCellStyle = cellStyleEstaciones;

            // =============================================
            // ESTILO PARA dgv_ciudades
            // =============================================
            DataGridViewCellStyle headerStyleCiudades = new DataGridViewCellStyle();
            headerStyleCiudades.BackColor = Color.FromArgb(156, 39, 176); // Púrpura
            headerStyleCiudades.ForeColor = Color.White;
            headerStyleCiudades.SelectionBackColor = Color.FromArgb(156, 39, 176);
            headerStyleCiudades.SelectionForeColor = Color.White;
            headerStyleCiudades.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            headerStyleCiudades.Alignment = DataGridViewContentAlignment.MiddleCenter;

            DataGridViewCellStyle cellStyleCiudades = new DataGridViewCellStyle();
            cellStyleCiudades.BackColor = Color.White;
            cellStyleCiudades.ForeColor = Color.FromArgb(50, 50, 50);
            cellStyleCiudades.SelectionBackColor = Color.FromArgb(0, 150, 136); // Teal/Verde azulado para seleccion
            cellStyleCiudades.SelectionForeColor = Color.White;
            cellStyleCiudades.Font = new Font("Segoe UI", 9F);
            cellStyleCiudades.Alignment = DataGridViewContentAlignment.MiddleCenter;

            dgv_ciudades.EnableHeadersVisualStyles = false;
            dgv_ciudades.ColumnHeadersDefaultCellStyle = headerStyleCiudades;
            dgv_ciudades.DefaultCellStyle = cellStyleCiudades;
            dgv_ciudades.AlternatingRowsDefaultCellStyle = cellStyleCiudades; // Mismo estilo para filas alternadas
            dgv_ciudades.BackgroundColor = Color.White;
            dgv_ciudades.GridColor = Color.FromArgb(220, 220, 220);
            dgv_ciudades.RowTemplate.DefaultCellStyle = cellStyleCiudades;
        }

        private void HabilitarDoubleBuffered(DataGridView dgv)
        {
            // Usar reflexión para habilitar DoubleBuffered (no es público en DataGridView)
            typeof(DataGridView).InvokeMember(
                "DoubleBuffered",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.SetProperty,
                null,
                dgv,
                new object[] { true }
            );
        }


        private void btn_play_Click(object sender, EventArgs e)
        {
            if (dgv_archivos.SelectedRows.Count > 0)
            {
                string filePath = songPaths[dgv_archivos.SelectedRows[0].Index];
                audioPlayer.Play(filePath);
                playerState.CurrentStream = audioPlayer.CurrentStream; 
                playerState.CurrentPlayingRowIndex = dgv_archivos.SelectedRows[0].Index;
                currentPlayingRowIndex = dgv_archivos.SelectedRows[0].Index;
                playbackTimer.Start();  
            }
        }
        private void btn_stop_Click(object sender, EventArgs e)
        {
            audioPlayer.Stop();
            currentPlayingRowIndex = -1;
        }
        private void btn_eliminar_archivo_Click(object sender, EventArgs e)
        {
            // Eliminar el archivo seleccionado de dgv_archivos
            if (dgv_archivos.SelectedRows.Count > 0)
            {
                int selectedIndex = dgv_archivos.SelectedRows[0].Index;

                // Si se está reproduciendo el archivo seleccionado, detener
                if (currentPlayingRowIndex == selectedIndex)
                {
                    audioPlayer.Stop();
                    currentPlayingRowIndex = -1;
                }
                else if (currentPlayingRowIndex > selectedIndex)
                {
                    // Ajustar el índice si el archivo eliminado está antes del que se reproduce
                    currentPlayingRowIndex--;
                }

                // Eliminar de la lista de paths si existe
                if (selectedIndex < songPaths.Count)
                {
                    songPaths.RemoveAt(selectedIndex);
                }

                // Eliminar la fila del DataGridView
                dgv_archivos.Rows.RemoveAt(selectedIndex);

                // Actualizar los números de fila
                for (int i = 0; i < dgv_archivos.Rows.Count; i++)
                {
                    dgv_archivos.Rows[i].Cells[0].Value = i + 1;
                }
                index = dgv_archivos.Rows.Count + 1;
            }
        }
        private void btn_limpiar_Click(object sender, EventArgs e)
        {
            // Detener la reproducción
            audioPlayer.Stop();
            // Limpiar la lista de canciones del dgv_archivos
            songPaths.Clear();
            dgv_archivos.Rows.Clear();
            index = 1;
            // Resetear la fila que se está reproduciendo
            currentPlayingRowIndex = -1;
            // Liberar la instancia de BASS
            Bass.BASS_Free();
            // Y volver a cargarla para seguir reproduciendo música si se quiere
            BassPlayer.InitBass();
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Desuscribirse del evento de actualizacion de datos
            ConfigManager.OnDatosActualizados -= OnDatosActualizadosHandler;

            // Detener el watcher de sincronizacion
            if (_watcherRed != null)
            {
                _watcherRed.EnableRaisingEvents = false;
                _watcherRed.Dispose();
                _watcherRed = null;
            }

            // Si es ruta de red, sincronizar cambios hacia el servidor
            if (ConfigManager.EsRutaDeRed)
            {
                ConfigManager.SincronizarHaciaRed();
            }

            // Llama a BASS_Free para liberar todos los recursos utilizados por BASS
            Bass.BASS_Free();
        }

        private void ConfigurarMenuContextualDGVArchivos()
        {
            ContextMenuStrip menuContextual = new ContextMenuStrip();

            // Opción para Pauteo Rápido (nueva forma rápida)
            ToolStripMenuItem pauteoRapidoMenuItem = new ToolStripMenuItem("Pauteo Rápido");
            pauteoRapidoMenuItem.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            pauteoRapidoMenuItem.ForeColor = Color.FromArgb(33, 150, 243);
            pauteoRapidoMenuItem.Click += PauteoRapido_Click;
            menuContextual.Items.Add(pauteoRapidoMenuItem);

            menuContextual.Items.Add(new ToolStripSeparator());

            // Opción para Pautear con formulario (forma tradicional)
            ToolStripMenuItem pautearMenuItem = new ToolStripMenuItem("Pautear (Formulario)");
            pautearMenuItem.Click += PautearComercial_Click;
            menuContextual.Items.Add(pautearMenuItem);

            dgv_archivos.ContextMenuStrip = menuContextual;
        }

        private void PauteoRapido_Click(object sender, EventArgs e)
        {
            if (dgv_archivos.SelectedRows.Count > 0)
            {
                int rowIndex = dgv_archivos.SelectedRows[0].Index;
                if (rowIndex >= 0 && rowIndex < songPaths.Count)
                {
                    string audioPath = songPaths[rowIndex];
                    EnviarAudioAPauteoRapido(audioPath);
                }
            }
        }

        private void dgv_archivos_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.RowIndex < songPaths.Count)
            {
                // Doble clic ahora envía al Pauteo Rápido para mayor eficiencia
                string audioPath = songPaths[e.RowIndex];
                EnviarAudioAPauteoRapido(audioPath);
            }
        }

        private void dgv_archivos_SelectionChanged(object sender, EventArgs e)
        {
            // Al seleccionar un archivo, cargarlo automáticamente en Pauteo Rápido
            if (dgv_archivos.SelectedRows.Count > 0)
            {
                int rowIndex = dgv_archivos.SelectedRows[0].Index;
                if (rowIndex >= 0 && rowIndex < songPaths.Count)
                {
                    string audioPath = songPaths[rowIndex];
                    EnviarAudioAPauteoRapidoManteniendo(audioPath);
                }
            }
        }

        private void PautearComercial_Click(object sender, EventArgs e)
        {
            if (dgv_archivos.SelectedRows.Count > 0)
            {
                int rowIndex = dgv_archivos.SelectedRows[0].Index;
                AbrirFormularioPauteo(rowIndex);
            }
        }

        private async void AbrirFormularioPauteo(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= songPaths.Count)
            {
                MessageBox.Show("No se ha seleccionado un archivo válido.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string filePath = songPaths[rowIndex];

            // Usar el panel de Pauteo Rápido directamente
            EnviarAudioAPauteoRapido(filePath);
            MessageBox.Show("Use el panel de Pauteo Rápido a la derecha para pautear el comercial.",
                "Pauteo Rápido", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private async void cboFiltroEstado_SelectedIndexChanged(object sender, EventArgs e)
        {
            // No recargar durante la inicialización para evitar cargas dobles
            if (_inicializando || cboFiltroEstado.SelectedIndex == -1)
                return;

            // Usar busqueda SQL optimizada
            await BuscarConFiltrosAsync();
        }

        public async Task LimpiarBaseDeDatosCompletaAsync()
        {
            DialogResult resultado = MessageBox.Show(
                "¿Estás seguro de que deseas limpiar TODA la base de datos? Esta acción eliminará TODOS los comerciales y no se puede deshacer.",
                "ADVERTENCIA - Limpiar Base de Datos",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (resultado == DialogResult.Yes)
            {
                try
                {
                    string connectionString = DatabaseConfig.ConnectionString;
                    await DataAccess.LimpiarTodasLasBaseDeDatosAsync(connectionString);

                    // Limpiar el DataGridView
                    dgv_base.Rows.Clear();

                    MessageBox.Show("Base de datos limpiada correctamente. Todos los registros han sido eliminados.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al limpiar la base de datos: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void btnBuscar_Click(object sender, EventArgs e)
        {
            FiltrarPorColumna();
        }

        private void txtBusqueda_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true; // Evitar el sonido de "beep"
                FiltrarPorColumna();
            }
        }

        private async void btnLimpiarFiltro_Click(object sender, EventArgs e)
        {
            // Limpiar el campo de búsqueda
            txtBusqueda.Text = "";

            // Restablecer el filtro de estado a "Todos"
            cboFiltroEstado.SelectedIndex = 0;

            // Limpiar filtros de ciudad y radio
            _filtroCiudadActual = null;
            _filtroRadioActual = null;
            dgv_estaciones.ClearSelection();
            dgv_ciudades.ClearSelection();

            // Limpiar filtros de ciudad y radio
            _filtroCiudadActual = null;
            _filtroRadioActual = null;

            // Mostrar controles de paginacion y recargar
            MostrarControlesPaginacion(true);
            _paginaActual = 1;
            await CargarDBAsync();
            await MostrarInfoRegistrosAsync();
        }

        private void ConfigurarWatcherRed()
        {
            try
            {
                string rutaRed = ConfigManager.RutaRedOriginal;
                if (string.IsNullOrEmpty(rutaRed) || !File.Exists(rutaRed))
                    return;

                string directorio = Path.GetDirectoryName(rutaRed);
                string archivo = Path.GetFileName(rutaRed);

                _watcherRed = new FileSystemWatcher(directorio);
                _watcherRed.Filter = archivo;
                _watcherRed.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size;

                // Evento cuando el archivo cambia
                _watcherRed.Changed += WatcherRed_Changed;

                // Activar el watcher
                _watcherRed.EnableRaisingEvents = true;

                System.Diagnostics.Debug.WriteLine($"FileSystemWatcher configurado para: {rutaRed}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error configurando FileSystemWatcher: {ex.Message}");
            }
        }

        private DateTime _ultimaCargaRed = DateTime.MinValue;

        private async void WatcherRed_Changed(object sender, FileSystemEventArgs e)
        {
            try
            {
                // Evitar multiples disparos en menos de 1 segundo (debounce)
                if ((DateTime.Now - _ultimaCargaRed).TotalMilliseconds < 1000)
                    return;

                _ultimaCargaRed = DateTime.Now;

                System.Diagnostics.Debug.WriteLine($"Cambio detectado en red: {e.FullPath} - Tipo: {e.ChangeType}");

                // Usar Invoke para ejecutar en el hilo de UI
                if (this.InvokeRequired)
                {
                    this.BeginInvoke(new Action(async () => await RecargarDatosDesdeRed()));
                }
                else
                {
                    await RecargarDatosDesdeRed();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en WatcherRed_Changed: {ex.Message}");
            }
        }

        private async Task RecargarDatosDesdeRed()
        {
            try
            {
                // Sincronizar desde red
                ConfigManager.SincronizarDesdeRed();

                // Recargar los datos
                await CargarDBAsync();

                // Refrescar dashboard
                _dashboardControl?.RefrescarDashboard();

                // Aplicar filtros si hay alguno activo
                if (!string.IsNullOrEmpty(_filtroCiudadActual) || !string.IsNullOrEmpty(_filtroRadioActual))
                {
                    AplicarFiltrosCombinados();
                }

                System.Diagnostics.Debug.WriteLine("Datos recargados automaticamente desde la red");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error recargando datos desde red: {ex.Message}");
            }
        }

        private async void FiltrarPorColumna()
        {
            await BuscarConFiltrosAsync();
        }

        /// <summary>
        /// Busca comerciales usando filtros SQL (optimizado para grandes volumenes)
        /// Usa datos agrupados para mantener consistencia con el grid principal
        /// </summary>
        private async Task BuscarConFiltrosAsync()
        {
            // Solo permitir búsqueda si hay una ciudad o estación seleccionada
            if (string.IsNullOrEmpty(_filtroCiudadActual) && string.IsNullOrEmpty(_filtroRadioActual))
            {
                dgv_base.Rows.Clear();
                _totalRegistros = 0;
                _totalArchivosUnicos = 0;
                ActualizarControlesPaginacion();
                lblUsuarioActual.Text = $"Usuario: {UserManager.UsuarioActual?.NombreCompleto ?? "Desconocido"} | Seleccione una ciudad o estación";
                return;
            }

            try
            {
                // Mostrar indicador de carga
                this.Cursor = Cursors.WaitCursor;
                lblUsuarioActual.Text = "Buscando...";
                Application.DoEvents();

                string textoBusqueda = txtBusqueda.Text.Trim();
                string estadoFiltro = cboFiltroEstado.SelectedItem?.ToString() ?? "Todos";

                // Obtener filtros de ciudad y radio (siempre activos si estan seleccionados)
                string ciudadFiltro = _filtroCiudadActual;
                string radioFiltro = _filtroRadioActual;

                // Verificar si hay filtros activos
                bool hayFiltrosActivos = !string.IsNullOrEmpty(textoBusqueda) ||
                                         estadoFiltro != "Todos" ||
                                         !string.IsNullOrEmpty(ciudadFiltro) ||
                                         !string.IsNullOrEmpty(radioFiltro);

                // Ocultar controles de paginacion cuando hay filtros activos
                MostrarControlesPaginacion(!hayFiltrosActivos);

                // Usar datos agrupados para mantener consistencia con el grid
                var tableData = await DataAccess.CargarComercialesAgrupadosAsync(
                    DatabaseConfig.ConnectionString,
                    DatabaseConfig.TableName,
                    1000,  // limite
                    0,     // offset
                    string.IsNullOrEmpty(textoBusqueda) ? null : textoBusqueda,
                    estadoFiltro,
                    ciudadFiltro,
                    radioFiltro);

                // Contar total de resultados agrupados
                int totalResultados = await DataAccess.ContarArchivosUnicosAsync(
                    DatabaseConfig.ConnectionString,
                    DatabaseConfig.TableName,
                    string.IsNullOrEmpty(textoBusqueda) ? null : textoBusqueda,
                    estadoFiltro,
                    ciudadFiltro,
                    radioFiltro);

                // Suspender el DataGridView mientras se actualiza (evita parpadeo)
                _cargandoDatos = true;
                dgv_base.SuspendLayout();
                dgv_base.Rows.Clear();

                foreach (DataRow row in tableData.Rows)
                {
                    // Formatear posición con prefijo P si es solo números
                    string posicion = row["Posicion"].ToString();
                    if (!string.IsNullOrEmpty(posicion) && !posicion.StartsWith("P"))
                    {
                        posicion = "P" + posicion;
                    }

                    dgv_base.Rows.Add(
                        row["FilePath"].ToString(),
                        row["CodigoNumerico"].ToString(),
                        row["NombreArchivo"].ToString(),
                        row["TotalRegistros"].ToString(),
                        row["Ciudad"].ToString(),
                        row["Radio"].ToString(),
                        Convert.ToDateTime(row["FechaMinima"]).ToString("dd/MM/yyyy"),
                        Convert.ToDateTime(row["FechaMaxima"]).ToString("dd/MM/yyyy"),
                        row["EstadoGeneral"].ToString(),
                        posicion
                    );
                }

                dgv_base.ResumeLayout();

                // Mostrar info del usuario y resultados
                string infoUsuario = UserManager.HayUsuarioLogueado
                    ? UserManager.UsuarioActual.NombreCompleto
                    : "Sin sesion";

                string infoResultados = tableData.Rows.Count < totalResultados
                    ? $"Mostrando {tableData.Rows.Count} de {totalResultados}"
                    : $"{totalResultados} resultados";
                lblUsuarioActual.Text = $"{infoUsuario} | {infoResultados}";

                // Si hay filas, asegurar que la primera esté seleccionada
                // Mantenemos _cargandoDatos = true para evitar duplicación
                if (dgv_base.Rows.Count > 0 && dgv_base.SelectedRows.Count == 0)
                {
                    dgv_base.Rows[0].Selected = true;
                    if (dgv_base.Columns.Contains("Codigo"))
                        dgv_base.CurrentCell = dgv_base.Rows[0].Cells["Codigo"];
                }

                // Ahora permitimos SelectionChanged
                _cargandoDatos = false;

                // Cargar pautas si hay filas seleccionadas
                if (dgv_base.SelectedRows.Count > 0)
                {
                    await CargarRegistrosDelArchivoSeleccionadoAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en busqueda: {ex.Message}");
                MessageBox.Show($"Error al buscar: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }

        private int ObtenerIndiceColumna(string nombreColumna)
        {
            switch (nombreColumna)
            {
                case "Código":
                    return 0;
                case "Spot":
                    return 1;
                case "Ciudad":
                    return 4;
                case "Radio":
                    return 5;
                case "Estado":
                    return 7;
                default:
                    return -1;
            }
        }

        // =============================================
        // PAUTEO RAPIDO
        // =============================================

        private void InicializarPauteoRapido()
        {
            pauteoRapidoPanel = new PauteoRapidoPanel();
            pauteoRapidoPanel.PautaGenerada += PauteoRapido_PautaGenerada;
            pauteoRapidoPanel.TandaClicked += PauteoRapido_TandaClicked;

            // Crear contenedor para dgv_pautas y panel de eliminacion (van a la derecha)
            var pnlPautasContainer = new Panel
            {
                Dock = DockStyle.Fill,
                Width = 320,
                BackColor = Color.FromArgb(250, 252, 255),
                BorderStyle = BorderStyle.FixedSingle,
                Name = "pnlPautasContainer",
                Padding = new Padding(5)
            };

            // Mover dgv_pautas al nuevo contenedor
            pnlBottomRight.Controls.Remove(dgv_pautas);
            pnlBottomRight.Controls.Remove(lblPautasTitulo);
            pnlBottomRight.Controls.Remove(grpEliminarPautas);
            pnlBottomRight.Controls.Remove(dgv_estaciones);
            pnlBottomRight.Controls.Remove(dgv_ciudades);

            // === ARRIBA: Estaciones / Ciudades ===
            var lblEstacionesCiudades = new Label
            {
                Text = "Estaciones / Ciudades",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 150, 243),
                Location = new Point(5, 5),
                Size = new Size(305, 20),
                TextAlign = ContentAlignment.MiddleCenter
            };

            dgv_estaciones.Dock = DockStyle.None;
            dgv_estaciones.Location = new Point(5, 28);
            dgv_estaciones.Size = new Size(148, 150);
            dgv_estaciones.ColumnHeadersHeight = 24;
            dgv_estaciones.RowTemplate.Height = 22;
            dgv_estaciones.Anchor = AnchorStyles.Top | AnchorStyles.Left;

            dgv_ciudades.Dock = DockStyle.None;
            dgv_ciudades.Location = new Point(158, 28);
            dgv_ciudades.Size = new Size(148, 150);
            dgv_ciudades.ColumnHeadersHeight = 24;
            dgv_ciudades.RowTemplate.Height = 22;
            dgv_ciudades.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            // === MEDIO: Pautas ===
            lblPautasTitulo.Location = new Point(5, 185);
            lblPautasTitulo.Size = new Size(305, 20);
            lblPautasTitulo.Visible = true;

            dgv_pautas.Location = new Point(5, 208);
            dgv_pautas.Size = new Size(305, 150);
            dgv_pautas.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            // === ABAJO: Eliminar Pautas ===
            grpEliminarPautas.Location = new Point(5, 365);
            grpEliminarPautas.Size = new Size(305, 165);
            grpEliminarPautas.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            grpEliminarPautas.Visible = true;

            // === MÁS ABAJO: Panel Vista Previa de Spots ===
            grpVistaPrevia = new GroupBox
            {
                Text = "Vista Previa Spots",
                Location = new Point(5, 535),
                Size = new Size(305, 120),
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 150, 243),
                BackColor = Color.FromArgb(250, 252, 255),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            lblVistaPreviaHora = new Label
            {
                Text = "Click derecho en una hora para ver spots",
                Location = new Point(10, 18),
                Size = new Size(285, 16),
                Font = new Font("Segoe UI", 7.5F, FontStyle.Italic),
                ForeColor = Color.Gray
            };
            grpVistaPrevia.Controls.Add(lblVistaPreviaHora);

            lstVistaPreviaSpots = new ListBox
            {
                Location = new Point(10, 36),
                Size = new Size(285, 75),
                Font = new Font("Consolas", 8F),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(248, 250, 255),
                SelectionMode = SelectionMode.None
            };
            grpVistaPrevia.Controls.Add(lstVistaPreviaSpots);

            pnlPautasContainer.Controls.Add(lblEstacionesCiudades);
            pnlPautasContainer.Controls.Add(dgv_estaciones);
            pnlPautasContainer.Controls.Add(dgv_ciudades);
            pnlPautasContainer.Controls.Add(lblPautasTitulo);
            pnlPautasContainer.Controls.Add(dgv_pautas);
            pnlPautasContainer.Controls.Add(grpEliminarPautas);
            pnlPautasContainer.Controls.Add(grpVistaPrevia);

            // Reorganizar layout: Pauteo Rapido ARRIBA, DataGridView ABAJO
            // Cambiar pnlBottomRight a Dock.Top para el Pauteo Rapido
            pnlBottomRight.Controls.Clear();
            pnlBottomRight.Dock = DockStyle.Top;
            pnlBottomRight.Height = 380; // Altura para el pauteo rapido (reducido para dar mas espacio a dgv_base)
            pauteoRapidoPanel.Panel.Dock = DockStyle.Fill;
            pnlBottomRight.Controls.Add(pauteoRapidoPanel.Panel);

            // pnlTopRight (con dgv_base) ahora queda abajo con Dock.Fill
            pnlTopRight.Dock = DockStyle.Fill;

            // Agregar al pnlMain (TableLayoutPanel) como una tercera columna
            if (pnlMain != null && pnlMain is TableLayoutPanel tableLayout)
            {
                if (tableLayout.ColumnCount < 3)
                {
                    tableLayout.ColumnCount = 3;
                    tableLayout.ColumnStyles.Clear();
                    tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 320F)); // Panel izquierdo (archivos)
                    tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));  // Panel central (grids)
                    tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 320F)); // Panel pautas/eliminar

                    pnlPautasContainer.Dock = DockStyle.Fill;
                    tableLayout.Controls.Add(pnlPautasContainer, 2, 0);
                }
            }
            else if (pnlRight != null)
            {
                pnlRight.Controls.Add(pnlPautasContainer);
                pnlPautasContainer.BringToFront();
            }

            System.Diagnostics.Debug.WriteLine("[PAUTEO RAPIDO] Panel inicializado correctamente");
        }

        private async void PauteoRapido_PautaGenerada(object sender, PauteoRapidoPanel.PautaGeneradaEventArgs e)
        {
            if (e.Exito)
            {
                // Invalidar caché y recargar datos
                InvalidarCache();
                await MostrarInfoRegistrosAsync();
                await CargarDatosAgrupadosAsync(true);

                System.Diagnostics.Debug.WriteLine($"[PAUTEO RAPIDO] Pauta generada exitosamente: {Path.GetFileName(e.AudioPath)}");
            }
        }

        /// <summary>
        /// Carga y muestra los spots pauteados para una hora específica (vista previa)
        /// </summary>
        private async void PauteoRapido_TandaClicked(object sender, PauteoRapidoPanel.TandaClickedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"[FORM1] TandaClicked - Hora: {e.Hora}, Ciudad: {e.Ciudad}, Radio: {e.Radio}, Fecha: {e.Fecha}");

            // Actualizar el panel de vista previa en el lado derecho
            if (lstVistaPreviaSpots == null || lblVistaPreviaHora == null) return;

            lstVistaPreviaSpots.Items.Clear();

            if (string.IsNullOrEmpty(e.Hora) || string.IsNullOrEmpty(e.Ciudad) || string.IsNullOrEmpty(e.Radio))
            {
                lblVistaPreviaHora.Text = "Seleccione ciudad/radio primero";
                lblVistaPreviaHora.ForeColor = Color.FromArgb(244, 67, 54);
                return;
            }

            lblVistaPreviaHora.Text = $"Hora: {e.Hora} - {e.Fecha:dd/MM/yyyy} - Cargando...";
            lblVistaPreviaHora.ForeColor = Color.FromArgb(33, 150, 243);
            lstVistaPreviaSpots.Items.Add("Cargando...");

            try
            {
                // Calcular fila (índice de hora)
                TipoTanda tipoTanda = DetectarTipoProgramacionPorRadio(e.Radio);
                int fila = TandasHorarias.GetFilaParaHora(e.Hora, tipoTanda);
                System.Diagnostics.Debug.WriteLine($"[FORM1] TandaClicked - TipoTanda: {tipoTanda}, Fila: {fila}");

                if (fila < 0)
                {
                    lblVistaPreviaHora.Text = $"Hora: {e.Hora} - Hora inválida";
                    lblVistaPreviaHora.ForeColor = Color.FromArgb(244, 67, 54);
                    lstVistaPreviaSpots.Items.Clear();
                    lstVistaPreviaSpots.Items.Add("Hora no válida para este tipo de tanda");
                    return;
                }

                // Calcular columna basada en día de semana de la fecha
                int diaSemana = (int)e.Fecha.DayOfWeek;
                int columna = diaSemana == 0 ? 8 : diaSemana + 1;

                // Obtener comerciales de esa hora usando caché para mayor velocidad
                var spots = await CacheService.ObtenerSpotsPorHoraCacheadosAsync(
                    fila, columna, e.Fecha, e.Ciudad, e.Radio);

                lstVistaPreviaSpots.Items.Clear();

                if (spots.Count == 0)
                {
                    lblVistaPreviaHora.Text = $"Hora: {e.Hora} - {e.Fecha:dd/MM/yyyy} (vacío)";
                    lblVistaPreviaHora.ForeColor = Color.Gray;
                    lstVistaPreviaSpots.Items.Add("(Sin spots programados)");
                }
                else
                {
                    lblVistaPreviaHora.Text = $"Hora: {e.Hora} - {e.Fecha:dd/MM/yyyy} ({spots.Count} spots)";
                    lblVistaPreviaHora.ForeColor = Color.FromArgb(76, 175, 80);

                    foreach (var spot in spots)
                    {
                        lstVistaPreviaSpots.Items.Add(spot);
                    }
                }
            }
            catch (Exception ex)
            {
                lblVistaPreviaHora.Text = $"Hora: {e.Hora} - Error";
                lblVistaPreviaHora.ForeColor = Color.FromArgb(244, 67, 54);
                lstVistaPreviaSpots.Items.Clear();
                lstVistaPreviaSpots.Items.Add("Error al cargar spots");
                System.Diagnostics.Debug.WriteLine($"[VISTA PREVIA] Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Envía un audio al panel de Pauteo Rápido desde el explorador de archivos
        /// </summary>
        private void EnviarAudioAPauteoRapido(string audioPath)
        {
            if (pauteoRapidoPanel != null)
            {
                pauteoRapidoPanel.SetAudioSeleccionado(audioPath);
                pauteoRapidoPanel.SetCiudadRadio(_filtroCiudadActual, _filtroRadioActual);
            }
        }

        /// <summary>
        /// Envía un audio al panel de Pauteo Rápido manteniendo la configuración actual
        /// Solo cambia el audio y avanza la posición automáticamente
        /// </summary>
        private void EnviarAudioAPauteoRapidoManteniendo(string audioPath)
        {
            if (pauteoRapidoPanel != null)
            {
                pauteoRapidoPanel.SetAudioManteniendo(audioPath);
            }
        }

        // =============================================
        // FILTROS POR CIUDAD Y RADIO
        // =============================================

        private void dgv_estaciones_SelectionChanged(object sender, EventArgs e)
        {
            if (dgv_estaciones.SelectedRows.Count > 0)
            {
                string radioSeleccionada = dgv_estaciones.SelectedRows[0].Cells[0].Value?.ToString();

                if (!string.IsNullOrEmpty(radioSeleccionada))
                {
                    System.Diagnostics.Debug.WriteLine($"[TIMING] *** Cambio de ESTACIÓN: {_filtroRadioActual ?? "(ninguna)"} -> {radioSeleccionada} ***");
                    _filtroRadioActual = radioSeleccionada;

                    // Actualizar tandas en el panel de pauteo rápido
                    pauteoRapidoPanel?.SetCiudadRadio(_filtroCiudadActual, radioSeleccionada);
                }

                // Aplicar filtro automaticamente cuando se selecciona una radio
                AplicarFiltrosCombinados();
            }
        }

        private void dgv_ciudades_SelectionChanged(object sender, EventArgs e)
        {
            if (dgv_ciudades.SelectedRows.Count > 0)
            {
                string ciudadSeleccionada = dgv_ciudades.SelectedRows[0].Cells[0].Value?.ToString();

                if (!string.IsNullOrEmpty(ciudadSeleccionada))
                {
                    System.Diagnostics.Debug.WriteLine($"[TIMING] *** Cambio de CIUDAD: {_filtroCiudadActual ?? "(ninguna)"} -> {ciudadSeleccionada} ***");
                    _filtroCiudadActual = ciudadSeleccionada;

                    // Actualizar ciudad en el panel de pauteo rápido
                    pauteoRapidoPanel?.SetCiudadRadio(ciudadSeleccionada, _filtroRadioActual);
                }

                // Aplicar filtro automaticamente cuando se selecciona una ciudad
                AplicarFiltrosCombinados();
            }
        }

        private async void AplicarFiltrosCombinados()
        {
            // No aplicar filtros durante la inicialización
            if (_inicializando)
                return;

            // Solo cargar datos si hay al menos una ciudad o estación seleccionada
            if (string.IsNullOrEmpty(_filtroCiudadActual) && string.IsNullOrEmpty(_filtroRadioActual))
            {
                dgv_base.Rows.Clear();
                _totalRegistros = 0;
                _totalArchivosUnicos = 0;
                ActualizarControlesPaginacion();
                return;
            }

            // Vista siempre agrupada - recargar con filtros de ciudad/radio
            _paginaActual = 1;
            await CargarDatosAgrupadosAsync();
        }

        /// <summary>
        /// Limpia los filtros de ciudad y radio
        /// </summary>
        public void LimpiarFiltrosCiudadRadio()
        {
            _filtroCiudadActual = null;
            _filtroRadioActual = null;

            // Limpiar el dgv_base ya que no hay filtro seleccionado
            dgv_base.Rows.Clear();
            _totalRegistros = 0;
            _totalArchivosUnicos = 0;
            ActualizarControlesPaginacion();

            // Deseleccionar filas en los dgv de filtro
            dgv_estaciones.ClearSelection();
            dgv_ciudades.ClearSelection();
        }

        /// <summary>
        /// Extrae el codigo numerico de un codigo completo (ej: ACC-42262-ABA-EXI-0000 -> 42262)
        /// </summary>
        private string ExtraerCodigoNumerico(string codigoCompleto)
        {
            if (string.IsNullOrEmpty(codigoCompleto))
                return "";

            // El formato es: ACC-NUMERO-CIUDAD-RADIO-HORA
            // Ejemplo: ACC-42262-ABA-EXI-0000
            string[] partes = codigoCompleto.Split('-');
            if (partes.Length >= 2)
            {
                return partes[1]; // Retorna solo el numero (42262)
            }
            return codigoCompleto;
        }

        #region Eliminar Pautas por Codigo/Fecha/Hora

        /// <summary>
        /// Inicializa el combo de horas para eliminacion (version sincrona basica)
        /// </summary>
        private void InicializarComboHorasEliminar(string radio = "")
        {
            cboHoraElim.Items.Clear();
            cboHoraElim.Items.Add("(Todas)");

            if (cboHoraElim.Items.Count > 0)
                cboHoraElim.SelectedIndex = 0;
        }

        /// <summary>
        /// Carga las horas que tienen pautas programadas para un codigo especifico
        /// </summary>
        private async Task CargarHorasPautadasParaCodigoAsync(string codigo, string ciudad, string radio)
        {
            cboHoraElim.Items.Clear();
            cboHoraElim.Items.Add("(Todas)");
            int cantidadHoras = 0;
            lblConteoHoras.Text = "";

            // Debug a archivo
            string debugPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "debug_horas.txt");
            try
            {
                using (var debugWriter = new System.IO.StreamWriter(debugPath, false))
                {
                    debugWriter.WriteLine($"=== DEBUG CargarHorasPautadas === {DateTime.Now}");
                    debugWriter.WriteLine($"Codigo: '{codigo}', Ciudad: '{ciudad}', Radio: '{radio}'");

                    // Detectar tipo de tanda según la radio
                    TipoTanda tipoTanda = DetectarTipoProgramacionPorRadio(radio);
                    debugWriter.WriteLine($"TipoTanda detectado: {tipoTanda}");

                    // Crear instancia del servicio de base de datos
                    var dbService = new DatabaseService();

                    // Obtener las horas únicas que tienen pautas para este codigo
                    // (busca en ComercialesAsignados y en los códigos de Comerciales)
                    // Filtra por ciudad y radio para obtener solo las horas correctas
                    debugWriter.Flush();
                    var horasUnicas = await dbService.ObtenerHorasUnicasPorCodigoAsync(codigo, tipoTanda, ciudad, radio, debugPath);

                    // Reabrir para continuar escribiendo
                    using (var debugWriter2 = new System.IO.StreamWriter(debugPath, true))
                    {
                        debugWriter2.WriteLine($"");
                        debugWriter2.WriteLine($"=== Resultado en Form1 ===");
                        debugWriter2.WriteLine($"Horas encontradas: {horasUnicas.Count}");
                        foreach (string h in horasUnicas)
                        {
                            debugWriter2.WriteLine($"  - Hora: {h}");
                        }

                        foreach (string horaStr in horasUnicas)
                        {
                            if (!cboHoraElim.Items.Contains(horaStr))
                            {
                                cboHoraElim.Items.Add(horaStr);
                                cantidadHoras++;
                            }
                        }

                        // Mostrar conteo de horarios en el label (arriba del combo)
                        if (cantidadHoras > 0)
                        {
                            lblConteoHoras.Text = $"({cantidadHoras} horarios)";
                        }
                        debugWriter2.WriteLine($"Cantidad final en combo: {cantidadHoras}");
                    }
                }
            }
            catch (Exception ex)
            {
                // Si falla el debug, al menos cargar las horas
                try
                {
                    TipoTanda tipoTanda = DetectarTipoProgramacionPorRadio(radio);
                    var dbService = new DatabaseService();
                    var horasUnicas = await dbService.ObtenerHorasUnicasPorCodigoAsync(codigo, tipoTanda, ciudad, radio, null);
                    foreach (string horaStr in horasUnicas)
                    {
                        if (!cboHoraElim.Items.Contains(horaStr))
                        {
                            cboHoraElim.Items.Add(horaStr);
                            cantidadHoras++;
                        }
                    }
                    if (cantidadHoras > 0)
                    {
                        lblConteoHoras.Text = $"({cantidadHoras} horarios)";
                    }
                }
                catch (Exception)
                {
                    // Error en fallback
                }
            }

            if (cboHoraElim.Items.Count > 0)
                cboHoraElim.SelectedIndex = 0;
        }

        /// <summary>
        /// Cuenta cuántos comerciales hay en la BD para un FilePath específico (diagnóstico)
        /// </summary>
        private async Task<int> ContarComercialesParaFilePathAsync(string filePath)
        {
            try
            {
                using (var conn = new Npgsql.NpgsqlConnection(DatabaseConfig.ConnectionString))
                {
                    await conn.OpenAsync();
                    using (var cmd = new Npgsql.NpgsqlCommand("SELECT COUNT(*) FROM Comerciales WHERE FilePath = @FilePath", conn))
                    {
                        cmd.Parameters.AddWithValue("@FilePath", filePath);
                        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
                    }
                }
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Detecta el tipo de tanda basándose en el nombre de la radio.
        /// KARIBEÑA y LA KALLE usan 4 tandas por hora (00, 20, 30, 50).
        /// Las demás radios usan 2 tandas por hora (00, 30).
        /// </summary>
        private TipoTanda DetectarTipoProgramacionPorRadio(string radio)
        {
            if (string.IsNullOrEmpty(radio))
                return TipoTanda.Tandas_00_30;

            string radioUpper = radio.ToUpper();

            // KARIBEÑA y LA KALLE usan las 4 tandas: 00, 20, 30, 50
            // Incluir variantes de codificación: KARIBEÑA, KARIBENA, KARIBEÃA (UTF-8 mal interpretado)
            if (radioUpper.Contains("KARIBEÑA") || radioUpper.Contains("KARIBENA") ||
                radioUpper.Contains("KARIBEÃ") || radioUpper.Contains("KARIBE") ||
                radioUpper.Contains("LAKALLE") || radioUpper.Contains("LA KALLE") || radioUpper.Contains("KALLE"))
            {
                return TipoTanda.Tandas_00_20_30_50;
            }

            // EXITOSA y otros usan 00-30 por defecto
            return TipoTanda.Tandas_00_30;
        }

        /// <summary>
        /// Elimina pautas asignadas por codigo y rango de fechas
        /// </summary>
        private async void btnEliminarPorFechas_Click(object sender, EventArgs e)
        {
            string codigoBuscar = txtCodigoEliminar.Text.Trim();

            if (string.IsNullOrEmpty(codigoBuscar))
            {
                MessageBox.Show("Ingrese un codigo para buscar.", "Codigo Requerido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DateTime fechaInicio = dtpFechaElimI.Value.Date;
            DateTime fechaFin = dtpFechaElimF.Value.Date;

            if (fechaInicio > fechaFin)
            {
                MessageBox.Show("La fecha inicial no puede ser mayor que la fecha final.", "Fechas Invalidas", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Obtener ciudad y radio del comercial seleccionado
            string ciudad = "";
            string radio = "";
            if (dgv_base.SelectedRows.Count > 0)
            {
                ciudad = dgv_base.SelectedRows[0].Cells["Ciudad"]?.Value?.ToString() ?? "";
                radio = dgv_base.SelectedRows[0].Cells["Radio"]?.Value?.ToString() ?? "";
            }

            // Confirmar eliminacion
            DialogResult confirmar = MessageBox.Show(
                $"Se eliminaran las pautas del codigo '{codigoBuscar}' entre las fechas:\n\n" +
                $"Desde: {fechaInicio:dd/MM/yyyy}\n" +
                $"Hasta: {fechaFin:dd/MM/yyyy}\n\n" +
                $"¿Esta seguro?",
                "Confirmar Eliminacion",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirmar != DialogResult.Yes)
            {
                return;
            }

            try
            {
                var dbService = new DatabaseService();
                int eliminados = await dbService.EliminarComercialesAsignadosPorCodigoYFechasAsync(
                    codigoBuscar, fechaInicio, fechaFin, ciudad, radio);

                // Regenerar archivos TXT para las fechas eliminadas
                if (eliminados > 0 && !string.IsNullOrEmpty(ciudad) && !string.IsNullOrEmpty(radio))
                {
                    try
                    {
                        var generador = new GenerarPauta();
                        for (DateTime fecha = fechaInicio.Date; fecha <= fechaFin.Date; fecha = fecha.AddDays(1))
                        {
                            await generador.RegenerarArchivoPorFechaAsync(fecha, ciudad, radio);
                        }
                    }
                    catch (Exception)
                    {
                        // Error al regenerar TXT
                    }
                }

                // Actualizar el grid de pautas y los DateTimePicker
                if (dgv_base.SelectedRows.Count > 0)
                {
                    string codigoSeleccionado = dgv_base.SelectedRows[0].Cells["Codigo"]?.Value?.ToString();
                    if (!string.IsNullOrEmpty(codigoSeleccionado))
                    {
                        await CargarPautasDelComercialAsync(codigoSeleccionado);

                        // Actualizar los DateTimePicker y la tabla Comerciales con las nuevas fechas
                        await ActualizarFechasDelComercialAsync(codigoSeleccionado);
                    }
                }

                // Notificar cambio en BD para que otros formularios se actualicen
                ConfigManager.NotificarCambioEnBD();

                MessageBox.Show($"Se eliminaron {eliminados} registros de pautas.\nLos archivos TXT han sido regenerados.",
                    "Eliminacion Completada", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al eliminar pautas: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Elimina pautas asignadas por codigo, hora especifica Y rango de fechas
        /// </summary>
        private async void btnEliminarPorHora_Click(object sender, EventArgs e)
        {
            string codigoBuscar = txtCodigoEliminar.Text.Trim();

            if (string.IsNullOrEmpty(codigoBuscar))
            {
                MessageBox.Show("Ingrese un codigo para buscar.", "Codigo Requerido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string horaSeleccionada = cboHoraElim.SelectedItem?.ToString();
            bool eliminarTodasHoras = string.IsNullOrEmpty(horaSeleccionada) || horaSeleccionada == "(Todas)";

            // Obtener rango de fechas
            DateTime fechaInicio = dtpFechaElimI.Value.Date;
            DateTime fechaFin = dtpFechaElimF.Value.Date;

            if (fechaInicio > fechaFin)
            {
                MessageBox.Show("La fecha inicial no puede ser mayor que la fecha final.", "Fechas Invalidas", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Obtener ciudad y radio del comercial seleccionado
            string ciudad = "";
            string radio = "";
            if (dgv_base.SelectedRows.Count > 0)
            {
                ciudad = dgv_base.SelectedRows[0].Cells["Ciudad"]?.Value?.ToString() ?? "";
                radio = dgv_base.SelectedRows[0].Cells["Radio"]?.Value?.ToString() ?? "";
            }

            // Confirmar eliminacion
            string mensajeHora = eliminarTodasHoras ? "TODAS las horas" : $"la hora '{horaSeleccionada}'";
            DialogResult confirmar = MessageBox.Show(
                $"Se eliminaran las pautas del codigo '{codigoBuscar}' en {mensajeHora}.\n\n" +
                $"Desde: {fechaInicio:dd/MM/yyyy}\n" +
                $"Hasta: {fechaFin:dd/MM/yyyy}\n\n" +
                $"¿Esta seguro?",
                "Confirmar Eliminacion",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirmar != DialogResult.Yes)
            {
                return;
            }

            try
            {
                var dbService = new DatabaseService();
                int eliminados = 0;

                if (eliminarTodasHoras)
                {
                    // Si es "Todas las horas", usar el método de eliminar por fechas
                    eliminados = await dbService.EliminarComercialesAsignadosPorCodigoYFechasAsync(
                        codigoBuscar, fechaInicio, fechaFin, ciudad, radio);
                }
                else
                {
                    // Calcular el indice de fila para la hora seleccionada
                    int fila = CalcularFilaParaHora(horaSeleccionada);

                    if (fila >= 0)
                    {
                        // Nuevo método que combina hora + rango de fechas
                        eliminados = await dbService.EliminarComercialesAsignadosPorCodigoHoraYFechasAsync(
                            codigoBuscar, fila, fechaInicio, fechaFin, ciudad, radio);
                    }
                }

                // Regenerar archivos TXT para las fechas eliminadas
                if (eliminados > 0 && !string.IsNullOrEmpty(ciudad) && !string.IsNullOrEmpty(radio))
                {
                    try
                    {
                        lblUsuarioActual.Text = "Regenerando pautas...";
                        Application.DoEvents();

                        var generador = new GenerarPauta();
                        var progreso = new Progress<(int porcentaje, string mensaje)>(p => {
                            lblUsuarioActual.Text = p.mensaje;
                            Application.DoEvents();
                        });

                        await generador.RegenerarArchivosParaRangoAsync(fechaInicio, fechaFin, ciudad, radio, progreso);
                    }
                    catch (Exception)
                    {
                        // Error al regenerar TXT, no interrumpir el flujo
                    }
                }

                MessageBox.Show($"Se eliminaron {eliminados} registros de pautas y se actualizaron los archivos.",
                    "Eliminacion Completada", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Actualizar el grid de pautas
                if (dgv_base.SelectedRows.Count > 0)
                {
                    string codigoSeleccionado = dgv_base.SelectedRows[0].Cells["Codigo"]?.Value?.ToString();
                    if (!string.IsNullOrEmpty(codigoSeleccionado))
                    {
                        await CargarPautasDelComercialAsync(codigoSeleccionado);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al eliminar pautas: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                lblUsuarioActual.Text = "";
            }
        }

        /// <summary>
        /// Calcula el indice de fila para una hora en formato "HH:MM"
        /// </summary>
        private int CalcularFilaParaHora(string hora)
        {
            if (string.IsNullOrEmpty(hora)) return -1;

            // Obtener el tipo de tanda del comercial seleccionado
            TipoTanda tipoTanda = TipoTanda.Tandas_00_30; // Por defecto
            if (dgv_base.SelectedRows.Count > 0)
            {
                string radio = dgv_base.SelectedRows[0].Cells["Radio"]?.Value?.ToString() ?? "";
                tipoTanda = DetectarTipoProgramacionPorRadio(radio);
            }

            // Usar TandasHorarias para obtener el índice correcto
            return TandasHorarias.GetFilaParaHora(hora, tipoTanda);
        }

        /// <summary>
        /// Carga las pautas de un comercial especifico
        /// ULTRA OPTIMIZADO: Usa DataTable + VirtualMode para carga instantánea
        /// </summary>
        private async Task CargarPautasDelComercialAsync(string codigo)
        {
            try
            {
                // Mostrar indicador de carga
                lblPautasTitulo.Text = "Cargando...";
                lblPautasTitulo.Visible = true;

                var dbService = new DatabaseService();

                // Obtener nombre del archivo primero (instantáneo, ya está en memoria)
                string nombreArchivo = "";
                if (dgv_base.SelectedRows.Count > 0)
                {
                    nombreArchivo = dgv_base.SelectedRows[0].Cells["NombreArchivo"]?.Value?.ToString() ?? "";
                    nombreArchivo = System.IO.Path.GetFileNameWithoutExtension(nombreArchivo);
                }

                // Ejecutar TODO en un hilo de fondo para no bloquear UI
                var resultado = await Task.Run(async () =>
                {
                    // Consultas en paralelo
                    var tareasTipoTanda = ObtenerTipoTandaDelComercialAsyncInternal(codigo);
                    var tareasFechasHoras = dbService.ObtenerFechasYHorasUnicasAsync(codigo);

                    await Task.WhenAll(tareasTipoTanda, tareasFechasHoras);

                    TipoTanda tipoTanda = tareasTipoTanda.Result;
                    var (fechasUnicas, horasUnicas) = tareasFechasHoras.Result;
                    string[] horarios = TandasHorarias.GetHorarios(tipoTanda);

                    int totalPautas = fechasUnicas.Count * horasUnicas.Count;

                    // Crear DataTable en memoria (mucho más rápido que agregar filas)
                    var dt = new System.Data.DataTable();
                    dt.Columns.Add("Fecha", typeof(string));
                    dt.Columns.Add("Hora", typeof(string));

                    // Ordenar una sola vez
                    var fechasOrdenadas = fechasUnicas.OrderBy(f => f).ToArray();
                    var horasOrdenadas = horasUnicas.OrderBy(h => h).ToArray();

                    if (fechasUnicas.Count <= 31)
                    {
                        // Rangos cortos: mostrar fecha+hora
                        foreach (var fecha in fechasOrdenadas)
                        {
                            string fechaStr = fecha.ToString("dd/MM/yyyy");
                            foreach (var fila in horasOrdenadas)
                            {
                                string horaStr = fila >= 0 && fila < horarios.Length
                                    ? horarios[fila]
                                    : $"{fila / 2:D2}:{(fila % 2) * 30:D2}";
                                dt.Rows.Add(fechaStr, horaStr);
                            }
                        }
                    }
                    else
                    {
                        // Rangos largos: solo fechas con resumen
                        string resumenHoras = $"{horasUnicas.Count} horas";
                        foreach (var fecha in fechasOrdenadas)
                        {
                            dt.Rows.Add(fecha.ToString("dd/MM/yyyy"), resumenHoras);
                        }
                    }

                    return (DataTable: dt, TotalPautas: totalPautas);
                });

                // Limpiar columnas existentes antes de asignar DataSource
                // para evitar columnas duplicadas
                dgv_pautas.DataSource = null;
                dgv_pautas.Columns.Clear();

                // Asignar DataTable al grid (instantáneo)
                dgv_pautas.DataSource = resultado.DataTable;

                // Ajustar columnas y estilos
                if (dgv_pautas.Columns.Count > 0)
                {
                    dgv_pautas.Columns[0].Width = 90;
                    dgv_pautas.Columns[1].Width = 70;

                    // Aplicar estilos
                    dgv_pautas.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(63, 81, 181);
                    dgv_pautas.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
                    dgv_pautas.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold);
                    dgv_pautas.DefaultCellStyle.Font = new Font("Segoe UI", 8.5F);
                    dgv_pautas.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(245, 248, 255);
                }

                lblPautasTitulo.Text = $"{nombreArchivo} ({resultado.TotalPautas})";
            }
            catch (Exception)
            {
                lblPautasTitulo.Text = "Error";
            }
        }

        /// <summary>
        /// Actualiza los DateTimePicker con las fechas mínima y máxima de las pautas restantes.
        /// También actualiza la FechaInicio en la tabla Comerciales si es necesario.
        /// </summary>
        private async Task ActualizarFechasDelComercialAsync(string codigo)
        {
            try
            {
                using (var conn = new Npgsql.NpgsqlConnection(DatabaseConfig.ConnectionString))
                {
                    await conn.OpenAsync();

                    DateTime? nuevaFechaMin = null;
                    DateTime? nuevaFechaMax = null;

                    // 1. Buscar fechas en registros CON fecha
                    string queryConFecha = @"SELECT MIN(Fecha) as FechaMin, MAX(Fecha) as FechaMax
                                             FROM ComercialesAsignados
                                             WHERE Codigo LIKE @Codigo
                                             AND Fecha IS NOT NULL";

                    using (var cmd = new Npgsql.NpgsqlCommand(queryConFecha, conn))
                    {
                        cmd.Parameters.AddWithValue("@Codigo", $"%{codigo}%");
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                if (reader["FechaMin"] != DBNull.Value)
                                    nuevaFechaMin = Convert.ToDateTime(reader["FechaMin"]);
                                if (reader["FechaMax"] != DBNull.Value)
                                    nuevaFechaMax = Convert.ToDateTime(reader["FechaMax"]);
                            }
                        }
                    }

                    // 2. Si no hay registros con fecha, buscar en registros legacy (por columna)
                    if (!nuevaFechaMin.HasValue)
                    {
                        string queryLegacy = @"SELECT c.FechaInicio, MIN(ca.Columna) as ColMin, MAX(ca.Columna) as ColMax
                                               FROM ComercialesAsignados ca
                                               INNER JOIN Comerciales c ON ca.Codigo = c.Codigo
                                               WHERE ca.Codigo LIKE @Codigo
                                               AND ca.Fecha IS NULL
                                               GROUP BY c.FechaInicio";

                        using (var cmd = new Npgsql.NpgsqlCommand(queryLegacy, conn))
                        {
                            cmd.Parameters.AddWithValue("@Codigo", $"%{codigo}%");
                            using (var reader = await cmd.ExecuteReaderAsync())
                            {
                                if (await reader.ReadAsync())
                                {
                                    DateTime fechaInicioComercial = Convert.ToDateTime(reader["FechaInicio"]);
                                    int colMin = Convert.ToInt32(reader["ColMin"]);
                                    int colMax = Convert.ToInt32(reader["ColMax"]);

                                    nuevaFechaMin = fechaInicioComercial.AddDays(colMin - 2);
                                    nuevaFechaMax = fechaInicioComercial.AddDays(colMax - 2);
                                }
                            }
                        }
                    }

                    // 3. Actualizar los DateTimePicker
                    if (nuevaFechaMin.HasValue && nuevaFechaMax.HasValue)
                    {
                        dtpFechaElimI.Value = nuevaFechaMin.Value;
                        dtpFechaElimF.Value = nuevaFechaMax.Value;

                        // 4. Actualizar FechaInicio en la tabla Comerciales si cambió
                        string queryUpdateFechaInicio = @"UPDATE Comerciales
                                                          SET FechaInicio = @NuevaFechaInicio
                                                          WHERE Codigo LIKE @Codigo
                                                          AND FechaInicio < @NuevaFechaInicio";

                        using (var cmdUpdate = new Npgsql.NpgsqlCommand(queryUpdateFechaInicio, conn))
                        {
                            cmdUpdate.Parameters.AddWithValue("@Codigo", $"%{codigo}%");
                            cmdUpdate.Parameters.AddWithValue("@NuevaFechaInicio", nuevaFechaMin.Value);
                            int actualizados = await cmdUpdate.ExecuteNonQueryAsync();
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Error al actualizar fechas
            }
        }

        /// <summary>
        /// Versión interna para ejecutar en hilo de fondo
        /// </summary>
        private async Task<TipoTanda> ObtenerTipoTandaDelComercialAsyncInternal(string codigo)
        {
            try
            {
                using (var conn = new Npgsql.NpgsqlConnection(DatabaseConfig.ConnectionString))
                {
                    await conn.OpenAsync();
                    // Usar LIKE para soportar tanto "0009" como "CU-0009"
                    string query = @"SELECT TipoProgramacion FROM Comerciales WHERE Codigo LIKE @Codigo LIMIT 1";
                    using (var cmd = new Npgsql.NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Codigo", $"%{codigo}%");
                        var result = await cmd.ExecuteScalarAsync();
                        if (result != null && result != DBNull.Value)
                        {
                            return TandasHorarias.GetTipoTandaFromString(result.ToString());
                        }
                    }
                }
            }
            catch { }
            return TipoTanda.Tandas_00_30;
        }

        /// <summary>
        /// Obtiene el TipoTanda desde la BD consultando el TipoProgramacion guardado para un comercial específico
        /// </summary>
        private async Task<TipoTanda> ObtenerTipoTandaDelComercialAsync(string codigo)
        {
            try
            {
                using (var conn = new Npgsql.NpgsqlConnection(DatabaseConfig.ConnectionString))
                {
                    await conn.OpenAsync();

                    string query = @"SELECT TipoProgramacion FROM Comerciales WHERE Codigo = @Codigo";

                    using (var cmd = new Npgsql.NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Codigo", codigo);

                        var result = await cmd.ExecuteScalarAsync();
                        if (result != null && result != DBNull.Value)
                        {
                            string tipoProgramacion = result.ToString();
                            return TandasHorarias.GetTipoTandaFromString(tipoProgramacion);
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Error al obtener TipoTanda
            }

            // Fallback: usar 48 tandas por defecto
            return TipoTanda.Tandas_00_30;
        }

        #endregion

    }
}

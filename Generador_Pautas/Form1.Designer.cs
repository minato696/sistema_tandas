namespace Generador_Pautas
{
    partial class Form1
    {
        /// <summary>
        /// Variable del diseñador necesaria.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Limpiar los recursos que se estén usando.
        /// </summary>
        /// <param name="disposing">true si los recursos administrados se deben desechar; false en caso contrario.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.Shown -= Form1_Shown;

                if (components != null)
                {
                    components.Dispose();
                }
            }
            base.Dispose(disposing);
        }


        #region Código generado por el Diseñador de Windows Forms

        /// <summary>
        /// Método necesario para admitir el Diseñador. No se puede modificar
        /// el contenido de este método con el editor de código.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.pnlMain = new System.Windows.Forms.TableLayoutPanel();
            this.pnlLeft = new System.Windows.Forms.Panel();
            this.dgv_archivos = new System.Windows.Forms.DataGridView();
            this.Column1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column3 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column4 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column5 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.pnlPlayer = new System.Windows.Forms.Panel();
            this.btn_limpiar = new System.Windows.Forms.Button();
            this.btn_pause = new System.Windows.Forms.Button();
            this.btn_stop = new System.Windows.Forms.Button();
            this.btn_play = new System.Windows.Forms.Button();
            this.elegantProgressBar1 = new System.Windows.Forms.ProgressBar();
            this.progressBarRight = new System.Windows.Forms.ProgressBar();
            this.progressBarLeft = new System.Windows.Forms.ProgressBar();
            this.pnlRight = new System.Windows.Forms.Panel();
            this.pnlTopRight = new System.Windows.Forms.Panel();
            this.dgv_base = new System.Windows.Forms.DataGridView();
            this.Column20 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column21 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column22 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column23 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column24 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column25 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column26 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column27 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.pnlFiltros = new System.Windows.Forms.Panel();
            this.btnLimpiarFiltro = new System.Windows.Forms.Button();
            this.btnBuscar = new System.Windows.Forms.Button();
            this.txtBusqueda = new System.Windows.Forms.TextBox();
            this.cboFiltroColumna = new System.Windows.Forms.ComboBox();
            this.lblFiltroColumna = new System.Windows.Forms.Label();
            this.cboFiltroEstado = new System.Windows.Forms.ComboBox();
            this.lblFiltroEstado = new System.Windows.Forms.Label();
            this.pnlToolbar = new System.Windows.Forms.Panel();
            this.dashboardControl1 = new System.Windows.Forms.Panel();
            this.pnlBottomRight = new System.Windows.Forms.Panel();
            this.dgv_pautas = new System.Windows.Forms.DataGridView();
            this.grpEliminarPautas = new System.Windows.Forms.GroupBox();
            this.lblCodigoEliminar = new System.Windows.Forms.Label();
            this.txtCodigoEliminar = new System.Windows.Forms.TextBox();
            this.lblFechaElimI = new System.Windows.Forms.Label();
            this.dtpFechaElimI = new System.Windows.Forms.DateTimePicker();
            this.lblFechaElimF = new System.Windows.Forms.Label();
            this.dtpFechaElimF = new System.Windows.Forms.DateTimePicker();
            this.lblHoraElim = new System.Windows.Forms.Label();
            this.lblConteoHoras = new System.Windows.Forms.Label();
            this.cboHoraElim = new System.Windows.Forms.ComboBox();
            this.btnEliminarPorFechas = new System.Windows.Forms.Button();
            this.btnEliminarPorHora = new System.Windows.Forms.Button();
            this.lblTotalPautas = new System.Windows.Forms.Label();
            this.lblPautasTitulo = new System.Windows.Forms.Label();
            this.dgv_ciudades = new System.Windows.Forms.DataGridView();
            this.dataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dgv_estaciones = new System.Windows.Forms.DataGridView();
            this.Column6 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.lblCreditos = new System.Windows.Forms.ToolStripStatusLabel();
            this.lblVersion = new System.Windows.Forms.ToolStripStatusLabel();
            this.pnlMain.SuspendLayout();
            this.pnlLeft.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgv_archivos)).BeginInit();
            this.pnlPlayer.SuspendLayout();
            this.pnlRight.SuspendLayout();
            this.pnlTopRight.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgv_base)).BeginInit();
            this.pnlFiltros.SuspendLayout();
            this.pnlToolbar.SuspendLayout();
            this.pnlBottomRight.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgv_pautas)).BeginInit();
            this.grpEliminarPautas.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgv_ciudades)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgv_estaciones)).BeginInit();
            this.statusStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnlMain
            // 
            this.pnlMain.ColumnCount = 2;
            this.pnlMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 320F));
            this.pnlMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.pnlMain.Controls.Add(this.pnlLeft, 0, 0);
            this.pnlMain.Controls.Add(this.pnlRight, 1, 0);
            this.pnlMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlMain.Location = new System.Drawing.Point(0, 0);
            this.pnlMain.Name = "pnlMain";
            this.pnlMain.Padding = new System.Windows.Forms.Padding(10);
            this.pnlMain.RowCount = 1;
            this.pnlMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.pnlMain.Size = new System.Drawing.Size(1400, 750);
            this.pnlMain.TabIndex = 0;
            // 
            // pnlLeft
            // 
            this.pnlLeft.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(245)))), ((int)(((byte)(250)))));
            this.pnlLeft.Controls.Add(this.dgv_archivos);
            this.pnlLeft.Controls.Add(this.pnlPlayer);
            this.pnlLeft.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlLeft.Location = new System.Drawing.Point(13, 13);
            this.pnlLeft.Name = "pnlLeft";
            this.pnlLeft.Size = new System.Drawing.Size(314, 724);
            this.pnlLeft.TabIndex = 0;
            // 
            // dgv_archivos
            // 
            this.dgv_archivos.AllowUserToAddRows = false;
            this.dgv_archivos.AllowUserToDeleteRows = false;
            this.dgv_archivos.AllowUserToResizeRows = false;
            this.dgv_archivos.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgv_archivos.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(248)))), ((int)(((byte)(250)))));
            this.dgv_archivos.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.dgv_archivos.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.SingleHorizontal;
            this.dgv_archivos.ColumnHeadersHeight = 35;
            this.dgv_archivos.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.dgv_archivos.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Column1,
            this.Column2,
            this.Column3,
            this.Column4,
            this.Column5});
            this.dgv_archivos.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgv_archivos.EnableHeadersVisualStyles = false;
            this.dgv_archivos.GridColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(210)))), ((int)(((byte)(220)))));
            this.dgv_archivos.Location = new System.Drawing.Point(0, 0);
            this.dgv_archivos.Name = "dgv_archivos";
            this.dgv_archivos.ReadOnly = true;
            this.dgv_archivos.RowHeadersVisible = false;
            this.dgv_archivos.RowTemplate.Height = 28;
            this.dgv_archivos.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgv_archivos.Size = new System.Drawing.Size(314, 594);
            this.dgv_archivos.TabIndex = 1;
            // 
            // Column1
            // 
            this.Column1.FillWeight = 25F;
            this.Column1.HeaderText = "#";
            this.Column1.MinimumWidth = 30;
            this.Column1.Name = "Column1";
            this.Column1.ReadOnly = true;
            // 
            // Column2
            // 
            this.Column2.FillWeight = 120F;
            this.Column2.HeaderText = "Nombre";
            this.Column2.MinimumWidth = 100;
            this.Column2.Name = "Column2";
            this.Column2.ReadOnly = true;
            // 
            // Column3
            // 
            this.Column3.FillWeight = 45F;
            this.Column3.HeaderText = "Dur.";
            this.Column3.MinimumWidth = 45;
            this.Column3.Name = "Column3";
            this.Column3.ReadOnly = true;
            // 
            // Column4
            // 
            this.Column4.FillWeight = 45F;
            this.Column4.HeaderText = "Pos.";
            this.Column4.MinimumWidth = 45;
            this.Column4.Name = "Column4";
            this.Column4.ReadOnly = true;
            // 
            // Column5
            // 
            this.Column5.FillWeight = 50F;
            this.Column5.HeaderText = "Kbps";
            this.Column5.MinimumWidth = 55;
            this.Column5.Name = "Column5";
            this.Column5.ReadOnly = true;
            // 
            // pnlPlayer
            // 
            this.pnlPlayer.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(225)))), ((int)(((byte)(230)))), ((int)(((byte)(240)))));
            this.pnlPlayer.Controls.Add(this.btn_limpiar);
            this.pnlPlayer.Controls.Add(this.btn_pause);
            this.pnlPlayer.Controls.Add(this.btn_stop);
            this.pnlPlayer.Controls.Add(this.btn_play);
            this.pnlPlayer.Controls.Add(this.elegantProgressBar1);
            this.pnlPlayer.Controls.Add(this.progressBarRight);
            this.pnlPlayer.Controls.Add(this.progressBarLeft);
            this.pnlPlayer.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlPlayer.Location = new System.Drawing.Point(0, 594);
            this.pnlPlayer.Name = "pnlPlayer";
            this.pnlPlayer.Padding = new System.Windows.Forms.Padding(8);
            this.pnlPlayer.Size = new System.Drawing.Size(314, 130);
            this.pnlPlayer.TabIndex = 2;
            // 
            // btn_limpiar
            // 
            this.btn_limpiar.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(120)))), ((int)(((byte)(144)))), ((int)(((byte)(156)))));
            this.btn_limpiar.FlatAppearance.BorderSize = 0;
            this.btn_limpiar.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_limpiar.Font = new System.Drawing.Font("Segoe UI Semibold", 7F, System.Drawing.FontStyle.Bold);
            this.btn_limpiar.ForeColor = System.Drawing.Color.White;
            this.btn_limpiar.Location = new System.Drawing.Point(233, 82);
            this.btn_limpiar.Name = "btn_limpiar";
            this.btn_limpiar.Size = new System.Drawing.Size(70, 36);
            this.btn_limpiar.TabIndex = 6;
            this.btn_limpiar.Text = "LIMPIAR";
            this.btn_limpiar.UseVisualStyleBackColor = false;
            this.btn_limpiar.Click += new System.EventHandler(this.btn_limpiar_Click);
            // 
            // btn_pause
            // 
            this.btn_pause.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(152)))), ((int)(((byte)(0)))));
            this.btn_pause.FlatAppearance.BorderSize = 0;
            this.btn_pause.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_pause.Font = new System.Drawing.Font("Segoe UI Semibold", 6.5F, System.Drawing.FontStyle.Bold);
            this.btn_pause.ForeColor = System.Drawing.Color.White;
            this.btn_pause.Location = new System.Drawing.Point(158, 82);
            this.btn_pause.Name = "btn_pause";
            this.btn_pause.Size = new System.Drawing.Size(70, 36);
            this.btn_pause.TabIndex = 5;
            this.btn_pause.Text = "ELIMINAR";
            this.btn_pause.UseVisualStyleBackColor = false;
            this.btn_pause.Click += new System.EventHandler(this.btn_eliminar_archivo_Click);
            // 
            // btn_stop
            // 
            this.btn_stop.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(244)))), ((int)(((byte)(67)))), ((int)(((byte)(54)))));
            this.btn_stop.FlatAppearance.BorderSize = 0;
            this.btn_stop.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_stop.Font = new System.Drawing.Font("Segoe UI Semibold", 7F, System.Drawing.FontStyle.Bold);
            this.btn_stop.ForeColor = System.Drawing.Color.White;
            this.btn_stop.Location = new System.Drawing.Point(83, 82);
            this.btn_stop.Name = "btn_stop";
            this.btn_stop.Size = new System.Drawing.Size(70, 36);
            this.btn_stop.TabIndex = 4;
            this.btn_stop.Text = "STOP";
            this.btn_stop.UseVisualStyleBackColor = false;
            this.btn_stop.Click += new System.EventHandler(this.btn_stop_Click);
            // 
            // btn_play
            // 
            this.btn_play.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(76)))), ((int)(((byte)(175)))), ((int)(((byte)(80)))));
            this.btn_play.FlatAppearance.BorderSize = 0;
            this.btn_play.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_play.Font = new System.Drawing.Font("Segoe UI Semibold", 7F, System.Drawing.FontStyle.Bold);
            this.btn_play.ForeColor = System.Drawing.Color.White;
            this.btn_play.Location = new System.Drawing.Point(8, 82);
            this.btn_play.Name = "btn_play";
            this.btn_play.Size = new System.Drawing.Size(70, 36);
            this.btn_play.TabIndex = 3;
            this.btn_play.Text = "PLAY";
            this.btn_play.UseVisualStyleBackColor = false;
            this.btn_play.Click += new System.EventHandler(this.btn_play_Click);
            // 
            // elegantProgressBar1
            // 
            this.elegantProgressBar1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.elegantProgressBar1.Location = new System.Drawing.Point(8, 48);
            this.elegantProgressBar1.Name = "elegantProgressBar1";
            this.elegantProgressBar1.Size = new System.Drawing.Size(298, 20);
            this.elegantProgressBar1.TabIndex = 2;
            // 
            // progressBarRight
            // 
            this.progressBarRight.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.progressBarRight.Location = new System.Drawing.Point(8, 28);
            this.progressBarRight.Name = "progressBarRight";
            this.progressBarRight.Size = new System.Drawing.Size(298, 12);
            this.progressBarRight.TabIndex = 1;
            // 
            // progressBarLeft
            // 
            this.progressBarLeft.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.progressBarLeft.Location = new System.Drawing.Point(8, 12);
            this.progressBarLeft.Name = "progressBarLeft";
            this.progressBarLeft.Size = new System.Drawing.Size(298, 12);
            this.progressBarLeft.TabIndex = 0;
            // 
            // pnlRight
            // 
            this.pnlRight.Controls.Add(this.pnlTopRight);
            this.pnlRight.Controls.Add(this.pnlBottomRight);
            this.pnlRight.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlRight.Location = new System.Drawing.Point(333, 13);
            this.pnlRight.Name = "pnlRight";
            this.pnlRight.Size = new System.Drawing.Size(1054, 724);
            this.pnlRight.TabIndex = 1;
            // 
            // pnlTopRight
            // 
            this.pnlTopRight.Controls.Add(this.dgv_base);
            this.pnlTopRight.Controls.Add(this.pnlFiltros);
            this.pnlTopRight.Controls.Add(this.pnlToolbar);
            this.pnlTopRight.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlTopRight.Location = new System.Drawing.Point(0, 0);
            this.pnlTopRight.Name = "pnlTopRight";
            this.pnlTopRight.Size = new System.Drawing.Size(1054, 524);
            this.pnlTopRight.TabIndex = 0;
            // 
            // dgv_base
            // 
            this.dgv_base.AllowUserToAddRows = false;
            this.dgv_base.AllowUserToDeleteRows = false;
            this.dgv_base.AllowUserToResizeRows = false;
            this.dgv_base.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgv_base.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.dgv_base.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.dgv_base.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.SingleHorizontal;
            this.dgv_base.ColumnHeadersHeight = 40;
            this.dgv_base.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.dgv_base.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Column20,
            this.Column21,
            this.Column22,
            this.Column23,
            this.Column24,
            this.Column25,
            this.Column26,
            this.Column27});
            this.dgv_base.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgv_base.EnableHeadersVisualStyles = false;
            this.dgv_base.GridColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(50)))), ((int)(((byte)(50)))));
            this.dgv_base.Location = new System.Drawing.Point(0, 100);
            this.dgv_base.Name = "dgv_base";
            this.dgv_base.ReadOnly = true;
            this.dgv_base.RowHeadersVisible = false;
            this.dgv_base.RowTemplate.Height = 32;
            this.dgv_base.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgv_base.Size = new System.Drawing.Size(1054, 424);
            this.dgv_base.TabIndex = 2;
            this.dgv_base.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgv_base_CellDoubleClick);
            // 
            // Column20
            // 
            this.Column20.FillWeight = 70F;
            this.Column20.HeaderText = "Código";
            this.Column20.Name = "Column20";
            this.Column20.ReadOnly = true;
            // 
            // Column21
            // 
            this.Column21.FillWeight = 200F;
            this.Column21.HeaderText = "Spot";
            this.Column21.Name = "Column21";
            this.Column21.ReadOnly = true;
            // 
            // Column22
            // 
            this.Column22.FillWeight = 90F;
            this.Column22.HeaderText = "Inicia";
            this.Column22.Name = "Column22";
            this.Column22.ReadOnly = true;
            // 
            // Column23
            // 
            this.Column23.FillWeight = 90F;
            this.Column23.HeaderText = "Hasta";
            this.Column23.Name = "Column23";
            this.Column23.ReadOnly = true;
            // 
            // Column24
            // 
            this.Column24.FillWeight = 80F;
            this.Column24.HeaderText = "Ciudad";
            this.Column24.Name = "Column24";
            this.Column24.ReadOnly = true;
            // 
            // Column25
            // 
            this.Column25.FillWeight = 80F;
            this.Column25.HeaderText = "Radio";
            this.Column25.Name = "Column25";
            this.Column25.ReadOnly = true;
            // 
            // Column26
            // 
            this.Column26.FillWeight = 45F;
            this.Column26.HeaderText = "Pos";
            this.Column26.Name = "Column26";
            this.Column26.ReadOnly = true;
            // 
            // Column27
            // 
            this.Column27.FillWeight = 65F;
            this.Column27.HeaderText = "Estado";
            this.Column27.Name = "Column27";
            this.Column27.ReadOnly = true;
            // 
            // pnlFiltros
            // 
            this.pnlFiltros.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(225)))), ((int)(((byte)(230)))), ((int)(((byte)(240)))));
            this.pnlFiltros.Controls.Add(this.btnLimpiarFiltro);
            this.pnlFiltros.Controls.Add(this.btnBuscar);
            this.pnlFiltros.Controls.Add(this.txtBusqueda);
            this.pnlFiltros.Controls.Add(this.cboFiltroColumna);
            this.pnlFiltros.Controls.Add(this.lblFiltroColumna);
            this.pnlFiltros.Controls.Add(this.cboFiltroEstado);
            this.pnlFiltros.Controls.Add(this.lblFiltroEstado);
            this.pnlFiltros.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlFiltros.Location = new System.Drawing.Point(0, 50);
            this.pnlFiltros.Name = "pnlFiltros";
            this.pnlFiltros.Padding = new System.Windows.Forms.Padding(10);
            this.pnlFiltros.Size = new System.Drawing.Size(1054, 50);
            this.pnlFiltros.TabIndex = 1;
            // 
            // btnLimpiarFiltro
            // 
            this.btnLimpiarFiltro.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(158)))), ((int)(((byte)(158)))), ((int)(((byte)(158)))));
            this.btnLimpiarFiltro.FlatAppearance.BorderSize = 0;
            this.btnLimpiarFiltro.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnLimpiarFiltro.Font = new System.Drawing.Font("Segoe UI Semibold", 9F, System.Drawing.FontStyle.Bold);
            this.btnLimpiarFiltro.ForeColor = System.Drawing.Color.White;
            this.btnLimpiarFiltro.Location = new System.Drawing.Point(785, 10);
            this.btnLimpiarFiltro.Name = "btnLimpiarFiltro";
            this.btnLimpiarFiltro.Size = new System.Drawing.Size(80, 28);
            this.btnLimpiarFiltro.TabIndex = 6;
            this.btnLimpiarFiltro.Text = "Limpiar";
            this.btnLimpiarFiltro.UseVisualStyleBackColor = false;
            this.btnLimpiarFiltro.Click += new System.EventHandler(this.btnLimpiarFiltro_Click);
            // 
            // btnBuscar
            // 
            this.btnBuscar.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(122)))), ((int)(((byte)(204)))));
            this.btnBuscar.FlatAppearance.BorderSize = 0;
            this.btnBuscar.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnBuscar.Font = new System.Drawing.Font("Segoe UI Semibold", 9F, System.Drawing.FontStyle.Bold);
            this.btnBuscar.ForeColor = System.Drawing.Color.White;
            this.btnBuscar.Location = new System.Drawing.Point(695, 10);
            this.btnBuscar.Name = "btnBuscar";
            this.btnBuscar.Size = new System.Drawing.Size(80, 28);
            this.btnBuscar.TabIndex = 5;
            this.btnBuscar.Text = "Buscar";
            this.btnBuscar.UseVisualStyleBackColor = false;
            this.btnBuscar.Click += new System.EventHandler(this.btnBuscar_Click);
            // 
            // txtBusqueda
            // 
            this.txtBusqueda.BackColor = System.Drawing.Color.White;
            this.txtBusqueda.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtBusqueda.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.txtBusqueda.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.txtBusqueda.Location = new System.Drawing.Point(385, 12);
            this.txtBusqueda.Name = "txtBusqueda";
            this.txtBusqueda.Size = new System.Drawing.Size(300, 24);
            this.txtBusqueda.TabIndex = 4;
            this.txtBusqueda.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtBusqueda_KeyDown);
            // 
            // cboFiltroColumna
            // 
            this.cboFiltroColumna.BackColor = System.Drawing.Color.White;
            this.cboFiltroColumna.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboFiltroColumna.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cboFiltroColumna.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.cboFiltroColumna.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.cboFiltroColumna.FormattingEnabled = true;
            this.cboFiltroColumna.Location = new System.Drawing.Point(255, 12);
            this.cboFiltroColumna.Name = "cboFiltroColumna";
            this.cboFiltroColumna.Size = new System.Drawing.Size(120, 23);
            this.cboFiltroColumna.TabIndex = 3;
            // 
            // lblFiltroColumna
            // 
            this.lblFiltroColumna.AutoSize = true;
            this.lblFiltroColumna.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblFiltroColumna.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(50)))), ((int)(((byte)(50)))));
            this.lblFiltroColumna.Location = new System.Drawing.Point(185, 16);
            this.lblFiltroColumna.Name = "lblFiltroColumna";
            this.lblFiltroColumna.Size = new System.Drawing.Size(64, 15);
            this.lblFiltroColumna.TabIndex = 2;
            this.lblFiltroColumna.Text = "Buscar en:";
            // 
            // cboFiltroEstado
            // 
            this.cboFiltroEstado.BackColor = System.Drawing.Color.White;
            this.cboFiltroEstado.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboFiltroEstado.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cboFiltroEstado.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.cboFiltroEstado.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.cboFiltroEstado.FormattingEnabled = true;
            this.cboFiltroEstado.Location = new System.Drawing.Point(65, 12);
            this.cboFiltroEstado.Name = "cboFiltroEstado";
            this.cboFiltroEstado.Size = new System.Drawing.Size(100, 23);
            this.cboFiltroEstado.TabIndex = 1;
            this.cboFiltroEstado.SelectedIndexChanged += new System.EventHandler(this.cboFiltroEstado_SelectedIndexChanged);
            // 
            // lblFiltroEstado
            // 
            this.lblFiltroEstado.AutoSize = true;
            this.lblFiltroEstado.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblFiltroEstado.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(50)))), ((int)(((byte)(50)))));
            this.lblFiltroEstado.Location = new System.Drawing.Point(10, 16);
            this.lblFiltroEstado.Name = "lblFiltroEstado";
            this.lblFiltroEstado.Size = new System.Drawing.Size(46, 15);
            this.lblFiltroEstado.TabIndex = 0;
            this.lblFiltroEstado.Text = "Estado:";
            // 
            // pnlToolbar
            // 
            this.pnlToolbar.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(245)))));
            this.pnlToolbar.Controls.Add(this.dashboardControl1);
            this.pnlToolbar.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlToolbar.Location = new System.Drawing.Point(0, 0);
            this.pnlToolbar.Name = "pnlToolbar";
            this.pnlToolbar.Padding = new System.Windows.Forms.Padding(8);
            this.pnlToolbar.Size = new System.Drawing.Size(1054, 50);
            this.pnlToolbar.TabIndex = 0;
            // 
            // dashboardControl1
            // 
            this.dashboardControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.dashboardControl1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(245)))));
            this.dashboardControl1.Location = new System.Drawing.Point(762, 0);
            this.dashboardControl1.Name = "dashboardControl1";
            this.dashboardControl1.Size = new System.Drawing.Size(280, 51);
            this.dashboardControl1.TabIndex = 4;
            // 
            // pnlBottomRight
            // 
            this.pnlBottomRight.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(240)))), ((int)(((byte)(245)))));
            this.pnlBottomRight.Controls.Add(this.dgv_pautas);
            this.pnlBottomRight.Controls.Add(this.grpEliminarPautas);
            this.pnlBottomRight.Controls.Add(this.lblPautasTitulo);
            this.pnlBottomRight.Controls.Add(this.dgv_ciudades);
            this.pnlBottomRight.Controls.Add(this.dgv_estaciones);
            this.pnlBottomRight.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlBottomRight.Location = new System.Drawing.Point(0, 524);
            this.pnlBottomRight.Name = "pnlBottomRight";
            this.pnlBottomRight.Padding = new System.Windows.Forms.Padding(10);
            this.pnlBottomRight.Size = new System.Drawing.Size(1054, 160);
            this.pnlBottomRight.TabIndex = 1;
            // 
            // dgv_pautas
            // 
            this.dgv_pautas.AllowUserToAddRows = false;
            this.dgv_pautas.AllowUserToDeleteRows = false;
            this.dgv_pautas.AllowUserToResizeRows = false;
            this.dgv_pautas.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.dgv_pautas.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgv_pautas.BackgroundColor = System.Drawing.Color.White;
            this.dgv_pautas.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.dgv_pautas.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.SingleHorizontal;
            this.dgv_pautas.ColumnHeadersHeight = 25;
            this.dgv_pautas.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.dgv_pautas.EnableHeadersVisualStyles = false;
            this.dgv_pautas.GridColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(230)))), ((int)(((byte)(230)))));
            this.dgv_pautas.Location = new System.Drawing.Point(435, 30);
            this.dgv_pautas.Name = "dgv_pautas";
            this.dgv_pautas.ReadOnly = true;
            this.dgv_pautas.RowHeadersVisible = false;
            this.dgv_pautas.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgv_pautas.Size = new System.Drawing.Size(260, 160);
            this.dgv_pautas.TabIndex = 7;
            // 
            // grpEliminarPautas
            // 
            this.grpEliminarPautas.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.grpEliminarPautas.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(250)))), ((int)(((byte)(250)))));
            this.grpEliminarPautas.Controls.Add(this.lblCodigoEliminar);
            this.grpEliminarPautas.Controls.Add(this.txtCodigoEliminar);
            this.grpEliminarPautas.Controls.Add(this.lblFechaElimI);
            this.grpEliminarPautas.Controls.Add(this.dtpFechaElimI);
            this.grpEliminarPautas.Controls.Add(this.lblFechaElimF);
            this.grpEliminarPautas.Controls.Add(this.dtpFechaElimF);
            this.grpEliminarPautas.Controls.Add(this.lblHoraElim);
            this.grpEliminarPautas.Controls.Add(this.lblConteoHoras);
            this.grpEliminarPautas.Controls.Add(this.cboHoraElim);
            this.grpEliminarPautas.Controls.Add(this.btnEliminarPorFechas);
            this.grpEliminarPautas.Controls.Add(this.btnEliminarPorHora);
            this.grpEliminarPautas.Controls.Add(this.lblTotalPautas);
            this.grpEliminarPautas.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Bold);
            this.grpEliminarPautas.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.grpEliminarPautas.Location = new System.Drawing.Point(710, 12);
            this.grpEliminarPautas.Name = "grpEliminarPautas";
            this.grpEliminarPautas.Size = new System.Drawing.Size(330, 180);
            this.grpEliminarPautas.TabIndex = 8;
            this.grpEliminarPautas.TabStop = false;
            this.grpEliminarPautas.Text = "Eliminar Pautas";
            // 
            // lblCodigoEliminar
            // 
            this.lblCodigoEliminar.AutoSize = true;
            this.lblCodigoEliminar.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Bold);
            this.lblCodigoEliminar.Location = new System.Drawing.Point(10, 38);
            this.lblCodigoEliminar.Name = "lblCodigoEliminar";
            this.lblCodigoEliminar.Size = new System.Drawing.Size(45, 13);
            this.lblCodigoEliminar.TabIndex = 0;
            this.lblCodigoEliminar.Text = "Codigo";
            // 
            // txtCodigoEliminar
            // 
            this.txtCodigoEliminar.BackColor = System.Drawing.Color.White;
            this.txtCodigoEliminar.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtCodigoEliminar.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.txtCodigoEliminar.Location = new System.Drawing.Point(60, 35);
            this.txtCodigoEliminar.Name = "txtCodigoEliminar";
            this.txtCodigoEliminar.Size = new System.Drawing.Size(70, 23);
            this.txtCodigoEliminar.TabIndex = 1;
            // 
            // lblFechaElimI
            // 
            this.lblFechaElimI.AutoSize = true;
            this.lblFechaElimI.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Bold);
            this.lblFechaElimI.Location = new System.Drawing.Point(10, 68);
            this.lblFechaElimI.Name = "lblFechaElimI";
            this.lblFechaElimI.Size = new System.Drawing.Size(46, 13);
            this.lblFechaElimI.TabIndex = 2;
            this.lblFechaElimI.Text = "Fecha I.";
            // 
            // dtpFechaElimI
            // 
            this.dtpFechaElimI.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.dtpFechaElimI.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpFechaElimI.Location = new System.Drawing.Point(60, 65);
            this.dtpFechaElimI.Name = "dtpFechaElimI";
            this.dtpFechaElimI.Size = new System.Drawing.Size(100, 22);
            this.dtpFechaElimI.TabIndex = 3;
            // 
            // lblFechaElimF
            // 
            this.lblFechaElimF.AutoSize = true;
            this.lblFechaElimF.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Bold);
            this.lblFechaElimF.Location = new System.Drawing.Point(10, 96);
            this.lblFechaElimF.Name = "lblFechaElimF";
            this.lblFechaElimF.Size = new System.Drawing.Size(48, 13);
            this.lblFechaElimF.TabIndex = 4;
            this.lblFechaElimF.Text = "Fecha F.";
            // 
            // dtpFechaElimF
            // 
            this.dtpFechaElimF.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.dtpFechaElimF.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpFechaElimF.Location = new System.Drawing.Point(60, 93);
            this.dtpFechaElimF.Name = "dtpFechaElimF";
            this.dtpFechaElimF.Size = new System.Drawing.Size(100, 22);
            this.dtpFechaElimF.TabIndex = 5;
            // 
            // lblHoraElim
            // 
            this.lblHoraElim.AutoSize = true;
            this.lblHoraElim.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Bold);
            this.lblHoraElim.Location = new System.Drawing.Point(175, 38);
            this.lblHoraElim.Name = "lblHoraElim";
            this.lblHoraElim.Size = new System.Drawing.Size(32, 13);
            this.lblHoraElim.TabIndex = 6;
            this.lblHoraElim.Text = "Hora";
            // 
            // lblConteoHoras
            // 
            this.lblConteoHoras.AutoSize = true;
            this.lblConteoHoras.Font = new System.Drawing.Font("Segoe UI", 7F, System.Drawing.FontStyle.Bold);
            this.lblConteoHoras.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(76)))), ((int)(((byte)(175)))), ((int)(((byte)(80)))));
            this.lblConteoHoras.Location = new System.Drawing.Point(210, 38);
            this.lblConteoHoras.Name = "lblConteoHoras";
            this.lblConteoHoras.Size = new System.Drawing.Size(0, 12);
            this.lblConteoHoras.TabIndex = 12;
            // 
            // cboHoraElim
            // 
            this.cboHoraElim.BackColor = System.Drawing.Color.White;
            this.cboHoraElim.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboHoraElim.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.cboHoraElim.Location = new System.Drawing.Point(175, 55);
            this.cboHoraElim.Name = "cboHoraElim";
            this.cboHoraElim.Size = new System.Drawing.Size(100, 21);
            this.cboHoraElim.TabIndex = 7;
            // 
            // btnEliminarPorFechas
            // 
            this.btnEliminarPorFechas.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(244)))), ((int)(((byte)(67)))), ((int)(((byte)(54)))));
            this.btnEliminarPorFechas.FlatAppearance.BorderSize = 0;
            this.btnEliminarPorFechas.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnEliminarPorFechas.Font = new System.Drawing.Font("Segoe UI", 7.5F, System.Drawing.FontStyle.Bold);
            this.btnEliminarPorFechas.ForeColor = System.Drawing.Color.White;
            this.btnEliminarPorFechas.Location = new System.Drawing.Point(10, 125);
            this.btnEliminarPorFechas.Name = "btnEliminarPorFechas";
            this.btnEliminarPorFechas.Size = new System.Drawing.Size(130, 28);
            this.btnEliminarPorFechas.TabIndex = 8;
            this.btnEliminarPorFechas.Text = "Eliminar por fecha";
            this.btnEliminarPorFechas.UseVisualStyleBackColor = false;
            this.btnEliminarPorFechas.Click += new System.EventHandler(this.btnEliminarPorFechas_Click);
            // 
            // btnEliminarPorHora
            // 
            this.btnEliminarPorHora.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(244)))), ((int)(((byte)(67)))), ((int)(((byte)(54)))));
            this.btnEliminarPorHora.FlatAppearance.BorderSize = 0;
            this.btnEliminarPorHora.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnEliminarPorHora.Font = new System.Drawing.Font("Segoe UI", 7.5F, System.Drawing.FontStyle.Bold);
            this.btnEliminarPorHora.ForeColor = System.Drawing.Color.White;
            this.btnEliminarPorHora.Location = new System.Drawing.Point(150, 125);
            this.btnEliminarPorHora.Name = "btnEliminarPorHora";
            this.btnEliminarPorHora.Size = new System.Drawing.Size(130, 28);
            this.btnEliminarPorHora.TabIndex = 9;
            this.btnEliminarPorHora.Text = "Eliminar por hora";
            this.btnEliminarPorHora.UseVisualStyleBackColor = false;
            this.btnEliminarPorHora.Click += new System.EventHandler(this.btnEliminarPorHora_Click);
            // 
            // lblTotalPautas
            // 
            this.lblTotalPautas.AutoSize = true;
            this.lblTotalPautas.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Bold);
            this.lblTotalPautas.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(122)))), ((int)(((byte)(204)))));
            this.lblTotalPautas.Location = new System.Drawing.Point(10, 18);
            this.lblTotalPautas.Name = "lblTotalPautas";
            this.lblTotalPautas.Size = new System.Drawing.Size(0, 13);
            this.lblTotalPautas.TabIndex = 11;
            // 
            // lblPautasTitulo
            // 
            this.lblPautasTitulo.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Bold);
            this.lblPautasTitulo.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(33)))), ((int)(((byte)(150)))), ((int)(((byte)(243)))));
            this.lblPautasTitulo.Location = new System.Drawing.Point(435, 8);
            this.lblPautasTitulo.Name = "lblPautasTitulo";
            this.lblPautasTitulo.Size = new System.Drawing.Size(260, 18);
            this.lblPautasTitulo.TabIndex = 6;
            this.lblPautasTitulo.Text = "Pautas";
            this.lblPautasTitulo.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblPautasTitulo.Visible = false;
            // 
            // dgv_ciudades
            // 
            this.dgv_ciudades.AllowUserToAddRows = false;
            this.dgv_ciudades.AllowUserToDeleteRows = false;
            this.dgv_ciudades.AllowUserToResizeColumns = false;
            this.dgv_ciudades.AllowUserToResizeRows = false;
            this.dgv_ciudades.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgv_ciudades.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.dgv_ciudades.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.dgv_ciudades.ColumnHeadersHeight = 30;
            this.dgv_ciudades.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.dgv_ciudades.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.dataGridViewTextBoxColumn1});
            this.dgv_ciudades.EnableHeadersVisualStyles = false;
            this.dgv_ciudades.GridColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(60)))));
            this.dgv_ciudades.Location = new System.Drawing.Point(220, 50);
            this.dgv_ciudades.Name = "dgv_ciudades";
            this.dgv_ciudades.ReadOnly = true;
            this.dgv_ciudades.RowHeadersVisible = false;
            this.dgv_ciudades.RowTemplate.Height = 28;
            this.dgv_ciudades.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgv_ciudades.Size = new System.Drawing.Size(200, 140);
            this.dgv_ciudades.TabIndex = 3;
            // 
            // dataGridViewTextBoxColumn1
            // 
            this.dataGridViewTextBoxColumn1.HeaderText = "CIUDADES";
            this.dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
            this.dataGridViewTextBoxColumn1.ReadOnly = true;
            // 
            // dgv_estaciones
            // 
            this.dgv_estaciones.AllowUserToAddRows = false;
            this.dgv_estaciones.AllowUserToDeleteRows = false;
            this.dgv_estaciones.AllowUserToResizeColumns = false;
            this.dgv_estaciones.AllowUserToResizeRows = false;
            this.dgv_estaciones.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgv_estaciones.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.dgv_estaciones.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.dgv_estaciones.ColumnHeadersHeight = 30;
            this.dgv_estaciones.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.dgv_estaciones.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Column6});
            this.dgv_estaciones.EnableHeadersVisualStyles = false;
            this.dgv_estaciones.GridColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(60)))));
            this.dgv_estaciones.Location = new System.Drawing.Point(10, 50);
            this.dgv_estaciones.Name = "dgv_estaciones";
            this.dgv_estaciones.ReadOnly = true;
            this.dgv_estaciones.RowHeadersVisible = false;
            this.dgv_estaciones.RowTemplate.Height = 28;
            this.dgv_estaciones.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgv_estaciones.Size = new System.Drawing.Size(200, 140);
            this.dgv_estaciones.TabIndex = 2;
            // 
            // Column6
            // 
            this.Column6.HeaderText = "ESTACIONES";
            this.Column6.Name = "Column6";
            this.Column6.ReadOnly = true;
            // 
            // statusStrip1
            // 
            this.statusStrip1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(240)))), ((int)(((byte)(245)))));
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.lblCreditos,
            this.lblVersion});
            this.statusStrip1.Location = new System.Drawing.Point(0, 728);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(1400, 22);
            this.statusStrip1.TabIndex = 1;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // lblCreditos
            // 
            this.lblCreditos.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(100)))), ((int)(((byte)(100)))));
            this.lblCreditos.Name = "lblCreditos";
            this.lblCreditos.Size = new System.Drawing.Size(1141, 17);
            this.lblCreditos.Spring = true;
            // 
            // lblVersion
            // 
            this.lblVersion.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(100)))), ((int)(((byte)(100)))));
            this.lblVersion.Name = "lblVersion";
            this.lblVersion.Size = new System.Drawing.Size(244, 17);
            this.lblVersion.Text = "Versión 2.0.0  |  dev@corporacionuniversal.pe";
            this.lblVersion.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(240)))), ((int)(((byte)(245)))));
            this.ClientSize = new System.Drawing.Size(1400, 750);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.pnlMain);
            this.DoubleBuffered = true;
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MinimumSize = new System.Drawing.Size(1024, 600);
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Generador de Pautas - Sistema de Comerciales";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.Load += new System.EventHandler(this.Form1_Load);
            this.pnlMain.ResumeLayout(false);
            this.pnlLeft.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgv_archivos)).EndInit();
            this.pnlPlayer.ResumeLayout(false);
            this.pnlRight.ResumeLayout(false);
            this.pnlTopRight.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgv_base)).EndInit();
            this.pnlFiltros.ResumeLayout(false);
            this.pnlFiltros.PerformLayout();
            this.pnlToolbar.ResumeLayout(false);
            this.pnlBottomRight.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgv_pautas)).EndInit();
            this.grpEliminarPautas.ResumeLayout(false);
            this.grpEliminarPautas.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgv_ciudades)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgv_estaciones)).EndInit();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        // Paneles principales
        private System.Windows.Forms.TableLayoutPanel pnlMain;
        private System.Windows.Forms.Panel pnlLeft;
        private System.Windows.Forms.Panel pnlRight;
        private System.Windows.Forms.Panel pnlTopRight;
        private System.Windows.Forms.Panel pnlBottomRight;
        private System.Windows.Forms.Panel pnlToolbar;
        private System.Windows.Forms.Panel pnlFiltros;
        private System.Windows.Forms.Panel pnlPlayer;

        // FileExplorer
        public System.Windows.Forms.DataGridView dgv_base;
        private System.Windows.Forms.DataGridView dgv_estaciones;
        private System.Windows.Forms.DataGridView dgv_ciudades;

        // Columnas dgv_base
        private System.Windows.Forms.DataGridViewTextBoxColumn Column20;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column21;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column22;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column23;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column24;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column25;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column26;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column27;

        // Columnas auxiliares
        private System.Windows.Forms.DataGridViewTextBoxColumn Column6;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;


        // Botones principales

        // Botones reproductor
        public System.Windows.Forms.Button btn_play;
        public System.Windows.Forms.Button btn_stop;
        public System.Windows.Forms.Button btn_pause;
        public System.Windows.Forms.Button btn_limpiar;

        // Filtros
        private System.Windows.Forms.Label lblFiltroEstado;
        private System.Windows.Forms.ComboBox cboFiltroEstado;
        private System.Windows.Forms.Label lblFiltroColumna;
        private System.Windows.Forms.ComboBox cboFiltroColumna;
        private System.Windows.Forms.TextBox txtBusqueda;
        private System.Windows.Forms.Button btnBuscar;
        private System.Windows.Forms.Button btnLimpiarFiltro;


        // Progress Bars
        private System.Windows.Forms.ProgressBar elegantProgressBar1;
        private System.Windows.Forms.ProgressBar progressBarLeft;
        private System.Windows.Forms.ProgressBar progressBarRight;

        // Pautas del comercial seleccionado
        private System.Windows.Forms.DataGridView dgv_pautas;
        private System.Windows.Forms.Label lblPautasTitulo;

        // Panel de eliminacion de pautas
        private System.Windows.Forms.GroupBox grpEliminarPautas;
        private System.Windows.Forms.Label lblCodigoEliminar;
        private System.Windows.Forms.TextBox txtCodigoEliminar;
        private System.Windows.Forms.Label lblFechaElimI;
        private System.Windows.Forms.DateTimePicker dtpFechaElimI;
        private System.Windows.Forms.Label lblFechaElimF;
        private System.Windows.Forms.DateTimePicker dtpFechaElimF;
        private System.Windows.Forms.Label lblHoraElim;
        private System.Windows.Forms.Label lblConteoHoras;
        private System.Windows.Forms.ComboBox cboHoraElim;
        private System.Windows.Forms.Button btnEliminarPorFechas;
        private System.Windows.Forms.Button btnEliminarPorHora;
        private System.Windows.Forms.Label lblTotalPautas;

        // StatusStrip (Version y Creditos)
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel lblVersion;
        private System.Windows.Forms.ToolStripStatusLabel lblCreditos;
        public System.Windows.Forms.DataGridView dgv_archivos;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column1;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column2;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column3;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column4;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column5;
        private System.Windows.Forms.Panel dashboardControl1;
    }
}

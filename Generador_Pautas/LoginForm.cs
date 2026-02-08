using System;
using System.Drawing;
using System.Windows.Forms;

namespace Generador_Pautas
{
    public partial class LoginForm : Form
    {
        public bool LoginExitoso { get; private set; } = false;
        private CheckBox chkRecordarSesion;

        public LoginForm()
        {
            InitializeComponent();
            AppIcon.ApplyTo(this); // Aplicar icono de la aplicacion
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // Configuracion del formulario
            this.Text = "Iniciar Sesion - Generador de Pautas";
            this.Size = new Size(400, 350);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(240, 240, 245);

            // Panel principal
            Panel panelPrincipal = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(30)
            };

            // Titulo
            Label lblTitulo = new Label
            {
                Text = "Generador de Pautas",
                Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 120, 215),
                AutoSize = true,
                Location = new Point(70, 20)
            };

            // Subtitulo
            Label lblSubtitulo = new Label
            {
                Text = "Inicie sesion para continuar",
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(100, 100, 100),
                AutoSize = true,
                Location = new Point(110, 55)
            };

            // Label Usuario
            Label lblUsuario = new Label
            {
                Text = "Usuario:",
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(60, 60, 60),
                AutoSize = true,
                Location = new Point(30, 100)
            };

            // TextBox Usuario
            TextBox txtUsuario = new TextBox
            {
                Name = "txtUsuario",
                Font = new Font("Segoe UI", 11F),
                Size = new Size(320, 30),
                Location = new Point(30, 125)
            };

            // Label Contrasena
            Label lblContrasena = new Label
            {
                Text = "Contrasena:",
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(60, 60, 60),
                AutoSize = true,
                Location = new Point(30, 165)
            };

            // TextBox Contrasena
            TextBox txtContrasena = new TextBox
            {
                Name = "txtContrasena",
                Font = new Font("Segoe UI", 11F),
                Size = new Size(320, 30),
                Location = new Point(30, 190),
                PasswordChar = '*'
            };

            // CheckBox Recordar Sesion
            chkRecordarSesion = new CheckBox
            {
                Name = "chkRecordarSesion",
                Text = "Recordar sesion (no volver a pedir login)",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(80, 80, 80),
                AutoSize = true,
                Location = new Point(30, 225),
                Checked = false
            };

            // Boton Iniciar Sesion
            Button btnLogin = new Button
            {
                Name = "btnLogin",
                Text = "Iniciar Sesion",
                Font = new Font("Segoe UI Semibold", 11F),
                Size = new Size(320, 40),
                Location = new Point(30, 260),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnLogin.FlatAppearance.BorderSize = 0;
            btnLogin.Click += BtnLogin_Click;

            // Agregar controles al panel
            panelPrincipal.Controls.Add(lblTitulo);
            panelPrincipal.Controls.Add(lblSubtitulo);
            panelPrincipal.Controls.Add(lblUsuario);
            panelPrincipal.Controls.Add(txtUsuario);
            panelPrincipal.Controls.Add(lblContrasena);
            panelPrincipal.Controls.Add(txtContrasena);
            panelPrincipal.Controls.Add(chkRecordarSesion);
            panelPrincipal.Controls.Add(btnLogin);

            this.Controls.Add(panelPrincipal);

            // Evento KeyDown para Enter
            txtUsuario.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) txtContrasena.Focus(); };
            txtContrasena.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) BtnLogin_Click(null, null); };

            this.ResumeLayout(false);
            this.AcceptButton = btnLogin;
        }

        private async void BtnLogin_Click(object sender, EventArgs e)
        {
            TextBox txtUsuario = (TextBox)this.Controls[0].Controls["txtUsuario"];
            TextBox txtContrasena = (TextBox)this.Controls[0].Controls["txtContrasena"];

            string usuario = txtUsuario.Text.Trim();
            string contrasena = txtContrasena.Text;

            if (string.IsNullOrEmpty(usuario) || string.IsNullOrEmpty(contrasena))
            {
                MessageBox.Show("Por favor, ingrese usuario y contrasena.", "Advertencia",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Deshabilitar controles durante el login
            txtUsuario.Enabled = false;
            txtContrasena.Enabled = false;
            chkRecordarSesion.Enabled = false;
            Button btnLogin = (Button)this.Controls[0].Controls["btnLogin"];
            btnLogin.Enabled = false;
            btnLogin.Text = "Verificando...";

            try
            {
                var (exito, mensaje) = await UserManager.LoginAsync(usuario, contrasena);

                if (exito)
                {
                    // Si marco "Recordar sesion", guardar credenciales
                    if (chkRecordarSesion.Checked)
                    {
                        UserManager.GuardarSesion(usuario, contrasena);
                    }

                    LoginExitoso = true;
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    MessageBox.Show(mensaje, "Error de autenticacion",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);

                    // Limpiar contrasena y enfocar
                    txtContrasena.Text = "";
                    txtContrasena.Focus();
                }
            }
            finally
            {
                // Rehabilitar controles
                txtUsuario.Enabled = true;
                txtContrasena.Enabled = true;
                chkRecordarSesion.Enabled = true;
                btnLogin.Enabled = true;
                btnLogin.Text = "Iniciar Sesion";
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!LoginExitoso && e.CloseReason == CloseReason.UserClosing)
            {
                this.DialogResult = DialogResult.Cancel;
            }
            base.OnFormClosing(e);
        }
    }
}

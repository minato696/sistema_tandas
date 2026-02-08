using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Generador_Pautas
{
    public partial class AdminUsuariosForm : Form
    {
        private DataGridView dgvUsuarios;
        private TextBox txtUsuario;
        private TextBox txtContrasena;
        private TextBox txtNombreCompleto;
        private ComboBox cboRol;
        private ComboBox cboEstado;
        private Button btnAgregar;
        private Button btnGuardar;
        private Button btnCambiarContrasena;

        public AdminUsuariosForm()
        {
            InitializeComponent();
            AppIcon.ApplyTo(this); // Aplicar icono de la aplicacion
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // Configuracion del formulario
            this.Text = "Administrar Usuarios";
            this.Size = new Size(700, 550);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(240, 240, 245);

            // Titulo
            Label lblTitulo = new Label
            {
                Text = "Administrar Usuarios",
                Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 120, 215),
                AutoSize = true,
                Location = new Point(20, 15)
            };

            // DataGridView
            dgvUsuarios = new DataGridView
            {
                Location = new Point(20, 50),
                Size = new Size(640, 200),
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            // Columnas del DataGridView
            dgvUsuarios.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", HeaderText = "ID", Width = 40 });
            dgvUsuarios.Columns.Add(new DataGridViewTextBoxColumn { Name = "Usuario", HeaderText = "Usuario", Width = 100 });
            dgvUsuarios.Columns.Add(new DataGridViewTextBoxColumn { Name = "NombreCompleto", HeaderText = "Nombre Completo", Width = 180 });
            dgvUsuarios.Columns.Add(new DataGridViewTextBoxColumn { Name = "Rol", HeaderText = "Rol", Width = 100 });
            dgvUsuarios.Columns.Add(new DataGridViewTextBoxColumn { Name = "Estado", HeaderText = "Estado", Width = 80 });
            dgvUsuarios.Columns.Add(new DataGridViewTextBoxColumn { Name = "FechaCreacion", HeaderText = "Creado", Width = 100 });

            dgvUsuarios.SelectionChanged += DgvUsuarios_SelectionChanged;

            // Panel de edicion
            Label lblNuevo = new Label
            {
                Text = "Datos del Usuario:",
                Font = new Font("Segoe UI Semibold", 10F),
                AutoSize = true,
                Location = new Point(20, 265)
            };

            // Usuario
            Label lblUsuario = new Label { Text = "Usuario:", Location = new Point(20, 295), AutoSize = true };
            txtUsuario = new TextBox { Location = new Point(20, 315), Size = new Size(150, 25), Font = new Font("Segoe UI", 10F) };

            // Contrasena
            Label lblContrasena = new Label { Text = "Contrasena:", Location = new Point(180, 295), AutoSize = true };
            txtContrasena = new TextBox { Location = new Point(180, 315), Size = new Size(150, 25), Font = new Font("Segoe UI", 10F), PasswordChar = '*' };

            // Nombre Completo
            Label lblNombre = new Label { Text = "Nombre Completo:", Location = new Point(340, 295), AutoSize = true };
            txtNombreCompleto = new TextBox { Location = new Point(340, 315), Size = new Size(200, 25), Font = new Font("Segoe UI", 10F) };

            // Rol
            Label lblRol = new Label { Text = "Rol:", Location = new Point(20, 355), AutoSize = true };
            cboRol = new ComboBox
            {
                Location = new Point(20, 375),
                Size = new Size(150, 25),
                Font = new Font("Segoe UI", 10F),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cboRol.Items.AddRange(new[] { "Administrador", "Usuario" });
            cboRol.SelectedIndex = 1;

            // Estado
            Label lblEstado = new Label { Text = "Estado:", Location = new Point(180, 355), AutoSize = true };
            cboEstado = new ComboBox
            {
                Location = new Point(180, 375),
                Size = new Size(150, 25),
                Font = new Font("Segoe UI", 10F),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cboEstado.Items.AddRange(new[] { "Activo", "Inactivo" });
            cboEstado.SelectedIndex = 0;

            // Botones
            btnAgregar = new Button
            {
                Text = "Agregar Nuevo",
                Font = new Font("Segoe UI", 9F),
                Size = new Size(120, 32),
                Location = new Point(20, 420),
                BackColor = Color.FromArgb(0, 150, 136),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnAgregar.FlatAppearance.BorderSize = 0;
            btnAgregar.Click += BtnAgregar_Click;

            btnGuardar = new Button
            {
                Text = "Guardar Cambios",
                Font = new Font("Segoe UI", 9F),
                Size = new Size(130, 32),
                Location = new Point(150, 420),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnGuardar.FlatAppearance.BorderSize = 0;
            btnGuardar.Click += BtnGuardar_Click;

            btnCambiarContrasena = new Button
            {
                Text = "Cambiar Contrasena",
                Font = new Font("Segoe UI", 9F),
                Size = new Size(150, 32),
                Location = new Point(290, 420),
                BackColor = Color.FromArgb(255, 152, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnCambiarContrasena.FlatAppearance.BorderSize = 0;
            btnCambiarContrasena.Click += BtnCambiarContrasena_Click;

            // Nota informativa
            Label lblNota = new Label
            {
                Text = "Nota: Las contrasenas se almacenan de forma segura (hash SHA256).",
                Font = new Font("Segoe UI", 8F, FontStyle.Italic),
                ForeColor = Color.Gray,
                AutoSize = true,
                Location = new Point(20, 470)
            };

            // Agregar controles
            this.Controls.Add(lblTitulo);
            this.Controls.Add(dgvUsuarios);
            this.Controls.Add(lblNuevo);
            this.Controls.Add(lblUsuario);
            this.Controls.Add(txtUsuario);
            this.Controls.Add(lblContrasena);
            this.Controls.Add(txtContrasena);
            this.Controls.Add(lblNombre);
            this.Controls.Add(txtNombreCompleto);
            this.Controls.Add(lblRol);
            this.Controls.Add(cboRol);
            this.Controls.Add(lblEstado);
            this.Controls.Add(cboEstado);
            this.Controls.Add(btnAgregar);
            this.Controls.Add(btnGuardar);
            this.Controls.Add(btnCambiarContrasena);
            this.Controls.Add(lblNota);

            this.Load += AdminUsuariosForm_Load;

            this.ResumeLayout(false);
        }

        private async void AdminUsuariosForm_Load(object sender, EventArgs e)
        {
            await CargarUsuariosAsync();
        }

        private void DgvUsuarios_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvUsuarios.SelectedRows.Count > 0)
            {
                var row = dgvUsuarios.SelectedRows[0];
                txtUsuario.Text = row.Cells["Usuario"].Value?.ToString() ?? "";
                txtNombreCompleto.Text = row.Cells["NombreCompleto"].Value?.ToString() ?? "";
                cboRol.SelectedItem = row.Cells["Rol"].Value?.ToString() ?? "Usuario";
                cboEstado.SelectedItem = row.Cells["Estado"].Value?.ToString() ?? "Activo";
                txtContrasena.Text = ""; // No mostrar contrasena

                // Deshabilitar edicion del nombre de usuario existente
                txtUsuario.Enabled = false;
            }
            else
            {
                txtUsuario.Enabled = true;
            }
        }

        private async Task CargarUsuariosAsync()
        {
            dgvUsuarios.Rows.Clear();

            try
            {
                var usuarios = await UserManager.ObtenerUsuariosAsync();

                foreach (var usuario in usuarios)
                {
                    int rowIndex = dgvUsuarios.Rows.Add();
                    dgvUsuarios.Rows[rowIndex].Cells["Id"].Value = usuario.Id.ToString();
                    dgvUsuarios.Rows[rowIndex].Cells["Usuario"].Value = usuario.NombreUsuario;
                    dgvUsuarios.Rows[rowIndex].Cells["NombreCompleto"].Value = usuario.NombreCompleto;
                    dgvUsuarios.Rows[rowIndex].Cells["Rol"].Value = usuario.Rol;
                    dgvUsuarios.Rows[rowIndex].Cells["Estado"].Value = usuario.Estado;
                    dgvUsuarios.Rows[rowIndex].Cells["FechaCreacion"].Value = usuario.FechaCreacion.ToString("dd/MM/yyyy");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar usuarios: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void BtnAgregar_Click(object sender, EventArgs e)
        {
            string usuario = txtUsuario.Text.Trim();
            string contrasena = txtContrasena.Text;
            string nombre = txtNombreCompleto.Text.Trim();
            string rol = cboRol.SelectedItem?.ToString() ?? "Usuario";

            if (string.IsNullOrEmpty(usuario))
            {
                MessageBox.Show("Ingrese el nombre de usuario.", "Advertencia",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrEmpty(contrasena))
            {
                MessageBox.Show("Ingrese la contrasena.", "Advertencia",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (contrasena.Length < 6)
            {
                MessageBox.Show("La contrasena debe tener al menos 6 caracteres.", "Advertencia",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var (exito, mensaje) = await UserManager.CrearUsuarioAsync(usuario, contrasena, rol, nombre);

            if (exito)
            {
                MessageBox.Show(mensaje, "Exito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LimpiarCampos();
                await CargarUsuariosAsync();
            }
            else
            {
                MessageBox.Show(mensaje, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void BtnGuardar_Click(object sender, EventArgs e)
        {
            if (dgvUsuarios.SelectedRows.Count == 0)
            {
                MessageBox.Show("Seleccione un usuario para actualizar.", "Advertencia",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int id = int.Parse(dgvUsuarios.SelectedRows[0].Cells["Id"].Value.ToString());
            string nombre = txtNombreCompleto.Text.Trim();
            string rol = cboRol.SelectedItem?.ToString() ?? "Usuario";
            string estado = cboEstado.SelectedItem?.ToString() ?? "Activo";

            var (exito, mensaje) = await UserManager.ActualizarUsuarioAsync(id, rol, nombre, estado);

            if (exito)
            {
                MessageBox.Show(mensaje, "Exito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                await CargarUsuariosAsync();
            }
            else
            {
                MessageBox.Show(mensaje, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void BtnCambiarContrasena_Click(object sender, EventArgs e)
        {
            if (dgvUsuarios.SelectedRows.Count == 0)
            {
                MessageBox.Show("Seleccione un usuario para cambiar su contrasena.", "Advertencia",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string nuevaContrasena = txtContrasena.Text;

            if (string.IsNullOrEmpty(nuevaContrasena))
            {
                MessageBox.Show("Ingrese la nueva contrasena.", "Advertencia",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (nuevaContrasena.Length < 6)
            {
                MessageBox.Show("La contrasena debe tener al menos 6 caracteres.", "Advertencia",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int id = int.Parse(dgvUsuarios.SelectedRows[0].Cells["Id"].Value.ToString());

            var (exito, mensaje) = await UserManager.CambiarContrasenaAsync(id, nuevaContrasena);

            if (exito)
            {
                MessageBox.Show(mensaje, "Exito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                txtContrasena.Text = "";
            }
            else
            {
                MessageBox.Show(mensaje, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LimpiarCampos()
        {
            txtUsuario.Text = "";
            txtContrasena.Text = "";
            txtNombreCompleto.Text = "";
            cboRol.SelectedIndex = 1;
            cboEstado.SelectedIndex = 0;
            txtUsuario.Enabled = true;
        }
    }
}

using System;
using System.Drawing;
using System.Windows.Forms;

namespace Generador_Pautas
{
    /// <summary>
    /// Formulario que muestra el progreso de la generación masiva de pautas.
    /// </summary>
    public class FormProgresoGeneracion : Form
    {
        private ProgressBar progressBar;
        private Label lblTitulo;
        private Label lblInfo;
        private Label lblPorcentaje;
        private Label lblMensaje;

        public FormProgresoGeneracion()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // Configuración del formulario
            this.Text = "Generando Pautas";
            this.Size = new Size(450, 200);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterScreen; // Centrar en pantalla
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ControlBox = false;
            this.BackColor = Color.White;
            this.ShowInTaskbar = false;
            this.TopMost = true; // Mantener siempre visible

            // Título
            lblTitulo = new Label
            {
                Text = "Generando Pautas Masivas",
                Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(76, 175, 80),
                Location = new Point(20, 20),
                Size = new Size(400, 30),
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Controls.Add(lblTitulo);

            // Información (ciudad/radio, rango de fechas)
            lblInfo = new Label
            {
                Text = "Preparando...",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(100, 100, 100),
                Location = new Point(20, 55),
                Size = new Size(400, 20),
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Controls.Add(lblInfo);

            // Barra de progreso
            progressBar = new ProgressBar
            {
                Location = new Point(20, 85),
                Size = new Size(400, 25),
                Style = ProgressBarStyle.Continuous,
                Minimum = 0,
                Maximum = 100,
                Value = 0
            };
            this.Controls.Add(progressBar);

            // Porcentaje
            lblPorcentaje = new Label
            {
                Text = "0%",
                Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(76, 175, 80),
                Location = new Point(20, 115),
                Size = new Size(60, 25),
                TextAlign = ContentAlignment.MiddleLeft
            };
            this.Controls.Add(lblPorcentaje);

            // Mensaje de estado
            lblMensaje = new Label
            {
                Text = "Iniciando...",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(80, 80, 80),
                Location = new Point(80, 115),
                Size = new Size(340, 25),
                TextAlign = ContentAlignment.MiddleLeft
            };
            this.Controls.Add(lblMensaje);

            this.ResumeLayout(false);
        }

        /// <summary>
        /// Actualiza la información mostrada (ciudad, radio, rango de fechas)
        /// </summary>
        public void ActualizarInfo(string info)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => ActualizarInfo(info)));
                return;
            }
            lblInfo.Text = info;
            Application.DoEvents();
        }

        /// <summary>
        /// Actualiza el progreso de la barra y el mensaje
        /// </summary>
        public void ActualizarProgreso(int porcentaje, string mensaje)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => ActualizarProgreso(porcentaje, mensaje)));
                return;
            }

            // Asegurar que el porcentaje esté en rango válido
            porcentaje = Math.Max(0, Math.Min(100, porcentaje));

            progressBar.Value = porcentaje;
            lblPorcentaje.Text = $"{porcentaje}%";
            lblMensaje.Text = mensaje;

            Application.DoEvents();
        }

        /// <summary>
        /// Muestra un mensaje de completado
        /// </summary>
        public void MostrarCompletado(int archivosGenerados)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => MostrarCompletado(archivosGenerados)));
                return;
            }

            progressBar.Value = 100;
            lblPorcentaje.Text = "100%";
            lblMensaje.Text = $"Completado - {archivosGenerados} archivos generados";
            lblTitulo.Text = "Generación Completada";

            Application.DoEvents();
        }

        /// <summary>
        /// Cambia el título del formulario
        /// </summary>
        public void CambiarTitulo(string titulo)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => CambiarTitulo(titulo)));
                return;
            }
            this.Text = titulo;
            lblTitulo.Text = titulo;
            Application.DoEvents();
        }

        /// <summary>
        /// Cambia el color del título y porcentaje
        /// </summary>
        public void CambiarColorTema(Color color)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => CambiarColorTema(color)));
                return;
            }
            lblTitulo.ForeColor = color;
            lblPorcentaje.ForeColor = color;
            Application.DoEvents();
        }

        /// <summary>
        /// Muestra mensaje de completado genérico
        /// </summary>
        public void MostrarCompletadoGenerico(string mensaje)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => MostrarCompletadoGenerico(mensaje)));
                return;
            }

            progressBar.Value = 100;
            lblPorcentaje.Text = "100%";
            lblMensaje.Text = mensaje;

            Application.DoEvents();
        }
    }
}

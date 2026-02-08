using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Generador_Pautas
{
    [ToolboxItem(true)]
    [DesignTimeVisible(true)]
    public partial class ElegantProgressBar : UserControl
    {
        private uint _value = 0;
        private uint _maximum = 100;
        private Color _backgroundColor = Color.LightGray;
        private Color _progressColorStart = Color.FromArgb(0, 39, 127);
        private Color _progressColorEnd = Color.FromArgb(0, 39, 127);

        public ElegantProgressBar()
        {
            InitializeComponent();
            this.SetStyle(ControlStyles.UserPaint |
                          ControlStyles.OptimizedDoubleBuffer |
                          ControlStyles.AllPaintingInWmPaint |
                          ControlStyles.ResizeRedraw |
                          ControlStyles.Opaque, true);
            this.DoubleBuffered = true;
            this.UpdateStyles();
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

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            this.Invalidate();
        }

        public uint Value
        {
            get { return _value; }
            set
            {
                _value = Math.Min(value, Maximum);
                Invalidate();
            }
        }

        public uint Maximum
        {
            get { return _maximum; }
            set
            {
                _maximum = value;
                Value = Math.Min(Value, Maximum);
                Invalidate();
            }
        }

        public Color BackgroundColor
        {
            get { return _backgroundColor; }
            set
            {
                _backgroundColor = value;
                Invalidate();
            }
        }

        public Color ProgressColorStart
        {
            get { return _progressColorStart; }
            set
            {
                _progressColorStart = value;
                Invalidate();
            }
        }

        public Color ProgressColorEnd
        {
            get { return _progressColorEnd; }
            set
            {
                _progressColorEnd = value;
                Invalidate();
            }
        }
        public bool IsAudioLevel { get; set; } = false;

        protected override void OnPaint(PaintEventArgs e)
        {
            // Evitar errores si el control es muy pequeño
            if (ClientRectangle.Width <= 0 || ClientRectangle.Height <= 0)
                return;

            // Dibujar el fondo sólido (sin gradiente para evitar artefactos)
            using (var bgBrush = new SolidBrush(BackgroundColor))
            {
                e.Graphics.FillRectangle(bgBrush, ClientRectangle);
            }

            // Dibujar el progreso
            var progressWidth = (int)(ClientRectangle.Width * ((double)Value / Maximum));
            if (progressWidth > 0)
            {
                Color progressColor;
                // Si se está midiendo el nivel de audio, cambia los colores a verde y amarillo
                if (IsAudioLevel)
                {
                    if (Value <= Maximum * 0.9)
                    {
                        progressColor = Color.FromArgb(0, 200, 83);
                    }
                    else
                    {
                        progressColor = Color.Yellow;
                    }
                }
                // Si no, usa los colores predeterminados
                else
                {
                    progressColor = ProgressColorStart;
                }

                var progressRect = new Rectangle(0, 0, progressWidth, ClientRectangle.Height);
                using (var progressBrush = new SolidBrush(progressColor))
                {
                    e.Graphics.FillRectangle(progressBrush, progressRect);
                }
            }
        }


















        private uint _minimum = 0;

        public uint Minimum
        {
            get { return _minimum; }
            set
            {
                _minimum = value;
                Value = Math.Max(Value, Minimum);
                Invalidate();
            }
        }






    }
}

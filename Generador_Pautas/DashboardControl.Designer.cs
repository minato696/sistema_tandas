namespace Generador_Pautas
{
    partial class DashboardControl
    {
        /// <summary>
        /// Variable del disenador necesaria.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        #region Codigo generado por el Disenador de componentes

        /// <summary>
        /// Metodo necesario para admitir el Disenador. No se puede modificar
        /// el contenido de este metodo con el editor de codigo.
        /// </summary>
        private void InitializeComponent()
        {
            this.lblTitulo = new System.Windows.Forms.Label();
            this.panelAlertas = new System.Windows.Forms.Panel();
            this.lblAlertasTitulo = new System.Windows.Forms.Label();
            this.SuspendLayout();
            //
            // lblTitulo
            //
            this.lblTitulo.AutoSize = true;
            this.lblTitulo.Font = new System.Drawing.Font("Segoe UI Semibold", 11.25F, System.Drawing.FontStyle.Bold);
            this.lblTitulo.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(63)))), ((int)(((byte)(81)))), ((int)(((byte)(181)))));
            this.lblTitulo.Location = new System.Drawing.Point(15, 12);
            this.lblTitulo.Name = "lblTitulo";
            this.lblTitulo.Size = new System.Drawing.Size(0, 20);
            this.lblTitulo.TabIndex = 0;
            //
            // panelAlertas - oculto en modo compacto
            //
            this.panelAlertas.AutoScroll = true;
            this.panelAlertas.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(250)))), ((int)(((byte)(255)))));
            this.panelAlertas.Location = new System.Drawing.Point(15, 130);
            this.panelAlertas.Name = "panelAlertas";
            this.panelAlertas.Size = new System.Drawing.Size(450, 0);
            this.panelAlertas.TabIndex = 1;
            this.panelAlertas.Visible = false;
            //
            // lblAlertasTitulo - oculto en modo compacto
            //
            this.lblAlertasTitulo.AutoSize = true;
            this.lblAlertasTitulo.Font = new System.Drawing.Font("Segoe UI Semibold", 9.75F, System.Drawing.FontStyle.Bold);
            this.lblAlertasTitulo.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(152)))), ((int)(((byte)(0)))));
            this.lblAlertasTitulo.Location = new System.Drawing.Point(15, 130);
            this.lblAlertasTitulo.Name = "lblAlertasTitulo";
            this.lblAlertasTitulo.Size = new System.Drawing.Size(175, 17);
            this.lblAlertasTitulo.TabIndex = 2;
            this.lblAlertasTitulo.Text = "Alertas - Proximos a Vencer";
            this.lblAlertasTitulo.Visible = false;
            //
            // DashboardControl
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(245)))));
            this.Controls.Add(this.lblAlertasTitulo);
            this.Controls.Add(this.panelAlertas);
            this.Controls.Add(this.lblTitulo);
            this.Name = "DashboardControl";
            this.Size = new System.Drawing.Size(240, 44);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label lblTitulo;
        private System.Windows.Forms.Panel panelAlertas;
        private System.Windows.Forms.Label lblAlertasTitulo;
    }
}

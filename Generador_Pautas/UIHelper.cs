using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Generador_Pautas
{
    public class UIHelper
    {
        public void LimpiarCampos(TextBox txtCodigoCu, DateTimePicker dtpInicia, DateTimePicker dtpFinaliza, ComboBox cboCiudad, ComboBox cboRadio, ComboBox cboPosicion, ComboBox cboEstado)
        {
            txtCodigoCu.Clear();
            dtpInicia.Value = DateTime.Now;
            dtpFinaliza.Value = DateTime.Now;
            cboCiudad.SelectedIndex = -1;
            cboRadio.SelectedIndex = -1;
            cboPosicion.SelectedIndex = -1;
            cboEstado.SelectedIndex = -1;
        }
        public void AgregarFila(DataGridView dgv_base, AgregarComercialesData comercialesData, string filePath)
        {
            DataGridViewRow row = new DataGridViewRow();
            row.Cells.Add(new DataGridViewTextBoxCell { Value = comercialesData.Codigo });
            row.Cells.Add(new DataGridViewTextBoxCell { Value = filePath });
            row.Cells.Add(new DataGridViewTextBoxCell { Value = comercialesData.FechaInicio.ToString("dd/MM/yyyy") });
            row.Cells.Add(new DataGridViewTextBoxCell { Value = comercialesData.FechaFinal.ToString("dd/MM/yyyy") });
            row.Cells.Add(new DataGridViewTextBoxCell { Value = comercialesData.Ciudad });
            row.Cells.Add(new DataGridViewTextBoxCell { Value = comercialesData.Radio });
            row.Cells.Add(new DataGridViewTextBoxCell { Value = comercialesData.Posicion });
            row.Cells.Add(new DataGridViewTextBoxCell { Value = comercialesData.Estado });
            dgv_base.Rows.Add(row);
        }
        public void PopulateComboBoxes(ComboBox cboEstado, ComboBox cboProgramacion, ComboBox cboCiudad, ComboBox cboRadio)
        {
            PopulateComboBox(cboEstado, new[] { "Activo", "Inactivo" });
            // Ordenados: primero los de 2 tandas por hora (48), luego el de 4 tandas por hora (96)
            PopulateComboBox(cboProgramacion, new[] {
                "Cada 00-30 (48 tandas)",
                "Cada 10-40 (48 tandas)",
                "Cada 15-45 (48 tandas)",
                "Cada 20-50 (48 tandas)",
                "Cada 00-20-30-50 (96 tandas)"
            });

            // Cargar ciudades y radios desde la base de datos
            CargarCiudadesDesdeDB(cboCiudad);
            CargarRadiosDesdeDB(cboRadio);
        }

        private void CargarCiudadesDesdeDB(ComboBox cboCiudad)
        {
            cboCiudad.Items.Clear();

            try
            {
                // Usar Task.Run para ejecutar el metodo async de forma sincrona
                var ciudades = Task.Run(async () => await AdminCiudadesForm.ObtenerCiudadesActivasAsync()).Result;

                foreach (string ciudad in ciudades)
                {
                    cboCiudad.Items.Add(ciudad);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al cargar ciudades: {ex.Message}");
                // Si hay error, usar ciudades por defecto
                cboCiudad.Items.AddRange(new[] { "LIMA", "PROVINCIAS" });
            }
        }

        private void CargarRadiosDesdeDB(ComboBox cboRadio)
        {
            cboRadio.Items.Clear();

            try
            {
                // Usar Task.Run para ejecutar el metodo async de forma sincrona
                var radios = Task.Run(async () => await AdminRadiosForm.ObtenerRadiosActivasAsync()).Result;

                foreach (string radio in radios)
                {
                    cboRadio.Items.Add(radio);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al cargar radios: {ex.Message}");
                // Si hay error, usar radios por defecto
                cboRadio.Items.AddRange(new[] { "EXITOSA", "KARIBEÑA", "LAKALLE" });
            }
        }

        /// <summary>
        /// Carga las radios filtradas por ciudad seleccionada
        /// </summary>
        public void CargarRadiosPorCiudad(ComboBox cboRadio, string ciudadSeleccionada)
        {
            cboRadio.Items.Clear();

            if (string.IsNullOrEmpty(ciudadSeleccionada))
            {
                CargarRadiosDesdeDB(cboRadio);
                return;
            }

            try
            {
                // Usar Task.Run para ejecutar el metodo async de forma sincrona
                var radios = Task.Run(async () => await AdminRadiosForm.ObtenerRadiosPorCiudadAsync(ciudadSeleccionada)).Result;

                foreach (string radio in radios)
                {
                    cboRadio.Items.Add(radio);
                }

                // Seleccionar la primera radio si hay disponibles
                if (cboRadio.Items.Count > 0)
                {
                    cboRadio.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al cargar radios por ciudad: {ex.Message}");
                // Si hay error, cargar todas las radios
                CargarRadiosDesdeDB(cboRadio);
            }
        }
        public void SetComboBoxesDefault(ComboBox cboEstado, ComboBox cboProgramacion, ComboBox cboCiudad, ComboBox cboRadio)
        {
            SetComboBoxDefault(cboEstado, "Activo");
            SetComboBoxDefault(cboProgramacion, "Cada 00-30 (48 tandas)");
            SetComboBoxDefault(cboRadio, "EXITOSA");

            // Seleccionar la primera ciudad disponible
            if (cboCiudad.Items.Count > 0)
            {
                cboCiudad.SelectedIndex = 0;
            }
        }
        private void PopulateComboBox(ComboBox comboBox, string[] items)
        {
            comboBox.Items.Clear();
            comboBox.Items.AddRange(items);
        }
        private void SetComboBoxDefault(ComboBox comboBox, string defaultItem)
        {
            comboBox.SelectedIndex = comboBox.Items.IndexOf(defaultItem);
        }
    }
}

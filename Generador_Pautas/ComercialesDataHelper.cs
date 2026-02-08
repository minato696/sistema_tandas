using System;
using System.Data;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Generador_Pautas
{
    /// <summary>
    /// Helper para operaciones de UI relacionadas con comerciales.
    /// Maneja la carga de datos en controles y la creacion de objetos de datos.
    /// </summary>
    public class ComercialesDataHelper
    {
        /// <summary>
        /// Carga datos de comerciales en un DataGridView
        /// </summary>
        public async Task CargarDatosAsync(string connectionString, string tableName, DataGridView dgv)
        {
            var tableData = await DataAccess.CargarDatosDesdeBaseDeDatosAsync(connectionString, tableName);
            LlenarDataGridView(dgv, tableData);
        }

        /// <summary>
        /// Carga datos filtrados por estado en un DataGridView
        /// </summary>
        public async Task CargarDatosFiltradosAsync(string connectionString, string tableName, DataGridView dgv, string estadoFiltro)
        {
            var tableData = await DataAccess.CargarDatosFiltradosPorEstadoAsync(connectionString, tableName, estadoFiltro);
            LlenarDataGridView(dgv, tableData);
        }

        /// <summary>
        /// Llena un DataGridView con los datos de un DataTable
        /// </summary>
        private void LlenarDataGridView(DataGridView dgv, DataTable tableData)
        {
            dgv.Rows.Clear();
            foreach (DataRow row in tableData.Rows)
            {
                dgv.Rows.Add(
                    row["Codigo"].ToString(),
                    row["FilePath"].ToString(),
                    Convert.ToDateTime(row["FechaInicio"]).ToString("dd/MM/yyyy"),
                    Convert.ToDateTime(row["FechaFinal"]).ToString("dd/MM/yyyy"),
                    row["Ciudad"].ToString(),
                    row["Radio"].ToString(),
                    row["Posicion"].ToString(),
                    row["Estado"].ToString()
                );
            }
        }

        /// <summary>
        /// Crea un objeto AgregarComercialesData con los valores proporcionados
        /// </summary>
        public AgregarComercialesData GetComercialesData(
            string codigo, DateTime fechaInicio, DateTime fechaFinal,
            string ciudad, string radio, string posicion, string estado,
            string tipoProgramacion = "Cada 00-30")
        {
            return new AgregarComercialesData
            {
                Codigo = codigo,
                FechaInicio = fechaInicio,
                FechaFinal = fechaFinal,
                Ciudad = ciudad,
                Radio = radio,
                Posicion = posicion,
                Estado = estado,
                TipoProgramacion = tipoProgramacion
            };
        }

        /// <summary>
        /// Carga datos de un comercial en los controles del formulario
        /// </summary>
        public void LoadData(
            AgregarComercialesData data,
            TextBox txtCodigoCu, TextBox txtSpot,
            DateTimePicker dtpInicia, DateTimePicker dtpFinaliza,
            ComboBox cboCiudad, ComboBox cboRadio,
            ComboBox cboPosicion, ComboBox cboEstado,
            ComboBox cboProgramacion = null)
        {
            if (data == null) return;

            // Codigo y archivo
            txtCodigoCu.Text = ExtraerCodigoNumerico(data.Codigo);
            txtSpot.Text = Path.GetFileName(data.FilePath);

            // Fechas con validacion
            CargarFechaSegura(dtpInicia, data.FechaInicio);
            CargarFechaSegura(dtpFinaliza, data.FechaFinal);

            // Combos
            SeleccionarOAgregar(cboCiudad, data.Ciudad);
            SeleccionarOAgregar(cboRadio, data.Radio);

            // Posicion (quitar P si existe)
            if (!string.IsNullOrEmpty(data.Posicion))
            {
                string posicion = data.Posicion.StartsWith("P", StringComparison.OrdinalIgnoreCase)
                    ? data.Posicion.Substring(1)
                    : data.Posicion;
                cboPosicion.SelectedItem = posicion;
            }

            cboEstado.SelectedItem = data.Estado;

            // Tipo de programacion
            if (cboProgramacion != null)
            {
                string tipoProg = data.TipoProgramacion;
                if (string.IsNullOrEmpty(tipoProg) || tipoProg == "Importado Access" || tipoProg == "Cada 00-30")
                {
                    tipoProg = DetectarTipoProgramacionPorRadio(data.Radio);
                }
                SeleccionarPorPrefijo(cboProgramacion, tipoProg);
            }
        }

        /// <summary>
        /// Extrae el codigo numerico del formato ACC-42265-ABA-KAR-0050 o CU-0001
        /// </summary>
        private string ExtraerCodigoNumerico(string codigoCompleto)
        {
            if (string.IsNullOrEmpty(codigoCompleto)) return codigoCompleto;

            string[] partes = codigoCompleto.Split('-');
            if (partes.Length >= 2 && (partes[0] == "ACC" || partes[0] == "CU"))
            {
                return partes[1];
            }
            return codigoCompleto;
        }

        /// <summary>
        /// Detecta el tipo de programacion basandose en el nombre de la radio
        /// </summary>
        private string DetectarTipoProgramacionPorRadio(string radio)
        {
            if (string.IsNullOrEmpty(radio)) return "Cada 00-30";

            string radioUpper = radio.ToUpper();
            if (radioUpper.Contains("KARIBEÑA") || radioUpper.Contains("KARIBENA") ||
                radioUpper.Contains("KARIBEÃ") || radioUpper.Contains("KARIBE") ||
                radioUpper.Contains("LAKALLE") || radioUpper.Contains("LA KALLE") || radioUpper.Contains("KALLE"))
            {
                return "Cada 00-20-30-50";
            }
            return "Cada 00-30";
        }

        /// <summary>
        /// Carga una fecha en un DateTimePicker con validacion de rango
        /// </summary>
        private void CargarFechaSegura(DateTimePicker dtp, DateTime fecha)
        {
            if (fecha >= dtp.MinDate && fecha <= dtp.MaxDate)
                dtp.Value = fecha;
            else
                dtp.Value = DateTime.Today;
        }

        /// <summary>
        /// Selecciona un item en un ComboBox, o lo agrega si no existe
        /// </summary>
        private void SeleccionarOAgregar(ComboBox combo, string valor)
        {
            if (string.IsNullOrEmpty(valor)) return;

            int index = combo.FindStringExact(valor);
            if (index >= 0)
            {
                combo.SelectedIndex = index;
            }
            else
            {
                combo.Items.Add(valor);
                combo.SelectedItem = valor;
            }
        }

        /// <summary>
        /// Selecciona un item que comience con el prefijo especificado
        /// </summary>
        private void SeleccionarPorPrefijo(ComboBox combo, string prefijo)
        {
            int index = combo.FindStringExact(prefijo);
            if (index < 0)
            {
                for (int i = 0; i < combo.Items.Count; i++)
                {
                    if (combo.Items[i].ToString().StartsWith(prefijo, StringComparison.OrdinalIgnoreCase))
                    {
                        index = i;
                        break;
                    }
                }
            }
            if (index >= 0) combo.SelectedIndex = index;
        }
    }
}

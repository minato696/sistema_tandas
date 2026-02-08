using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Generador_Pautas
{
    public class ComercialesDataHelper
    {
        public async Task CargarDatosAsync(string connectionString, string tableName, DataGridView dataGridView1)
        {
            var tableData = await DataAccess.CargarDatosDesdeBaseDeDatosAsync(connectionString, tableName);

            dataGridView1.Rows.Clear(); // Limpia las filas existentes en el DataGridView

            foreach (DataRow row in tableData.Rows)
            {
                // Agregar fila con valores directamente (no crear DataGridViewRow manualmente)
                dataGridView1.Rows.Add(
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

        public async Task CargarDatosFiltradosAsync(string connectionString, string tableName, DataGridView dataGridView1, string estadoFiltro)
        {
            var tableData = await DataAccess.CargarDatosFiltradosPorEstadoAsync(connectionString, tableName, estadoFiltro);

            dataGridView1.Rows.Clear(); // Limpia las filas existentes en el DataGridView

            foreach (DataRow row in tableData.Rows)
            {
                // Agregar fila con valores directamente (no crear DataGridViewRow manualmente)
                dataGridView1.Rows.Add(
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

        public AgregarComercialesData GetComercialesData(string codigo, DateTime fechaInicio, DateTime fechaFinal, string ciudad, string radio, string posicion, string estado, string tipoProgramacion = "Cada 00-30")
        {
            AgregarComercialesData data = new AgregarComercialesData();

            // Obtener los datos del formulario
            data.Codigo = codigo;
            data.FechaInicio = fechaInicio;
            data.FechaFinal = fechaFinal;
            data.Ciudad = ciudad;
            data.Radio = radio;
            data.Posicion = posicion;
            data.Estado = estado;
            data.TipoProgramacion = tipoProgramacion;
            return data;
        }

        public void LoadData(AgregarComercialesData data, TextBox txtCodigoCu, TextBox txtSpot, DateTimePicker dtpInicia, DateTimePicker dtpFinaliza, ComboBox cboCiudad, ComboBox cboRadio, ComboBox cboPosicion, ComboBox cboEstado, ComboBox cboProgramacion = null)
        {
            if (data != null)
            {
                // Extraer solo el codigo numerico del formato ACC-42265-ABA-KAR-0050
                txtCodigoCu.Text = ExtraerCodigoNumerico(data.Codigo);
                txtSpot.Text = Path.GetFileName(data.FilePath); // Obtener solo el nombre del archivo

                // Validar fechas antes de asignar (evitar fechas fuera de rango)
                DateTime fechaMinima = dtpInicia.MinDate;
                DateTime fechaMaxima = dtpInicia.MaxDate;

                if (data.FechaInicio >= fechaMinima && data.FechaInicio <= fechaMaxima)
                {
                    dtpInicia.Value = data.FechaInicio;
                }
                else
                {
                    dtpInicia.Value = DateTime.Today; // Usar fecha actual si la fecha es inválida
                }

                if (data.FechaFinal >= fechaMinima && data.FechaFinal <= fechaMaxima)
                {
                    dtpFinaliza.Value = data.FechaFinal;
                }
                else
                {
                    dtpFinaliza.Value = DateTime.Today; // Usar fecha actual si la fecha es inválida
                }

                // Intentar seleccionar ciudad, si no existe agregarla
                if (!string.IsNullOrEmpty(data.Ciudad))
                {
                    int indexCiudad = cboCiudad.FindStringExact(data.Ciudad);
                    if (indexCiudad >= 0)
                    {
                        cboCiudad.SelectedIndex = indexCiudad;
                    }
                    else
                    {
                        // Agregar la ciudad si no existe
                        cboCiudad.Items.Add(data.Ciudad);
                        cboCiudad.SelectedItem = data.Ciudad;
                    }
                }

                // Intentar seleccionar radio, si no existe agregarla
                if (!string.IsNullOrEmpty(data.Radio))
                {
                    int indexRadio = cboRadio.FindStringExact(data.Radio);
                    if (indexRadio >= 0)
                    {
                        cboRadio.SelectedIndex = indexRadio;
                    }
                    else
                    {
                        // Agregar la radio si no existe
                        cboRadio.Items.Add(data.Radio);
                        cboRadio.SelectedItem = data.Radio;
                    }
                }

                // Manejar posicion con formato "P01" o "01"
                string posicion = data.Posicion;
                if (!string.IsNullOrEmpty(posicion))
                {
                    // Si viene con formato "P01", quitar la P
                    if (posicion.StartsWith("P", StringComparison.OrdinalIgnoreCase))
                    {
                        posicion = posicion.Substring(1);
                    }
                    cboPosicion.SelectedItem = posicion;
                }

                cboEstado.SelectedItem = data.Estado;

                // Seleccionar tipo de programacion - usar el valor guardado en la BD
                if (cboProgramacion != null)
                {
                    string tipoProgramacion = !string.IsNullOrEmpty(data.TipoProgramacion) ? data.TipoProgramacion : "Cada 00-30";

                    // Si es "Importado Access" o valor genérico, detectar por radio
                    if (tipoProgramacion == "Importado Access" || tipoProgramacion == "Cada 00-30")
                    {
                        tipoProgramacion = DetectarTipoProgramacionPorRadio(data.Radio);
                    }

                    // Primero intentar match exacto
                    int indexProgramacion = cboProgramacion.FindStringExact(tipoProgramacion);

                    // Si no encuentra match exacto, buscar por match parcial (el valor en BD puede no tener "(48 tandas)")
                    if (indexProgramacion < 0)
                    {
                        // Buscar un item que comience con el valor de la BD
                        for (int i = 0; i < cboProgramacion.Items.Count; i++)
                        {
                            string item = cboProgramacion.Items[i].ToString();
                            if (item.StartsWith(tipoProgramacion, StringComparison.OrdinalIgnoreCase))
                            {
                                indexProgramacion = i;
                                break;
                            }
                        }
                    }

                    if (indexProgramacion >= 0)
                    {
                        cboProgramacion.SelectedIndex = indexProgramacion;
                    }
                }
            }
        }

        /// <summary>
        /// Extrae el codigo numerico del formato ACC-42265-ABA-KAR-0050
        /// Retorna "42265" (el segundo segmento)
        /// </summary>
        private string ExtraerCodigoNumerico(string codigoCompleto)
        {
            if (string.IsNullOrEmpty(codigoCompleto))
                return codigoCompleto;

            // Si el codigo tiene formato ACC-XXXXX-XXX-XXX-XXXX, extraer el segundo segmento
            string[] partes = codigoCompleto.Split('-');
            if (partes.Length >= 2 && partes[0] == "ACC")
            {
                return partes[1]; // Retorna el codigo numerico (42265)
            }

            // Si no tiene el formato esperado, retornar el codigo original
            return codigoCompleto;
        }

        /// <summary>
        /// Detecta el tipo de programacion basandose en el nombre de la radio
        /// </summary>
        private string DetectarTipoProgramacionPorRadio(string radio)
        {
            if (string.IsNullOrEmpty(radio))
                return "Cada 00-30";

            string radioUpper = radio.ToUpper();

            // KARIBEÑA y LA KALLE usan las 4 tandas: 00, 20, 30, 50
            // Incluir variantes de codificación: KARIBEÑA, KARIBENA, KARIBEÃA (UTF-8 mal interpretado)
            if (radioUpper.Contains("KARIBEÑA") || radioUpper.Contains("KARIBENA") ||
                radioUpper.Contains("KARIBEÃ") || radioUpper.Contains("KARIBE") ||
                radioUpper.Contains("LAKALLE") || radioUpper.Contains("LA KALLE") || radioUpper.Contains("KALLE"))
            {
                return "Cada 00-20-30-50";
            }

            // EXITOSA y otros usan 00-30 por defecto
            return "Cada 00-30";
        }
    }

}

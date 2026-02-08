using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Generador_Pautas
{
    public class DataHandler
    {
        public async Task<DataTable> LoadDataFromDatabaseAsync(string connectionString, string tableName)
        {
            DataTable tableData = await DataAccess.CargarDatosDesdeBaseDeDatosAsync(connectionString, tableName);
            return tableData;
        }

        public List<DataGridViewRow> ConvertComercialesDataTableToDataGridViewRows(DataTable tableData)
        {
            List<DataGridViewRow> rows = new List<DataGridViewRow>();

            foreach (DataRow row in tableData.Rows)
            {
                string codigo = row["Codigo"]?.ToString() ?? "";
                string filePath = row["FilePath"]?.ToString() ?? "";

                // PostgreSQL almacena fechas como DateTime, parseamos manualmente por compatibilidad
                DateTime fechaInicio = DateTime.MinValue;
                DateTime fechaFinal = DateTime.MinValue;
                DateTime.TryParse(row["FechaInicio"]?.ToString(), out fechaInicio);
                DateTime.TryParse(row["FechaFinal"]?.ToString(), out fechaFinal);

                string ciudad = row["Ciudad"]?.ToString() ?? "";
                string radio = row["Radio"]?.ToString() ?? "";
                string posicion = row["Posicion"]?.ToString() ?? "";
                string estado = row["Estado"]?.ToString() ?? "";

                DataGridViewRow newRow = new DataGridViewRow();
                newRow.Cells.Add(new DataGridViewTextBoxCell { Value = codigo });
                newRow.Cells.Add(new DataGridViewTextBoxCell { Value = filePath });
                newRow.Cells.Add(new DataGridViewTextBoxCell { Value = fechaInicio.ToString("dd/MM/yyyy") });
                newRow.Cells.Add(new DataGridViewTextBoxCell { Value = fechaFinal.ToString("dd/MM/yyyy") });
                newRow.Cells.Add(new DataGridViewTextBoxCell { Value = ciudad });
                newRow.Cells.Add(new DataGridViewTextBoxCell { Value = radio });
                newRow.Cells.Add(new DataGridViewTextBoxCell { Value = posicion });
                newRow.Cells.Add(new DataGridViewTextBoxCell { Value = estado });

                rows.Add(newRow);
            }

            return rows;
        }
    }
}

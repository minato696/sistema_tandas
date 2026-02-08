using System;
using System.Data;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Generador_Pautas
{
    /// <summary>
    /// Maneja la carga de datos en DataGridViews.
    /// Wrapper simplificado sobre DataAccess.
    /// </summary>
    public class DatabaseManager
    {
        /// <summary>
        /// Carga datos desde la base de datos con limite para carga rapida inicial.
        /// </summary>
        public async Task LoadDataFromDatabaseAsync(DataGridView dgv, string connectionString, string tableName, int limite = 500)
        {
            ConfigManager.ActualizarCacheAntesDeLectura();
            DataTable tableData = await DataAccess.CargarDatosDesdeBaseDeDatosAsync(connectionString, tableName, limite);

            if (tableName != "Comerciales")
                throw new ArgumentException($"Tabla no reconocida: {tableName}");

            dgv.SuspendLayout();
            dgv.Rows.Clear();

            foreach (DataRow row in tableData.Rows)
            {
                DateTime.TryParse(row["FechaInicio"]?.ToString(), out DateTime fechaInicio);
                DateTime.TryParse(row["FechaFinal"]?.ToString(), out DateTime fechaFinal);

                dgv.Rows.Add(
                    row["Codigo"]?.ToString() ?? "",
                    row["FilePath"]?.ToString() ?? "",
                    fechaInicio.ToString("dd/MM/yyyy"),
                    fechaFinal.ToString("dd/MM/yyyy"),
                    row["Ciudad"]?.ToString() ?? "",
                    row["Radio"]?.ToString() ?? "",
                    row["Posicion"]?.ToString() ?? "",
                    row["Estado"]?.ToString() ?? ""
                );
            }

            dgv.ResumeLayout();
        }
    }
}

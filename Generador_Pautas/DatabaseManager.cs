using System;
using System.Data;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Generador_Pautas
{
    public class DatabaseManager
    {
        /// <summary>
        /// Carga datos desde la base de datos con limite para carga rapida inicial.
        /// Por defecto carga solo 500 registros mas recientes.
        /// </summary>
        public async Task LoadDataFromDatabaseAsync(DataGridView dgv_base, string connectionString, string tableName, int limite = 500)
        {
            // Actualizar cache antes de cargar datos
            ConfigManager.ActualizarCacheAntesDeLectura();

            // Cargar datos con limite (mas rapido para carga inicial)
            DataTable tableData = await DataAccess.CargarDatosDesdeBaseDeDatosAsync(connectionString, tableName, limite);

            if (tableName != "Comerciales")
            {
                throw new ArgumentException($"No se reconoce el nombre de la tabla: {tableName}");
            }

            // Suspender layout para evitar parpadeo y mejorar rendimiento
            dgv_base.SuspendLayout();
            dgv_base.Rows.Clear();

            // Agregar filas directamente sin crear objetos DataGridViewRow intermedios
            foreach (DataRow row in tableData.Rows)
            {
                string codigo = row["Codigo"]?.ToString() ?? "";
                string filePath = row["FilePath"]?.ToString() ?? "";

                DateTime fechaInicio = DateTime.MinValue;
                DateTime fechaFinal = DateTime.MinValue;
                DateTime.TryParse(row["FechaInicio"]?.ToString(), out fechaInicio);
                DateTime.TryParse(row["FechaFinal"]?.ToString(), out fechaFinal);

                string ciudad = row["Ciudad"]?.ToString() ?? "";
                string radio = row["Radio"]?.ToString() ?? "";
                string posicion = row["Posicion"]?.ToString() ?? "";
                string estado = row["Estado"]?.ToString() ?? "";

                dgv_base.Rows.Add(
                    codigo,
                    filePath,
                    fechaInicio.ToString("dd/MM/yyyy"),
                    fechaFinal.ToString("dd/MM/yyyy"),
                    ciudad,
                    radio,
                    posicion,
                    estado
                );
            }

            dgv_base.ResumeLayout();
        }
    }
}

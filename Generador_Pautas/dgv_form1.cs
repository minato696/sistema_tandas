using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Generador_Pautas
{
    public class DGV_Form1
    {
        private DataGridView dgv_archivos;
        private DataGridView dgv_base;
        private DataGridView dgv_estaciones;
        private DataGridView dgv_ciudades;

        public DGV_Form1(DataGridView dgvArchivos, DataGridView dgvBase, DataGridView dgvEstaciones, DataGridView dgvCiudades)
        {
            this.dgv_archivos = dgvArchivos;
            this.dgv_base = dgvBase;
            this.dgv_estaciones = dgvEstaciones;
            this.dgv_ciudades = dgvCiudades;

            PersonalizarDataGridView();
        }
        private void PersonalizarDataGridView()
        {
            PersonalizarDGV(dgv_archivos, new[] { "Column2", "Column3", "Column4" },
                new[] { 450, 50, 50 });
            // dgv_base se configura dinámicamente en Form1.cs
            PersonalizarDGV(dgv_estaciones, new string[0], new int[0]);
            PersonalizarDGV(dgv_ciudades, new string[0], new int[0]);
            AgregarFila(dgv_estaciones, new[] { "EXITOSA", "KARIBEÑA", "LA KALLE" });
            AgregarFila(dgv_ciudades, new[] { "CHICLAYO", "TRUJILLO", "PIURA" });
        }

        private void PersonalizarDGV(DataGridView dgv, string[] columnas, int[] anchos)
        {
            for (int i = 0; i < columnas.Length; i++)
            {
                dgv.Columns[columnas[i]].Width = anchos[i];
            }
            dgv.CellFormatting += (sender, e) =>
            {
                // Columna 0 (Nombre) alineada a la izquierda, el resto centrado
                if (e.ColumnIndex == 0)
                    e.CellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                else
                    e.CellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            };
            dgv.CellPainting += (sender, e) =>
            {
                if (e.RowIndex == -1)
                    e.CellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            };
        }

        private void AgregarFila(DataGridView dgv, string[] filas)
        {
            foreach (var fila in filas)
            {
                dgv.Rows.Add(fila);
            }
        }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Generador_Pautas
{
    public class AgregarComercialesData
    {
        public string Codigo { get; set; }
        public string FilePath { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFinal { get; set; }
        public string Ciudad { get; set; }
        public string Radio { get; set; }
        public string Posicion { get; set; }
        public string Estado { get; set; }
        public string TipoProgramacion { get; set; } = "Cada 00-30";

    }
}

using System;

namespace Generador_Pautas
{
    /// <summary>
    /// Clase para almacenar información de comerciales asignados a tandas
    /// </summary>
    public class ComercialAsignadoInfo
    {
        public int Fila { get; set; }
        public int Columna { get; set; }
        public string ComercialAsignado { get; set; }
        public DateTime? Fecha { get; set; }
    }
}

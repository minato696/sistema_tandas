using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Generador_Pautas
{
    public enum TipoTanda
    {
        Tandas_00_30,  // 00 y 30 minutos - 48 tandas
        Tandas_20_50,  // 20 y 50 minutos - 48 tandas
        Tandas_10_40,  // 10 y 40 minutos - 48 tandas
        Tandas_15_45,  // 15 y 45 minutos - 48 tandas
        Tandas_00_20_30_50  // 00, 20, 30 y 50 minutos - 96 tandas (4 por hora)
    }

    public static class TandasHorarias
    {
        // Tandas a las 00 y 30 minutos de cada hora (48 tandas totales)
        private static readonly string[] tandas_00_30 =
        {
            "00:00", "00:30", "01:00", "01:30", "02:00", "02:30", "03:00", "03:30",
            "04:00", "04:30", "05:00", "05:30", "06:00", "06:30", "07:00", "07:30",
            "08:00", "08:30", "09:00", "09:30", "10:00", "10:30", "11:00", "11:30",
            "12:00", "12:30", "13:00", "13:30", "14:00", "14:30", "15:00", "15:30",
            "16:00", "16:30", "17:00", "17:30", "18:00", "18:30", "19:00", "19:30",
            "20:00", "20:30", "21:00", "21:30", "22:00", "22:30", "23:00", "23:30"
        };

        // Tandas a las 20 y 50 minutos de cada hora (48 tandas totales)
        private static readonly string[] tandas_20_50 =
        {
            "00:20", "00:50", "01:20", "01:50", "02:20", "02:50", "03:20", "03:50",
            "04:20", "04:50", "05:20", "05:50", "06:20", "06:50", "07:20", "07:50",
            "08:20", "08:50", "09:20", "09:50", "10:20", "10:50", "11:20", "11:50",
            "12:20", "12:50", "13:20", "13:50", "14:20", "14:50", "15:20", "15:50",
            "16:20", "16:50", "17:20", "17:50", "18:20", "18:50", "19:20", "19:50",
            "20:20", "20:50", "21:20", "21:50", "22:20", "22:50", "23:20", "23:50"
        };

        // Tandas a las 10 y 40 minutos de cada hora (48 tandas totales)
        private static readonly string[] tandas_10_40 =
        {
            "00:10", "00:40", "01:10", "01:40", "02:10", "02:40", "03:10", "03:40",
            "04:10", "04:40", "05:10", "05:40", "06:10", "06:40", "07:10", "07:40",
            "08:10", "08:40", "09:10", "09:40", "10:10", "10:40", "11:10", "11:40",
            "12:10", "12:40", "13:10", "13:40", "14:10", "14:40", "15:10", "15:40",
            "16:10", "16:40", "17:10", "17:40", "18:10", "18:40", "19:10", "19:40",
            "20:10", "20:40", "21:10", "21:40", "22:10", "22:40", "23:10", "23:40"
        };

        // Tandas a las 15 y 45 minutos de cada hora (48 tandas totales)
        private static readonly string[] tandas_15_45 =
        {
            "00:15", "00:45", "01:15", "01:45", "02:15", "02:45", "03:15", "03:45",
            "04:15", "04:45", "05:15", "05:45", "06:15", "06:45", "07:15", "07:45",
            "08:15", "08:45", "09:15", "09:45", "10:15", "10:45", "11:15", "11:45",
            "12:15", "12:45", "13:15", "13:45", "14:15", "14:45", "15:15", "15:45",
            "16:15", "16:45", "17:15", "17:45", "18:15", "18:45", "19:15", "19:45",
            "20:15", "20:45", "21:15", "21:45", "22:15", "22:45", "23:15", "23:45"
        };

        // Tandas a las 00, 20, 30 y 50 minutos de cada hora (96 tandas totales - 4 por hora)
        private static readonly string[] tandas_00_20_30_50 =
        {
            "00:00", "00:20", "00:30", "00:50", "01:00", "01:20", "01:30", "01:50",
            "02:00", "02:20", "02:30", "02:50", "03:00", "03:20", "03:30", "03:50",
            "04:00", "04:20", "04:30", "04:50", "05:00", "05:20", "05:30", "05:50",
            "06:00", "06:20", "06:30", "06:50", "07:00", "07:20", "07:30", "07:50",
            "08:00", "08:20", "08:30", "08:50", "09:00", "09:20", "09:30", "09:50",
            "10:00", "10:20", "10:30", "10:50", "11:00", "11:20", "11:30", "11:50",
            "12:00", "12:20", "12:30", "12:50", "13:00", "13:20", "13:30", "13:50",
            "14:00", "14:20", "14:30", "14:50", "15:00", "15:20", "15:30", "15:50",
            "16:00", "16:20", "16:30", "16:50", "17:00", "17:20", "17:30", "17:50",
            "18:00", "18:20", "18:30", "18:50", "19:00", "19:20", "19:30", "19:50",
            "20:00", "20:20", "20:30", "20:50", "21:00", "21:20", "21:30", "21:50",
            "22:00", "22:20", "22:30", "22:50", "23:00", "23:20", "23:30", "23:50"
        };

        private static readonly Dictionary<TipoTanda, string[]> tiposTandas = new Dictionary<TipoTanda, string[]>
        {
            { TipoTanda.Tandas_00_30, tandas_00_30 },
            { TipoTanda.Tandas_20_50, tandas_20_50 },
            { TipoTanda.Tandas_10_40, tandas_10_40 },
            { TipoTanda.Tandas_15_45, tandas_15_45 },
            { TipoTanda.Tandas_00_20_30_50, tandas_00_20_30_50 }
        };

        public static string[] GetHorarios(TipoTanda tipoTanda)
        {
            if (tiposTandas.TryGetValue(tipoTanda, out string[] horarios))
            {
                return horarios;
            }
            return new string[0];
        }

        public static TipoTanda GetTipoTandaFromString(string texto)
        {
            // Buscar patrones de minutos en el texto
            if (texto.Contains("00-20-30-50") || texto.Contains("00 20 30 50"))
            {
                return TipoTanda.Tandas_00_20_30_50;
            }
            else if (texto.Contains("00-30") || texto.Contains("00 30"))
            {
                return TipoTanda.Tandas_00_30;
            }
            else if (texto.Contains("10-40") || texto.Contains("10 40"))
            {
                return TipoTanda.Tandas_10_40;
            }
            else if (texto.Contains("15-45") || texto.Contains("15 45"))
            {
                return TipoTanda.Tandas_15_45;
            }
            else if (texto.Contains("20-50") || texto.Contains("20 50"))
            {
                return TipoTanda.Tandas_20_50;
            }
            return TipoTanda.Tandas_00_30; // Por defecto
        }

        /// <summary>
        /// Obtiene el indice de fila para una hora especifica en un tipo de tanda dado
        /// </summary>
        public static int GetFilaParaHora(string hora, TipoTanda tipoTanda)
        {
            var horarios = GetHorarios(tipoTanda);
            for (int i = 0; i < horarios.Length; i++)
            {
                if (horarios[i] == hora)
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// Convierte hora y minutos a fila segun el tipo de tanda
        /// </summary>
        public static int GetFilaParaHoraMinutos(int horas, int minutos, TipoTanda tipoTanda)
        {
            string horaFormateada = string.Format("{0:D2}:{1:D2}", horas, minutos);
            return GetFilaParaHora(horaFormateada, tipoTanda);
        }
    }
}

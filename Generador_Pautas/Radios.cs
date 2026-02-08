using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Generador_Pautas
{
    public enum RadioName
    {
        EXITOSA,
        KARIBEÑA,
        LAKALLE
    }

    public static class Radios
    {
        private static readonly string[] exitosaTimes =
        {
            "00:00", "00:30", "01:00", "01:30", "02:00", "02:30", "03:00", "03:30", "04:00", "04:30",
            "05:00", "05:30", "06:00", "06:30", "07:00", "07:30", "08:00", "08:30", "09:00", "09:30",
            "10:00", "10:30", "11:00", "11:30", "12:00", "12:30", "13:00", "13:30", "14:00", "14:30",
            "15:00", "15:30", "16:00", "16:30", "17:00", "17:30", "18:00", "18:30", "19:00", "19:30",
            "20:00", "20:30", "21:00", "21:30", "22:00", "22:30", "23:00", "23:30"
        };
        private static readonly string[] otherTimes =
        {
            "00:00", "00:20", "00:30", "00:50", "01:00", "01:20", "01:30", "01:50", "02:00", "02:20",
            "02:30", "02:50", "03:00", "03:20", "03:30", "03:50", "04:00", "04:20", "04:30", "04:50",
            "05:00", "05:20", "05:30", "05:50", "06:00", "06:20", "06:30", "06:50", "07:00", "07:20",
            "07:30", "07:50", "08:00", "08:20", "08:30", "08:50", "09:00", "09:20", "09:30", "09:50",
            "10:00", "10:20", "10:30", "10:50", "11:00", "11:20", "11:30", "11:50", "12:00", "12:20",
            "12:30", "12:50", "13:00", "13:20", "13:30", "13:50", "14:00", "14:20", "14:30", "14:50",
            "15:00", "15:20", "15:30", "15:50", "16:00", "16:20", "16:30", "16:50", "17:00", "17:20",
            "17:30", "17:50", "18:00", "18:20", "18:30", "18:50", "19:00", "19:20", "19:30", "19:50",
            "20:00", "20:20", "20:30", "20:50", "21:00", "21:20", "21:30", "21:50", "22:00", "22:20",
            "22:30", "22:50", "23:00", "23:20", "23:30", "23:50"
        };

        private static readonly Dictionary<RadioName, string[]> radioTimes = new Dictionary<RadioName, string[]>
        {
            { RadioName.EXITOSA, exitosaTimes },
            { RadioName.KARIBEÑA, otherTimes },
            { RadioName.LAKALLE, otherTimes }
        };
        public static string[] GetTimes(RadioName radioName)
        {
            if (radioTimes.TryGetValue(radioName, out string[] times))
            {
                return times;
            }

            return new string[0]; 
        }
    }
}


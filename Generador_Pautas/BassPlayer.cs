using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Un4seen.Bass;

namespace Generador_Pautas
{
    public class BassPlayer
    {
        public int CurrentStream { get; set; } = 0;  // Ahora CurrentStream tiene un setter
        public BassPlayer()
        {
            BassNet.Registration("d.c_78@hotmail.com", "2X9383018312422");
            InitBass();
        }

        public void InitBass()
        {
            // Liberar la instancia actual de BASS
            Bass.BASS_Free();

            // Inicializar BASS utilizando el dispositivo seleccionado
            if (!Bass.BASS_Init(-1, 44100, BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero))
            {
                throw new Exception("BASS_Init error!");
            }

            // Configurar el tamaño del búfer de audio y la frecuencia de actualización
            int bufferSizeInMilliseconds = 50;
            Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_BUFFER, bufferSizeInMilliseconds);
            Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_UPDATEPERIOD, 20);
        }

        public int CreateStream(string filePath)
        {
            CurrentStream = Bass.BASS_StreamCreateFile(filePath, 0, 0, BASSFlag.BASS_STREAM_DECODE);
            return CurrentStream;
        }

        public TimeSpan GetAudioDuration(string filePath)
        {
            int stream = CreateStream(filePath);
            if (stream == 0)
            {
                // No se pudo crear el stream
                return TimeSpan.Zero;
            }
            long lengthBytes = Bass.BASS_ChannelGetLength(stream);
            double lengthSeconds = Bass.BASS_ChannelBytes2Seconds(stream, lengthBytes);
            // No olvides liberar el stream después de obtener la duración
            Bass.BASS_StreamFree(stream);
            return TimeSpan.FromSeconds(lengthSeconds);
        }
    }
}
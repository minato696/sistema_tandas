using System;
using System.Windows.Forms;
using Un4seen.Bass;

namespace Generador_Pautas
{
    public class AudioPlayer
    {
        private int currentStream = 0;
        private Timer playbackTimer = new Timer();
        private long pausedPosition = 0;
        private bool isPaused = false;
        private double lastProgressPercentage = 0;
        public event EventHandler AudioFinished;
        public event EventHandler<double> AudioProgressUpdated;
        public int CurrentStream => currentStream;
        public bool IsPlaying { get; private set; } = false;

        public AudioPlayer()
        {
            playbackTimer.Interval = 100;
            playbackTimer.Tick += PlaybackTimer_Tick;
        }

        public void Play(string filePath)
        {
            Stop();

            currentStream = Bass.BASS_StreamCreateFile(filePath, 0, 0, BASSFlag.BASS_DEFAULT);

            if (currentStream != 0)
            {
                if (isPaused)
                {
                    Bass.BASS_ChannelSetPosition(currentStream, pausedPosition);
                    Bass.BASS_ChannelPlay(currentStream, false);
                    isPaused = false;
                }
                else
                {
                    Bass.BASS_ChannelPlay(currentStream, false);
                    IsPlaying = true;
                    playbackTimer.Start();
                }
            }
            else
            {
                MessageBox.Show("No se pudo reproducir el archivo de audio.");
            }
        }

        public void Pause()
        {
            if (currentStream != 0 && Bass.BASS_ChannelIsActive(currentStream) == BASSActive.BASS_ACTIVE_PLAYING)
            {
                pausedPosition = Bass.BASS_ChannelGetPosition(currentStream);
                Bass.BASS_ChannelPause(currentStream);
                isPaused = true;
                playbackTimer.Stop();
            }
        }

        public void Stop()
        {
            if (currentStream != 0)
            {
                Bass.BASS_ChannelStop(currentStream);
                Bass.BASS_StreamFree(currentStream);
                currentStream = 0;
                IsPlaying = false;
                playbackTimer.Stop();
                AudioFinished?.Invoke(this, EventArgs.Empty);
            }
        }

        public void PlaybackTimer_Tick(object sender, EventArgs e)
        {
            if (currentStream != 0)
            {
                long positionBytes = Bass.BASS_ChannelGetPosition(currentStream);
                double positionSeconds = Bass.BASS_ChannelBytes2Seconds(currentStream, positionBytes);
                long totalLengthBytes = Bass.BASS_ChannelGetLength(currentStream);
                double totalLengthSeconds = Bass.BASS_ChannelBytes2Seconds(currentStream, totalLengthBytes);
                double progressPercentage = (positionSeconds / totalLengthSeconds) * 100;

                if (progressPercentage >= 100 && progressPercentage == lastProgressPercentage)
                {
                    Stop();
                    return;
                }

                lastProgressPercentage = progressPercentage;
                AudioProgressUpdated?.Invoke(this, progressPercentage);
            }
        }

        public double GetTotalLengthSeconds()
        {
            long totalLengthBytes = Bass.BASS_ChannelGetLength(CurrentStream);
            double totalLengthSeconds = Bass.BASS_ChannelBytes2Seconds(CurrentStream, totalLengthBytes);
            return totalLengthSeconds;
        }

        public (uint leftLevel, uint rightLevel) UpdateAudioLevels()
        {
            // Obtén los niveles de audio de los canales izquierdo y derecho
            int levels = Bass.BASS_ChannelGetLevel(CurrentStream);
            short leftLevel = (short)(levels & 0xFFFF); // Nivel del canal izquierdo
            short rightLevel = (short)(levels >> 16);   // Nivel del canal derecho

            // Calcula los niveles de audio como porcentaje y devuelve los valores
            uint leftPercentage = (uint)((Math.Abs(Math.Max((short)0, leftLevel)) / 32768.0) * 100);
            uint rightPercentage = (uint)((Math.Abs(Math.Max((short)0, rightLevel)) / 32768.0) * 100);
            return (leftPercentage, rightPercentage);
        }



    }
}

using System;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using Mpv.NET.Player;

namespace livelywpf
{
    /// <summary>
    /// Interaction logic for MPVElement.xaml
    /// </summary>
    public partial class MPVElement : Window
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly MpvPlayer player;

        public MPVElement(string filePath)
        {
            InitializeComponent();
            try
            {
                this.Loaded += MediaPlayer_Loaded;
                player = new MpvPlayer(PlayerHost.Handle, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", "libMPVPlayer", "lib", "mpv-1.dll"))
                {
                    Loop = true,
                    Volume = 0,
                };
                player.MediaError += Player_MediaError;
                player.API.SetPropertyString("hwdec", "auto");
                player.API.SetPropertyString("keepaspect", "no");
                //Enable Windows screensaver
                player.API.SetPropertyString("stop-screensaver", "no");
                player.Load(filePath);
                player.Resume();
            }
            catch(Exception e)
            {
                Logger.Error("libMPV Init Failure:" + e.ToString());
            }
        }

        private void Player_MediaError(object sender, EventArgs e)
        {
            Logger.Error("libMPV Playback Failure:" + e.ToString());
            MessageBox.Show(Properties.Resources.LivelyExceptionMediaPlayback);
        }

        private void MediaPlayer_Loaded(object sender, RoutedEventArgs e)
        {
            //update window style.
            WindowOperations.RemoveWindowFromTaskbar(new WindowInteropHelper(this).Handle);
            //this hides the window from taskbar and also fixes crash when win10 taskview is launched. 
            this.ShowInTaskbar = false;
            this.ShowInTaskbar = true;
        }

        public void PausePlayer()
        {
            if(player != null)
            {
                player.Pause();
            }
        }

        public void PlayMedia()
        {
            if (player != null)
            {
                player.Resume();
            }
        }

        public void StopPlayer()
        {
            if (player != null)
            {
                player.Stop();
            }
        }

        public void SetVolume(int val)
        {
            if (player != null)
            {
                player.Volume = val;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if(player != null)
            {
                player.Dispose();
            }
        }
    }
}

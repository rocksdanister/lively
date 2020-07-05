using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
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
            this.Loaded += MediaPlayer_Loaded;

            player = new MpvPlayer(PlayerHost.Handle)
            {
                Loop = true,
                Volume = 100,
            };

            player.MediaError += Player_MediaError;
            player.API.SetPropertyString("hwdec", "auto");
            player.API.SetPropertyString("keepaspect", "no");
            player.Load(filePath);
            player.Resume();
        }

        private void Player_MediaError(object sender, EventArgs e)
        {
            Logger.Error(e.ToString());
        }

        private void MediaPlayer_Loaded(object sender, RoutedEventArgs e)
        {
            //ShowInTaskbar = false :- causing issue with windows10 Taskview.
            WindowOperations.RemoveWindowFromTaskbar(new WindowInteropHelper(this).Handle);
        }

        public void PausePlayer()
        {
            player.Pause();
        }

        public void PlayMedia()
        {
            player.Resume();
        }

        public void StopPlayer()
        {
            player.Stop();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            player.Dispose();
        }
    }
}

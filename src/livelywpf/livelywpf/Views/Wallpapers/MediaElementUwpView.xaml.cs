using Microsoft.Toolkit.Win32.UI.Controls.Interop.WinRT;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Windows.Media.Core;

namespace livelywpf.Views.Wallpapers
{
    /// <summary>
    /// Interaction logic for MediaElementUWP.xaml
    /// </summary>
    public partial class MediaElementUwpView : Window
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        public MediaElementUwpView(string path, int playSpeed)
        {
            InitializeComponent();
            this.Loaded += Window_Loaded;

            //https://github.com/windows-toolkit/Microsoft.Toolkit.Win32/issues/106  :'(
            Microsoft.Toolkit.Win32.UI.Controls.Interop.WinRT.MediaPlayer mediaPlayer = new Microsoft.Toolkit.Win32.UI.Controls.Interop.WinRT.MediaPlayer(new Windows.Media.Playback.MediaPlayer());
            mediaPlayer.Source = MediaSource.CreateFromUri(new Uri(path));
            mediaPlayer.AutoPlay = true;
            mediaPlayer.IsLoopingEnabled = true;

            mePlayer.Stretch = Microsoft.Toolkit.Win32.UI.Controls.Interop.WinRT.Stretch.Fill;
            mePlayer.SetMediaPlayer(mediaPlayer);

        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //ShowInTaskbar = false :- causing issue with windows10 Taskview.
            //SetupDesktop.RemoveWindowFromTaskbar(new WindowInteropHelper(this).Handle);
        }
    }
}

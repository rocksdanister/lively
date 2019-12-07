using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using WPFMediaKit.DirectShow.Controls;

namespace livelywpf
{
    
    /// <summary>
    /// Interaction logic for Mediakit.xaml
    /// </summary>
    public partial class Mediakit : Window
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        public Mediakit(string path)
        {
            InitializeComponent();

            mePlayer.LoadedBehavior = WPFMediaKit.DirectShow.MediaPlayers.MediaState.Manual;
            mePlayer.Source = new Uri(path);
            mePlayer.Stretch = SaveData.config.VideoScaler;
            mePlayer.MediaFailed += MePlayer_MediaFailed;
            mePlayer.MediaEnded += MePlayer_MediaEnded;
            mePlayer.MediaOpened += MePlayer_MediaOpened;
            mePlayer.Loop = true; //convenient!
            if (SaveData.config.MuteVideo || MainWindow.multiscreen)
                mePlayer.Volume = 0;
            else
                mePlayer.Volume = 1;

            mePlayer.Play();
        }

        private void MePlayer_MediaOpened(object sender, RoutedEventArgs e)
        {

        }

        public void MutePlayer(bool val)
        {
            if (val)
                mePlayer.Volume = 0;
            else
                mePlayer.Volume = 1;
        }

        public void PausePlayer()
        {
            mePlayer.Pause();
        }

        public void PlayMedia()
        {
            mePlayer.Play();
        }

        public void StopPlayer()
        {
            mePlayer.Stop();
        }

        private void MePlayer_MediaFailed(object sender, WPFMediaKit.DirectShow.MediaPlayers.MediaFailedEventArgs e)
        {
            Logger.Error("Mediakit Playback Failure: " + e.Exception.ToString());
            if (e.Exception.HResult != -2147467261) //nullreference error(when mediaload fails), otherwise double error message!.
            {
                System.Windows.MessageBox.Show(Properties.Resources.msgMediakitFailure + "\n\nError:\n" + e.Message, Properties.Resources.txtLivelyErrorMsgTitle);
            }

        }

        private void MePlayer_MediaEnded(object sender, RoutedEventArgs e)
        {
            //mePlayer.MediaPosition = 0;
            //mePlayer.Play();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            mePlayer.MediaFailed -= MePlayer_MediaFailed;
            mePlayer.MediaEnded -= MePlayer_MediaEnded;
            mePlayer.MediaOpened -= MePlayer_MediaOpened;

            mePlayer.Stop();
            mePlayer.Source = null;
            mePlayer.Close();
        }

        //prevent mouseclick focus steal(bottom-most wp rendering mode.
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var source = PresentationSource.FromVisual(this) as HwndSource;
            source.AddHook(WndProc);
        }
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_MOUSEACTIVATE)
            {
                handled = true;
                return new IntPtr(MA_NOACTIVATE);
            }
            else
            {
                return IntPtr.Zero;
            }
        }
        private const int WM_MOUSEACTIVATE = 0x0021;
        private const int MA_NOACTIVATE = 0x0003;
    }
}

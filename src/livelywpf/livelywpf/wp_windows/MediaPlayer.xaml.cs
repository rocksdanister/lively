using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
//using MahApps.Metro.Controls;

namespace livelywpf
{
    /// <summary>
    /// Interaction logic for MediaPlayer.xaml
    /// </summary>
    public partial class MediaPlayer : Window
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public MediaPlayer(string path, int playSpeed)
        {
            InitializeComponent();

            mePlayer.LoadedBehavior = MediaState.Manual;
            mePlayer.Source = new Uri(path);
            mePlayer.Stretch = SaveData.config.VideoScaler;
            mePlayer.MediaOpened += MePlayer_MediaOpened;
            mePlayer.MediaEnded += MePlayer_MediaEnded;
            mePlayer.MediaFailed += MePlayer_MediaFailed;

            mePlayer.SpeedRatio = playSpeed/100f; // 0<=x<=inf, default=1
            if (SaveData.config.MuteVideo || MainWindow.Multiscreen)
                mePlayer.Volume = 0;
            else
                mePlayer.Volume = 1;

            mePlayer.Play();
        }

        private void MePlayer_MediaOpened(object sender, RoutedEventArgs e)
        {

        }
        private void MePlayer_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            //todo proper error handling.
            Logger.Error("MediaFoundation Playback Failure:-" + e.ErrorException);

            if (App.W != null)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new ThreadStart(delegate
                {
                    App.W.WpfNotification(MainWindow.NotificationType.errorUrl, Properties.Resources.txtLivelyErrorMsgTitle, Properties.Resources.msgMediaFoundationFailure + "\n" + e.ErrorException, "https://github.com/rocksdanister/lively/wiki/Video-Guide");
                }));
            }
            else
            {
                MessageBox.Show(Properties.Resources.msgMediaFoundationFailure, Properties.Resources.txtLivelyErrorMsgTitle);
            }
        }

        public void SetPlayBackSpeed(int percent)
        {
            mePlayer.SpeedRatio = percent;
        }

        public void MutePlayer( bool isMute)
        {
            if (isMute)
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

        //credit: https://stackoverflow.com/questions/4338951/how-do-i-determine-if-mediaelement-is-playing/4341285
        private static MediaState GetMediaState(MediaElement myMedia)
        {
            FieldInfo hlp = typeof(MediaElement).GetField("_helper", BindingFlags.NonPublic | BindingFlags.Instance);
            object helperObject = hlp.GetValue(myMedia);
            FieldInfo stateField = helperObject.GetType().GetField("_currentState", BindingFlags.NonPublic | BindingFlags.Instance);
            MediaState state = (MediaState)stateField.GetValue(helperObject);
            return state;
        }

        private void MePlayer_MediaEnded(object sender, RoutedEventArgs e)
        {
            //mePlayer.Stop(); //fix for GetMediaState()
            mePlayer.Position = TimeSpan.Zero;//new TimeSpan(0, 0, 0, 1);
            mePlayer.Play();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            mePlayer.MediaOpened -= MePlayer_MediaOpened;
            mePlayer.MediaEnded -= MePlayer_MediaEnded;
            mePlayer.MediaFailed -= MePlayer_MediaFailed;

            mePlayer.Stop();
            mePlayer.Source = null;
            mePlayer.Close();
        }

        //prevent mouseclick focus steal(bottom-most wp rendering mode).
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

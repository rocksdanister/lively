using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace livelywpf
{
    /// <summary>
    /// MediaElement video player (old, compatible with win7 onwards.)
    /// </summary>
    public partial class MediaElementWPF : Window
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public MediaElementWPF(string filePath, int playSpeed)
        {
            InitializeComponent();
            this.Loaded += MediaPlayer_Loaded;

            mePlayer.LoadedBehavior = MediaState.Manual;
            mePlayer.Source = new Uri(filePath);
            mePlayer.Stretch = Stretch.Fill;//SaveData.config.VideoScaler;
            mePlayer.MediaOpened += MePlayer_MediaOpened;
            mePlayer.MediaEnded += MePlayer_MediaEnded;
            mePlayer.MediaFailed += MePlayer_MediaFailed;

            mePlayer.SpeedRatio = playSpeed / 100f; // 0<=x<=inf, default=1
            /*
            if (SaveData.config.MuteVideo || MainWindow.Multiscreen)
                mePlayer.Volume = 0;
            else
                mePlayer.Volume = 1;
            */
            mePlayer.Play();
        }


        private void MediaPlayer_Loaded(object sender, RoutedEventArgs e)
        {
            //ShowInTaskbar = false :- causing issue with windows10 Taskview.
            //SetupDesktop.RemoveWindowFromTaskbar(new WindowInteropHelper(this).Handle);
        }

        private void MePlayer_MediaOpened(object sender, RoutedEventArgs e)
        {

        }
        private void MePlayer_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            //todo proper error handling.
            Logger.Error("MediaFoundation Playback Failure:-" + e.ErrorException);
            /*
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
            */
        }

        public void SetPlayBackSpeed(int percent)
        {
            mePlayer.SpeedRatio = percent;
        }

        public void MutePlayer(bool isMute)
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
    }
}

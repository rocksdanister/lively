using livelywpf.Helpers;
using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;

namespace livelywpf.Views.Wallpapers
{
    /// <summary>
    /// MediaElement video player (old, compatible with win7 onwards.)
    /// </summary>
    public partial class MediaElementView : Window
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public MediaElementView(string filePath, WallpaperScaler scaler = WallpaperScaler.fill)
        {
            InitializeComponent();
            this.Loaded += MediaPlayer_Loaded;

            mePlayer.LoadedBehavior = MediaState.Manual;
            mePlayer.Source = new Uri(filePath);
            mePlayer.Stretch = (Stretch)scaler;
            //mePlayer.MediaOpened += MePlayer_MediaOpened;
            mePlayer.MediaEnded += MePlayer_MediaEnded;
            mePlayer.MediaFailed += MePlayer_MediaFailed;
            mePlayer.Volume = 0;
            mePlayer.Play();
        }


        private void MediaPlayer_Loaded(object sender, RoutedEventArgs e)
        {
            //ShowInTaskbar = false :- causing issue with windows10 Taskview.
            WindowOperations.RemoveWindowFromTaskbar(new WindowInteropHelper(this).Handle);
            //this hides the window from taskbar and also fixes crash when win10 taskview is launched. 
            this.ShowInTaskbar = false;
            this.ShowInTaskbar = true;
        }

        private void MePlayer_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            Logger.Error("MediaFoundation Playback Failure:" + e.ErrorException);
            MessageBox.Show(Properties.Resources.LivelyExceptionMediaPlayback);   
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

        public void SetVolume(int val)
        {
            mePlayer.Volume = (double)val/100;
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
            //mePlayer.MediaOpened -= MePlayer_MediaOpened;
            mePlayer.MediaEnded -= MePlayer_MediaEnded;
            mePlayer.MediaFailed -= MePlayer_MediaFailed;

            mePlayer.Stop();
            mePlayer.Source = null;
            mePlayer.Close();
        }
    }
}

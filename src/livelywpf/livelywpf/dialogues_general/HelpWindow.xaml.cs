using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace livelywpf.Dialogues
{
    /// <summary>
    /// Interaction logic for HelpWindow.xaml
    /// </summary>
    public partial class HelpWindow : MetroWindow
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public HelpWindow(string videoPath)
        {
            InitializeComponent();

            mePlayer.LoadedBehavior = MediaState.Manual;
            if (System.IO.File.Exists(videoPath))
            {
                mePlayer.Source = new Uri(videoPath);
                mePlayer.Stretch = Stretch.Uniform;
                mePlayer.MediaEnded += MePlayer_MediaEnded;
                //mePlayer.MediaOpened += MePlayer_MediaOpened; 
                mePlayer.MediaFailed += MePlayer_MediaFailed;
                mePlayer.Volume = 0;

                mePlayer.Play();
            }
            
            changelogtext.Text = "What's new in Lively v" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
        }

        private void PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            Dialogues.Changelog changelogWindow = new Dialogues.Changelog
            {
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ShowActivated = true
            };
            changelogWindow.ShowDialog();
        }

        private void MePlayer_MediaOpened(object sender, RoutedEventArgs e)
        {

        }

        //license file hyperlink
        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(e.Uri.AbsoluteUri);
        }

        private void MePlayer_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            Logger.Error("MediaFoundation Playback Failure:-" + e.ToString());
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

        private void MePlayer_MediaEnded(object sender, RoutedEventArgs e)
        {
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

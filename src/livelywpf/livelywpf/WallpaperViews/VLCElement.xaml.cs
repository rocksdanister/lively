using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using LibVLCSharp.Shared;

namespace livelywpf
{
    /// <summary>
    /// Interaction logic for VLCElement.xaml
    /// </summary>
    public partial class VLCElement : Window
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        bool _mediaReady = false;
        LibVLC _libVLC;
        MediaPlayer _mediaPlayer;
        Media _media;
        string _filePath;
        bool _isStream;
        //float vidPosition;

        //todo:https://code.videolan.org/videolan/LibVLCSharp/-/issues/136
        //take screenshot and display static image when player.Stop() is called.
        public VLCElement(string filePath, bool isStream = false)
        {
            InitializeComponent();
            videoView.Loaded += VideoView_Loaded;
            _filePath = filePath;
            _isStream = isStream;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //update window style.
            WindowOperations.RemoveWindowFromTaskbar(new WindowInteropHelper(this).Handle);
            //this hides the window from taskbar and also fixes crash when win10 taskview is launched. 
            this.ShowInTaskbar = false;
            this.ShowInTaskbar = true;
        }

        async void VideoView_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                LibVLCSharp.Shared.Core.Initialize(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", "libVLCPlayer", "libvlc", "win-x86"));

                //flags: 
                //"--no-disable-screensaver" : enable monitor sleep.
                //ref: https://wiki.videolan.org/VLC_command-line_help
                _libVLC = new LibVLC("--no-disable-screensaver");
                _mediaPlayer = new MediaPlayer(_libVLC)
                {
                    AspectRatio = "Fill",
                    EnableHardwareDecoding = true,
                    Volume = 0
                };
                _mediaPlayer.EndReached += _mediaPlayer_EndReached;
                _mediaPlayer.EncounteredError += _mediaPlayer_EncounteredError;
                videoView.MediaPlayer = _mediaPlayer;

                if (_isStream)
                {
                    //ref: https://code.videolan.org/videolan/LibVLCSharp/-/issues/156#note_35657
                    _media = new Media(_libVLC, _filePath, FromType.FromLocation);
                    await _media.Parse(MediaParseOptions.ParseNetwork);
                    _mediaPlayer.Play(_media.SubItems.First());
                }
                else
                {
                    _media = new Media(_libVLC, _filePath, FromType.FromPath);
                    _mediaPlayer.Play(_media);
                }
                _mediaReady = true;
            }
            catch(Exception ex)
            {
                Logger.Error("libVLC Init Failure:" + ex.ToString());
            }

        }

        private void _mediaPlayer_EncounteredError(object sender, EventArgs e)
        {
            Logger.Error("libVLC Playback Failure:" + e.ToString());
            MessageBox.Show(Properties.Resources.LivelyExceptionMediaPlayback);
        }

        private void _mediaPlayer_EndReached(object sender, EventArgs e)
        {
            if(_isStream)
            {
                ThreadPool.QueueUserWorkItem(_ => _mediaPlayer.Play(_media.SubItems.First()));
            }
            else
            {
                ThreadPool.QueueUserWorkItem(_ => _mediaPlayer.Play(_media));
            }
        }

        public void PausePlayer()
        {
            if (_mediaPlayer.IsPlaying && _mediaReady)
            {
                //vidPosition = _mediaPlayer.Position;
                //_mediaPlayer.Stop();
                _mediaPlayer.Pause();
            }
        }

        public void PlayMedia()
        {
            if (_mediaReady && !_mediaPlayer.IsPlaying)
            {
                _mediaPlayer.Play();
                //_mediaPlayer.Position = vidPosition;
            }
        }


        public void StopPlayer()
        {
            if (_mediaReady)
            {
                _mediaPlayer.Stop();
            }
        }

        public void SetVolume(int val)
        {
            if (_mediaReady)
            {
                _mediaPlayer.Volume = val;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                _mediaReady = false;
                _mediaPlayer.EndReached -= _mediaPlayer_EndReached;
                _mediaPlayer.Dispose();
                _libVLC.Dispose();
                _media.Dispose();
            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString());
            }
        }
    }
}

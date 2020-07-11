using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using LibVLCSharp.Shared;

namespace livelywpf
{
    /// <summary>
    /// Interaction logic for VLCElement.xaml
    /// </summary>
    public partial class VLCElement : Window
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        bool _coreInitialized = false;
        bool _isNetworkFile = false;
        LibVLC _libVLC;
        MediaPlayer _mediaPlayer;
        Media _media;
        string _filePath;

        public VLCElement(string filePath)
        {
            InitializeComponent();
            videoView.Loaded += VideoView_Loaded;
            _filePath = filePath;
        }

        async void VideoView_Loaded(object sender, RoutedEventArgs e)
        {
            LibVLCSharp.Shared.Core.Initialize();

            _libVLC = new LibVLC();
            _mediaPlayer = new MediaPlayer(_libVLC)
            {
                AspectRatio = "Fill",
                EnableHardwareDecoding = true
            };
            _mediaPlayer.EndReached += _mediaPlayer_EndReached;
            videoView.MediaPlayer = _mediaPlayer;

            if (_filePath.Contains("youtube.com/watch?v="))
            {
                //ref: https://code.videolan.org/videolan/LibVLCSharp/-/issues/156#note_35657
                _media = new Media(_libVLC, _filePath, FromType.FromLocation);
                await _media.Parse(MediaParseOptions.ParseNetwork);
                _mediaPlayer.Play(_media.SubItems.First());
                _isNetworkFile = true;
            }
            else
            {
                _media = new Media(_libVLC, _filePath, FromType.FromPath);
                _mediaPlayer.Play(_media);
            }
            _coreInitialized = true;
        }

        private void _mediaPlayer_EndReached(object sender, EventArgs e)
        {
            if(_isNetworkFile)
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
            if (_mediaPlayer.IsPlaying && _coreInitialized)
            {
                ThreadPool.QueueUserWorkItem(_ => _mediaPlayer.Pause());
            }
        }

        public void PlayMedia()
        {
            if (_coreInitialized)
            {
                ThreadPool.QueueUserWorkItem(_ => _mediaPlayer.Play());
            }
        }

        public void StopPlayer()
        {
            if (_coreInitialized)
            {
                ThreadPool.QueueUserWorkItem(_ => _mediaPlayer.Stop());
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //todo: somethings off..corrupt memory error on close.
            _mediaPlayer.EndReached -= _mediaPlayer_EndReached;
            ThreadPool.QueueUserWorkItem(_ => _mediaPlayer.Stop());
            _mediaPlayer.Dispose();
            _libVLC.Dispose();
            _media.Dispose();
        }
    }
}

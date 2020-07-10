using System;
using System.IO;
using System.Threading;
using System.Windows;
using LibVLCSharp.Shared;

namespace livelywpf
{
    /// <summary>
    /// Interaction logic for VLCElement.xaml
    /// </summary>
    public partial class VLCElement : Window
    {
        bool _coreInitialized = false;
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

        void VideoView_Loaded(object sender, RoutedEventArgs e)
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
            _media = new Media(_libVLC, new Uri(_filePath));
            _mediaPlayer.Play(_media);
            _coreInitialized = true;
        }

        private void _mediaPlayer_EndReached(object sender, EventArgs e)
        {
            ThreadPool.QueueUserWorkItem(_ => _mediaPlayer.Play(_media));
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
            _mediaPlayer.Dispose();
            _libVLC.Dispose();
            _media.Dispose();
        }
    }
}

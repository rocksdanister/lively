using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using LibVLCSharp.Shared;

namespace libVLCPlayer
{
    /// <summary>
    /// lively libvlc videoplayer (External.)
    /// </summary>
    public partial class MainWindow : Window
    {
        bool _mediaReady = false;
        LibVLC _libVLC;
        MediaPlayer _mediaPlayer;
        Media _media;
        string _filePath;
        bool _isStream;
        float vidPosition;

        //todo:https://code.videolan.org/videolan/LibVLCSharp/-/issues/136
        //take screenshot and display static image when player.Stop() is called.
        public MainWindow(string[] args)
        {
            InitializeComponent();
            _filePath = args[0];
            _isStream = false;
            videoView.Loaded += VideoView_Loaded;
        }

        async void VideoView_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                //DLL load hangs thread.
                LibVLCSharp.Shared.Core.Initialize();
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
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                ListenToParent();
            }
        }

        private void _mediaPlayer_EndReached(object sender, EventArgs e)
        {
            if (_mediaPlayer == null)
                return;

            if (_isStream)
            {
                ThreadPool.QueueUserWorkItem(_ => _mediaPlayer.Play(_media.SubItems.First()));
            }
            else
            {
                ThreadPool.QueueUserWorkItem(_ => _mediaPlayer.Play(_media));
            }
        }

        private void PausePlayer()
        {
            if (_mediaPlayer == null)
                return;

            if (_mediaPlayer.IsPlaying && _mediaReady)
            {
                vidPosition = _mediaPlayer.Position;
                _mediaPlayer.Stop();
            }
        }

        private void PlayMedia()
        {
            if (_mediaPlayer == null)
                return;

            if (_mediaReady && !_mediaPlayer.IsPlaying)
            {
                _mediaPlayer.Play();
                _mediaPlayer.Position = vidPosition;
            }
        }

        private void StopPlayer()
        {
            if (_mediaPlayer == null)
                return;

            if (_mediaReady)
            {
                _mediaPlayer.Stop();
            }
        }

        private void SetVolume(int volume)
        {
            if(_mediaReady)
            {
                _mediaPlayer.Volume = volume;
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
            catch { }
        }

        private void _mediaPlayer_EncounteredError(object sender, EventArgs e)
        {
            Console.WriteLine("Media playback Error");
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            IntPtr handle = new WindowInteropHelper(this).Handle;
            var styleNewWindowExtended =
                           (Int64)WS_EX_NOACTIVATE |
                           (Int64)WS_EX_TOOLWINDOW;

            // update window styles
            SetWindowLongPtr(new HandleRef(null, handle), (-20), (IntPtr)styleNewWindowExtended);
            //this hides the window from taskbar and also fixes crash when win10 taskview is launched. 
            this.ShowInTaskbar = false;
            this.ShowInTaskbar = true;

            //passing handle to lively.
            Console.WriteLine("HWND" + handle);
        }

        /// <summary>
        /// std I/O redirect, used to communicate with lively. 
        /// </summary>
        public async void ListenToParent()
        {
            try
            {
                await Task.Run(async () =>
                {
                    // Loop runs only once per line received
                    while (true) 
                    {
                        string text = await Console.In.ReadLineAsync();
                        if (string.IsNullOrEmpty(text))
                        {
                            //When the redirected stream is closed, a null line is sent to the event handler. 
                            break;
                        }
                        else if (text.Equals("lively:vid-pause", StringComparison.OrdinalIgnoreCase))
                        {
                            PausePlayer();
                        }
                        else if (text.Equals("lively:vid-play", StringComparison.OrdinalIgnoreCase))
                        {
                            PlayMedia();
                        }
                        else if (text.Equals("lively:terminate", StringComparison.OrdinalIgnoreCase))
                        {
                            break;
                        }
                        else if (text.Contains("lively:vid-volume", StringComparison.OrdinalIgnoreCase))
                        {
                            var msg = text.Split(' ');
                            if (msg.Length < 2)
                                continue;

                            if (int.TryParse(msg[1], out int value))
                            {
                                SetVolume(value);
                            }
                        }
                    }
                });
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            finally
            {
                Application.Current.Shutdown();
            }
        }

        #region helpers

        [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
        private static extern int SetWindowLong32(HandleRef hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
        private static extern IntPtr SetWindowLongPtr64(HandleRef hWnd, int nIndex, IntPtr dwNewLong);
        private const uint WS_EX_NOACTIVATE = 0x08000000;
        private const uint WS_EX_TOOLWINDOW = 0x00000080;
        // This helper static method is required because the 32-bit version of user32.dll does not contain this API
        // (on any versions of Windows), so linking the method will fail at run-time. The bridge dispatches the request
        // to the correct function (GetWindowLong in 32-bit mode and GetWindowLongPtr in 64-bit mode)
        private static IntPtr SetWindowLongPtr(HandleRef hWnd, int nIndex, IntPtr dwNewLong)
        {

            if (IntPtr.Size == 8)
                return SetWindowLongPtr64(hWnd, nIndex, dwNewLong);
            else
                return new IntPtr(SetWindowLong32(hWnd, nIndex, dwNewLong.ToInt32()));

        }

        #endregion //helpers
    }
}

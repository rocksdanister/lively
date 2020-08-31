using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
//using System.Windows.Forms;
using System.Windows.Interop;
using CommandLine;
using Mpv.NET.Player;

namespace libMPVPlayer
{
    /// <summary>
    /// lively libmpv videoplayer (External.)
    /// Instructions on setting up: https://github.com/hudec117/Mpv.NET-lib-
    /// </summary>
    public partial class MainWindow : Window
    {
        private MpvPlayer player;
        public MainWindow(string[] args)
        {
            InitializeComponent();
            CommandLine.Parser.Default.ParseArguments<Options>(args)
            .WithParsed(RunOptions)
            .WithNotParsed(HandleParseError);
        }

        #region cmdline

        class Options
        {
            [Option("path",
            Required = true,
            HelpText = "The file/video stream path.")]
            public string FilePath { get; set; }

            [Option("stream",
            Required = false,
            Default = 0,
            HelpText = "ytdl stream quality.")]
            public int StreamQuality { get; set; }
        }

        private void RunOptions(Options opts)
        {
            try
            {
                player = new MpvPlayer(PlayerHost.Handle)
                {
                    Loop = true,
                    Volume = 100,
                };
                player.MediaError += Player_MediaError1;
                //use gpu decoding if preferable.
                player.API.SetPropertyString("hwdec", "auto");
                player.API.SetPropertyString("keepaspect", "no");
                //Enable Windows screensaver.
                player.API.SetPropertyString("stop-screensaver", "no");
                //ytdl.
                player.EnableYouTubeDl();
                YouTubeDlVideoQuality quality = YouTubeDlVideoQuality.Highest;
                try 
                {
                    quality = (YouTubeDlVideoQuality)(Enum.GetValues(typeof(YouTubeDlVideoQuality))).GetValue(opts.StreamQuality);
                }
                catch { }
                player.YouTubeDlVideoQuality = quality;
                //stream/file.
                player.Load(opts.FilePath);
                player.Resume();
            }
            catch (Exception)
            {
                //todo: pass msg to parent process.
            }
            finally
            {
                ListenToParent();
            }
        }

        private void HandleParseError(IEnumerable<Error> errs)
        {
            //todo: pass msg to parent process.
            Application.Current.Shutdown();
        }

        #endregion //cmdline

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            IntPtr handle = new WindowInteropHelper(this).Handle;
            var styleNewWindowExtended =
                           (Int64)WS_EX_NOACTIVATE |
                           (Int64)WS_EX_TOOLWINDOW;

            //update window styles
            SetWindowLongPtr(new HandleRef(null, handle), (-20), (IntPtr)styleNewWindowExtended);
            //this hides the window from taskbar and also fixes crash when win10 taskview is launched. 
            this.ShowInTaskbar = false;
            this.ShowInTaskbar = true;

            //passing handle to lively.
            Console.WriteLine("HWND" + handle);
        }

        private void Player_MediaError1(object sender, System.EventArgs e)
        {
            //todo: pass msg to parent process.
        }

        public void PausePlayer()
        {
            if(player != null)
            {
                player.Pause();
            }
        }

        public void PlayMedia()
        {
            if (player != null)
            {
                player.Resume();
            }
        }

        public void StopPlayer()
        {
            if (player != null)
            {
                player.Stop();
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (player != null)
            {
                player.Dispose();
            }
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
                        if (String.Equals(text, "lively:vid-pause", StringComparison.OrdinalIgnoreCase))
                        {
                            PausePlayer();
                        }
                        else if (String.Equals(text, "lively:vid-play", StringComparison.OrdinalIgnoreCase))
                        {
                            PlayMedia();
                        }
                        else if (String.Equals(text, "lively:terminate", StringComparison.OrdinalIgnoreCase))
                        {
                            break;
                        }
                    }
                });
                Application.Current.Shutdown();
            }
            catch { }
        }

        #region pinvoke

        [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
        public static extern int SetWindowLong32(HandleRef hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
        public static extern IntPtr SetWindowLongPtr64(HandleRef hWnd, int nIndex, IntPtr dwNewLong);
        private const uint WS_EX_NOACTIVATE = 0x08000000;
        private const uint WS_EX_TOOLWINDOW = 0x00000080;
        // This helper static method is required because the 32-bit version of user32.dll does not contain this API
        // (on any versions of Windows), so linking the method will fail at run-time. The bridge dispatches the request
        // to the correct function (GetWindowLong in 32-bit mode and GetWindowLongPtr in 64-bit mode)
        public static IntPtr SetWindowLongPtr(HandleRef hWnd, int nIndex, IntPtr dwNewLong)
        {

            if (IntPtr.Size == 8)
                return SetWindowLongPtr64(hWnd, nIndex, dwNewLong);
            else
                return new IntPtr(SetWindowLong32(hWnd, nIndex, dwNewLong.ToInt32()));

        }

        #endregion //pinvoke
    }
}

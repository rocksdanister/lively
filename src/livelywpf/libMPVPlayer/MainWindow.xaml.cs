using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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

            [Option("stretch",
            Required = false,
            Default = 0,
            HelpText = "Video Scaling algorithm.")]
            public int StretchMode { get; set; }

            [Option("datadir",
            Required = false,
            HelpText = "App data directory")]
            public string AppDataDir { get; set; }
        }

        private void RunOptions(Options opts)
        {
            try
            {
                player = new MpvPlayer(PlayerHost.Handle)
                {
                    Loop = true,
                    Volume = 0,          
                };
                player.MediaError += Player_MediaError1;
                if (File.Exists(Path.Combine(opts.AppDataDir, "mpv", "mpv.conf")))
                {
                    Console.WriteLine("Init custom mpv.conf");
                    player.API.LoadConfigFile(Path.Combine(opts.AppDataDir, "mpv", "mpv.conf"));
                }
                else
                {
                    //flags ref: https://mpv.io/manual/master/
                    //use gpu decoding if preferable.
                    player.API.SetPropertyString("hwdec", "auto");
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
                    //video scaling.
                    System.Windows.Media.Stretch stretch = (System.Windows.Media.Stretch)opts.StretchMode;
                    switch (stretch)
                    {
                        //I think these are the mpv equivalent scaler settings.
                        case System.Windows.Media.Stretch.None:
                            player.API.SetPropertyString("video-unscaled", "yes");
                            break;
                        case System.Windows.Media.Stretch.Fill:
                            player.API.SetPropertyString("keepaspect", "no");
                            break;
                        case System.Windows.Media.Stretch.Uniform:
                            player.API.SetPropertyString("keepaspect", "yes");
                            break;
                        case System.Windows.Media.Stretch.UniformToFill:
                            player.API.SetPropertyString("panscan", "1.0");
                            break;
                        default:
                            player.API.SetPropertyString("keepaspect", "no");
                            break;
                    }
                }
                //stream/file.
                player.Load(opts.FilePath);
                player.Resume();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            finally
            {
                ListenToParent();
            }
        }

        private void HandleParseError(IEnumerable<Error> errs)
        {
            Console.WriteLine("Error parsing cmdline args, Exiting!");
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
            Console.WriteLine("Media playback Error");
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

        private void SetVolume(int volume)
        {
            if (player != null)
            {
                player.Volume = volume;
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
                        if(String.IsNullOrEmpty(text))
                        {
                            //When the redirected stream is closed, a null line is sent to the event handler. 
                            break;
                        }
                        else if (String.Equals(text, "lively:vid-pause", StringComparison.OrdinalIgnoreCase))
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
                        else if (Contains(text, "lively:vid-volume", StringComparison.OrdinalIgnoreCase))
                        {
                            var msg = text.Split(' ');
                            if (msg.Length < 2)
                                continue;

                            if (int.TryParse(msg[1], out int value))
                            {
                                SetVolume(value);
                            }
                        }
                        else if (Contains(text, "lively:customise", StringComparison.OrdinalIgnoreCase))
                        {
                            try
                            {
                                LivelyPropertiesMsg(text);
                            }
                            catch
                            {
                                //todo: logging.
                            }
                        }
                    }
                });
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }
            finally
            {
                Application.Current.Shutdown();
            }
        }

        private void LivelyPropertiesMsg(string val)
        {
            // TODO: 
            // Having trouble passing double without decimal to SetPropertyDouble
            // Load from json file for initial start.
            // Implement restore.
            // Test and see if what all Lively controls are required based on available options: https://mpv.io/manual/master/
            // Maybe block some commands? like a blacklist

            var msg = val.Split(' ');
            if (msg.Length < 4)
                return;

            string uiElementType = msg[1];
            if (uiElementType.Equals("dropdown", StringComparison.OrdinalIgnoreCase))
            {
                if (int.TryParse(msg[3], out int value))
                {
                    player.API.SetPropertyString(msg[2], msg[3]);
                }
            }
            else if (uiElementType.Equals("slider", StringComparison.OrdinalIgnoreCase))
            {
                if (double.TryParse(msg[3], out double value))
                {
                    if(player != null)
                    {
                        player.API.SetPropertyString(msg[2], msg[3]);
                    }
                }
            }
            else if (uiElementType.Equals("checkbox", StringComparison.OrdinalIgnoreCase))
            {
                if (bool.TryParse(msg[3], out bool value))
                {
                    player.API.SetPropertyString(msg[2], msg[3]);
                }
            }
            else if (uiElementType.Equals("textbox", StringComparison.OrdinalIgnoreCase))
            {
                var sIndex = val.IndexOf("\"") + 1;
                var lIndex = val.LastIndexOf("\"") - 1;
                player.API.SetPropertyString(msg[2], val.Substring(sIndex, lIndex - sIndex + 1));
            }
            /*
            //I don't think these control is needed.
            else if (uiElementType.Equals("color", StringComparison.OrdinalIgnoreCase))
            {
                player.API.SetPropertyString(msg[2], msg[3]);
            }
            else if (uiElementType.Equals("button", StringComparison.OrdinalIgnoreCase))
            {
                if (msg[2].Equals("lively_default_settings_reload", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        //load new file.
                        WidgetData.LoadLivelyProperties(livelyPropertyPath);
                        //restore new property values.
                        RestoreLivelyPropertySettings();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.ToString(), "Lively Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    Form1.chromeBrowser.ExecuteScriptAsync("livelyPropertyListener", msg[2], true);
                }
            }
            else if (uiElementType.Equals("folderDropdown", StringComparison.OrdinalIgnoreCase))
            {
                var sIndex = val.IndexOf("\"") + 1;
                var lIndex = val.LastIndexOf("\"") - 1;
                var filePath = Path.Combine(Path.GetDirectoryName(htmlPath), val.Substring(sIndex, lIndex - sIndex + 1));
                if (File.Exists(filePath))
                {
                    Form1.chromeBrowser.ExecuteScriptAsync("livelyPropertyListener",
                        msg[2],
                        val.Substring(sIndex, lIndex - sIndex + 1));
                }
                else
                {
                    Form1.chromeBrowser.ExecuteScriptAsync("livelyPropertyListener",
                        msg[2],
                        null); //or custom msg
                }
            }
            */
        }

        #region helpers

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

        /// <summary>
        /// String Contains method with StringComparison property.
        /// </summary>
        /// <param name="str"></param>
        /// <param name="substring"></param>
        /// <param name="comp"></param>
        /// <returns></returns>
        private static bool Contains(String str, String substring,
                                    StringComparison comp)
        {
            if (substring == null | str == null)
                throw new ArgumentNullException("string",
                                             "substring/string cannot be null.");
            else if (!Enum.IsDefined(typeof(StringComparison), comp))
                throw new ArgumentException("comp is not a member of StringComparison",
                                         "comp");

            return str.IndexOf(substring, comp) >= 0;
        }

        #endregion //helpers
    }
}

using CommandLine;
using Lively.Common.Helpers;
using Lively.Common.JsonConverters;
using Lively.Models.Message;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;

namespace Lively.Player.Wmf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow(string[] args)
        {
            InitializeComponent();
            Parser.Default.ParseArguments<StartArgs>(args)
                .WithParsed(RunOptions)
                .WithNotParsed(HandleParseError);
        }

        private void RunOptions(StartArgs opts)
        {
            try
            {
                mePlayer.LoadedBehavior = MediaState.Manual;
                mePlayer.Source = new Uri(opts.FilePath);
                mePlayer.Stretch = (Stretch)opts.StretchMode;
                mePlayer.MediaEnded += MePlayer_MediaEnded;
                mePlayer.MediaFailed += MePlayer_MediaFailed;
                mePlayer.MediaOpened += (s, e) => App.WriteToParent(new LivelyMessageWallpaperLoaded() { Success = true });
                this.Closing += (s, e) => mePlayer.Close();
                this.Loaded += MediaPlayer_Loaded;
                SetVolume(opts.Volume);
                mePlayer.Play();
            }
            catch (Exception e)
            {
                App.WriteToParent(new LivelyMessageConsole()
                {
                    Category = ConsoleMessageType.error,
                    Message = $"Initialziation failed: {e.Message}",
                });
            }
            finally
            {
                _ = StdInListener();
            }
        }

        private void HandleParseError(IEnumerable<Error> errs)
        {
            App.WriteToParent(new LivelyMessageConsole()
            {
                Category = ConsoleMessageType.error,
                Message = $"Error parsing cmdline args: {errs.First()}",
            });
            Application.Current.Shutdown();
        }

        private void MediaPlayer_Loaded(object sender, RoutedEventArgs e)
        {
            IntPtr handle = new WindowInteropHelper(this).Handle;
            //ShowInTaskbar = false : causing issue with windows10 Taskview.
            WindowUtil.RemoveWindowFromTaskbar(handle);
            //this hides the window from taskbar and also fixes crash when win10 taskview is launched. 
            this.ShowInTaskbar = false;
            this.ShowInTaskbar = true;

            //Passing hwnd to core.
            App.WriteToParent(new LivelyMessageHwnd()
            {
                Hwnd = handle.ToInt32(),
            });
        }

        private void MePlayer_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            App.WriteToParent(new LivelyMessageConsole()
            {
                Category = ConsoleMessageType.error,
                Message = "Media playback failure:" + e.ErrorException.Message,
            });
        }

        private void MePlayer_MediaEnded(object sender, RoutedEventArgs e)
        {
            mePlayer.Position = TimeSpan.Zero;
            mePlayer.Play();
        }

        public void Pause()
        {
            mePlayer.Pause();
        }

        public void Play()
        {
            mePlayer.Play();
        }

        public void Stop()
        {
            mePlayer.Stop();
        }

        public void SetVolume(int volume)
        {
            mePlayer.Volume = (double)volume / 100;
        }

        public async Task StdInListener()
        {
            try
            {
                await Task.Run(async () =>
                {
                    while (true)
                    {
                        var msg = await Console.In.ReadLineAsync();
                        if (string.IsNullOrEmpty(msg))
                        {
                            //When the redirected stream is closed, a null line is sent to the event handler. 
                            break;
                        }
                        else
                        {
                            try
                            {
                                var close = false;
                                var obj = JsonConvert.DeserializeObject<IpcMessage>(msg, new JsonSerializerSettings() { Converters = { new IpcMessageConverter() } });
                                this.Dispatcher.Invoke(() =>
                                {
                                    switch (obj.Type)
                                    {
                                        case MessageType.cmd_suspend:
                                            Pause();
                                            break;
                                        case MessageType.cmd_resume:
                                            Play();
                                            break;
                                        case MessageType.cmd_close:
                                            close = true;
                                            break;
                                        case MessageType.cmd_volume:
                                            var vc = (LivelyVolumeCmd)obj;
                                            SetVolume(vc.Volume);
                                            break;
                                    }
                                });

                                if (close)
                                {
                                    break;
                                }
                            }
                            catch (Exception ie)
                            {
                                App.WriteToParent(new LivelyMessageConsole()
                                {
                                    Category = ConsoleMessageType.error,
                                    Message = $"Ipc action error: {ie.Message}"
                                });
                            }
                        }
                    }
                });
            }
            catch (Exception e)
            {
                App.WriteToParent(new LivelyMessageConsole()
                {
                    Category = ConsoleMessageType.error,
                    Message = $"Ipc stdin error: {e.Message}",
                });
            }
            finally
            {
                Application.Current.Dispatcher.Invoke(Application.Current.Shutdown);
            }
        }
    }
}

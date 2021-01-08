using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using livelywpf.Core;

namespace livelywpf.Views
{
    /// <summary>
    /// Interaction logic for WallpaperPreviewWindow.xaml
    /// </summary>
    public partial class WallpaperPreviewWindow : Window
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        //wallpaper loader.
        private readonly LibraryModel wallpaperData;
        private IWallpaper wallpaper = null;
        private bool _initializedWallpaper = false;

        //video capture
        private DispatcherTimer dispatcherTimer;
        private bool _recording = false;
        private Helpers.IScreenRecorder recorder;
        private int elapsedTime;

        #region wallpaper loader

        public WallpaperPreviewWindow(LibraryModel wp)
        {
            InitializeComponent();
            this.SizeChanged += WallpaperPreviewWindow_SizeChanged;
            this.wallpaperData = wp;
        }

        private void WallpaperPreviewWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if(wallpaper != null)
            {
                var item = WindowOperations.GetAbsolutePlacement(PreviewBorder, true);
                NativeMethods.POINT pts = new NativeMethods.POINT() { X = (int)item.Left, Y = (int)item.Top };
                if (NativeMethods.ScreenToClient(new WindowInteropHelper(this).Handle, ref pts))
                {
                    NativeMethods.SetWindowPos(wallpaper.GetHWND(), 1, pts.X, pts.Y, (int)item.Width, (int)item.Height, 0 | 0x0010);
                }
                resolutionText.Text = item.Width + "x" + item.Height;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadWallpaper(wallpaperData);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if(!_initializedWallpaper || _recording)
            {
                e.Cancel = true;
                return;
            }

            if(wallpaper != null)
            {
                //detach wallpaper window from this dialogue.
                WindowOperations.SetParentSafe(wallpaper.GetHWND(), IntPtr.Zero);
                try
                {
                    //temporary..till webprocess async close is ready.
                    if(wallpaper.GetWallpaperType() == WallpaperType.url)
                    {
                        var Proc = wallpaper.GetProcess();
                        Proc.Refresh();
                        Proc.StandardInput.WriteLine("lively:terminate");
                        if (!Proc.WaitForExit(4000))
                        {
                            wallpaper.Terminate();
                        }
                    }
                    else
                    {
                        wallpaper.Close();
                    }
                }
                catch
                {
                    wallpaper.Terminate();
                }
            }
        }

        private void LoadWallpaper(LibraryModel wp)
        {
            var targetDisplay = Program.SettingsVM.Settings.SelectedDisplay;
            if (wp.LivelyInfo.Type == WallpaperType.web
                || wp.LivelyInfo.Type == WallpaperType.webaudio
                || wp.LivelyInfo.Type == WallpaperType.url)
            {
                wp.ItemStartup = true;
                var item = new WebProcess(wp.FilePath, wp, targetDisplay);
                item.WindowInitialized += SetupDesktop_WallpaperInitialized;
                item.Show();
            }
            else if (wp.LivelyInfo.Type == WallpaperType.app
                || wp.LivelyInfo.Type == WallpaperType.godot
                || wp.LivelyInfo.Type == WallpaperType.unity)
            {
                if (Program.IsMSIX)
                {
                    Logger.Info("WallpaperPreview: Skipping program wallpaper on MSIX package.");
                    return;
                }

                wp.ItemStartup = true;
                var item = new ExtPrograms(wp.FilePath, wp, targetDisplay, 
                    Program.SettingsVM.Settings.WallpaperWaitTime);
                item.WindowInitialized += SetupDesktop_WallpaperInitialized;
                item.Show();
            }
            else if (wp.LivelyInfo.Type == WallpaperType.video)
            {
                wp.ItemStartup = true;
                var item = new VideoPlayerMPVExt(wp.FilePath, wp, targetDisplay,
                    Program.SettingsVM.Settings.WallpaperScaling);
                item.WindowInitialized += SetupDesktop_WallpaperInitialized;
                item.Show();
            }
            else if (wp.LivelyInfo.Type == WallpaperType.videostream)
            {
                if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", "libMPVPlayer", "lib", "youtube-dl.exe")))
                {
                    wp.ItemStartup = true;
                    var item = new VideoPlayerMPVExt(wp.FilePath, wp, targetDisplay,
                        Program.SettingsVM.Settings.WallpaperScaling, Program.SettingsVM.Settings.StreamQuality);
                    item.WindowInitialized += SetupDesktop_WallpaperInitialized;
                    item.Show();
                }
                else
                {
                    Logger.Info("Wallpaper Preview: yt-dl not found, using cef browser instead.");
                    //note: wallpaper type will be videostream, don't forget..
                    wp.ItemStartup = true;
                    var item = new WebProcess(wp.FilePath, wp, targetDisplay);
                    item.WindowInitialized += SetupDesktop_WallpaperInitialized;
                    item.Show();
                }
            }
            else if (wp.LivelyInfo.Type == WallpaperType.gif || wp.LivelyInfo.Type == WallpaperType.picture)
            {
                var item = new GIFPlayerUWP(wp.FilePath, wp,
                    targetDisplay, Program.SettingsVM.Settings.WallpaperScaling);
                item.WindowInitialized += SetupDesktop_WallpaperInitialized;
                item.Show();
            }
        }

        private void SetupDesktop_WallpaperInitialized(object sender, WindowInitializedArgs e)
        {
            try
            {
                _initializedWallpaper = true;
                wallpaper = (IWallpaper)sender;
                wallpaper.WindowInitialized -= SetupDesktop_WallpaperInitialized;
                _ = this.Dispatcher.BeginInvoke(new Action(() => {
                    wallpaper.GetWallpaperData().ItemStartup = false;
                    ProgressRing.IsActive = false;
                }));

                if (e.Success)
                {
                    _ = this.Dispatcher.BeginInvoke(new Action(() => {       
                        //attach wp hwnd to border ui element.
                        WindowOperations.SetProgramToFramework(this, wallpaper.GetHWND(), PreviewBorder);
                        //fix for wallpaper overlapping window bordere in high dpi screens.
                        this.Width += 1;
                    }));
                }
                else
                {
                    if (e.Error != null)
                    {
                        Logger.Error("Wallpaper Preview: Failed to launch wallpaper=>" + e.Msg + "\n" + e.Error);
                    }
                    else
                    {
                        Logger.Error("Wallpaper Preview: Failed to launch wallpaper=> (No Exception thrown)" + e.Msg);
                    }
                    MessageBox.Show(Properties.Resources.LivelyExceptionGeneral, Properties.Resources.TextError);

                    _ = this.Dispatcher.BeginInvoke(new Action(() => {
                        this.Close();
                    }));
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Wallpaper Preview: Failed processing wallpaper: " + ex.ToString());
                if (wallpaper != null)
                {
                    _ = this.Dispatcher.BeginInvoke(new Action(() => {
                        this.Close();
                    }));
                }
            }
        }

        #endregion //wallpaper loader

        #region video capture

        //todo: INCOMPLETE: error/capture failure handling, auto library import after capture etc..
        private void recordBtn_Click(object sender, RoutedEventArgs e)
        {
            if(!_recording)
            {
                _recording = true;
                var item = WindowOperations.GetAbsolutePlacement(PreviewBorder, true);
                recorder = new Helpers.ScreenRecorderDesktopDuplication();
                recorder.Initialize(Path.Combine(@"J:\Test", "test.mp4"), item, 60, 8000 * 1000, false, false);
                recorder.RecorderStatus += Recorder_RecorderStatus;
                recorder.StartRecording();
               
                if(dispatcherTimer == null)
                {
                    dispatcherTimer = new DispatcherTimer
                    {
                        Interval = new TimeSpan(0, 0, 0, 0, 1000)
                    };
                    dispatcherTimer.Tick += DispatcherTimer_Tick;
                }
                elapsedTime = 0;
                dispatcherTimer.Start();

                //ui refresh.
                //todo: mvvm rewrite.
                recordBtn.ToolTip = null;
                recordStatusText.Text = "0:00";
                recordStatusGlyph.Foreground = new SolidColorBrush(Colors.Red);
            }
            else
            {
                _recording = false;
                dispatcherTimer?.Stop();
                recorder?.StopRecording();

                //ui refresh.
                recordStatusText.Text = Properties.Resources.TextStart;
                recordBtn.ToolTip = Properties.Resources.DescriptionRecordStart;
                recordStatusGlyph.Foreground = new SolidColorBrush(Colors.Gray);
            }
        }

        private void DispatcherTimer_Tick(object sender, EventArgs e)
        {
            elapsedTime++;
            var span = TimeSpan.FromSeconds(elapsedTime);
            var time = string.Format("{0}:{1:00}",
                                (int)span.TotalMinutes,
                                span.Seconds);
            recordStatusText.Text = time;
        }

        private void Recorder_RecorderStatus(object sender, Helpers.ScreenRecorderStatus e)
        {
            switch (e)
            {
                case Helpers.ScreenRecorderStatus.idle:
                    break;
                case Helpers.ScreenRecorderStatus.paused:
                    break;
                case Helpers.ScreenRecorderStatus.fail:
                    break;
                case Helpers.ScreenRecorderStatus.recording:
                    break;
                case Helpers.ScreenRecorderStatus.finishing:
                    break;
                case Helpers.ScreenRecorderStatus.success:
                    break;
            }
            Logger.Info("Record status:" + e);
        }

        private bool GetIsScreenRecording()
        {
            return _recording;
        }

        #endregion //video capture

        #region window move/resize lock

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var source = PresentationSource.FromVisual(this) as HwndSource;
            source.AddHook(WndProc);
        }

        //prevent window resize and move during recording.
        //ref: https://stackoverflow.com/questions/3419909/how-do-i-lock-a-wpf-window-so-it-can-not-be-moved-resized-minimized-maximized
        public IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == (int)NativeMethods.WM.WINDOWPOSCHANGING && GetIsScreenRecording())
            {
                var wp = Marshal.PtrToStructure<NativeMethods.WINDOWPOS>(lParam);
                wp.flags |= (int)NativeMethods.SetWindowPosFlags.SWP_NOMOVE | (int)NativeMethods.SetWindowPosFlags.SWP_NOSIZE;
                Marshal.StructureToPtr(wp, lParam, false);
            }
            return IntPtr.Zero;
        }

        #endregion //window move/resize lock
    }
}

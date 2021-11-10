using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using livelywpf.Core;
using livelywpf.Core.API;
using livelywpf.Core.Wallpapers;
using livelywpf.Factories;
using livelywpf.Helpers;
using livelywpf.Helpers.Pinvoke;
using livelywpf.Helpers.ScreenRecord;
using livelywpf.Models;
using livelywpf.Services;
using Microsoft.Extensions.DependencyInjection;

namespace livelywpf.Views.Dialogues
{
    /// <summary>
    /// Interaction logic for WallpaperPreviewWindow.xaml
    /// </summary>
    public partial class WallpaperPreviewWindow : Window
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        //wallpaper loader.
        private readonly ILibraryModel wallpaperData;
        private IWallpaper wallpaper = null;
        private bool _initializedWallpaper = false;

        //video capture
        private DispatcherTimer dispatcherTimer;
        private bool _recording = false;
        private int elapsedTime;

        private readonly IWallpaperFactory wallpaperFactory;
        private readonly IUserSettingsService userSettings;
        private readonly IScreenRecorder recorder;

        #region wallpaper loader

        public WallpaperPreviewWindow(ILibraryModel model)
        {
            userSettings = App.Services.GetRequiredService<IUserSettingsService>();
            recorder = App.Services.GetRequiredService<IScreenRecorder>();
            wallpaperFactory = App.Services.GetRequiredService<IWallpaperFactory>();
            this.wallpaperData = model;

            InitializeComponent();
            this.SizeChanged += WallpaperPreviewWindow_SizeChanged;
        }

        private void WallpaperPreviewWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (wallpaper != null)
            {
                var item = WindowOperations.GetAbsolutePlacement(PreviewBorder, true);
                NativeMethods.POINT pts = new NativeMethods.POINT() { X = (int)item.Left, Y = (int)item.Top };
                if (NativeMethods.ScreenToClient(new WindowInteropHelper(this).Handle, ref pts))
                {
                    NativeMethods.SetWindowPos(wallpaper.Handle, 1, pts.X, pts.Y, (int)item.Width, (int)item.Height, 0 | 0x0010);
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
            if (!_initializedWallpaper || _recording)
            {
                e.Cancel = true;
                ModernWpf.Controls.Primitives.FlyoutBase.ShowAttachedFlyout(recordBtn);
                return;
            }

            if (wallpaper != null)
            {
                //detach wallpaper window from this dialogue.
                WindowOperations.SetParentSafe(wallpaper.Handle, IntPtr.Zero);
                try
                {
                    var proc = wallpaper.Proc;
                    if (wallpaper.Category == WallpaperType.url && proc != null)
                    {
                        wallpaper.SendMessage(new LivelyCloseCmd());
                        proc.Refresh();
                        if (!proc.WaitForExit(4000))
                        {
                            wallpaper.Terminate();
                        }
                    }
                    else
                    {
                        wallpaper.Terminate();
                    }
                }
                catch
                {
                    wallpaper.Terminate();
                }
            }
        }

        private void LoadWallpaper(ILibraryModel model)
        {
            try
            {
                IWallpaper instance = wallpaperFactory.CreateWallpaper(model, userSettings.Settings.SelectedDisplay, userSettings, true);
                model.ItemStartup = true;
                instance.WindowInitialized += WallpaperInitialized;
                instance.Show();
            }
            catch (Exception e)
            {
                Logger.Error(e.ToString());
            }
        }

        private void WallpaperInitialized(object sender, WindowInitializedArgs e)
        {
            try
            {
                _initializedWallpaper = true;
                wallpaper = (IWallpaper)sender;
                wallpaper.WindowInitialized -= WallpaperInitialized;
                _ = this.Dispatcher.BeginInvoke(new Action(() => {
                    wallpaper.Model.ItemStartup = false;
                    ProgressRing.IsActive = false;
                }));

                if (e.Success)
                {
                    _ = this.Dispatcher.BeginInvoke(new Action(() => {       
                        //attach wp hwnd to border ui element.
                        WindowOperations.SetProgramToFramework(this, wallpaper.Handle, PreviewBorder);
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
            if (!_recording)
            {
                //save dialog
                string savePath = string.Empty;
                var prevBorder = WindowOperations.GetAbsolutePlacement(PreviewBorder, true);
                var saveFileDialog1 = new Microsoft.Win32.SaveFileDialog()
                {
                    Title = "Select location to save the file",
                    Filter = Properties.Resources.TextVideo + "|*.mp4",
                    //title ending with '.' can have diff extension (example: parallax.js)
                    FileName = Path.GetFileNameWithoutExtension(wallpaperData.Title) + "_" + prevBorder.Width + "x" + prevBorder.Height,
                };
                if (saveFileDialog1.ShowDialog() == true)
                {
                    savePath = saveFileDialog1.FileName;
                }
                if (string.IsNullOrEmpty(savePath))
                {
                    return;
                }
                //overwrite existing file.
                if (File.Exists(savePath))
                {
                    try
                    {
                        File.Delete(savePath);
                    }
                    catch(Exception ex)
                    {
                        Logger.Error("Record status:Failed to delete existing file=>" + ex.Message);
                        return;
                    }
                }

                //recorder initialization
                recorder.Initialize(savePath, prevBorder, 60, 8000 * 1000, false, false);
                //recorder.Initialize(savePath, new WindowInteropHelper(this).Handle, 60, 8000 * 1000, false, false);
                recorder.RecorderStatus += Recorder_RecorderStatus;
                //recording timer.
                if (dispatcherTimer == null)
                {
                    dispatcherTimer = new DispatcherTimer
                    {
                        Interval = new TimeSpan(0, 0, 0, 0, 1000)
                    };
                    dispatcherTimer.Tick += DispatcherTimer_Tick;
                }
                StartRecording();
            }
            else
            {
                StopRecording();
            }
        }
        
        private void StartRecording()
        {

            elapsedTime = 0;
            _recording = true;
            dispatcherTimer?.Start();
            recorder?.StartRecording();

            //ui refresh.
            //todo: mvvm rewrite.
            recordBtn.ToolTip = null;
            recordStatusText.Text = "0:00";
            recordStatusGlyph.Foreground = new SolidColorBrush(Colors.Red);
        }

        private void StopRecording()
        {
            _recording = false;
            dispatcherTimer?.Stop();
            recorder?.StopRecording();

            //ui refresh
            recordStatusText.Text = Properties.Resources.TextStart;
            recordBtn.ToolTip = Properties.Resources.DescriptionRecordStart;
            recordStatusGlyph.Foreground = new SolidColorBrush(Colors.Gray);
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

        private void Recorder_RecorderStatus(object sender, ScreenRecorderStatus e)
        {
            switch (e)
            {
                case ScreenRecorderStatus.idle:
                    break;
                case ScreenRecorderStatus.paused:
                    break;
                case ScreenRecorderStatus.fail:
                    _ = this.Dispatcher.BeginInvoke(new Action(() => {
                        StopRecording();
                    }));
                    break;
                case ScreenRecorderStatus.recording:
                    break;
                case ScreenRecorderStatus.finishing:
                    break;
                case ScreenRecorderStatus.success:
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

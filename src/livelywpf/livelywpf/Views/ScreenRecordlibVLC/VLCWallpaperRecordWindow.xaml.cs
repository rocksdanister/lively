using livelywpf.Core;
using System.Windows;
using LibVLCSharp.Shared;
using System.Threading.Tasks;
using System;
using System.Diagnostics;
using System.Windows.Forms;
using System.IO;

namespace livelywpf.Views
{
    /// <summary>
    /// Interaction logic for VLCWallpaperRecordWindow.xaml
    /// </summary>
    public partial class VLCWallpaperRecordWindow : Window
    {
        IWallpaper wallpaper;
        bool _processing = false;
        readonly string wallpaperDir = Path.Combine(Program.LivelyDir, "wallpapers", Path.GetRandomFileName());
        CapturelibVLC capture = new CapturelibVLC();

        public VLCWallpaperRecordWindow(IWallpaper wp)
        {
            wallpaper = wp;
            Directory.CreateDirectory(wallpaperDir);
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //attach wallpaper window to the hidden border control.
            WindowOperations.SetProgramToFramework(this, wallpaper.GetHWND(), PreviewBorder);

            Rect previewPanelPos = WindowOperations.GetAbsolutePlacement(PreviewBorder, true);
            Size previewPanelSize = WindowOperations.GetElementPixelSize(PreviewBorder);

            //Window size is fixed to 720p.
            //When using non 16:9 resolutions capture is producing corrupted file?!
            capture = new CapturelibVLC();
            capture.Initialize(
                Path.Combine(wallpaperDir, "lively_capture_720p.mp4"), 
                (int)previewPanelSize.Width, 
                (int)previewPanelSize.Height, 
                (int)previewPanelPos.Left, 
                (int)previewPanelPos.Top);

            ScreenRecordlibVLC.ScreenRecordFloatingControl floatMenu = 
                new ScreenRecordlibVLC.ScreenRecordFloatingControl(
                this.Left + this.Width / 2.5f,
                this.Top + this.Height);
            floatMenu.RecordEvent += FloatMenu_RecordEvent;
            floatMenu.Show();
        }

        private void FloatMenu_RecordEvent(object sender, bool e)
        {
            if(e)
            {
                _processing = true;
                capture.StartRecord();
            }
            else
            {
                capture.StopRecord();
                capture.Dispose();

                if(wallpaper.GetWallpaperData().PreviewClipPath != null)
                    File.Copy(
                        wallpaper.GetWallpaperData().PreviewClipPath, 
                        Path.Combine(wallpaperDir, Path.GetFileName(wallpaper.GetWallpaperData().PreviewClipPath)));

                if(wallpaper.GetWallpaperData().ThumbnailPath != null)
                    File.Copy(
                        wallpaper.GetWallpaperData().ThumbnailPath, 
                        Path.Combine(wallpaperDir, Path.GetFileName(wallpaper.GetWallpaperData().ThumbnailPath)));

                wallpaper.GetWallpaperData().LivelyInfo.Title += "[v]";
                wallpaper.GetWallpaperData().LivelyInfo.FileName = "lively_capture_720p.mp4";
                wallpaper.GetWallpaperData().LivelyInfo.Type = WallpaperType.video;
                if (wallpaper.GetWallpaperData().LivelyInfo.IsAbsolutePath)
                {
                    wallpaper.GetWallpaperData().LivelyInfo.Preview = Path.GetFileName(wallpaper.GetWallpaperData().PreviewClipPath);
                    wallpaper.GetWallpaperData().LivelyInfo.Thumbnail = Path.GetFileName(wallpaper.GetWallpaperData().ThumbnailPath);
                }
                //save the new wallpaper metadata file.
                LivelyInfoJSON.SaveWallpaperMetaData(
                    wallpaper.GetWallpaperData().LivelyInfo, 
                    Path.Combine(wallpaperDir, "LivelyInfo.json"));

                Program.LibraryVM.AddWallpaper(wallpaperDir);

                _processing = false;
                this.Close();
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_processing)
            {
                e.Cancel = true;
                return;
            }

            //detach wallpaper window from this dialogue.
            WindowOperations.SetParentSafe(wallpaper.GetHWND(), IntPtr.Zero);
        }

    }
}

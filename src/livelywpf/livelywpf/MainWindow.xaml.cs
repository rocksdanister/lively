using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using System.Diagnostics;
using System.Reflection;
using Ionic.Zip;
using System.Windows.Forms;
using System.IO;
using Microsoft.Win32;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using MessageBox = System.Windows.MessageBox;
using Path = System.IO.Path;
using System.Security.Cryptography;
using Octokit;
using FileMode = System.IO.FileMode;
using Microsoft.WindowsAPICodePack.Shell;
using System.Threading;
using File = System.IO.File;
using NLog;
using System.Collections.ObjectModel;
using System.Windows.Media.Animation;
using System.ComponentModel;
using static livelywpf.SaveData;
using System.Windows.Interop;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;
using System.Globalization;
using livelywpf.Lively.Helpers;
using Enterwell.Clients.Wpf.Notifications;
using MahApps.Metro;

namespace livelywpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public static bool Multiscreen { get; private set;}
        public static bool HighContrastFix { get; private set; }
        private ProgressDialogController progressController = null;
        private ObservableCollection<TileData> tileDataList = new ObservableCollection<TileData>();
        private ObservableCollection<TileData> selectedTile = new ObservableCollection<TileData>();

        private ICollectionView tileDataFiltered;
        private bool _isRestoringWallpapers = false;

        private Release gitRelease = null;
        private string gitUrl = null;
        RawInputDX DesktopInputForward = null;

        public MainWindow()
        {
            SystemInfo.LogHardwareInfo();

            #region lively_SubProcess
            //External process that runs, kills external pgm wp's( unity, app etc) & refresh desktop in the event lively crashed, could do this in UnhandledException event but this is guaranteed to work even if user kills livelywpf in taskmgr.
            //todo:- should reconsider.
            try
            {
                Process.Start(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "livelySubProcess.exe"), Process.GetCurrentProcess().Id.ToString(CultureInfo.InvariantCulture));
            }
            catch(Exception e)
            {
                Logger.Error(e,"Starting livelySubProcess failure: " + e.Message);
            }
            #endregion lively_SubProcess

            //settings applied only during app relaunch.
            #region misc_fixes
            SetupDesktop.wallpaperWaitTime = SaveData.config.WallpaperWaitTime;

            if(SaveData.config.WallpaperRendering == WallpaperRenderingMode.bottom_most)
            {
                HighContrastFix = true;
            }
            else
            {
                HighContrastFix = false;
            }

            if (SaveData.config.Ui120FPS)
            {
                //force 120fps, in some systems gpu downclocking too much due to low power usage..this is a workaround for smoother ui.
                Timeline.DesiredFrameRateProperty.OverrideMetadata(typeof(Timeline),
                             new FrameworkPropertyMetadata { DefaultValue = 120 });
            }

            //disable UI HW-Acceleration, for very low end systems optional.
            if(SaveData.config.UiDisableHW)
                RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;

            //always show tooltip, even disabled ui elements.
            ToolTipService.ShowOnDisabledProperty.OverrideMetadata(typeof(System.Windows.Controls.Control),
                                                            new FrameworkPropertyMetadata(true));
            #endregion misc_fixes

            InitializeComponent();
            notify.Manager = new NotificationMessageManager();
            this.Closing += MainWindow_Closing;
            //SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged; //static event, unsubcribe!
            //todo:- Window.DpiChangedEvent, so far not required.
            //todo:- Suspend/hibernate events (SystemEvents.PowerModeChanged += SystemEvents_PowerModeChanged)

            CreateSysTray();
            //this.DataContext = SaveData.config;
            RestoreSaveSettings();

            //data binding
            wallpapersLV.ItemsSource = tileDataList;
            tileDataFiltered = CollectionViewSource.GetDefaultView(tileDataList);
            tileInfo.ItemsSource = selectedTile;
            //this.DataContext = selectedTile; //todo: figure out why is this not working?

            SubcribeUI();

            lblVersionNumber.Text = "v" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();

            //attribution document.
            TextRange textRange = new TextRange(licenseDocument.ContentStart, licenseDocument.ContentEnd);
            try
            {
                using (FileStream fileStream = File.Open(Path.Combine(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "docs", "license.rtf")), FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    textRange.Load(fileStream, System.Windows.DataFormats.Rtf);
                }
                licenseFlowDocumentViewer.Document = licenseDocument;
            }
            catch(Exception e)
            {
                Logger.Error("Failed to load license file:" + e.Message);
            }

            //whats new (changelog) screen!
            if (!SaveData.config.AppVersion.Equals(System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(), StringComparison.OrdinalIgnoreCase)
                && SaveData.config.IsFirstRun != true)
            {
                //if previous savedata version is different from currently running app, show help/update info screen.
                SaveData.config.AppVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
                SaveData.SaveConfig();

                Dialogues.Changelog cl = new Dialogues.Changelog
                {
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    ShowActivated = true
                };
                cl.Show();
            }

            //restore previously running wp's.
            Multiscreen = false;
            SystemEvents_DisplaySettingsChanged(this, null);
            SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged; //static event, unsubcribe!

            //Incomplete, currently in development:- all process algorithm with multiscreen is buggy.
            if (Multiscreen && SaveData.config.ProcessMonitorAlgorithm == ProcessMonitorAlgorithm.all)
            {
                Logger.Info("Skipping all-process algorthm on multiscreen(in-development)");
                comboBoxPauseAlgorithm.SelectedIndex = (int)ProcessMonitorAlgorithm.foreground; //event will save settings.
            }

        }

        private async void RestoreSaveSettings()
        {
            //load savefiles. 
            SaveData.LoadApplicationRules();
            //SaveData.LoadConfig(); //App() loads config file.
            SaveData.LoadWallpaperLayout();
            RestoreMenuSettings();

            //github update check
            try
            {
                gitRelease = await UpdaterGit.GetLatestRelease("lively", "rocksdanister", 45000); //45sec
                int result = UpdaterGit.CompareAssemblyVersion(gitRelease);
                if (result > 0) //github ver greater, update available!
                {
                    try {
                        //asset format: lively_setup_x86_full_vXXXX.exe, XXXX - 4 digit version no.
                        gitUrl = await UpdaterGit.GetAssetUrl("lively_setup_x86_full", gitRelease, "lively", "rocksdanister");
                    }
                    catch (Exception e) 
                    {
                        Logger.Error("Error retriving asseturl for update: " + e.Message);
                    }

                    update_traybtn.Text = Properties.Resources.txtContextMenuUpdate2;  
                    if (UpdateNotifyOrNot())
                    {
                        if (App.W != null)
                        {
                            //system tray notification, only displayed if lively is minimized to tray.
                            if (!App.W.IsVisible)
                            {
                                _notifyIcon.ShowBalloonTip(2000, "lively", Properties.Resources.toolTipUpdateMsg, ToolTipIcon.None);
                            }

                            notify.Manager.CreateMessage()
                               .Animates(true)
                               .AnimationInDuration(0.75)
                               .AnimationOutDuration(0.75)
                               .Accent("#808080")
                               .Background("#333")
                               .HasBadge("Info")
                               .HasMessage(Properties.Resources.txtUpdateBanner + " " + gitRelease.TagName)
                               .WithButton(Properties.Resources.txtDownload, button => { ShowLivelyUpdateWindow(); })
                               .Dismiss().WithButton(Properties.Resources.txtLabel37, button => { })
                                /*
                                .WithAdditionalContent(ContentLocation.Bottom,
                                new Border
                                {
                                    BorderThickness = new Thickness(0, 1, 0, 0),
                                    BorderBrush = new SolidColorBrush(Color.FromArgb(128, 28, 28, 28)),
                                    Child = new System.Windows.Controls.CheckBox
                                    {
                                        Margin = new Thickness(12, 8, 12, 8),
                                        Content = Properties.Resources.txtIgnore + " " + gitRelease.TagName
                                    }
                                })
                                */
                            .Queue();
                        }
                    }
                }
                else if (result < 0) //this is early access software.
                {
                    update_traybtn.Text = Properties.Resources.txtContextMenuUpdate3;
                }
                else //up-to-date
                {
                    update_traybtn.Text = Properties.Resources.txtContextMenuUpdate4;
                }
            }
            catch(Exception e)
            {
                //todo: retry after waiting.
                update_traybtn.Text = Properties.Resources.txtContextMenuUpdate5;
                Logger.Error("Error checking for update: " + e.Message);
            }
            update_traybtn.Enabled = true;
        }

        #region wp_input_setup
        private void WallpaperInputForwardingToggle(bool isEnable)
        {
            if (isEnable)
            {
                if (DesktopInputForward == null)
                {
                    DesktopInputForward = new RawInputDX();
                    DesktopInputForward.Closing += DesktopInputForward_Closing;
                    DesktopInputForward.Show();
                }
            }
            else
            {
                if (DesktopInputForward != null)
                {
                    DesktopInputForward.Close();
                }
            }
        }

        private void DesktopInputForward_Closing(object sender, CancelEventArgs e)
        {
            DesktopInputForward = null;
        }
        #endregion

        #region git_update

        Dialogues.AppUpdate appUpdateWindow = null;
        private void ShowLivelyUpdateWindow()
        {
            if (appUpdateWindow == null)
            {            
                appUpdateWindow = new Dialogues.AppUpdate(gitRelease, gitUrl)
                {
                    WindowStartupLocation = WindowStartupLocation.CenterScreen
                };
                if (App.W.IsVisible)
                {
                    appUpdateWindow.Owner = App.W;
                    WindowStartupLocation = WindowStartupLocation.CenterOwner;
                }
                
                appUpdateWindow.Show();
                appUpdateWindow.Closed += AppUpdateWindow_Closed;
            }
        }

        private void AppUpdateWindow_Closed(object sender, EventArgs e)
        {
            appUpdateWindow = null;
        }

        private bool UpdateNotifyOrNot()
        {
            if(gitRelease == null || gitUrl == null)
            {
                return false;
            }
            else if(SaveData.config.IsFirstRun || String.IsNullOrWhiteSpace(gitRelease.TagName) ||
                gitRelease.TagName.Equals(SaveData.config.IgnoreUpdateTag, StringComparison.Ordinal))
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        #endregion git_update

        #region CefGallery
        //incomplete: work in progress, not sure weather to finish this.. deviantart downloader.
        Process webProcess;
        private void StartCefBrowserNewWindow(string url)
        {
            webProcess = new Process();
            ProcessStartInfo start1 = new ProcessStartInfo();
            //start1.Arguments = url + @" deviantart";
            start1.Arguments = url + @" online";

            start1.FileName = App.PathData + @"\external\cef\LivelyCefSharp.exe";
            start1.RedirectStandardInput = true;
            start1.RedirectStandardOutput = true;
            start1.UseShellExecute = false;

            webProcess = new Process();
            webProcess = Process.Start(start1);
            webProcess.EnableRaisingEvents = true;
            webProcess.OutputDataReceived += WebProcess_OutputDataReceived;
            webProcess.Exited += WebProcess_Exited;
            webProcess.BeginOutputReadLine();

        }

        private void WebProcess_Exited(object sender, EventArgs e)
        {
            webProcess.OutputDataReceived -= WebProcess_OutputDataReceived;
            webProcess.Close();
        }

        private static void WebProcess_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            Logger.Info("CEF:" + e.Data);
            try
            {
                if (e.Data.Contains("LOADWP"))
                {
                    var downloadedFilePath = e.Data.Replace("LOADWP", String.Empty);

                    System.Windows.Application.Current.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Background, new ThreadStart(delegate
                    {
                        App.W.ShowMainWindow();
                        App.W.WallpaperInstaller(downloadedFilePath);
                    }));
                }
            }
            catch (NullReferenceException)
            {

            }
            catch (Exception)
            {
                //todo
            }
        }
        #endregion CefGallery

        #region system_events
        private void SystemEvents_PowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            throw new NotImplementedException();
        }

        bool _startupRun = true;
        /// <summary>
        /// Display device settings changed event. 
        /// Closes & restarts wp's in the event system display layout changes.(based on wallpaperlayout SaveData file)
        /// Updates wp dimensions in the event ONLY resolution changes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void SystemEvents_DisplaySettingsChanged(object sender, EventArgs e)
        {
            //bool _startupRun = false;
            if (Screen.AllScreens.Length > 1)
            {
                foreach (var item in Screen.AllScreens)
                {
                    Debug.WriteLine("Detected Displays:- " + item);
                    Logger.Debug("Detected Displays:- " + item);
                }
                Multiscreen = true;
            }
            else
            {
                Multiscreen = false;
                Logger.Debug("Single Display Mode:- " + Screen.PrimaryScreen);
            }

            List<SaveData.WallpaperLayout> toBeRemoved = new List<SaveData.WallpaperLayout>();
            List<SaveData.WallpaperLayout> wallpapersToBeLoaded = new List<SaveData.WallpaperLayout>(SetupDesktop.wallpapers);

            if (_startupRun) //first run.
            {
                _startupRun = true;
            }
            else
            {
                Logger.Info("Display Settings Changed Event..");
            }

            bool found;
            foreach (var item in wallpapersToBeLoaded)
            {
                found = false;
                foreach (var scr in Screen.AllScreens)
                {
                    if (item.DeviceName == scr.DeviceName) //ordinal comparison
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    toBeRemoved.Add(item);
                }
            }
            if (toBeRemoved.Count > 0)
                wallpapersToBeLoaded = wallpapersToBeLoaded.Except(toBeRemoved).ToList(); //new list

            foreach (var item in wallpapersToBeLoaded)
            {
                Logger.Info("Display(s) wallpapers to load:-" + item.DeviceName);
            }

            if (!wallpapersToBeLoaded.SequenceEqual(SetupDesktop.wallpapers) || _startupRun)
            {
                SetupDesktop.CloseAllWallpapers(); //todo: only close wallpapers that which is running on disconnected display.
                Logger.Info("Restarting/Restoring All Wallpaper(s)");

                //remove wp's with file missing on disk, except for url type( filePath =  website url).
                if (wallpapersToBeLoaded.RemoveAll(x => !File.Exists(x.FilePath) && x.Type != SetupDesktop.WallpaperType.url && x.Type != SetupDesktop.WallpaperType.video_stream) > 0)
                {
                    _notifyIcon.ShowBalloonTip(10000, "lively", Properties.Resources.toolTipWallpaperSkip, ToolTipIcon.None);
                    notify.Manager.CreateMessage()
                    .Accent("#FF0000")
                    .HasBadge("Warn")
                    .Background("#333")
                    .HasHeader(Properties.Resources.txtLivelyErrorMsgTitle)
                    .HasMessage(Properties.Resources.toolTipWallpaperSkip)
                    .Dismiss().WithButton("Ok", button => { })
                    .Queue();
                }

                if (SaveData.config.WallpaperArrangement == WallpaperArrangement.span)
                {
                    //unlikely to happen unless user edits the json file manually or some file error? 
                    if (wallpapersToBeLoaded.Count > 1)
                    {
                        //span across all display(s), only 1 wp allowed!
                        wallpapersToBeLoaded.RemoveRange(1, (wallpapersToBeLoaded.Count - 1));
                    }
                }
                RestoreWallpaper(wallpapersToBeLoaded);
            }
            else
            {
                Logger.Info("Display(s) settings such as resolution etc changed, updating wp(s) dimensions");
                SetupDesktop.UpdateAllWallpaperRect();
            }
            //wallpapersToBeLoaded.Clear();
            _startupRun = false;
        }

        #endregion system_events

        #region windows_startup
        /// <summary>
        /// Adds startup entry in registry under application name "livelywpf", current user ONLY. (Does not require admin rights).
        /// </summary>
        /// <param name="setStartup">false: delete entry, true: add/update entry.</param>
        private void SetStartupRegistry(bool setStartup = false)
        {
            Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            Assembly curAssembly = Assembly.GetExecutingAssembly();
            if (setStartup)
            {
                try
                {
                    key.SetValue(curAssembly.GetName().Name, curAssembly.Location);
                }
                catch (Exception ex)
                {
                    StartupToggle.IsChecked = false;
                    Logger.Error(ex.ToString());
                    WpfNotification(NotificationType.error, Properties.Resources.txtLivelyErrorMsgTitle, "Failed to setup startup: " + ex.Message);
                }
            }
            else
            {
                try
                {
                    key.DeleteValue(curAssembly.GetName().Name, false);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex.ToString());
                }
            }
            key.Close();
        }
        /// <summary>
        /// Checks if startup registry entry is present and returns key value.
        /// </summary>
        /// <returns></returns>
        private static string CheckStartupRegistry()
        {
            Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            Assembly curAssembly = Assembly.GetExecutingAssembly();
            string result = null;
            try
            {
                result = (string)key.GetValue(curAssembly.GetName().Name);
            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString());
            }
            finally
            {
                key.Close();
            }

            return result;
            /*
            if (String.IsNullOrEmpty(result))
            {            
                return false;
            }
            else if(String.Equals(result, curAssembly.Location, StringComparison.Ordinal))
            {
                return true;
            }
            else
            {
                return false;
            }     
            */
        }

        [Obsolete("Fails to work when folderpath contains non-english characters(WshShell is ancient afterall); use SetStartupRegistry() instead.")]
        /// <summary>
        /// Creates application shortcut & copy to startup folder of current user(does not require admin rights).
        /// </summary>
        /// <param name="setStartup"></param>
        private void SetStartupFolder(bool setStartup = false)
        {
            string shortcutAddress = Environment.GetFolderPath(Environment.SpecialFolder.Startup) + "\\LivelyWallpaper.lnk";
            if (setStartup)
            {
                try
                {
                    IWshRuntimeLibrary.WshShell shell = new IWshRuntimeLibrary.WshShell();
                    System.Reflection.Assembly curAssembly = System.Reflection.Assembly.GetExecutingAssembly();

                    IWshRuntimeLibrary.IWshShortcut shortcut = (IWshRuntimeLibrary.IWshShortcut)shell.CreateShortcut(shortcutAddress);
                    shortcut.Description = "Lively Wallpaper System";
                    shortcut.WorkingDirectory = App.PathData;
                    shortcut.TargetPath = curAssembly.Location;
                    shortcut.Save();
                }
                catch(Exception e) 
                {
                    StartupToggle.IsChecked = false;
                    Logger.Error(e.ToString());
                    MessageBox.Show("Failed to setup startup", Properties.Resources.txtLivelyErrorMsgTitle);
                }
            }
            else
            {
                if(File.Exists(shortcutAddress))
                {
                    try
                    {
                        File.Delete(shortcutAddress);
                    }
                    catch (UnauthorizedAccessException e)
                    {
                        Logger.Error(e.ToString());
                        MessageBox.Show("UnauthorizedAccessException: The caller does not have the required permission.? try restarting lively with admin access or delete the file yourself:\n" 
                            + Environment.GetFolderPath(Environment.SpecialFolder.Startup), Properties.Resources.txtLivelyErrorMsgTitle);
                    }
                }
            }
        }

        private void StartupToggle_IsCheckedChanged(object sender, EventArgs e)
        {
            SetStartupRegistry(StartupToggle.IsChecked.Value);

            SaveData.config.Startup = StartupToggle.IsChecked.Value;
            SaveData.SaveConfig();
        }
        #endregion windows_startup

        #region wallpaper_library  

        /// <summary>
        /// Loads & populate lively wp library from "//wallpapers" path if any. 
        /// </summary>
        public void UpdateWallpaperLibrary()
        {
            tileDataList.Clear();
            selectedTile.Clear();
            //wallpapersLV.SelectedIndex = -1;
            List<SaveData.TileData> tmpLoadedWallpapers = new List<SaveData.TileData>();
            var wpDir = Directory.GetDirectories( Path.Combine( App.PathData, "wallpapers"));
            var tmpDir = Directory.GetDirectories( Path.Combine(App.PathData, "SaveData", "wptmp"));
            var dir = wpDir.Concat(tmpDir).ToArray();

            for (int i = 0; i < dir.Length; i++)
            {
                var item = dir[i];
                if (File.Exists(Path.Combine(item, "LivelyInfo.json")))
                {
                    if (SaveData.LoadWallpaperMetaData(item))
                    {       
                        if (i < wpDir.Length) //wallpaper folder; relative path.
                        {
                            if (info.Type == SetupDesktop.WallpaperType.video_stream || info.Type == SetupDesktop.WallpaperType.url)
                            {
                                //online content.
                            }
                            else
                            {
                                SaveData.info.FileName = Path.Combine(item, SaveData.info.FileName);
                            }

                            try
                            {
                                SaveData.info.Preview = Path.Combine(item, SaveData.info.Preview);
                            }
                            catch(ArgumentNullException)
                            {
                                SaveData.info.Preview = null;
                            }
                            catch(ArgumentException)
                            {
                                SaveData.info.Preview = null;
                            }

                            try
                            {
                                SaveData.info.Thumbnail = Path.Combine(item, SaveData.info.Thumbnail);
                            }
                            catch(ArgumentNullException)
                            {
                                SaveData.info.Thumbnail = null;
                            }
                            catch(ArgumentException)
                            {
                                SaveData.info.Thumbnail = null;
                            }
                        }
                        else //absolute path wp's ( //SaveData//wptmp//)
                        {                     
                            if (File.Exists(SaveData.info.Preview) != true)
                            {
                                //backward compatible with portable ver of lively, if file is moved and absolute path is wrong.
                                if (File.Exists(Path.Combine(item, Path.GetFileName(SaveData.info.Preview))))
                                {
                                    SaveData.info.Preview = Path.Combine(item, Path.GetFileName(SaveData.info.Preview));
                                }
                                else
                                    SaveData.info.Preview = null;
                            }

                            if (File.Exists(SaveData.info.Thumbnail) != true)
                            {
                                if (File.Exists(Path.Combine(item, Path.GetFileName(SaveData.info.Thumbnail))))
                                {
                                    SaveData.info.Thumbnail = Path.Combine(item, Path.GetFileName(SaveData.info.Thumbnail));
                                }
                                else
                                    SaveData.info.Thumbnail = null;
                            }
                            
                        }

                        //load anyway for absolutepath, setupwallpaper will check if file exists for this type and give warning.
                        //this also prevents disk powerup in the event the files are in different hdd thats sleeping and lively is launched from tray.
                        if (info.IsAbsolutePath) 
                        {
                            Logger.Info("Loading Wallpaper (absolute path):- " + SaveData.info.FileName + " " + SaveData.info.Type);
                            tmpLoadedWallpapers.Add(new TileData(info, item));
                        }
                        else if(info.Type == SetupDesktop.WallpaperType.video_stream
                                || info.Type == SetupDesktop.WallpaperType.url) //no files for this type.)
                        {
                            Logger.Info("Loading Wallpaper (url/stream):- " + SaveData.info.FileName + " " + SaveData.info.Type);
                            tmpLoadedWallpapers.Add(new TileData(info, item));
                        }
                        else if (File.Exists(SaveData.info.FileName))
                        {
                            Logger.Info("Loading Wallpaper (wp dir):- " + SaveData.info.FileName + " " + SaveData.info.Type);
                            tmpLoadedWallpapers.Add(new TileData(info, item));
                        }
                        else
                        {
                            Logger.Info("Skipping wallpaper:- " + SaveData.info.FileName + " " + SaveData.info.Type);
                        }
                    }
                }
                else
                {
                    Logger.Info("Not a lively wallpaper folder, skipping:- " + item);
                }
            }

            //tmpItems.Sort((x, y) => string.Compare(x.LivelyInfo.Title, y.LivelyInfo.Title));
            //sorting based on alphabetical order of wp title text. 
            var sortedList = tmpLoadedWallpapers.OrderBy(x => x.LivelyInfo.Title).ToList();
            foreach (var item in sortedList)
            {               
                tileDataList.Add(new TileData(item.LivelyInfo, item.LivelyInfoDirectoryLocation));
            }

            sortedList.Clear();
            tmpLoadedWallpapers.Clear();
            sortedList = null;
            tmpLoadedWallpapers = null;

            InitializeTilePreviewGifs();

            if (prevSelectedLibIndex < tileDataList.Count)
                wallpapersLV.SelectedIndex = prevSelectedLibIndex;
            else
                wallpapersLV.SelectedIndex = -1;
        }

        /// <summary>
        /// Copy & load wallpaper file from tmpdata/wpdata folder into Library.
        /// </summary>
        public void LoadWallpaperFromWpDataFolder()
        {
            //library tab.
            if(tabControl1.SelectedIndex != 0)
                tabControl1.SelectedIndex = 0;

            var randomFolderName = Path.GetRandomFileName();
            var dir = Path.Combine(App.PathData, "SaveData", "wptmp", randomFolderName);
            try
            {
                Directory.CreateDirectory(dir);
            }
            catch(Exception e)
            {
                Logger.Error(e.ToString());
                return;
            }

            if (SaveData.LoadWallpaperMetaData(Path.Combine(App.PathData, "tmpdata","wpdata\\") ))
            {
                //making the thumbnail & preview absolute paths.
                if(File.Exists( Path.Combine(App.PathData, "tmpdata", "wpdata", SaveData.info.Preview) ))
                    SaveData.info.Preview = Path.Combine(dir,SaveData.info.Preview);
                if (File.Exists( Path.Combine(App.PathData, "tmpdata", "wpdata", SaveData.info.Thumbnail) ))
                    SaveData.info.Thumbnail = Path.Combine(dir, SaveData.info.Thumbnail);

                SaveData.SaveWallpaperMetaData(SaveData.info, dir);
            }
            else
            {
                Logger.Error("LoadWallpaperFromWpDataFolder(): Failed to load livelyinfo for tmpwallpaper!..deleting tmpfiles.");
                Task.Run(() => (FileOperations.EmptyDirectory( Path.Combine(App.PathData, "tmpdata", "wpdata\\") )));
                try
                {
                    Directory.Delete(dir);
                }
                catch (Exception ie1)
                {
                    Logger.Error(ie1.ToString());
                }
                return;
            }

            try
            {
                if (File.Exists( Path.Combine(App.PathData, "tmpdata", "wpdata", Path.GetFileName(SaveData.info.Thumbnail)) ))
                    File.Copy( Path.Combine(App.PathData, "tmpdata", "wpdata", Path.GetFileName(SaveData.info.Thumbnail)), SaveData.info.Thumbnail);

                if (File.Exists( Path.Combine(App.PathData, "tmpdata", "wpdata", Path.GetFileName(SaveData.info.Preview)) ))
                    File.Copy( Path.Combine(App.PathData, "tmpdata", "wpdata", Path.GetFileName(SaveData.info.Preview)), SaveData.info.Preview);
                    
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
            }
            finally
            {
                UpdateWallpaperLibrary();
                Task.Run(() => (FileOperations.EmptyDirectory(Path.Combine(App.PathData, "tmpdata", "wpdata\\"))));
                //selecting newly added wp.
                foreach (var item in tileDataList)
                {
                    if (item.LivelyInfoDirectoryLocation.Contains(randomFolderName))
                    {
                        wallpapersLV.SelectedItem = item;
                        if (SaveData.config.LivelyZipGenerate)
                        {
                            MenuItem_CreateZip_Click(this, null);
                        }
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// User selected tile in library.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WallpapersLV_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedTile.Clear();
            if (wallpapersLV.SelectedIndex == -1)
            {
                return;
            }
            var selection = (TileData)wallpapersLV.SelectedItem;

            selection.CustomiseBtnToggle = false;
            if ((SetupDesktop.wallpapers.FindIndex(x => x.FilePath.Equals(selection.LivelyInfo.FileName, StringComparison.Ordinal))) != -1)
            {
                if (selection.IsCustomisable)
                {
                    selection.CustomiseBtnToggle = true;
                }
            }
            selectedTile.Add(selection);
            wallpapersLV.ScrollIntoView(selection);
            /*
            if(!File.Exists(selectedTile[0].LivelyInfo.Preview)) //if no preview gif, load image instead.
            {
                if (File.Exists(selectedTile[0].LivelyInfo.Thumbnail))
                {
                    selectedTile[0].LivelyInfo.Preview = selectedTile[0].LivelyInfo.Thumbnail;
                }
                else
                {
                    selectedTile[0].LivelyInfo.Preview = null;
                }
            }
            */
        }

        #region library_filter
        private void TextBoxLibrarySearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (String.IsNullOrWhiteSpace(textBoxLibrarySearch.Text))
            {
                tileDataFiltered.Filter = null;
            }
            else
            {
                TileData tmpvar;
                //search based on title +(or) desc
                tileDataFiltered.Filter = i => ( (tmpvar = (TileData)i).LivelyInfo.Title + tmpvar.LivelyInfo.Desc).IndexOf(textBoxLibrarySearch.Text, StringComparison.OrdinalIgnoreCase) > -1;
            }
            //todo:- fix, search buggy with my dynamic gif loading code.
            //prevIndexOffset = 666;
            //WallpapersLV_ScrollChanged(null, null);
        }

        #endregion library_filter

        #region scroll_gif_logic

        /// <summary>
        /// Initialize only few gif preview to reduce cpu usage, gif's are loaded & disposed(atleast marked) based on ScrollChanged event.
        /// </summary>
        private void InitializeTilePreviewGifs()
        {
            if (!SaveData.config.LiveTile)
                return;

            for (int i = 0; i < tileDataList.Count; i++)
            {
                if (i >= 20)
                    return;
                if(File.Exists(tileDataList[i].LivelyInfo.Preview))
                    tileDataList[i].TilePreview = tileDataList[i].LivelyInfo.Preview;
            }
        }

        private int prevIndexOffset = 0;
        /// <summary>
        /// only loading upto a certain no: of gif at a time in library to reduce load.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WallpapersLV_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (!SaveData.config.LiveTile)
                return;

            ScrollViewer scrollViewer = GetDescendantByType(wallpapersLV, typeof(ScrollViewer)) as ScrollViewer;
            if (scrollViewer != null)
            {
                //Debug.WriteLine(scrollViewer.VerticalOffset );
                //Debug.WriteLine("Visible Item Count:{0}", scrollViewer.ViewportHeight);
                int indexOffset = 0;
                double percent = scrollViewer.ViewportHeight * .33f;
                double shiftY = scrollViewer.VerticalOffset / percent;

                int startIndex = 0;
                /*
                for (int i = 0; i < tileDataList.Count; i++)
                {
                    if (tileDataList[i].LivelyInfo.title.StartsWith(textBoxLibrarySearch.Text, StringComparison.OrdinalIgnoreCase))
                    {
                        startIndex = i;
                        break;
                    }
                }
                */
                if (shiftY > 0 || sender == null )
                {
                    indexOffset = startIndex +  Convert.ToInt32(shiftY) * 5;
                }

                if (indexOffset != prevIndexOffset)
                {
                    int count = 0;
                    for (int i = 0; i < tileDataList.Count; i++)
                    {
                        if (i >= indexOffset && count <= 20)
                        {
                            if (File.Exists(tileDataList[i].LivelyInfo.Preview))
                            {
                                tileDataList[i].TilePreview = tileDataList[i].LivelyInfo.Preview;
                            }
                            count++;
                        }
                        else
                        {
                            tileDataList[i].TilePreview = null;
                        }
                    }
                    //wallpapersLV.Items.Refresh();
                }
                prevIndexOffset = indexOffset;
            }           
        }

        public static Visual GetDescendantByType(Visual element, Type type)
        {
            if (element == null)
            {
                return null;
            }
            if (element.GetType() == type)
            {
                return element;
            }
            Visual foundElement = null;
            if (element is FrameworkElement)
            {
                (element as FrameworkElement).ApplyTemplate();
            }
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
            {
                Visual visual = VisualTreeHelper.GetChild(element, i) as Visual;
                foundElement = GetDescendantByType(visual, type);
                if (foundElement != null)
                {
                    break;
                }
            }
            return foundElement;
        }

        #endregion scroll_gif_logic

        /// <summary>
        /// Determine video resolution
        /// </summary>
        /// <param name="videoFullPath"></param>
        /// <returns>x = width, y = heigh</returns>
        public static Size GetVideoSize(string videoFullPath)
        {
            try
            {
                if (File.Exists(videoFullPath))
                {
                    ShellFile shellFile = ShellFile.FromFilePath(videoFullPath);

                    int videoWidth = (int)shellFile.Properties.System.Video.FrameWidth.Value;
                    int videoHeight = (int)shellFile.Properties.System.Video.FrameHeight.Value;

                    return new Size(videoWidth, videoHeight);
                }
            }
            catch(Exception)
            {
                return Size.Empty;
            }
            return Size.Empty;
        }

        private void MenuItem_SetWallpaper_Click(object sender, RoutedEventArgs e) //contextmenu
        {
            SetWallpaperBtn_Click(this, null);
        }
        private async void SetWallpaperBtn_Click(object sender, RoutedEventArgs e)
        {
            if (wallpapersLV.SelectedIndex == -1)
                return;

            var selection = (TileData)wallpapersLV.SelectedItem;
            if (selection.LivelyInfo.Type == SetupDesktop.WallpaperType.app || selection.LivelyInfo.Type == SetupDesktop.WallpaperType.godot
                || selection.LivelyInfo.Type == SetupDesktop.WallpaperType.unity || selection.LivelyInfo.Type == SetupDesktop.WallpaperType.unity_audio)
            {
                var ch = await this.ShowMessageAsync(Properties.Resources.msgExternalAppWarningTitle,Properties.Resources.msgExternalAppWarning, MessageDialogStyle.AffirmativeAndNegative,
                            new MetroDialogSettings() { DialogTitleFontSize = 18, ColorScheme = MetroDialogColorScheme.Inverted, DialogMessageFontSize = 16,
                            AnimateHide = false, AnimateShow = false});

                if (ch == MessageDialogResult.Negative)
                    return;
                else if (ch == MessageDialogResult.Affirmative)
                {}
            }
            else if(selection.LivelyInfo.Type == SetupDesktop.WallpaperType.url || selection.LivelyInfo.Type == SetupDesktop.WallpaperType.video_stream)
            {
                var ch = await this.ShowMessageAsync(Properties.Resources.msgUrlWarningTitle, Properties.Resources.msgUrlWarning +"\n\n"+ selection.LivelyInfo.FileName, 
                                    MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings() { DialogTitleFontSize = 18, 
                                    ColorScheme = MetroDialogColorScheme.Inverted, DialogMessageFontSize = 16,
                                    AnimateShow = false, AnimateHide = false});

                if (ch == MessageDialogResult.Negative)
                    return;
                else if (ch == MessageDialogResult.Affirmative)
                {}
            }

            if (selection.IsCustomisable)
            {
                //show customise btn when wp set.
                selection.CustomiseBtnToggle = true;
            }
            
            if (selection.LivelyInfo.Type == SetupDesktop.WallpaperType.app)
            {
                SetupWallpaper(selection.LivelyInfo.FileName, selection.LivelyInfo.Type, selection.LivelyInfo.Arguments, true);
            }
            else
                SetupWallpaper(selection.LivelyInfo.FileName, selection.LivelyInfo.Type, null, true);
        }

        /// <summary>
        /// Library wp customise btn.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItem_CustomiseWallpaper_Click(object sender, RoutedEventArgs e) //contextmenu
        {
            //ShowCustomiseWidget();
            if (wallpapersLV.SelectedIndex == -1)
                return;

            if (Multiscreen)
            {
                var selection = (TileData)wallpapersLV.SelectedItem;

                //checking if same wp running more than 1 instance.
                var wp = SetupDesktop.webProcesses.FindAll(x => x.FilePath.Equals(selection.LivelyInfo.FileName, StringComparison.Ordinal));
                if (wp.Count > 1)
                {
                    //monitor select dialog
                    DisplaySelectWindow displaySelectWindow = new DisplaySelectWindow
                    {
                        WindowStartupLocation = WindowStartupLocation.CenterScreen
                    };
                    displaySelectWindow.ShowDialog();

                    if (DisplaySelectWindow.selectedDisplay == null) //none
                    {
                        return;
                    }
                    SetupDesktop.SendCustomiseMsgtoWallpaper(DisplaySelectWindow.selectedDisplay);
                }
                else
                {
                    SetupDesktop.SendCustomiseMsgtoWallpaper2(selection.LivelyInfo.FileName);
                }
            }
            else
            {
                SetupDesktop.SendCustomiseMsgtoWallpaper(Screen.PrimaryScreen.DeviceName);
            }
        }

        /// <summary>
        /// System tray customise option.
        /// Always display selection dialog for multiple screens.
        /// </summary>
        private static void ShowCustomiseWidget()
        {
            if (Multiscreen)
            {
                //monitor select dialog
                DisplaySelectWindow displaySelectWindow = new DisplaySelectWindow
                {
                    WindowStartupLocation = WindowStartupLocation.CenterScreen
                };
                displaySelectWindow.ShowDialog();

                if (DisplaySelectWindow.selectedDisplay == null) //none
                {
                    return;
                }

                SetupDesktop.SendCustomiseMsgtoWallpaper(DisplaySelectWindow.selectedDisplay);
            }
            else
            {
                SetupDesktop.SendCustomiseMsgtoWallpaper(Screen.PrimaryScreen.DeviceName);
            }
        }

        private void MenuItem_ShowOnDisk_Click(object sender, RoutedEventArgs e) 
        {
            if (wallpapersLV.SelectedIndex == -1)
                return;

            try
            {
                var selection = (TileData)wallpapersLV.SelectedItem;
                string folderPath;
                if (selection.LivelyInfo.Type == SetupDesktop.WallpaperType.url || selection.LivelyInfo.Type == SetupDesktop.WallpaperType.video_stream)
                {
                    folderPath = selection.LivelyInfoDirectoryLocation;
                }
                else
                {
                    folderPath = Path.GetDirectoryName(selection.LivelyInfo.FileName);
                }

                if (Directory.Exists(folderPath))
                {
                    ProcessStartInfo startInfo = new ProcessStartInfo
                    {
                        Arguments = "\"" + folderPath + "\"",
                        FileName = "explorer.exe"
                    };
                    Process.Start(startInfo);
                }
            }
            catch (Exception e1)
            {
                Logger.Error("folder open error:- " + e1.ToString());
                WpfNotification(NotificationType.error, Properties.Resources.txtLivelyErrorMsgTitle, e1.Message);
            }
        }

        
        class ZipCreateInfo
        {
            public MetroProgressBar ProgressBar { get; private set; }
            public INotificationMessage Notification { get; private set; }
            public ZipFile ZipFile { get; private set; }
            public bool AbortZipExtraction { get; set; }
            public ZipCreateInfo(MetroProgressBar progressBar, INotificationMessage notification, ZipFile zipFile)
            {
                this.Notification = notification;
                this.ProgressBar = progressBar;
                this.ZipFile = zipFile;
                AbortZipExtraction = false;
            }
        }
        
        List<ZipCreateInfo> zipCreator = new List<ZipCreateInfo>();
        /// <summary>
        /// Creates Lively zip file for already extracted wp's in Library.
        /// </summary>
        private async void MenuItem_CreateZip_Click(object sender, RoutedEventArgs e)
        {
            if (wallpapersLV.SelectedIndex == -1)
                return;

            string savePath = "";
            SaveFileDialog saveFileDialog1 = new SaveFileDialog()
            {
                Title = "Select location to save the file",
                Filter = "Lively/zip file|*.zip"
            };

            if (saveFileDialog1.ShowDialog() == true)
            {
                savePath = saveFileDialog1.FileName;
            }

            if (String.IsNullOrEmpty(savePath))
            {
                return;
            }

            ZipCreateInfo zipInstance = null;
            List<string> folderContents = new List<string>();
            var selection = (TileData)wallpapersLV.SelectedItem;

            string parentDirectory = null;
            if (selection.LivelyInfo.Type == SetupDesktop.WallpaperType.video_stream
                || selection.LivelyInfo.Type == SetupDesktop.WallpaperType.url)
            {
                parentDirectory = selection.LivelyInfoDirectoryLocation;
            }
            else
            {
                parentDirectory = Path.GetDirectoryName(selection.LivelyInfo.FileName);
            }

            //absolute path values in livelyinfo.json, the wallpaper files are outside of lively folder, requires some work..
            if (selection.LivelyInfo.IsAbsolutePath)
            {
                //only single file on disk.
                if (selection.LivelyInfo.Type == SetupDesktop.WallpaperType.video
                    || selection.LivelyInfo.Type == SetupDesktop.WallpaperType.gif)
                {
                    folderContents.Add(selection.LivelyInfo.FileName);
                    //preview gif/thumb, livelyinfo, liveyproperty.json & maybe wp files..
                    folderContents.AddRange(Directory.GetFiles(selection.LivelyInfoDirectoryLocation, "*.*", SearchOption.AllDirectories));
                }
                //no file, online wp.
                else if (selection.LivelyInfo.Type == SetupDesktop.WallpaperType.video_stream
                      || selection.LivelyInfo.Type == SetupDesktop.WallpaperType.url) 
                {
                    //folderContents.Clear();
                }
                // exe, html etc with more files.
                else
                {
                    folderContents.AddRange(Directory.GetFiles(Path.GetDirectoryName(selection.LivelyInfo.FileName), "*.*", SearchOption.AllDirectories));
                    folderContents.AddRange(Directory.GetFiles(selection.LivelyInfoDirectoryLocation, "*.*", SearchOption.AllDirectories));
                }

                if (folderContents.Count != 0)
                {
                    CreateWallpaperAddedFiles w = new CreateWallpaperAddedFiles(folderContents)
                    {
                        Owner = this,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner
                    };
                    w.ShowDialog();

                    if (w.DialogResult.HasValue && w.DialogResult.Value) //ok btn
                    {
                    }
                    else //back btn
                    {
                        folderContents.Clear();
                        return;
                    }
                }
            }
            else // already installed lively wp, just zip the folder.
            {
                folderContents.AddRange(Directory.GetFiles(parentDirectory, "*.*", SearchOption.AllDirectories));
            }

            try
            {
                using (ZipFile zip = new ZipFile(savePath))
                {
                    zip.CompressionLevel = Ionic.Zlib.CompressionLevel.BestCompression;
                    zip.ZipErrorAction = ZipErrorAction.Throw;

                    //lively metadata files..
                    if (selection.LivelyInfo.IsAbsolutePath)
                    {
                        //converting absolute path to relative & saving livelyinfo file.
                        if (SaveData.LoadWallpaperMetaData(Path.GetDirectoryName(selection.LivelyInfo.Thumbnail)))
                        {
                            SaveData.info.IsAbsolutePath = false;
                            try
                            {
                                SaveData.info.Thumbnail = Path.GetFileName(selection.LivelyInfo.Thumbnail);
                            }
                            catch(ArgumentException)
                            {
                                SaveData.info.Thumbnail = null;
                            }
                            try
                            {
                                SaveData.info.Preview = Path.GetFileName(selection.LivelyInfo.Preview);
                            }
                            catch(ArgumentException)
                            {
                                SaveData.info.Preview = null;
                            }

                            if (selection.LivelyInfo.Type == SetupDesktop.WallpaperType.video_stream
                                    || selection.LivelyInfo.Type == SetupDesktop.WallpaperType.url)
                            {
                                //SaveData.info.FileName = SaveData.info.FileName;
                            }
                            else
                            {
                                try
                                {
                                    SaveData.info.FileName = Path.GetFileName(selection.LivelyInfo.FileName);
                                }
                                catch(ArgumentException)
                                {
                                    SaveData.info.FileName = null;
                                }
                            }
                            SaveData.SaveWallpaperMetaData(SaveData.info, App.PathData + "\\tmpdata\\wpdata\\");
                        }
                        else
                        {
                            return;
                        }
                        zip.AddFile(App.PathData + "\\tmpdata\\wpdata\\LivelyInfo.json", "");
                        folderContents.Remove(folderContents.Single(x => Contains(x, "LivelyInfo.json", StringComparison.OrdinalIgnoreCase)));
                    }
                    else
                    {
                        var infoFile = folderContents.Single(x => Contains(x, "LivelyInfo.json", StringComparison.OrdinalIgnoreCase));
                        zip.AddFile(infoFile, "");
                        folderContents.Remove(infoFile);
                    }

                    if (!String.IsNullOrWhiteSpace(selection.LivelyInfo.Thumbnail))
                    {
                        zip.AddFile(selection.LivelyInfo.Thumbnail, "");
                        folderContents.Remove(selection.LivelyInfo.Thumbnail);
                    }

                    if (!String.IsNullOrWhiteSpace(selection.LivelyInfo.Preview))
                    {
                        zip.AddFile(selection.LivelyInfo.Preview, "");
                        folderContents.Remove(selection.LivelyInfo.Preview);
                    }

                    for (int i = 0; i < folderContents.Count; i++)
                    {
                        try
                        {
                            //adding files in root directory of zip, maintaining folder structure.
                            zip.AddFile(folderContents[i], Path.GetDirectoryName(folderContents[i]).Replace(parentDirectory, string.Empty));
                            
                        }
                        catch(Exception ie)
                        {
                            //MessageBox.Show(ie.Message + ": " + folderContents[i]);
                            Logger.Info(ie.Message + ": " + folderContents[i]);
                            WpfNotification(NotificationType.error, Properties.Resources.txtLivelyErrorMsgTitle, ie.Message);
                            break;
                        }
                    }

                    MetroProgressBar progressBar = null;
                    var notification = notify.Manager.CreateMessage()
                        //.Accent("#808080")
                        .Background("#333")
                        .HasHeader(Properties.Resources.txtLivelyWaitMsgTitle)
                        .HasMessage(Properties.Resources.txtCreatingZip + " " + selection.LivelyInfo.Title)
                        .Dismiss().WithButton("Stop", button => { ZipCreationCancel(zip); }) 
                        .WithOverlay(progressBar = new MetroProgressBar
                        {
                            Minimum = 0,
                            Maximum = 100,
                            VerticalAlignment = VerticalAlignment.Bottom,
                            HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch,
                                //Height = 0.5f,
                                //BorderThickness = new Thickness(0),
                                //Foreground = new SolidColorBrush(Color.FromArgb(128, 255, 255, 255)),
                                Background = Brushes.Transparent,
                            IsIndeterminate = false,
                            IsHitTestVisible = false,

                        })
                        .Queue();
                    zipCreator.Add(zipInstance = new ZipCreateInfo(progressBar, notification, zip));

                    zip.SaveProgress += Zip_SaveProgress;
                    await Task.Run(() => zip.Save());
                }
            }
            catch (Ionic.Zip.ZipException e1)
            {
                WpfNotification(NotificationType.error, Properties.Resources.txtLivelyErrorMsgTitle, e1.Message);
                Logger.Error(e1.ToString());
            }
            catch (Exception e2)
            {
                WpfNotification(NotificationType.error, Properties.Resources.txtLivelyErrorMsgTitle, e2.Message);
                Logger.Error(e2.ToString());
            }
            finally
            {
                if (zipInstance != null)
                {
                    if (zipInstance.Notification != null)
                        notify.Manager.Dismiss(zipInstance.Notification);

                    if (zipInstance.AbortZipExtraction)
                    {
                        //ionic zip deletes the file when aborted, nothing to do here.
                    }
                    else
                    {
                        try
                        {
                            ProcessStartInfo startInfo = new ProcessStartInfo
                            {
                                Arguments = "\"" + Path.GetDirectoryName(savePath) + "\"",
                                FileName = "explorer.exe"
                            };
                            Process.Start(startInfo);
                        }
                        catch { }
                    }
                    zipCreator.Remove(zipInstance);
                }
            }

        }

        private void Zip_SaveProgress(object sender, SaveProgressEventArgs e)
        {
            var zip = (ZipFile)sender;
            var obj = zipCreator.Find(x => x.ZipFile.Equals(zip));
            if (obj == null)
                return;

            if(obj.AbortZipExtraction)
            {
                e.Cancel = true;
                return;
            }
            //if (zipWasCanceled) { e.Cancel = true; }

            if (e.EntriesTotal != 0)
            {

                if(obj.ProgressBar != null)
                {
                    this.Dispatcher.Invoke(() => {
                        obj.ProgressBar.Value = ((float)e.EntriesSaved / (float)e.EntriesTotal) * 100f;
                    });
                }

                if (e.EntriesSaved == e.EntriesTotal && e.EntriesTotal != 0) //completion
                {

                }
            }
        }

        private void ZipCreationCancel(ZipFile zip)
        {
            var obj = zipCreator.Find(x => x.ZipFile.Equals(zip));
            if (obj == null)
                return;

            obj.AbortZipExtraction = true;
        }

        /// <summary>
        /// String Contains method with StringComparison property.
        /// </summary>
        /// <param name="str"></param>
        /// <param name="substring"></param>
        /// <param name="comp"></param>
        /// <returns></returns>
        public static bool Contains(String str, String substring,
                                    StringComparison comp)
        {
            if (substring == null)
                throw new ArgumentNullException("substring",
                                             "substring cannot be null.");
            else if (!Enum.IsDefined(typeof(StringComparison), comp))
                throw new ArgumentException("comp is not a member of StringComparison",
                                         "comp");

            return str.IndexOf(substring, comp) >= 0;
        }

        private async void MenuItem_DeleteWallpaper_Click(object sender, RoutedEventArgs e)
        {
            if (wallpapersLV.SelectedIndex == -1)
                return;
            var selection = (TileData)wallpapersLV.SelectedItem;

            if (selection.LivelyInfo.IsAbsolutePath != true)
            {
                var ch = await this.ShowMessageAsync(Properties.Resources.msgDeleteConfirmationTitle, Properties.Resources.msgDeleteConfirmation, 
                              MessageDialogStyle.AffirmativeAndNegative,new MetroDialogSettings() { AffirmativeButtonText = "Yes", NegativeButtonText = "No", DialogTitleFontSize = 18,
                               ColorScheme = MetroDialogColorScheme.Inverted, DialogMessageFontSize = 16, AnimateShow = false, AnimateHide = false });

                if (ch == MessageDialogResult.Negative)
                    return;
                else if (ch == MessageDialogResult.Affirmative)
                {

                }
            }

            if (selection.LivelyInfo.IsAbsolutePath)
            {
                //since original file is not deleted, safe to continue.
            } 
            else
            {
                //check if wp is running, if so abort!
                if (SetupDesktop.wallpapers.FindIndex(x => x.FilePath.Equals(selection.LivelyInfo.FileName, StringComparison.OrdinalIgnoreCase)) != -1)
                {
                    await this.ShowMessageAsync(Properties.Resources.msgDeletionFailureTitle, Properties.Resources.msgDeletionFailure, MessageDialogStyle.Affirmative,
                        new MetroDialogSettings() { AnimateHide = false, AnimateShow = false});
                    return;
                }
            }

            selectedTile.Remove(selection);
            tileDataList.Remove(selection);
            wallpapersLV.SelectedIndex = -1; //clears selectedTile info panel.

            FileOperations.DeleteDirectoryAsync(selection.LivelyInfoDirectoryLocation);
            try
            {   
                //Delete LivelyProperties.info copy folder.
                string[] wpdataDir = Directory.GetDirectories(Path.Combine(App.PathData, "SaveData", "wpdata"));
                var wpFolderName = new System.IO.DirectoryInfo(selection.LivelyInfoDirectoryLocation).Name;
                for (int i = 0; i < wpdataDir.Length; i++)
                {
                    var item = new System.IO.DirectoryInfo(wpdataDir[i]).Name;
                    if (wpFolderName.Equals(item, StringComparison.Ordinal))
                    {
                        FileOperations.DeleteDirectoryAsync(wpdataDir[i]);
                        break;
                    }
                }
            }
            catch(Exception ex)
            {
                Logger.Error(ex.ToString());
            }
        }

        #endregion wallpaper_library

        #region systray
        private static System.Windows.Forms.NotifyIcon _notifyIcon;
        private static bool _isExit;

        private void CreateSysTray()
        {
            //NotifyIcon Fix: https://stackoverflow.com/questions/28833702/wpf-notifyicon-crash-on-first-run-the-root-visual-of-a-visualtarget-cannot-hav/29116917
            //Rarely I get this error "The root Visual of a VisualTarget cannot have a parent..", hard to pinpoint not knowing how to recreate the error.
            System.Windows.Controls.ToolTip tt = new System.Windows.Controls.ToolTip();
            tt.IsOpen = true;
            tt.IsOpen = false;

            _notifyIcon = new System.Windows.Forms.NotifyIcon();
            _notifyIcon.DoubleClick += (s, args) => ShowMainWindow();
            _notifyIcon.Icon = Properties.Icons.icons8_seed_of_life_96_normal;

            CreateContextMenu();
            _notifyIcon.Visible = true;
        }

        System.Windows.Forms.ToolStripMenuItem update_traybtn, pause_traybtn, configure_traybtn;
        //private bool playpauseToggle = false;
        private void CreateContextMenu()
        {
            _notifyIcon.ContextMenuStrip =
              new System.Windows.Forms.ContextMenuStrip();
            _notifyIcon.Text = Properties.Resources.txtTitlebar;

            _notifyIcon.ContextMenuStrip.Items.Add(Properties.Resources.txtContextMenuOpenLively, Properties.Icons.icon_monitor).Click += (s, e) => ShowMainWindow();
            _notifyIcon.ContextMenuStrip.Items.Add(Properties.Resources.txtContextMenuCloseAll, Properties.Icons.icon_erase).Click += (s, e) => SetupDesktop.CloseAllWallpapers();
            update_traybtn = new System.Windows.Forms.ToolStripMenuItem(Properties.Resources.txtContextMenuUpdate1, Properties.Icons.icon_update);
            //update_traybtn.Click += (s, e) => Process.Start("https://github.com/rocksdanister/lively");
            update_traybtn.Click += (s,e) => ShowLivelyUpdateWindow();
            update_traybtn.Enabled = false;

            //todo:- store a "state" in setupdesktop, maintain that state even after wp change. (also checkmark this menu if paused)
            pause_traybtn = new System.Windows.Forms.ToolStripMenuItem("Pause All Wallpapers", Properties.Icons.icons8_pause_30);
            pause_traybtn.Click += (s, e) => ToggleWallpaperPlaybackState();
            _notifyIcon.ContextMenuStrip.Items.Add(pause_traybtn);

            configure_traybtn = new System.Windows.Forms.ToolStripMenuItem("Customize Wallpaper", Properties.Icons.gear_color_48);
            configure_traybtn.Click += (s, e) => ShowCustomiseWidget();
            _notifyIcon.ContextMenuStrip.Items.Add(configure_traybtn);

            _notifyIcon.ContextMenuStrip.Items.Add("-");
            _notifyIcon.ContextMenuStrip.Items.Add(update_traybtn);

            _notifyIcon.ContextMenuStrip.Items.Add("-");

            _notifyIcon.ContextMenuStrip.Items.Add(Properties.Resources.txtSupport, Properties.Icons.icons8_heart_outline_16).Click += (s, e) => Hyperlink_SupportPage(null,null) ;
            _notifyIcon.ContextMenuStrip.Items.Add("-");
            _notifyIcon.ContextMenuStrip.Items.Add(Properties.Resources.txtContextMenuExit, Properties.Icons.icon_close).Click += (s, e) => ExitApplication();
        }

        private static void ToggleWallpaperPlaybackState()
        {
            if (App.W != null)
            {
                if (SetupDesktop.GetEngineState() == SetupDesktop.EngineState.normal)
                {
                    SetupDesktop.SetEngineState(SetupDesktop.EngineState.paused);
                    App.W.pause_traybtn.Checked = true;
                }
                else
                {
                    SetupDesktop.SetEngineState(SetupDesktop.EngineState.normal);
                    App.W.pause_traybtn.Checked = false;
                }
            }
        }

        public static void SwitchTrayIcon(bool isPaused)
        {
            try
            {
                //don't make much sense with per-display rule in multiple display systems, so turning off.
                if ( (!Multiscreen || SaveData.config.WallpaperArrangement == WallpaperArrangement.span) && !_isExit)
                {
                    if (isPaused)
                    {
                        _notifyIcon.Icon = Properties.Icons.icons8_seed_of_life_96_pause;
                    }
                    else
                    {
                        _notifyIcon.Icon = Properties.Icons.icons8_seed_of_life_96_normal;
                    }
                }
            }
            catch (NullReferenceException)
            {
                //app closing.
            }
        }

        private int prevSelectedLibIndex = -1;
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!_isExit)
            {
                e.Cancel = true;
                //show notification regarding app minimized status.
                if (SaveData.config.IsFirstRun)
                {
                    Dialogues.HelpWindow w = new Dialogues.HelpWindow(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "docs","help_vid_2.mp4"))
                    {
                        Owner = this,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner
                    };
                    w.Show();
                    _notifyIcon.ShowBalloonTip(3000, "Lively",Properties.Resources.toolTipMinimizeMsg,ToolTipIcon.None);

                    SaveData.config.IsFirstRun = false;
                    SaveData.SaveConfig();
                }

                prevSelectedLibIndex = wallpapersLV.SelectedIndex;
                tileDataList.Clear();
                selectedTile.Clear();

                this.Hide();
                //testing
                GC.Collect();
            }
            else
            {
                //static event, otherwise memory leak.
                SystemEvents.DisplaySettingsChanged -= SystemEvents_DisplaySettingsChanged;

                SaveData.config.SafeShutdown = true;
                SaveData.SaveConfig();

                SetupDesktop.CloseAllWallpapers(true);
         
                SetupDesktop.RefreshDesktop();

                //systraymenu dispose
                _notifyIcon.Visible = false;
                _notifyIcon.Icon.Dispose();
                //_notifyIcon.Icon = null;
                _notifyIcon.Dispose();
            }
        }

        public void ExitApplication()
        {
            _isExit = true;
            System.Windows.Application.Current.Shutdown();
            //this.Close(); // MainWindow_Closing() handles the exit actions.
        }

        public async void ShowMainWindow()
        {
            //TODO:- add error if waitin to kill browser process, onclose runnin and user call this fn.
            if (App.W.IsVisible)//this.IsVisible)
            {
                if (App.W.WindowState == WindowState.Minimized)
                {
                    App.W.WindowState = WindowState.Normal;
                }
                App.W.Activate();
            }
            else
            {
                UpdateWallpaperLibrary();
                App.W.Show();
                //App.w.Activate();

            }
        }

        #endregion systray

        #region wp_setup
        /// <summary>
        /// Sets up wallpaper, shows dialog to select display if multiple displays are detected.
        /// </summary>
        /// <param name="path">wallpaper location.</param>
        /// <param name="type">wallpaper category.</param>
        private async void SetupWallpaper(string path, SetupDesktop.WallpaperType type, string args = null, bool showAddWallpaperWindow = false)
        {
            if(_isRestoringWallpapers)
            {
                //_ = Task.Run(() => (MessageBox.Show(Properties.Resources.msgRestoringInProgress, Properties.Resources.txtLivelyWaitMsgTitle, MessageBoxButton.OK, MessageBoxImage.Information)));
                WpfNotification(NotificationType.info, Properties.Resources.txtLivelyWaitMsgTitle, Properties.Resources.msgRestoringInProgress);
                return;
            }

            if ( !(File.Exists(path) || type == SetupDesktop.WallpaperType.video_stream || type == SetupDesktop.WallpaperType.url) )
            {
                //_ = Task.Run(() => (MessageBox.Show("File missing on disk!\n" + path, Properties.Resources.txtLivelyErrorMsgTitle, MessageBoxButton.OK , MessageBoxImage.Error)));
                WpfNotification(NotificationType.error, Properties.Resources.txtLivelyErrorMsgTitle, Properties.Resources.toolTipWallpaperSkip + "\n" + path);
                return;
            }

            SaveData.WallpaperLayout tmpData = new SaveData.WallpaperLayout();
            tmpData.Arguments = args;

            if (type == SetupDesktop.WallpaperType.app && args == null)
            {
                var arg = await this.ShowInputAsync(Properties.Resources.msgAppCommandLineArgsTitle, Properties.Resources.msgAppCommandLineArgs, new MetroDialogSettings()
                { DialogTitleFontSize = 16, DialogMessageFontSize = 14, AnimateShow = false, AnimateHide = false });
                if (arg == null) //cancel btn or ESC key
                    return;

                if (!string.IsNullOrWhiteSpace(arg))
                    tmpData.Arguments = arg;
            }
            else if (type == SetupDesktop.WallpaperType.web || type == SetupDesktop.WallpaperType.url || type == SetupDesktop.WallpaperType.web_audio)
            {
                if (HighContrastFix)
                {
                    Logger.Info("behind-icon mode, skipping cef.");
                    _ = Task.Run(() => (MessageBox.Show("Web wallpaper is not available in High Contrast mode workaround, coming soon.", Properties.Resources.txtLivelyErrorMsgTitle)));
                    return;
                }

                if (!File.Exists(App.PathData + "\\external\\cef\\LivelyCefSharp.exe"))
                {
                    Logger.Info("cefsharp is missing, skipping wallpaper.");
                    //_ = Task.Run(() => (MessageBox.Show(Properties.Resources.msgWebBrowserMissing, Properties.Resources.txtLivelyErrorMsgTitle, 
                    //                                                                                            MessageBoxButton.OK, MessageBoxImage.Information)));
                    WpfNotification(NotificationType.info, Properties.Resources.txtLivelyErrorMsgTitle, Properties.Resources.msgWebBrowserMissing);
                    return;
                }
            }
            else if ((type == SetupDesktop.WallpaperType.video && SaveData.config.VidPlayer == VideoPlayer.mpv))
            {
                if (!File.Exists(App.PathData + "\\external\\mpv\\mpv.exe"))
                {
                    //_ = Task.Run(() => (MessageBox.Show("mpv player missing!\nwww.github.com/rocksdanister/lively/wiki/Video-Guide", Properties.Resources.txtLivelyErrorMsgTitle, 
                    //                                                                                                MessageBoxButton.OK, MessageBoxImage.Information)));
                    WpfNotification(NotificationType.infoUrl, Properties.Resources.txtLivelyErrorMsgTitle, "mpv player missing!", "https://www.github.com/rocksdanister/lively/wiki/Video-Guide");
                    return;
                }
            }
            else if (type == SetupDesktop.WallpaperType.video_stream)
            {
                if (!File.Exists(App.PathData + "\\external\\mpv\\mpv.exe") || !File.Exists(App.PathData + "\\external\\mpv\\youtube-dl.exe"))
                {
                    WpfNotification(NotificationType.infoUrl, Properties.Resources.txtLivelyErrorMsgTitle, "mpv player/youtube-dl missing!", "https://github.com/rocksdanister/lively/wiki/Youtube-Wallpaper");
                    return;
                }
            }

            if (type == SetupDesktop.WallpaperType.video_stream)
            {
                tmpData.Arguments = YoutubeDLArgGenerate(path);
            }

            //if previously running or cancelled, waiting to end.
            CancelWallpaperWaiting();
            while (SetupDesktop.IsProcessWaitDone() == 0)
            {
                await Task.Delay(1);
            }

            MetroProgressBar progressBar = null;
            isProcessWaitCancelled = false;
            INotificationMessage notification = null;
            if (Multiscreen && SaveData.config.WallpaperArrangement == WallpaperArrangement.duplicate)
            {
                List<WallpaperLayout> tmp = new List<WallpaperLayout>();
                foreach (var item in Screen.AllScreens)
                {
                    tmp.Add(new WallpaperLayout() { Arguments = tmpData.Arguments, DeviceName = item.DeviceName, FilePath = path, Type = type });
                }

                foreach (var item in tmp)
                {
                    Logger.Info("Duplicating wp(s):" + item.FilePath + " " + item.DeviceName);
                }
                RestoreWallpaper(tmp);
                return;
            }
            else if (Multiscreen && SaveData.config.WallpaperArrangement == WallpaperArrangement.per)
            {   
                //monitor select dialog
                DisplaySelectWindow displaySelectWindow = new DisplaySelectWindow
                {
                    Owner = this,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };
                displaySelectWindow.ShowDialog();

                if (DisplaySelectWindow.selectedDisplay == null) //none
                {
                    return;
                }
                else
                {
                    tmpData.FilePath = path;
                    tmpData.Type = type;

                    tmpData.DeviceName = DisplaySelectWindow.selectedDisplay;

                    //remove prev if new wallpaper on same screen
                    int i = 0;
                    if ((i = SetupDesktop.wallpapers.FindIndex(x => x.DeviceName == tmpData.DeviceName)) != -1)
                    {
                        SetupDesktop.CloseWallpaper(SetupDesktop.wallpapers[i].DeviceName);
                    }
                }
            }
            else //single screen
            {
                tmpData.FilePath = path;
                tmpData.Type = type;
                //tmpData.displayID = 0;
                tmpData.DeviceName = Screen.PrimaryScreen.DeviceName;

                SetupDesktop.CloseAllWallpapers(); //close previous wallpapers.
            }

            //progressbar
            if (type == SetupDesktop.WallpaperType.app 
                || type == SetupDesktop.WallpaperType.unity 
                || type == SetupDesktop.WallpaperType.bizhawk 
                || type == SetupDesktop.WallpaperType.godot 
                || type == SetupDesktop.WallpaperType.unity_audio
                || type == SetupDesktop.WallpaperType.video_stream
                //|| SaveData.config.VidPlayer == VideoPlayer.mpv   //should be fast enough, no need for progressdialog..youtube parsing is apptype       
                )
            {
                /*
                progressController = await this.ShowProgressAsync(Properties.Resources.txtLivelyWaitMsgTitle, Properties.Resources.msgLoadingAppWallpaper, true,
                     new MetroDialogSettings() { AnimateHide = true, AnimateShow = false });
                //progressController.Canceled += ProgressController_Canceled;
                */
                notification = notify.Manager.CreateMessage()
                    .Accent("#FF0000")
                    .Background("#333")
                    .HasHeader(Properties.Resources.txtLivelyWaitMsgTitle)
                    .HasMessage(Properties.Resources.msgLoadingAppWallpaper)
                    .Dismiss().WithButton("Stop", button => { CancelWallpaperWaiting(); })
                    .WithOverlay(progressBar = new MetroProgressBar
                    {
                        Minimum = 0,
                        Maximum = 100,
                        VerticalAlignment = VerticalAlignment.Bottom,
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch,
                                //Height = 0.5f,
                                //BorderThickness = new Thickness(0),
                                //Foreground = new SolidColorBrush(Color.FromArgb(128, 255, 255, 255)),
                                Background = Brushes.Transparent,
                        IsIndeterminate = false,
                        IsHitTestVisible = false,

                    })
                    .Queue();
            }

            Logger.Info("Setting up wallpaper:-" + tmpData.FilePath);
            SetupDesktop.SetWallpaper(tmpData, !showAddWallpaperWindow); //set wallpaper

            float progress = 0;

            while (SetupDesktop.IsProcessWaitDone() == 0)
            {
                if(progressBar != null)
                {
                    if (progress > 100)
                        progressBar.Value = 100 ;

                    progressBar.Value = progress;
                    progress += (100f / SetupDesktop.wallpaperWaitTime)*100f; //~approximation
                    
                    if (isProcessWaitCancelled)
                    {
                        break;
                    }
                }
                await Task.Delay(50);
            }
            if (progressController != null)
            {
                progressController.SetProgress(1);
                //progressController.Canceled -= ProgressController_Canceled;
                await progressController.CloseAsync();
                progressController = null;
            }
            
            if(progressBar != null)
            {
                if (notification != null)
                {
                    notify.Manager.Dismiss(notification);
                }
            }
            
        }

        private bool isProcessWaitCancelled = false;
        private void CancelWallpaperWaiting()
        {
            SetupDesktop.TaskProcessWaitCancel();
            isProcessWaitCancelled = true;
        }

        private bool isProcessRestoreCancelled = false;
        private void CancelWallpaperRestoring()
        {
            SetupDesktop.TaskProcessWaitCancel();
            isProcessRestoreCancelled = true;
        }

        /// <summary>
        /// Restores saved list of wallpapers. (no dialog asking user for input etc compared to setupdesktop());
        /// </summary>
        /// <param name="layout"></param>
        private async void RestoreWallpaper(List<SaveData.WallpaperLayout> layoutList)
        {
            if (_isRestoringWallpapers)
                return;

            isProcessRestoreCancelled = false;
            float progress = 0;
            int loadedWallpaperCount = 0;
            _isRestoringWallpapers = true;

            MetroProgressBar progressBar = null;
            var notification = notify.Manager.CreateMessage()
                .Accent("#FF0000")
                .Background("#333")
                .HasHeader(Properties.Resources.txtLivelyWaitMsgTitle)
                .HasMessage(Properties.Resources.msgLoadingAppWallpaper)
                .Dismiss().WithButton("Stop", button => { CancelWallpaperWaiting(); })
                .WithOverlay(progressBar = new MetroProgressBar
                {
                    Minimum = 0,
                    Maximum = 100,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch,
                    //Height = 0.5f,
                    //BorderThickness = new Thickness(0),
                    //Foreground = new SolidColorBrush(Color.FromArgb(128, 255, 255, 255)),
                    Background = Brushes.Transparent,
                    IsIndeterminate = false,
                    IsHitTestVisible = false,

                })
                .Queue();

            foreach (var layout in layoutList)
            {
                if (layout.Type == SetupDesktop.WallpaperType.web || layout.Type == SetupDesktop.WallpaperType.url || layout.Type == SetupDesktop.WallpaperType.web_audio)
                {
                    if (HighContrastFix)
                    {
                        Logger.Info("behind-icon mode, skipping cef.");
                        _ = Task.Run(() => (MessageBox.Show("Web wallpaper is not available in High Contrast mode workaround, coming soon.", "Lively: Error, High Contrast Mode")));
                        continue;
                    }

                    if (!File.Exists(Path.Combine(App.PathData + "\\external\\cef\\LivelyCefSharp.exe")))
                    {
                        Logger.Info("cefsharp is missing, skipping wallpaper.");
                        //_ = Task.Run(() => (MessageBox.Show(Properties.Resources.msgWebBrowserMissing, Properties.Resources.txtLivelyErrorMsgTitle)));
                        WpfNotification(NotificationType.error, Properties.Resources.txtLivelyErrorMsgTitle, Properties.Resources.msgWebBrowserMissing);
                        continue;
                    }
                }
                else if (layout.Type == SetupDesktop.WallpaperType.video && SaveData.config.VidPlayer == VideoPlayer.mpv)
                {
                    if (!File.Exists(App.PathData + "\\external\\mpv\\mpv.exe"))
                    {
                        //_ = Task.Run(() => (MessageBox.Show("mpv player missing!\nwww.github.com/rocksdanister/lively/wiki/Video-Guide", Properties.Resources.txtLivelyErrorMsgTitle)));
                        WpfNotification(NotificationType.infoUrl, Properties.Resources.txtLivelyErrorMsgTitle, "mpv player missing!", "https://www.github.com/rocksdanister/lively/wiki/Video-Guide");
                        continue;
                    }
                }
                else if (layout.Type == SetupDesktop.WallpaperType.video_stream)
                {
                    if (!File.Exists(App.PathData + "\\external\\mpv\\mpv.exe") || !File.Exists(App.PathData + "\\external\\mpv\\youtube-dl.exe"))
                    {
                        WpfNotification(NotificationType.infoUrl, Properties.Resources.txtLivelyErrorMsgTitle, "mpv player/youtube-dl missing!", "https://github.com/rocksdanister/lively/wiki/Youtube-Wallpaper");
                        continue;
                    }
                }

                SaveData.WallpaperLayout tmpData = new SaveData.WallpaperLayout();
                if (Multiscreen && (SaveData.config.WallpaperArrangement == WallpaperArrangement.per || SaveData.config.WallpaperArrangement == WallpaperArrangement.duplicate))
                {
                    tmpData.FilePath = layout.FilePath;
                    tmpData.Type = layout.Type;

                    if (layout.Type == SetupDesktop.WallpaperType.video_stream)
                    {
                        tmpData.Arguments = YoutubeDLArgGenerate(layout.FilePath);
                    }
                    else
                        tmpData.Arguments = layout.Arguments;

                    tmpData.DeviceName = layout.DeviceName;

                    //remove prev if new wallpaper on same screen, in restore case this can happen if savefile is messed up or user just messed with it?
                    int i = 0;
                    if ((i = SetupDesktop.wallpapers.FindIndex(x => x.DeviceName == tmpData.DeviceName)) != -1)
                    {
                        SetupDesktop.CloseWallpaper(SetupDesktop.wallpapers[i].DeviceName);
                    }
                }
                else //single screen
                {
                    tmpData.FilePath = layout.FilePath;
                    tmpData.Type = layout.Type;
                    tmpData.DeviceName = Screen.PrimaryScreen.DeviceName;

                    if (layout.Type == SetupDesktop.WallpaperType.video_stream)
                    {
                        tmpData.Arguments = YoutubeDLArgGenerate(layout.FilePath);
                    }
                    else
                        tmpData.Arguments = layout.Arguments;

                    SetupDesktop.CloseAllWallpapers(); //close previous wallpapers.
                }

                Logger.Info("Setting up wallpaper:-" + tmpData.FilePath);
                SetupDesktop.SetWallpaper(tmpData, false); //set wallpaper
                loadedWallpaperCount++;


                progressBar.Value = progress * 100;
                progress += (float)loadedWallpaperCount / (float)layoutList.Count;
                //SetupDesktop.wallpapers.Add(tmpData);
                while (SetupDesktop.IsProcessWaitDone() == 0)
                {
                    await Task.Delay(50);
                    if (isProcessRestoreCancelled)
                    {
                        break;
                    }
                }
                
                if (isProcessRestoreCancelled)
                { 
                    break;
                }
            }

            _isRestoringWallpapers = false;
            layoutList.Clear();
            layoutList = null;

            notify.Manager.Dismiss(notification);
        }
        #endregion wp_setup

        #region wallpaper_installer

        class ZipInstallInfo
        {
            public MetroProgressBar ProgressBar { get; private set; }
            public INotificationMessage Notification { get; private set; }
            public ZipFile ZipFile { get; private set; }
            public bool AbortZipExtraction { get; set; }
            public ZipInstallInfo(MetroProgressBar progressBar, INotificationMessage notification, ZipFile zipFile)
            {
                this.Notification = notification;
                this.ProgressBar = progressBar;
                this.ZipFile = zipFile;
                AbortZipExtraction = false;
            }
        }

        List<ZipInstallInfo> zipInstaller = new List<ZipInstallInfo>();
        private async void WallpaperInstaller(string zipLocation)
        {
            ZipInstallInfo zipInstance = null;
            string randomFolderName = Path.GetRandomFileName();
            string extractPath = null;
            extractPath = App.PathData + "\\wallpapers\\" + randomFolderName;

            //Todo: implement CheckZip() {thread blocking}, Error will be thrown during extractiong, which is being handled so not a big deal.
            //Ionic.Zip.ZipFile.CheckZip(zipLocation)

            if (Directory.Exists(extractPath)) //likely impossible.
            {
                Debug.WriteLine("same foldername with files, should be impossible... retrying with new random foldername");
                extractPath = App.PathData + "\\wallpapers\\" + Path.GetRandomFileName();

                if (Directory.Exists(extractPath))
                {
                    Logger.Error("same folderpath name, stopping wallpaper installation");
                    return;
                }
            }
            Directory.CreateDirectory(extractPath);

            string zipPath = zipLocation;
            // Normalizes the path.
            extractPath = Path.GetFullPath(extractPath);

            // Ensures that the last character on the extraction path
            // is the directory separator char. 
            // Without this, a malicious zip file could try to traverse outside of the expected
            // extraction path.
            if (!extractPath.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
                extractPath += Path.DirectorySeparatorChar;

            try
            {
                // Specifying Console.Out here causes diagnostic msgs to be sent to the Console
                // In a WinForms or WPF or Web app, you could specify nothing, or an alternate
                // TextWriter to capture diagnostic messages.

                //var options = new ReadOptions { StatusMessageWriter = System.Console.Out };
                using (ZipFile zip = ZipFile.Read(zipPath))//, options))
                {
                    zip.ZipErrorAction = ZipErrorAction.Throw; //todo:- test with a corrupted zip that starts extracting.
                    //zip.ZipError += Zip_ZipError;
                    // This call to ExtractAll() assumes:
                    //   - none of the entries are password-protected.
                    //   - want to extract all entries to current working directory
                    //   - none of the files in the zip already exist in the directory;
                    //     if they do, the method will throw.
                    if (zip.ContainsEntry("LivelyInfo.json")) //outer directory only.
                    {
                        MetroProgressBar progressBar = null;
                        var notification = notify.Manager.CreateMessage()
                            //.Accent("#808080")
                            .Background("#333")
                            .HasHeader(Properties.Resources.txtLivelyWaitMsgTitle)
                            .HasMessage(Properties.Resources.txtLabel39 +" " + Path.GetFileName(zipLocation))
                            .Dismiss().WithButton("Stop", button => { Zip_ExtractCancel(zip); }) //HOW TO CANCEL?
                            .WithOverlay(progressBar = new MetroProgressBar
                            {
                                Minimum = 0,
                                Maximum = 100,
                                VerticalAlignment = VerticalAlignment.Bottom,
                                HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch,
                                //Height = 0.5f,
                                //BorderThickness = new Thickness(0),
                                //Foreground = new SolidColorBrush(Color.FromArgb(128, 255, 255, 255)),
                                Background = Brushes.Transparent,
                                IsIndeterminate = false,
                                IsHitTestVisible = false,

                            })
                            .Queue();

                         zipInstaller.Add(zipInstance = new ZipInstallInfo(progressBar, notification, zip));

                        //progressController = await this.ShowProgressAsync(Properties.Resources.txtLivelyWaitMsgTitle, Properties.Resources.txtLabel39, true);
                        zip.ExtractProgress += Zip_ExtractProgress;
                        //zip.ExtractAll(extractPath);
                        await Task.Run(() => zip.ExtractAll(extractPath));

                    }
                    else
                    {
                        try
                        {
                            Directory.Delete(extractPath, true);
                        }
                        catch 
                        {
                            Logger.Error("Extractionpath delete error");
                        }

                        notify.Manager.CreateMessage()
                        .Accent("#FF0000")
                        .HasBadge("Warn")
                        .Background("#333")
                        .HasHeader(Properties.Resources.txtLivelyErrorMsgTitle)
                        .HasMessage("Not Lively wallpaper .zip file.")
                        .Dismiss().WithButton("Ok", button => { })
                        .Queue();

                        //await this.ShowMessageAsync("Error", "Not Lively wallpaper file.\nCheck out wiki page on how to create proper wallpaper file", MessageDialogStyle.Affirmative,
                        //    new MetroDialogSettings() { DialogTitleFontSize = 25, ColorScheme = MetroDialogColorScheme.Inverted, DialogMessageFontSize = 16 });
                    }
                }
            }
            catch (Ionic.Zip.ZipException e)
            {
                try
                {
                    Directory.Delete(extractPath, true);
                }
                catch 
                {
                    Logger.Error("Extractionpath delete error");
                }

                Logger.Error(e.ToString());
                WpfNotification(NotificationType.error, Properties.Resources.txtLivelyErrorMsgTitle, Properties.Resources.msgDamangedLivelyFile +"\n" + e.Message);
            }
            catch (Exception ex)
            {
                try
                {
                    Directory.Delete(extractPath, true);
                }
                catch 
                {
                    Logger.Error("Extractionpath delete error");
                }
                Logger.Error(ex.ToString());
                WpfNotification(NotificationType.error, Properties.Resources.txtLivelyErrorMsgTitle, ex.Message);
            }
            finally
            {
                if (zipInstance != null)
                {
                    if(zipInstance.Notification != null)
                        notify.Manager.Dismiss(zipInstance.Notification);
         
                    if (zipInstance.AbortZipExtraction)
                    {
                        try
                        {
                            Directory.Delete(extractPath, true);
                        }
                        catch
                        {
                            Logger.Error("Extractionpath delete error (Aborted)");
                        }
                    }
                    zipInstaller.Remove(zipInstance);
                }
            }

            UpdateWallpaperLibrary();
            //selecting installed wp..
            foreach (var item in tileDataList)
            {
                if(item.LivelyInfoDirectoryLocation.Contains(randomFolderName))
                {
                    wallpapersLV.SelectedItem = item;
                    break;
                }
            }
        }

        private void Zip_ExtractCancel(ZipFile zip)
        {
            var obj = zipInstaller.Find(x => x.ZipFile.Equals(zip));
            if (obj == null)
                return;

            obj.AbortZipExtraction = true;
        }

        private async void Zip_ExtractProgress(object sender, ExtractProgressEventArgs e)
        {
            var zip = (ZipFile)sender;
            var obj = zipInstaller.Find(x => x.ZipFile.Equals(zip));
            if (obj == null)
                return;

            if(obj.AbortZipExtraction)
            {
                e.Cancel = true;
                return;
            }

            if (e.EntriesTotal != 0)
            {
                if(obj.ProgressBar != null)
                {
                    this.Dispatcher.Invoke(() => {
                        obj.ProgressBar.Value = ((float)e.EntriesExtracted / (float)e.EntriesTotal) * 100f;
                    });
                }

            }

            if(e.EntriesExtracted == e.EntriesTotal && e.EntriesTotal != 0)
            {
                //completion.
            }
        }

        public void Button_Click_InstallWallpaper(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog()
            {
                Title = "Open Lively Wallpaper File",
                Filter = "Lively Wallpaper (*.zip) |*.zip"
            };

            if (openFileDialog1.ShowDialog() == true)
            {
                if (tabControl1.SelectedIndex != 0) //switch to library tab.)
                    tabControl1.SelectedIndex = 0;
                WallpaperInstaller(openFileDialog1.FileName);
            }
        }

        /// <summary>
        /// Calculates SHA256 hash of file.
        /// </summary>
        /// <param name="filepath">path to the file.</param>
        string CalculateFileCheckSum(string filepath)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                using (var stream = File.OpenRead(filepath))
                {
                    var hash = sha256.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }

        #endregion wallpaper_installer

        #region tile_events

        private void Tile_Video_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog
            {
                Filter = "All Videos Files |*.dat; *.wmv; *.3g2; *.3gp; *.3gp2; *.3gpp; *.amv; *.asf;  *.avi; *.bin; *.cue; *.divx; *.dv; *.flv; *.gxf; *.iso; *.m1v; *.m2v; *.m2t; *.m2ts; *.m4v; " +
                  " *.mkv; *.mov; *.mp2; *.mp2v; *.mp4; *.mp4v; *.mpa; *.mpe; *.mpeg; *.mpeg1; *.mpeg2; *.mpeg4; *.mpg; *.mpv2; *.mts; *.nsv; *.nuv; *.ogg; *.ogm; *.ogv; *.ogx; *.ps; *.rec; *.rm; *.rmvb; *.tod; *.ts; *.tts; *.vob; *.vro; *.webm"
            };

            if (openFileDialog1.ShowDialog() == true)
            {
                SetupWallpaper(openFileDialog1.FileName, SetupDesktop.WallpaperType.video);
            }

        }

        private void Tile_GIF_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog()
            {
                Filter = "Animated GIF (*.gif) |*.gif"
            };

            if (openFileDialog1.ShowDialog() == true)
            {
                SetupWallpaper(openFileDialog1.FileName, SetupDesktop.WallpaperType.gif);
            }

        }

        private async void Tile_Unity_Click(object sender, RoutedEventArgs e)
        {
            if (SaveData.config.WarningUnity == 0)
            {
                var ch = await this.ShowMessageAsync(Properties.Resources.msgExternalAppWarningTitle, Properties.Resources.msgExternalAppWarning, MessageDialogStyle.AffirmativeAndNegative,
                           new MetroDialogSettings() { DialogTitleFontSize = 18, ColorScheme = MetroDialogColorScheme.Inverted, DialogMessageFontSize = 16 });

                if (ch == MessageDialogResult.Negative)
                    return;
                else if (ch == MessageDialogResult.Affirmative)
                {
                    SaveData.config.WarningUnity++;
                    SaveData.SaveConfig();
                }
            }
            OpenFileDialog openFileDialog1 = new OpenFileDialog()
            {
                Title = "Select Unity game",
                Filter = "Executable |*.exe"
            };

            if (openFileDialog1.ShowDialog() == true)
            {
                SetupWallpaper(openFileDialog1.FileName, SetupDesktop.WallpaperType.unity);
            }
        }

        private async void Tile_UNITY_AUDIO_Click(object sender, RoutedEventArgs e)
        {
            if (SaveData.config.WarningUnity == 0)
            {
                var ch = await this.ShowMessageAsync(Properties.Resources.msgExternalAppWarningTitle, Properties.Resources.msgExternalAppWarning, MessageDialogStyle.AffirmativeAndNegative,
                           new MetroDialogSettings() { DialogTitleFontSize = 18, ColorScheme = MetroDialogColorScheme.Inverted, DialogMessageFontSize = 16 });

                if (ch == MessageDialogResult.Negative)
                    return;
                else if (ch == MessageDialogResult.Affirmative)
                {
                    SaveData.config.WarningUnity++;
                    SaveData.SaveConfig();
                }
            }

            OpenFileDialog openFileDialog1 = new OpenFileDialog()
            {
                Title = "Select Unity audio visualiser",
                Filter = "Executable |*.exe"
            };

            if (openFileDialog1.ShowDialog() == true)
            {
                SetupWallpaper(openFileDialog1.FileName, SetupDesktop.WallpaperType.unity_audio);
            }

        }

        private void Tile_LIVELY_ZIP_Click(object sender, RoutedEventArgs e)
        {
            //tabControl1.SelectedIndex = 0; //switch to library tab.
            Button_Click_InstallWallpaper(this, null);
        }

        private async void Tile_Godot_Click(object sender, RoutedEventArgs e)
        {
            if (SaveData.config.WarningGodot == 0)
            {
                var ch = await this.ShowMessageAsync(Properties.Resources.msgExternalAppWarningTitle, Properties.Resources.msgExternalAppWarning, MessageDialogStyle.AffirmativeAndNegative,
                           new MetroDialogSettings() { DialogTitleFontSize = 18, ColorScheme = MetroDialogColorScheme.Inverted, DialogMessageFontSize = 16 });

                if (ch == MessageDialogResult.Negative)
                    return;
                else if (ch == MessageDialogResult.Affirmative)
                {
                    SaveData.config.WarningGodot++;
                    SaveData.SaveConfig();
                }
            }

            OpenFileDialog openFileDialog1 = new OpenFileDialog
            {
                Title = "Select Godot game",
                Filter = "Executable |*.exe"
            };
            if (openFileDialog1.ShowDialog() == true)
            {
                SetupWallpaper(openFileDialog1.FileName, SetupDesktop.WallpaperType.godot);
            }
        }

        private async void Tile_BizHawk_Click(object sender, RoutedEventArgs e)
        {
            WpfNotification(NotificationType.info, Properties.Resources.txtLivelyErrorMsgTitle, Properties.Resources.txtComingSoon);
            return;

            var dir = Directory.GetFiles(App.PathData + @"\external\bizhawk", "EmuHawk.exe", SearchOption.AllDirectories); //might be slow, only check top?
            if (dir.Length != 0)
            {
                SetupWallpaper(dir[0], SetupDesktop.WallpaperType.bizhawk);
            }
            else if (File.Exists(SaveData.config.BizHawkPath))
            {
                SetupWallpaper(SaveData.config.BizHawkPath, SetupDesktop.WallpaperType.bizhawk);
            }
            else
            {
                var ch = await this.ShowMessageAsync("Bizhawk Not Found", "Download BizHawk Emulator:\nhttps://github.com/TASVideos/BizHawk\nExtract & copy contents to:\nexternal\\bizhawk folder" +
                        "\n\n\t\tOR\n\nClick Browse & select EmuHawk.exe", MessageDialogStyle.AffirmativeAndNegative,
                        new MetroDialogSettings() { AffirmativeButtonText = "Ok", NegativeButtonText = "Browse", DialogTitleFontSize = 18, ColorScheme = MetroDialogColorScheme.Theme, DialogMessageFontSize = 16 });

                if (ch == MessageDialogResult.Affirmative) //Ok
                {
                    return;
                }
                else if (ch == MessageDialogResult.Negative) //Browse
                {

                    OpenFileDialog openFileDialog1 = new OpenFileDialog
                    {
                        Title = "Select EmuHawk.exe",
                        FileName = "EmuHawk.exe"
                    };
                    // openFileDialog1.Filter = formatsVideo;
                    if (openFileDialog1.ShowDialog() == true)
                    {
                        SaveData.config.BizHawkPath = openFileDialog1.FileName;
                        SaveData.SaveConfig();

                        SetupWallpaper(openFileDialog1.FileName, SetupDesktop.WallpaperType.bizhawk);
                    }
                }
            }

        }

        private async void Tile_Other_Click(object sender, RoutedEventArgs e)
        {
            var ch = await this.ShowMessageAsync(Properties.Resources.txtLivelyWaitMsgTitle, Properties.Resources.txtLivelyAppWarning, MessageDialogStyle.AffirmativeAndNegative,
                       new MetroDialogSettings() { DialogTitleFontSize = 18, ColorScheme = MetroDialogColorScheme.Inverted, DialogMessageFontSize = 16,
                       AnimateHide = false, AnimateShow = false});

            if (ch == MessageDialogResult.Negative)
                return;
            else if (ch == MessageDialogResult.Affirmative)
            {

            }

            OpenFileDialog openFileDialog1 = new OpenFileDialog
            {
                Filter = "Application (*.exe) |*.exe"
            };
            if (openFileDialog1.ShowDialog() == true)
            {
                SetupWallpaper(openFileDialog1.FileName, SetupDesktop.WallpaperType.app);
            }
        }

        private void Tile_HTML_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog
            {
                Filter = "Web Page (*.html) |*.html"
            };
            if (openFileDialog1.ShowDialog() == true)
            {
                SetupWallpaper(openFileDialog1.FileName, SetupDesktop.WallpaperType.web);
            }
        }


        private void Tile_HTML_AUDIO_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog
            {
                Filter = "Web Page with visualiser (*.html) |*.html"
            };
            if (openFileDialog1.ShowDialog() == true)
            {
                SetupWallpaper(openFileDialog1.FileName, SetupDesktop.WallpaperType.web_audio);
            }
        }

        private async void Tile_Video_stream_Click(object sender, RoutedEventArgs e)
        {
            if (SaveData.config.WarningURL == 0)
            {
                var ch = await this.ShowMessageAsync(Properties.Resources.msgUrlWarningTitle, Properties.Resources.msgUrlWarning, MessageDialogStyle.AffirmativeAndNegative,
                                new MetroDialogSettings() { DialogTitleFontSize = 18, ColorScheme = MetroDialogColorScheme.Inverted, DialogMessageFontSize = 16 });

                if (ch == MessageDialogResult.Negative)
                    return;
                else if (ch == MessageDialogResult.Affirmative)
                {
                    SaveData.config.WarningURL++;
                    SaveData.SaveConfig();
                }
            }

            var url = await this.ShowInputAsync("Stream", "Load online video..", new MetroDialogSettings() { 
                DialogTitleFontSize = 16, DialogMessageFontSize = 14, DefaultText = String.Empty, AnimateHide = false, AnimateShow = false });
            if (string.IsNullOrEmpty(url))
                return;

            SetupWallpaper(url, SetupDesktop.WallpaperType.video_stream);
        }

        private async void Tile_URL_Click(object sender, RoutedEventArgs e)
        {
            if (SaveData.config.WarningURL == 0)
            {
                var ch = await this.ShowMessageAsync(Properties.Resources.msgUrlWarningTitle, Properties.Resources.msgUrlWarning, MessageDialogStyle.AffirmativeAndNegative,
                                new MetroDialogSettings() { DialogTitleFontSize = 18, ColorScheme = MetroDialogColorScheme.Inverted, DialogMessageFontSize = 16 });

                if (ch == MessageDialogResult.Negative)
                    return;
                else if (ch == MessageDialogResult.Affirmative)
                {
                    SaveData.config.WarningURL++;
                    SaveData.SaveConfig();
                }
            }

            var url = await this.ShowInputAsync(Properties.Resources.msgUrlLoadTitle, Properties.Resources.msgUrlLoad, new MetroDialogSettings() { DialogTitleFontSize = 16, DialogMessageFontSize = 14, DefaultText = SaveData.config.DefaultURL });
            if (string.IsNullOrEmpty(url))
                return;

            SaveData.config.DefaultURL = url;
            SaveData.SaveConfig();

            WebLoadDragDrop(url);
            //SetupWallpaper(url, SetupDesktop.WallpaperType.url);
        }

        #endregion tile_events

        #region ui_events
        private void SubcribeUI()
        {
            //Subscribe to events here to prevent triggering calls during RestoreSaveSettings() todo: rewrite with data binding instead(Enum type need some extra code for convertion, skipping for now).
            comboBoxVideoPlayer.SelectionChanged += ComboBoxVideoPlayer_SelectionChanged;
            comboBoxGIFPlayer.SelectionChanged += ComboBoxGIFPlayer_SelectionChanged;
            comboBoxFocusedPerf.SelectionChanged += ComboBoxFocusedPerf_SelectionChanged;
            comboBoxFullscreenPerf.SelectionChanged += ComboBoxFullscreenPerf_SelectionChanged;
            comboBoxMonitorPauseRule.SelectionChanged += ComboBoxMonitorPauseRule_SelectionChanged;
            transparencyToggle.IsCheckedChanged += TransparencyToggle_IsCheckedChanged;
            StartupToggle.IsCheckedChanged += StartupToggle_IsCheckedChanged;
            videoMuteToggle.IsCheckedChanged += VideoMuteToggle_IsCheckedChanged;
            comboBoxFullscreenPerf.SelectionChanged += ComboBoxFullscreenPerf_SelectionChanged1;
            cefAudioInMuteToggle.IsCheckedChanged += CefAudioInMuteToggle_IsCheckedChanged;
            comboBoxPauseAlgorithm.SelectionChanged += ComboBoxPauseAlgorithm_SelectionChanged;
            TileAnimateToggle.IsCheckedChanged += TileAnimateToggle_IsCheckedChanged;
            appMuteToggle.IsCheckedChanged += AppMuteToggle_IsCheckedChanged;
            //fpsUIToggle.IsCheckedChanged += FpsUIToggle_IsCheckedChanged;
            //disableUIHWToggle.IsCheckedChanged += DisableUIHWToggle_IsCheckedChanged;
            comboBoxLanguage.SelectionChanged += ComboBoxLanguage_SelectionChanged;
            comboBoxTheme.SelectionChanged += ComboBoxTheme_SelectionChanged;
            audioFocusedToggle.IsCheckedChanged += AudioFocusedToggle_IsCheckedChanged;
            cmbBoxStreamQuality.SelectionChanged += CmbBoxStreamQuality_SelectionChanged;
            transparencySlider.ValueChanged += TransparencySlider_ValueChanged;
            TileGenerateToggle.IsCheckedChanged += TileGenerateToggle_IsCheckedChanged;
            comboBoxVideoPlayerScaling.SelectionChanged += ComboBoxVideoPlayerScaling_SelectionChanged;
            comboBoxGIFPlayerScaling.SelectionChanged += ComboBoxGIFPlayerScaling_SelectionChanged;
            comboBoxBatteryPerf.SelectionChanged += ComboBoxBatteryPerf_SelectionChanged;
            comboBoxWpInputSettings.SelectionChanged += ComboBoxWpInputSettings_SelectionChanged;
            chkboxMouseOtherAppsFocus.Checked += ChkboxMouseOtherAppsFocus_Checked;
            chkboxMouseOtherAppsFocus.Unchecked += ChkboxMouseOtherAppsFocus_Checked;
        }

        private void ChkboxMouseOtherAppsFocus_Checked(object sender, RoutedEventArgs e)
        {
            SaveData.config.MouseInputMovAlways = chkboxMouseOtherAppsFocus.IsChecked.Value;
            SaveData.SaveConfig();
        }

        private void ComboBoxWpInputSettings_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(comboBoxWpInputSettings.SelectedIndex == 0)
            {
                WallpaperInputForwardingToggle(false);
            }
            else if(comboBoxWpInputSettings.SelectedIndex == 1)
            {
                WallpaperInputForwardingToggle(true);
            }
            SaveData.config.InputForwardMode = comboBoxWpInputSettings.SelectedIndex;
            SaveData.SaveConfig();
        }

        private void ComboBoxBatteryPerf_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SaveData.config.BatteryPause = (SaveData.AppRulesEnum)comboBoxBatteryPerf.SelectedIndex;
            SaveData.SaveConfig();
        }

        private void ComboBoxGIFPlayerScaling_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SaveData.config.GifScaler = (Stretch)comboBoxGIFPlayerScaling.SelectedIndex;
            SaveData.SaveConfig();

            var result = SetupDesktop.wallpapers.FindAll(x => x.Type == SetupDesktop.WallpaperType.gif);
            SetupDesktop.CloseAllWallpapers(SetupDesktop.WallpaperType.gif);

            if (result.Count != 0)
            {
                RestoreWallpaper(result);
            }
        }

        private void ComboBoxVideoPlayerScaling_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SaveData.config.VideoScaler = (Stretch)comboBoxVideoPlayerScaling.SelectedIndex;
            SaveData.SaveConfig();

            var videoWp = SetupDesktop.wallpapers.FindAll(x => x.Type == SetupDesktop.WallpaperType.video); //youtube is started as apptype, not included!
            SetupDesktop.CloseAllWallpapers(SetupDesktop.WallpaperType.video);

            if (videoWp.Count != 0)
            {
                RestoreWallpaper(videoWp);
            }
        }

        private void TileGenerateToggle_IsCheckedChanged(object sender, EventArgs e)
        {
            SaveData.config.GenerateTile = TileGenerateToggle.IsChecked.Value;
            SaveData.SaveConfig();
        }

        private void TransparencySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            this.Opacity = transparencySlider.Value;
            SaveData.config.AppTransparencyPercent = transparencySlider.Value;
            SaveData.SaveConfig();
        }

        private void CmbBoxStreamQuality_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbBoxStreamQuality.SelectedIndex == -1)
                return;

            SaveData.config.StreamQuality = (SaveData.StreamQualitySuggestion)cmbBoxStreamQuality.SelectedIndex;
            SaveData.SaveConfig();
           
            var streamWP = SetupDesktop.wallpapers.FindAll(x => x.Type == SetupDesktop.WallpaperType.video_stream); 
            SetupDesktop.CloseAllWallpapers(SetupDesktop.WallpaperType.video_stream);

            if (streamWP.Count != 0)
            {
                RestoreWallpaper(streamWP);
            }
            
        }

        private void AudioFocusedToggle_IsCheckedChanged(object sender, EventArgs e)
        {
            SaveData.config.AlwaysAudio = audioFocusedToggle.IsChecked.Value;
            SaveData.SaveConfig();
        }

        private void ComboBoxTheme_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (comboBoxTheme.SelectedIndex == -1)
                return;
            //todo:- do it more elegantly.
            SaveData.config.Theme = comboBoxTheme.SelectedIndex;
            //SaveData.SaveConfig();

            RestartLively();
        }

        private void AppMuteToggle_IsCheckedChanged(object sender, EventArgs e)
        {
            SaveData.config.MuteAppWP = !appMuteToggle.IsChecked.Value;
            SaveData.SaveConfig();
        }

        private void RestoreMenuSettings()
        {
            chkboxMouseOtherAppsFocus.IsChecked = SaveData.config.MouseInputMovAlways;

            if (SaveData.config.InputForwardMode == 1)
            {
                WallpaperInputForwardingToggle(true);
            }

            try
            {
                comboBoxWpInputSettings.SelectedIndex = SaveData.config.InputForwardMode;
            }
            catch(ArgumentOutOfRangeException)
            {
                SaveData.config.InputForwardMode = 1;
                SaveData.SaveConfig();
                comboBoxWpInputSettings.SelectedIndex = 1;
            }

            if (!App.isPortableBuild)
            {
                lblPortableTxt.Visibility = Visibility.Collapsed;
            }
            
            if (SaveData.config.AppTransparency)
            {
                if (SaveData.config.AppTransparencyPercent >= 0.5 && SaveData.config.AppTransparencyPercent <= 0.9)
                {
                    this.Opacity = SaveData.config.AppTransparencyPercent;
                }
                else
                {
                    this.Opacity = 0.9f;
                }
                transparencyToggle.IsChecked = true;
                transparencySlider.IsEnabled = true;
            }
            else
            {
                this.Opacity = 1.0f;
                transparencyToggle.IsChecked = false;
                transparencySlider.IsEnabled = false;
            }

            if (SaveData.config.AppTransparencyPercent >= 0.5 && SaveData.config.AppTransparencyPercent <= 0.9)
                transparencySlider.Value = SaveData.config.AppTransparencyPercent;
            else
                transparencySlider.Value = 0.9f;

            audioFocusedToggle.IsChecked = SaveData.config.AlwaysAudio;
            TileGenerateToggle.IsChecked = SaveData.config.GenerateTile;
            appMuteToggle.IsChecked = !SaveData.config.MuteAppWP;      
            TileAnimateToggle.IsChecked = SaveData.config.LiveTile;
            //fpsUIToggle.IsChecked = SaveData.config.Ui120FPS;
            //disableUIHWToggle.IsChecked = SaveData.config.UiDisableHW;

            try
            {
                comboBoxGIFPlayerScaling.SelectedIndex = (int)SaveData.config.GifScaler;
            }
            catch (ArgumentOutOfRangeException)
            {
                SaveData.config.GifScaler = Stretch.UniformToFill;
                SaveData.SaveConfig();
                comboBoxGIFPlayerScaling.SelectedIndex = (int)SaveData.config.GifScaler;
            }

            try
            {
                comboBoxBatteryPerf.SelectedIndex = (int)SaveData.config.BatteryPause;
            }
            catch (ArgumentOutOfRangeException)
            {
                SaveData.config.BatteryPause = AppRulesEnum.ignore;
                SaveData.SaveConfig();
                comboBoxBatteryPerf.SelectedIndex = (int)SaveData.config.BatteryPause;
            }

            try
            {
                comboBoxVideoPlayerScaling.SelectedIndex = (int)SaveData.config.VideoScaler;
            }
            catch (ArgumentOutOfRangeException)
            {
                SaveData.config.VideoScaler= Stretch.UniformToFill;
                SaveData.SaveConfig();
                comboBoxVideoPlayerScaling.SelectedIndex = (int)SaveData.config.VideoScaler;
            }

            try
            {
                comboBoxPauseAlgorithm.SelectedIndex = (int)SaveData.config.ProcessMonitorAlgorithm;
            }
            catch (ArgumentOutOfRangeException)
            {
                SaveData.config.ProcessMonitorAlgorithm = SaveData.ProcessMonitorAlgorithm.foreground;
                SaveData.SaveConfig();
                comboBoxPauseAlgorithm.SelectedIndex = (int)SaveData.config.ProcessMonitorAlgorithm;
            }

            //stream
            try
            {
                cmbBoxStreamQuality.SelectedIndex = (int)SaveData.config.StreamQuality;
            }
            catch (ArgumentOutOfRangeException)
            {
                SaveData.config.StreamQuality = SaveData.StreamQualitySuggestion.h720p;
                SaveData.SaveConfig();
                cmbBoxStreamQuality.SelectedIndex = (int)SaveData.config.StreamQuality;
            }


            cefAudioInMuteToggle.IsChecked = !SaveData.config.MuteCefAudioIn;
            if (!SaveData.config.MuteCefAudioIn)
                web_audio_WarningText.Visibility = Visibility.Hidden;
            else
                web_audio_WarningText.Visibility = Visibility.Visible;

            videoMuteToggle.IsChecked = !SaveData.config.MuteVideo;

            // performance ui

            try
            {
                comboBoxFullscreenPerf.SelectedIndex = (int)SaveData.config.AppFullscreenPause;
            }
            catch (ArgumentOutOfRangeException)
            {
                SaveData.config.AppFullscreenPause = SaveData.AppRulesEnum.pause;
                SaveData.SaveConfig();
                comboBoxFullscreenPerf.SelectedIndex = (int)SaveData.config.AppFullscreenPause;
            }

            try
            {
                comboBoxFocusedPerf.SelectedIndex = (int)SaveData.config.AppFocusPause;
            }
            catch (ArgumentOutOfRangeException)
            {
                SaveData.config.AppFocusPause = SaveData.AppRulesEnum.ignore;
                SaveData.SaveConfig();
                comboBoxFocusedPerf.SelectedIndex = (int)SaveData.config.AppFocusPause;
            }

            try
            {
                comboBoxFullscreenPerf.SelectedIndex = (int)SaveData.config.AppFullscreenPause;
            }
            catch (ArgumentOutOfRangeException)
            {
                SaveData.config.AppFullscreenPause = SaveData.AppRulesEnum.pause;
                SaveData.SaveConfig();
                comboBoxFullscreenPerf.SelectedIndex = (int)SaveData.config.AppFullscreenPause;
            }

            try
            {
                comboBoxMonitorPauseRule.SelectedIndex = (int)SaveData.config.DisplayPauseSettings;
            }
            catch (ArgumentOutOfRangeException)
            {
                SaveData.config.DisplayPauseSettings = SaveData.DisplayPauseEnum.perdisplay;
                SaveData.SaveConfig();
                comboBoxMonitorPauseRule.SelectedIndex = (int)SaveData.config.DisplayPauseSettings;
            }

            try
            {
                comboBoxVideoPlayer.SelectedIndex = (int)SaveData.config.VidPlayer;
            }
            catch (ArgumentOutOfRangeException)
            {
                SaveData.config.VidPlayer = SaveData.VideoPlayer.windowsmp;
                SaveData.SaveConfig();
                comboBoxVideoPlayer.SelectedIndex = (int)SaveData.config.VidPlayer;
            }

            try
            {
                comboBoxGIFPlayer.SelectedIndex = (int)SaveData.config.GifPlayer;
            }
            catch (ArgumentOutOfRangeException)
            {
                SaveData.config.VidPlayer = (int)SaveData.GIFPlayer.xaml;
                SaveData.SaveConfig();
                comboBoxGIFPlayer.SelectedIndex = (int)SaveData.config.GifPlayer;
            }

            //ignoring save file, instead check registry if key exists..
            var startupkeyValue = CheckStartupRegistry();
            if (String.IsNullOrEmpty(startupkeyValue))
            {
                SaveData.config.Startup = false;
            }
            else if (String.Equals(startupkeyValue, Assembly.GetExecutingAssembly().Location, StringComparison.Ordinal))
            {
                //everything looks good.
                SaveData.config.Startup = true;
            }
            else
            {
                SaveData.config.Startup = true;
                //key value do not match, delete & add key again.
                SetStartupRegistry(true);
            }
            StartupToggle.IsChecked = SaveData.config.Startup;
            SaveData.SaveConfig(); //saving startup state.

            #region shit
            //todo:- do it more elegantly.
            //language
            foreach (var item in SaveData.supportedLanguages)
            {
                comboBoxLanguage.Items.Add(item.Language);
            }

            bool found = false;
            for (int i = 0; i < SaveData.supportedLanguages.Length; i++)
            {
                if (Array.Exists(SaveData.supportedLanguages[i].Codes, x => x.Equals(SaveData.config.Language, StringComparison.OrdinalIgnoreCase)))
                {
                    comboBoxLanguage.SelectedIndex = i;
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                comboBoxLanguage.SelectedIndex = 0; //en-US
            }
            
            //theme
            foreach (var item in SaveData.livelyThemes)
            {
                comboBoxTheme.Items.Add(item.Name);
            }

            try
            {
                comboBoxTheme.SelectedIndex = SaveData.config.Theme;
            }
            catch(ArgumentOutOfRangeException)
            {
                SaveData.config.Theme = 0; //DarkLime
                SaveData.SaveConfig();
                comboBoxTheme.SelectedIndex = SaveData.config.Theme;
            }

            #endregion shit

        }

        private void ComboBoxPauseAlgorithm_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (comboBoxPauseAlgorithm.SelectedIndex == 1)
            {
                if (Multiscreen)
                {
                    comboBoxPauseAlgorithm.SelectedIndex = 0;
                    WpfNotification(NotificationType.info, Properties.Resources.txtLivelyErrorMsgTitle, "Currently this algorithm is incomplete in multiple display systems, disabling.");
                    return;
                }
            }

            SaveData.config.ProcessMonitorAlgorithm = (SaveData.ProcessMonitorAlgorithm)comboBoxPauseAlgorithm.SelectedIndex;
            SaveData.SaveConfig();
        }

        private void CefAudioInMuteToggle_IsCheckedChanged(object sender, EventArgs e)
        {

            SaveData.config.MuteCefAudioIn = !cefAudioInMuteToggle.IsChecked.Value;
            SaveData.SaveConfig();

            if (!SaveData.config.MuteCefAudioIn)
                web_audio_WarningText.Visibility = Visibility.Hidden;
            else
                web_audio_WarningText.Visibility = Visibility.Visible;
        }

        private void ComboBoxFullscreenPerf_SelectionChanged1(object sender, SelectionChangedEventArgs e)
        {
            SaveData.config.AppFullscreenPause = (SaveData.AppRulesEnum)comboBoxFullscreenPerf.SelectedIndex;
            SaveData.SaveConfig();
        }

        private void VideoMuteToggle_IsCheckedChanged(object sender, EventArgs e)
        {
            SaveData.config.MuteVideo = !videoMuteToggle.IsChecked.Value;
            SaveData.SaveConfig();
        }

        private void ComboBoxMonitorPauseRule_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SaveData.config.DisplayPauseSettings = (SaveData.DisplayPauseEnum)comboBoxMonitorPauseRule.SelectedIndex;
            try
            {
                comboBoxMonitorPauseRule.SelectedIndex = (int)SaveData.config.DisplayPauseSettings;
            }
            catch (ArgumentOutOfRangeException)
            {
                SaveData.config.DisplayPauseSettings = SaveData.DisplayPauseEnum.perdisplay;
                comboBoxMonitorPauseRule.SelectedIndex = (int)SaveData.config.DisplayPauseSettings;
            }
            SaveData.SaveConfig();
        }

        private void ComboBoxFullscreenPerf_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SaveData.config.AppFullscreenPause = (SaveData.AppRulesEnum)comboBoxFullscreenPerf.SelectedIndex;
            try
            {
                comboBoxFullscreenPerf.SelectedIndex = (int)SaveData.config.AppFullscreenPause;
            }
            catch (ArgumentOutOfRangeException)
            {
                SaveData.config.AppFullscreenPause = SaveData.AppRulesEnum.pause;
                comboBoxFullscreenPerf.SelectedIndex = (int)SaveData.config.AppFullscreenPause;
            }
            SaveData.SaveConfig();
        }

        private void ComboBoxFocusedPerf_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SaveData.config.AppFocusPause = (SaveData.AppRulesEnum)comboBoxFocusedPerf.SelectedIndex;
            try
            {
                comboBoxFocusedPerf.SelectedIndex = (int)SaveData.config.AppFocusPause;
            }
            catch (ArgumentOutOfRangeException)
            {
                SaveData.config.AppFocusPause = SaveData.AppRulesEnum.ignore;
                comboBoxFocusedPerf.SelectedIndex = (int)SaveData.config.AppFocusPause;
            }
            SaveData.SaveConfig();
        }

        private void TransparencyToggle_IsCheckedChanged(object sender, EventArgs e)
        {
            if (transparencyToggle.IsChecked == true)
            {
                if (SaveData.config.AppTransparencyPercent >= 0.5 && SaveData.config.AppTransparencyPercent <= 0.9)
                    this.Opacity = SaveData.config.AppTransparencyPercent;
                else
                    this.Opacity = 0.9f;
                SaveData.config.AppTransparency = true;
                transparencySlider.IsEnabled = true;
            }
            else
            {
                this.Opacity = 1.0f;
                SaveData.config.AppTransparency = false;
                transparencySlider.IsEnabled = false;
            }
            SaveData.SaveConfig();
        }


        private void TileAnimateToggle_IsCheckedChanged(object sender, EventArgs e)
        {
            if (TileAnimateToggle.IsChecked == true)
            {
                SaveData.config.LiveTile = true;
                foreach (var item in tileDataList)
                {
                    if (File.Exists(item.LivelyInfo.Preview)) //only if preview gif exist, clear existing image.
                    {
                        item.Img = null;
                    }
                }
                InitializeTilePreviewGifs(); //loads first 15gifs. into TilePreview
            }
            else
            {
                SaveData.config.LiveTile = false;
                foreach (var item in tileDataList)
                {
                    item.Img = item.LoadConvertImage(item.LivelyInfo.Thumbnail);
                    //item.Img = item.LoadImage(item.LivelyInfo.Thumbnail);
                    item.TilePreview = null;
                }
            }

            textBoxLibrarySearch.Text = null;
            ScrollViewer scrollViewer = GetDescendantByType(wallpapersLV, typeof(ScrollViewer)) as ScrollViewer;
            if (scrollViewer != null)
            {
                scrollViewer.ScrollToVerticalOffset(0);
            }
            wallpapersLV.Items.Refresh(); //force redraw: not refreshing everything, even with INotifyPropertyChanged.
            SaveData.SaveConfig();
        }

        public void Button_Click_HowTo(object sender, RoutedEventArgs e)
        {
            Dialogues.HelpWindow w = new Dialogues.HelpWindow(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "docs","help_vid_1.mp4"))
            {
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            w.ShowDialog();
        }

        /// <summary>
        /// Display layout panel show.
        /// </summary>
        private void Image_Display_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            DisplayLayoutWindow displayWindow = new DisplayLayoutWindow(this)
            {
                Owner = Window.GetWindow(this)
            };
            displayWindow.ShowDialog();
            displayWindow.Close();
            this.Activate();
            //Debug.WriteLine("retured val:- " + DisplayLayoutWindow.index);
        }

        private void Display_layout_Btn(object sender, EventArgs e)
        {
            DisplayLayoutWindow displayWindow = new DisplayLayoutWindow(this)
            {
                Owner = Window.GetWindow(this)
            };
            displayWindow.ShowDialog();
            displayWindow.Close();
            this.Activate();
            //Debug.WriteLine("retured val:- " + DisplayLayoutWindow.index);
        }

        /// <summary>
        /// Videoplayer change, restarts currently playing wp's to newly selected system.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ComboBoxVideoPlayer_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SaveData.config.VidPlayer = (SaveData.VideoPlayer)comboBoxVideoPlayer.SelectedIndex;
            SaveData.SaveConfig();

            var videoWp = SetupDesktop.wallpapers.FindAll(x => x.Type == SetupDesktop.WallpaperType.video); //youtube is started as apptype, not included!
            SetupDesktop.CloseAllWallpapers(SetupDesktop.WallpaperType.video);

            if (videoWp.Count != 0)
            {
                RestoreWallpaper(videoWp);
            }
        }

        /// <summary>
        ///  Gif player change, restarts currently playing wp's to newly selected system.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ComboBoxGIFPlayer_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SaveData.config.GifPlayer = (SaveData.GIFPlayer)comboBoxGIFPlayer.SelectedIndex;
            SaveData.SaveConfig();

            var result = SetupDesktop.wallpapers.FindAll(x => x.Type == SetupDesktop.WallpaperType.gif);
            SetupDesktop.CloseAllWallpapers(SetupDesktop.WallpaperType.gif);

            if (result.Count != 0)
            {
                RestoreWallpaper(result);
            }
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            try
            {
                Process.Start(e.Uri.AbsoluteUri);
            }
            catch { } //if no default mail client, win7 error.
        }

        private void Hyperlink_SupportPage(object sender, RoutedEventArgs e)
        {
            Process.Start(@"https://ko-fi.com/rocksdanister");
        }

        private void Button_Click_CreateWallpaper(object sender, RoutedEventArgs e)
        {
            CreateWallpaper obj = new CreateWallpaper
            {
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            obj.ShowDialog();
        }

        /// <summary>
        /// Shows warning msg with link before proceeding to load hyperlink.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Hyperlink_RequestNavigate_Warning(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            var ch = await this.ShowMessageAsync(Properties.Resources.msgLoadExternalLinkTitle, Properties.Resources.msgLoadExternalLink + "\n" + e.Uri.ToString(), MessageDialogStyle.AffirmativeAndNegative,
                         new MetroDialogSettings() { DialogTitleFontSize = 18, ColorScheme = MetroDialogColorScheme.Inverted, DialogMessageFontSize = 16 });

            if (ch == MessageDialogResult.Negative)
                return;
            else if (ch == MessageDialogResult.Affirmative)
            {

            }

            Process.Start(e.Uri.AbsoluteUri);
        }

        /// <summary>
        /// Drag and Drop wallpaper.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MetroWindow_Drop(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                string[] droppedFiles = e.Data.GetData(System.Windows.DataFormats.FileDrop, true) as string[];

                if ((null == droppedFiles) || (!droppedFiles.Any())) { return; }

                Logger.Info("Dropped File, Selecting first file:- " + droppedFiles[0]);

                if (String.IsNullOrWhiteSpace(Path.GetExtension(droppedFiles[0])))
                    return;

                if (Path.GetExtension(droppedFiles[0]).Equals(".gif", StringComparison.OrdinalIgnoreCase))
                    SetupWallpaper(droppedFiles[0], SetupDesktop.WallpaperType.gif);
                else if (Path.GetExtension(droppedFiles[0]).Equals(".html", StringComparison.OrdinalIgnoreCase))
                    SetupWallpaper(droppedFiles[0], SetupDesktop.WallpaperType.web);
                else if (Path.GetExtension(droppedFiles[0]).Equals(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    tabControl1.SelectedIndex = 0; //switch to library tab.
                    WallpaperInstaller(droppedFiles[0]);
                }
                else if (IsVideoFile(droppedFiles[0]))
                    SetupWallpaper(droppedFiles[0], SetupDesktop.WallpaperType.video);
                else
                {
                    //exe format is skipped for drag & drop, mainly due to security reasons.
                    if (this.IsVisible)
                        this.Activate(); //bugfix.

                    System.Windows.Controls.Button btn = new System.Windows.Controls.Button
                    {
                        Margin = new Thickness(12, 8, 12, 8),
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                        Content = "Goto Type"
                    };
                    btn.Click += Btn_Gototab;

                    notify.Manager.CreateMessage()
                     .Accent("#808080")
                     .HasBadge("Info")
                     .Background("#333")
                     .HasHeader(Properties.Resources.msgDragDropOtherFormatsTitle)
                     .HasMessage(Properties.Resources.msgDragDropOtherFormats + "\n" + droppedFiles[0])
                     .Dismiss().WithButton("Ok", button => { })
                     .WithAdditionalContent(ContentLocation.Bottom,
                       new Border
                       {
                           BorderThickness = new Thickness(0, 1, 0, 0),
                           BorderBrush = new SolidColorBrush(Color.FromArgb(128, 28, 28, 28)),
                           Child = btn
                       })
                     .Queue();

                }

            }
            else if(e.Data.GetDataPresent(System.Windows.DataFormats.Text))
            {
                string droppedText = (string)e.Data.GetData(System.Windows.DataFormats.Text, true);
                Logger.Info("Dropped Text:- " + droppedText);
                if ( (String.IsNullOrWhiteSpace(droppedText)) ) 
                { 
                    return;
                }
                WebLoadDragDrop(droppedText);
            }
        }

        private void Btn_Gototab(object sender, RoutedEventArgs e)
        {
            tabControl1.SelectedIndex = 1;   
        }

        private void WebLoadDragDrop(string link)
        {
            if (link.Contains("youtube.com/watch?v=") || link.Contains("bilibili.com/video/")) //drag drop only for youtube.com streams
            {
                SetupWallpaper(link, SetupDesktop.WallpaperType.video_stream);
            }
            else
            {
                SetupWallpaper(link, SetupDesktop.WallpaperType.url);
            }
        }

        /// <summary>
        /// Returns commandline argument for youtube-dl + mpv, depending on the saved Quality setting.
        /// todo: add codec selection.
        /// </summary>
        /// <param name="link"></param>
        /// <returns></returns>
        private static string YoutubeDLArgGenerate(string link)
        {
            string quality = null;
            if (!link.Contains("bilibili.com/video/")) //youtube-dl failing if quality flag is set.
            {
                switch (SaveData.config.StreamQuality)
                {
                    case StreamQualitySuggestion.best:
                        quality = String.Empty;
                        break;
                    case StreamQualitySuggestion.h2160p:
                        quality = " --ytdl-format bestvideo[height<=2160]+bestaudio/best[height<=2160]";
                        break;
                    case StreamQualitySuggestion.h1440p:
                        quality = " --ytdl-format bestvideo[height<=1440]+bestaudio/best[height<=1440]";
                        break;
                    case StreamQualitySuggestion.h1080p:
                        quality = " --ytdl-format bestvideo[height<=1080]+bestaudio/best[height<=1080]";
                        break;
                    case StreamQualitySuggestion.h720p:
                        quality = " --ytdl-format bestvideo[height<=720]+bestaudio/best[height<=720]";
                        break;
                    case StreamQualitySuggestion.h480p:
                        quality = " --ytdl-format bestvideo[height<=480]+bestaudio/best[height<=480]";
                        break;
                    default:
                        quality = " --ytdl-format bestvideo[height<=720]+bestaudio/best[height<=720]";
                        break;
                }
            }
            else
            {
                quality = String.Empty;
            }

            return "\"" + link + "\"" + " --force-window=yes --loop-file --keep-open --hwdec=yes --no-keepaspect" + quality;
        }

//        public readonly static string[] formatsVideo = { ".dat", ".wmv", ".3g2", ".3gp", ".3gp2", ".3gpp", ".amv", ".asf",  ".avi", ".bin", ".cue", ".divx", ".dv", ".flv", ".gxf", ".iso", ".m1v", ".m2v", ".m2t", ".m2ts", ".m4v",
//                                        ".mkv", ".mov", ".mp2", ".mp2v", ".mp4", ".mp4v", ".mpa", ".mpe", ".mpeg", ".mpeg1", ".mpeg2", ".mpeg4", ".mpg", ".mpv2", ".mts", ".nsv", ".nuv", ".ogg", ".ogm", ".ogv", ".ogx", ".ps", ".rec", ".rm",
//                                        ".rmvb", ".tod", ".ts", ".tts", ".vob", ".vro", ".webm" };
        static bool IsVideoFile(string path)
        {
            string[] formatsVideo = { ".dat", ".wmv", ".3g2", ".3gp", ".3gp2", ".3gpp", ".amv", ".asf",  ".avi", ".bin", ".cue", ".divx", ".dv", ".flv", ".gxf", ".iso", ".m1v", ".m2v", ".m2t", ".m2ts", ".m4v",
                                        ".mkv", ".mov", ".mp2", ".mp2v", ".mp4", ".mp4v", ".mpa", ".mpe", ".mpeg", ".mpeg1", ".mpeg2", ".mpeg4", ".mpg", ".mpv2", ".mts", ".nsv", ".nuv", ".ogg", ".ogm", ".ogv", ".ogx", ".ps", ".rec", ".rm",
                                        ".rmvb", ".tod", ".ts", ".tts", ".vob", ".vro", ".webm" };
            if (formatsVideo.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
            {
                return true;
            }
            return false;
        }

        private void Button_AppRule_Click(object sender, RoutedEventArgs e)
        {
            Dialogues.ApplicationRuleDialogWindow w = new Dialogues.ApplicationRuleDialogWindow
            {
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this
            };
            w.ShowDialog();
        }

        /*
        private void DisableUIHWToggle_IsCheckedChanged(object sender, EventArgs e)
        {
            if (disableUIHWToggle.IsChecked == true)
                SaveData.config.UiDisableHW = true;
            else
                SaveData.config.UiDisableHW = false;

            SaveData.SaveConfig();
        }

        private void FpsUIToggle_IsCheckedChanged(object sender, EventArgs e)
        {
            if (fpsUIToggle.IsChecked == true)
                SaveData.config.Ui120FPS = true;
            else
                SaveData.config.Ui120FPS = false;

            SaveData.SaveConfig();
        }
        */
        private void ComboBoxLanguage_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (comboBoxLanguage.SelectedIndex == -1)
                return;
            //todo:- do it more elegantly.
            SaveData.config.Language = SaveData.supportedLanguages[comboBoxLanguage.SelectedIndex].Codes[0];
            //SaveData.SaveConfig();
            RestartLively();
        }

        /// <summary>
        /// save config & restart lively.
        /// </summary>
        public static void RestartLively()
        {
            //Need more testing, mutex(for single instance of lively) release might not be quick enough.
            _isExit = true;
            SaveData.config.IsRestart = true;
            SaveData.SaveConfig();
            System.Diagnostics.Process.Start(System.Windows.Application.ResourceAssembly.Location);
            System.Windows.Application.Current.Shutdown();
        }

        Dialogues.Changelog changelogWindow = null;
        private void lblVersionNumber_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (changelogWindow == null)
            {
                changelogWindow = new Dialogues.Changelog
                {
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    ShowActivated = true
                };
                changelogWindow.Closed += ChangelogWindow_Closed;
                changelogWindow.Show();
            }
            else
            {
                if (changelogWindow.IsVisible)
                {
                    changelogWindow.Activate();
                }
            }
        }

        private void ChangelogWindow_Closed(object sender, EventArgs e)
        {
            changelogWindow = null;
        }

        private void hyperlinkUpdateBanner_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ShowLivelyUpdateWindow();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }

        #endregion ui_events

        #region notification_dialogues

        public enum NotificationType
        {
            info,
            error,
            alert,
            infoUrl,
            errorUrl
        }
        public void WpfNotification(NotificationType type, object title, object message, string url = null)
        {
            Tuple<AppTheme, Accent> appStyle = ThemeManager.DetectAppStyle(System.Windows.Application.Current);
            var accentColor = this.TryFindResource("AccentColorBrush") as SolidColorBrush;

            if (type == NotificationType.info)
            {
                notify.Manager.CreateMessage()
                   .Accent(accentColor)
                   .HasBadge("INFO")
                   .Background("#333")
                   .HasHeader((string)title)
                   .HasMessage((string)message)
                   .Dismiss().WithButton("Ok", button => { })
                   .Queue();
            }
            else if (type == NotificationType.infoUrl)
            {

                Hyperlink hyper = new Hyperlink
                {                
                    Foreground = new SolidColorBrush(Colors.Gray),
                };

                try
                {
                    hyper.NavigateUri = new System.Uri(url);
                }
                catch
                {
                    url = "https://github.com/rocksdanister/lively/wiki";
                    hyper.NavigateUri = new System.Uri(url);
                }
                hyper.RequestNavigate += Hyperlink_RequestNavigate;

                TextBlock tb = new TextBlock()
                {
                    Margin = new Thickness(12, 8, 12, 8),
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Left
                };

                Run run = new Run
                {
                    Text = url,      
                };

                hyper.Inlines.Add(run);
                tb.Inlines.Add(hyper);

                notify.Manager.CreateMessage()
                    .Accent(this.TryFindResource("AccentColorBrush") as SolidColorBrush)
                    //.Accent("#808080")
                    .HasBadge("INFO")
                    .Background("#333")
                    .HasHeader((string)title)
                    .HasMessage((string)message)
                    .Dismiss().WithButton("Ok", button => { })
                    .WithAdditionalContent(ContentLocation.Bottom, new Border
                    {
                        BorderThickness = new Thickness(0, 1, 0, 0),
                        BorderBrush = new SolidColorBrush(Color.FromArgb(128, 28, 28, 28)),
                        Child = tb
                    })
                    //.WithAdditionalContent(ContentLocation.Bottom, hyper)
                    .Queue();

            }
            else if( type == NotificationType.error)
            {
                notify.Manager.CreateMessage()
                 .Accent("#FF0000")
                 .HasBadge("ERROR")
                 .Background("#333")
                 .HasHeader((string)title)
                 .HasMessage((string)message)
                 .Dismiss().WithButton("Ok", button => { })
                 .Queue();
            }
            else if( type == NotificationType.errorUrl)
            {
                Hyperlink hyper = new Hyperlink
                {
                    Foreground = new SolidColorBrush(Colors.Gray),
                };

                try
                {
                    hyper.NavigateUri = new System.Uri(url);
                }
                catch
                {
                    url = "https://github.com/rocksdanister/lively/wiki";
                    hyper.NavigateUri = new System.Uri(url);
                }
                hyper.RequestNavigate += Hyperlink_RequestNavigate;

                TextBlock tb = new TextBlock()
                {
                    Margin = new Thickness(12, 8, 12, 8),
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Left
                };

                Run run = new Run
                {
                    Text = url,
                };

                hyper.Inlines.Add(run);
                tb.Inlines.Add(hyper);

                notify.Manager.CreateMessage()
                    .Accent("#FF0000")
                    //.Accent("#808080")
                    .HasBadge("ERROR")
                    .Background("#333")
                    .HasHeader((string)title)
                    .HasMessage((string)message)
                    .Dismiss().WithButton("Ok", button => { })
                    .WithAdditionalContent(ContentLocation.Bottom, new Border
                    {
                        BorderThickness = new Thickness(0, 1, 0, 0),
                        BorderBrush = new SolidColorBrush(Color.FromArgb(128, 28, 28, 28)),
                        Child = tb
                    })
                    //.WithAdditionalContent(ContentLocation.Bottom, hyper)
                    .Queue();
            }
        }

        #endregion 
    }
}

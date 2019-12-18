using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using System.Diagnostics;

using System.Reflection;
using Ionic.Zip;
//using System.IO.Compression;
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
using IWshRuntimeLibrary;
using System.Threading;
using File = System.IO.File;
using NLog;
using System.Collections.ObjectModel;
using System.Windows.Media.Animation;
using System.ComponentModel;

using static livelywpf.SaveData;
using System.Text.RegularExpressions;
using System.Windows.Interop;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;
using System.Globalization;

namespace livelywpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public static bool multiscreen = false;
        ProgressDialogController progressController = null;
        private ObservableCollection<TileData> tileDataList = new ObservableCollection<TileData>();
        private ObservableCollection<TileData> selectedTile = new ObservableCollection<TileData>();

        private ICollectionView tileDataFiltered;
        private bool _isRestoringWallpapers = false;
        public static bool highContrastFix = false;

        public MainWindow()
        {
            SystemInfo.LogHardwareInfo();

            #region lively_SubProcess
            //External process that runs, kills external pgm wp's( unity, app etc) & refresh desktop in the event lively crashed, could do this in UnhandledException event but this is guaranteed to work even if user kills livelywpf in taskmgr.
            //todo:- should reconsider.
            try
            {
                Process.Start(Path.Combine(AppDomain.CurrentDomain.BaseDirectory + "\\livelySubProcess.exe"), Process.GetCurrentProcess().Id.ToString(CultureInfo.InvariantCulture));
            }
            catch(Exception e)
            {
                Logger.Error(e,"Starting livelybg.exe failure: " + e.ToString());
            }
            #endregion lively_SubProcess

            //settings applied only during app relaunch.
            #region misc_fixes
            SetupDesktop.wallpaperWaitTime = SaveData.config.WallpaperWaitTime;

            if(SaveData.config.WallpaperRendering == WallpaperRenderingMode.bottom_most)
            {
                highContrastFix = true;
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

            this.Closing += MainWindow_Closing;
            SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged; //static event, unsubcribe!
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
            if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory + "\\docs\\license.rtf")))
            {
                try
                {
                    using (FileStream fileStream = File.Open(Path.Combine(AppDomain.CurrentDomain.BaseDirectory + "\\docs\\license.rtf"), FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        textRange.Load(fileStream, System.Windows.DataFormats.Rtf);
                    }
                    licenseFlowDocumentViewer.Document = licenseDocument;
                }
                catch
                {
                    Logger.Error("Failed to load license file");
                }
            }
            /*
            DoubleAnimation anim = new DoubleAnimation();
            Storyboard storyBoard = (Storyboard)this.Resources["fidgetSpinner"];
            */

            //whats new screen!
            if (!SaveData.config.AppVersion.Equals(System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(), StringComparison.OrdinalIgnoreCase)
                && SaveData.config.IsFirstRun != true)
            {
                //if previous savedata version is different from currently running app, show help/update info screen.
                SaveData.config.AppVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
                SaveData.SaveConfig();

                dialogues_general.Changelog cl = new dialogues_general.Changelog
                {
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    ShowActivated = true
                };
                cl.Show();
            }

            SystemEvents_DisplaySettingsChanged(this, null); //restore previously running wp's.

            //Incomplete, currently in development:- all process algorithm & multiscreen
            if (multiscreen && SaveData.config.ProcessMonitorAlgorithm == ProcessMonitorAlgorithm.all)
            {
                Logger.Info("Skipping all-process algorthm on multiscreen(in-development)");
                comboBoxPauseAlgorithm.SelectedIndex = (int)ProcessMonitorAlgorithm.foreground; //event will save settings.
            }
        }

        private async void RestoreSaveSettings()
        {
            //load savefiles. 
            SaveData.LoadApplicationRules();
            //SaveData.LoadConfig(); //app.xaml.cs loads config file.
            SaveData.LoadWallpaperLayout();
            RestoreMenuSettings();
            SetStartupRegistry(SaveData.config.Startup);

            await GithubCheck(); 
            update_traybtn.Enabled = true;
        }

        #region github_update_check
        /// <summary>
        /// Compares application Version string with github release version & shows native windows notification.
        /// Note: Comaprison result<0 if github release tag is less than 4 digits.
        /// </summary>
        private async Task GithubCheck()
        {
            await Task.Delay(45000); //45sec delay (computer startup..)
            //await Task.Delay(100);
            try
            {
                GitHubClient client = new GitHubClient(new ProductHeaderValue("lively"));
                var releases = await client.Repository.Release.GetAll("rocksdanister", "lively");
                //GitHubClient client = new GitHubClient(new ProductHeaderValue("rePaper"));
                //var releases = await client.Repository.Release.GetAll("rocksdanister", "rePaper");
                var latest = releases[0];

                //string tmp = latest.TagName.Replace("v", string.Empty);
                string tmp = Regex.Replace(latest.TagName, "[A-Za-z ]", "");
                var gitVersion = new Version(tmp);
                var appVersion = new Version(System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString());
                var result = gitVersion.CompareTo(appVersion);
                if (result > 0) //github ver greater, update available!
                {
                    _notifyIcon.ShowBalloonTip(2000, "lively", Properties.Resources.toolTipUpdateMsg, ToolTipIcon.None);
                    update_traybtn.Text = Properties.Resources.txtContextMenuUpdate2;
                    hyperlinkUpdateBannerText.Text = Properties.Resources.txtUpdateBanner+" v" + tmp;
                    hyperlinkUpdateBanner.Visibility = Visibility.Visible;
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
            catch (Exception e)
            {
                update_traybtn.Text = Properties.Resources.txtContextMenuUpdate5;
                Logger.Error("Error checking for update: " + e.Message);
            }
        }
        #endregion github_update_check

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
                multiscreen = true;
            }
            else
            {
                multiscreen = false;
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
                Debug.WriteLine("Display Settings Changed Event. ");
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
                Debug.WriteLine("tobereloaded:- " + item.DeviceName);
                Logger.Info("Display(s) wallpapers to load:-" + item.DeviceName);
            }

            if (!wallpapersToBeLoaded.SequenceEqual(SetupDesktop.wallpapers) || _startupRun)
            {
                SetupDesktop.CloseAllWallpapers(); //todo: only close wallpapers that which is running on disconnected display.
                Logger.Info("Restarting/Restoring All Wallpaper(s)");

                //remove wp's with file missing on disk, except for url type( filePath =  website url).
                if( wallpapersToBeLoaded.RemoveAll(x => !File.Exists(x.FilePath) && x.Type != SetupDesktop.WallpaperType.url && x.Type != SetupDesktop.WallpaperType.video_stream) > 0)
                {
                    _notifyIcon.ShowBalloonTip(10000,"lively",Properties.Resources.toolTipWallpaperSkip, ToolTipIcon.None);
                }

                if(SaveData.config.WallpaperArrangement == WallpaperArrangement.span)
                {
                    //unlikely to happen unless user edits the json file manually or some file error? 
                    if(wallpapersToBeLoaded.Count > 1)
                    {
                        //span across all display(s), only 1 wp allowed!
                        wallpapersToBeLoaded.RemoveRange(1, (wallpapersToBeLoaded.Count-1) );
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
                    MessageBox.Show("Failed to setup startup", Properties.Resources.txtLivelyErrorMsgTitle);
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
                    shortcut.WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory;
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
            List<SaveData.LivelyInfo> tmpLoadedWallpapers = new List<SaveData.LivelyInfo>();

            Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + "\\wallpapers"); //creates if does not exist.
            var dir = Directory.GetDirectories(AppDomain.CurrentDomain.BaseDirectory + "\\wallpapers");
            foreach (var item in dir)
            {
                if(File.Exists(item + "\\LivelyInfo.json"))
                {
                    if (SaveData.LoadWallpaperMetaData(item))
                    {
                        if (SaveData.info.Type == SetupDesktop.WallpaperType.url)
                        {
                            Logger.Info("Skipping url type wallpaper(not allowed in Library):- " + SaveData.info.FileName + " " + SaveData.info.Type);
                            continue;
                        }

                        SaveData.info.FileName = item + "\\" + SaveData.info.FileName;
                        //SaveData.info.Preview = null;
                        //SaveData.info.Thumbnail = null;
                        SaveData.info.Preview = item + "\\" + SaveData.info.Preview;
                        SaveData.info.Thumbnail = item + "\\" + SaveData.info.Thumbnail;
                        if (File.Exists(SaveData.info.FileName)) //&& File.Exists(SaveData.info.Thumbnail) && File.Exists(SaveData.info.Preview) )
                        {
                            Logger.Info("Loading Wallpaper:- " + SaveData.info.FileName + " " + SaveData.info.Type);
                            //tileDataList.Add(new TileData(SaveData.info));
                            tmpLoadedWallpapers.Add(info);
                            #region testing
                            for (int i = 0; i < 0; i++)
                            {
                                tileDataList.Add(new TileData(SaveData.info));
                            }

                            #endregion
                        }
                        else
                        {
                            Logger.Info("Files does not exist, skipping wallpaper:- " + SaveData.info.FileName + " " + SaveData.info.Type);
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
            var sortedList = tmpLoadedWallpapers.OrderBy(x => x.Title).ToList();
            foreach (var item in sortedList)
            {
                tileDataList.Add(new TileData(item));
            }

            sortedList.Clear();
            tmpLoadedWallpapers.Clear();
            sortedList = null;
            tmpLoadedWallpapers = null;

            InitializeTilePreviewGifs();

            if(prevSelectedLibIndex < tileDataList.Count )
                wallpapersLV.SelectedIndex = prevSelectedLibIndex;
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
            selectedTile.Add((TileData)wallpapersLV.SelectedItem);
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

        private async void SetWallpaperBtn_Click(object sender, RoutedEventArgs e)
        {
            if (wallpapersLV.SelectedIndex == -1)
                return;

            var selection = (TileData)wallpapersLV.SelectedItem;
            if (selection.LivelyInfo.Type == SetupDesktop.WallpaperType.app || selection.LivelyInfo.Type == SetupDesktop.WallpaperType.godot
                || selection.LivelyInfo.Type == SetupDesktop.WallpaperType.unity || selection.LivelyInfo.Type == SetupDesktop.WallpaperType.unity_audio)
            {
                var ch = await this.ShowMessageAsync(Properties.Resources.msgExternalAppWarningTitle,Properties.Resources.msgExternalAppWarning, MessageDialogStyle.AffirmativeAndNegative,
                            new MetroDialogSettings() { DialogTitleFontSize = 18, ColorScheme = MetroDialogColorScheme.Inverted, DialogMessageFontSize = 16 });

                if (ch == MessageDialogResult.Negative)
                    return;
                else if (ch == MessageDialogResult.Affirmative)
                {

                }
            }

            if(selection.LivelyInfo.Type == SetupDesktop.WallpaperType.app)
            {
                SetupWallpaper(selection.LivelyInfo.FileName, selection.LivelyInfo.Type, selection.LivelyInfo.Arguments);
            }
            else
                SetupWallpaper(selection.LivelyInfo.FileName, selection.LivelyInfo.Type);
        }

        private void MenuItem_SetWallpaper_Click(object sender, RoutedEventArgs e) //contextmenu
        {
            SetWallpaperBtn_Click(null, null);
        }

        private void MenuItem_ShowOnDisk_Click(object sender, RoutedEventArgs e) 
        {
            if (wallpapersLV.SelectedIndex == -1)
                return;

            try
            {
                var obj = (TileData)wallpapersLV.SelectedItem;
                var folderPath = System.IO.Path.GetDirectoryName(obj.LivelyInfo.FileName);
                if (Directory.Exists(folderPath))
                {
                    ProcessStartInfo startInfo = new ProcessStartInfo
                    {
                        Arguments = folderPath,
                        FileName = "explorer.exe"
                    };
                    Process.Start(startInfo);
                }
            }
            catch (Exception e1)
            {
                Logger.Error("folder open error:- " + e1.ToString());
                MessageBox.Show("Failed to open folder:- " + e1.ToString(), Properties.Resources.txtLivelyErrorMsgTitle);
            }
        }

        /// <summary>
        /// Set to true to Cancel zip creation process.
        /// </summary>
        private bool zipWasCanceled = false;
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

            var selection = (TileData)wallpapersLV.SelectedItem;
            string parentDirectory = Path.GetDirectoryName(selection.LivelyInfo.FileName);
            List<string> folderContents = new List<string>();
            folderContents.AddRange(Directory.GetFiles(parentDirectory, "*.*", SearchOption.AllDirectories));

            try
            {
                using (ZipFile zip = new ZipFile(savePath))
                {
                    zip.CompressionLevel = Ionic.Zlib.CompressionLevel.BestCompression;
                    zip.ZipErrorAction = ZipErrorAction.Throw;

                    for (int i = 0; i < folderContents.Count; i++)
                    {
                        try
                        {
                            //adding files in root directory of zip, maintaining folder structure.
                            zip.AddFile(folderContents[i], Path.GetDirectoryName(folderContents[i]).Replace(parentDirectory, string.Empty));
                        }
                        catch
                        {
                            Logger.Info("zip: ignoring some files due to repeated filename.");
                        }
                    }

                    zipWasCanceled = false;
                    progressController = await this.ShowProgressAsync(Properties.Resources.txtLivelyWaitMsgTitle, Properties.Resources.txtCreatingZip, true);
                    progressController.Canceled += ProgressController_Canceled;
                    zip.SaveProgress += Zip_SaveProgress;
                    await Task.Run(() => zip.Save());
                }
            }
            catch (Ionic.Zip.ZipException e1)
            {
                MessageBox.Show("File creation failure(zip):" + e1.ToString(), Properties.Resources.txtLivelyErrorMsgTitle);
                Logger.Error(e1.ToString());
            }
            catch (Exception e2)
            {
                MessageBox.Show("File creation failure:" + e2.ToString(), Properties.Resources.txtLivelyErrorMsgTitle);
                Logger.Error(e2.ToString());
            }

            if (!zipWasCanceled)
            {
                var openDirectory = Path.GetDirectoryName(savePath);
                if (Directory.Exists(openDirectory))
                {
                    ProcessStartInfo startInfo = new ProcessStartInfo
                    {
                        Arguments = openDirectory,
                        FileName = "explorer.exe"
                    };
                    Process.Start(startInfo);
                }
            }
        }
        private async void Zip_SaveProgress(object sender, SaveProgressEventArgs e)
        {
            if (zipWasCanceled) { e.Cancel = true; }

            if (e.EntriesTotal != 0)
            {
                if (progressController != null)
                {
                    progressController.SetProgress((float)e.EntriesSaved / (float)e.EntriesTotal);
                    //   progressController.SetProgress(1);
                }

                if (e.EntriesSaved == e.EntriesTotal && e.EntriesTotal != 0)
                {
                    if (progressController != null)
                    {
                        await progressController.CloseAsync();
                        progressController = null;
                    }
                }
            }
        }

        private async void ProgressController_Canceled(object sender, EventArgs e)
        {
            zipWasCanceled = true;
            progressController.Canceled -= ProgressController_Canceled;
            await progressController.CloseAsync();
            progressController = null;
        }

        private async void MenuItem_DeleteWallpaper_Click(object sender, RoutedEventArgs e)
        {
            if (wallpapersLV.SelectedIndex == -1)
                return;

            var ch = await this.ShowMessageAsync(Properties.Resources.msgDeleteConfirmationTitle, Properties.Resources.msgDeleteConfirmation, MessageDialogStyle.AffirmativeAndNegative,
                       new MetroDialogSettings() {  AffirmativeButtonText ="Yes", NegativeButtonText ="No",DialogTitleFontSize = 18, ColorScheme = MetroDialogColorScheme.Inverted, DialogMessageFontSize = 16, AnimateShow = false, AnimateHide = false });

            if (ch == MessageDialogResult.Negative)
                return;
            else if (ch == MessageDialogResult.Affirmative)
            {

            }
            var selection = (TileData)wallpapersLV.SelectedItem;
            //check if currently running wallpaper
            if (SetupDesktop.wallpapers.FindIndex(x => x.FilePath.Equals(selection.LivelyInfo.FileName, StringComparison.OrdinalIgnoreCase) ) != -1)
            {
                await this.ShowMessageAsync(Properties.Resources.msgDeletionFailureTitle, Properties.Resources.msgDeletionFailure);
                return;
            }

            var folderPath = System.IO.Path.GetDirectoryName(selection.LivelyInfo.FileName);
            if (Directory.Exists(folderPath))
            {
                selectedTile.Remove(selection);
                tileDataList.Remove(selection);
                wallpapersLV.SelectedIndex = -1; //clears selectedTile info panel.

                await Task.Delay(1000); //todo:- find if gif is dealloacted & do this more elegantly.
                try
                {
                    await Task.Run(() => Directory.Delete(folderPath, true)); //thread blocking otherwise
                }
                catch (IOException ex1) 
                {
                    Logger.Error("IOException: failed to delete wp from library, waiting 4sec for gif to dealloac:" +ex1.ToString());
                    await Task.Delay(4000);
                    try
                    {
                        await Task.Run(() => Directory.Delete(folderPath, true));
                    }
                    catch(Exception ie)
                    {
                        MessageBox.Show("Folder Delete Failure:- " + ie.Message, Properties.Resources.txtLivelyErrorMsgTitle);
                    }
                }
                catch (Exception ex2)
                {
                    Logger.Error("WP folder delete error:- " + ex2.ToString());
                    MessageBox.Show("Folder Delete Failure:- " + ex2.Message, Properties.Resources.txtLivelyErrorMsgTitle);
                }
            }  
        }

        #endregion wallpaper_library

        #region systray
        private static System.Windows.Forms.NotifyIcon _notifyIcon;
        private static bool _isExit;

        private void CreateSysTray()
        {
            _notifyIcon = new System.Windows.Forms.NotifyIcon();
            _notifyIcon.DoubleClick += (s, args) => ShowMainWindow();
            //_notifyIcon.Click += (s, args) => ShowMainWindow();
            //_notifyIcon.Icon = System.Drawing.Icon.ExtractAssociatedIcon(System.Reflection.Assembly.GetEntryAssembly().ManifestModule.Name);
            _notifyIcon.Icon = Properties.Icons.icons8_seed_of_life_96_normal;
            _notifyIcon.Visible = true;

            CreateContextMenu();
        }
        public static void SwitchTrayIcon(bool isPaused)
        {
            try
            {
                //don't make much sense with per-display rule in multiple display systems, so turning off.
                if (!multiscreen && !_isExit)
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

        System.Windows.Forms.ToolStripMenuItem update_traybtn;
        private void CreateContextMenu()
        {
            _notifyIcon.ContextMenuStrip =
              new System.Windows.Forms.ContextMenuStrip();
            _notifyIcon.Text = Properties.Resources.txtTitlebar;

            _notifyIcon.ContextMenuStrip.Items.Add(Properties.Resources.txtContextMenuOpenLively, Properties.Icons.icon_monitor).Click += (s, e) => ShowMainWindow();
            _notifyIcon.ContextMenuStrip.Items.Add(Properties.Resources.txtContextMenuCloseAll, Properties.Icons.icon_erase).Click += (s, e) => SetupDesktop.CloseAllWallpapers();
            update_traybtn = new System.Windows.Forms.ToolStripMenuItem(Properties.Resources.txtContextMenuUpdate1, Properties.Icons.icon_update);
            update_traybtn.Click += (s, e) => Process.Start("https://github.com/rocksdanister/lively");
            update_traybtn.Enabled = false;
            _notifyIcon.ContextMenuStrip.Items.Add(update_traybtn);

            _notifyIcon.ContextMenuStrip.Items.Add("-");
            _notifyIcon.ContextMenuStrip.Items.Add(Properties.Resources.txtContextMenuExit, Properties.Icons.icon_close).Click += (s, e) => ExitApplication();
        }

        private int prevSelectedLibIndex = -1;
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!_isExit)
            {
                e.Cancel = true;
                if (SaveData.config.IsFirstRun)
                {
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
                //MessageBox.Show(System.Windows.Application.Current.Windows.Count.ToString());
                SystemEvents.DisplaySettingsChanged -= SystemEvents_DisplaySettingsChanged;

                SaveData.config.SafeShutdown = true;
                SaveData.SaveConfig();

                SetupDesktop.CloseAllWallpapers(true);
         
                SetupDesktop.RefreshDesktop();

                //systraymenu dispose
                _notifyIcon.Visible = false;
                //_notifyIcon.Icon = null;
                _notifyIcon.Icon.Dispose();
                _notifyIcon.Icon = null;
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
            if (this.IsVisible)
            {
                if (this.WindowState == WindowState.Minimized)
                {
                    this.WindowState = WindowState.Normal;
                }
                this.Activate();
            }
            else
            {
                UpdateWallpaperLibrary();
                this.Show();
                this.Activate();

                //todo:- make sure it closes in the event restorewallpaper() is already done.
                if (_isRestoringWallpapers && progressController == null)
                {
                    if (this.IsVisible)
                    {
                        try
                        {
                            progressController = await this.ShowProgressAsync(Properties.Resources.txtLivelyWaitMsgTitle, Properties.Resources.msgRestoringPrevWallpapers, true,
                                   new MetroDialogSettings() { AnimateHide = true, AnimateShow = false });
                        }
                        catch (Exception) //just in case.
                        {
                            progressController = null;
                        }
                    }
                }
            }
        }

        #endregion systray

        #region wp_setup
        /// <summary>
        /// Sets up wallpaper, shows dialog to select display if multiple displays are detected.
        /// </summary>
        /// <param name="path">wallpaper location.</param>
        /// <param name="type">wallpaper category.</param>
        private async void SetupWallpaper(string path, SetupDesktop.WallpaperType type, string args = null)
        {
            if(_isRestoringWallpapers)
            {
                _ = Task.Run(() => (MessageBox.Show(Properties.Resources.msgRestoringInProgress, Properties.Resources.txtLivelyWaitMsgTitle) ));
                return;
            }
            /*
            if(SaveData.config.VidPlayer == VideoPlayer.mpv && type == SetupDesktop.WallpaperType.video)
            {
                SetupWallpaper(@"C:\Users\rocks\source\repos\livelywpf\livelywpf\bin\x86\Release\external\mpv\mpv.exe", SetupDesktop.WallpaperType.app, "\"" + path + "\"" + " --loop-file --keep-open");
                return;
            }
            */

            SaveData.WallpaperLayout tmpData = new SaveData.WallpaperLayout();
            tmpData.Arguments = args;

            if (type == SetupDesktop.WallpaperType.app && args == null)
            {
                var arg = await this.ShowInputAsync(Properties.Resources.msgAppCommandLineArgsTitle, Properties.Resources.msgAppCommandLineArgs, new MetroDialogSettings() 
                { DialogTitleFontSize = 16, DialogMessageFontSize = 14});
                if (arg == null) //cancel btn or ESC key
                    return;

                if (!string.IsNullOrWhiteSpace(arg))
                    tmpData.Arguments = arg;
            }
            else if(type == SetupDesktop.WallpaperType.web || type == SetupDesktop.WallpaperType.url || type == SetupDesktop.WallpaperType.web_audio)
            {
                if(highContrastFix)
                {
                    Logger.Info("behind-icon mode, skipping cef.");
                    _ = Task.Run(() => (MessageBox.Show("Web wallpaper is not available in High Contrast mode workaround, coming soon.", "Lively: Error, High Contrast Mode")));
                    return;
                }

                if (!File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory + "\\external\\cef\\LivelyCefSharp.exe")))
                {
                    Logger.Info("cefsharp is missing, skipping wallpaper.");
                    _ = Task.Run(() => (MessageBox.Show(Properties.Resources.msgWebBrowserMissing, Properties.Resources.txtLivelyErrorMsgTitle)));
                    return;
                }
            }
            else if(type == SetupDesktop.WallpaperType.video && SaveData.config.VidPlayer == VideoPlayer.mpv)
            {
                if(!File.Exists(AppDomain.CurrentDomain.BaseDirectory + "\\external\\mpv\\mpv.exe"))
                {
                    _ = Task.Run(() => (MessageBox.Show("mpv player missing!\nwww.github.com/rocksdanister/lively/wiki/Video-Guide", Properties.Resources.txtLivelyErrorMsgTitle)));
                    return;
                }
            }

            if (type == SetupDesktop.WallpaperType.video_stream)
            {
                tmpData.Arguments = YoutubeDLArgGenerate(path);
            }

            if (multiscreen && SaveData.config.WallpaperArrangement == WallpaperArrangement.duplicate)
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
            else if (multiscreen && SaveData.config.WallpaperArrangement == WallpaperArrangement.per)
            {    /*          
                if(type == SetupDesktop.WallpaperType.bizhawk)
                {
                    System.Windows.MessageBox.Show("Currently Bizhawk is not supported in multiple monitor configuration.", "Lively: Hold up");
                    return;
                }
                */
                //monitor select dialog
                DisplaySelectWindow displaySelectWindow = new DisplaySelectWindow
                {
                    Owner = this,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };
                displaySelectWindow.ShowDialog();

                Debug.WriteLine("selecterd display:- " + DisplaySelectWindow.selectedDisplay);

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
                progressController = await this.ShowProgressAsync(Properties.Resources.txtLivelyWaitMsgTitle, Properties.Resources.msgLoadingAppWallpaper, true,
                     new MetroDialogSettings() { AnimateHide = true, AnimateShow = false });
                //progressController.Canceled += ProgressController_Canceled;
            }

            Logger.Info("Setting up wallpaper:-" + tmpData.FilePath);
            SetupDesktop.SetWallpaper(tmpData); //set wallpaper
            float progress = 0;

            while (SetupDesktop.IsProcessWaitDone() == 0)
            {
                if (progressController != null)
                {
                    if (progress > 1)
                        progress = 1;

                    progressController.SetProgress(progress);
                    progress += 100f / SetupDesktop.wallpaperWaitTime; //~approximation

                    if (progressController.IsCanceled)
                    {
                        //cancelled = true;
                        SetupDesktop.TaskProcessWaitCancel();
                        break;
                    }
                }
                await Task.Delay(100);
            }
            if (progressController != null)
            {
                progressController.SetProgress(1);
                //progressController.Canceled -= ProgressController_Canceled;
                await progressController.CloseAsync();
                progressController = null;
            }
        }
        /*
        private async void ProgressController_Canceled(object sender, EventArgs e)
        {
            SetupDesktop.TaskProcessWaitCancel();
            await progressController.CloseAsync();
            progressController = null;
        }
        */

        /// <summary>
        /// Restores saved list of wallpapers. (no dialog asking user for input etc compared to setupdesktop());
        /// </summary>
        /// <param name="layout"></param>
        private async void RestoreWallpaper(List<SaveData.WallpaperLayout> layoutList)
        {
            bool cancelled = false;
            float progress = 0;
            int loadedWallpaperCount = 0;
            _isRestoringWallpapers = true;
            //cant show dialog when app started with window minimized.
            if (this.IsVisible)
            {
                try
                {
                    progressController = await this.ShowProgressAsync(Properties.Resources.txtLivelyWaitMsgTitle, Properties.Resources.msgRestoringPrevWallpapers, true,
                           new MetroDialogSettings() { AnimateHide = true, AnimateShow = false });
                }
                catch(Exception) //just in case.
                {
                    progressController = null;
                }               
            }

            foreach (var layout in layoutList)
            {
                if (layout.Type == SetupDesktop.WallpaperType.web || layout.Type == SetupDesktop.WallpaperType.url || layout.Type == SetupDesktop.WallpaperType.web_audio)
                {
                    if (highContrastFix)
                    {
                        Logger.Info("behind-icon mode, skipping cef.");
                        _ = Task.Run(() => (MessageBox.Show("Web wallpaper is not available in High Contrast mode workaround, coming soon.", "Lively: Error, High Contrast Mode")));
                        continue;
                    }

                    if (!File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory + "\\external\\cef\\LivelyCefSharp.exe")))
                    {
                        Logger.Info("cefsharp is missing, skipping wallpaper.");
                        _ = Task.Run(() => (MessageBox.Show(Properties.Resources.msgWebBrowserMissing, Properties.Resources.txtLivelyErrorMsgTitle)));
                        continue;
                    }
                }
                else if (layout.Type == SetupDesktop.WallpaperType.video && SaveData.config.VidPlayer == VideoPlayer.mpv)
                {
                    if (!File.Exists(AppDomain.CurrentDomain.BaseDirectory + "\\external\\mpv\\mpv.exe"))
                    {
                        _ = Task.Run(() => (MessageBox.Show("mpv player missing!\nwww.github.com/rocksdanister/lively/wiki/Video-Guide", Properties.Resources.txtLivelyErrorMsgTitle)));
                        continue;
                    }
                }


                SaveData.WallpaperLayout tmpData = new SaveData.WallpaperLayout();
                if (multiscreen && (SaveData.config.WallpaperArrangement == WallpaperArrangement.per || SaveData.config.WallpaperArrangement == WallpaperArrangement.duplicate))
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
                SetupDesktop.SetWallpaper(tmpData); //set wallpaper
                loadedWallpaperCount++;

                if (progressController != null)
                {
                    if (progress > 1)
                        progress = 1;
                    progressController.SetProgress(progress);
                    progress += (float)loadedWallpaperCount / (float)layoutList.Count;
                }

                //SetupDesktop.wallpapers.Add(tmpData);
                while (SetupDesktop.IsProcessWaitDone() == 0)
                {
                    await Task.Delay(50);

                    if (progressController != null)
                    {
                        if (progressController.IsCanceled)
                        {
                            cancelled = true;
                            SetupDesktop.TaskProcessWaitCancel();
                            break;
                        }
                    }
                }
                
                if(cancelled)
                { 
                    break;
                }
            }

            _isRestoringWallpapers = false;
            if (progressController != null)
            {
                progressController.SetProgress(1);
                //progressController.Canceled -= ProgressController_Canceled;
                await progressController.CloseAsync();
                progressController = null;
            }
            layoutList.Clear();
            layoutList = null;
        }
        #endregion wp_setup

        #region wallpaper_installer
        private async void WallpaperInstaller(string zipLocation)
        {
            string extractPath = null;
            extractPath = AppDomain.CurrentDomain.BaseDirectory + "\\wallpapers\\" + Path.GetRandomFileName();

            //Todo: implement CheckZip() {thread blocking}, Error will be thrown during extractiong, which is being handled so not a big deal.
            //Ionic.Zip.ZipFile.CheckZip(zipLocation)

            if (Directory.Exists(extractPath)) //likely impossible.
            {
                Debug.WriteLine("same foldername with files, should be impossible... retrying with new random foldername");
                extractPath = AppDomain.CurrentDomain.BaseDirectory + "\\wallpapers\\" + Path.GetRandomFileName();

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
                        progressController = await this.ShowProgressAsync(Properties.Resources.txtLivelyWaitMsgTitle, Properties.Resources.txtLabel39, true);
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
                        catch (Exception)
                        {
                            Logger.Error("Extractionpath delete error");
                            //Debug.WriteLine("extractionpath deletion error");
                        }

                        await this.ShowMessageAsync("Error", "Not Lively wallpaper file.\nCheck out wiki page on how to create proper wallpaper file", MessageDialogStyle.Affirmative,
                            new MetroDialogSettings() { DialogTitleFontSize = 25, ColorScheme = MetroDialogColorScheme.Inverted, DialogMessageFontSize = 16 });

                        return;
                    }
                }
            }
            catch (Ionic.Zip.ZipException e)
            {
                try
                {
                    Directory.Delete(extractPath, true);
                }
                catch (Exception)
                {
                    Logger.Error("Extractionpath delete error");
                    Debug.WriteLine("extractionpath deletion error");
                }

                if (progressController != null)
                {
                    await progressController.CloseAsync();
                    progressController = null;
                }

                Logger.Error(e.ToString());
                MessageBox.Show(Properties.Resources.msgDamangedLivelyFile, Properties.Resources.txtLivelyErrorMsgTitle);
                return;
            }
            catch (Exception ex)
            {
                try
                {
                    Directory.Delete(extractPath, true);
                }
                catch (Exception)
                {
                    Logger.Error("Extractionpath delete error");
                    Debug.WriteLine(" extractiontpath deletion error");
                }

                //System.Console.Error.WriteLine("zip-exception: " + ex.Message);
                if (progressController != null)
                {
                    await progressController.CloseAsync();
                    progressController = null;
                }

                Logger.Error(ex.ToString());
                MessageBox.Show(ex.Message, "Zip Error");
                return;
            }

            UpdateWallpaperLibrary();
            foreach (var item in tileDataList)
            {
                if(Path.GetDirectoryName(item.LivelyInfo.FileName).Equals(Path.GetDirectoryName(extractPath), StringComparison.Ordinal))
                {
                    wallpapersLV.SelectedItem = item;
                    break;
                }
            }


            //add new wallpaper to library
            //var dir = Directory.GetDirectories(AppDomain.CurrentDomain.BaseDirectory + "\\wallpapers");            
            /*
            if (File.Exists(extractPath + "\\LivelyInfo.json"))
            {
                //load it into SaveData.info
                if (SaveData.LoadWallpaperMetaData(extractPath))
                {
                    SaveData.info.FileName = extractPath + "\\" + SaveData.info.FileName;
                    SaveData.info.Preview = extractPath + "\\" + SaveData.info.Preview;
                    SaveData.info.Thumbnail = extractPath + "\\" + SaveData.info.Thumbnail;
                    Debug.WriteLine(SaveData.info.FileName + " " + SaveData.info.Type);

                    tileDataList.Add(new TileData(SaveData.info));
                    textBoxLibrarySearch.Text = null;
                    wallpapersLV.SelectedIndex = wallpapersLV.Items.Count - 1;
                }
                else
                {
                    MessageBox.Show("Damaged wallpaper file, redownload the file & try again.", "Zip Error");
                    try
                    {
                        Directory.Delete(extractPath, true);
                    }
                    catch (Exception)
                    {
                        Logger.Error("Extractionpath delete error");
                        Debug.WriteLine(" extractiontpath deletion error");
                    }
                }
            }
            */
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

        private async void Zip_ExtractProgress(object sender, ExtractProgressEventArgs e)
        {
            if (e.EntriesTotal != 0)
            {
                if (progressController != null)
                {
                    progressController.SetProgress((float)e.EntriesExtracted / (float)e.EntriesTotal);
                 //   progressController.SetProgress(1);
                }

                Debug.WriteLine((float)e.EntriesExtracted / (float)e.EntriesTotal);
            }

            if(e.EntriesExtracted == e.EntriesTotal && e.EntriesTotal != 0)
            {
                if (progressController != null)
                {
                    await progressController.CloseAsync();
                    progressController = null;
                }
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
            MessageBox.Show(Properties.Resources.txtComingSoon);
            return;

            var dir = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory + @"\external\bizhawk", "EmuHawk.exe", SearchOption.AllDirectories); //might be slow, only check top?
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
                       new MetroDialogSettings() { DialogTitleFontSize = 18, ColorScheme = MetroDialogColorScheme.Inverted, DialogMessageFontSize = 16 });

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
            var url = await this.ShowInputAsync("Stream", "Load video, youtube...", new MetroDialogSettings() { DialogTitleFontSize = 16, DialogMessageFontSize = 14, DefaultText = String.Empty });
            if (string.IsNullOrEmpty(url))
                return;

            if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + "\\external\\mpv\\youtube-dl.exe") && File.Exists(AppDomain.CurrentDomain.BaseDirectory + "\\external\\mpv\\mpv.exe"))
            {
                SetupWallpaper(url, SetupDesktop.WallpaperType.video_stream);
                //SetupWallpaper(AppDomain.CurrentDomain.BaseDirectory + "\\external\\mpv\\mpv.exe", SetupDesktop.WallpaperType.app, "\"" + url + "\"" + " --loop-file --keep-open");
            }
            else
            {
                _ = Task.Run(() => (MessageBox.Show("youtube-dl & mpv player is required for video stream playback!" +
                                            "\nhttps://github.com/rocksdanister/lively/wiki/Youtube-Wallpaper", Properties.Resources.txtLivelyErrorMsgTitle)));
            }
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

            if (streamWP != null)
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

            appMuteToggle.IsChecked = !SaveData.config.MuteAppWP;

            TileAnimateToggle.IsChecked = SaveData.config.LiveTile;
            //fpsUIToggle.IsChecked = SaveData.config.Ui120FPS;
            //disableUIHWToggle.IsChecked = SaveData.config.UiDisableHW;

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
            StartupToggle.IsChecked = SaveData.config.Startup;

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
                SaveData.config.Theme = 0;
                SaveData.SaveConfig();
                comboBoxTheme.SelectedIndex = SaveData.config.Theme;
            }

            #endregion shit

        }

        private void ComboBoxPauseAlgorithm_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (comboBoxPauseAlgorithm.SelectedIndex == 1)
            {
                if (multiscreen)
                {
                    comboBoxPauseAlgorithm.SelectedIndex = 0;
                    MessageBox.Show("Currently this algorithm is incomplete in multiple display systems, disabling.");
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
            //Process.Start("https://github.com/rocksdanister/lively/wiki");
            HelpWindow w = new HelpWindow
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

            if (videoWp != null)
                RestoreWallpaper(videoWp);
        }

        /// <summary>
        ///  Gif player change, restarts currently playing wp's to newly selected system.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ComboBoxGIFPlayer_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            /*
            if (setup == null)
                return;
            */
            SaveData.config.GifPlayer = (SaveData.GIFPlayer)comboBoxGIFPlayer.SelectedIndex;
            SaveData.SaveConfig();

            var result = SetupDesktop.wallpapers.FindAll(x => x.Type == SetupDesktop.WallpaperType.gif);
            SetupDesktop.CloseAllWallpapers(SetupDesktop.WallpaperType.gif);

            //no gif wp's currently running to restore; ignore.
            if (result == null)
                return;
            else
                RestoreWallpaper(result);
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(e.Uri.AbsoluteUri);
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
                    if (this.IsVisible)
                        this.Activate(); //bugfix.
                    MessageBox.Show(Properties.Resources.msgDragDropOtherFormats +"\n\n" + droppedFiles[0], Properties.Resources.msgDragDropOtherFormatsTitle);
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

        private void WebLoadDragDrop(string link)
        {
            if (link.Contains("youtube.com/watch?v=")) //drag drop only for youtube.com streams
            {
                if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + "\\external\\mpv\\youtube-dl.exe") && File.Exists(AppDomain.CurrentDomain.BaseDirectory + "\\external\\mpv\\mpv.exe"))
                {
                    //YoutubeDLArgGenerate(link);
                    SetupWallpaper(link, SetupDesktop.WallpaperType.video_stream);
                }
                else
                {
                    _ = Task.Run(() => (MessageBox.Show("youtube-dl & mpv player is required for youtube playback!" +
                                                "\nhttps://github.com/rocksdanister/lively/wiki/Youtube-Wallpaper", Properties.Resources.txtLivelyErrorMsgTitle)));
                    //fallback action..
                    SetupWallpaper(link, SetupDesktop.WallpaperType.url); 
                }
            }
            else
            {
                SetupWallpaper(link, SetupDesktop.WallpaperType.url);
            }
        }

        private string YoutubeDLArgGenerate(string link)
        {
            string quality = null;
            switch(SaveData.config.StreamQuality)
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

            return "\"" + link + "\"" + " --loop-file --keep-open" + quality;
            //SetupWallpaper(link, SetupDesktop.WallpaperType.video_stream, "\"" + link + "\"" + " --loop-file --keep-open" + quality);
        }

        public readonly static string[] formatsVideo = { ".dat", ".wmv", ".3g2", ".3gp", ".3gp2", ".3gpp", ".amv", ".asf",  ".avi", ".bin", ".cue", ".divx", ".dv", ".flv", ".gxf", ".iso", ".m1v", ".m2v", ".m2t", ".m2ts", ".m4v",
                                        ".mkv", ".mov", ".mp2", ".mp2v", ".mp4", ".mp4v", ".mpa", ".mpe", ".mpeg", ".mpeg1", ".mpeg2", ".mpeg4", ".mpg", ".mpv2", ".mts", ".nsv", ".nuv", ".ogg", ".ogm", ".ogv", ".ogx", ".ps", ".rec", ".rm",
                                        ".rmvb", ".tod", ".ts", ".tts", ".vob", ".vro", ".webm" };
        static bool IsVideoFile(string path)
        {
            if (formatsVideo.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
            {
                return true;
            }
            return false;
        }

        private void Button_AppRule_Click(object sender, RoutedEventArgs e)
        {
            ApplicationRuleDialogWindow w = new ApplicationRuleDialogWindow
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

        dialogues_general.Changelog changelogWindow = null;
        private void lblVersionNumber_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (changelogWindow == null)
            {
                changelogWindow = new dialogues_general.Changelog
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

        #endregion ui_events
    }
}

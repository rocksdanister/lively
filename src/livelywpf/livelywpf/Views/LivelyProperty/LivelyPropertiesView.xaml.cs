using livelywpf.Core;
using livelywpf.Core.API;
using livelywpf.Helpers;
using livelywpf.Helpers.Files;
using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using livelywpf.Models;
using Path = System.IO.Path;
using Microsoft.Extensions.DependencyInjection;
using livelywpf.Services;
using System.Linq;
using livelywpf.Views;
using livelywpf.Helpers.Storage;

namespace livelywpf.Views.LivelyProperty
{
    /// <summary>
    /// Interaction logic for LivelyPropertiesView.xaml
    /// </summary>
    public partial class LivelyPropertiesView : Page
    {
        #region init

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly string livelyPropertyCopyPath;
        private readonly ILibraryModel wallpaperData;
        private readonly ILivelyScreen screen;
        private JObject livelyPropertyCopyData;

        //UI
        private readonly Thickness margin = new Thickness(0, 10, 0, 0);
        private readonly double maxWidth = 200;

        private readonly IUserSettingsService userSettings;
        private readonly IDesktopCore desktopCore;

        public LivelyPropertiesView(ILibraryModel model)
        {
            userSettings = App.Services.GetRequiredService<IUserSettingsService>();
            desktopCore = App.Services.GetRequiredService<IDesktopCore>();

            InitializeComponent();
            wallpaperData = model;
            try
            {
                var wpInfo = GetLivelyPropertyDetails(model, userSettings.Settings.WallpaperArrangement, userSettings.Settings.SelectedDisplay);
                this.livelyPropertyCopyPath = wpInfo.Item1;
                this.screen = wpInfo.Item2;
            }
            catch (Exception e)
            {
                Logger.Error(e.ToString());
                return;
            }
            LoadUI();
        }

        private void LoadUI()
        {
            try
            {
                if (livelyPropertyCopyPath != null)
                {
                    this.livelyPropertyCopyData = JsonUtil.Read(livelyPropertyCopyPath);
                }
                GenerateUIElements();
            }
            catch (Exception e)
            {
                Logger.Error(e.ToString());
                _= Task.Run(() => (MessageBox.Show(e.ToString(), Properties.Resources.TitleAppName)));
            }
        }

        private void GenerateUIElements()
        {
            if (livelyPropertyCopyData == null)
            {
                var msg = "Property file not found!";
                if (wallpaperData.LivelyInfo.Type == WallpaperType.video ||
                    wallpaperData.LivelyInfo.Type == WallpaperType.videostream ||
                    wallpaperData.LivelyInfo.Type == WallpaperType.gif ||
                    wallpaperData.LivelyInfo.Type == WallpaperType.picture)
                {
                    msg += "\n(Mpv player is required.)";
                }
                //Empty..
                AddUIElement(new TextBlock
                {
                    Text = msg,
                    //Background = Brushes.Red,
                    FontSize = 18,
                    Foreground = Brushes.Gray,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Margin = new Thickness(0, 50, 0, 0)
                });
                return;
            }
            else if (livelyPropertyCopyData.Count == 0)
            {
                //Empty..
                AddUIElement(new TextBlock
                {
                    Text = "El Psy Congroo",
                    Foreground = Brushes.Yellow,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Margin = margin
                });
                return;
            }

            dynamic obj = null;
            foreach (var item in livelyPropertyCopyData)
            {
                string uiElementType = item.Value["type"].ToString();
                if (uiElementType.Equals("slider", StringComparison.OrdinalIgnoreCase))
                {
                    var xamlSlider = new Slider()
                    {
                        Name = item.Key,
                        MaxWidth = maxWidth,
                        MinWidth = maxWidth,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        Margin = margin,
                        Minimum = (double)item.Value["min"],
                        Maximum = (double)item.Value["max"],
                        Value = (double)item.Value["value"],
                        AutoToolTipPlacement = System.Windows.Controls.Primitives.AutoToolTipPlacement.TopLeft,
                    };
                    if (item.Value["step"] != null)
                    {
                        if (!String.IsNullOrWhiteSpace(item.Value["step"].ToString()))
                        {
                            xamlSlider.TickFrequency = (double)item.Value["step"];
                            xamlSlider.AutoToolTipPrecision = 2;
                            xamlSlider.IsSnapToTickEnabled = true;
                        }
                    }
                    else
                    {
                        xamlSlider.TickFrequency = 1;
                        xamlSlider.IsSnapToTickEnabled = true;
                        xamlSlider.TickPlacement = System.Windows.Controls.Primitives.TickPlacement.None;
                    }
                    xamlSlider.ValueChanged += XamlSlider_ValueChanged;
                    obj = xamlSlider;
                }
                else if (uiElementType.Equals("textbox", StringComparison.OrdinalIgnoreCase))
                {
                    var tb = new TextBox
                    {
                        Name = item.Key,
                        Text = item.Value["value"].ToString(),
                        AcceptsReturn = true,
                        MaxWidth = maxWidth,
                        MinWidth = maxWidth,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        Margin = margin
                    };
                    tb.TextChanged += Textbox_TextChanged;
                    obj = tb;
                }
                else if (uiElementType.Equals("button", StringComparison.OrdinalIgnoreCase))
                {
                    var btn = new Button
                    {
                        Name = item.Key,
                        Content = item.Value["value"].ToString(),
                        MaxWidth = maxWidth,
                        MinWidth = maxWidth,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        Margin = margin
                    };
                    btn.Click += Button_Click;
                    obj = btn;
                }
                else if (uiElementType.Equals("color", StringComparison.OrdinalIgnoreCase))
                {
                    var pb = new Rectangle
                    {
                        Name = item.Key,
                        Fill = (SolidColorBrush)new BrushConverter().ConvertFromString(item.Value["value"].ToString()),
                        Stroke = new SolidColorBrush(Color.FromRgb(200, 200 ,200)),
                        StrokeThickness = 0.25,
                        MinWidth = maxWidth,
                        MaxWidth = maxWidth,
                        MaxHeight = 15,
                        MinHeight = 15,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        Margin = margin,
                        RadiusX = 5,
                        RadiusY = 5,
                    };
                    pb.MouseUp += Rectangle_Click;
                    obj = pb;
                }
                else if (uiElementType.Equals("checkbox", StringComparison.OrdinalIgnoreCase))
                {
                    var chk = new CheckBox
                    {
                        Name = item.Key,
                        Content = item.Value["text"].ToString(),
                        IsChecked = (bool)item.Value["value"],
                        HorizontalAlignment = HorizontalAlignment.Left,
                        MaxWidth = maxWidth,
                        MinWidth = maxWidth,
                        Margin = margin
                    };
                    chk.Checked += Checkbox_CheckedChanged;
                    chk.Unchecked += Checkbox_CheckedChanged;
                    obj = chk;
                }
                else if (uiElementType.Equals("dropdown", StringComparison.OrdinalIgnoreCase))
                {
                    var cmbBox = new ComboBox()
                    {
                        Name = item.Key,
                        MaxWidth = maxWidth,
                        MinWidth = maxWidth,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        Margin = margin,
                        SelectedIndex = (int)item.Value["value"],

                    };
                    foreach (var dropItem in item.Value["items"])
                    {
                        cmbBox.Items.Add(dropItem);
                    }
                    cmbBox.SelectionChanged += XamlCmbBox_SelectionChanged;
                    obj = cmbBox;
                }
                else if (uiElementType.Equals("folderDropdown", StringComparison.OrdinalIgnoreCase))
                {
                    var cmbBox = new ComboBox
                    {
                        Name = item.Key,
                        MaxWidth = maxWidth,
                        MinWidth = maxWidth,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        Margin = margin
                    };
                    //filter syntax: "*.jpg|*.png"
                    var files = GetFileNames(Path.Combine(Path.GetDirectoryName(wallpaperData.FilePath), item.Value["folder"].ToString()),
                                                item.Value["filter"].ToString(),
                                                SearchOption.TopDirectoryOnly);

                    foreach (var file in files)
                    {
                        cmbBox.Items.Add(file);
                    }
                    cmbBox.SelectedIndex = Array.FindIndex(files, x => x.Contains(item.Value["value"].ToString())); //returns -1 if not found, none selected.
                    cmbBox.SelectionChanged += XamlFolderCmbBox_SelectionChanged;
                    obj = cmbBox;
                }
                else if (uiElementType.Equals("label", StringComparison.OrdinalIgnoreCase))
                {
                    var label = new Label
                    {
                        Name = item.Key,
                        Content = item.Value["value"].ToString(),
                        MaxWidth = maxWidth,
                        MinWidth = maxWidth,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        Margin = margin
                    };
                    obj = label;
                }
                else
                {
                    continue;
                }

                //Title
                if (item.Value["text"] != null &&
                    !uiElementType.Equals("checkbox", StringComparison.OrdinalIgnoreCase) &&
                    !uiElementType.Equals("label", StringComparison.OrdinalIgnoreCase))
                {

                    AddUIElement(new Label
                    {
                        Content = item.Value["text"].ToString(),
                        HorizontalAlignment = HorizontalAlignment.Left,
                        MaxWidth = maxWidth,
                        MinWidth = maxWidth,
                        Margin = margin
                    });
                }

                AddUIElement(obj);
                //File browser for folderdropdown.
                if (uiElementType.Equals("folderDropdown", StringComparison.OrdinalIgnoreCase))
                {
                    var folderDropDownOpenFileBtn = new Button()
                    {
                        Tag = item.Key,
                        Content = Properties.Resources.TextBrowse,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        MaxWidth = maxWidth,
                        MinWidth = maxWidth,
                        Margin = new Thickness(0, 5, 0, 0),
                    };
                    folderDropDownOpenFileBtn.Click += FolderDropDownOpenFileBtn_Click;
                    AddUIElement(folderDropDownOpenFileBtn);
                }
            }

            //restore-default btn.
            var defaultBtn = new Button
            {
                Name = "defaultBtn",
                Content = "Restore Default",
                MaxWidth = maxWidth,
                MinWidth = maxWidth,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = margin
            };
            defaultBtn.Click += DefaultBtn_Click;
            AddUIElement(defaultBtn);
        }

        private void AddUIElement(dynamic obj)
        {
            uiPanel.Children.Add(obj);
        }

        #endregion //init

        #region slider

        private void XamlSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                var item = (Slider)sender;
                WallpaperSendMsg(new LivelySlider() { Name = item.Name, Value = item.Value, Step = item.TickFrequency });
                livelyPropertyCopyData[item.Name]["value"] = item.Value;
                UpdatePropertyFile();
            }
            catch { }
        }

        #endregion //slider

        #region dropdown

        private void XamlFolderCmbBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var item = (ComboBox)sender;
                var filePath = Path.Combine(livelyPropertyCopyData[item.Name]["folder"].ToString(), item.SelectedItem.ToString()); //filename is unique.
                WallpaperSendMsg(new LivelyFolderDropdown() { Name = item.Name, Value = filePath });
                livelyPropertyCopyData[item.Name]["value"] = item.SelectedItem.ToString();
                UpdatePropertyFile();
            }
            catch { }
        }

        private void XamlCmbBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var item = (ComboBox)sender;
                WallpaperSendMsg(new LivelyDropdown() { Name = item.Name, Value = item.SelectedIndex });
                livelyPropertyCopyData[item.Name]["value"] = item.SelectedIndex;
                UpdatePropertyFile();
            }
            catch { }
        }

        private void FolderDropDownOpenFileBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var btn = sender as Button;
                //find the folderdropdown control..
                ComboBox cmbBox = null;
                foreach (object element in uiPanel.Children)
                {
                    if ((element as FrameworkElement).Name == btn.Tag.ToString())
                    {
                        cmbBox = (ComboBox)element;
                        break;
                    }
                }
                if (cmbBox == null)
                {
                    return;
                }

                foreach (var lp in livelyPropertyCopyData)
                {
                    string uiElementType = lp.Value["type"].ToString();
                    if (uiElementType.Equals("folderDropdown", StringComparison.OrdinalIgnoreCase) && btn.Tag.ToString() == lp.Key)
                    {
                        Microsoft.Win32.OpenFileDialog openFileDlg = new Microsoft.Win32.OpenFileDialog
                        {
                            Title = Properties.Resources.TitleAddWallpaper,
                            CheckFileExists = true,
                            CheckPathExists = true,
                            Multiselect = true,
                        };
                        openFileDlg.Filter = $"{lp.Value["text"]}|{lp.Value["filter"].ToString().Replace("|", ";")}";
                        if (openFileDlg.ShowDialog() == true)
                        {
                            var destFiles = new List<string>();
                            var destFolder = Path.Combine(Path.GetDirectoryName(wallpaperData.FilePath), lp.Value["folder"].ToString());
                            //copy the new file over..
                            foreach (var srcFile in openFileDlg.FileNames)
                            {
                                var destFile = Path.Combine(destFolder, Path.GetFileName(srcFile));
                                if (!File.Exists(destFile))
                                {
                                    File.Copy(srcFile, destFile);
                                }
                                else
                                {
                                    destFile = FileOperations.NextAvailableFilename(destFile);
                                    File.Copy(srcFile, destFile);
                                }
                                destFiles.Add(Path.GetFileName(destFile));
                            }
                            destFiles.Sort();
                            //add copied files to bottom of dropdown..
                            foreach (var file in destFiles)
                            {
                                cmbBox.Items.Add(file);
                            }
                        }
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString());
                _ = MessageBox.Show(ex.Message, Properties.Resources.TextError);
            }
        }

        private static string[] GetFileNames(string path, string searchPattern, SearchOption searchOption)
        {
            string[] searchPatterns = searchPattern.Split('|');
            List<string> files = new List<string>();
            foreach (string sp in searchPatterns)
                files.AddRange(System.IO.Directory.GetFiles(path, sp, searchOption));
            files.Sort();

            List<string> tmp = new List<string>();
            foreach (var item in files)
            {
                tmp.Add(Path.GetFileName(item));
            }
            return tmp.ToArray();
        }

        #endregion //dropdown

        #region color picker

        private void Rectangle_Click(object sender, EventArgs e)
        {
            try
            {
                var item = (Rectangle)sender;
                var fill = ((SolidColorBrush)item.Fill).Color;
                var cpicker = new Views.Dialogues.ColorDialog(new Color() { A = fill.A, R = fill.R, G = fill.G, B = fill.B });
                var appWindow = App.Services.GetRequiredService<MainWindow>();
                if (appWindow.IsVisible)
                {
                    cpicker.Owner = appWindow;
                    cpicker.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                }
                else
                {
                    //spawn close to cursor..
                    var cursor = System.Windows.Forms.Cursor.Position;
                    var screen = ScreenHelper.GetScreenFromPoint(cursor);
                    cpicker.WindowStartupLocation = WindowStartupLocation.Manual;
                    cpicker.Top = (cursor.Y + cpicker.Height) > screen.Bounds.Bottom ? cursor.Y - cpicker.Height : cursor.Y;
                    cpicker.Left = (cursor.X + cpicker.Width) > screen.Bounds.Right ? cursor.X - cpicker.Width : cursor.X;
                }

                if (cpicker.ShowDialog() == true)
                {
                    item.Fill = new SolidColorBrush(Color.FromArgb(cpicker.CurrentColor.A, cpicker.CurrentColor.R, cpicker.CurrentColor.G, cpicker.CurrentColor.B));
                    WallpaperSendMsg(new LivelyColorPicker() { Name = item.Name, Value = ToHexValue(cpicker.CurrentColor) });
                    livelyPropertyCopyData[item.Name]["value"] = ToHexValue(cpicker.CurrentColor);
                    UpdatePropertyFile();
                }
            }
            catch { }
        }

        private static string ToHexValue(System.Drawing.Color color)
        {
            return "#" + color.R.ToString("X2") +
                         color.G.ToString("X2") +
                         color.B.ToString("X2");
        }

        private static string ToHexValue(System.Windows.Media.Color color)
        {
            return "#" + color.R.ToString("X2") +
                         color.G.ToString("X2") +
                         color.B.ToString("X2");
        }

        #endregion //color picker

        #region button

        private void DefaultBtn_Click(object sender, EventArgs e)
        {
            if (RestoreOriginalPropertyFile(wallpaperData, livelyPropertyCopyPath))
            {
                uiPanel.Children.Clear();
                LoadUI();
                WallpaperSendMsg(new LivelyButton() { Name = "lively_default_settings_reload", IsDefault = true });
            }
        }

        private void Button_Click(object sender, EventArgs e)
        {
            try
            {
                var item = (Button)sender;
                WallpaperSendMsg(new LivelyButton() { Name = item.Name });
            }
            catch { }
        }

        #endregion //button

        #region checkbox

        private void Checkbox_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                var item = (CheckBox)sender;
                WallpaperSendMsg(new LivelyCheckbox() { Name = item.Name, Value = (item.IsChecked == true) });
                livelyPropertyCopyData[item.Name]["value"] = item.IsChecked == true;
                UpdatePropertyFile();
            }
            catch { }
        }

        #endregion //checkbox

        #region textbox

        private void Textbox_TextChanged(object sender, EventArgs e)
        {
            try
            {
                var item = (TextBox)sender;
                WallpaperSendMsg(new LivelyTextBox() { Name = item.Name, Value = item.Text });
                livelyPropertyCopyData[item.Name]["value"] = item.Text;
                UpdatePropertyFile();
            }
            catch { }
        }

        #endregion //textbox

        #region helpers

        private void UpdatePropertyFile()
        {
            try
            {
                JsonUtil.Write(livelyPropertyCopyPath, livelyPropertyCopyData);
            }
            catch (Exception e)
            {
                Logger.Error(e.ToString());
            }
        }

        private void WallpaperSendMsg(IpcMessage msg)
        {
            switch (userSettings.Settings.WallpaperArrangement)
            {
                case WallpaperArrangement.per:
                    desktopCore.SendMessageWallpaper(screen, wallpaperData, msg);
                    break;
                case WallpaperArrangement.span:
                case WallpaperArrangement.duplicate:
                    desktopCore.SendMessageWallpaper(wallpaperData, msg);
                    break;
            }
        }

        /// <summary>
        /// Copies LivelyProperties.json from root to the per monitor file.
        /// </summary>
        /// <param name="wallpaperData">Wallpaper info.</param>
        /// <param name="livelyPropertyCopyPath">Modified LivelyProperties.json path.</param>
        /// <returns></returns>
        public static bool RestoreOriginalPropertyFile(ILibraryModel wallpaperData, string livelyPropertyCopyPath)
        {
            bool status = false;
            try
            {
                //todo: Use DirectoryWatcher..
                if (wallpaperData.LivelyInfo.Type == WallpaperType.video ||
                    wallpaperData.LivelyInfo.Type == WallpaperType.videostream || 
                    wallpaperData.LivelyInfo.Type == WallpaperType.gif || 
                    wallpaperData.LivelyInfo.Type == WallpaperType.picture)
                {
                    //user defined property file if it exists..
                    var lpp = Path.Combine(wallpaperData.LivelyInfoFolderPath, "LivelyProperties.json");
                    //default property file..
                    var dlpp = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                                "plugins", "mpv", "api", "LivelyProperties.json");
                    if (File.Exists(lpp))
                    {
                        //if user created a file at runtime.. update
                        if (!string.Equals(wallpaperData.LivelyPropertyPath, lpp, StringComparison.OrdinalIgnoreCase))
                        {
                            wallpaperData.LivelyPropertyPath = lpp;
                        }
                    }
                    else
                    {
                        //if user deleted user defined property file at runtime.. update
                        if (!string.Equals(wallpaperData.LivelyPropertyPath, dlpp, StringComparison.OrdinalIgnoreCase))
                        {
                            wallpaperData.LivelyPropertyPath = dlpp;
                        }
                    }
                }

                File.Copy(wallpaperData.LivelyPropertyPath, livelyPropertyCopyPath, true);
                status = true;
            }
            catch(Exception e)
            {
                Logger.Error(e.ToString());
            }
            return status;
        }

        /// <summary>
        /// Get LivelyProperties.json copy filepath and corresponding screen logic.
        /// </summary>
        /// <param name="obj">LibraryModel object</param>
        /// <returns></returns>
        public Tuple<string, ILivelyScreen> GetLivelyPropertyDetails(ILibraryModel obj, WallpaperArrangement arrangement, ILivelyScreen selectedScreen)
        {
            if (obj.LivelyPropertyPath == null)
            {
                throw new ArgumentException("Non-customizable wallpaper.");
            }

            string livelyPropertyCopy = string.Empty;
            ILivelyScreen screen = null;
            var items = desktopCore.Wallpapers.ToList().FindAll(x => x.Model == obj);
            if (items.Count == 0)
            {
                try
                {
                    screen = selectedScreen;
                    var dataFolder = Path.Combine(Program.WallpaperDir, "SaveData", "wpdata");
                    if (screen.DeviceNumber != null)
                    {
                        //Create a directory with the wp foldername in SaveData/wpdata/, copy livelyproperties.json into this.
                        //Further modifications are done to the copy file.
                        string wpdataFolder = null;
                        switch (arrangement)
                        {
                            case WallpaperArrangement.per:
                                wpdataFolder = Path.Combine(dataFolder, new DirectoryInfo(obj.LivelyInfoFolderPath).Name, screen.DeviceNumber);
                                break;
                            case WallpaperArrangement.span:
                                wpdataFolder = Path.Combine(dataFolder, new DirectoryInfo(obj.LivelyInfoFolderPath).Name, "span");
                                break;
                            case WallpaperArrangement.duplicate:
                                wpdataFolder = Path.Combine(dataFolder, new DirectoryInfo(obj.LivelyInfoFolderPath).Name, "duplicate");
                                break;
                        }
                        Directory.CreateDirectory(wpdataFolder);
                        //copy the original file if not found..
                        livelyPropertyCopy = Path.Combine(wpdataFolder, "LivelyProperties.json");
                        if (!File.Exists(livelyPropertyCopy))
                        {
                            File.Copy(obj.LivelyPropertyPath, livelyPropertyCopy);
                        }
                    }
                    else
                    {
                        //todo: fallback, use the original file (restore feature disabled.)
                    }
                }
                catch (Exception e)
                {
                    //todo: fallback, use the original file (restore feature disabled.)
                    Logger.Error(e.ToString());
                }
            }
            else if (items.Count == 1)
            {
                //send regardless of selected display, if wallpaper is running on non-selected display - its modified instead.
                livelyPropertyCopy = items[0].LivelyPropertyCopyPath;
                screen = items[0].Screen;
            }
            else
            {
                switch (arrangement)
                {
                    case WallpaperArrangement.per:
                        {
                            //more than one screen; if selected display, sendpath otherwise send the first one found.
                            int index = items.FindIndex(x => ScreenHelper.ScreenCompare(selectedScreen, x.Screen, DisplayIdentificationMode.deviceId));
                            livelyPropertyCopy = index != -1 ? items[index].LivelyPropertyCopyPath : items[0].LivelyPropertyCopyPath;
                            screen = index != -1 ? items[index].Screen : items[0].Screen;
                        }
                        break;
                    case WallpaperArrangement.span:
                    case WallpaperArrangement.duplicate:
                        {
                            livelyPropertyCopy = items[0].LivelyPropertyCopyPath;
                            screen = items[0].Screen;
                        }
                        break;
                }
            }
            return new Tuple<string, ILivelyScreen>(livelyPropertyCopy, screen);
        }

        #endregion //helpers

    }
}

using Lively.Common;
using Lively.Common.API;
using Lively.Common.Helpers.Files;
using Lively.Common.Helpers.Storage;
using Lively.Grpc.Client;
using Lively.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.Storage.Pickers;
using Windows.UI;
using Lively.Common.Helpers.Pinvoke;
using Microsoft.UI.Dispatching;
using CommunityToolkit.WinUI.UI.Controls;

namespace Lively.UI.WinUI.Views.LivelyProperty
{
    public sealed partial class LivelyPropertiesView : Page
    {
        #region init

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private string livelyPropertyCopyPath;
        private ILibraryModel libraryItem;
        private IDisplayMonitor screen;
        private JObject livelyPropertyCopyData;

        //UI
        private readonly Thickness margin = new Thickness(0, 0, 20, 10);
        private readonly double minWidth = 200;

        private readonly IUserSettingsClient userSettings;
        private readonly IDesktopCoreClient desktopCore;
        private readonly IDisplayManagerClient displayManager;
        private readonly DispatcherQueue dispatcherQueue;

        public LivelyPropertiesView() // Default constructor for OnNavigatedTo
        {
            this.InitializeComponent();
            userSettings = App.Services.GetRequiredService<IUserSettingsClient>();
            desktopCore = App.Services.GetRequiredService<IDesktopCoreClient>();
            displayManager = App.Services.GetRequiredService<IDisplayManagerClient>();
            //MainWindow dispatcher may not be ready yet, creating our own instead..
            dispatcherQueue = DispatcherQueue.GetForCurrentThread() ?? DispatcherQueueController.CreateOnCurrentThread().DispatcherQueue;
        }

        public LivelyPropertiesView(ILibraryModel model) : this()
        {
            CreateUI(model);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e) =>
            CreateUI((ILibraryModel)e.Parameter);

        private void CreateUI(ILibraryModel model)
        {
            libraryItem = model;
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
            ReadUI();
        }

        private void ReadUI()
        {
            try
            {
                if (livelyPropertyCopyPath != null)
                {
                    this.livelyPropertyCopyData = JsonUtil.ReadJObject(livelyPropertyCopyPath);
                }
                GenerateUIElements();
            }
            catch (Exception e)
            {
                restoreDefaultBtn.Visibility = Visibility.Collapsed;
                Logger.Error(e);
            }
        }

        private void GenerateUIElements()
        {
            if (livelyPropertyCopyData == null)
            {
                var msg = "Property file not found!";
                if (libraryItem.LivelyInfo.Type == WallpaperType.video ||
                    libraryItem.LivelyInfo.Type == WallpaperType.videostream ||
                    libraryItem.LivelyInfo.Type == WallpaperType.gif ||
                    libraryItem.LivelyInfo.Type == WallpaperType.picture)
                {
                    msg += "\n(Mpv player is required.)";
                }
                //Empty..
                AddUIElement(new TextBlock
                {
                    Text = msg,
                    //Background = Brushes.Red,
                    FontSize = 18,
                    //Foreground = Brushes.Gray,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Margin = new Thickness(0, 50, 0, 0)
                });
                restoreDefaultBtn.IsEnabled = false;
                return;
            }
            else if (livelyPropertyCopyData.Count == 0)
            {
                //Empty..
                AddUIElement(new TextBlock
                {
                    Text = "El Psy Congroo",
                    //Foreground = Brushes.Yellow,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Margin = margin
                });
                restoreDefaultBtn.IsEnabled = false;
                return;
            }

            UIElement obj = null;
            foreach (var item in livelyPropertyCopyData)
            {
                string uiElementType = item.Value["type"].ToString();
                if (uiElementType.Equals("slider", StringComparison.OrdinalIgnoreCase))
                {
                    var slider = new Slider()
                    {
                        Name = item.Key,
                        //MaxWidth = maxWidth,
                        MinWidth = minWidth,
                        //HorizontalAlignment = HorizontalAlignment.Left,
                        Margin = margin,
                        Minimum = (double)item.Value["min"],
                        Maximum = (double)item.Value["max"],
                        Value = (double)item.Value["value"],
                    };
                    if (item.Value["step"] != null && !string.IsNullOrWhiteSpace(item.Value["step"].ToString()))
                    {
                        slider.StepFrequency = (double)item.Value["step"];
                    }
                    if (item.Value["help"] != null && !string.IsNullOrWhiteSpace(item.Value["help"].ToString()))
                    {
                        ToolTipService.SetToolTip(slider, new ToolTip() { Content = (string)item.Value["help"] });
                    }
                    slider.ValueChanged += XamlSlider_ValueChanged;
                    obj = slider;
                }
                else if (uiElementType.Equals("textbox", StringComparison.OrdinalIgnoreCase))
                {
                    var tb = new TextBox
                    {
                        Name = item.Key,
                        Text = item.Value["value"].ToString(),
                        AcceptsReturn = true,
                        MaxWidth = minWidth,
                        MinWidth = minWidth,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        Margin = margin
                    };
                    if (item.Value["help"] != null && !string.IsNullOrWhiteSpace(item.Value["help"].ToString()))
                    {
                        ToolTipService.SetToolTip(tb, new ToolTip() { Content = (string)item.Value["help"] });
                    }
                    tb.TextChanged += Textbox_TextChanged;
                    obj = tb;
                }
                else if (uiElementType.Equals("button", StringComparison.OrdinalIgnoreCase))
                {
                    var btn = new Button
                    {
                        Name = item.Key,
                        Content = item.Value["value"].ToString(),
                        MaxWidth = minWidth,
                        MinWidth = minWidth,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        Margin = margin
                    };
                    if (item.Value["help"] != null && !string.IsNullOrWhiteSpace(item.Value["help"].ToString()))
                    {
                        ToolTipService.SetToolTip(btn, new ToolTip() { Content = (string)item.Value["help"] });
                    }
                    btn.Click += Btn_Click;
                    obj = btn;
                }
                else if (uiElementType.Equals("color", StringComparison.OrdinalIgnoreCase))
                {
                    var selectedColorBrush = GetSolidColorBrush(item.Value["value"].ToString());
                    var panel = new StackPanel()
                    {
                        Name = item.Key,
                        Margin = margin,
                        Orientation = Orientation.Horizontal
                    };
                    var colorPickerBtn = new ColorPickerButton()
                    {
                        SelectedColor = selectedColorBrush.Color,
                        Width = 60,
                        Height = 33,
                    };
                    var eyeDropBtn = new Button()
                    {
                        //Tag = item.Key, //used for searching the splitbtn
                        //HorizontalAlignment = HorizontalAlignment.Right,
                        Margin = new Thickness(5, 0, 0, 0),
                        Width = 40,
                        Height = 33,
                        Content = new FontIcon
                        {
                            Glyph = "\uEF3C",
                            FontSize = 15,
                        },
                    };
                    panel.Children.Add(colorPickerBtn);
                    panel.Children.Add(eyeDropBtn);
                    colorPickerBtn.Loaded += (_, _) =>
                    {
                        colorPickerBtn.ColorPicker.Tag = panel;
                        colorPickerBtn.ColorPicker.ColorChanged += ColorPicker_ColorChanged;
                    };
                    eyeDropBtn.Click += EyeDropBtn_Click;
                    obj = panel;
                }
                else if (uiElementType.Equals("checkbox", StringComparison.OrdinalIgnoreCase))
                {
                    var chk = new CheckBox
                    {
                        Name = item.Key,
                        Content = item.Value["text"].ToString(),
                        IsChecked = (bool)item.Value["value"],
                        HorizontalAlignment = HorizontalAlignment.Left,
                        //MaxWidth = minWidth,
                        MinWidth = minWidth,
                        Margin = margin
                    };
                    if (item.Value["help"] != null && !string.IsNullOrWhiteSpace(item.Value["help"].ToString()))
                    {
                        ToolTipService.SetToolTip(chk, new ToolTip() { Content = (string)item.Value["help"] });
                    }
                    chk.Checked += Checkbox_CheckedChanged;
                    chk.Unchecked += Checkbox_CheckedChanged;
                    obj = chk;
                }
                else if (uiElementType.Equals("dropdown", StringComparison.OrdinalIgnoreCase))
                {
                    var cmbBox = new ComboBox()
                    {
                        Name = item.Key,
                        MaxWidth = minWidth,
                        MinWidth = minWidth,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        Margin = margin,
                        SelectedIndex = (int)item.Value["value"],
                    };
                    foreach (var dropItem in item.Value["items"])
                    {
                        cmbBox.Items.Add(dropItem.ToString());
                    }
                    if (item.Value["help"] != null && !string.IsNullOrWhiteSpace(item.Value["help"].ToString()))
                    {
                        ToolTipService.SetToolTip(cmbBox, new ToolTip() { Content = (string)item.Value["help"] });
                    }
                    cmbBox.SelectionChanged += XamlCmbBox_SelectionChanged;
                    obj = cmbBox;
                }
                else if (uiElementType.Equals("folderDropdown", StringComparison.OrdinalIgnoreCase))
                {
                    var panel = new StackPanel()
                    {
                        Name = item.Key,
                        Margin = margin,
                        Orientation = Orientation.Horizontal,
                        HorizontalAlignment= HorizontalAlignment.Left,
                    };
                    var cmbBox = new ComboBox
                    {
                        //Tag = item.Key,
                        MaxWidth = minWidth,
                        MinWidth = minWidth,
                        MinHeight = 35,
                    };
                    var fileOpenBtn = new Button()
                    {
                        //Tag = item.Key,
                        Content = new FontIcon
                        {
                            Glyph = "\uE8E5",
                        },
                        Margin = new Thickness(5, 0, 0, 0),
                    };
                    if (item.Value["help"] != null && !string.IsNullOrWhiteSpace(item.Value["help"].ToString()))
                    {
                        ToolTipService.SetToolTip(cmbBox, new ToolTip() { Content = (string)item.Value["help"] });
                    }

                    try
                    {
                        var destDir = Path.Combine(Path.GetDirectoryName(libraryItem.FilePath), item.Value["folder"].ToString());
                        Directory.CreateDirectory(destDir);
                        //filter syntax: "*.jpg|*.png"
                        var files = GetFileNames(destDir, item.Value["filter"].ToString(), SearchOption.TopDirectoryOnly);
                        foreach (var file in files)
                        {
                            cmbBox.Items.Add(file);
                        }
                        cmbBox.SelectedIndex = Array.FindIndex(files, x => x.Contains(item.Value["value"].ToString())); //returns -1 if not found, none selected.
                    }
                    catch (Exception ie1)
                    {
                        Logger.Error($"FolderDropDown({item.Key}) failed to initialize: {ie1.Message}");
                    }
                    cmbBox.SelectionChanged += XamlFolderCmbBox_SelectionChanged;
                    fileOpenBtn.Click += FolderDropDownOpenFileBtn_Click;
                    panel.Children.Add(cmbBox);
                    panel.Children.Add(fileOpenBtn);
                    obj = panel;
                }
                else if (uiElementType.Equals("label", StringComparison.OrdinalIgnoreCase))
                {
                    var label = new TextBlock
                    {
                        Name = item.Key,
                        Text = item.Value["value"].ToString(),
                        //MaxWidth = minWidth,
                        MinWidth = minWidth,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        Margin = margin
                    };
                    obj = label;
                }
                else
                {
                    continue;
                }

                //Title for Slider, ComboBox..
                if (item.Value["text"] != null &&
                    !uiElementType.Equals("checkbox", StringComparison.OrdinalIgnoreCase) &&
                    !uiElementType.Equals("label", StringComparison.OrdinalIgnoreCase))
                {
                    var tb = new TextBlock
                    {
                        Text = item.Value["text"].ToString(),
                        HorizontalAlignment = HorizontalAlignment.Left,
                        //MaxWidth = minWidth,
                        MinWidth = minWidth,
                        Margin = margin
                    };
                    AddUIElement(tb);
                    if (item.Value["help"] != null && !string.IsNullOrWhiteSpace(item.Value["help"].ToString()))
                    {
                        ToolTipService.SetToolTip(tb, new ToolTip() { Content = (string)item.Value["help"] });
                    }
                }

                AddUIElement(obj);
            }
        }

        private void AddUIElement(UIElement obj) => uiPanel.Children.Add(obj);

        #endregion //init

        #region slider

        private void XamlSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            try
            {
                var item = (Slider)sender;
                WallpaperSendMsg(new LivelySlider() { Name = item.Name, Value = item.Value, Step = item.StepFrequency });
                livelyPropertyCopyData[item.Name]["value"] = item.Value;
                UpdatePropertyFile();
            }
            catch { }
        }

        #endregion //slider

        #region dropdown

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

        private void XamlFolderCmbBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var menuItem = (ComboBox)sender;
                var propertyName = (menuItem.Parent as StackPanel).Name;
                var filePath = Path.Combine(livelyPropertyCopyData[propertyName]["folder"].ToString(), menuItem.SelectedItem.ToString()); //filename is unique.
                WallpaperSendMsg(new LivelyFolderDropdown() { Name = propertyName, Value = filePath });
                livelyPropertyCopyData[propertyName]["value"] = menuItem.SelectedItem.ToString();
                UpdatePropertyFile();
            }
            catch { }
        }

        private async void FolderDropDownOpenFileBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var btn = sender as Button;
                //find folder selection ComboBox
                var panel = btn.Parent as StackPanel;
                var cmbBox = panel.Children[0] as ComboBox;

                foreach (var lp in livelyPropertyCopyData)
                {
                    string uiElementType = lp.Value["type"].ToString();
                    if (uiElementType.Equals("folderDropdown", StringComparison.OrdinalIgnoreCase) && panel.Name == lp.Key)
                    {
                        var filePicker = new FileOpenPicker();
                        filePicker.SetOwnerWindow(App.StartFlags.TrayWidget ? new Window() : App.Services.GetRequiredService<MainWindow>());
                        var filterString = lp.Value["filter"].ToString();
                        var filter = filterString == "*" ? new string[] {"*"} : filterString.Replace("*", string.Empty).Split("|");
                        foreach (var item in filter)
                        {
                            filePicker.FileTypeFilter.Add(item);
                        }
                        var selectedFiles = await filePicker.PickMultipleFilesAsync();
                        if (selectedFiles.Count > 0)
                        {
                            var destFiles = new List<string>();
                            var destFolder = Path.Combine(Path.GetDirectoryName(libraryItem.FilePath), lp.Value["folder"].ToString());
                            //copy the new file over..
                            foreach (var srcFile in selectedFiles)
                            {
                                var destFile = Path.Combine(destFolder, Path.GetFileName(srcFile.Path));
                                if (!File.Exists(destFile))
                                {
                                    File.Copy(srcFile.Path, destFile);
                                }
                                else
                                {
                                    destFile = FileOperations.NextAvailableFilename(destFile);
                                    File.Copy(srcFile.Path, destFile);
                                }
                                destFiles.Add(Path.GetFileName(destFile));
                            }
                            destFiles.Sort();
                            //add copied files to bottom of dropdown..
                            foreach (var file in destFiles)
                            {
                                cmbBox.Items.Add(file);
                            }

                            if (selectedFiles.Count == 1)
                            {
                                cmbBox.SelectedIndex = cmbBox.Items.Count - 1;
                            }
                        }
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
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

        private void ColorPicker_ColorChanged(Microsoft.UI.Xaml.Controls.ColorPicker sender, ColorChangedEventArgs args)
        {
            try
            {
                var panel = sender.Tag as StackPanel;
                var colorPickerBtn = panel.Children[0] as ColorPickerButton;
                colorPickerBtn.SelectedColor = Color.FromArgb(
                    255,
                    args.NewColor.R,
                    args.NewColor.G,
                    args.NewColor.B
                );

                WallpaperSendMsg(new LivelyColorPicker() { Name = panel.Name, Value = ToHexValue(args.NewColor) });
                livelyPropertyCopyData[panel.Name]["value"] = ToHexValue(args.NewColor);
                UpdatePropertyFile();
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        private void EyeDropBtn_Click(object sender, RoutedEventArgs e)
        {
            var stackPanel = (sender as Button).Parent as StackPanel;
            var colorPickerBtn = stackPanel.Children[0] as ColorPickerButton;

            var eyeDropper = new ColorEyeDropWindow();
            eyeDropper.Activate();
            eyeDropper.Closed += (_, _) =>
            {
                if (eyeDropper.SelectedColor != null && colorPickerBtn != null)
                {
                    this.DispatcherQueue.TryEnqueue(() => {
                        var color = (Color)eyeDropper.SelectedColor;
                        //not updating colorpicker
                        //colorPickerBtn.SelectedColor = color;
                        colorPickerBtn.ColorPicker.Color = color;
                    });
                }
            };
        }

        //private static string ToHexValue(System.Drawing.Color color)
        //{
        //    return "#" + color.R.ToString("X2") +
        //                 color.G.ToString("X2") +
        //                 color.B.ToString("X2");
        //}

        public static Color GetColorAt(int x, int y)
        {
            IntPtr desk = NativeMethods.GetDesktopWindow();
            IntPtr dc = NativeMethods.GetWindowDC(desk);
            try
            {
                int a = (int)NativeMethods.GetPixel(dc, x, y);
                return Color.FromArgb(255, (byte)((a >> 0) & 0xff), (byte)((a >> 8) & 0xff), (byte)((a >> 16) & 0xff));
            }
            finally
            {
                NativeMethods.ReleaseDC(desk, dc);
            }
        }


        private static string ToHexValue(Color color)
        {
            return "#" + color.R.ToString("X2") +
                         color.G.ToString("X2") +
                         color.B.ToString("X2");
        }

        public SolidColorBrush GetSolidColorBrush(string hexaColor)
        {
            return new SolidColorBrush(Color.FromArgb(
                    255,
                    Convert.ToByte(hexaColor.Substring(1, 2), 16),
                    Convert.ToByte(hexaColor.Substring(3, 2), 16),
                    Convert.ToByte(hexaColor.Substring(5, 2), 16)
                ));
        }

        #endregion //color picker

        #region button

        private void DefaultBtn_Click(object sender, RoutedEventArgs e)
        {
            if (RestoreOriginalPropertyFile(libraryItem, livelyPropertyCopyPath))
            {
                uiPanel.Children.Clear();
                ReadUI();
                WallpaperSendMsg(new LivelyButton() { Name = "lively_default_settings_reload", IsDefault = true });
            }
        }

        private void Btn_Click(object sender, RoutedEventArgs e)
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

        private void Checkbox_CheckedChanged(object sender, RoutedEventArgs e)
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

        private void Textbox_TextChanged(object sender, TextChangedEventArgs e)
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
            _ = dispatcherQueue.TryEnqueue(() =>
            {
                switch (userSettings.Settings.WallpaperArrangement)
                {
                    case WallpaperArrangement.per:
                        desktopCore.SendMessageWallpaper(screen, libraryItem, msg);
                        break;
                    case WallpaperArrangement.span:
                    case WallpaperArrangement.duplicate:
                        desktopCore.SendMessageWallpaper(libraryItem, msg);
                        break;
                }
            });
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
                File.Copy(wallpaperData.LivelyPropertyPath, livelyPropertyCopyPath, true);
                status = true;
            }
            catch (Exception e)
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
        public Tuple<string, IDisplayMonitor> GetLivelyPropertyDetails(ILibraryModel obj, WallpaperArrangement arrangement, IDisplayMonitor selectedScreen)
        {
            if (obj.LivelyPropertyPath == null)
            {
                throw new ArgumentException("Non-customizable wallpaper.");
            }

            string livelyPropertyCopy = string.Empty;
            IDisplayMonitor screen = null;
            var items = desktopCore.Wallpapers.ToList().FindAll(x => x.LivelyInfoFolderPath == obj.LivelyInfoFolderPath);
            if (items.Count == 0)
            {
                try
                {
                    screen = selectedScreen;
                    var dataFolder = Path.Combine(userSettings.Settings.WallpaperDir, Constants.CommonPartialPaths.WallpaperSettingsDir);
                    if (screen?.Index.ToString() != null)
                    {
                        //Create a directory with the wp foldername in SaveData/wpdata/, copy livelyproperties.json into this.
                        //Further modifications are done to the copy file.
                        string wpdataFolder = null;
                        switch (arrangement)
                        {
                            case WallpaperArrangement.per:
                                wpdataFolder = Path.Combine(dataFolder, new DirectoryInfo(obj.LivelyInfoFolderPath).Name, screen.Index.ToString());
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
                screen = displayManager.DisplayMonitors.FirstOrDefault(x => x.Equals(items[0].Display));
            }
            else
            {
                switch (arrangement)
                {
                    case WallpaperArrangement.per:
                        {
                            //more than one screen; if selected display, sendpath otherwise send the first one found.
                            int index = items.FindIndex(x => selectedScreen.Equals(x.Display));
                            livelyPropertyCopy = index != -1 ? items[index].LivelyPropertyCopyPath : items[0].LivelyPropertyCopyPath;
                            screen = index != -1 ? items[index].Display : items[0].Display;
                        }
                        break;
                    case WallpaperArrangement.span:
                    case WallpaperArrangement.duplicate:
                        {
                            livelyPropertyCopy = items[0].LivelyPropertyCopyPath;
                            screen = items[0].Display;
                        }
                        break;
                }
            }
            return new Tuple<string, IDisplayMonitor>(livelyPropertyCopy, screen);
        }

        #endregion //helpers
    }
}

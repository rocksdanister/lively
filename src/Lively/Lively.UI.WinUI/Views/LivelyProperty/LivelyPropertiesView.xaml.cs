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
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.Pickers;
using Windows.UI;
using H.Hooks;
using Lively.Common.Helpers.Pinvoke;
using System.Threading.Tasks;
using System.Threading;

namespace Lively.UI.WinUI.Views.LivelyProperty
{
    public sealed partial class LivelyPropertiesView : Page
    {
        #region init

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly string livelyPropertyCopyPath;
        private readonly ILibraryModel libraryItem;
        private readonly IDisplayMonitor screen;
        private JObject livelyPropertyCopyData;
        private readonly object _sendMsgLock = new();
        private readonly object _colorPickerTipLock = new();

        //UI
        private readonly Thickness margin = new Thickness(0, 10, 20, 10);
        private readonly double minWidth = 200;
        //Color picker
        private LowLevelMouseHook mouseHook;
        private SplitButton currEyeDropSplitBtn;
        private ToggleButton currEyeDropBtn;

        private readonly IUserSettingsClient userSettings;
        private readonly IDesktopCoreClient desktopCore;
        private readonly IDisplayManagerClient displayManager;

        public LivelyPropertiesView(ILibraryModel model)
        {
            userSettings = App.Services.GetRequiredService<IUserSettingsClient>();
            desktopCore = App.Services.GetRequiredService<IDesktopCoreClient>();
            displayManager = App.Services.GetRequiredService<IDisplayManagerClient>();

            this.InitializeComponent();

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
            LoadUI();
        }

        private void LoadUI()
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
                return;
            }

            dynamic obj = null;
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
                    if (item.Value["step"] != null)
                    {
                        if (!string.IsNullOrWhiteSpace(item.Value["step"].ToString()))
                        {
                            slider.TickFrequency = (double)item.Value["step"];
                        }
                    }
                    else
                    {
                        slider.TickFrequency = 1;
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
                        //MaxWidth = minWidth,
                        MinWidth = minWidth,
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
                        //MaxWidth = minWidth,
                        MinWidth = minWidth,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        Margin = margin
                    };
                    btn.Click += Btn_Click;
                    obj = btn;
                }
                else if (uiElementType.Equals("color", StringComparison.OrdinalIgnoreCase))
                {
                    var selectedColorBrush = GetSolidColorBrush(item.Value["value"].ToString());
                    var cpicker = new ColorPicker
                    {
                        Tag = item.Key, //used for searching the splitbtn
                        ColorSpectrumShape = ColorSpectrumShape.Box,
                        IsMoreButtonVisible = false,
                        IsColorSliderVisible = true,
                        IsColorChannelTextInputVisible = true,
                        IsHexInputVisible = true,
                        IsAlphaEnabled = false,
                        IsAlphaSliderVisible = true,
                        Color = selectedColorBrush.Color,
                    };
                    var eyeDropBtn = new ToggleButton()
                    {
                        Tag = item.Key, //used for searching the splitbtn
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Margin = new Thickness(0, 10, 0, 0),
                        Content = new FontIcon
                        {
                            Glyph = "\uEF3C",
                        },
                    };
                    var cpickerPanel = new StackPanel();
                    cpickerPanel.Children.Add(cpicker);
                    cpickerPanel.Children.Add(eyeDropBtn);
                    var sb = new SplitButton
                    {
                        Name = item.Key,
                        Margin = margin,
                        Content = new Border
                        {
                            Width = 32,
                            Height = 32,
                            CornerRadius = new CornerRadius(4),
                            Background = selectedColorBrush,
                        },
                        Flyout = new Flyout
                        {
                            Content = cpickerPanel,
                        },
                    };
                    cpicker.ColorChanged += Cpicker_ColorChanged;
                    eyeDropBtn.Click += EyeDropBtn_Click;
                    obj = sb;
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
                    chk.Checked += Checkbox_CheckedChanged;
                    chk.Unchecked += Checkbox_CheckedChanged;
                    obj = chk;
                }
                else if (uiElementType.Equals("dropdown", StringComparison.OrdinalIgnoreCase))
                {
                    var cmbBox = new ComboBox()
                    {
                        Name = item.Key,
                        //MaxWidth = minWidth,
                        MinWidth = minWidth,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        Margin = margin,
                        SelectedIndex = (int)item.Value["value"],
                    };
                    foreach (var dropItem in item.Value["items"])
                    {
                        cmbBox.Items.Add(dropItem.ToString());
                    }
                    cmbBox.SelectionChanged += XamlCmbBox_SelectionChanged;
                    obj = cmbBox;
                }
                else if (uiElementType.Equals("folderDropdown", StringComparison.OrdinalIgnoreCase))
                {
                    var cmbBox = new ComboBox
                    {
                        Name = item.Key,
                        //MaxWidth = minWidth,
                        MinWidth = minWidth,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        Margin = margin
                    };
                    //filter syntax: "*.jpg|*.png"
                    var files = GetFileNames(Path.Combine(Path.GetDirectoryName(libraryItem.FilePath), item.Value["folder"].ToString()),
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

                //Title
                if (item.Value["text"] != null &&
                    !uiElementType.Equals("checkbox", StringComparison.OrdinalIgnoreCase) &&
                    !uiElementType.Equals("label", StringComparison.OrdinalIgnoreCase))
                {

                    AddUIElement(new TextBlock
                    {
                        Text = item.Value["text"].ToString(),
                        HorizontalAlignment = HorizontalAlignment.Left,
                        //MaxWidth = minWidth,
                        MinWidth = minWidth,
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
                        Content = "Browse",
                        HorizontalAlignment = HorizontalAlignment.Left,
                        //MaxWidth = minWidth,
                        MinWidth = minWidth,
                        Margin = new Thickness(0, 5, 0, 0),
                    };
                    folderDropDownOpenFileBtn.Click += FolderDropDownOpenFileBtn_Click;
                    AddUIElement(folderDropDownOpenFileBtn);
                }
            }

            //restore-default btn.
            var defaultPanel = new StackPanel()
            {
                Orientation = Orientation.Horizontal,
            };
            var defaultIcon = new FontIcon
            {
                Glyph = "\uE777",
                FontSize = 13,
                Margin = new Thickness(0, 0, 5, 0),
            };
            var defaultText = new TextBlock
            {
                Text = "Restore default",
            };
            defaultPanel.Children.Add(defaultIcon);
            defaultPanel.Children.Add(defaultText);
            var defaultBtn = new Button
            {
                Name = "defaultBtn",
                Content = defaultPanel,
                //MaxWidth = minWidth,
                MinWidth = minWidth,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = margin
            };
            defaultBtn.Click += DefaultBtn_Click;
            AddUIElement(defaultBtn);
        }

        private void AddUIElement(dynamic obj) => uiPanel.Children.Add(obj);

        #endregion //init

        #region slider

        private void XamlSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
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

        private async void FolderDropDownOpenFileBtn_Click(object sender, RoutedEventArgs e)
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
                        var filePicker = new FileOpenPicker();
                        filePicker.SetOwnerWindow(App.Services.GetRequiredService<MainWindow>());
                        filePicker.FileTypeFilter.Add("*"); //openFileDlg.Filter = $"{lp.Value["text"]}|{lp.Value["filter"].ToString().Replace("|", ";")}";
                        var selectedFiles = await filePicker.PickMultipleFilesAsync();
                        if (selectedFiles != null)
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

        private void Cpicker_ColorChanged(ColorPicker sender, ColorChangedEventArgs args)
        {
            try
            {
                SplitButton splBtn = null;
                foreach (object element in uiPanel.Children)
                {
                    if ((element as FrameworkElement).Name == sender.Tag.ToString())
                    {
                        splBtn = (SplitButton)element;
                        break;
                    }
                }
                if (splBtn == null)
                {
                    return;
                }

                Border border = (Border)splBtn.Content;
                border.Background = new SolidColorBrush(Color.FromArgb(
                    255,
                    args.NewColor.R,
                    args.NewColor.G,
                    args.NewColor.B
                ));

                WallpaperSendMsg(new LivelyColorPicker() { Name = splBtn.Name, Value = ToHexValue(args.NewColor) });
                livelyPropertyCopyData[splBtn.Name]["value"] = ToHexValue(args.NewColor);
                UpdatePropertyFile();

            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
          
        }

        private void EyeDropBtn_Click(object sender, RoutedEventArgs e)
        {
            if (mouseHook != null)
                return;

            try
            {
                var btn = sender as ToggleButton;
                SplitButton splBtn = null;
                foreach (object element in uiPanel.Children)
                {
                    if ((element as FrameworkElement).Name == btn.Tag.ToString())
                    {
                        splBtn = (SplitButton)element;
                        break;
                    }
                }
                if (splBtn == null)
                {
                    return;
                }
                currEyeDropSplitBtn = splBtn;
                currEyeDropBtn = btn;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }

            currEyeDropBtn.IsChecked = true;
            mouseHook = new LowLevelMouseHook() { GenerateMouseMoveEvents = true, Handling = false };
            mouseHook.Move += MouseHook_Move;
            mouseHook.Down += MouseHook_Down;
            mouseHook.Start();
        }

        private void MouseHook_Move(object sender, MouseEventArgs e)
        {
            lock (_colorPickerTipLock)
                Monitor.PulseAll(_colorPickerTipLock);

            lock (_colorPickerTipLock)
            {
                if (!Monitor.Wait(_colorPickerTipLock, 100))
                {
                    var color = GetColorAt(e.Position.X, e.Position.Y);
                    _ = App.Services.GetRequiredService<MainWindow>().DispatcherQueue.TryEnqueue(() =>
                    {
                        currEyeDropBtn.Background = new SolidColorBrush(Color.FromArgb(
                          255,
                          color.R,
                          color.G,
                          color.B
                        ));
                    });
                }
            }
        }

        private void MouseHook_Down(object sender, H.Hooks.MouseEventArgs e)
        {
            try
            {
                //e.IsHandled = true;
                var color = GetColorAt(e.Position.X, e.Position.Y);
                if (currEyeDropSplitBtn != null && currEyeDropBtn != null)
                {
                    _ = App.Services.GetRequiredService<MainWindow>().DispatcherQueue.TryEnqueue(() =>
                    {
                        currEyeDropBtn.IsChecked = false;
                        //currEyeDropBtn.Background = (SolidColorBrush)Application.Current.Resources["SystemControlForegroundAccentBrush"];
                        Border border = (Border)currEyeDropSplitBtn.Content;
                        border.Background = new SolidColorBrush(Color.FromArgb(
                          255,
                          color.R,
                          color.G,
                          color.B
                        ));

                        WallpaperSendMsg(new LivelyColorPicker() { Name = currEyeDropSplitBtn.Name, Value = ToHexValue(color) });
                        livelyPropertyCopyData[currEyeDropSplitBtn.Name]["value"] = ToHexValue(color);
                        UpdatePropertyFile();
                    });
                }
            }
            finally
            {
                mouseHook.Dispose();
                mouseHook = null;
            }
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
                LoadUI();
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

        private async void WallpaperSendMsg(IpcMessage msg)
        {
            lock (_sendMsgLock)
                Monitor.PulseAll(_sendMsgLock);

            await Task.Run(() =>
            {
                lock (_sendMsgLock)
                {
                    if (!Monitor.Wait(_sendMsgLock, 100))
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
                    }
                }
            }).ConfigureAwait(false);
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
                    var dataFolder = Path.Combine(userSettings.Settings.WallpaperDir, "SaveData", "wpdata");
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

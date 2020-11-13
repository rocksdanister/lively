using livelywpf.Core;
using Microsoft.Toolkit.Wpf.UI.XamlHost;
using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Path = System.IO.Path;

namespace livelywpf.Cef
{
    /// <summary>
    /// Interaction logic for LivelyPropertiesView.xaml
    /// </summary>
    public partial class LivelyPropertiesView : Page
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly string livelyPropertyPath;
        private readonly LibraryModel wallpaperData;
        private readonly LivelyScreen screen;
        private JObject livelyPropertyData;

        //UI
        private readonly Thickness margin = new Thickness(0, 10, 0, 0);
        private readonly double maxWidth = 200;

        public LivelyPropertiesView(LibraryModel data, string livelyPropertyPath, LivelyScreen screen)
        {
            InitializeComponent();
            this.screen = screen;
            this.livelyPropertyPath = livelyPropertyPath; 
            //todo: use LivelyScreen instead, what if wp instance closes.
            wallpaperData = data;
            LoadUI();

        }

        private void LoadUI()
        {
            try
            {
                this.livelyPropertyData = LivelyPropertiesJSON.LoadLivelyProperties(livelyPropertyPath);
                //Path.Combine(Path.GetDirectoryName(wallpaperData.FilePath), "LivelyProperties.json"));
                GenerateUIElements();
            }
            catch (Exception e)
            {
                Logger.Error(e.ToString());
                Task.Run(() => (MessageBox.Show(e.ToString())));
            }
        }

        #region ui generation

        private void GenerateUIElements()
        {
            if (livelyPropertyData.Count == 0)
            {
                //Empty..
                AddUIElement(new TextBlock
                {
                    Text = "El Psy Congroo",
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Margin = margin
                });
                return;
            }

            dynamic obj = null;
            foreach (var item in livelyPropertyData)
            {
                string uiElementType = item.Value["type"].ToString();
                if (uiElementType.Equals("slider", StringComparison.OrdinalIgnoreCase))
                {
                    WindowsXamlHost xamlSlider = new WindowsXamlHost()
                    {
                        Name = item.Key,
                        MaxWidth = maxWidth,
                        MinWidth = maxWidth,
                        InitialTypeName = "Windows.UI.Xaml.Controls.Slider",
                        HorizontalAlignment = HorizontalAlignment.Left,
                        Margin = margin
                    };
                    xamlSlider.ChildChanged += XamlSlider_ChildChanged;
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
                        StrokeThickness = 0.5,
                        MinWidth = maxWidth,
                        MaxWidth = maxWidth,
                        MaxHeight = 15,
                        MinHeight = 15,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        Margin = margin
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

                    WindowsXamlHost xamlCmbBox = new WindowsXamlHost()
                    {
                        Name = item.Key,
                        MaxWidth = maxWidth,
                        MinWidth = maxWidth,
                        InitialTypeName = "Windows.UI.Xaml.Controls.ComboBox",       
                        HorizontalAlignment = HorizontalAlignment.Left,
                        Margin = margin
                    };
                    xamlCmbBox.ChildChanged += XmlCmbBox_ChildChanged;                  
                    obj = xamlCmbBox;
                }
                else if (uiElementType.Equals("folderDropdown", StringComparison.OrdinalIgnoreCase))
                {
                    WindowsXamlHost xamlFolderCmbBox = new WindowsXamlHost()
                    {
                        Name = item.Key,
                        MaxWidth = maxWidth,
                        MinWidth = maxWidth,
                        InitialTypeName = "Windows.UI.Xaml.Controls.ComboBox",
                        HorizontalAlignment = HorizontalAlignment.Left,
                        Margin = margin
                    };
                    xamlFolderCmbBox.ChildChanged += XamlFolderCmbBox_ChildChanged;
                    obj = xamlFolderCmbBox;
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
            }

            //restore button disabled for wp outside lively folder.
            if (!wallpaperData.LivelyInfo.IsAbsolutePath)
            {
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
        }

        private void AddUIElement(dynamic obj)
        {
            uiPanel.Children.Add(obj);
        }

        #endregion //ui generation

        #region slider

        private void XamlSlider_ChildChanged(object sender, EventArgs e)
        {
            try
            {
                var xml = (WindowsXamlHost)sender;
                var slider = (Windows.UI.Xaml.Controls.Slider)xml.Child;

                if (slider != null)
                {
                    foreach (var item in livelyPropertyData)
                    {
                        string uiElementType = item.Value["type"].ToString();
                        if (uiElementType.Equals("slider", StringComparison.OrdinalIgnoreCase))
                        {
                            if (xml.Name.Equals(item.Key, StringComparison.OrdinalIgnoreCase))
                            {
                                slider.Name = item.Key;
                                slider.Minimum = (double)item.Value["min"];
                                slider.Maximum = (double)item.Value["max"];
                                slider.Value = (double)item.Value["value"];
                                if (item.Value["step"] != null)
                                {
                                    if (!String.IsNullOrWhiteSpace(item.Value["step"].ToString()))
                                    {
                                        slider.StepFrequency = (double)item.Value["step"];
                                    }
                                }
                                slider.ValueChanged += Slider_ValueChanged;
                                break;
                            }
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                Logger.Error(ex.ToString());
            }
        }

        private void Slider_ValueChanged(object sender, Windows.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            try
            {
                var item = (Windows.UI.Xaml.Controls.Slider)sender;

                WallpaperSendMsg("lively:customise slider " + item.Name + " " + item.Value);
                livelyPropertyData[item.Name]["value"] = item.Value;
                UpdatePropertyFile();
            }
            catch { }
        }

        #endregion //slider

        #region dropdown

        private void XamlFolderCmbBox_ChildChanged(object sender, EventArgs e)
        {
            try
            {
                var xml = (WindowsXamlHost)sender;
                var cmbBox = (Windows.UI.Xaml.Controls.ComboBox)xml.Child;

                if (cmbBox != null)
                {
                    foreach (var item in livelyPropertyData)
                    {
                        string uiElementType = item.Value["type"].ToString();
                        if (uiElementType.Equals("folderDropdown", StringComparison.OrdinalIgnoreCase))
                        {
                            if (xml.Name.Equals(item.Key, StringComparison.OrdinalIgnoreCase))
                            {
                                cmbBox.Name = item.Key;
                                //filter syntax: "*.jpg|*.png"
                                var files = GetFileNames(Path.Combine(Path.GetDirectoryName(wallpaperData.FilePath), item.Value["folder"].ToString()),
                                                            item.Value["filter"].ToString(),
                                                            SearchOption.TopDirectoryOnly);

                                foreach (var file in files)
                                {
                                    cmbBox.Items.Add(file);
                                }
                                cmbBox.SelectedIndex = Array.FindIndex(files, x => x.Contains(item.Value["value"].ToString())); //returns -1 if not found, none selected.
                                cmbBox.SelectionChanged += CmbBox_SelectionChanged1;
                                break;
                            }
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                Logger.Error(ex.ToString());
            }
        }

        private void CmbBox_SelectionChanged1(object sender, Windows.UI.Xaml.Controls.SelectionChangedEventArgs e)
        {
            try
            {
                var item = (Windows.UI.Xaml.Controls.ComboBox)sender;
                var filePath = Path.Combine(livelyPropertyData[item.Name]["folder"].ToString(), item.SelectedItem.ToString()); //filename is unique.
                WallpaperSendMsg("lively:customise folderDropdown " + item.Name + " " + "\"" + filePath + "\"");
                livelyPropertyData[item.Name]["value"] = item.SelectedItem.ToString();
                UpdatePropertyFile();
            }
            catch { }
        }

        private void XmlCmbBox_ChildChanged(object sender, EventArgs e)
        {
            var xml = (WindowsXamlHost)sender;
            var cmbBox = (Windows.UI.Xaml.Controls.ComboBox)xml.Child;

            if (cmbBox != null)
            {
                foreach (var item in livelyPropertyData)
                {
                    string uiElementType = item.Value["type"].ToString();
                    if (uiElementType.Equals("dropdown", StringComparison.OrdinalIgnoreCase))
                    {
                        if (xml.Name.Equals(item.Key, StringComparison.OrdinalIgnoreCase))
                        {
                            cmbBox.Name = item.Key;
                            foreach (var dropItem in item.Value["items"])
                            {
                                cmbBox.Items.Add(dropItem);
                            }
                            cmbBox.SelectedIndex = (int)item.Value["value"];
                            cmbBox.SelectionChanged += CmbBox_SelectionChanged;
                            break;
                        }
                    }
                }                    
            }
        }

        private void CmbBox_SelectionChanged(object sender, Windows.UI.Xaml.Controls.SelectionChangedEventArgs e)
        {
            try
            {
                var item = (Windows.UI.Xaml.Controls.ComboBox)sender;
                //Form1.chromeBrowser.ExecuteScriptAsync("livelyPropertyListener", item.Name, item.SelectedIndex);
                WallpaperSendMsg("lively:customise dropdown " + item.Name + " " + item.SelectedIndex);
                livelyPropertyData[item.Name]["value"] = item.SelectedIndex;
                UpdatePropertyFile();
            }
            catch { }
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
                //wpf has no native color picker :(
                var colorDialog = new System.Windows.Forms.ColorDialog()
                {
                    AllowFullOpen = true,
                    Color = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(fill.A, fill.R, fill.G, fill.B)).Color
                };

                if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    item.Fill = new SolidColorBrush(Color.FromArgb(colorDialog.Color.A, colorDialog.Color.R, colorDialog.Color.G, colorDialog.Color.B));
                    //Form1.chromeBrowser.ExecuteScriptAsync("livelyPropertyListener", item.Name, ToHexValue(colorDialog.Color));
                    WallpaperSendMsg("lively:customise color " + item.Name + " " + ToHexValue(colorDialog.Color));
                    livelyPropertyData[item.Name]["value"] = ToHexValue(colorDialog.Color);
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

        #endregion //color picker

        #region button

        private void DefaultBtn_Click(object sender, EventArgs e)
        {
            if (RestoreOriginalPropertyFile())
            {
                uiPanel.Children.Clear();
                LoadUI();
                WallpaperSendMsg("lively:customise button lively_default_settings_reload 1");
            }
        }

        private void Button_Click(object sender, EventArgs e)
        {
            try
            {
                var item = (Button)sender;
                //Form1.chromeBrowser.ExecuteScriptAsync("livelyPropertyListener", item.Name, true);
                WallpaperSendMsg("lively:customise button " + item.Name + " " + true);
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
                //Form1.chromeBrowser.ExecuteScriptAsync("livelyPropertyListener", item.Name, item.Checked);
                WallpaperSendMsg("lively:customise checkbox " + item.Name + " " + (item.IsChecked == true));
                Debug.WriteLine("lively:customise " + item.Name + " " + (item.IsChecked == true));
                livelyPropertyData[item.Name]["value"] = item.IsChecked == true;
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
                //Form1.chromeBrowser.ExecuteScriptAsync("livelyPropertyListener", item.Name, item.Text);
                WallpaperSendMsg("lively:customise textbox " + item.Name + " " + "\"" + item.Text + "\"");
                Debug.WriteLine("lively:customise textbox " + item.Name + " " + "\"" + item.Text + "\"");
                livelyPropertyData[item.Name]["value"] = item.Text;
                UpdatePropertyFile();
            }
            catch { }
        }

        #endregion //textbox

        #region helpers

        private void UpdatePropertyFile()
        {
            Cef.LivelyPropertiesJSON.SaveLivelyProperties(livelyPropertyPath, livelyPropertyData);
        }

        private void WallpaperSendMsg(string message)
        {
            SetupDesktop.SendMessageWallpaper(screen, message);
        }

        private void ReloadMenuValues()
        {

        }

        private bool RestoreOriginalPropertyFile()
        {
            bool status = false;
            try
            {
                File.Copy(wallpaperData.LivelyPropertyPath, livelyPropertyPath, true);
                status = true;
            }
            catch(Exception e)
            {
                Logger.Error(e.ToString());
            }
            return status;
        }

        #endregion //helpers

    }
}

using System;
using H.Hooks;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using ColorUwp = Windows.UI.Color;
using ColorMedia = System.Windows.Media.Color;
using CursorForm = System.Windows.Forms.Cursor;
using livelywpf.Helpers.Pinvoke;

namespace livelywpf.Views.Dialogues
{
    /// <summary>
    /// Interaction logic for ColorDialog.xaml
    /// </summary>
    public partial class ColorDialog : Window
    {
        private bool _colorPickerMode;
        private readonly LowLevelMouseHook mouseHook;
        public ColorMedia CurrentColor { get; private set; }

        public ColorDialog(ColorMedia defaultColor)
        {
            InitializeComponent();
            PreviewKeyDown += (s, e) => { if (e.Key == System.Windows.Input.Key.Escape) this.Close(); };
            //CurrentColor = defaultColor;
            Cpicker.SelectedColor = defaultColor;
            mouseHook ??= new LowLevelMouseHook() { GenerateMouseMoveEvents = true, Handling = true };
            mouseHook.Move += MouseHook_Move;
            mouseHook.Down += MouseHook_Down;
        }

        private void Cpicker_ColorChanged(object sender, RoutedEventArgs e)
        {
            CurrentColor = Cpicker.SelectedColor;
        }

        private void Ok_Button_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void Cancel_Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void AccentBtn_Click(object sender, RoutedEventArgs e)
        {
            var color = Helpers.WindowsPersonalize.GetAccentColorUwp();
            Cpicker.SelectedColor = new ColorMedia()
            {
                A = color.A,
                R = color.R,
                G = color.G,
                B = color.B
            };
        }

        private void PickerBtn_Click(object sender, RoutedEventArgs e)
        {
            _colorPickerMode = !_colorPickerMode;
            if (_colorPickerMode)
            {
                ctt.IsOpen = true;
                pickerBtn.IsEnabled = false;
                var mousePos = CursorForm.Position;
                UpdateToolTip(mousePos.X, mousePos.Y);
            }

            if (!mouseHook.IsStarted)
            {
                mouseHook.Start();
            }
        }

        private void MouseHook_Move(object sender, H.Hooks.MouseEventArgs e)
        {
            if (_colorPickerMode)
            {
                UpdateToolTip(e.Position.X, e.Position.Y);
            }
        }

        private void MouseHook_Down(object sender, H.Hooks.MouseEventArgs e)
        {
            if (_colorPickerMode)
            {
                e.IsHandled = true;
                _colorPickerMode = false;
                var color = GetColorAt(e.Position.X, e.Position.Y);
                this.Dispatcher.Invoke(new Action(() => {
                    ctt.IsOpen = false;
                    pickerBtn.IsEnabled = true;
                    if (e.Keys.Values.Contains(Key.MouseLeft))
                    {
                        Cpicker.SelectedColor = new ColorMedia()
                        {
                            R = color.R,
                            G = color.G,
                            B = color.B,
                            A = color.A,
                        };
                    }
                }));
            }
        }

        private void UpdateToolTip(int x, int y)
        {
            var color = GetColorAt(x, y);
            _ = this.Dispatcher.BeginInvoke(new Action(() => {
                var dpi = VisualTreeHelper.GetDpi(ctt).DpiScaleX;
                dpi = dpi != 0f ? dpi : 1.0f;
                ctt.HorizontalOffset = (x + 15) / dpi;
                ctt.VerticalOffset = (y + 15) / dpi;
                cttColor.Fill = new SolidColorBrush(color);
                cttText.Text = $"rgb({color.R}, {color.G}, {color.B})";
            }));
        }

        public static ColorMedia GetColorAt(int x, int y)
        {
            IntPtr desk = NativeMethods.GetDesktopWindow();
            IntPtr dc = NativeMethods.GetWindowDC(desk);
            try
            {
                int a = (int)NativeMethods.GetPixel(dc, x, y);
                return ColorMedia.FromArgb(255, (byte)((a >> 0) & 0xff), (byte)((a >> 8) & 0xff), (byte)((a >> 16) & 0xff));
            }
            finally
            {
                NativeMethods.ReleaseDC(desk, dc);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            mouseHook.Move -= MouseHook_Move;
            mouseHook.Down -= MouseHook_Down;
            mouseHook?.Dispose();
            ctt.IsOpen = false;
        }
    }
}

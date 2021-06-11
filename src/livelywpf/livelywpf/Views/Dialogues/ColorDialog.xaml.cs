using System;
using System.Windows;
using Color = Windows.UI.Color;
using Microsoft.Toolkit.Wpf.UI.XamlHost;

namespace livelywpf.Views
{
    /// <summary>
    /// Interaction logic for ColorDialog.xaml
    /// </summary>
    public partial class ColorDialog : Window
    {
        public Color CColor { get; private set; }

        public ColorDialog(Color defaultColor)
        {
            InitializeComponent();
            CColor = defaultColor;
        }

        private void Cpicker_ChildChanged(object sender, EventArgs e)
        {
            WindowsXamlHost windowsXamlHost = (WindowsXamlHost)sender;
            var picker = (Windows.UI.Xaml.Controls.ColorPicker)windowsXamlHost.Child;
            if (picker != null)
            {
                picker.ColorChanged += CPicker_ColorChanged;
                picker.Color = CColor;
            }
        }

        private void CPicker_ColorChanged(Windows.UI.Xaml.Controls.ColorPicker sender, Windows.UI.Xaml.Controls.ColorChangedEventArgs args)
        {
            CColor = args.NewColor;
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
    }
}

using System;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.UI;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Lively.UI.WinUI.Views.LivelyProperty;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Lively.UI.WinUI.Extensions;
using System.Windows.Forms.VisualStyles;

namespace Lively.UI.WinUI.UserControls
{
    public sealed partial class ColorPickerButton : UserControl
    {
        public ColorPicker ColorPicker => colorPicker;

        // Using #RRGGBB representation, converter is used to remove alpha from the control.
        // Issue: DependencyProperty changed event fires when value is unchanged for (non-primitive) value types like Color.
        // Ref: https://github.com/microsoft/microsoft-ui-xaml/issues/1735
        public string SelectedColor
        {
            get { return (string)GetValue(SelectedColorProperty); }
            set 
            { 
                SetValue(SelectedColorProperty, value);
                ColorChangedCommand?.Execute(CommandParameter);
            }
        }

        public static readonly DependencyProperty SelectedColorProperty =
            DependencyProperty.Register("SelectedColor", typeof(string), typeof(ColorPickerButton), new PropertyMetadata("#FFC0CB"));

        public ICommand ColorChangedCommand
        {
            get { return (ICommand)GetValue(ColorChangedCommandProperty); }
            set { SetValue(ColorChangedCommandProperty, value); }
        }

        public static readonly DependencyProperty ColorChangedCommandProperty =
            DependencyProperty.Register("ColorChangedCommand", typeof(ICommand), typeof(ColorPickerButton), new PropertyMetadata(null));

        public object CommandParameter
        {
            get { return GetValue(CommandParameterProperty); }
            set { SetValue(CommandParameterProperty, value); }
        }

        public static readonly DependencyProperty CommandParameterProperty =
            DependencyProperty.Register("CommandParameter", typeof(object), typeof(ColorPickerButton), new PropertyMetadata(null));

        public RelayCommand OpenEyeDropperCommand { get; }

        public ColorPickerButton()
        {
            this.InitializeComponent();
            OpenEyeDropperCommand = new RelayCommand(OpenEyeDropper);
        }

        private void OpenEyeDropper()
        {
            var window = new EyeDropper();
            window.Activate();
            window.Closed += (_, _) =>
            {
                if (window.SelectedColor is null)
                    return;

                this.DispatcherQueue.TryEnqueue(() =>
                {
                    var color = (Color)window.SelectedColor;
                    // Note: Since x:Load is used, ColorPicker may not be available.
                    SelectedColor = color.ToHex();
                });
            };
        }

        // Workaround: Crashing at times when opening flyout
        // Ref: https://github.com/microsoft/microsoft-ui-xaml/issues/8412
        private void Flyout_Opened(object sender, object e)
        {
            this.DispatcherQueue.TryEnqueue(() =>
            {
                this.FindName("colorPicker");
            });
        }
    }
}

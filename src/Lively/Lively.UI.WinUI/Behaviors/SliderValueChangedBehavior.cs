using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Lively.UI.WinUI.Behaviors
{
    public class SliderValueChangedBehavior
    {
        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.RegisterAttached("Command", typeof(ICommand), typeof(SliderValueChangedBehavior), new PropertyMetadata(null, OnCommandChanged));

        public static readonly DependencyProperty CommandParameterProperty =
            DependencyProperty.RegisterAttached("CommandParameter", typeof(object), typeof(SliderValueChangedBehavior), new PropertyMetadata(null));

        public static ICommand GetCommand(Slider slider)
        {
            return (ICommand)slider.GetValue(CommandProperty);
        }

        public static void SetCommand(Slider slider, ICommand value)
        {
            slider.SetValue(CommandProperty, value);
        }

        public static object GetCommandParameter(Slider slider)
        {
            return slider.GetValue(CommandParameterProperty);
        }

        public static void SetCommandParameter(Slider slider, object value)
        {
            slider.SetValue(CommandParameterProperty, value);
        }

        private static void OnCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Slider slider)
            {
                slider.ValueChanged -= OnSliderValueChanged;
                if (e.NewValue is ICommand command)
                {
                    slider.ValueChanged += OnSliderValueChanged;
                }
            }
        }

        private static void OnSliderValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (sender is Slider slider && GetCommand(slider) != null && GetCommand(slider).CanExecute(GetCommandParameter(slider)))
            {
                GetCommand(slider).Execute(GetCommandParameter(slider));
            }
        }
    }
}

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
    public class ComboBoxSelectionChangedBehavior
    {
        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.RegisterAttached("Command", typeof(ICommand), typeof(ComboBoxSelectionChangedBehavior), new PropertyMetadata(null, OnCommandChanged));

        public static readonly DependencyProperty CommandParameterProperty =
            DependencyProperty.RegisterAttached("CommandParameter", typeof(object), typeof(ComboBoxSelectionChangedBehavior), new PropertyMetadata(null));

        public static ICommand GetCommand(ComboBox comboBox)
        {
            return (ICommand)comboBox.GetValue(CommandProperty);
        }

        public static void SetCommand(ComboBox comboBox, ICommand value)
        {
            comboBox.SetValue(CommandProperty, value);
        }

        public static object GetCommandParameter(ComboBox comboBox)
        {
            return comboBox.GetValue(CommandParameterProperty);
        }

        public static void SetCommandParameter(ComboBox comboBox, object value)
        {
            comboBox.SetValue(CommandParameterProperty, value);
        }

        private static void OnCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ComboBox comboBox)
            {
                comboBox.SelectionChanged -= OnComboBoxSelectionChanged;
                if (e.NewValue is ICommand command)
                {
                    comboBox.SelectionChanged += OnComboBoxSelectionChanged;
                }
            }
        }

        private static void OnComboBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && GetCommand(comboBox) != null && GetCommand(comboBox).CanExecute(GetCommandParameter(comboBox)))
            {
                GetCommand(comboBox).Execute(GetCommandParameter(comboBox));
            }
        }
    }
}

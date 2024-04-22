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
    public class TextBoxTextChangedBehavior
    {
        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.RegisterAttached("Command", typeof(ICommand), typeof(TextBoxTextChangedBehavior), new PropertyMetadata(null, OnCommandChanged));

        public static readonly DependencyProperty CommandParameterProperty =
            DependencyProperty.RegisterAttached("CommandParameter", typeof(object), typeof(TextBoxTextChangedBehavior), new PropertyMetadata(null));

        public static ICommand GetCommand(TextBox textBox)
        {
            return (ICommand)textBox.GetValue(CommandProperty);
        }

        public static void SetCommand(TextBox textBox, ICommand value)
        {
            textBox.SetValue(CommandProperty, value);
        }

        public static object GetCommandParameter(TextBox textBox)
        {
            return textBox.GetValue(CommandParameterProperty);
        }

        public static void SetCommandParameter(TextBox textBox, object value)
        {
            textBox.SetValue(CommandParameterProperty, value);
        }

        private static void OnCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBox textBox)
            {
                textBox.TextChanged -= OnTextBoxTextChanged;
                if (e.NewValue is ICommand command)
                {
                    textBox.TextChanged += OnTextBoxTextChanged;
                }
            }
        }

        private static void OnTextBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox && GetCommand(textBox) != null && GetCommand(textBox).CanExecute(GetCommandParameter(textBox)))
            {
                GetCommand(textBox).Execute(GetCommandParameter(textBox));
            }
        }
    }
}

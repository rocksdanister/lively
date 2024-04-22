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
    public static class GridViewItemClickBehavior
    {
        public static readonly DependencyProperty ItemClickCommandProperty =
            DependencyProperty.RegisterAttached("ItemClickCommand", typeof(ICommand), typeof(GridViewItemClickBehavior), new PropertyMetadata(null, OnItemClickCommandChanged));

        public static ICommand GetItemClickCommand(DependencyObject obj)
        {
            return (ICommand)obj.GetValue(ItemClickCommandProperty);
        }

        public static void SetItemClickCommand(DependencyObject obj, ICommand value)
        {
            obj.SetValue(ItemClickCommandProperty, value);
        }

        private static void OnItemClickCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is GridView gridView)
            {
                gridView.ItemClick += (sender, args) =>
                {
                    ICommand command = GetItemClickCommand(gridView);
                    if (command?.CanExecute(args.ClickedItem) == true)
                    {
                        command.Execute(args.ClickedItem);
                    }
                };
            }
        }
    }
}

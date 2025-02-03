using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using System;

namespace Lively.UI.WinUI.Helpers.Converters
{
    public class BoolToBackButtonVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool isBackEnabled)
            {
                return isBackEnabled ? NavigationViewBackButtonVisible.Visible : NavigationViewBackButtonVisible.Collapsed;
            }
            return NavigationViewBackButtonVisible.Auto;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is NavigationViewBackButtonVisible visibility)
            {
                return visibility == NavigationViewBackButtonVisible.Visible;
            }
            return false;
        }
    }
}

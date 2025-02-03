using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;
using System;

namespace Lively.UI.WinUI.Helpers.Converters
{
    public class UriToBitmapImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value is Uri uri ? new BitmapImage(uri) : null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return (value as BitmapImage)?.UriSource?.ToString();
        }
    }
}

using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using System;
using Windows.UI;
using Lively.UI.WinUI.Extensions;

namespace Lively.UI.WinUI.Helpers.Converters
{
    public class HexStringToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            try
            {
                return (value as string).ToColor();
            }
            catch 
            {
                return "#FFC0CB".ToColor();
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            try
            {
                return ((Color)value).ToHex();
            }
            catch
            {
                return Colors.Pink.ToHex();
            }
        }
    }
}

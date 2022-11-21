using Lively.Common;
using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lively.UI.WinUI.Helpers.Converters
{
    public sealed class WallpaperTypeEnumToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var targetValue = string.Empty;
            try
            {
                var type = (WallpaperType)value;
                targetValue = LocalizationUtil.LocalizeWallpaperCategory(type);
            }
            catch { }
            return targetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}

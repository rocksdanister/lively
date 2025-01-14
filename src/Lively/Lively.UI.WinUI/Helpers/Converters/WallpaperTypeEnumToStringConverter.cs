using Lively.Common.Services;
using Lively.Models.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Data;
using System;

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
                targetValue = App.Services.GetRequiredService<IResourceService>().GetString(type);
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

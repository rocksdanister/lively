using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Lively.UI.WinUI.Helpers.Converters
{
    public class ListViewSelectionModeStringToEnumConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value.ToString().ToLower() switch
            {
                "none" => ListViewSelectionMode.None,
                "single" => ListViewSelectionMode.Single,
                "multiple" => ListViewSelectionMode.Multiple,
                "extended" => ListViewSelectionMode.Extended,
                _ => ListViewSelectionMode.Single,
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}

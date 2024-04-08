using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lively.UI.WinUI.Helpers.Converters
{
    class StringToInfoBarSeverityIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return (value as string) switch
            {
                "Informational" => $"ms-appx:///Assets/Icons/icons8-info.svg",
                "Success" => $"ms-appx:///Assets/Icons/icons8-ok.svg",
                "Warning" => $"ms-appx:///Assets/Icons/icons8-warn.svg",
                "Error" => $"ms-appx:///Assets/Icons/icons8-error.svg",
                _ => $"ms-appx:///Assets/Icons/icons8-error.svg",
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lively.UI.WinUI.Helpers.Converters
{
    public class StringToInfoBarSeverityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return (value as string) switch
            {
                "Informational" => InfoBarSeverity.Informational,
                "Success" => InfoBarSeverity.Success,
                "Warning" => InfoBarSeverity.Warning,
                "Error" => InfoBarSeverity.Error,
                _ => InfoBarSeverity.Error,
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}

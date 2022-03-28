using Lively.Common;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lively.Helpers.Theme
{
    public static class ThemeUtil
    {
        public static AppTheme GetWindowsTheme()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                var registryValueObject = key?.GetValue("AppsUseLightTheme");
                if (registryValueObject == null)
                {
                    return AppTheme.Light;
                }
                var registryValue = (int)registryValueObject;
                return registryValue > 0 ? AppTheme.Light : AppTheme.Dark;
            }
            catch
            {
                return AppTheme.Dark;
            }
        }
    }
}

using Lively.Models.Enums;
using Microsoft.Win32;

namespace Lively.Helpers
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

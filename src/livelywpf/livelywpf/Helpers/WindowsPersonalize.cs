using ImageMagick;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.ViewManagement;

namespace livelywpf.Helpers
{
    class WindowsPersonalize
    {
        public static Color GetAccentColor()
        {
            var uiSettings = new UISettings();
            var accentColor = uiSettings.GetColorValue(UIColorType.Accent);
            return Color.FromArgb(accentColor.A, accentColor.R, accentColor.G, accentColor.B);
        }

        public static void SetAccentColor(Color color)
        {
            throw new NotImplementedException();
        }

        public static string GetLockScreenWallpaper()
        {
            throw new NotImplementedException();
        }

        public static async Task SetLockScreenWallpaper(string imgPath)
        {
            var file = await Windows.Storage.StorageFile.GetFileFromPathAsync(imgPath);
            await Windows.System.UserProfile.LockScreen.SetImageFileAsync(file);
        }


        public static string GetDesktopWallpaper()
        {
            throw new NotImplementedException();
        }

        //todo: look into multiple monitor support, ref: https://stackoverflow.com/questions/1540337/how-to-set-multiple-desktop-backgrounds-dual-monitor
        public static async Task<bool> SetDesktopWallpaper(string imgPath)
        {
            bool result = false;
            if (Windows.System.UserProfile.UserProfilePersonalizationSettings.IsSupported())
            {
                var file = await Windows.Storage.StorageFile.GetFileFromPathAsync(imgPath);
                result = await Windows.System.UserProfile.UserProfilePersonalizationSettings.Current.TrySetWallpaperImageAsync(file);
            }
            return result;
        }

        public static void SetSystemTheme(SystemTheme theme, SystemThemeType type)
        {
            throw new NotImplementedException();
        }

        public static SystemTheme GetSystemTheme(SystemThemeType type)
        {
            throw new NotImplementedException();
        }

        #region helpers

        /// <summary>
        /// Quickly computes the average color of image file.
        /// </summary>
        /// <param name="imgPath">Image file path.</param>
        /// <returns></returns>
        public static Color GetAverageColor(string imgPath)
        {
            //avg of colors by resizing to 1x1..
            using var image = new MagickImage(imgPath);
            //same as resize with box filter, Sample(1,1) was unreliable although faster..
            image.Scale(1, 1);

            //take the new pixel..
            using var pixels = image.GetPixels();
            var color = pixels.GetPixel(0, 0).ToColor();

            //ImageMagick color range is 0 - 65535.
            return Color.FromArgb(255 * color.R / 65535, 255 * color.G / 65535, 255 * color.B / 65535);
        }

        public enum SystemTheme
        {
            Light,
            Dark
        }

        public enum SystemThemeType
        {
            app,
            system
        }

        #endregion //helpers
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace livelywpf.Helpers.MVVM
{
    class ImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                string fileName = value as string;
                if (fileName != null)
                {
                    BitmapImage bi = new BitmapImage();
                    bi.BeginInit();
                    //downscale
                    bi.DecodePixelWidth = 200;
                    //bi.DecodePixelHeight = 100;
                    bi.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                    bi.CacheOption = BitmapCacheOption.OnLoad; //allow deletion of file on disk.
                    bi.UriSource = new Uri(fileName);
                    bi.EndInit();
                    bi.Freeze();
                    return bi;
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;

namespace Lively.UI.WinUI.Extensions
{
    public static class ColorExtensions
    {
        public static Color ToColor(this string hexaColor)
        {
            // Remove alpha channel if exists
            if (hexaColor.Length == 9 && hexaColor.StartsWith('#'))
                hexaColor = hexaColor.Substring(3);

            return Color.FromArgb(
                    255,
                    System.Convert.ToByte(hexaColor.Substring(1, 2), 16),
                    System.Convert.ToByte(hexaColor.Substring(3, 2), 16),
                    System.Convert.ToByte(hexaColor.Substring(5, 2), 16)
                );
        }

        public static string ToHex(this Color color)
        {
            return "#" + color.R.ToString("X2") +
                         color.G.ToString("X2") +
                         color.B.ToString("X2");
        }
    }
}

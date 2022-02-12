using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace Lively.UI.WinUI.UserControls
{
    public class AspectContentControl : ContentControl
    {
        protected override Size MeasureOverride(Size availableSize)
        {
            return new Size(availableSize.Width, availableSize.Width * 0.56); //272/153
        }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;


namespace livelywpf
{
    /// <summary>
    /// Opens wallpaper in a new window for preview, wp closes when window close.
    /// </summary>
    public partial class QuickPreview : Window
    {
        public QuickPreview(SaveData.LivelyInfo info)
        {
            InitializeComponent();
        }
    }
}

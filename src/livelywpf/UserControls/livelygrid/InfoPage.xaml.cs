using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace livelygrid
{
    public class LocalizeTextInfoPage
    {
        public string Type { get; set; }
        public string Author { get; set; }
        public string Website { get; set; }
    }

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class InfoPage : Page
    {
        public LocalizeTextInfoPage UIText { get; set; }
        public InfoPage()
        {
            this.InitializeComponent();
        }
    }
}

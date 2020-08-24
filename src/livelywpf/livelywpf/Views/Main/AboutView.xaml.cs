using ModernWpf.Controls;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Documents;
using Page = System.Windows.Controls.Page;

namespace livelywpf.Views
{
    /// <summary>
    /// Interaction logic for AboutView.xaml
    /// </summary>
    public partial class AboutView : Page
    {
        public AboutView()
        {
            InitializeComponent();
            appVersionText.Text = "v" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            try
            {
                //attribution document.
                TextRange textRange = new TextRange(licenseDocument.ContentStart, licenseDocument.ContentEnd);
                using (FileStream fileStream = File.Open(Path.Combine(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Docs", "license.rtf")), FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    textRange.Load(fileStream, System.Windows.DataFormats.Rtf);
                }
                licenseFlowDocumentViewer.Document = licenseDocument;
            }
            catch { }
        }

        private async void licenseDocument_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            e.Handled = true;
            var result = await ShowNavigateDialogue(this, e.Uri);
            if(result == ContentDialogResult.Primary)
            {
                try
                {
                    var ps = new ProcessStartInfo(e.Uri.AbsoluteUri)
                    {
                        UseShellExecute = true,
                        Verb = "open"
                    };
                    Process.Start(ps);
                }
                catch { }
            }
        }

        private async Task<ContentDialogResult> ShowNavigateDialogue(object sender, Uri arg)
        {
            ContentDialog confirmDialog = new ContentDialog
            {
                Title = "Do you wish to navigate to external website?",
                Content = arg.ToString(),
                PrimaryButtonText = "Yes",
                SecondaryButtonText = "No",
            };
            return await confirmDialog.ShowAsync();
        }
    }
}

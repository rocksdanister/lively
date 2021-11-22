using ModernWpf.Controls;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;

namespace livelywpf.Views.Dialogues
{
    /// <summary>
    /// Interaction logic for DocView.xaml
    /// </summary>
    public partial class DocView : Window
    {
        public DocView(string docPath)
        {
            InitializeComponent();
            LoadDocument(docPath);
        }

        private void LoadDocument(string filePath)
        {
            try
            {
                //attribution document.
                TextRange textRange = new TextRange(flowDocument.ContentStart, flowDocument.ContentEnd);
                using (FileStream fileStream =
                    File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    textRange.Load(fileStream, System.Windows.DataFormats.Rtf);
                }
                flowDocumentViewer.Document = flowDocument;
            }
            catch { }
        }

        private async void FlowDocument_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            e.Handled = true;
            var result = await ShowNavigateDialogue(this, e.Uri);
            if (result == ContentDialogResult.Primary)
            {
                Helpers.LinkHandler.OpenBrowser(e.Uri);
            }
        }

        private async Task<ContentDialogResult> ShowNavigateDialogue(object sender, Uri arg)
        {
            ContentDialog confirmDialog = new ContentDialog
            {
                Title = "Do you wish to navigate to external website?",
                Content = arg.ToString(),
                PrimaryButtonText = Properties.Resources.TextYes,
                SecondaryButtonText = Properties.Resources.TextNo,
                DefaultButton = ContentDialogButton.Primary,
            };
            return await confirmDialog.ShowAsync();
        }
    }
}

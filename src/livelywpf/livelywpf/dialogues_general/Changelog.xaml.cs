using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.IO;
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

namespace livelywpf.Dialogues
{
    /// <summary>
    /// Interaction logic for Changelog.xaml
    /// </summary>
    public partial class Changelog : MetroWindow
    {

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        public Changelog()
        {
            InitializeComponent();

            this.Title ="Changelog v" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            //changelog document.
            TextRange textRange = new TextRange(flowDocument.ContentStart, flowDocument.ContentEnd);
            if (File.Exists(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory + "\\docs\\changelog.rtf")))
            {
                try
                {
                    using (FileStream fileStream = File.Open(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory + "\\docs\\changelog.rtf"), FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        textRange.Load(fileStream, System.Windows.DataFormats.Rtf);
                    }
                    flowDocumentViewer.Document = flowDocument;
                }
                catch
                {
                    Logger.Error("Failed to load changelog file");
                }
            }
        }

        private  void Hyperlink_RequestNavigate_Warning(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            if (MessageBox.Show(Properties.Resources.msgLoadExternalLink + "\n" + e.Uri.ToString(), Properties.Resources.msgLoadExternalLinkTitle, MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                System.Diagnostics.Process.Start(e.Uri.AbsoluteUri);
            }
            else
            {
                return;
            }
        }
    }
}

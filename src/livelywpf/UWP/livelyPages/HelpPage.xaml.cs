using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace livelyPages
{
    public sealed partial class HelpPage : UserControl
    {
        public LocalizeText UIText { get; set; }

        public HelpPage()
        {
            this.InitializeComponent();
        }

        public class LocalizeText
        {
            public string TitleWebsite { get; set; }
            public string TitleDocumentation { get; set; }
            public string TitleCommunity { get; set; }
            public string TitleSourceCode { get; set; }
            public string TitleReportBug { get; set; }
            public string TitleSupport { get; set; }
            public string TitleStoreReview { get; set; }
            public string DescWebsite { get; set; }
            public string DescDocumentation { get; set; }
            public string DescCommunity { get; set; }
            public string DescSourceCode { get; set; }
            public string DescReportBug { get; set; }
            public string DescSupport { get; set; }
            public string DescStoreReview { get; set; }
        }
    }
}

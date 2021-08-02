using livelywpf.Helpers;
using ModernWpf.Controls;
using System;
using System.Diagnostics;
using System.Drawing.Text;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Threading;
using Page = System.Windows.Controls.Page;

namespace livelywpf.Views
{
    /// <summary>
    /// Interaction logic for AboutView.xaml
    /// </summary>
    public partial class AboutView : Page
    {
        private bool updateAvailable = false;
        public AboutView()
        {
            InitializeComponent();
            //app info
            appVersionText.Text = "v" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString() + 
                (Program.IsTestBuild ? "b": (Program.IsMSIX ? Properties.Resources.TitleStore : string.Empty));
            //update info
            UpdateMenu(AppUpdaterService.Instance.Status, AppUpdaterService.Instance.LastCheckTime, AppUpdaterService.Instance.LastCheckVersion);
            AppUpdaterService.Instance.UpdateChecked += AppUpdateChecked;
        }

        private void AppUpdateChecked(object sender, AppUpdaterEventArgs e)
        {
            _ = Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new ThreadStart(delegate
            {
                UpdateMenu(e.UpdateStatus, e.UpdateDate, e.UpdateVersion);
            }));
        }

        private void UpdateMenu(AppUpdateStatus status, DateTime date, Version version)
        {
            switch (status)
            {
                case AppUpdateStatus.uptodate:
                    txtBoxUpdateStatus.Text = Properties.Resources.TextUpdateUptodate;
                    break;
                case AppUpdateStatus.available:
                    updateAvailable = true;
                    txtBoxUpdateStatus.Text = $"{Properties.Resources.DescriptionUpdateAvailable} (v{version})";
                    break;
                case AppUpdateStatus.invalid:
                    txtBoxUpdateStatus.Text = "This version of software has unique tag >_<";
                    break;
                case AppUpdateStatus.notchecked:
                    txtBoxUpdateStatus.Text = Properties.Resources.TextUpdateChecking;
                    break;
                case AppUpdateStatus.error:
                    txtBoxUpdateStatus.Text = Properties.Resources.TextupdateCheckFail;
                    break;
            }
            txtboxUpdateDate.Text = $"Last checked: {date}";
            btnUpdate.Content = updateAvailable ? Properties.Resources.TextInstall : Properties.Resources.TextUpdateCheck;
            btnUpdate.IsEnabled = true;
        }

        private void btnUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (updateAvailable)
            {
                Program.AppUpdateDialog(AppUpdaterService.Instance.GetUri(), AppUpdaterService.Instance.GetChangelog());
            }
            else
            {
                btnUpdate.IsEnabled = false;
                _ = AppUpdaterService.Instance.CheckUpdate(0);
            }
        }

        private void btnLicense_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ShowDocDialog(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Docs", "license.rtf"));
        }

        private void btnAttribution_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ShowDocDialog(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Docs", "attribution.rtf"));
        }

        private void btnPrivacyPolicy_Click(object sender, RoutedEventArgs e)
        {
            Helpers.LinkHandler.OpenBrowser("https://github.com/rocksdanister/lively/blob/dev-v1.0-fluent-netcore/PRIVACY.md");
        }

        private void ShowDocDialog(string docPath)
        {
            var item = new DocView(docPath)
            {
                Title = Properties.Resources.TitleDocumentation,
            };
            if (App.AppWindow.IsVisible)
            {
                item.Owner = App.AppWindow;
                item.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
                item.Width = App.AppWindow.Width / 1.2;
                item.Height = App.AppWindow.Height / 1.2;
            }
            else
            {
                item.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
            }
            item.ShowDialog();
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            e.Handled = true;
            Helpers.LinkHandler.OpenBrowser(e.Uri);
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            AppUpdaterService.Instance.UpdateChecked -= AppUpdateChecked;
        }
    }
}

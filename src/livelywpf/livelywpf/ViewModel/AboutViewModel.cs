using livelywpf.Helpers;
using livelywpf.Helpers.MVVM;
using livelywpf.Helpers.Updater;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace livelywpf
{
    public class AboutViewModel : ObservableObject
    {
        private bool updateAvailable = false;

        public AboutViewModel()
        {
            MenuUpdate(AppUpdaterService.Instance.Status, AppUpdaterService.Instance.LastCheckTime, AppUpdaterService.Instance.LastCheckVersion);
            AppUpdaterService.Instance.UpdateChecked += AppUpdateChecked;
        }

        public string AppVersionText => "v" + Assembly.GetExecutingAssembly().GetName().Version.ToString() +
                (Program.IsTestBuild ? "b" : (Program.IsMSIX ? " " + Properties.Resources.TitleStore : string.Empty));

        private string _updateStatusText;
        public string UpdateStatusText
        {
            get { return _updateStatusText; }
            set
            {
                _updateStatusText = value;
                OnPropertyChanged();
            }
        }

        private string _updateDateText;
        public string UpdateDateText
        {
            get { return _updateDateText; }
            set
            {
                _updateDateText = value;
                OnPropertyChanged();
            }
        }

        private string _updateCommandText;
        public string UpdateCommandText
        {
            get { return _updateCommandText; }
            set
            {
                _updateCommandText = value;
                OnPropertyChanged();
            }
        }

        private bool canUpdateAppCommand = true;
        private RelayCommand _updateAppCommand;
        public RelayCommand UpdateAppCommand
        {
            get
            {
                if (_updateAppCommand == null)
                {
                    _updateAppCommand = new RelayCommand(
                        param => _ = CheckUpdate(),
                        param => canUpdateAppCommand);
                }
                return _updateAppCommand;
            }
        }

        private RelayCommand _licenseDocCommand;
        public RelayCommand LicenseDocCommand
        {
            get
            {
                if (_licenseDocCommand == null)
                {
                    _licenseDocCommand = new RelayCommand(
                        param => ShowRtfDocDialog(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Docs", "license.rtf")));
                }
                return _licenseDocCommand;
            }
        }

        private RelayCommand _attribDocCommand;
        public RelayCommand AttribDocCommand
        {
            get
            {
                if (_attribDocCommand == null)
                {
                    _attribDocCommand = new RelayCommand(
                        param => ShowRtfDocDialog(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Docs", "attribution.rtf")));
                }
                return _attribDocCommand;
            }
        }

        private RelayCommand _privacyDocCommand;
        public RelayCommand PrivacyDocCommand
        {
            get
            {
                if (_privacyDocCommand == null)
                {
                    _privacyDocCommand = new RelayCommand(
                        param => LinkHandler.OpenBrowser("https://github.com/rocksdanister/lively/blob/dev-v1.0-fluent-netcore/PRIVACY.md"));
                }
                return _privacyDocCommand;
            }
        }

        private async Task CheckUpdate()
        {
            if (updateAvailable)
            {
                Program.AppUpdateDialog(AppUpdaterService.Instance.LastCheckUri, AppUpdaterService.Instance.LastCheckChangelog);
            }
            else
            {
                try
                {
                    canUpdateAppCommand = false;
                    _updateAppCommand.RaiseCanExecuteChanged();
                    _ = await AppUpdaterService.Instance.CheckUpdate(0);
                    MenuUpdate(AppUpdaterService.Instance.Status, AppUpdaterService.Instance.LastCheckTime, AppUpdaterService.Instance.LastCheckVersion);
                }
                finally
                {
                    canUpdateAppCommand = true;
                    _updateAppCommand.RaiseCanExecuteChanged();
                }
            }
        }

        private void AppUpdateChecked(object sender, AppUpdaterEventArgs e)
        {
            _ = Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new ThreadStart(delegate
            {
                MenuUpdate(e.UpdateStatus, e.UpdateDate, e.UpdateVersion);
            }));
        }

        private void MenuUpdate(AppUpdateStatus status, DateTime date, Version version)
        {
            switch (status)
            {
                case AppUpdateStatus.uptodate:
                    updateAvailable = false;
                    UpdateStatusText = Properties.Resources.TextUpdateUptodate;
                    break;
                case AppUpdateStatus.available:
                    updateAvailable = true;
                    UpdateStatusText = $"{Properties.Resources.DescriptionUpdateAvailable} (v{version})";
                    break;
                case AppUpdateStatus.invalid:
                    updateAvailable = false;
                    UpdateStatusText = "This software has unique version tag >_<";
                    break;
                case AppUpdateStatus.notchecked:
                    updateAvailable = false;
                    UpdateStatusText = Properties.Resources.TextUpdateChecking;
                    break;
                case AppUpdateStatus.error:
                    updateAvailable = false;
                    UpdateStatusText = Properties.Resources.TextupdateCheckFail;
                    break;
            }
            UpdateDateText = date == DateTime.MinValue ? $"{Properties.Resources.TextLastChecked}: ---" : $"{Properties.Resources.TextLastChecked}: {date}";
            UpdateCommandText = updateAvailable ? Properties.Resources.TextDownload : Properties.Resources.TextUpdateCheck;
        }

        private void ShowRtfDocDialog(string docPath)
        {
            var item = new Views.DocView(docPath)
            {
                Title = Properties.Resources.TitleDocumentation,
            };
            if (App.AppWindow.IsVisible)
            {
                item.Owner = App.AppWindow;
                item.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                item.Width = App.AppWindow.Width / 1.2;
                item.Height = App.AppWindow.Height / 1.2;
            }
            else
            {
                item.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }
            item.ShowDialog();
        }

        public void OnViewClosing(object sender, RoutedEventArgs e)
        {
            AppUpdaterService.Instance.UpdateChecked -= AppUpdateChecked;
        }
    }
}

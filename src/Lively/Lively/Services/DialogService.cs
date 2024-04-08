using ABI.System;
using Lively.Common.Services.Update;
using Lively.ViewModels;
using Lively.Views;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Lively.Services
{
    public class DialogService : IDialogService
    {
        private readonly IAppUpdaterService appUpdaterService;

        public DialogService(IAppUpdaterService appUpdaterService)
        {
            this.appUpdaterService = appUpdaterService;
        }

        public void ShowErrorDialog(string title, string message)
        {
            // Since there is no main interface for Core, we are using MessageBox temporarily.
            // TODO: Create a custom ErrorWindow in the future.
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}

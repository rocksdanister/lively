using Lively.UI.Wpf.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace Lively.UI.Wpf.Views.Pages
{
    /// <summary>
    /// Interaction logic for LibraryView.xaml
    /// </summary>
    public partial class LibraryView : Page
    {
        private readonly LibraryViewModel libraryVm;

        public LibraryView()
        {
            libraryVm = App.Services.GetRequiredService<LibraryViewModel>();

            InitializeComponent();
            this.DataContext = libraryVm;
        }
    }
}

using Files.Enums;
using Files.Helpers;
using Files.Interacts;
using Files.ViewModels;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Uwp;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Files.UserControls
{
    public sealed partial class StatusCenter : UserControl
    {
        public StatusCenterViewModel StatusCenterViewModel { get; set; }
        public StatusCenter()
        {
            this.InitializeComponent();
        }

        // Dismiss banner button event handler
        private void DismissBanner(object sender, RoutedEventArgs e)
        {
            StatusBanner itemToDismiss = (sender as Button).DataContext as StatusBanner;
            StatusCenterViewModel.CloseBanner(itemToDismiss);
        }

        // Primary action button click
        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {
            StatusBanner itemToDismiss = (sender as Button).DataContext as StatusBanner;
            await Task.Run(itemToDismiss.PrimaryButtonClick);
            StatusCenterViewModel.CloseBanner(itemToDismiss);
        }
    }
}
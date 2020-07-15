using Files.Dialogs;
using Files.Filesystem;
using Files.Helpers;
using Files.Interacts;
using Files.UserControls;
using Files.View_Models;
using Files.Views;
using Files.Views.Pages;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Files
{
    public sealed partial class YourHome : Page
    {
        public SettingsViewModel AppSettings => App.AppSettings;

        public YourHome()
        {
            InitializeComponent();
            
        }

        protected override void OnNavigatedTo(NavigationEventArgs eventArgs)
        {
            base.OnNavigatedTo(eventArgs);
            App.CurrentInstance.InstanceViewModel.IsPageTypeNotHome = false;
            App.CurrentInstance.InstanceViewModel.IsPageTypeMtpDevice = false;
            App.CurrentInstance.InstanceViewModel.IsPageTypeRecycleBin = false;
            App.CurrentInstance.InstanceViewModel.IsPageTypeCloudDrive = false;
            var parameters = eventArgs.Parameter.ToString();
            App.CurrentInstance.MultitaskingControl?.SetSelectedTabInfo(parameters, null);
            App.CurrentInstance.MultitaskingControl?.SelectionChanged();
            App.CurrentInstance.NavigationToolbar.CanRefresh = false;
            App.CurrentInstance.NavigationToolbar.CanGoBack = App.CurrentInstance.ContentFrame.CanGoBack;
            App.CurrentInstance.NavigationToolbar.CanGoForward = App.CurrentInstance.ContentFrame.CanGoForward;
            App.CurrentInstance.NavigationToolbar.CanNavigateToParent = false;

            // Clear the path UI and replace with Favorites
            App.CurrentInstance.NavigationToolbar.PathComponents.Clear();
            string componentLabel = parameters;
            string tag = parameters;
            PathBoxItem item = new PathBoxItem()
            {
                Title = componentLabel,
                Path = tag,
            };
            App.CurrentInstance.NavigationToolbar.PathComponents.Add(item);
        }
    }
}
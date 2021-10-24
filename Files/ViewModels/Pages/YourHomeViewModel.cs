﻿using Files.EventArguments.Bundles;
using Files.Helpers;
using Files.ViewModels.Widgets;
using Files.ViewModels.Widgets.Bundles;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.Windows.Input;
using Windows.UI.Xaml;

namespace Files.ViewModels.Pages
{
    public class YourHomeViewModel : ObservableObject, IDisposable
    {
        private BundlesViewModel bundlesViewModel;

        private readonly WidgetsListControlViewModel widgetsViewModel;

        private IShellPage associatedInstance;

        public event EventHandler<RoutedEventArgs> YourHomeLoadedInvoked;

        public ICommand YourHomeLoadedCommand { get; private set; }

        public ICommand LoadBundlesCommand { get; private set; }

        public YourHomeViewModel(WidgetsListControlViewModel widgetsViewModel, IShellPage associatedInstance)
        {
            this.widgetsViewModel = widgetsViewModel;
            this.associatedInstance = associatedInstance;

            // Create commands
            YourHomeLoadedCommand = new RelayCommand<RoutedEventArgs>(YourHomeLoaded);
            LoadBundlesCommand = new RelayCommand<BundlesViewModel>(LoadBundles);
        }

        public void ChangeAppInstance(IShellPage associatedInstance)
        {
            this.associatedInstance = associatedInstance;
        }

        private void YourHomeLoaded(RoutedEventArgs e)
        {
            YourHomeLoadedInvoked?.Invoke(this, e);
        }

        private async void LoadBundles(BundlesViewModel viewModel)
        {
            bundlesViewModel = viewModel;

            bundlesViewModel.OpenPathEvent -= BundlesViewModel_OpenPathEvent;
            bundlesViewModel.OpenPathInNewPaneEvent -= BundlesViewModel_OpenPathInNewPaneEvent;
            bundlesViewModel.OpenPathEvent += BundlesViewModel_OpenPathEvent;
            bundlesViewModel.OpenPathInNewPaneEvent += BundlesViewModel_OpenPathInNewPaneEvent;

            await bundlesViewModel.Initialize();
        }

        private void BundlesViewModel_OpenPathInNewPaneEvent(object sender, string e)
        {
            associatedInstance.PaneHolder.OpenPathInNewPane(e);
        }

        private async void BundlesViewModel_OpenPathEvent(object sender, BundlesOpenPathEventArgs e)
        {
            await NavigationHelpers.OpenPath(e.path, associatedInstance, e.itemType, e.openSilent, e.openViaApplicationPicker, e.selectItems);
        }

        #region IDisposable

        public void Dispose()
        {
            if (bundlesViewModel != null)
            {
                bundlesViewModel.OpenPathEvent -= BundlesViewModel_OpenPathEvent;
                bundlesViewModel.OpenPathInNewPaneEvent -= BundlesViewModel_OpenPathInNewPaneEvent;
            }

            widgetsViewModel?.Dispose();
        }

        #endregion IDisposable
    }
}
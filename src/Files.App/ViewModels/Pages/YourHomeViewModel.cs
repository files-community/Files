using Files.App.EventArguments.Bundles;
using Files.App.Helpers;
using Files.App.ViewModels.Widgets;
using Files.App.ViewModels.Widgets.Bundles;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Windows.Input;
using Microsoft.UI.Xaml;
using System.Threading.Tasks;
using System.Collections.Generic;
using Files.Shared.Extensions;
using System.Text.Json;

namespace Files.App.ViewModels.Pages
{
    public class YourHomeViewModel : ObservableObject, IDisposable
    {
        private BundlesViewModel bundlesViewModel;

        private readonly WidgetsListControlViewModel widgetsViewModel;

        private IShellPage associatedInstance;

        private readonly JsonElement defaultJson = JsonSerializer.SerializeToElement("{}");

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
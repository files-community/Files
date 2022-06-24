using Files.Uwp.EventArguments.Bundles;
using Files.Uwp.Helpers;
using Files.Uwp.ViewModels.Widgets;
using Files.Uwp.ViewModels.Widgets.Bundles;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Windows.Input;
using Windows.UI.Xaml;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;
using Files.Shared;
using Files.Shared.Extensions;

namespace Files.Uwp.ViewModels.Pages
{
    public class YourHomeViewModel : ObservableObject, IDisposable
    {
        private BundlesViewModel bundlesViewModel;

        private readonly WidgetsListControlViewModel widgetsViewModel;

        private IShellPage associatedInstance;

        public event EventHandler<RoutedEventArgs> YourHomeLoadedInvoked;

        public ICommand YourHomeLoadedCommand { get; private set; }

        public ICommand LoadBundlesCommand { get; private set; }

        private NamedPipeAsAppServiceConnection connection;

        private NamedPipeAsAppServiceConnection Connection
        {
            get => connection;
            set
            {
                if (connection != null)
                {
                    connection.RequestReceived -= Connection_RequestReceived;
                }
                connection = value;
                if (connection != null)
                {
                    connection.RequestReceived += Connection_RequestReceived;
                }
            }
        }

        public YourHomeViewModel(WidgetsListControlViewModel widgetsViewModel, IShellPage associatedInstance)
        {
            this.widgetsViewModel = widgetsViewModel;
            this.associatedInstance = associatedInstance;

            // Create commands
            YourHomeLoadedCommand = new RelayCommand<RoutedEventArgs>(YourHomeLoaded);
            LoadBundlesCommand = new RelayCommand<BundlesViewModel>(LoadBundles);

            _ = InitializeConnectionAsync(); // fire and forget
            AppServiceConnectionHelper.ConnectionChanged += AppServiceConnectionHelper_ConnectionChanged;
        }

        private async Task InitializeConnectionAsync()
        {
            Connection ??= await AppServiceConnectionHelper.Instance;
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

        private async void AppServiceConnectionHelper_ConnectionChanged(object sender, Task<NamedPipeAsAppServiceConnection> e)
        {
            Connection = await e;
        }

        private async void Connection_RequestReceived(object sender, Dictionary<string, object> message)
        {
            if (message.ContainsKey("RecentItems"))
            {
                var changeType = message.Get("ChangeType", "");
                await App.RecentItemsManager.HandleWin32RecentItemsEvent(changeType);
            }
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

            if (connection != null)
            {
                connection.RequestReceived -= Connection_RequestReceived;
            }

            AppServiceConnectionHelper.ConnectionChanged -= AppServiceConnectionHelper_ConnectionChanged;
        }

        #endregion IDisposable
    }
}
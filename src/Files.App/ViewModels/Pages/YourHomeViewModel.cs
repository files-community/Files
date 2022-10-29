using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Files.App.EventArguments.Bundles;
using Files.App.Helpers;
using Files.App.ViewModels.Widgets;
using Files.App.ViewModels.Widgets.Bundles;
using Files.Shared.Extensions;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;

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

        private NamedPipeAsAppServiceConnection connection;

        private NamedPipeAsAppServiceConnection Connection
        {
            get => connection;
            set
            {
                if (connection is not null)
                {
                    connection.RequestReceived -= Connection_RequestReceived;
                }
                connection = value;
                if (connection is not null)
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

        private async void Connection_RequestReceived(object sender, Dictionary<string, JsonElement> message)
        {
            if (message.ContainsKey("RecentItems"))
            {
                var changeType = message.Get("ChangeType", defaultJson).GetString();
                await App.RecentItemsManager.HandleWin32RecentItemsEvent(changeType);
            }
        }

        #region IDisposable

        public void Dispose()
        {
            if (bundlesViewModel is not null)
            {
                bundlesViewModel.OpenPathEvent -= BundlesViewModel_OpenPathEvent;
                bundlesViewModel.OpenPathInNewPaneEvent -= BundlesViewModel_OpenPathInNewPaneEvent;
            }

            widgetsViewModel?.Dispose();

            if (connection is not null)
            {
                connection.RequestReceived -= Connection_RequestReceived;
            }

            AppServiceConnectionHelper.ConnectionChanged -= AppServiceConnectionHelper_ConnectionChanged;
        }

        #endregion IDisposable
    }
}
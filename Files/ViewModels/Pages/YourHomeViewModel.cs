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
        private readonly WidgetsListControlViewModel widgetsViewModel;

        private readonly IShellPage associatedInstance;

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

        private void YourHomeLoaded(RoutedEventArgs e)
        {
            YourHomeLoadedInvoked?.Invoke(this, e);
        }

        private async void LoadBundles(BundlesViewModel viewModel)
        {
            viewModel.Initialize(associatedInstance);
            await viewModel.Load();
        }

        #region IDisposable

        public void Dispose()
        {
            widgetsViewModel?.Dispose();
        }

        #endregion
    }
}

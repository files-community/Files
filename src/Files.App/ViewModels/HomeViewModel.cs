using Files.App.EventArguments.Bundles;
using Files.App.ViewModels.Widgets;
using Files.App.ViewModels.Widgets.Bundles;
using Microsoft.UI.Xaml;
using System.Windows.Input;

namespace Files.App.ViewModels
{
	public class HomeViewModel : ObservableObject, IDisposable
	{
		private BundlesViewModel bundlesViewModel;

		private readonly WidgetsListControlViewModel widgetsViewModel;

		private IShellPage associatedInstance;

		public event EventHandler<RoutedEventArgs> YourHomeLoadedInvoked;

		public ICommand YourHomeLoadedCommand { get; private set; }

		public ICommand LoadBundlesCommand { get; private set; }

		public HomeViewModel(WidgetsListControlViewModel widgetsViewModel, IShellPage associatedInstance)
		{
			this.widgetsViewModel = widgetsViewModel;
			this.associatedInstance = associatedInstance;

			// Create commands
			YourHomeLoadedCommand = new RelayCommand<RoutedEventArgs>(YourHomeLoaded);
			LoadBundlesCommand = new AsyncRelayCommand<BundlesViewModel>(LoadBundles);
		}

		public void ChangeAppInstance(IShellPage associatedInstance)
		{
			this.associatedInstance = associatedInstance;
		}

		private void YourHomeLoaded(RoutedEventArgs e)
		{
			YourHomeLoadedInvoked?.Invoke(this, e);
		}

		private async Task LoadBundles(BundlesViewModel viewModel)
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

		public void Dispose()
		{
			if (bundlesViewModel is not null)
			{
				bundlesViewModel.OpenPathEvent -= BundlesViewModel_OpenPathEvent;
				bundlesViewModel.OpenPathInNewPaneEvent -= BundlesViewModel_OpenPathInNewPaneEvent;
			}

			widgetsViewModel?.Dispose();
		}
	}
}

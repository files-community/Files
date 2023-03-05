using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Files.App.EventArguments.Bundles;
using Files.App.Helpers;
using Files.App.ViewModels.UserControls.Widgets;
using Files.App.ViewModels.UserControls.Widgets.Bundles;
using Microsoft.UI.Xaml;
using System;
using System.Text.Json;
using System.Windows.Input;

namespace Files.App.ViewModels
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
			if (bundlesViewModel is not null)
			{
				bundlesViewModel.OpenPathEvent -= BundlesViewModel_OpenPathEvent;
				bundlesViewModel.OpenPathInNewPaneEvent -= BundlesViewModel_OpenPathInNewPaneEvent;
			}

			widgetsViewModel?.Dispose();
		}

		#endregion IDisposable
	}
}

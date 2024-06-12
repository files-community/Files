// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Files.App.Views
{
	public sealed partial class HomePage : Page, IDisposable
	{
		// Dependency injections

		public HomeViewModel ViewModel { get; } = Ioc.Default.GetRequiredService<HomeViewModel>();

		// Properties

		private IShellPage AppInstance { get; set; } = null!;

		// Constructor

		public HomePage()
		{
			InitializeComponent();
		}

		// Methods

		protected override async void OnNavigatedTo(NavigationEventArgs e)
		{
			if (e.Parameter is not NavigationArguments parameters)
				return;

			AppInstance = parameters.AssociatedTabInstance!;

			AppInstance.InstanceViewModel.IsPageTypeNotHome = false;
			AppInstance.InstanceViewModel.IsPageTypeSearchResults = false;
			AppInstance.InstanceViewModel.IsPageTypeMtpDevice = false;
			AppInstance.InstanceViewModel.IsPageTypeRecycleBin = false;
			AppInstance.InstanceViewModel.IsPageTypeCloudDrive = false;
			AppInstance.InstanceViewModel.IsPageTypeFtp = false;
			AppInstance.InstanceViewModel.IsPageTypeZipFolder = false;
			AppInstance.InstanceViewModel.IsPageTypeLibrary = false;
			AppInstance.InstanceViewModel.GitRepositoryPath = null;
			AppInstance.InstanceViewModel.IsGitRepository = false;
			AppInstance.AddressToolbarViewModel.CanRefresh = true;
			AppInstance.AddressToolbarViewModel.CanGoBack = AppInstance.CanNavigateBackward;
			AppInstance.AddressToolbarViewModel.CanGoForward = AppInstance.CanNavigateForward;
			AppInstance.AddressToolbarViewModel.CanNavigateToParent = false;

			AppInstance.AddressToolbarViewModel.RefreshRequested -= ToolbarViewModel_RefreshRequested;
			AppInstance.AddressToolbarViewModel.RefreshRequested += ToolbarViewModel_RefreshRequested;

			// Set path of working directory empty
			await AppInstance.ShellViewModel.SetWorkingDirectoryAsync("Home");

			AppInstance.LayoutPage?.StatusBarViewModel.UpdateGitInfo(false, string.Empty, null);

			AppInstance.AddressToolbarViewModel.PathComponents.Clear();

			string componentLabel =
				parameters?.NavPathParam == "Home"
					? "Home".GetLocalizedResource()
					: parameters?.NavPathParam
				?? string.Empty;

			string tag = parameters?.NavPathParam ?? string.Empty;

			var item = new PathBoxItem()
			{
				Title = componentLabel,
				Path = tag,
			};

			AppInstance.AddressToolbarViewModel.PathComponents.Add(item);

			base.OnNavigatedTo(e);
		}

		protected override void OnNavigatedFrom(NavigationEventArgs e)
		{
			Dispose();
		}

		private async void ToolbarViewModel_RefreshRequested(object? sender, EventArgs e)
		{
			AppInstance.AddressToolbarViewModel.CanRefresh = false;

			await Task.WhenAll(ViewModel.WidgetItems.Select(w => w.WidgetItemModel.RefreshWidgetAsync()));

			AppInstance.AddressToolbarViewModel.CanRefresh = true;
		}

		// Disposer

		public void Dispose()
		{
			AppInstance.AddressToolbarViewModel.RefreshRequested -= ToolbarViewModel_RefreshRequested;
			ViewModel?.Dispose();
		}
	}
}

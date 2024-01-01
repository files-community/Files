// Copyright (c) 2023 Files Community
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

		// Overridden methods

		protected override async void OnNavigatedTo(NavigationEventArgs e)
		{
			if (e.Parameter is not NavigationArguments parameters)
				return;

			AppInstance = parameters.AssociatedTabInstance!;
			ViewModel.AppInstance = parameters.AssociatedTabInstance!;

			// Set page type
			AppInstance.InstanceViewModel.IsPageTypeNotHome = false;
			AppInstance.InstanceViewModel.IsPageTypeSearchResults = false;
			AppInstance.InstanceViewModel.IsPageTypeMtpDevice = false;
			AppInstance.InstanceViewModel.IsPageTypeRecycleBin = false;
			AppInstance.InstanceViewModel.IsPageTypeCloudDrive = false;
			AppInstance.InstanceViewModel.IsPageTypeFtp = false;
			AppInstance.InstanceViewModel.IsPageTypeZipFolder = false;
			AppInstance.InstanceViewModel.IsPageTypeLibrary = false;
			AppInstance.InstanceViewModel.GitRepositoryPath = null;

			// Set navigation ability
			AppInstance.ToolbarViewModel.CanRefresh = true;
			AppInstance.ToolbarViewModel.CanGoBack = AppInstance.CanNavigateBackward;
			AppInstance.ToolbarViewModel.CanGoForward = AppInstance.CanNavigateForward;
			AppInstance.ToolbarViewModel.CanNavigateToParent = false;

			// Hook page refresh request event
			AppInstance.ToolbarViewModel.RefreshRequested -= ToolbarViewModel_RefreshRequested;
			AppInstance.ToolbarViewModel.RefreshRequested += ToolbarViewModel_RefreshRequested;

			// Set working directory to Home
			await AppInstance.FilesystemViewModel.SetWorkingDirectoryAsync("Home");

			// Update git info to hide status bar Git info area
			AppInstance.SlimContentPage?.DirectoryPropertiesViewModel.UpdateGitInfo(false, string.Empty, null);

			// Clear the path UI and replace with Favorites
			AppInstance.ToolbarViewModel.PathComponents.Clear();

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

			// Set PathBreadcrumb to Home, not a path
			AppInstance.ToolbarViewModel.PathComponents.Add(item);

			base.OnNavigatedTo(e);
		}

		protected override void OnNavigatedFrom(NavigationEventArgs e)
		{
			Dispose();

			base.OnNavigatedFrom(e);
		}

		// Event methods

		private async void ToolbarViewModel_RefreshRequested(object? sender, EventArgs e)
		{
			AppInstance.ToolbarViewModel.CanRefresh = false;
			await Task.WhenAll(ViewModel.WidgetItems.Select(w => w.WidgetViewModel.RefreshWidgetAsync()));
			AppInstance.ToolbarViewModel.CanRefresh = true;
		}

		// Disposer

		public void Dispose()
		{
			AppInstance.ToolbarViewModel.RefreshRequested -= ToolbarViewModel_RefreshRequested;

			ViewModel?.Dispose();
		}
	}
}

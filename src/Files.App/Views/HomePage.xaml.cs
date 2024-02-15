// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Files.App.Views
{
	/// <summary>
	/// Represents Files home <see cref="Page"/>, shows widgets.
	/// </summary>
	public sealed partial class HomePage : Page, IDisposable
	{
		// Dependency injections

		public HomeViewModel HomeViewModel { get; } = Ioc.Default.GetRequiredService<HomeViewModel>();

		// Properties

		private IShellPage CurrentShellPage { get; set; } = null!;

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

			CurrentShellPage = parameters.AssociatedTabInstance!;

			CurrentShellPage.InstanceViewModel.IsPageTypeNotHome = false;
			CurrentShellPage.InstanceViewModel.IsPageTypeSearchResults = false;
			CurrentShellPage.InstanceViewModel.IsPageTypeMtpDevice = false;
			CurrentShellPage.InstanceViewModel.IsPageTypeRecycleBin = false;
			CurrentShellPage.InstanceViewModel.IsPageTypeCloudDrive = false;
			CurrentShellPage.InstanceViewModel.IsPageTypeFtp = false;
			CurrentShellPage.InstanceViewModel.IsPageTypeZipFolder = false;
			CurrentShellPage.InstanceViewModel.IsPageTypeLibrary = false;
			CurrentShellPage.InstanceViewModel.GitRepositoryPath = null;
			CurrentShellPage.ToolbarViewModel.CanRefresh = true;
			CurrentShellPage.ToolbarViewModel.CanGoBack = CurrentShellPage.CanNavigateBackward;
			CurrentShellPage.ToolbarViewModel.CanGoForward = CurrentShellPage.CanNavigateForward;
			CurrentShellPage.ToolbarViewModel.CanNavigateToParent = false;

			CurrentShellPage.ToolbarViewModel.RefreshRequested -= ToolbarViewModel_RefreshRequested;
			CurrentShellPage.ToolbarViewModel.RefreshRequested += ToolbarViewModel_RefreshRequested;

			// Set path of working directory empty
			await CurrentShellPage.FilesystemViewModel.SetWorkingDirectoryAsync("Home");

			CurrentShellPage.SlimContentPage?.DirectoryPropertiesViewModel.UpdateGitInfo(false, string.Empty, null);

			// Clear the path UI and replace with Favorites
			CurrentShellPage.ToolbarViewModel.PathComponents.Clear();

			var item = new PathBoxItem()
			{
				Title = parameters?.NavPathParam == "Home"
						? "Home".GetLocalizedResource()
						: parameters?.NavPathParam
					?? string.Empty,
				Path = parameters?.NavPathParam ?? string.Empty,
			};

			CurrentShellPage.ToolbarViewModel.PathComponents.Add(item);

			base.OnNavigatedTo(e);
		}

		protected override void OnNavigatedFrom(NavigationEventArgs e)
		{
			CurrentShellPage.ToolbarViewModel.RefreshRequested -= ToolbarViewModel_RefreshRequested;

			base.OnNavigatedFrom(e);
		}

		// Event methods

		private async void ToolbarViewModel_RefreshRequested(object? sender, EventArgs e)
		{
			CurrentShellPage.ToolbarViewModel.CanRefresh = false;

			// Refresh inner content of all widgets
			await Task.WhenAll(HomeViewModel.WidgetItems.Select(w => w.WidgetItemModel.RefreshWidgetAsync()));

			CurrentShellPage.ToolbarViewModel.CanRefresh = true;
		}

		// Disposer

		public void Dispose()
		{
			HomeViewModel?.Dispose();
		}
	}
}

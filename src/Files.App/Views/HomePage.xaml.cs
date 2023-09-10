// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Files.App.Views
{
	/// <summary>
	/// Represents <see cref="Page"/> for Files App Home page.
	/// </summary>
	public sealed partial class HomePage : Page, IDisposable
	{
		private IShellPage? _appInstance;

		public HomeViewModel ViewModel;

		public HomePage()
		{
			InitializeComponent();

			ViewModel = Ioc.Default.GetRequiredService<HomeViewModel>();
		}

		protected override async void OnNavigatedTo(NavigationEventArgs eventArgs)
		{
			if (eventArgs.Parameter is not NavigationArguments parameters || parameters.AssociatedTabInstance is null)
				return;

			// Set current shell page instance
			ViewModel.AppInstance = parameters.AssociatedTabInstance;

			// Set page type
			_appInstance = parameters.AssociatedTabInstance;
			_appInstance.InstanceViewModel.IsPageTypeNotHome = false;
			_appInstance.InstanceViewModel.IsPageTypeSearchResults = false;
			_appInstance.InstanceViewModel.IsPageTypeMtpDevice = false;
			_appInstance.InstanceViewModel.IsPageTypeRecycleBin = false;
			_appInstance.InstanceViewModel.IsPageTypeCloudDrive = false;
			_appInstance.InstanceViewModel.IsPageTypeFtp = false;
			_appInstance.InstanceViewModel.IsPageTypeZipFolder = false;
			_appInstance.InstanceViewModel.IsPageTypeLibrary = false;
			_appInstance.InstanceViewModel.GitRepositoryPath = null;
			_appInstance.ToolbarViewModel.CanRefresh = true;
			_appInstance.ToolbarViewModel.CanGoBack = _appInstance.CanNavigateBackward;
			_appInstance.ToolbarViewModel.CanGoForward = _appInstance.CanNavigateForward;
			_appInstance.ToolbarViewModel.CanNavigateToParent = false;

			// Set the working directory empty
			await _appInstance.FilesystemViewModel.SetWorkingDirectoryAsync("Home");

			// Set git info empty
			_appInstance.SlimContentPage?.DirectoryPropertiesViewModel.UpdateGitInfo(false, string.Empty, Array.Empty<BranchItem>());

			// Clear breadcrumbs
			_appInstance.ToolbarViewModel.PathComponents.Clear();

			var item = new PathBoxItem()
			{
				Title = parameters.NavPathParam == "Home" ? "Home".GetLocalizedResource() : parameters.NavPathParam,
				Path = parameters.NavPathParam,
			};

			// Set breadcrumbs
			_appInstance.ToolbarViewModel.PathComponents.Add(item);
		}

		protected override void OnNavigatedFrom(NavigationEventArgs e)
		{
			Dispose();
		}

		public void Dispose()
		{
			ViewModel?.Dispose();
		}
	}
}

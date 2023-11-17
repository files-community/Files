// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Dialogs;
using Files.App.UserControls.Widgets;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System.IO;
using System.Runtime.InteropServices;
using Windows.Storage;

namespace Files.App.Views
{
	/// <summary>
	/// Represents <see cref="Page"/> of the main Files page that provides quick shortcuts.
	/// </summary>
	public sealed partial class HomePage : Page, IDisposable
	{
		// Dependency injections

		private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();
		private HomeViewModel ViewModel { get; } = Ioc.Default.GetRequiredService<HomeViewModel>();

		// Fields

		private IShellPage? AppInstance = null;

		// Properties

		public FolderSettingsViewModel? FolderSettings
			=> AppInstance?.InstanceViewModel.FolderSettings;

		public HomePage()
		{
			InitializeComponent();

			ViewModel.HomePageLoadedInvoked += ViewModel_HomePageLoadedInvoked;
			ViewModel.WidgetListRefreshRequestedInvoked += ViewModel_WidgetListRefreshRequestedInvoked;
		}

		// Overridden methods

		protected override async void OnNavigatedTo(NavigationEventArgs e)
		{
			if (e.Parameter is not NavigationArguments parameters || parameters.AssociatedTabInstance is null)
				return;

			AppInstance = parameters.AssociatedTabInstance;
			AppInstance.InstanceViewModel.IsPageTypeNotHome = false;
			AppInstance.InstanceViewModel.IsPageTypeSearchResults = false;
			AppInstance.InstanceViewModel.IsPageTypeMtpDevice = false;
			AppInstance.InstanceViewModel.IsPageTypeRecycleBin = false;
			AppInstance.InstanceViewModel.IsPageTypeCloudDrive = false;
			AppInstance.InstanceViewModel.IsPageTypeFtp = false;
			AppInstance.InstanceViewModel.IsPageTypeZipFolder = false;
			AppInstance.InstanceViewModel.IsPageTypeLibrary = false;
			AppInstance.InstanceViewModel.GitRepositoryPath = null;
			AppInstance.ToolbarViewModel.CanRefresh = true;
			AppInstance.ToolbarViewModel.CanGoBack = AppInstance.CanNavigateBackward;
			AppInstance.ToolbarViewModel.CanGoForward = AppInstance.CanNavigateForward;
			AppInstance.ToolbarViewModel.CanNavigateToParent = false;

			AppInstance.ToolbarViewModel.RefreshRequested -= ToolbarViewModel_RefreshRequested;
			AppInstance.ToolbarViewModel.RefreshRequested += ToolbarViewModel_RefreshRequested;

			// Set path of working directory empty
			await AppInstance.FilesystemViewModel.SetWorkingDirectoryAsync("Home");

			AppInstance.SlimContentPage?.DirectoryPropertiesViewModel.UpdateGitInfo(false, string.Empty, Array.Empty<BranchItem>());

			// Clear the path UI and replace with Favorites
			AppInstance.PathBreadcrumbViewModel.PathBreadcrumbItems.Clear();

			string componentLabel = parameters.NavPathParam == "Home" ? "Home".GetLocalizedResource() : parameters.NavPathParam ?? string.Empty;
			string tag = parameters.NavPathParam ?? string.Empty;

			var item = new PathBreadcrumbItem()
			{
				Name = componentLabel,
				Path = tag,
			};

			AppInstance.PathBreadcrumbViewModel.PathBreadcrumbItems.Add(item);

			base.OnNavigatedTo(e);
		}

		protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
		{
			base.OnNavigatingFrom(e);

			AppInstance!.ToolbarViewModel.RefreshRequested -= ToolbarViewModel_RefreshRequested;
		}

		protected override void OnNavigatedFrom(NavigationEventArgs e)
		{
			Dispose();

			base.OnNavigatedFrom(e);
		}

		// Methods

		public void RefreshWidgetList()
		{
			ViewModel.RefreshWidgetList();
		}

		public void ReloadWidgets()
		{
			// Get availability of all widget items
			var shouldReloadQuickAccessWidget = WidgetsHelpers.ShouldReloadWidgetItem<QuickAccessWidgetViewModel>();
			var shouldReloadDrivesWidget = WidgetsHelpers.ShouldReloadWidgetItem<DrivesWidgetViewModel>();
			var shouldReloadFileTags = WidgetsHelpers.ShouldReloadWidgetItem<FileTagsWidgetViewModel>();
			var shouldReloadRecentFiles = WidgetsHelpers.ShouldReloadWidgetItem<RecentFilesWidgetViewModel>();

			// Reload QuickAccessWidget
			if (shouldReloadQuickAccessWidget)
			{
				var quickAccessWidget = new QuickAccessWidget();

				ViewModel.InsertWidget(
					new(
						quickAccessWidget,
						quickAccessWidget.ViewModel,
						(value) => UserSettingsService.GeneralSettingsService.FoldersWidgetExpanded = value,
						() => UserSettingsService.GeneralSettingsService.FoldersWidgetExpanded),
					0);

				quickAccessWidget.ViewModel.CardInvoked -= QuickAccessWidget_CardInvoked;
				quickAccessWidget.ViewModel.CardNewPaneInvoked -= WidgetCardNewPaneInvoked;
				quickAccessWidget.ViewModel.CardPropertiesInvoked -= QuickAccessWidget_CardPropertiesInvoked;
				quickAccessWidget.ViewModel.CardInvoked += QuickAccessWidget_CardInvoked;
				quickAccessWidget.ViewModel.CardNewPaneInvoked += WidgetCardNewPaneInvoked;
				quickAccessWidget.ViewModel.CardPropertiesInvoked += QuickAccessWidget_CardPropertiesInvoked;
			}

			// Reload DrivesWidget
			if (shouldReloadDrivesWidget)
			{
				var drivesWidget = new DrivesWidget();

				ViewModel.InsertWidget(
					new(
						drivesWidget,
						drivesWidget.ViewModel,
						(value) => UserSettingsService.GeneralSettingsService.DrivesWidgetExpanded = value,
						() => UserSettingsService.GeneralSettingsService.DrivesWidgetExpanded),
					1);

				drivesWidget.ViewModel.AppInstance = AppInstance!;
				drivesWidget.ViewModel.DrivesWidgetInvoked -= DrivesWidget_DrivesWidgetInvoked;
				drivesWidget.ViewModel.DrivesWidgetNewPaneInvoked -= DrivesWidget_DrivesWidgetNewPaneInvoked;
				drivesWidget.ViewModel.DrivesWidgetInvoked += DrivesWidget_DrivesWidgetInvoked;
				drivesWidget.ViewModel.DrivesWidgetNewPaneInvoked += DrivesWidget_DrivesWidgetNewPaneInvoked;
			}

			// Reload FileTags
			if (shouldReloadFileTags)
			{
				var fileTagsWidget = new FileTagsWidget();

				ViewModel.InsertWidget(
					new(
						fileTagsWidget,
						fileTagsWidget.ViewModel,
						(value) => UserSettingsService.GeneralSettingsService.FileTagsWidgetExpanded = value,
						() => UserSettingsService.GeneralSettingsService.FileTagsWidgetExpanded),
					2);

				fileTagsWidget.ViewModel.AppInstance = AppInstance!;
				fileTagsWidget.ViewModel.OpenAction = x => NavigationHelpers.OpenPath(x, AppInstance!);
				fileTagsWidget.ViewModel.FileTagsOpenLocationInvoked -= WidgetOpenLocationInvoked;
				fileTagsWidget.ViewModel.FileTagsNewPaneInvoked -= WidgetCardNewPaneInvoked;
				fileTagsWidget.ViewModel.FileTagsOpenLocationInvoked += WidgetOpenLocationInvoked;
				fileTagsWidget.ViewModel.FileTagsNewPaneInvoked += WidgetCardNewPaneInvoked;
				_ = fileTagsWidget.ViewModel.InitAsync();
			}

			// Reload RecentFilesWidget
			if (shouldReloadRecentFiles)
			{
				var recentFilesWidget = new RecentFilesWidget();

				ViewModel.InsertWidget(
					new(
						recentFilesWidget,
						recentFilesWidget.ViewModel,
						(value) => UserSettingsService.GeneralSettingsService.RecentFilesWidgetExpanded = value,
						() => UserSettingsService.GeneralSettingsService.RecentFilesWidgetExpanded),
					4);

				recentFilesWidget.ViewModel.AppInstance = AppInstance!;
				recentFilesWidget.ViewModel.RecentFilesOpenLocationInvoked -= WidgetOpenLocationInvoked;
				recentFilesWidget.ViewModel.RecentFileInvoked -= RecentFilesWidget_RecentFileInvoked;
				recentFilesWidget.ViewModel.RecentFilesOpenLocationInvoked += WidgetOpenLocationInvoked;
				recentFilesWidget.ViewModel.RecentFileInvoked += RecentFilesWidget_RecentFileInvoked;
			}
		}

		// Event methods

		private void ViewModel_WidgetListRefreshRequestedInvoked(object? sender, EventArgs e)
		{
			ReloadWidgets();
		}

		private void ViewModel_HomePageLoadedInvoked(object? sender, Microsoft.UI.Xaml.RoutedEventArgs e)
		{
			ReloadWidgets();
		}

		private void WidgetOpenLocationInvoked(object sender, PathNavigationEventArgs e)
		{
			AppInstance!.NavigateWithArguments(FolderSettings!.GetLayoutType(e.ItemPath), new NavigationArguments()
			{
				NavPathParam = e.ItemPath,
				SelectItems = new[] { e.ItemName },
				AssociatedTabInstance = AppInstance!
			});
		}

		private void WidgetCardNewPaneInvoked(object sender, QuickAccessCardInvokedEventArgs e)
		{
			AppInstance!.PaneHolder?.OpenPathInNewPane(e.Path);
		}

		private void QuickAccessWidget_CardInvoked(object sender, QuickAccessCardInvokedEventArgs e)
		{
			AppInstance!.NavigateWithArguments(FolderSettings!.GetLayoutType(e.Path), new NavigationArguments()
			{
				NavPathParam = e.Path
			});
		}

		private void QuickAccessWidget_CardPropertiesInvoked(object sender, QuickAccessCardEventArgs e)
		{
			ListedItem listedItem = new(null!)
			{
				ItemPath = e.Item.Path,
				ItemNameRaw = e.Item.Text,
				PrimaryItemAttribute = StorageItemTypes.Folder,
				ItemType = "Folder".GetLocalizedResource(),
			};

			FilePropertiesHelpers.OpenPropertiesWindow(listedItem, AppInstance!);
		}

		private void DrivesWidget_DrivesWidgetNewPaneInvoked(object sender, DrivesWidgetInvokedEventArgs e)
		{
			AppInstance!.PaneHolder?.OpenPathInNewPane(e.Path);
		}

		private void DrivesWidget_DrivesWidgetInvoked(object sender, DrivesWidgetInvokedEventArgs e)
		{
			AppInstance!.NavigateWithArguments(FolderSettings!.GetLayoutType(e.Path), new NavigationArguments()
			{
				NavPathParam = e.Path
			});
		}

		private async void ToolbarViewModel_RefreshRequested(object? sender, EventArgs e)
		{
			AppInstance!.ToolbarViewModel.CanRefresh = false;

			await Task.WhenAll(ViewModel.WidgetItems.Select(w => w.WidgetItemModel.RefreshWidgetAsync()));

			AppInstance.ToolbarViewModel.CanRefresh = true;
		}

		private async void RecentFilesWidget_RecentFileInvoked(object sender, PathNavigationEventArgs e)
		{
			try
			{
				if (e.IsFile)
				{
					var directoryName = Path.GetDirectoryName(e.ItemPath);
					await Win32Helpers.InvokeWin32ComponentAsync(e.ItemPath, AppInstance!, workingDirectory: directoryName ?? string.Empty);
				}
				else
				{
					AppInstance!.NavigateWithArguments(
						FolderSettings!.GetLayoutType(e.ItemPath),
						new()
						{
							NavPathParam = e.ItemPath
						});
				}
			}
			catch (UnauthorizedAccessException)
			{
				DynamicDialog dialog = DynamicDialogFactory.GetFor_ConsentDialog();
				await dialog.TryShowAsync();
			}
			catch (COMException) { }
			catch (ArgumentException) { }
		}

		// Disposer

		public void Dispose()
		{
			ViewModel.HomePageLoadedInvoked -= ViewModel_HomePageLoadedInvoked;
			ViewModel.WidgetListRefreshRequestedInvoked -= ViewModel_WidgetListRefreshRequestedInvoked;
			AppInstance!.ToolbarViewModel.RefreshRequested -= ToolbarViewModel_RefreshRequested;

			ViewModel?.Dispose();
		}
	}
}

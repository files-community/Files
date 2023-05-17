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
	public sealed partial class HomePage : Page, IDisposable
	{
		private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();

		private IShellPage AppInstance = null;

		public FolderSettingsService FolderSettings => AppInstance?.InstanceViewModel.FolderSettings;

		private QuickAccessWidget quickAccessWidget;
		private DrivesWidget drivesWidget;
		private BundlesWidget bundlesWidget;
		private FileTagsWidget fileTagsWidget;
		private RecentFilesWidget recentFilesWidget;

		public HomeViewModel ViewModel { get; set; }

		public HomePage()
		{
			InitializeComponent();

			ViewModel = new(Widgets.ViewModel, AppInstance);

			ViewModel.YourHomeLoadedInvoked += ViewModel_YourHomeLoadedInvoked;
			Widgets.ViewModel.WidgetListRefreshRequestedInvoked += ViewModel_WidgetListRefreshRequestedInvoked;
		}

		protected override void OnNavigatedFrom(NavigationEventArgs e)
		{
			Dispose();

			base.OnNavigatedFrom(e);
		}

		public void RefreshWidgetList() => Widgets.ViewModel.RefreshWidgetList();

		private void ViewModel_WidgetListRefreshRequestedInvoked(object? sender, EventArgs e)
		{
			ReloadWidgets();
		}

		public void ReloadWidgets()
		{
			quickAccessWidget = WidgetsHelpers.TryGetWidget(UserSettingsService.GeneralSettingsService, Widgets.ViewModel, out bool shouldReloadQuickAccessWidget, quickAccessWidget);
			drivesWidget =      WidgetsHelpers.TryGetWidget(UserSettingsService.GeneralSettingsService, Widgets.ViewModel, out bool shouldReloadDrivesWidget, drivesWidget);
			bundlesWidget =     WidgetsHelpers.TryGetWidget(UserSettingsService.GeneralSettingsService, Widgets.ViewModel, out bool shouldReloadBundles, bundlesWidget);
			fileTagsWidget =    WidgetsHelpers.TryGetWidget(UserSettingsService.GeneralSettingsService, Widgets.ViewModel, out bool shouldReloadFileTags, fileTagsWidget);
			recentFilesWidget = WidgetsHelpers.TryGetWidget(UserSettingsService.GeneralSettingsService, Widgets.ViewModel, out bool shouldReloadRecentFiles, recentFilesWidget);

			// Reload QuickAccessWidget
			if (shouldReloadQuickAccessWidget && quickAccessWidget is not null)
			{
				Widgets.ViewModel.InsertWidget(
					new(
						quickAccessWidget,
						(value) => UserSettingsService.GeneralSettingsService.FoldersWidgetExpanded = value,
						() => UserSettingsService.GeneralSettingsService.FoldersWidgetExpanded),
					0);

				quickAccessWidget.CardInvoked -= QuickAccessWidget_CardInvoked;
				quickAccessWidget.CardNewPaneInvoked -= WidgetCardNewPaneInvoked;
				quickAccessWidget.CardPropertiesInvoked -= QuickAccessWidget_CardPropertiesInvoked;
				quickAccessWidget.CardInvoked += QuickAccessWidget_CardInvoked;
				quickAccessWidget.CardNewPaneInvoked += WidgetCardNewPaneInvoked;
				quickAccessWidget.CardPropertiesInvoked += QuickAccessWidget_CardPropertiesInvoked;
			}

			// Reload DrivesWidget
			if (shouldReloadDrivesWidget && drivesWidget is not null)
			{
				Widgets.ViewModel.InsertWidget(new(drivesWidget, (value) => UserSettingsService.GeneralSettingsService.DrivesWidgetExpanded = value, () => UserSettingsService.GeneralSettingsService.DrivesWidgetExpanded), 1);

				drivesWidget.AppInstance = AppInstance;
				drivesWidget.DrivesWidgetInvoked -= DrivesWidget_DrivesWidgetInvoked;
				drivesWidget.DrivesWidgetNewPaneInvoked -= DrivesWidget_DrivesWidgetNewPaneInvoked;
				drivesWidget.DrivesWidgetInvoked += DrivesWidget_DrivesWidgetInvoked;
				drivesWidget.DrivesWidgetNewPaneInvoked += DrivesWidget_DrivesWidgetNewPaneInvoked;
			}

			// Reload FileTags
			if (shouldReloadFileTags && fileTagsWidget is not null)
			{
				Widgets.ViewModel.InsertWidget(new(fileTagsWidget, (value) => UserSettingsService.GeneralSettingsService.FileTagsWidgetExpanded = value, () => UserSettingsService.GeneralSettingsService.FileTagsWidgetExpanded), 2);

				fileTagsWidget.AppInstance = AppInstance;
				fileTagsWidget.OpenAction = x => NavigationHelpers.OpenPath(x, AppInstance);
				fileTagsWidget.FileTagsOpenLocationInvoked -= WidgetOpenLocationInvoked;
				fileTagsWidget.FileTagsNewPaneInvoked -= WidgetCardNewPaneInvoked;
				fileTagsWidget.FileTagsOpenLocationInvoked += WidgetOpenLocationInvoked;
				fileTagsWidget.FileTagsNewPaneInvoked += WidgetCardNewPaneInvoked;
				_ = fileTagsWidget.ViewModel.InitAsync();
			}

			// Reload BundlesWidget
			if (shouldReloadBundles && bundlesWidget is not null)
			{
				Widgets.ViewModel.InsertWidget(new(bundlesWidget, (value) => UserSettingsService.GeneralSettingsService.BundlesWidgetExpanded = value, () => UserSettingsService.GeneralSettingsService.BundlesWidgetExpanded), 3);
				ViewModel.LoadBundlesCommand.Execute(bundlesWidget.ViewModel);
			}

			// Reload RecentFilesWidget
			if (shouldReloadRecentFiles && recentFilesWidget is not null)
			{
				Widgets.ViewModel.InsertWidget(new(recentFilesWidget, (value) => UserSettingsService.GeneralSettingsService.RecentFilesWidgetExpanded = value, () => UserSettingsService.GeneralSettingsService.RecentFilesWidgetExpanded), 4);

				recentFilesWidget.AppInstance = AppInstance;
				recentFilesWidget.RecentFilesOpenLocationInvoked -= WidgetOpenLocationInvoked;
				recentFilesWidget.RecentFileInvoked -= RecentFilesWidget_RecentFileInvoked;
				recentFilesWidget.RecentFilesOpenLocationInvoked += WidgetOpenLocationInvoked;
				recentFilesWidget.RecentFileInvoked += RecentFilesWidget_RecentFileInvoked;
			}
		}

		private void ViewModel_YourHomeLoadedInvoked(object? sender, Microsoft.UI.Xaml.RoutedEventArgs e)
		{
			// NOTE: We must change the associatedInstance because only now it has loaded and not null
			ViewModel.ChangeAppInstance(AppInstance);

			ReloadWidgets();
		}

		private async void RecentFilesWidget_RecentFileInvoked(object sender, PathNavigationEventArgs e)
		{
			try
			{
				if (e.IsFile)
				{
					var directoryName = Path.GetDirectoryName(e.ItemPath);
					await Win32Helpers.InvokeWin32ComponentAsync(e.ItemPath, AppInstance, workingDirectory: directoryName);
				}
				else
				{
					AppInstance.NavigateWithArguments(
						FolderSettings.GetLayoutType(e.ItemPath),
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

		private void WidgetOpenLocationInvoked(object sender, PathNavigationEventArgs e)
		{
			AppInstance.NavigateWithArguments(FolderSettings.GetLayoutType(e.ItemPath), new NavigationArguments()
			{
				NavPathParam = e.ItemPath,
				SelectItems = new[] { e.ItemName },
				AssociatedTabInstance = AppInstance
			});
		}

		private void QuickAccessWidget_CardInvoked(object sender, QuickAccessCardInvokedEventArgs e)
		{
			AppInstance.NavigateWithArguments(FolderSettings.GetLayoutType(e.Path), new NavigationArguments()
			{
				NavPathParam = e.Path
			});

			// Show controls that were hidden on the home page
			AppInstance.InstanceViewModel.IsPageTypeNotHome = true;
		}

		private void WidgetCardNewPaneInvoked(object sender, QuickAccessCardInvokedEventArgs e)
		{
			AppInstance.PaneHolder?.OpenPathInNewPane(e.Path);
		}

		private async void QuickAccessWidget_CardPropertiesInvoked(object sender, QuickAccessCardEventArgs e)
		{
			ListedItem listedItem = new(null!)
			{
				ItemPath = e.Item.Path,
				ItemNameRaw = e.Item.Text,
				PrimaryItemAttribute = StorageItemTypes.Folder,
				ItemType = "Folder".GetLocalizedResource(),
			};

			FilePropertiesHelpers.OpenPropertiesWindow(listedItem, AppInstance);
		}

		private void DrivesWidget_DrivesWidgetNewPaneInvoked(object sender, DrivesWidget.DrivesWidgetInvokedEventArgs e)
		{
			AppInstance.PaneHolder?.OpenPathInNewPane(e.Path);
		}

		private void DrivesWidget_DrivesWidgetInvoked(object sender, DrivesWidget.DrivesWidgetInvokedEventArgs e)
		{
			AppInstance.NavigateWithArguments(FolderSettings.GetLayoutType(e.Path), new NavigationArguments()
			{
				NavPathParam = e.Path
			});

			// Show controls that were hidden on the home page
			AppInstance.InstanceViewModel.IsPageTypeNotHome = true;
		}

		protected override async void OnNavigatedTo(NavigationEventArgs eventArgs)
		{
			var parameters = eventArgs.Parameter as NavigationArguments;

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

			AppInstance.SlimContentPage?.DirectoryPropertiesViewModel.UpdateGitInfo(false, string.Empty, Array.Empty<string>());

			// Clear the path UI and replace with Favorites
			AppInstance.ToolbarViewModel.PathComponents.Clear();
			string componentLabel = parameters.NavPathParam == "Home" ? "Home".GetLocalizedResource() : parameters.NavPathParam;
			string tag = parameters.NavPathParam;
			var item = new PathBoxItem()
			{
				Title = componentLabel,
				Path = tag,
			};

			AppInstance.ToolbarViewModel.PathComponents.Add(item);
			base.OnNavigatedTo(eventArgs);
		}

		protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
		{
			base.OnNavigatingFrom(e);

			AppInstance.ToolbarViewModel.RefreshRequested -= ToolbarViewModel_RefreshRequested;
		}

		private async void ToolbarViewModel_RefreshRequested(object? sender, EventArgs e)
		{
			AppInstance.ToolbarViewModel.CanRefresh = false;
			await Task.WhenAll(Widgets.ViewModel.Widgets.Select(w => w.WidgetItemModel.RefreshWidget()));
			AppInstance.ToolbarViewModel.CanRefresh = true;
		}

		public void Dispose()
		{
			ViewModel.YourHomeLoadedInvoked -= ViewModel_YourHomeLoadedInvoked;
			Widgets.ViewModel.WidgetListRefreshRequestedInvoked -= ViewModel_WidgetListRefreshRequestedInvoked;
			AppInstance.ToolbarViewModel.RefreshRequested -= ToolbarViewModel_RefreshRequested;
			ViewModel?.Dispose();
		}
	}
}

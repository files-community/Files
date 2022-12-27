using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.DataModels.NavigationControlItems;
using Files.App.Dialogs;
using Files.App.Extensions;
using Files.App.Filesystem;
using Files.App.Helpers;
using Files.App.UserControls.Widgets;
using Files.App.ViewModels;
using Files.App.ViewModels.Pages;
using Files.Backend.Services.Settings;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Files.App.Views
{
	public sealed partial class WidgetsPage : Page, IDisposable
	{
		private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();

		private IShellPage AppInstance = null;
		public FolderSettingsViewModel FolderSettings => AppInstance?.InstanceViewModel.FolderSettings;

		private FolderWidget folderWidget;
		private DrivesWidget drivesWidget;
		private BundlesWidget bundlesWidget;
		private RecentFilesWidget recentFilesWidget;

		public YourHomeViewModel ViewModel
		{
			get => (YourHomeViewModel)DataContext;
			set => DataContext = value;
		}

		public WidgetsPage()
		{
			InitializeComponent();

			ViewModel = new YourHomeViewModel(Widgets.ViewModel, AppInstance);
			ViewModel.YourHomeLoadedInvoked += ViewModel_YourHomeLoadedInvoked;
			Widgets.ViewModel.WidgetListRefreshRequestedInvoked += ViewModel_WidgetListRefreshRequestedInvoked;
		}

		protected override void OnNavigatedFrom(NavigationEventArgs e)
		{
			this.Dispose();

			base.OnNavigatedFrom(e);
		}

		public void RefreshWidgetList() => Widgets.ViewModel.RefreshWidgetList();

		private void ViewModel_WidgetListRefreshRequestedInvoked(object? sender, EventArgs e)
		{
			ReloadWidgets();
		}

		private void ReloadWidgets()
		{
			folderWidget = WidgetsHelpers.TryGetWidget<FolderWidget>(UserSettingsService.AppearanceSettingsService, Widgets.ViewModel, out bool shouldReloadFolderWidget, folderWidget);
			drivesWidget = WidgetsHelpers.TryGetWidget<DrivesWidget>(UserSettingsService.AppearanceSettingsService, Widgets.ViewModel, out bool shouldReloadDrivesWidget, drivesWidget);
			bundlesWidget = WidgetsHelpers.TryGetWidget<BundlesWidget>(UserSettingsService.AppearanceSettingsService, Widgets.ViewModel, out bool shouldReloadBundles, bundlesWidget);
			recentFilesWidget = WidgetsHelpers.TryGetWidget<RecentFilesWidget>(UserSettingsService.AppearanceSettingsService, Widgets.ViewModel, out bool shouldReloadRecentFiles, recentFilesWidget);

			if (shouldReloadFolderWidget && folderWidget is not null)
			{
				Widgets.ViewModel.InsertWidget(new(folderWidget, (value) => UserSettingsService.AppearanceSettingsService.FoldersWidgetExpanded = value, () => UserSettingsService.AppearanceSettingsService.FoldersWidgetExpanded), 0);

				folderWidget.LibraryCardInvoked -= FolderWidget_LibraryCardInvoked;
				folderWidget.LibraryCardNewPaneInvoked -= FolderWidget_LibraryCardNewPaneInvoked;
				folderWidget.LibraryCardPropertiesInvoked -= FolderWidget_LibraryCardPropertiesInvoked;
				folderWidget.FolderWidgethowMultiPaneControlsInvoked -= FolderWidget_FolderWidgethowMultiPaneControlsInvoked;
				folderWidget.LibraryCardInvoked += FolderWidget_LibraryCardInvoked;
				folderWidget.LibraryCardNewPaneInvoked += FolderWidget_LibraryCardNewPaneInvoked;
				folderWidget.LibraryCardPropertiesInvoked += FolderWidget_LibraryCardPropertiesInvoked;
				folderWidget.FolderWidgethowMultiPaneControlsInvoked += FolderWidget_FolderWidgethowMultiPaneControlsInvoked;
			}
			if (shouldReloadDrivesWidget && drivesWidget is not null)
			{
				Widgets.ViewModel.InsertWidget(new(drivesWidget, (value) => UserSettingsService.AppearanceSettingsService.DrivesWidgetExpanded = value, () => UserSettingsService.AppearanceSettingsService.DrivesWidgetExpanded), 1);

				drivesWidget.AppInstance = AppInstance;
				drivesWidget.DrivesWidgetInvoked -= DrivesWidget_DrivesWidgetInvoked;
				drivesWidget.DrivesWidgetNewPaneInvoked -= DrivesWidget_DrivesWidgetNewPaneInvoked;
				drivesWidget.DrivesWidgetInvoked += DrivesWidget_DrivesWidgetInvoked;
				drivesWidget.DrivesWidgetNewPaneInvoked += DrivesWidget_DrivesWidgetNewPaneInvoked;
			}
			if (shouldReloadBundles && bundlesWidget is not null)
			{
				Widgets.ViewModel.InsertWidget(new(bundlesWidget, (value) => UserSettingsService.AppearanceSettingsService.BundlesWidgetExpanded = value, () => UserSettingsService.AppearanceSettingsService.BundlesWidgetExpanded), 2);
				ViewModel.LoadBundlesCommand.Execute(bundlesWidget.ViewModel);
			}
			if (shouldReloadRecentFiles && recentFilesWidget is not null)
			{
				Widgets.ViewModel.InsertWidget(new(recentFilesWidget, (value) => UserSettingsService.AppearanceSettingsService.RecentFilesWidgetExpanded = value, () => UserSettingsService.AppearanceSettingsService.RecentFilesWidgetExpanded), 3);

				recentFilesWidget.RecentFilesOpenLocationInvoked -= RecentFilesWidget_RecentFilesOpenLocationInvoked;
				recentFilesWidget.RecentFileInvoked -= RecentFilesWidget_RecentFileInvoked;
				recentFilesWidget.RecentFilesOpenLocationInvoked += RecentFilesWidget_RecentFilesOpenLocationInvoked;
				recentFilesWidget.RecentFileInvoked += RecentFilesWidget_RecentFileInvoked;
			}
		}

		private void ViewModel_YourHomeLoadedInvoked(object? sender, Microsoft.UI.Xaml.RoutedEventArgs e)
		{
			// We must change the associatedInstance because only now it has loaded and not null
			ViewModel.ChangeAppInstance(AppInstance);
			ReloadWidgets();
		}

		private void FolderWidget_FolderWidgethowMultiPaneControlsInvoked(object sender, EventArgs e)
		{
			FolderWidget FolderWidget = (FolderWidget)sender;

			FolderWidget.ShowMultiPaneControls = AppInstance.PaneHolder?.IsMultiPaneEnabled ?? false;
		}

		// WINUI3
		private static ContentDialog SetContentDialogRoot(ContentDialog contentDialog)
		{
			if (Windows.Foundation.Metadata.ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
			{
				contentDialog.XamlRoot = App.Window.Content.XamlRoot;
			}
			return contentDialog;
		}

		private async void RecentFilesWidget_RecentFileInvoked(object sender, UserControls.PathNavigationEventArgs e)
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
					AppInstance.NavigateWithArguments(FolderSettings.GetLayoutType(e.ItemPath), new NavigationArguments()
					{
						NavPathParam = e.ItemPath
					});
				}
			}
			catch (UnauthorizedAccessException)
			{
				DynamicDialog dialog = DynamicDialogFactory.GetFor_ConsentDialog();
				await SetContentDialogRoot(dialog).ShowAsync();
			}
			catch (Exception ex) when (ex is COMException || ex is ArgumentException)
			{
				await DialogDisplayHelper.ShowDialogAsync(
					"DriveUnpluggedDialog/Title".GetLocalizedResource(),
					"DriveUnpluggedDialog/Text".GetLocalizedResource());
			}
		}

		private void RecentFilesWidget_RecentFilesOpenLocationInvoked(object sender, UserControls.PathNavigationEventArgs e)
		{
			AppInstance.NavigateWithArguments(FolderSettings.GetLayoutType(e.ItemPath), new NavigationArguments()
			{
				NavPathParam = e.ItemPath,
				SelectItems = new[] { e.ItemName },
				AssociatedTabInstance = AppInstance
			});
		}

		private void FolderWidget_LibraryCardInvoked(object sender, LibraryCardInvokedEventArgs e)
		{
			AppInstance.NavigateWithArguments(FolderSettings.GetLayoutType(e.Path), new NavigationArguments()
			{
				NavPathParam = e.Path
			});
			AppInstance.InstanceViewModel.IsPageTypeNotHome = true;     // show controls that were hidden on the home page
		}

		private void FolderWidget_LibraryCardNewPaneInvoked(object sender, LibraryCardInvokedEventArgs e)
		{
			AppInstance.PaneHolder?.OpenPathInNewPane(e.Path);
		}

		private async void FolderWidget_LibraryCardPropertiesInvoked(object sender, LibraryCardEventArgs e)
		{
			await FilePropertiesHelpers.OpenPropertiesWindowAsync(new LibraryItem(e.Library), AppInstance);
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
			AppInstance.InstanceViewModel.IsPageTypeNotHome = true;     // show controls that were hidden on the home page
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
			AppInstance.ToolbarViewModel.CanRefresh = true;
			AppInstance.ToolbarViewModel.CanGoBack = AppInstance.CanNavigateBackward;
			AppInstance.ToolbarViewModel.CanGoForward = AppInstance.CanNavigateForward;
			AppInstance.ToolbarViewModel.CanNavigateToParent = false;

			AppInstance.ToolbarViewModel.RefreshRequested -= ToolbarViewModel_RefreshRequested;
			AppInstance.ToolbarViewModel.RefreshRequested += ToolbarViewModel_RefreshRequested;

			// Set path of working directory empty
			await AppInstance.FilesystemViewModel.SetWorkingDirectoryAsync("Home".GetLocalizedResource());

			// Clear the path UI and replace with Favorites
			AppInstance.ToolbarViewModel.PathComponents.Clear();
			string componentLabel = parameters.NavPathParam;
			string tag = parameters.NavPathParam;
			PathBoxItem item = new PathBoxItem()
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
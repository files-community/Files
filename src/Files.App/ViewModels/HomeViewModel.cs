// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.UserControls.Widgets;
using Microsoft.UI.Xaml;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Input;
using Windows.Foundation.Metadata;
using Windows.Storage;

namespace Files.App.ViewModels
{
	public class HomeViewModel : ObservableObject, IDisposable
	{
		// Dependency injections

		private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();

		// Fields

		private QuickAccessWidgetViewModel? quickAccessWidget;
		private DrivesWidgetViewModel? drivesWidget;
		private FileTagsWidgetViewModel? fileTagsWidget;
		private RecentFilesWidgetViewModel? recentFilesWidget;

		// Properties

		public ObservableCollection<WidgetContainerItem> WidgetItems { get; } = new();

		public IShellPage AppInstance { get; set; } = null!;

		public LayoutPreferencesManager LayoutPreferencesManager
			=> AppInstance?.InstanceViewModel.FolderSettings!;

		// Commands

		public ICommand HomePageLoadedCommand { get; }

		// Events

		public event EventHandler<RoutedEventArgs>? HomePageLoadedInvoked;
		public event EventHandler? WidgetListRefreshRequestedInvoked;

		// Constructor

		public HomeViewModel()
		{
			HomePageLoadedInvoked += HomePageLoaded;
			WidgetListRefreshRequestedInvoked += WidgetListRefreshRequested;

			HomePageLoadedCommand = new RelayCommand<RoutedEventArgs>(ExecuteHomePageLoadedCommand);
		}

		// Methods

		public void ReloadWidgets()
		{
			quickAccessWidget = WidgetsHelpers.TryGetWidget(this, out bool shouldReloadQuickAccessWidget, quickAccessWidget);
			drivesWidget = WidgetsHelpers.TryGetWidget(this, out bool shouldReloadDrivesWidget, drivesWidget);
			fileTagsWidget = WidgetsHelpers.TryGetWidget(this, out bool shouldReloadFileTags, fileTagsWidget);
			recentFilesWidget = WidgetsHelpers.TryGetWidget(this, out bool shouldReloadRecentFiles, recentFilesWidget);

			// Reload QuickAccessWidget
			if (shouldReloadQuickAccessWidget && quickAccessWidget is not null)
			{
				var control = new QuickAccessWidget();

				InsertWidget(new(control, (value) => UserSettingsService.GeneralSettingsService.FoldersWidgetExpanded = value, () => UserSettingsService.GeneralSettingsService.FoldersWidgetExpanded), 0);

				control.ViewModel = quickAccessWidget;
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
				var control = new DrivesWidget();

				InsertWidget(new(control, (value) => UserSettingsService.GeneralSettingsService.DrivesWidgetExpanded = value, () => UserSettingsService.GeneralSettingsService.DrivesWidgetExpanded), 1);

				control.ViewModel = drivesWidget;
				drivesWidget.AppInstance = AppInstance;
				drivesWidget.DrivesWidgetInvoked -= DrivesWidget_DrivesWidgetInvoked;
				drivesWidget.DrivesWidgetNewPaneInvoked -= DrivesWidget_DrivesWidgetNewPaneInvoked;
				drivesWidget.DrivesWidgetInvoked += DrivesWidget_DrivesWidgetInvoked;
				drivesWidget.DrivesWidgetNewPaneInvoked += DrivesWidget_DrivesWidgetNewPaneInvoked;
			}

			// Reload FileTagsWidget
			if (shouldReloadFileTags && fileTagsWidget is not null)
			{
				var control = new FileTagsWidget();

				InsertWidget(new(control, (value) => UserSettingsService.GeneralSettingsService.FileTagsWidgetExpanded = value, () => UserSettingsService.GeneralSettingsService.FileTagsWidgetExpanded), 2);

				control.ViewModel = fileTagsWidget;
				fileTagsWidget.AppInstance = AppInstance;
				fileTagsWidget.OpenAction = x => NavigationHelpers.OpenPath(x, AppInstance);
				fileTagsWidget.FileTagsOpenLocationInvoked -= WidgetOpenLocationInvoked;
				fileTagsWidget.FileTagsNewPaneInvoked -= WidgetCardNewPaneInvoked;
				fileTagsWidget.FileTagsOpenLocationInvoked += WidgetOpenLocationInvoked;
				fileTagsWidget.FileTagsNewPaneInvoked += WidgetCardNewPaneInvoked;
				_ = fileTagsWidget.InitAsync();
			}

			// Reload RecentFilesWidget
			if (shouldReloadRecentFiles && recentFilesWidget is not null)
			{
				var control = new RecentFilesWidget();

				InsertWidget(new(control, (value) => UserSettingsService.GeneralSettingsService.RecentFilesWidgetExpanded = value, () => UserSettingsService.GeneralSettingsService.RecentFilesWidgetExpanded), 4);

				control.ViewModel = recentFilesWidget;
				recentFilesWidget.AppInstance = AppInstance;
				recentFilesWidget.RecentFilesOpenLocationInvoked -= WidgetOpenLocationInvoked;
				recentFilesWidget.RecentFileInvoked -= RecentFilesWidget_RecentFileInvoked;
				recentFilesWidget.RecentFilesOpenLocationInvoked += WidgetOpenLocationInvoked;
				recentFilesWidget.RecentFileInvoked += RecentFilesWidget_RecentFileInvoked;
			}
		}

		public void RefreshWidgetList()
		{
			for (int i = 0; i < WidgetItems.Count; i++)
			{
				if (!WidgetItems[i].WidgetViewModel.IsWidgetSettingEnabled)
				{
					RemoveWidgetAt(i);
				}
			}

			WidgetListRefreshRequestedInvoked?.Invoke(this, EventArgs.Empty);
		}

		public bool AddWidget(WidgetContainerItem widgetContainerItem)
		{
			return InsertWidget(widgetContainerItem, WidgetItems.Count + 1);
		}

		public bool InsertWidget(WidgetContainerItem widgetContainerItem, int atIndex)
		{
			if (atIndex > WidgetItems.Count)
			{
				WidgetItems.Add(widgetContainerItem);
			}
			else
			{
				WidgetItems.Insert(atIndex, widgetContainerItem);
			}

			return true;
		}

		public bool CanAddWidget(string widgetName)
		{
			return !(WidgetItems.Any((item) => item.WidgetViewModel.WidgetName == widgetName));
		}

		public void RemoveWidgetAt(int index)
		{
			if (index < 0)
			{
				return;
			}

			WidgetItems[index].Dispose();
			WidgetItems.RemoveAt(index);
		}

		public void RemoveWidget<TWidget>() where TWidget : IWidgetViewModel
		{
			int indexToRemove = -1;

			for (int i = 0; i < WidgetItems.Count; i++)
			{
				if (typeof(TWidget).IsAssignableFrom(WidgetItems[i].WidgetControl.GetType()))
				{
					// Found matching types
					indexToRemove = i;
					break;
				}
			}

			RemoveWidgetAt(indexToRemove);
		}

		public void ReorderWidget(WidgetContainerItem widgetContainerItem, int place)
		{
			int widgetIndex = WidgetItems.IndexOf(widgetContainerItem);
			WidgetItems.Move(widgetIndex, place);
		}

		// Event methods

		private void WidgetListRefreshRequested(object? sender, EventArgs e)
		{
			ReloadWidgets();
		}

		private void HomePageLoaded(object? sender, RoutedEventArgs e)
		{
			ReloadWidgets();
		}

		private void WidgetOpenLocationInvoked(object sender, PathNavigationEventArgs e)
		{
			AppInstance.NavigateWithArguments(LayoutPreferencesManager.GetLayoutType(e.ItemPath), new NavigationArguments()
			{
				NavPathParam = e.ItemPath,
				SelectItems = new[] { e.ItemName },
				AssociatedTabInstance = AppInstance
			});
		}

		private void WidgetCardNewPaneInvoked(object sender, QuickAccessCardInvokedEventArgs e)
		{
			AppInstance.PaneHolder?.OpenPathInNewPane(e.Path);
		}

		private void QuickAccessWidget_CardInvoked(object sender, QuickAccessCardInvokedEventArgs e)
		{
			AppInstance.NavigateWithArguments(LayoutPreferencesManager.GetLayoutType(e.Path), new NavigationArguments()
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

			FilePropertiesHelpers.OpenPropertiesWindow(listedItem, AppInstance);
		}

		private void DrivesWidget_DrivesWidgetNewPaneInvoked(object sender, DrivesWidgetInvokedEventArgs e)
		{
			AppInstance.PaneHolder?.OpenPathInNewPane(e.Path);
		}

		private void DrivesWidget_DrivesWidgetInvoked(object sender, DrivesWidgetInvokedEventArgs e)
		{
			AppInstance.NavigateWithArguments(LayoutPreferencesManager.GetLayoutType(e.Path), new NavigationArguments()
			{
				NavPathParam = e.Path
			});
		}

		private async void RecentFilesWidget_RecentFileInvoked(object sender, PathNavigationEventArgs e)
		{
			try
			{
				if (e.IsFile)
				{
					var directoryName = Path.GetDirectoryName(e.ItemPath);
					await Win32Helpers.InvokeWin32ComponentAsync(e.ItemPath, AppInstance, workingDirectory: directoryName ?? string.Empty);
				}
				else
				{
					AppInstance.NavigateWithArguments(
						LayoutPreferencesManager.GetLayoutType(e.ItemPath),
						new()
						{
							NavPathParam = e.ItemPath
						});
				}
			}
			catch (UnauthorizedAccessException)
			{
				var dialog = DynamicDialogFactory.GetFor_ConsentDialog();

				if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
					dialog.XamlRoot = MainWindow.Instance.Content.XamlRoot;

				await dialog.TryShowAsync();
			}
			catch (COMException) { }
			catch (ArgumentException) { }
		}

		// Command methods

		private void ExecuteHomePageLoadedCommand(RoutedEventArgs? e)
		{
			HomePageLoadedInvoked?.Invoke(this, e!);
		}

		public void Dispose()
		{
			for (int i = 0; i < WidgetItems.Count; i++)
				WidgetItems[i].Dispose();

			WidgetItems.Clear();

			HomePageLoadedInvoked -= HomePageLoaded;
			WidgetListRefreshRequestedInvoked -= WidgetListRefreshRequested;
		}
	}
}

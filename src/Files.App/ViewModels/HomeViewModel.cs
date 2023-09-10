// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using System.Windows.Input;
using Files.App.Dialogs;
using Files.App.UserControls.Widgets;
using System.IO;
using System.Runtime.InteropServices;
using Windows.Storage;

namespace Files.App.ViewModels
{
	/// <summary>
	/// Represents ViewModel for <see cref="HomePage"/>.
	/// </summary>
	public class HomeViewModel : ObservableObject, IDisposable
	{
		private readonly IUserSettingsService _userSettingsService = Ioc.Default.GetRequiredService<IUserSettingsService>();

		private QuickAccessWidget quickAccessWidget;
		private DrivesWidget drivesWidget;
		private FileTagsWidget fileTagsWidget;
		private RecentFilesWidget recentFilesWidget;

		public IShellPage AppInstance { get; set; }

		public FolderSettingsViewModel FolderSettings
			=> AppInstance?.InstanceViewModel.FolderSettings;

		public ObservableCollection<WidgetsListControlItemViewModel> Widgets { get; private set; } = new();

		public event EventHandler<RoutedEventArgs> YourHomeLoadedInvoked;

		public ICommand YourHomeLoadedCommand { get; private set; }

		public HomeViewModel()
		{
			YourHomeLoadedInvoked += ViewModel_YourHomeLoadedInvoked;
			WidgetListRefreshRequestedInvoked += ViewModel_WidgetListRefreshRequestedInvoked;

			// Hook events
			AppInstance.ToolbarViewModel.RefreshRequested -= ToolbarViewModel_RefreshRequested;
			AppInstance.ToolbarViewModel.RefreshRequested += ToolbarViewModel_RefreshRequested;

			// Create commands
			YourHomeLoadedCommand = new RelayCommand<RoutedEventArgs>(YourHomeLoaded);
		}

		private void ViewModel_WidgetListRefreshRequestedInvoked(object? sender, EventArgs e)
		{
			ReloadWidgets();
		}

		public void ReloadWidgets()
		{
			quickAccessWidget = WidgetsHelpers.TryGetWidget(_userSettingsService.GeneralSettingsService, this, out bool shouldReloadQuickAccessWidget, quickAccessWidget);
			drivesWidget = WidgetsHelpers.TryGetWidget(_userSettingsService.GeneralSettingsService, this, out bool shouldReloadDrivesWidget, drivesWidget);
			fileTagsWidget = WidgetsHelpers.TryGetWidget(_userSettingsService.GeneralSettingsService, this, out bool shouldReloadFileTags, fileTagsWidget);
			recentFilesWidget = WidgetsHelpers.TryGetWidget(_userSettingsService.GeneralSettingsService, this, out bool shouldReloadRecentFiles, recentFilesWidget);

			// Reload QuickAccessWidget
			if (shouldReloadQuickAccessWidget && quickAccessWidget is not null)
			{
				InsertWidget(
					new(
						quickAccessWidget,
						(value) => _userSettingsService.GeneralSettingsService.FoldersWidgetExpanded = value,
						() => _userSettingsService.GeneralSettingsService.FoldersWidgetExpanded),
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
				InsertWidget(new(drivesWidget, (value) => _userSettingsService.GeneralSettingsService.DrivesWidgetExpanded = value, () => _userSettingsService.GeneralSettingsService.DrivesWidgetExpanded), 1);

				drivesWidget.AppInstance = AppInstance;
				drivesWidget.DrivesWidgetInvoked -= DrivesWidget_DrivesWidgetInvoked;
				drivesWidget.DrivesWidgetNewPaneInvoked -= DrivesWidget_DrivesWidgetNewPaneInvoked;
				drivesWidget.DrivesWidgetInvoked += DrivesWidget_DrivesWidgetInvoked;
				drivesWidget.DrivesWidgetNewPaneInvoked += DrivesWidget_DrivesWidgetNewPaneInvoked;
			}

			// Reload FileTags
			if (shouldReloadFileTags && fileTagsWidget is not null)
			{
				InsertWidget(new(fileTagsWidget, (value) => _userSettingsService.GeneralSettingsService.FileTagsWidgetExpanded = value, () => _userSettingsService.GeneralSettingsService.FileTagsWidgetExpanded), 2);

				fileTagsWidget.AppInstance = AppInstance;
				fileTagsWidget.OpenAction = x => NavigationHelpers.OpenPath(x, AppInstance);
				fileTagsWidget.FileTagsOpenLocationInvoked -= WidgetOpenLocationInvoked;
				fileTagsWidget.FileTagsNewPaneInvoked -= WidgetCardNewPaneInvoked;
				fileTagsWidget.FileTagsOpenLocationInvoked += WidgetOpenLocationInvoked;
				fileTagsWidget.FileTagsNewPaneInvoked += WidgetCardNewPaneInvoked;
				_ = fileTagsWidget.ViewModel.InitAsync();
			}

			// Reload RecentFilesWidget
			if (shouldReloadRecentFiles && recentFilesWidget is not null)
			{
				InsertWidget(new(recentFilesWidget, (value) => _userSettingsService.GeneralSettingsService.RecentFilesWidgetExpanded = value, () => _userSettingsService.GeneralSettingsService.RecentFilesWidgetExpanded), 4);

				recentFilesWidget.AppInstance = AppInstance;
				recentFilesWidget.RecentFilesOpenLocationInvoked -= WidgetOpenLocationInvoked;
				recentFilesWidget.RecentFileInvoked -= RecentFilesWidget_RecentFileInvoked;
				recentFilesWidget.RecentFilesOpenLocationInvoked += WidgetOpenLocationInvoked;
				recentFilesWidget.RecentFileInvoked += RecentFilesWidget_RecentFileInvoked;
			}
		}

		private void ViewModel_YourHomeLoadedInvoked(object? sender, RoutedEventArgs e)
		{
			// NOTE: We must change the AppInstance because only now it has loaded and not null
			ChangeAppInstance(AppInstance);

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
		}

		private async void ToolbarViewModel_RefreshRequested(object? sender, EventArgs e)
		{
			AppInstance.ToolbarViewModel.CanRefresh = false;
			await Task.WhenAll(Widgets.Select(w => w.WidgetItemModel.RefreshWidget()));
			AppInstance.ToolbarViewModel.CanRefresh = true;
		}

		public void ChangeAppInstance(IShellPage AppInstance)
		{
			this.AppInstance = AppInstance;
		}

		private void YourHomeLoaded(RoutedEventArgs e)
		{
			YourHomeLoadedInvoked?.Invoke(this, e);
		}

		public event EventHandler WidgetListRefreshRequestedInvoked;

		public void RefreshWidgetList()
		{
			for (int i = 0; i < Widgets.Count; i++)
			{
				if (!Widgets[i].WidgetItemModel.IsWidgetSettingEnabled)
				{
					RemoveWidgetAt(i);
				}
			}

			WidgetListRefreshRequestedInvoked?.Invoke(this, EventArgs.Empty);
		}

		public bool AddWidget(WidgetsListControlItemViewModel widgetModel)
		{
			return InsertWidget(widgetModel, Widgets.Count + 1);
		}

		public bool InsertWidget(WidgetsListControlItemViewModel widgetModel, int atIndex)
		{
			// The widget must not be null and must implement IWidgetItemModel
			if (widgetModel.WidgetItemModel is not IWidgetItemModel widgetItemModel)
			{
				return false;
			}

			// Don't add existing ones!
			if (!CanAddWidget(widgetItemModel.WidgetName))
			{
				return false;
			}

			if (atIndex > Widgets.Count)
			{
				Widgets.Add(widgetModel);
			}
			else
			{
				Widgets.Insert(atIndex, widgetModel);
			}

			return true;
		}

		public bool CanAddWidget(string widgetName)
		{
			return !Widgets.Any((item) => item.WidgetItemModel.WidgetName == widgetName);
		}

		public void RemoveWidgetAt(int index)
		{
			if (index < 0)
			{
				return;
			}

			Widgets[index].Dispose();
			Widgets.RemoveAt(index);
		}

		public void RemoveWidget<TWidget>() where TWidget : IWidgetItemModel
		{
			int indexToRemove = -1;

			for (int i = 0; i < Widgets.Count; i++)
			{
				if (typeof(TWidget).IsAssignableFrom(Widgets[i].WidgetControl.GetType()))
				{
					// Found matching types
					indexToRemove = i;
					break;
				}
			}

			RemoveWidgetAt(indexToRemove);
		}

		public void ReorderWidget(WidgetsListControlItemViewModel widgetModel, int place)
		{
			int widgetIndex = Widgets.IndexOf(widgetModel);
			Widgets.Move(widgetIndex, place);
		}

		public void Dispose()
		{
			YourHomeLoadedInvoked -= ViewModel_YourHomeLoadedInvoked;
			WidgetListRefreshRequestedInvoked -= ViewModel_WidgetListRefreshRequestedInvoked;
			AppInstance.ToolbarViewModel.RefreshRequested -= ToolbarViewModel_RefreshRequested;

			for (int i = 0; i < Widgets.Count; i++)
			{
				Widgets[i].Dispose();
			}

			Widgets.Clear();
		}
	}
}

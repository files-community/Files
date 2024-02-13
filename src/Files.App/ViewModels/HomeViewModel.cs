// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.UserControls.Widgets;
using Files.App.ViewModels.UserControls.Widgets;
using Microsoft.UI.Xaml;
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
		private IContentPageContext ContentPageContext { get; } = Ioc.Default.GetRequiredService<IContentPageContext>();

		// Properties

		public ObservableCollection<WidgetContainerItem> WidgetItems { get; } = [];

		public LayoutPreferencesManager LayoutPreferencesManager
			=> ContentPageContext.ShellPage?.InstanceViewModel.FolderSettings!;

		// Commands

		public ICommand HomePageLoadedCommand { get; }

		// Events
		public event EventHandler<RoutedEventArgs>? HomePageLoadedInvoked;
		public event EventHandler? WidgetListRefreshRequestedInvoked;

		// Constructor

		public HomeViewModel()
		{
			HomePageLoadedCommand = new RelayCommand<RoutedEventArgs>(ExecuteHomePageLoadedCommand);
		}

		// Methods

		public void ReloadWidgets()
		{
			var reloadQuickAccessWidget = CheckWidgetVisibility<QuickAccessWidgetViewModel>();
			var reloadDrivesWidget = CheckWidgetVisibility<DrivesWidgetViewModel>();
			var reloadFileTags = CheckWidgetVisibility<FileTagsWidgetViewModel>();
			var reloadRecentFiles = CheckWidgetVisibility<RecentFilesWidgetViewModel>();

			// Reload QuickAccess widget
			if (reloadQuickAccessWidget)
			{
				var quickAccessWidget = new QuickAccessWidget();

				InsertWidget(
					new(
						quickAccessWidget,
						quickAccessWidget.ViewModel,
						(value) => UserSettingsService.GeneralSettingsService.FoldersWidgetExpanded = value,
						() => UserSettingsService.GeneralSettingsService.FoldersWidgetExpanded),
					0);
			}

			// Reload DrivesWidget widget
			if (reloadDrivesWidget)
			{
				var drivesWidget = new DrivesWidget();

				InsertWidget(
					new(
						drivesWidget,
						drivesWidget.ViewModel,
						(value) => UserSettingsService.GeneralSettingsService.DrivesWidgetExpanded = value,
						() => UserSettingsService.GeneralSettingsService.DrivesWidgetExpanded),
					1);
			}

			// Reload FileTags widget
			if (reloadFileTags)
			{
				var fileTagsWidget = new FileTagsWidget();

				InsertWidget(
					new(
						fileTagsWidget,
						fileTagsWidget.ViewModel,
						(value) => UserSettingsService.GeneralSettingsService.FileTagsWidgetExpanded = value,
						() => UserSettingsService.GeneralSettingsService.FileTagsWidgetExpanded),
					2);
			}

			// Reload RecentFiles widget
			if (reloadRecentFiles)
			{
				var recentFilesWidget = new RecentFilesWidget();

				InsertWidget(
					new(
						recentFilesWidget,
						recentFilesWidget.ViewModel,
						(value) => UserSettingsService.GeneralSettingsService.RecentFilesWidgetExpanded = value,
						() => UserSettingsService.GeneralSettingsService.RecentFilesWidgetExpanded),
					3);
			}

			bool CheckWidgetVisibility<TWidget>()
			{
				return typeof(TWidget).Name switch
				{
					nameof(QuickAccessWidgetViewModel) => UserSettingsService.GeneralSettingsService.ShowQuickAccessWidget,
					nameof(DrivesWidgetViewModel) => UserSettingsService.GeneralSettingsService.ShowDrivesWidget,
					nameof(FileTagsWidgetViewModel) => UserSettingsService.GeneralSettingsService.ShowFileTagsWidget,
					nameof(RecentFilesWidgetViewModel) => UserSettingsService.GeneralSettingsService.ShowRecentFilesWidget,
					_ => false,
				};
			}
		}

		public void RefreshWidgetList()
		{
			for (int i = 0; i < WidgetItems.Count; i++)
			{
				if (!WidgetItems[i].WidgetItemModel.IsWidgetSettingEnabled)
				{
					RemoveWidgetAt(i);
				}
			}

			WidgetListRefreshRequestedInvoked?.Invoke(this, EventArgs.Empty);
		}

		public bool AddWidget(WidgetContainerItem widgetModel)
		{
			return InsertWidget(widgetModel, WidgetItems.Count + 1);
		}

		public bool InsertWidget(WidgetContainerItem widgetModel, int atIndex)
		{
			// The widget must not be null and must implement IWidgetItemModel
			if (widgetModel.WidgetItemModel is not IWidgetViewModel widgetItemModel)
			{
				return false;
			}

			// Don't add existing ones!
			if (!CanAddWidget(widgetItemModel.WidgetName))
			{
				return false;
			}

			if (atIndex > WidgetItems.Count)
			{
				WidgetItems.Add(widgetModel);
			}
			else
			{
				WidgetItems.Insert(atIndex, widgetModel);
			}

			return true;
		}

		public bool CanAddWidget(string widgetName)
		{
			return !(WidgetItems.Any((item) => item.WidgetItemModel.WidgetName == widgetName));
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

		public void ReorderWidget(WidgetContainerItem widgetModel, int place)
		{
			int widgetIndex = WidgetItems.IndexOf(widgetModel);
			WidgetItems.Move(widgetIndex, place);
		}

		// Command methods

		private void ExecuteHomePageLoadedCommand(RoutedEventArgs? e)
		{
			HomePageLoadedInvoked?.Invoke(this, e!);
		}

		// Disposer

		public void Dispose()
		{
			for (int i = 0; i < WidgetItems.Count; i++)
				WidgetItems[i].Dispose();

			WidgetItems.Clear();
		}
	}
}

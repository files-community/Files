// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Windows.Input;

namespace Files.App.ViewModels
{
	public sealed partial class HomeViewModel : ObservableObject, IDisposable
	{
		// Dependency injections

		private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();

		// Properties

		public ObservableCollection<WidgetContainerItem> WidgetItems { get; } = [];

		public bool ShowQuickAccessWidget
		{
			get => UserSettingsService.GeneralSettingsService.ShowQuickAccessWidget;
			set
			{
				if (value != UserSettingsService.GeneralSettingsService.ShowQuickAccessWidget)
					UserSettingsService.GeneralSettingsService.ShowQuickAccessWidget = value;
			}
		}

		public bool ShowDrivesWidget
		{
			get => UserSettingsService.GeneralSettingsService.ShowDrivesWidget;
			set
			{
				if (value != UserSettingsService.GeneralSettingsService.ShowDrivesWidget)
					UserSettingsService.GeneralSettingsService.ShowDrivesWidget = value;
			}
		}

		public bool ShowNetworkLocationsWidget
		{
			get => UserSettingsService.GeneralSettingsService.ShowNetworkLocationsWidget;
			set
			{
				if (value != UserSettingsService.GeneralSettingsService.ShowNetworkLocationsWidget)
					UserSettingsService.GeneralSettingsService.ShowNetworkLocationsWidget = value;
			}
		}

		public bool ShowFileTagsWidget
		{
			get => UserSettingsService.GeneralSettingsService.ShowFileTagsWidget;
			set
			{
				if (value != UserSettingsService.GeneralSettingsService.ShowFileTagsWidget)
					UserSettingsService.GeneralSettingsService.ShowFileTagsWidget = value;
			}
		}

		public bool ShowRecentFilesWidget
		{
			get => UserSettingsService.GeneralSettingsService.ShowRecentFilesWidget;
			set
			{
				if (value != UserSettingsService.GeneralSettingsService.ShowRecentFilesWidget)
					UserSettingsService.GeneralSettingsService.ShowRecentFilesWidget = value;
			}
		}

		// Commands

		public ICommand ReloadWidgetsCommand { get; }

		// Constructor

		public HomeViewModel()
		{
			ReloadWidgetsCommand = new AsyncRelayCommand(ExecuteReloadWidgetsCommand);

			UserSettingsService.GeneralSettingsService.PropertyChanged += GeneralSettingsService_PropertyChanged;
		}

		private void GeneralSettingsService_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IGeneralSettingsService.ShowQuickAccessWidget):
					OnPropertyChanged(nameof(ShowQuickAccessWidget));
					break;
				case nameof(IGeneralSettingsService.ShowDrivesWidget):
					OnPropertyChanged(nameof(ShowDrivesWidget));
					break;
				case nameof(IGeneralSettingsService.ShowNetworkLocationsWidget):
					OnPropertyChanged(nameof(ShowNetworkLocationsWidget));
					break;
				case nameof(IGeneralSettingsService.ShowFileTagsWidget):
					OnPropertyChanged(nameof(ShowFileTagsWidget));
					break;
				case nameof(IGeneralSettingsService.ShowRecentFilesWidget):
					OnPropertyChanged(nameof(ShowRecentFilesWidget));
					break;
			}
		}

		// Methods

		private void ReloadWidgets()
		{
			var reloadQuickAccessWidget = WidgetsHelpers.TryGetWidget<QuickAccessWidgetViewModel>(this);
			var reloadDrivesWidget = WidgetsHelpers.TryGetWidget<DrivesWidgetViewModel>(this);
			var reloadNetworkLocationsWidget = WidgetsHelpers.TryGetWidget<NetworkLocationsWidgetViewModel>(this);
			var reloadFileTagsWidget = WidgetsHelpers.TryGetWidget<FileTagsWidgetViewModel>(this);
			var reloadRecentFilesWidget = WidgetsHelpers.TryGetWidget<RecentFilesWidgetViewModel>(this);
			var insertIndex = 0;

			if (reloadQuickAccessWidget)
			{
				var quickAccessWidget = new QuickAccessWidget();

				InsertWidget(
					new(
						quickAccessWidget,
						quickAccessWidget.ViewModel,
						(value) => UserSettingsService.GeneralSettingsService.FoldersWidgetExpanded = value,
						() => UserSettingsService.GeneralSettingsService.FoldersWidgetExpanded),
					insertIndex++);
			}

			if (reloadDrivesWidget)
			{
				var drivesWidget = new DrivesWidget();

				InsertWidget(
					new(
						drivesWidget,
						drivesWidget.ViewModel,
						(value) => UserSettingsService.GeneralSettingsService.DrivesWidgetExpanded = value,
						() => UserSettingsService.GeneralSettingsService.DrivesWidgetExpanded),
					insertIndex++);
			}

			if (reloadNetworkLocationsWidget)
			{
				var networkLocationsWidget = new NetworkLocationsWidget();

				InsertWidget(
					new(
						networkLocationsWidget,
						networkLocationsWidget.ViewModel,
						(value) => UserSettingsService.GeneralSettingsService.NetworkLocationsWidgetExpanded = value,
						() => UserSettingsService.GeneralSettingsService.NetworkLocationsWidgetExpanded),
					insertIndex++);
			}

			if (reloadFileTagsWidget)
			{
				var fileTagsWidget = new FileTagsWidget();

				InsertWidget(
					new(
						fileTagsWidget,
						fileTagsWidget.ViewModel,
						(value) => UserSettingsService.GeneralSettingsService.FileTagsWidgetExpanded = value,
						() => UserSettingsService.GeneralSettingsService.FileTagsWidgetExpanded),
					insertIndex++);
			}

			if (reloadRecentFilesWidget)
			{
				var recentFilesWidget = new RecentFilesWidget();

				InsertWidget(
					new(
						recentFilesWidget,
						recentFilesWidget.ViewModel,
						(value) => UserSettingsService.GeneralSettingsService.RecentFilesWidgetExpanded = value,
						() => UserSettingsService.GeneralSettingsService.RecentFilesWidgetExpanded),
					insertIndex++);
			}
		}

		public void RefreshWidgetList()
		{
			for (int i = WidgetItems.Count - 1; i >= 0; i--)
			{
				if (!WidgetItems[i].WidgetItemModel.IsWidgetSettingEnabled)
					RemoveWidgetAt(i);
			}

			ReloadWidgets();
		}

		public async Task RefreshWidgetProperties()
		{
			await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(async () =>
			{
				foreach (var viewModel in WidgetItems.Select(x => x.WidgetItemModel).ToList())
					await viewModel.RefreshWidgetAsync();
			});
		}

		private bool InsertWidget(WidgetContainerItem widgetModel, int atIndex)
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
				MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(() =>
				{
					WidgetItems.Add(widgetModel);
				});
			}
			else
			{
				MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(() =>
				{
					WidgetItems.Insert(atIndex, widgetModel);
				});
			}

			return true;
		}

		public bool CanAddWidget(string widgetName)
		{
			return MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(() =>
			{
				return !(WidgetItems.Any((item) => item.WidgetItemModel.WidgetName == widgetName));
			}).GetAwaiter().GetResult();
		}

		private void RemoveWidgetAt(int index)
		{
			if (index < 0)
			{
				return;
			}

			MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(() =>
			{
				WidgetItems[index].Dispose();
				WidgetItems.RemoveAt(index);
			});
		}

		public void RemoveWidget<TWidget>() where TWidget : IWidgetViewModel
		{
			MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(() =>
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

				if (indexToRemove >= 0)
				{
					WidgetItems[indexToRemove].Dispose();
					WidgetItems.RemoveAt(indexToRemove);
				}
			});
		}

		// Command methods

		private async Task ExecuteReloadWidgetsCommand()
		{
			ReloadWidgets();
			await RefreshWidgetProperties();
		}

		// Disposer

		public void Dispose()
		{
			UserSettingsService.GeneralSettingsService.PropertyChanged -= GeneralSettingsService_PropertyChanged;

			MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(() =>
			{
				for (int i = 0; i < WidgetItems.Count; i++)
					WidgetItems[i].Dispose();

				WidgetItems.Clear();
			});
		}
	}
}

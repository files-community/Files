// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Controls;
using System.Windows.Input;
using Windows.Storage;

namespace Files.App.ViewModels.UserControls.Widgets
{
	public sealed class FileTagsWidgetViewModel : BaseWidgetViewModel, IWidgetViewModel
	{
		// Dependency injections

		private IFileTagsService FileTagsService { get; } = Ioc.Default.GetRequiredService<IFileTagsService>();

		// Properties

		public ObservableCollection<WidgetFileTagsContainerItem> Containers { get; } = [];

		public string WidgetName => nameof(FileTagsWidgetViewModel);
		public string WidgetHeader => "FileTags".GetLocalizedResource();
		public string AutomationProperties => "FileTags".GetLocalizedResource();
		public bool IsWidgetSettingEnabled => UserSettingsService.GeneralSettingsService.ShowFileTagsWidget;
		public bool ShowMenuFlyout => false;
		public MenuFlyoutItem? MenuFlyoutItem { get; } = null;

		// Events

		public static event EventHandler<IEnumerable<WidgetFileTagCardItem>>? SelectedTaggedItemsChanged;

		// Commands

		private ICommand OpenInNewPaneCommand;

		// Constructor

		public FileTagsWidgetViewModel()
		{
			_ = InitializeWidget();

			OpenInNewTabCommand = new AsyncRelayCommand<WidgetCardItem>(OpenInNewTabAsync);
			OpenInNewWindowCommand = new AsyncRelayCommand<WidgetCardItem>(OpenInNewWindowAsync);
			OpenFileLocationCommand = new RelayCommand<WidgetCardItem>(OpenFileLocation);
			OpenInNewPaneCommand = new RelayCommand<WidgetCardItem>(OpenInNewPane);
			PinToFavoritesCommand = new AsyncRelayCommand<WidgetCardItem>(PinToFavoritesAsync);
			UnpinFromFavoritesCommand = new AsyncRelayCommand<WidgetCardItem>(UnpinFromFavoritesAsync);
			OpenPropertiesCommand = new RelayCommand<WidgetCardItem>(OpenProperties);
		}

		// Methods

		public Task RefreshWidgetAsync()
		{
			return Task.CompletedTask;
		}

		public async Task InitializeWidget()
		{
			Containers.Clear();

			await foreach (var item in FileTagsService.GetTagsAsync())
			{
				var container = new WidgetFileTagsContainerItem()
				{
					TagId = item.Uid,
					Name = item.Name,
					Color = item.Color
				};

				Containers.Add(container);

				_ = container.InitializeAsync();
			}
		}

		public override List<ContextMenuFlyoutItemViewModel> GenerateContextFlyoutModel(bool isFolder = false)
		{
			return WidgetFileTagsItemContextFlyoutFactory.Generate(isFolder);
		}

		// Command methods

		private void OpenProperties(WidgetCardItem? item)
		{
			if (!HomePageContext.IsAnyItemRightClicked || item is null)
				return;

			var flyout = HomePageContext.ItemContextFlyoutMenu;
			EventHandler<object> flyoutClosed = null!;

			flyoutClosed = (s, e) =>
			{
				flyout!.Closed -= flyoutClosed;

				ListedItem listedItem = new(null!)
				{
					ItemPath = (item.Item as WidgetFileTagCardItem)?.Path ?? string.Empty,
					ItemNameRaw = (item.Item as WidgetFileTagCardItem)?.Name ?? string.Empty,
					PrimaryItemAttribute = StorageItemTypes.Folder,
					ItemType = "Folder".GetLocalizedResource(),
				};

				FilePropertiesHelpers.OpenPropertiesWindow(listedItem, ContentPageContext.ShellPage!);
			};

			flyout!.Closed += flyoutClosed;
		}

		private void OpenInNewPane(WidgetCardItem? item)
		{
			ContentPageContext.ShellPage!.PaneHolder?.OpenPathInNewPane(item?.Path ?? string.Empty);
		}

		private void OpenFileLocation(WidgetCardItem? item)
		{
			var path = SystemIO.Directory.GetParent(item?.Path ?? string.Empty)?.FullName ?? string.Empty;

			ContentPageContext.ShellPage!.NavigateWithArguments(
				ContentPageContext.ShellPage.InstanceViewModel.FolderSettings.GetLayoutType(path),
				new()
				{
					NavPathParam = path,
					SelectItems = new[] { SystemIO.Path.GetFileName(item?.Path ?? string.Empty) },
					AssociatedTabInstance = ContentPageContext.ShellPage!
				});
		}

		// Disposer

		public void Dispose()
		{
		}
	}
}

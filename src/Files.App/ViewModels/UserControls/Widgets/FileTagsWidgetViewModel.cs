// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Controls;
using System.IO;
using Windows.Storage;

namespace Files.App.ViewModels.UserControls.Widgets
{
	/// <summary>
	/// Represents view model of <see cref="FileTagsWidget"/>.
	/// </summary>
	public sealed partial class FileTagsWidgetViewModel : BaseWidgetViewModel, IWidgetViewModel
	{
		private CancellationTokenSource _updateCTS;

		// Properties

		public ObservableCollection<WidgetFileTagsContainerItem> Containers { get; } = [];

		public string WidgetName => nameof(FileTagsWidget);
		public string WidgetHeader => Strings.FileTags.GetLocalizedResource();
		public string AutomationProperties => Strings.FileTags.GetLocalizedResource();
		public bool IsWidgetSettingEnabled => UserSettingsService.GeneralSettingsService.ShowFileTagsWidget;
		public bool ShowMenuFlyout => false;
		public MenuFlyoutItem? MenuFlyoutItem => null;

		// Events

		public static event EventHandler<IEnumerable<WidgetFileTagCardItem>>? SelectedTaggedItemsChanged;


		// Constructor

		public FileTagsWidgetViewModel()
		{
			_ = InitializeWidget();

			FileTagsSettingsService.OnTagsUpdated += FileTagsSettingsService_OnTagsUpdated;

			PinToSidebarCommand = new AsyncRelayCommand<WidgetCardItem>(ExecutePinToSidebarCommand);
			UnpinFromSidebarCommand = new AsyncRelayCommand<WidgetCardItem>(ExecuteUnpinFromSidebarCommand);
			OpenFileLocationCommand = new RelayCommand<WidgetCardItem>(ExecuteOpenFileLocationCommand);
			OpenPropertiesCommand = new RelayCommand<WidgetCardItem>(ExecuteOpenPropertiesCommand);
		}

		// Methods

		public async Task InitializeWidget()
		{
			await foreach (var item in FileTagsService.GetTagsAsync())
			{
				CreateTagContainerItem(item);
			}
		}

		public async Task RefreshWidgetAsync()
		{
			_updateCTS?.Cancel();
			_updateCTS = new CancellationTokenSource();
			await foreach (var item in FileTagsService.GetTagsAsync())
			{
				if (_updateCTS.IsCancellationRequested)
					break;

				var matchingItem = Containers.First(c => c.Uid == item.Uid);
				if (matchingItem is null)
				{
					CreateTagContainerItem(item);
				}
				else
				{
					matchingItem.Name = item.Name;
					matchingItem.Color = item.Color;
					matchingItem.Tags.Clear();
					_ = matchingItem.InitAsync(_updateCTS.Token);
				}
			}
		}

		private void CreateTagContainerItem(TagViewModel tag)
		{
			var container = new WidgetFileTagsContainerItem(tag.Uid)
			{
				Name = tag.Name,
				Color = tag.Color
			};

			Containers.Add(container);
			_ = container.InitAsync();
		}

		public override List<ContextMenuFlyoutItemViewModel> GetItemMenuItems(WidgetCardItem item, bool isPinned, bool isFolder = false)
		{
			return new List<ContextMenuFlyoutItemViewModel>()
			{
				new ContextMenuFlyoutItemViewModelBuilder(CommandManager.OpenInNewTabFromHomeAction)
				{
					IsVisible = UserSettingsService.GeneralSettingsService.ShowOpenInNewTab && CommandManager.OpenInNewTabFromHomeAction.IsExecutable
				}.Build(),
				new ContextMenuFlyoutItemViewModelBuilder(CommandManager.OpenInNewWindowFromHomeAction)
				{
					IsVisible = UserSettingsService.GeneralSettingsService.ShowOpenInNewWindow && CommandManager.OpenInNewWindowFromHomeAction.IsExecutable
				}.Build(),
				new ContextMenuFlyoutItemViewModelBuilder(CommandManager.OpenInNewPaneFromHomeAction)
				{
					IsVisible = UserSettingsService.GeneralSettingsService.ShowOpenInNewPane && CommandManager.OpenInNewPaneFromHomeAction.IsExecutable
				}.Build(),
				new()
				{
					Text = Strings.OpenWith.GetLocalizedResource(),
					ThemedIconModel = new() { ThemedIconStyle = "App.ThemedIcons.OpenWith" },
					Tag = "OpenWithPlaceholder",
					ShowItem = !isFolder
				},
				new()
				{
					Text = Strings.OpenFileLocation.GetLocalizedResource(),
					Glyph = "\uED25",
					Command = OpenFileLocationCommand,
					CommandParameter = item,
					ShowItem = !isFolder
				},
				new()
				{
					Text = Strings.PinFolderToSidebar.GetLocalizedResource(),
					ThemedIconModel = new() { ThemedIconStyle = "App.ThemedIcons.FavoritePin" },
					Command = PinToSidebarCommand,
					CommandParameter = item,
					ShowItem = !isPinned && isFolder
				},
				new()
				{
					Text = Strings.UnpinFolderFromSidebar.GetLocalizedResource(),
					ThemedIconModel = new() { ThemedIconStyle = "App.ThemedIcons.FavoritePinRemove" },
					Command = UnpinFromSidebarCommand,
					CommandParameter = item,
					ShowItem = isPinned && isFolder
				},
				new()
				{
					Text = Strings.SendTo.GetLocalizedResource(),
					Tag = "SendToPlaceholder",
					ShowItem = UserSettingsService.GeneralSettingsService.ShowSendToMenu
				},
				new()
				{
					Text = Strings.Properties.GetLocalizedResource(),
					ThemedIconModel = new() { ThemedIconStyle = "App.ThemedIcons.Properties" },
					Command = OpenPropertiesCommand,
					CommandParameter = item,
					ShowItem = isFolder
				},
				new ContextMenuFlyoutItemViewModel()
				{
					ItemType = ContextMenuFlyoutItemType.Separator,
					ShowItem = CommandManager.OpenTerminalFromHome.IsExecutable
				},
				new ContextMenuFlyoutItemViewModelBuilder(CommandManager.OpenTerminalFromHome).Build(),
				new()
				{
					ItemType = ContextMenuFlyoutItemType.Separator,
					Tag = "OverflowSeparator",
				},
				new()
				{
					Text = Strings.Loading.GetLocalizedResource(),
					Glyph = "\xE712",
					Items = [],
					ID = "ItemOverflow",
					Tag = "ItemOverflow",
					IsEnabled = false,
				}
			}.Where(x => x.ShowItem).ToList();
		}

		// Command methods

		private void ExecuteOpenPropertiesCommand(WidgetCardItem? item)
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
					ItemType = Strings.Folder.GetLocalizedResource(),
				};

				FilePropertiesHelpers.OpenPropertiesWindow(listedItem, ContentPageContext.ShellPage!);
			};

			flyout!.Closed += flyoutClosed;
		}

		private void ExecuteOpenFileLocationCommand(WidgetCardItem? item)
		{
			var itemPath = Directory.GetParent(item?.Path ?? string.Empty)?.FullName ?? string.Empty;
			var itemName = Path.GetFileName(item?.Path ?? string.Empty);

			ContentPageContext.ShellPage!.NavigateWithArguments(
				ContentPageContext.ShellPage!.InstanceViewModel.FolderSettings.GetLayoutType(itemPath),
				new NavigationArguments()
				{
					NavPathParam = itemPath,
					SelectItems = new[] { itemName },
					AssociatedTabInstance = ContentPageContext.ShellPage!
				});
		}

		private async void FileTagsSettingsService_OnTagsUpdated(object? sender, EventArgs e)
		{
			await RefreshWidgetAsync();
		}

		// Disposer

		public void Dispose()
		{
			FileTagsSettingsService.OnTagsUpdated -= FileTagsSettingsService_OnTagsUpdated;
		}
	}
}

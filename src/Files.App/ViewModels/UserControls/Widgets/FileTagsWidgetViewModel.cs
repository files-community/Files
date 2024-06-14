﻿// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Controls;
using System.IO;
using System.Windows.Input;
using Windows.Storage;

namespace Files.App.ViewModels.UserControls.Widgets
{
	/// <summary>
	/// Represents view model of <see cref="FileTagsWidget"/>.
	/// </summary>
	public sealed partial class FileTagsWidgetViewModel : BaseWidgetViewModel, IWidgetViewModel
	{
		// Properties

		public ObservableCollection<WidgetFileTagsContainerItem> Containers { get; } = [];

		public string WidgetName => nameof(FileTagsWidget);
		public string WidgetHeader => "FileTags".GetLocalizedResource();
		public string AutomationProperties => "FileTags".GetLocalizedResource();
		public bool IsWidgetSettingEnabled => UserSettingsService.GeneralSettingsService.ShowFileTagsWidget;
		public bool ShowMenuFlyout => false;
		public MenuFlyoutItem? MenuFlyoutItem => null;

		// Events

		public static event EventHandler<IEnumerable<WidgetFileTagCardItem>>? SelectedTaggedItemsChanged;

		// Commands

		private ICommand OpenInNewPaneCommand { get; set; } = null!;

		// Constructor

		public FileTagsWidgetViewModel()
		{
			_ = InitializeWidget();

			OpenInNewTabCommand = new AsyncRelayCommand<WidgetCardItem>(ExecuteOpenInNewTabCommand);
			OpenInNewWindowCommand = new AsyncRelayCommand<WidgetCardItem>(ExecuteOpenInNewWindowCommand);
			PinToSidebarCommand = new AsyncRelayCommand<WidgetCardItem>(ExecutePinToSidebarCommand);
			UnpinFromSidebarCommand = new AsyncRelayCommand<WidgetCardItem>(ExecuteUnpinFromSidebarCommand);
			OpenFileLocationCommand = new RelayCommand<WidgetCardItem>(ExecuteOpenFileLocationCommand);
			OpenInNewPaneCommand = new RelayCommand<WidgetCardItem>(ExecuteOpenInNewPaneCommand);
			OpenPropertiesCommand = new RelayCommand<WidgetCardItem>(ExecuteOpenPropertiesCommand);
		}

		// Methods

		public async Task InitializeWidget()
		{
			await foreach (var item in FileTagsService.GetTagsAsync())
			{
				var container = new WidgetFileTagsContainerItem(item.Uid)
				{
					Name = item.Name,
					Color = item.Color
				};

				Containers.Add(container);
				_ = container.InitAsync();
			}
		}

		public Task RefreshWidgetAsync()
		{
			return Task.CompletedTask;
		}

		public override List<ContextMenuFlyoutItemViewModel> GetItemMenuItems(WidgetCardItem item, bool isPinned, bool isFolder = false)
		{
			return new List<ContextMenuFlyoutItemViewModel>()
			{
				new ContextMenuFlyoutItemViewModelBuilder(CommandManager.OpenInNewTabFromHomeAction).Build(),
				new ContextMenuFlyoutItemViewModelBuilder(CommandManager.OpenInNewWindowFromHomeAction).Build(),
				new ContextMenuFlyoutItemViewModelBuilder(CommandManager.OpenInNewPaneFromHomeAction).Build(),
				new()
				{
					Text = "OpenWith".GetLocalizedResource(),
					OpacityIcon = new() { OpacityIconStyle = "ColorIconOpenWith" },
					Tag = "OpenWithPlaceholder",
					ShowItem = !isFolder
				},
				new()
				{
					Text = "OpenFileLocation".GetLocalizedResource(),
					Glyph = "\uED25",
					Command = OpenFileLocationCommand,
					CommandParameter = item,
					ShowItem = !isFolder
				},
				new()
				{
					Text = "PinFolderToSidebar".GetLocalizedResource(),
					OpacityIcon = new() { OpacityIconStyle = "Icons.Pin.16x16" },
					Command = PinToSidebarCommand,
					CommandParameter = item,
					ShowItem = !isPinned && isFolder
				},
				new()
				{
					Text = "UnpinFolderFromSidebar".GetLocalizedResource(),
					OpacityIcon = new() { OpacityIconStyle = "Icons.Unpin.16x16" },
					Command = UnpinFromSidebarCommand,
					CommandParameter = item,
					ShowItem = isPinned && isFolder
				},
				new()
				{
					Text = "SendTo".GetLocalizedResource(),
					Tag = "SendToPlaceholder",
					ShowItem = UserSettingsService.GeneralSettingsService.ShowSendToMenu
				},
				new()
				{
					Text = "Properties".GetLocalizedResource(),
					OpacityIcon = new() { OpacityIconStyle = "ColorIconProperties" },
					Command = OpenPropertiesCommand,
					CommandParameter = item,
					ShowItem = isFolder
				},
				new()
				{
					ItemType = ContextMenuFlyoutItemType.Separator,
					Tag = "OverflowSeparator",
				},
				new()
				{
					Text = "Loading".GetLocalizedResource(),
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
					ItemType = "Folder".GetLocalizedResource(),
				};

				FilePropertiesHelpers.OpenPropertiesWindow(listedItem, ContentPageContext.ShellPage!);
			};

			flyout!.Closed += flyoutClosed;
		}

		private void ExecuteOpenInNewPaneCommand(WidgetCardItem? item)
		{
			ContentPageContext.ShellPage!.PaneHolder?.OpenSecondaryPane(item?.Path ?? string.Empty);
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

		// Disposer

		public void Dispose()
		{
		}
	}
}

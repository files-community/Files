// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.UserControls.Widgets;
using Microsoft.UI.Xaml.Controls;
using System.IO;
using System.Windows.Input;
using Windows.Storage;

namespace Files.App.UserControls.ViewModels.Widgets
{
	public sealed class FileTagsWidgetViewModel : BaseWidgetViewModel, IWidgetViewModel, INotifyPropertyChanged
	{
		// Dependency injections

		private IFileTagsService FileTagsService { get; } = Ioc.Default.GetRequiredService<IFileTagsService>();

		// Properties

		public ObservableCollection<WidgetFileTagsContainerItem> Containers { get; } = [];

		public string WidgetName => nameof(FileTagsWidget);
		public string WidgetHeader => "FileTags".GetLocalizedResource();
		public string AutomationProperties => "FileTags".GetLocalizedResource();
		public bool IsWidgetSettingEnabled => UserSettingsService.GeneralSettingsService.ShowFileTagsWidget;
		public bool ShowMenuFlyout => false;
		public MenuFlyoutItem? MenuFlyoutItem { get; } = null;

		// Events

		public delegate void FileTagsOpenLocationInvokedEventHandler(object sender, PathNavigationEventArgs e);
		public delegate void FileTagsNewPaneInvokedEventHandler(object sender, QuickAccessCardInvokedEventArgs e);
		public static event EventHandler<IEnumerable<WidgetFileTagCardItem>>? SelectedTaggedItemsChanged;
		public event FileTagsOpenLocationInvokedEventHandler? FileTagsOpenLocationInvoked;
		public event FileTagsNewPaneInvokedEventHandler? FileTagsNewPaneInvoked;
		public event PropertyChangedEventHandler? PropertyChanged;

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
				var container = new WidgetFileTagsContainerItem(item.Uid)
				{
					Name = item.Name,
					Color = item.Color
				};

				Containers.Add(container);

				_ = container.InitAsync();
			}
		}

		public override List<ContextMenuFlyoutItemViewModel> GetItemMenuItems(WidgetCardItem item, bool isPinned, bool isFolder = false)
		{
			return new List<ContextMenuFlyoutItemViewModel>()
			{
				new()
				{
					Text = "OpenWith".GetLocalizedResource(),
					OpacityIcon = new() { OpacityIconStyle = "ColorIconOpenWith" },
					Tag = "OpenWithPlaceholder",
					ShowItem = !isFolder
				},
				new()
				{
					Text = "SendTo".GetLocalizedResource(),
					Tag = "SendToPlaceholder",
					ShowItem = !isFolder && UserSettingsService.GeneralSettingsService.ShowSendToMenu
				},
				new()
				{
					Text = "OpenInNewTab".GetLocalizedResource(),
					OpacityIcon = new() { OpacityIconStyle = "ColorIconOpenInNewTab", },
					Command = OpenInNewTabCommand,
					CommandParameter = item,
					ShowItem = isFolder
				},
				new()
				{
					Text = "OpenInNewWindow".GetLocalizedResource(),
					OpacityIcon = new() { OpacityIconStyle = "ColorIconOpenInNewWindow" },
					Command = OpenInNewWindowCommand,
					CommandParameter = item,
					ShowItem = isFolder
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
					Text = "OpenInNewPane".GetLocalizedResource(),
					Command = OpenInNewPaneCommand,
					CommandParameter = item,
					ShowItem = UserSettingsService.GeneralSettingsService.ShowOpenInNewPane && isFolder
				},
				new()
				{
					Text = "PinToFavorites".GetLocalizedResource(),
					OpacityIcon = new() { OpacityIconStyle = "ColorIconPinToFavorites" },
					Command = PinToFavoritesCommand,
					CommandParameter = item,
					ShowItem = !isPinned && isFolder
				},
				new()
				{
					Text = "UnpinFromFavorites".GetLocalizedResource(),
					OpacityIcon = new() { OpacityIconStyle = "ColorIconUnpinFromFavorites" },
					Command = UnpinFromFavoritesCommand,
					CommandParameter = item,
					ShowItem = isPinned && isFolder
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
			FileTagsNewPaneInvoked?.Invoke(this, new() { Path = item?.Path ?? string.Empty });
		}

		private void OpenFileLocation(WidgetCardItem? item)
		{
			FileTagsOpenLocationInvoked?.Invoke(this, new PathNavigationEventArgs()
			{
				ItemPath = Directory.GetParent(item?.Path ?? string.Empty)?.FullName ?? string.Empty,
				ItemName = Path.GetFileName(item?.Path ?? string.Empty),
			});
		}

		// Disposer

		public void Dispose()
		{
		}
	}
}

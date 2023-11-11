// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.ViewModels.Widgets;
using Files.Shared.Utils;
using Files.App.Helpers.ContextFlyouts;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Windows.Storage;

namespace Files.App.ViewModels.UserControls.Widgets
{
	public sealed partial class FileTagsWidgetViewModel : BaseWidgetViewModel, IWidgetViewModel, INotifyPropertyChanged, IAsyncInitialize
	{
		private IFileTagsService FileTagsService { get; } = Ioc.Default.GetRequiredService<IFileTagsService>();
		private readonly IUserSettingsService userSettingsService;

		private readonly Func<string, Task> _openAction;

		public IShellPage AppInstance;

		public ObservableCollection<FileTagsContainerViewModel> Containers { get; }

		public Func<string, Task>? OpenAction { get; set; }

		public string WidgetName => "FileTags";
		public string WidgetHeader => "FileTags".GetLocalizedResource();
		public string AutomationProperties => "FileTags".GetLocalizedResource();
		public bool IsWidgetSettingEnabled => UserSettingsService.GeneralSettingsService.ShowFileTagsWidget;
		public bool ShowMenuFlyout => false;

		public delegate void FileTagsOpenLocationInvokedEventHandler(object sender, PathNavigationEventArgs e);
		public delegate void FileTagsNewPaneInvokedEventHandler(object sender, QuickAccessCardInvokedEventArgs e);
		public event FileTagsOpenLocationInvokedEventHandler FileTagsOpenLocationInvoked;
		public event FileTagsNewPaneInvokedEventHandler FileTagsNewPaneInvoked;
		public event PropertyChangedEventHandler? PropertyChanged;

		public MenuFlyoutItem? MenuFlyoutItem => null;

		private ICommand OpenInNewPaneCommand;

		public FileTagsWidgetViewModel(Func<string, Task> openAction)
		{
			_openAction = openAction;
			Containers = new();
		}

		public FileTagsWidgetViewModel()
		{
			userSettingsService = Ioc.Default.GetRequiredService<IUserSettingsService>();

			// Second function is layered on top to ensure that OpenPath function is late initialized and a null reference is not passed-in
			// See FileTagItemViewModel._openAction for more information

			OpenInNewTabCommand = new AsyncRelayCommand<WidgetCardItem>(OpenInNewTabAsync);
			OpenInNewWindowCommand = new AsyncRelayCommand<WidgetCardItem>(OpenInNewWindowAsync);
			OpenFileLocationCommand = new RelayCommand<WidgetCardItem>(OpenFileLocation);
			OpenInNewPaneCommand = new RelayCommand<WidgetCardItem>(OpenInNewPane);
			PinToFavoritesCommand = new AsyncRelayCommand<WidgetCardItem>(PinToFavoritesAsync);
			UnpinFromFavoritesCommand = new AsyncRelayCommand<WidgetCardItem>(UnpinFromFavoritesAsync);
			OpenPropertiesCommand = new RelayCommand<WidgetCardItem>(OpenProperties);
		}

		/// <inheritdoc/>
		public async Task InitAsync(CancellationToken cancellationToken = default)
		{
			await foreach (var item in FileTagsService.GetTagsAsync(cancellationToken))
			{
				var container = new FileTagsContainerViewModel(item.Uid, _openAction)
				{
					Name = item.Name,
					Color = item.Color
				};
				Containers.Add(container);

				_ = container.InitAsync(cancellationToken);
			}
		}

		private void OpenProperties(WidgetCardItem? item)
		{
			EventHandler<object> flyoutClosed = null!;
			flyoutClosed = (s, e) =>
			{
				ItemContextMenuFlyout.Closed -= flyoutClosed;
				ListedItem listedItem = new(null!)
				{
					ItemPath = (item.Item as FileTagsItemViewModel)?.Path ?? string.Empty,
					ItemNameRaw = (item.Item as FileTagsItemViewModel)?.Name ?? string.Empty,
					PrimaryItemAttribute = StorageItemTypes.Folder,
					ItemType = "Folder".GetLocalizedResource(),
				};
				FilePropertiesHelpers.OpenPropertiesWindow(listedItem, AppInstance);
			};
			ItemContextMenuFlyout.Closed += flyoutClosed;
		}

		private void OpenInNewPane(WidgetCardItem? item)
		{
			FileTagsNewPaneInvoked?.Invoke(this, new QuickAccessCardInvokedEventArgs()
			{
				Path = item?.Path ?? string.Empty
			});
		}

		private async void FileTagItem_ItemClick(object sender, ItemClickEventArgs e)
		{
			if (e.ClickedItem is FileTagsItemViewModel itemViewModel)
				await itemViewModel.ClickCommand.ExecuteAsync(null);
		}

		private void AdaptiveGridView_RightTapped(object sender, RightTappedRoutedEventArgs e)
		{
			if (e.OriginalSource is not FrameworkElement element ||
				element.DataContext is not FileTagsItemViewModel item)
			{
				return;
			}

			LoadContextMenu(
				element,
				e,
				GetItemMenuItems(item, QuickAccessService.IsItemPinned(item.Path), item.IsFolder),
				rightClickedItem: item);
		}

		private void LoadContextMenu(
			FrameworkElement element,
			RightTappedRoutedEventArgs e,
			List<ContextMenuFlyoutItemViewModel> menuItems,
			FileTagsItemViewModel? rightClickedItem = null)
		{
			var itemContextMenuFlyout = new CommandBarFlyout { Placement = FlyoutPlacementMode.Full };
			itemContextMenuFlyout.Opening += (sender, e) => App.LastOpenedFlyout = sender as CommandBarFlyout;

			var (_, secondaryElements) = ItemModelListToContextFlyoutHelper.GetAppBarItemsFromModel(menuItems);

			if (!UserSettingsService.GeneralSettingsService.MoveShellExtensionsToSubMenu)
				secondaryElements.OfType<FrameworkElement>()
								 .ForEach(i => i.MinWidth = Constants.UI.ContextMenuItemsMaxWidth); // Set menu min width if the overflow menu setting is disabled

			secondaryElements.ForEach(i => itemContextMenuFlyout.SecondaryCommands.Add(i));
			ItemContextMenuFlyout = itemContextMenuFlyout;
			if (rightClickedItem is not null)
			{
				FlyouItemPath = rightClickedItem.Path;
				ItemContextMenuFlyout.Opened += ItemContextMenuFlyout_Opened;
			}
			itemContextMenuFlyout.ShowAt(element, new FlyoutShowOptions { Position = e.GetPosition(element) });

			e.Handled = true;
		}

		private async void ItemContextMenuFlyout_Opened(object? sender, object e)
		{
			ItemContextMenuFlyout.Opened -= ItemContextMenuFlyout_Opened;
			await ShellContextmenuHelper.LoadShellMenuItemsAsync(FlyouItemPath, ItemContextMenuFlyout, showOpenWithMenu: true, showSendToMenu: true);
		}

		public override List<ContextMenuFlyoutItemViewModel> GetItemMenuItems(WidgetCardItem item, bool isPinned, bool isFolder = false)
		{
			return new List<ContextMenuFlyoutItemViewModel>()
			{
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "OpenWith".GetLocalizedResource(),
					OpacityIcon = new OpacityIconModel()
					{
						OpacityIconStyle = "ColorIconOpenWith",
					},
					Tag = "OpenWithPlaceholder",
					ShowItem = !isFolder
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "SendTo".GetLocalizedResource(),
					Tag = "SendToPlaceholder",
					ShowItem = !isFolder && userSettingsService.GeneralSettingsService.ShowSendToMenu
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "OpenInNewTab".GetLocalizedResource(),
					OpacityIcon = new OpacityIconModel()
					{
						OpacityIconStyle = "ColorIconOpenInNewTab",
					},
					Command = OpenInNewTabCommand,
					CommandParameter = item,
					ShowItem = isFolder
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "OpenInNewWindow".GetLocalizedResource(),
					OpacityIcon = new OpacityIconModel()
					{
						OpacityIconStyle = "ColorIconOpenInNewWindow",
					},
					Command = OpenInNewWindowCommand,
					CommandParameter = item,
					ShowItem = isFolder
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "OpenFileLocation".GetLocalizedResource(),
					Glyph = "\uED25",
					Command = OpenFileLocationCommand,
					CommandParameter = item,
					ShowItem = !isFolder
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "OpenInNewPane".GetLocalizedResource(),
					Command = OpenInNewPaneCommand,
					CommandParameter = item,
					ShowItem = userSettingsService.GeneralSettingsService.ShowOpenInNewPane && isFolder
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "PinToFavorites".GetLocalizedResource(),
					OpacityIcon = new OpacityIconModel()
					{
						OpacityIconStyle = "ColorIconPinToFavorites",
					},
					Command = PinToFavoritesCommand,
					CommandParameter = item,
					ShowItem = !isPinned && isFolder
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "UnpinFromFavorites".GetLocalizedResource(),
					OpacityIcon = new OpacityIconModel()
					{
						OpacityIconStyle = "ColorIconUnpinFromFavorites",
					},
					Command = UnpinFromFavoritesCommand,
					CommandParameter = item,
					ShowItem = isPinned && isFolder
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "Properties".GetLocalizedResource(),
					OpacityIcon = new OpacityIconModel()
					{
						OpacityIconStyle = "ColorIconProperties",
					},
					Command = OpenPropertiesCommand,
					CommandParameter = item,
					ShowItem = isFolder
				},
				new ContextMenuFlyoutItemViewModel()
				{
					ItemType = ContextMenuFlyoutItemType.Separator,
					Tag = "OverflowSeparator",
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "Loading".GetLocalizedResource(),
					Glyph = "\xE712",
					Items = new List<ContextMenuFlyoutItemViewModel>(),
					ID = "ItemOverflow",
					Tag = "ItemOverflow",
					IsEnabled = false,
				}
			}.Where(x => x.ShowItem).ToList();
		}

		public void OpenFileLocation(WidgetCardItem? item)
		{
			FileTagsOpenLocationInvoked?.Invoke(this, new PathNavigationEventArgs()
			{
				ItemPath = Directory.GetParent(item?.Path ?? string.Empty)?.FullName ?? string.Empty,
				ItemName = Path.GetFileName(item?.Path ?? string.Empty),
			});
		}

		public Task RefreshWidgetAsync()
		{
			return Task.CompletedTask;
		}

		private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		public void Dispose()
		{
		}
	}
}

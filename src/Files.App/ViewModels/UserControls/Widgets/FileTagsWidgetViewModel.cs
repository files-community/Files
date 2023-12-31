// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

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
	public sealed partial class FileTagsWidgetViewModel : BaseWidgetViewModel, IWidgetViewModel, INotifyPropertyChanged
	{
		// Properties

		public ObservableCollection<WidgetFileTagsContainerItem> Containers { get; }

		private IShellPage? _AppInstance;
		public IShellPage? AppInstance
		{
			get => _AppInstance;
			set
			{
				if (value != _AppInstance)
				{
					_AppInstance = value;
					NotifyPropertyChanged(nameof(AppInstance));
				}
			}
		}

		public Func<string, Task>? OpenAction { get; set; }

		public string WidgetName => nameof(FileTagsWidgetViewModel);
		public string WidgetHeader => "FileTags".GetLocalizedResource();
		public string AutomationProperties => "FileTags".GetLocalizedResource();
		public bool IsWidgetSettingEnabled => UserSettingsService.GeneralSettingsService.ShowFileTagsWidget;
		public bool ShowMenuFlyout => false;
		public MenuFlyoutItem? MenuFlyoutItem => null;

		// Events

		public delegate void FileTagsOpenLocationInvokedEventHandler(object sender, PathNavigationEventArgs e);
		public delegate void FileTagsNewPaneInvokedEventHandler(object sender, QuickAccessCardInvokedEventArgs e);
		public static event EventHandler<IEnumerable<WidgetFileTagsItem>>? SelectedTaggedItemsChanged;
		public event FileTagsOpenLocationInvokedEventHandler? FileTagsOpenLocationInvoked;
		public event FileTagsNewPaneInvokedEventHandler? FileTagsNewPaneInvoked;
		public event PropertyChangedEventHandler? PropertyChanged;

		// Commands

		private ICommand OpenInNewPaneCommand;

		// Constructor

		public FileTagsWidgetViewModel()
		{
			Containers = new();

			_ = LoadFileTagsContainers();

			// Second function is layered on top to ensure that OpenPath function is late initialized and a null reference is not passed-in
			// See FileTagItemViewModel._openAction for more information

			OpenInNewTabCommand = new AsyncRelayCommand<WidgetCardItem>(ExecuteOpenInNewTabCommand);
			OpenInNewWindowCommand = new AsyncRelayCommand<WidgetCardItem>(ExecuteOpenInNewWindowCommand);
			OpenFileLocationCommand = new RelayCommand<WidgetCardItem>(ExecuteOpenFileLocationCommand);
			OpenInNewPaneCommand = new RelayCommand<WidgetCardItem>(ExecuteOpenInNewPaneCommand);
			PinToFavoritesCommand = new AsyncRelayCommand<WidgetCardItem>(ExecutePinToFavoritesCommand);
			UnpinFromFavoritesCommand = new AsyncRelayCommand<WidgetCardItem>(ExecuteUnpinFromFavoritesCommand);
			OpenPropertiesCommand = new RelayCommand<WidgetCardItem>(ExecuteOpenPropertiesCommand);
		}

		// Methods

		public Task RefreshWidgetAsync()
		{
			return Task.CompletedTask;
		}

		public async Task LoadFileTagsContainers(CancellationToken cancellationToken = default)
		{
			await foreach (var item in FileTagsService.GetTagsAsync(cancellationToken))
			{
				var container = new WidgetFileTagsContainerItem(item.Uid, _openAction)
				{
					Name = item.Name,
					Color = item.Color
				};
				Containers.Add(container);

				_ = container.InitAsync(cancellationToken);
			}
		}

		public override List<ContextMenuFlyoutItemViewModel> GetItemMenuItems(WidgetCardItem item, bool isPinned, bool isFolder = false)
		{
			return new List<ContextMenuFlyoutItemViewModel>()
			{
				new()
				{
					Text = "OpenWith".GetLocalizedResource(),
					OpacityIcon = new OpacityIconModel()
					{
						OpacityIconStyle = "ColorIconOpenWith",
					},
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
					OpacityIcon = new OpacityIconModel()
					{
						OpacityIconStyle = "ColorIconOpenInNewTab",
					},
					Command = OpenInNewTabCommand!,
					CommandParameter = item,
					ShowItem = isFolder
				},
				new()
				{
					Text = "OpenInNewWindow".GetLocalizedResource(),
					OpacityIcon = new OpacityIconModel()
					{
						OpacityIconStyle = "ColorIconOpenInNewWindow",
					},
					Command = OpenInNewWindowCommand!,
					CommandParameter = item,
					ShowItem = isFolder
				},
				new()
				{
					Text = "OpenFileLocation".GetLocalizedResource(),
					Glyph = "\uED25",
					Command = OpenFileLocationCommand!,
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
					OpacityIcon = new OpacityIconModel()
					{
						OpacityIconStyle = "ColorIconPinToFavorites",
					},
					Command = PinToFavoritesCommand!,
					CommandParameter = item,
					ShowItem = !isPinned && isFolder
				},
				new()
				{
					Text = "UnpinFromFavorites".GetLocalizedResource(),
					OpacityIcon = new OpacityIconModel()
					{
						OpacityIconStyle = "ColorIconUnpinFromFavorites",
					},
					Command = UnpinFromFavoritesCommand!,
					CommandParameter = item,
					ShowItem = isPinned && isFolder
				},
				new()
				{
					Text = "Properties".GetLocalizedResource(),
					OpacityIcon = new OpacityIconModel()
					{
						OpacityIconStyle = "ColorIconProperties",
					},
					Command = OpenPropertiesCommand!,
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
					Items = new List<ContextMenuFlyoutItemViewModel>(),
					ID = "ItemOverflow",
					Tag = "ItemOverflow",
					IsEnabled = false,
				}
			}.Where(x => x.ShowItem).ToList();
		}

		// Event methods

		private async void FileTagItem_ItemClick(object sender, ItemClickEventArgs e)
		{
			if (e.ClickedItem is WidgetFileTagsItem itemViewModel)
				await itemViewModel.ClickCommand.ExecuteAsync(null);
		}

		private void AdaptiveGridView_RightTapped(object sender, RightTappedRoutedEventArgs e)
		{
			// Ensure values are not null
			if (e.OriginalSource is not FrameworkElement element ||
				element.DataContext is not WidgetFileTagsItem item)
				return;

			// Create a new Flyout
			var itemContextMenuFlyout = new CommandBarFlyout()
			{
				Placement = FlyoutPlacementMode.Full
			};

			// Hook events
			itemContextMenuFlyout.Opening += (sender, e) => App.LastOpenedFlyout = sender as CommandBarFlyout;
			itemContextMenuFlyout.Opened += (sender, e) => OnRightClickedItemChanged(null, null);

			_flyoutItemPath = item.Path;

			// Notify of the change on right clicked item
			OnRightClickedItemChanged(item, itemContextMenuFlyout);

			// Get items for the flyout
			var menuItems = GetItemMenuItems(item, QuickAccessService.IsItemPinned(item.Path), item.IsFolder);
			var (_, secondaryElements) = ItemModelListToContextFlyoutHelper.GetAppBarItemsFromModel(menuItems);

			// Set max width of the flyout
			secondaryElements
				.OfType<FrameworkElement>()
				.ForEach(i => i.MinWidth = Constants.UI.ContextMenuItemsMaxWidth);

			// Add menu items to the secondary flyout
			secondaryElements.ForEach(itemContextMenuFlyout.SecondaryCommands.Add);

			// Show the flyout
			itemContextMenuFlyout.ShowAt(element, new() { Position = e.GetPosition(element) });

			// Load shell menu items
			_ = ShellContextmenuHelper.LoadShellMenuItemsAsync(_flyoutItemPath, itemContextMenuFlyout);

			e.Handled = true;
		}

		private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		// Command methods

		private void ExecuteOpenFileLocationCommand(WidgetCardItem? item)
		{
			FileTagsOpenLocationInvoked?.Invoke(this, new PathNavigationEventArgs()
			{
				ItemPath = Directory.GetParent(item?.Path ?? string.Empty)?.FullName ?? string.Empty,
				ItemName = Path.GetFileName(item?.Path ?? string.Empty),
			});
		}

		private void ExecuteOpenPropertiesCommand(WidgetCardItem? item)
		{
			if (!HomePageContext.IsAnyItemRightClicked)
				return;

			EventHandler<object> flyoutClosed = null!;
			flyoutClosed = (s, e) =>
			{
				HomePageContext.ItemContextFlyoutMenu!.Closed -= flyoutClosed;

				ListedItem listedItem = new(null!)
				{
					ItemPath = (item.Item as WidgetFileTagsItem)?.Path ?? string.Empty,
					ItemNameRaw = (item.Item as WidgetFileTagsItem)?.Name ?? string.Empty,
					PrimaryItemAttribute = StorageItemTypes.Folder,
					ItemType = "Folder".GetLocalizedResource(),
				};
				FilePropertiesHelpers.OpenPropertiesWindow(listedItem, AppInstance);
			};

			HomePageContext.ItemContextFlyoutMenu!.Closed += flyoutClosed;
		}

		private void ExecuteOpenInNewPaneCommand(WidgetCardItem? item)
		{
			FileTagsNewPaneInvoked?.Invoke(this, new QuickAccessCardInvokedEventArgs()
			{
				Path = item?.Path ?? string.Empty
			});
		}

		// Disposer

		public void Dispose()
		{
		}
	}
}

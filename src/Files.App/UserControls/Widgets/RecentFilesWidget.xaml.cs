using CommunityToolkit.Mvvm.Input;
using Files.App.Extensions;
using Files.App.Filesystem;
using Files.App.Helpers;
using Files.App.Helpers.ContextFlyouts;
using Files.App.ViewModels;
using Files.App.ViewModels.Widgets;
using Files.Shared.Extensions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Files.App.UserControls.Widgets
{
	public sealed partial class RecentFilesWidget : HomePageWidget, IWidgetItemModel, INotifyPropertyChanged
	{
		public ICommand RefreshCommand { get; set; }
		public ICommand RemoveRecentItemCommand { get; set; }
		public ICommand ClearAllItemsCommand { get; set; }

		public delegate void RecentFilesOpenLocationInvokedEventHandler(object sender, PathNavigationEventArgs e);
		public event RecentFilesOpenLocationInvokedEventHandler RecentFilesOpenLocationInvoked;
		public delegate void RecentFileInvokedEventHandler(object sender, PathNavigationEventArgs e);
		public event RecentFileInvokedEventHandler RecentFileInvoked;
		public event PropertyChangedEventHandler PropertyChanged;
		public string WidgetName => nameof(RecentFilesWidget);
		public string AutomationProperties => "RecentFilesWidgetAutomationProperties/Name".GetLocalizedResource();
		public string WidgetHeader => "RecentFiles".GetLocalizedResource();
		public bool IsWidgetSettingEnabled => UserSettingsService.PreferencesSettingsService.ShowRecentFilesWidget;
		public bool ShowMenuFlyout => false;
		public MenuFlyoutItem? MenuFlyoutItem => null;
		private bool isRecentFilesDisabledInWindows = false;

		public bool IsEmptyTextVisible
		{
			get { return (bool)GetValue(IsEmptyTextVisibleProperty); }
			set { SetValue(IsEmptyTextVisibleProperty, value); }
		}

		public static readonly DependencyProperty IsEmptyTextVisibleProperty =
			DependencyProperty.Register("IsEmptyTextVisible", typeof(bool), typeof(RecentFilesWidget), new PropertyMetadata(false));

		public bool IsDisabledInWindows
		{
			get { return (bool)GetValue(IsDisabledInWindowsProperty); }
			set { SetValue(IsDisabledInWindowsProperty, value); }
		}

		public static readonly DependencyProperty IsDisabledInWindowsProperty =
			DependencyProperty.Register("IsDisabledInWindows", typeof(bool), typeof(RecentFilesWidget), new PropertyMetadata(false));

		private ObservableCollection<RecentItem> items = new ObservableCollection<RecentItem>();
		public ObservableCollection<RecentItem> Items
		{
			get => items;
			set
			{
				items.Clear();
				foreach (var item in value)
				{
					items.Add(item);
				}
			}
		}


		public RecentFilesWidget()
		{
			InitializeComponent();
			this.Loaded += RecentFilesWidget_Loaded;
			OpenFileLocationCommand = new RelayCommand<RecentItem>(OpenFileLocation);
		}

		private async void RecentFilesWidget_Loaded(object sender, RoutedEventArgs e)
		{
			this.Loaded -= RecentFilesWidget_Loaded;
			await RefreshWidget();
		}

		private void Grid_RightTapped(object sender, RightTappedRoutedEventArgs e)
		{
			var itemContextMenuFlyout = new CommandBarFlyout { Placement = FlyoutPlacementMode.Full };
			itemContextMenuFlyout.Opening += (sender, e) => App.LastOpenedFlyout = sender as CommandBarFlyout;
			if (sender is not Grid recentItemsGrid || recentItemsGrid.DataContext is not RecentItem item)
				return;

			var menuItems = GetItemMenuItems(item, false);
			var (_, secondaryElements) = ItemModelListToContextFlyoutHelper.GetAppBarItemsFromModel(menuItems);

			secondaryElements.OfType<FrameworkElement>()
							 .ForEach(i => i.MinWidth = Constants.UI.ContextMenuItemsMaxWidth);

			secondaryElements.ForEach(i => itemContextMenuFlyout.SecondaryCommands.Add(i));
			itemContextMenuFlyout.ShowAt(recentItemsGrid, new FlyoutShowOptions { Position = e.GetPosition(recentItemsGrid) });

			_ = ShellContextmenuHelper.LoadShellMenuItems(item.Path, itemContextMenuFlyout, showOpenWithMenu: true, showSendToMenu: true);

			e.Handled = true;
		}

		public override List<ContextMenuFlyoutItemViewModel> GetItemMenuItems(WidgetCardItem item, bool isPinned, bool isFolder = false)
		{
			return new List<ContextMenuFlyoutItemViewModel>()
			{
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "OpenItemsWithCaptionText".GetLocalizedResource(),
					OpacityIcon = new OpacityIconModel()
					{
						OpacityIconStyle = "ColorIconOpenWith",
					},
					Tag = "OpenWithPlaceholder",
					IsEnabled = false
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "SendTo".GetLocalizedResource(),
					Tag = "SendToPlaceholder",
					IsEnabled = false
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "RecentItemRemove/Text".GetLocalizedResource(),
					Glyph = "\uE738",
					Command = RemoveRecentItemCommand,
					CommandParameter = item
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "RecentItemClearAll/Text".GetLocalizedResource(),
					Glyph = "\uE74D",
					Command = ClearAllItemsCommand
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "OpenFileLocation".GetLocalizedResource(),
					Glyph = "\uED25",
					Command = OpenFileLocationCommand,
					CommandParameter = item
				},
				new ContextMenuFlyoutItemViewModel()
				{
					ItemType = ItemType.Separator,
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
			};
		}

		public Task RefreshWidget()
		{
			RefreshCommand.Execute(null);
			return Task.CompletedTask;
		}

		private void OpenFileLocation(RecentItem item)
		{
			RecentFilesOpenLocationInvoked?.Invoke(this, new PathNavigationEventArgs()
			{
				ItemPath = Directory.GetParent(item.RecentPath).FullName,    // parent directory
				ItemName = Path.GetFileName(item.RecentPath),                // file name w extension
			});
		}

		private void RecentsView_ItemClick(object sender, ItemClickEventArgs e)
		{
			var recentItem = e.ClickedItem as RecentItem;
			RecentFileInvoked?.Invoke(this, new PathNavigationEventArgs()
			{
				ItemPath = recentItem.RecentPath,
				IsFile = recentItem.IsFile
			});
		}

		private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		public void Dispose()
		{
			this.Loaded -= RecentFilesWidget_Loaded;
		}
	}
}
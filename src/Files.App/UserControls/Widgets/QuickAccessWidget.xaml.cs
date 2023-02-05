using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI;
using Files.App.DataModels.NavigationControlItems;
using Files.App.Extensions;
using Files.App.Filesystem;
using Files.App.Helpers;
using Files.App.Helpers.XamlHelpers;
using Files.App.ServicesImplementation;
using Files.App.ViewModels.Widgets;
using Files.Backend.Services.Settings;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Vanara;
using Windows.Storage;
using Windows.System;
using Windows.UI.Core;

namespace Files.App.UserControls.Widgets
{
	public class QuickAccessCardEventArgs : EventArgs
	{
		public LocationItem Item { get; set; }
	}

	public class QuickAccessCardInvokedEventArgs : EventArgs
	{
		public string Path { get; set; }
	}

	public class ModifyQuickAccessEventArgs : EventArgs
	{
		public string[] Paths { get; set; }
		public bool Add;
		public bool Pin = true;

		public ModifyQuickAccessEventArgs(string[] paths, bool add)
		{
			Paths = paths;
			Add = add;
		}
	}

	public class FolderCardItem : ObservableObject, IWidgetCardItem<LocationItem>
	{
		private BitmapImage thumbnail;
		private byte[] thumbnailData;

		public string AutomationProperties { get; set; }
		public bool HasPath => !string.IsNullOrEmpty(Path);
		public bool HasThumbnail => thumbnail is not null && thumbnailData is not null;
		public BitmapImage Thumbnail
		{
			get => thumbnail;
			set => SetProperty(ref thumbnail, value);
		}
		public bool IsLibrary => Item is LibraryLocationItem;
		public bool IsUserCreatedLibrary => IsLibrary && !LibraryManager.IsDefaultLibrary(Item.Path);
		public LocationItem Item { get; private set; }
		public string Path { get; set; }
		public ICommand SelectCommand { get; set; }
		public string Text { get; set; }
		public bool IsPinned { get; set; }

		public FolderCardItem(LocationItem item = null, string text = null, bool isPinned = true) : this(text, isPinned)
		{
			Item = item;
		}

		public FolderCardItem(string text, bool isPinned)
		{
			if (!string.IsNullOrWhiteSpace(text))
			{
				Text = text;
				AutomationProperties = Text;
				IsPinned = isPinned;
			}
		}

		public async Task LoadCardThumbnailAsync()
		{
			if (thumbnailData is null || thumbnailData.Length == 0)
			{
				thumbnailData = await FileThumbnailHelper.LoadIconFromPathAsync(Path, Convert.ToUInt32(Constants.Widgets.WidgetIconSize), Windows.Storage.FileProperties.ThumbnailMode.SingleItem);
			}
			if (thumbnailData is not null && thumbnailData.Length > 0)
			{
				Thumbnail = await App.Window.DispatcherQueue.EnqueueAsync(() => thumbnailData.ToBitmapAsync(Constants.Widgets.WidgetIconSize));
			}
		}
	}

	public sealed partial class QuickAccessWidget : UserControl, IWidgetItemModel, INotifyPropertyChanged
	{
		private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();

		private readonly IQuickAccessService QuickAccessService = Ioc.Default.GetRequiredService<IQuickAccessService>();

		public ObservableCollection<FolderCardItem> ItemsAdded = new();

		private bool showMultiPaneControls;

		public QuickAccessWidget()
		{
			InitializeComponent();

			QuickAccessCardCommand = new AsyncRelayCommand<FolderCardItem>(OpenCard);

			Loaded += QuickAccessWidget_Loaded;
			Unloaded += QuickAccessWidget_Unloaded;
		}

		public delegate void LibraryCardInvokedEventHandler(object sender, QuickAccessCardInvokedEventArgs e);

		public delegate void LibraryCardNewPaneInvokedEventHandler(object sender, QuickAccessCardInvokedEventArgs e);

		public delegate void LibraryCardPropertiesInvokedEventHandler(object sender, QuickAccessCardEventArgs e);

		public event LibraryCardInvokedEventHandler CardInvoked;

		public event LibraryCardNewPaneInvokedEventHandler CardNewPaneInvoked;

		public event LibraryCardPropertiesInvokedEventHandler CardPropertiesInvoked;

		public event EventHandler QuickAccessWidgetShowMultiPaneControlsInvoked;

		public event PropertyChangedEventHandler PropertyChanged;

		public bool IsWidgetSettingEnabled => UserSettingsService.PreferencesSettingsService.ShowQuickAccessWidget;

		public ICommand QuickAccessCardCommand { get; }

		public ICommand ShowCreateNewLibraryDialogCommand { get; } = new RelayCommand(LibraryManager.ShowCreateNewLibraryDialog);

		public readonly ICommand ShowRestoreLibrariesDialogCommand = new RelayCommand(LibraryManager.ShowRestoreDefaultLibrariesDialog);

		public bool ShowMultiPaneControls
		{
			get
			{
				QuickAccessWidgetShowMultiPaneControlsInvoked?.Invoke(this, EventArgs.Empty);

				return showMultiPaneControls;
			}
			set
			{
				if (value != showMultiPaneControls)
				{
					showMultiPaneControls = value;
					NotifyPropertyChanged(nameof(ShowMultiPaneControls));
				}
			}
		}

		public string WidgetName => nameof(QuickAccessWidget);

		public string AutomationProperties => "QuickAccess".GetLocalizedResource();

		public string WidgetHeader => "QuickAccess".GetLocalizedResource();

		private async void ModifyItem(object? sender, ModifyQuickAccessEventArgs? e)
		{
			if (e is null)
				return;

			if (e.Add)
			{
				var locationItems = new List<LocationItem>();
				foreach (var item in e.Paths)
					locationItems.Add(await App.QuickAccessManager.Model.CreateLocationItemFromPathAsync(item));

				foreach (var item in locationItems)
					ItemsAdded.Insert(e.Pin ? ItemsAdded.Count - 4 : ItemsAdded.Count, new FolderCardItem(Path.GetFileName(item.Text), e.Pin) // Add just after the Recent Folders
					{
						Path = item.Path,
						SelectCommand = QuickAccessCardCommand
					});

				var cardLoadTasks = ItemsAdded.Select(cardItem => cardItem.LoadCardThumbnailAsync());
				await Task.WhenAll(cardLoadTasks);
			}
			else
				foreach (var itemToRemove in ItemsAdded.Where(x => e.Paths.Contains(x.Path)).ToList())
					ItemsAdded.Remove(itemToRemove);
		}

		private async void QuickAccessWidget_Loaded(object sender, RoutedEventArgs e)
		{
			Loaded -= QuickAccessWidget_Loaded;

			var itemsToAdd = await QuickAccessService.GetPinnedFoldersAsync(true);

			var locationItems = new List<LocationItem>();
			foreach (var item in itemsToAdd)
				locationItems.Add(await App.QuickAccessManager.Model.CreateLocationItemFromPathAsync(item));

			int idx = 0;
			foreach (var item in locationItems)
			{
				ItemsAdded.Add(new FolderCardItem(item, Path.GetFileName(item.Text), idx < locationItems.Count - 4)
				{
					Path = item.Path,
					SelectCommand = QuickAccessCardCommand
				});
				idx++;
			}

			App.QuickAccessManager.UpdateQuickAccessWidget += ModifyItem;

			var cardLoadTasks = ItemsAdded.Select(cardItem => cardItem.LoadCardThumbnailAsync());
			await Task.WhenAll(cardLoadTasks);
		}

		private void QuickAccessWidget_Unloaded(object sender, RoutedEventArgs e)
		{
			Unloaded -= QuickAccessWidget_Unloaded;
			App.QuickAccessManager.UpdateQuickAccessWidget += ModifyItem;
		}

		private void MenuFlyout_Opening(object sender, object e)
		{
			var newPaneMenuItem = (sender as MenuFlyout).Items.SingleOrDefault(x => x.Name == "OpenInNewPane");
			// eg. an empty library doesn't have OpenInNewPane context menu item
			if (newPaneMenuItem is not null)
				newPaneMenuItem.Visibility = ShowMultiPaneControls ? Visibility.Visible : Visibility.Collapsed;
			
			var pinToFavoritesItem = (sender as MenuFlyout).Items.SingleOrDefault(x => x.Name == "PinToFavorites");
			if (pinToFavoritesItem is not null) 
				pinToFavoritesItem.Visibility = (pinToFavoritesItem.DataContext as FolderCardItem).IsPinned ? Visibility.Collapsed : Visibility.Visible;

			var unpinFromFavoritesItem = (sender as MenuFlyout).Items.SingleOrDefault(x => x.Name == "UnpinFromFavorites");
			if (unpinFromFavoritesItem is not null)
				unpinFromFavoritesItem.Visibility = (unpinFromFavoritesItem.DataContext as FolderCardItem).IsPinned ? Visibility.Visible : Visibility.Collapsed;
		}

		private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		private void OpenInNewPane_Click(object sender, RoutedEventArgs e)
		{
			var item = ((MenuFlyoutItem)sender).DataContext as FolderCardItem;
			CardNewPaneInvoked?.Invoke(this, new QuickAccessCardInvokedEventArgs { Path = item.Path });
		}

		private async void OpenInNewTab_Click(object sender, RoutedEventArgs e)
		{
			var item = ((MenuFlyoutItem)sender).DataContext as FolderCardItem;
			await NavigationHelpers.OpenPathInNewTab(item.Path);
		}

		private async void Button_PointerPressed(object sender, PointerRoutedEventArgs e)
		{
			if (e.GetCurrentPoint(null).Properties.IsMiddleButtonPressed) // check middle click
			{
				string navigationPath = ((Button)sender).Tag.ToString()!;
				await NavigationHelpers.OpenPathInNewTab(navigationPath);
			}
		}

		private async void OpenInNewWindow_Click(object sender, RoutedEventArgs e)
		{
			var item = ((MenuFlyoutItem)sender).DataContext as FolderCardItem;
			await NavigationHelpers.OpenPathInNewWindowAsync(item.Path);
		}

		private void OpenProperties_Click(object sender, RoutedEventArgs e)
		{
			var presenter = DependencyObjectHelpers.FindParent<MenuFlyoutPresenter>((MenuFlyoutItem)sender);
			var flyoutParent = presenter?.Parent as Popup;
			var propertiesItem = ((MenuFlyoutItem)sender).DataContext as FolderCardItem;
			if (propertiesItem is null  || flyoutParent is null)
				return;

			EventHandler<object> flyoutClosed = null!;
			flyoutClosed = (s, e) =>
			{
				flyoutParent.Closed -= flyoutClosed;
				CardPropertiesInvoked?.Invoke(this, new QuickAccessCardEventArgs { Item = propertiesItem.Item });
			};
			flyoutParent.Closed += flyoutClosed;
		}

		private async void PinToFavorites_Click(object sender, RoutedEventArgs e)
		{
			var item = ((MenuFlyoutItem)sender).DataContext as FolderCardItem;
			await QuickAccessService.PinToSidebar(item.Path);
			ModifyItem(this, new ModifyQuickAccessEventArgs(new[] { item.Path }, false));
			var items = await QuickAccessService.GetPinnedFoldersAsync(true);
			items.RemoveRange(0, items.Count - 4);
			var recentItem = items.Where(x => !ItemsAdded.Select(y => y.Path).Contains(x)).FirstOrDefault();
			ModifyItem(this, new ModifyQuickAccessEventArgs(new[] { recentItem }, true)
			{
				Pin = false
			});
		}

		private void UnpinFromFavorites_Click(object sender, RoutedEventArgs e)
		{
			var item = ((MenuFlyoutItem)sender).DataContext as FolderCardItem;
			_ = QuickAccessService.UnpinFromSidebar(item.Path);
		}

		private Task OpenCard(FolderCardItem item)
		{
			if (string.IsNullOrEmpty(item.Path))
			{
				return Task.CompletedTask;
			}
			if (item.Item is LibraryLocationItem lli && lli.IsEmpty)
			{
				// TODO: show message?
				return Task.CompletedTask;
			}

			var ctrlPressed = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
			if (ctrlPressed)
			{
				return NavigationHelpers.OpenPathInNewTab(item.Path);
			}

			CardInvoked?.Invoke(this, new QuickAccessCardInvokedEventArgs { Path = item.Path });

			return Task.CompletedTask;
		}

		public Task RefreshWidget()
		{
			return Task.CompletedTask;
		}

		public void Dispose()
		{

		}
	}
}
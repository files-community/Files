using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI;
using Files.App.DataModels.NavigationControlItems;
using Files.App.Extensions;
using Files.App.Filesystem;
using Files.App.Helpers;
using Files.App.Helpers.XamlHelpers;
using Files.App.ViewModels.Widgets;
using Files.Backend.Services.Settings;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Storage;
using Windows.System;
using Windows.UI.Core;

namespace Files.App.UserControls.Widgets
{
	public class LibraryCardEventArgs : EventArgs
	{
		public LibraryLocationItem Library { get; set; }
	}

	public class LibraryCardInvokedEventArgs : EventArgs
	{
		public string Path { get; set; }
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

		public FolderCardItem(LocationItem item = null, string text = null) : this(text)
		{
			this.Item = item;
		}

		public FolderCardItem(string text)
		{
			if (!string.IsNullOrWhiteSpace(text))
			{
				this.Text = text;
				AutomationProperties = Text;
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

	public sealed partial class FolderWidget : UserControl, IWidgetItemModel, INotifyPropertyChanged
	{
		private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();

		public ObservableCollection<FolderCardItem> ItemsAdded = new();

		private bool showMultiPaneControls;

		public FolderWidget()
		{
			InitializeComponent();

			LibraryCardCommand = new AsyncRelayCommand<FolderCardItem>(OpenLibraryCard);

			Loaded += FolderWidget_Loaded;
			Unloaded += FolderWidget_Unloaded;
		}

		public delegate void LibraryCardInvokedEventHandler(object sender, LibraryCardInvokedEventArgs e);

		public delegate void LibraryCardNewPaneInvokedEventHandler(object sender, LibraryCardInvokedEventArgs e);

		public delegate void LibraryCardPropertiesInvokedEventHandler(object sender, LibraryCardEventArgs e);

		public event LibraryCardInvokedEventHandler LibraryCardInvoked;

		public event LibraryCardNewPaneInvokedEventHandler LibraryCardNewPaneInvoked;

		public event LibraryCardPropertiesInvokedEventHandler LibraryCardPropertiesInvoked;

		public event EventHandler FolderWidgethowMultiPaneControlsInvoked;

		public event PropertyChangedEventHandler PropertyChanged;

		public bool IsWidgetSettingEnabled => UserSettingsService.AppearanceSettingsService.ShowFoldersWidget;

		public ICommand LibraryCardCommand { get; }

		public ICommand ShowCreateNewLibraryDialogCommand { get; } = new RelayCommand(LibraryManager.ShowCreateNewLibraryDialog);

		public readonly ICommand ShowRestoreLibrariesDialogCommand = new RelayCommand(LibraryManager.ShowRestoreDefaultLibrariesDialog);

		public bool ShowMultiPaneControls
		{
			get
			{
				FolderWidgethowMultiPaneControlsInvoked?.Invoke(this, EventArgs.Empty);

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

		public string WidgetName => nameof(FolderWidget);

		public string AutomationProperties => "FolderWidgetAutomationProperties/Name".GetLocalizedResource();

		public string WidgetHeader => "Folders".GetLocalizedResource();

		private async void FolderWidget_Loaded(object sender, RoutedEventArgs e)
		{
			Loaded -= FolderWidget_Loaded;

			ItemsAdded.Add(new FolderCardItem("Desktop".GetLocalizedResource())
			{
				Path = UserDataPaths.GetDefault().Desktop,
				SelectCommand = LibraryCardCommand
			});
			ItemsAdded.Add(new FolderCardItem("Documents".GetLocalizedResource())
			{
				Path = UserDataPaths.GetDefault().Documents,
				SelectCommand = LibraryCardCommand
			});
			ItemsAdded.Add(new FolderCardItem("Downloads".GetLocalizedResource())
			{
				Path = UserDataPaths.GetDefault().Downloads,
				SelectCommand = LibraryCardCommand
			});
			ItemsAdded.Add(new FolderCardItem("Music".GetLocalizedResource())
			{
				Path = UserDataPaths.GetDefault().Music,
				SelectCommand = LibraryCardCommand
			});
			ItemsAdded.Add(new FolderCardItem("Pictures".GetLocalizedResource())
			{
				Path = UserDataPaths.GetDefault().Pictures,
				SelectCommand = LibraryCardCommand
			});
			ItemsAdded.Add(new FolderCardItem("Videos".GetLocalizedResource())
			{
				Path = UserDataPaths.GetDefault().Videos,
				SelectCommand = LibraryCardCommand
			});

			var cardLoadTasks = ItemsAdded.Select(cardItem => cardItem.LoadCardThumbnailAsync());
			await Task.WhenAll(cardLoadTasks);
		}

		private void FolderWidget_Unloaded(object sender, RoutedEventArgs e)
		{
			Unloaded -= FolderWidget_Unloaded;
		}

		private void MenuFlyout_Opening(object sender, object e)
		{
			var newPaneMenuItem = (sender as MenuFlyout).Items.SingleOrDefault(x => x.Name == "OpenInNewPane");
			// eg. an empty library doesn't have OpenInNewPane context menu item
			if (newPaneMenuItem is not null)
			{
				newPaneMenuItem.Visibility = ShowMultiPaneControls ? Visibility.Visible : Visibility.Collapsed;
			}
		}

		private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		private void OpenInNewPane_Click(object sender, RoutedEventArgs e)
		{
			var item = ((MenuFlyoutItem)sender).DataContext as FolderCardItem;
			LibraryCardNewPaneInvoked?.Invoke(this, new LibraryCardInvokedEventArgs { Path = item.Path });
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

		private void OpenLibraryProperties_Click(object sender, RoutedEventArgs e)
		{
			var presenter = DependencyObjectHelpers.FindParent<MenuFlyoutPresenter>((MenuFlyoutItem)sender);
			var flyoutParent = presenter?.Parent as Popup;
			var propertiesItem = ((MenuFlyoutItem)sender).DataContext as FolderCardItem;
			if (propertiesItem is null || !propertiesItem.IsLibrary || flyoutParent is null)
				return;

			EventHandler<object> flyoutClosed = null!;
			flyoutClosed = (s, e) =>
			{
				flyoutParent.Closed -= flyoutClosed;
				LibraryCardPropertiesInvoked?.Invoke(this, new LibraryCardEventArgs { Library = (propertiesItem.Item as LibraryLocationItem)! });
			};
			flyoutParent.Closed += flyoutClosed;
		}

		private Task OpenLibraryCard(FolderCardItem item)
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

			LibraryCardInvoked?.Invoke(this, new LibraryCardInvokedEventArgs { Path = item.Path });

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
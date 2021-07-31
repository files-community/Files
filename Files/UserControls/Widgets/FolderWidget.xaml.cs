﻿using Files.Extensions;
using Files.Filesystem;
using Files.Helpers;
using Files.ViewModels;
using Files.ViewModels.Widgets;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Uwp;
using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Imaging;

namespace Files.UserControls.Widgets
{
    public class LibraryCardEventArgs : EventArgs
    {
        public LibraryLocationItem Library { get; set; }
    }

    public class LibraryCardInvokedEventArgs : EventArgs
    {
        public string Path { get; set; }
    }

    public class LibraryCardItem : ObservableObject
    {
        public string AutomationProperties { get; set; }
        public bool HasPath => !string.IsNullOrEmpty(Path);

        private BitmapImage icon;
        public BitmapImage Icon
        {
            get => icon;
            set => SetProperty(ref icon, value);
        }

        public byte[] IconData { get; set; }
        public bool IsLibrary => Library != null;
        public bool IsUserCreatedLibrary => Library != null && !LibraryHelper.IsDefaultLibrary(Library.Path);
        public LibraryLocationItem Library { get; set; }
        public string Path { get; set; }
        public RelayCommand<LibraryCardItem> SelectCommand { get; set; }
        public string Text { get; set; }
    }

    public sealed partial class FolderWidget : UserControl, IWidgetItemModel, INotifyPropertyChanged
    {
        public BulkConcurrentObservableCollection<LibraryCardItem> ItemsAdded = new BulkConcurrentObservableCollection<LibraryCardItem>();
        private bool showMultiPaneControls;

        public FolderWidget()
        {
            InitializeComponent();

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

        public SettingsViewModel AppSettings => App.AppSettings;

        public bool IsWidgetSettingEnabled => App.AppSettings.ShowFolderWidgetWidget;

        public RelayCommand<LibraryCardItem> LibraryCardClicked => new RelayCommand<LibraryCardItem>(async (item) =>
        {
            if (string.IsNullOrEmpty(item.Path))
            {
                return;
            }
            if (item.IsLibrary && item.Library.IsEmpty)
            {
                // TODO: show message?
                return;
            }

            var ctrlPressed = Window.Current.CoreWindow.GetKeyState(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
            if (ctrlPressed)
            {
                await NavigationHelpers.OpenPathInNewTab(item.Path);
                return;
            }

            LibraryCardInvoked?.Invoke(this, new LibraryCardInvokedEventArgs { Path = item.Path });
        });

        public RelayCommand ShowCreateNewLibraryDialogCommand => new RelayCommand(LibraryHelper.ShowCreateNewLibraryDialog);

        public readonly RelayCommand ShowRestoreLibrariesDialogCommand = new RelayCommand(LibraryHelper.ShowRestoreDefaultLibrariesDialog);

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

        public void Dispose()
        {
        }

        private async Task GetItemsAddedIcon()
        {
            foreach (var item in ItemsAdded)
            {
                item.SelectCommand = LibraryCardClicked;
                item.AutomationProperties = item.Text;
                await this.LoadLibraryIcon(item);
            }
        }

        private async void FolderWidget_Loaded(object sender, RoutedEventArgs e)
        {
            ItemsAdded.BeginBulkOperation();
            ItemsAdded.Add(new LibraryCardItem
            {
                Text = "SidebarDesktop".GetLocalized(),
                Path = UserDataPaths.GetDefault().Desktop
            });
            ItemsAdded.Add(new LibraryCardItem
            {
                Text = "SidebarDocuments".GetLocalized(),
                Path = UserDataPaths.GetDefault().Documents,
            });
            ItemsAdded.Add(new LibraryCardItem
            {
                Text = "SidebarDownloads".GetLocalized(),
                Path = UserDataPaths.GetDefault().Downloads,
            });
            ItemsAdded.Add(new LibraryCardItem
            {
                Text = "SidebarMusic".GetLocalized(),
                Path = UserDataPaths.GetDefault().Music,
            });
            ItemsAdded.Add(new LibraryCardItem
            {
                Text = "SidebarPictures".GetLocalized(),
                Path = UserDataPaths.GetDefault().Pictures,
            });
            ItemsAdded.Add(new LibraryCardItem
            {
                Text = "SidebarVideos".GetLocalized(),
                Path = UserDataPaths.GetDefault().Videos,
            });

            await GetItemsAddedIcon();

            ItemsAdded.EndBulkOperation();
            Loaded -= FolderWidget_Loaded;
        }

        private void FolderWidget_Unloaded(object sender, RoutedEventArgs e)
        {
            Unloaded -= FolderWidget_Unloaded;
        }

        private void MenuFlyout_Opening(object sender, object e)
        {
            var newPaneMenuItem = (sender as MenuFlyout).Items.SingleOrDefault(x => x.Name == "OpenInNewPane");
            // eg. an empty library doesn't have OpenInNewPane context menu item
            if (newPaneMenuItem != null)
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
            var item = ((MenuFlyoutItem)sender).DataContext as LibraryCardItem;
            LibraryCardNewPaneInvoked?.Invoke(this, new LibraryCardInvokedEventArgs { Path = item.Path });
        }

        private async void OpenInNewTab_Click(object sender, RoutedEventArgs e)
        {
            var item = ((MenuFlyoutItem)sender).DataContext as LibraryCardItem;
            await NavigationHelpers.OpenPathInNewTab(item.Path);
        }

        private async void Button_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (e.GetCurrentPoint(null).Properties.IsMiddleButtonPressed) // check middle click
            {
                string navigationPath = (sender as Button).Tag.ToString();
                await NavigationHelpers.OpenPathInNewTab(navigationPath);
            }
        }

        private async void OpenInNewWindow_Click(object sender, RoutedEventArgs e)
        {
            var item = ((MenuFlyoutItem)sender).DataContext as LibraryCardItem;
            await NavigationHelpers.OpenPathInNewWindowAsync(item.Path);
        }

        private void OpenLibraryProperties_Click(object sender, RoutedEventArgs e)
        {
            var item = (sender as MenuFlyoutItem).DataContext as LibraryCardItem;
            if (item.IsLibrary)
            {
                LibraryCardPropertiesInvoked?.Invoke(this, new LibraryCardEventArgs { Library = item.Library });
            }
        }

        private async Task LoadLibraryIcon(LibraryCardItem item)
        {
            byte[] iconData = await FileThumbnailHelper.LoadIconWithoutOverlayAsync(item.Path, 48u);
            if (iconData != null)
            {
                item.Icon = await CoreApplication.MainView.DispatcherQueue.EnqueueAsync(() => iconData.ToBitmapAsync());
            }
            else
            {
                StorageFolder folder = await FilesystemTasks.Wrap(() => StorageFolder.GetFolderFromPathAsync(item.Path).AsTask());
                if (folder != null)
                {
                    await CoreApplication.MainView.DispatcherQueue.EnqueueAsync(async () =>
                    {
                        item.Icon = new BitmapImage();
                        using var thumbnail = await folder.GetThumbnailAsync(Windows.Storage.FileProperties.ThumbnailMode.ListView, 48, Windows.Storage.FileProperties.ThumbnailOptions.ReturnOnlyIfCached);
                        if (thumbnail != null)
                        {
                            await item.Icon.SetSourceAsync(thumbnail);
                        }
                    });
                }
            }
        }
    }
}
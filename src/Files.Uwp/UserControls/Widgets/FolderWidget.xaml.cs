using Files.Uwp.Filesystem;
using Files.Uwp.Helpers;
using Files.Backend.Services.Settings;
using Files.Uwp.ViewModels.Widgets;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Toolkit.Uwp;
using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Imaging;
using Files.Uwp.DataModels.NavigationControlItems;

namespace Files.Uwp.UserControls.Widgets
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
        public bool HasThumbnail => thumbnail != null && thumbnailData != null;
        public BitmapImage Thumbnail
        {
            get => thumbnail;
            set => SetProperty(ref thumbnail, value);
        }
        public bool IsLibrary => Item is LibraryLocationItem;
        public bool IsUserCreatedLibrary => IsLibrary && !LibraryHelper.IsDefaultLibrary(Item.Path);
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

        public async Task LoadCardThumbnailAsync(int overrideThumbnailSize = 32)
        {
            if (thumbnailData == null || thumbnailData.Length == 0)
            {
                thumbnailData = await FileThumbnailHelper.LoadIconFromPathAsync(Path, Convert.ToUInt32(overrideThumbnailSize), Windows.Storage.FileProperties.ThumbnailMode.ListView);
                if (thumbnailData != null && thumbnailData.Length > 0)
                {
                    Thumbnail = await CoreApplication.MainView.DispatcherQueue.EnqueueAsync(() => thumbnailData.ToBitmapAsync(overrideThumbnailSize));
                }
            }
        }
    }

    public sealed partial class FolderWidget : UserControl, IWidgetItemModel, INotifyPropertyChanged
    {
        private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetService<IUserSettingsService>();

        public BulkConcurrentObservableCollection<FolderCardItem> ItemsAdded = new BulkConcurrentObservableCollection<FolderCardItem>();
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

        public bool IsWidgetSettingEnabled => UserSettingsService.WidgetsSettingsService.ShowFoldersWidget;

        public ICommand LibraryCardCommand { get; }

        public ICommand ShowCreateNewLibraryDialogCommand { get; } = new RelayCommand(LibraryHelper.ShowCreateNewLibraryDialog);

        public readonly ICommand ShowRestoreLibrariesDialogCommand = new RelayCommand(LibraryHelper.ShowRestoreDefaultLibrariesDialog);

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

        public string AutomationProperties => "FolderWidgetAutomationProperties/Name".GetLocalized();

        public string WidgetHeader => "Folders".GetLocalized();

        private async void FolderWidget_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= FolderWidget_Loaded;

            ItemsAdded.BeginBulkOperation();
            ItemsAdded.Add(new FolderCardItem("Desktop".GetLocalized())
            {
                Path = UserDataPaths.GetDefault().Desktop,
                SelectCommand = LibraryCardCommand
            });
            ItemsAdded.Add(new FolderCardItem("Documents".GetLocalized())
            {
                Path = UserDataPaths.GetDefault().Documents,
                SelectCommand = LibraryCardCommand
            });
            ItemsAdded.Add(new FolderCardItem("Downloads".GetLocalized())
            {
                Path = UserDataPaths.GetDefault().Downloads,
                SelectCommand = LibraryCardCommand
            });
            ItemsAdded.Add(new FolderCardItem("Music".GetLocalized())
            {
                Path = UserDataPaths.GetDefault().Music,
                SelectCommand = LibraryCardCommand
            });
            ItemsAdded.Add(new FolderCardItem("Pictures".GetLocalized())
            {
                Path = UserDataPaths.GetDefault().Pictures,
                SelectCommand = LibraryCardCommand
            });
            ItemsAdded.Add(new FolderCardItem("Videos".GetLocalized())
            {
                Path = UserDataPaths.GetDefault().Videos,
                SelectCommand = LibraryCardCommand
            });

            foreach (var cardItem in ItemsAdded.ToList()) // ToList() is necessary
            {
                await cardItem.LoadCardThumbnailAsync();
            }

            ItemsAdded.EndBulkOperation();
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
                string navigationPath = (sender as Button).Tag.ToString();
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
            var item = (sender as MenuFlyoutItem).DataContext as FolderCardItem;
            if (item.IsLibrary)
            {
                LibraryCardPropertiesInvoked?.Invoke(this, new LibraryCardEventArgs { Library = item.Item as LibraryLocationItem });
            }
        }

        private async Task OpenLibraryCard(FolderCardItem item)
        {
            if (string.IsNullOrEmpty(item.Path))
            {
                return;
            }
            if (item.Item is LibraryLocationItem lli && lli.IsEmpty)
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
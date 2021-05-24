using Files.Dialogs;
using Files.Enums;
using Files.Filesystem;
using Files.Helpers;
using Files.ViewModels;
using Files.ViewModels.Dialogs;
using Files.ViewModels.Widgets;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Uwp;
using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
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

    public class LibraryCardItem
    {
        public string AutomationProperties { get; set; }
        public bool HasPath => !string.IsNullOrEmpty(Path);
        public BitmapImage Icon { get; set; }
        public byte[] IconData { get; set; }
        public bool IsLibrary => Library != null;
        public bool IsUserCreatedLibrary => Library != null && !LibraryHelper.IsDefaultLibrary(Library.Path);
        public LibraryLocationItem Library { get; set; }
        public string Path { get; set; }
        public RelayCommand<LibraryCardItem> SelectCommand { get; set; }
        public string Text { get; set; }
    }

    public sealed partial class LibraryCards : UserControl, IWidgetItemModel, INotifyPropertyChanged
    {
        public BulkConcurrentObservableCollection<LibraryCardItem> ItemsAdded = new BulkConcurrentObservableCollection<LibraryCardItem>();
        private bool showMultiPaneControls;

        public LibraryCards()
        {
            InitializeComponent();

            Loaded += LibraryCards_Loaded;
            Unloaded += LibraryCards_Unloaded;
        }

        public delegate void LibraryCardDeleteInvokedEventHandler(object sender, LibraryCardEventArgs e);

        public delegate void LibraryCardInvokedEventHandler(object sender, LibraryCardInvokedEventArgs e);

        public delegate void LibraryCardNewPaneInvokedEventHandler(object sender, LibraryCardInvokedEventArgs e);

        public delegate void LibraryCardPropertiesInvokedEventHandler(object sender, LibraryCardEventArgs e);

        public event LibraryCardDeleteInvokedEventHandler LibraryCardDeleteInvoked;

        public event LibraryCardInvokedEventHandler LibraryCardInvoked;

        public event LibraryCardNewPaneInvokedEventHandler LibraryCardNewPaneInvoked;

        public event LibraryCardPropertiesInvokedEventHandler LibraryCardPropertiesInvoked;

        public event EventHandler LibraryCardShowMultiPaneControlsInvoked;

        public event PropertyChangedEventHandler PropertyChanged;

        public Func<string, uint, Task<byte[]>> LoadIconOverlay;

        public SettingsViewModel AppSettings => App.AppSettings;

        public bool IsWidgetSettingEnabled => App.AppSettings.ShowLibraryCardsWidget;

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

        public RelayCommand OpenCreateNewLibraryDialogCommand => new RelayCommand(OpenCreateNewLibraryDialog);

        public bool ShowMultiPaneControls
        {
            get
            {
                LibraryCardShowMultiPaneControlsInvoked?.Invoke(this, EventArgs.Empty);

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

        public string WidgetName => nameof(LibraryCards);

        public void Dispose()
        {
        }

        private async void DeleteLibrary_Click(object sender, RoutedEventArgs e)
        {
            var item = (sender as MenuFlyoutItem).DataContext as LibraryCardItem;
            if (item.IsUserCreatedLibrary)
            {
                var dialog = new DynamicDialog(new DynamicDialogViewModel
                {
                    TitleText = "LibraryCardsDeleteLibraryDialogTitleText".GetLocalized(),
                    SubtitleText = "LibraryCardsDeleteLibraryDialogSubtitleText".GetLocalized(),
                    PrimaryButtonText = "DialogDeleteLibraryButtonText".GetLocalized(),
                    CloseButtonText = "DialogCancelButtonText".GetLocalized(),
                    PrimaryButtonAction = (vm, e) => LibraryCardDeleteInvoked?.Invoke(this, new LibraryCardEventArgs { Library = item.Library }),
                    CloseButtonAction = (vm, e) => vm.HideDialog(),
                    KeyDownAction = (vm, e) =>
                    {
                        if (e.Key == VirtualKey.Enter)
                        {
                            vm.PrimaryButtonAction(vm, null);
                        }
                        else if (e.Key == VirtualKey.Escape)
                        {
                            vm.HideDialog();
                        }
                    },
                    DynamicButtons = DynamicDialogButtons.Primary | DynamicDialogButtons.Cancel
                });
                await dialog.ShowAsync();
            }
        }

        private async Task<byte[]> GetIcon(string path)
        {
            return await LoadIconOverlay(path, 48u);
        }

        private async Task GetItemsAddedIcon()
        {
            foreach (var item in ItemsAdded)
            {
                var iconData = await GetIcon(item.Path);
                item.Icon = await CoreApplication.MainView.DispatcherQueue.EnqueueAsync(async () =>
                {
                    return await iconData.ToBitmapAsync();
                });
                if (item.Library != null)
                {
                    item.Library.IconData = iconData;
                }
                item.SelectCommand = LibraryCardClicked;
                item.AutomationProperties = item.Text;
            }
        }

        private void Libraries_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) => ReloadLibraryItems();

        private async void LibraryCards_Loaded(object sender, RoutedEventArgs e)
        {
            ItemsAdded.BeginBulkOperation();
            ItemsAdded.Add(new LibraryCardItem
            {
                Text = "SidebarDesktop".GetLocalized(),
                Path = AppSettings.DesktopPath,
            });
            ItemsAdded.Add(new LibraryCardItem
            {
                Text = "SidebarDownloads".GetLocalized(),
                Path = AppSettings.DownloadsPath,
            });
            await GetItemsAddedIcon();
            ItemsAdded.EndBulkOperation();

            if (App.LibraryManager.Libraries.Count > 0)
            {
                ReloadLibraryItems();
            }
            App.LibraryManager.Libraries.CollectionChanged += Libraries_CollectionChanged;
            Loaded -= LibraryCards_Loaded;
        }

        private void LibraryCards_Unloaded(object sender, RoutedEventArgs e)
        {
            App.LibraryManager.Libraries.CollectionChanged -= Libraries_CollectionChanged;
            Unloaded -= LibraryCards_Unloaded;
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

        private async void OpenCreateNewLibraryDialog()
        {
            var inputText = new TextBox
            {
                PlaceholderText = "LibraryCardsCreateNewLibraryInputPlaceholderText".GetLocalized()
            };
            var tipText = new TextBlock
            {
                Text = string.Empty,
                Visibility = Visibility.Collapsed
            };

            var dialog = new DynamicDialog(new DynamicDialogViewModel
            {
                DisplayControl = new Grid
                {
                    Children =
                    {
                        new StackPanel
                        {
                            Spacing = 4d,
                            Children =
                            {
                                inputText,
                                tipText
                            }
                        }
                    }
                },
                TitleText = "LibraryCardsCreateNewLibraryDialogTitleText".GetLocalized(),
                SubtitleText = "LibraryCardsCreateNewLibraryDialogSubtitleText".GetLocalized(),
                PrimaryButtonText = "DialogCreateLibraryButtonText".GetLocalized(),
                CloseButtonText = "DialogCancelButtonText".GetLocalized(),
                PrimaryButtonAction = async (vm, e) =>
                {
                    var (result, reason) = App.LibraryManager.CanCreateLibrary(inputText.Text);
                    tipText.Text = reason;
                    tipText.Visibility = result ? Visibility.Collapsed : Visibility.Visible;
                    if (!result)
                    {
                        e.Cancel = true;
                        return;
                    }
                    await App.LibraryManager.CreateNewLibrary(inputText.Text);
                },
                CloseButtonAction = (vm, e) =>
                {
                    vm.HideDialog();
                },
                KeyDownAction = async (vm, e) =>
                {
                    if (e.Key == VirtualKey.Enter)
                    {
                        await App.LibraryManager.CreateNewLibrary(inputText.Text);
                    }
                    else if (e.Key == VirtualKey.Escape)
                    {
                        vm.HideDialog();
                    }
                },
                DynamicButtons = DynamicDialogButtons.Primary | DynamicDialogButtons.Cancel
            });
            await dialog.ShowAsync();
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

        private async void ReloadLibraryItems()
        {
            ItemsAdded.BeginBulkOperation();
            var toRemove = ItemsAdded.Where(i => i.IsLibrary).ToList();
            foreach (var item in toRemove)
            {
                ItemsAdded.Remove(item);
            }
            foreach (var lib in App.LibraryManager.Libraries)
            {
                var iconData = await GetIcon(lib.Path);
                lib.IconData = iconData;
                ItemsAdded.Add(new LibraryCardItem
                {
                    Icon = await CoreApplication.MainView.DispatcherQueue.EnqueueAsync(async () =>
                    {
                        return await iconData.ToBitmapAsync();
                    }),
                    Text = lib.Text,
                    Path = lib.Path,
                    SelectCommand = LibraryCardClicked,
                    AutomationProperties = lib.Text,
                    Library = lib,
                });
            }
            ItemsAdded.EndBulkOperation();
        }
    }
}
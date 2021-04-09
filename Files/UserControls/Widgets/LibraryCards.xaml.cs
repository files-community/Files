using Files.Dialogs;
using Files.Enums;
using Files.Filesystem;
using Files.Helpers;
using Files.Interacts;
using Files.ViewModels;
using Files.ViewModels.Dialogs;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Uwp;
using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Media.Imaging;

namespace Files.UserControls.Widgets
{
    public sealed partial class LibraryCards : UserControl, INotifyPropertyChanged
    {
        public SettingsViewModel AppSettings => App.AppSettings;

        public delegate void LibraryCardInvokedEventHandler(object sender, LibraryCardInvokedEventArgs e);

        public event LibraryCardInvokedEventHandler LibraryCardInvoked;

        public delegate void LibraryCardNewPaneInvokedEventHandler(object sender, LibraryCardInvokedEventArgs e);

        public event LibraryCardNewPaneInvokedEventHandler LibraryCardNewPaneInvoked;

        public delegate void LibraryCardPropertiesInvokedEventHandler(object sender, LibraryCardEventArgs e);

        public event LibraryCardPropertiesInvokedEventHandler LibraryCardPropertiesInvoked;

        public delegate void LibraryCardDeleteInvokedEventHandler(object sender, LibraryCardEventArgs e);

        public event LibraryCardDeleteInvokedEventHandler LibraryCardDeleteInvoked;

        public event PropertyChangedEventHandler PropertyChanged;

        public BulkConcurrentObservableCollection<LibraryCardItem> ItemsAdded = new BulkConcurrentObservableCollection<LibraryCardItem>();

        public RelayCommand<LibraryCardItem> LibraryCardClicked => new RelayCommand<LibraryCardItem>(item =>
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
            LibraryCardInvoked?.Invoke(this, new LibraryCardInvokedEventArgs { Path = item.Path });
        });

        public RelayCommand OpenCreateNewLibraryDialogCommand => new RelayCommand(OpenCreateNewLibraryDialog);

        public LibraryCards()
        {
            InitializeComponent();

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
            foreach (var item in ItemsAdded)
            {
                item.Icon = GlyphHelper.GetIconUri(item.Path);
                item.SelectCommand = LibraryCardClicked;
                item.AutomationProperties = item.Text;
            }
            ItemsAdded.EndBulkOperation();

            Loaded += LibraryCards_Loaded;
            Unloaded += LibraryCards_Unloaded;
        }

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void LibraryCards_Loaded(object sender, RoutedEventArgs e)
        {
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

        private void Libraries_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) => ReloadLibraryItems();

        private void ReloadLibraryItems()
        {
            ItemsAdded.BeginBulkOperation();
            var toRemove = ItemsAdded.Where(i => i.IsLibrary).ToList();
            foreach (var item in toRemove)
            {
                ItemsAdded.Remove(item);
            }
            foreach (var lib in App.LibraryManager.Libraries)
            {
                ItemsAdded.Add(new LibraryCardItem
                {
                    Icon = GlyphHelper.GetIconUri(lib.DefaultSaveFolder),
                    Text = lib.Text,
                    Path = lib.Path,
                    SelectCommand = LibraryCardClicked,
                    AutomationProperties = lib.Text,
                    Library = lib,
                });
            }
            ItemsAdded.EndBulkOperation();
        }

        private void GridScaleUp(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            // Source for the scaling: https://github.com/windows-toolkit/WindowsCommunityToolkit/blob/master/Microsoft.Toolkit.Uwp.SampleApp/SamplePages/Implicit%20Animations/ImplicitAnimationsPage.xaml.cs
            // Search for "Scale Element".
            var element = sender as UIElement;
            var visual = ElementCompositionPreview.GetElementVisual(element);
            visual.Scale = new Vector3(1.02f, 1.02f, 1);
        }

        private void GridScaleNormal(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            var element = sender as UIElement;
            var visual = ElementCompositionPreview.GetElementVisual(element);
            visual.Scale = new Vector3(1);
        }

        private bool showMultiPaneControls;

        public bool ShowMultiPaneControls
        {
            get => showMultiPaneControls;
            set
            {
                if (value != showMultiPaneControls)
                {
                    showMultiPaneControls = value;
                    NotifyPropertyChanged(nameof(ShowMultiPaneControls));
                }
            }
        }

        private void OpenInNewTab_Click(object sender, RoutedEventArgs e)
        {
            var item = ((MenuFlyoutItem)sender).DataContext as LibraryCardItem;
            NavigationHelpers.OpenPathInNewTab(item.Path);
        }

        private async void OpenInNewWindow_Click(object sender, RoutedEventArgs e)
        {
            var item = ((MenuFlyoutItem)sender).DataContext as LibraryCardItem;
            await NavigationHelpers.OpenPathInNewWindowAsync(item.Path);
        }

        private void OpenInNewPane_Click(object sender, RoutedEventArgs e)
        {
            var item = ((MenuFlyoutItem)sender).DataContext as LibraryCardItem;
            LibraryCardNewPaneInvoked?.Invoke(this, new LibraryCardInvokedEventArgs { Path = item.Path });
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

        private void OpenLibraryProperties_Click(object sender, RoutedEventArgs e)
        {
            var item = (sender as MenuFlyoutItem).DataContext as LibraryCardItem;
            if (item.IsLibrary)
            {
                LibraryCardPropertiesInvoked?.Invoke(this, new LibraryCardEventArgs { Library = item.Library });
            }
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
    }

    public class LibraryCardInvokedEventArgs : EventArgs
    {
        public string Path { get; set; }
    }

    public class LibraryCardEventArgs : EventArgs
    {
        public LibraryLocationItem Library { get; set; }
    }

    public class LibraryCardItem
    {
        public SvgImageSource Icon { get; set; }
        public string Text { get; set; }
        public string Path { get; set; }
        public LibraryLocationItem Library { get; set; }
        public string AutomationProperties { get; set; }
        public RelayCommand<LibraryCardItem> SelectCommand { get; set; }

        public bool IsLibrary => Library != null;

        public bool IsUserCreatedLibrary => Library != null && !LibraryHelper.IsDefaultLibrary(Library.Path);

        public bool HasPath => !string.IsNullOrEmpty(Path);
    }
}
using Files.Filesystem;
using Files.Helpers;
using Files.Interacts;
using Files.ViewModels;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Uwp;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;

namespace Files.UserControls.Widgets
{
    public sealed partial class LibraryCards : UserControl, INotifyPropertyChanged
    {
        public SettingsViewModel AppSettings => App.AppSettings;

        public delegate void LibraryCardInvokedEventHandler(object sender, LibraryCardInvokedEventArgs e);

        public event LibraryCardInvokedEventHandler LibraryCardInvoked;

        public delegate void LibraryCardNewPaneInvokedEventHandler(object sender, LibraryCardInvokedEventArgs e);

        public event LibraryCardNewPaneInvokedEventHandler LibraryCardNewPaneInvoked;

        public delegate void LibraryCardPropertiesInvokedEventHandler(object sender, LibraryCardPropertiesInvokedEventArgs e);

        public event LibraryCardPropertiesInvokedEventHandler LibraryCardPropertiesInvoked;

        public event PropertyChangedEventHandler PropertyChanged;

        public static ObservableCollection<LibraryCardItem> ItemsAdded = new ObservableCollection<LibraryCardItem>();

        public RelayCommand<LibraryCardItem> LibraryCardClicked => new RelayCommand<LibraryCardItem>(item =>
        {
            if (string.IsNullOrEmpty(item.Path))
            {
                return;
            }
            LibraryCardInvoked?.Invoke(this, new LibraryCardInvokedEventArgs { Path = item.Path });
        });

        public LibraryCards()
        {
            InitializeComponent();

            ItemsAdded.Clear();
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
                item.Icon = GlyphHelper.GetItemIcon(item.Path);
                item.SelectCommand = LibraryCardClicked;
                item.AutomationProperties = item.Text;
            }

            Loaded += LibraryCards_Loaded;
        }

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private async void LibraryCards_Loaded(object sender, RoutedEventArgs e)
        {
            var libs = await LibraryHelper.Instance.ListUserLibraries(true);
            foreach (var lib in libs)
            {
                ItemsAdded.Add(new LibraryCardItem
                {
                    Icon = GlyphHelper.GetItemIcon(lib.DefaultSaveFolder),
                    Text = lib.Text,
                    Path = lib.Path,
                    SelectCommand = LibraryCardClicked,
                    AutomationProperties = lib.Text,
                    Library = lib,
                });
            }

            Loaded -= LibraryCards_Loaded;
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
            Interaction.OpenPathInNewTab(item.Path);
        }

        private async void OpenInNewWindow_Click(object sender, RoutedEventArgs e)
        {
            var item = ((MenuFlyoutItem)sender).DataContext as LibraryCardItem;
            await Interaction.OpenPathInNewWindowAsync(item.Path);
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
                LibraryCardPropertiesInvoked?.Invoke(this, new LibraryCardPropertiesInvokedEventArgs { Library = item.Library });
            }
        }
    }

    public class LibraryCardInvokedEventArgs : EventArgs
    {
        public string Path { get; set; }
    }

    public class LibraryCardPropertiesInvokedEventArgs : EventArgs
    {
        public LibraryLocationItem Library { get; set; }
    }

    public class LibraryCardItem
    {
        public string Icon { get; set; }
        public string Text { get; set; }
        public string Path { get; set; }
        public LibraryLocationItem Library { get; set; }
        public string AutomationProperties { get; set; }
        public RelayCommand<LibraryCardItem> SelectCommand { get; set; }

        public bool IsLibrary => Library != null;

        public bool HasPath => !string.IsNullOrEmpty(Path);
    }
}
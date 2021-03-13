using Files.Filesystem;
using Files.Helpers;
using Files.Interacts;
using Files.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;

namespace Files.UserControls.Widgets
{
    public sealed partial class DrivesWidget : UserControl, INotifyPropertyChanged
    {
        public SettingsViewModel AppSettings => App.AppSettings;

        public delegate void DrivesWidgetInvokedEventHandler(object sender, DrivesWidgetInvokedEventArgs e);

        public event DrivesWidgetInvokedEventHandler DrivesWidgetInvoked;

        public delegate void DrivesWidgetNewPaneInvokedEventHandler(object sender, DrivesWidgetInvokedEventArgs e);

        public event DrivesWidgetNewPaneInvokedEventHandler DrivesWidgetNewPaneInvoked;

        public event PropertyChangedEventHandler PropertyChanged;

        public static ObservableCollection<INavigationControlItem> ItemsAdded = new ObservableCollection<INavigationControlItem>();

        private IShellPage associatedInstance;

        public IShellPage AppInstance
        {
            get => associatedInstance;
            set
            {
                if (value != associatedInstance)
                {
                    associatedInstance = value;
                    NotifyPropertyChanged(nameof(AppInstance));
                }
            }
        }

        public DrivesWidget()
        {
            InitializeComponent();
        }

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private async void EjectDevice_Click(object sender, RoutedEventArgs e)
        {
            var item = ((MenuFlyoutItem)sender).DataContext as DriveItem;
            await DeviceHelpers.EjectDeviceAsync(item.Path);
        }

        private void OpenInNewTab_Click(object sender, RoutedEventArgs e)
        {
            var item = ((MenuFlyoutItem)sender).DataContext as DriveItem;
            Interaction.OpenPathInNewTab(item.Path);
        }

        private async void OpenInNewWindow_Click(object sender, RoutedEventArgs e)
        {
            var item = ((MenuFlyoutItem)sender).DataContext as DriveItem;
            await Interaction.OpenPathInNewWindowAsync(item.Path);
        }

        private async void OpenDriveProperties_Click(object sender, RoutedEventArgs e)
        {
            var item = ((MenuFlyoutItem)sender).DataContext as DriveItem;
            await AppInstance.InteractionOperations.OpenPropertiesWindowAsync(item);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string NavigationPath = ""; // path to navigate
            string ClickedCard = (sender as Button).Tag.ToString();

            NavigationPath = ClickedCard;

            DrivesWidgetInvoked?.Invoke(this, new DrivesWidgetInvokedEventArgs()
            {
                Path = NavigationPath
            });
        }

        public class DrivesWidgetInvokedEventArgs : EventArgs
        {
            public string Path { get; set; }
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

        private void OpenInNewPane_Click(object sender, RoutedEventArgs e)
        {
            var item = ((MenuFlyoutItem)sender).DataContext as DriveItem;
            DrivesWidgetNewPaneInvoked?.Invoke(this, new DrivesWidgetInvokedEventArgs()
            {
                Path = item.Path
            });
        }

        private void MenuFlyout_Opening(object sender, object e)
        {
            var newPaneMenuItem = (sender as MenuFlyout).Items.Single(x => x.Name == "OpenInNewPane");
            newPaneMenuItem.Visibility = ShowMultiPaneControls ? Visibility.Visible : Visibility.Collapsed;
        }

        private async void MapNetworkDrive_Click(object sender, RoutedEventArgs e)
        {
            if (AppInstance.ServiceConnection != null)
            {
                await AppInstance.ServiceConnection.SendMessageAsync(new ValueSet()
                    {
                        { "Arguments", "NetworkDriveOperation" },
                        { "netdriveop", "OpenMapNetworkDriveDialog" }
                    });
            }
        }

        private async void DisconnectNetworkDrive_Click(object sender, RoutedEventArgs e)
        {
            var item = ((MenuFlyoutItem)sender).DataContext as DriveItem;
            if (AppInstance.ServiceConnection != null)
            {
                await AppInstance.ServiceConnection.SendMessageAsync(new ValueSet()
                    {
                        { "Arguments", "NetworkDriveOperation" },
                        { "netdriveop", "DisconnectNetworkDrive" },
                        { "drive", item.Path }
                    });
            }
        }
    }
}
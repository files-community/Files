using Files.Filesystem;
using Files.Interacts;
using Files.View_Models;
using Files.Views;
using System;
using System.Collections.ObjectModel;
using System.Numerics;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;

namespace Files
{
    public sealed partial class DrivesWidget : UserControl
    {
        public SettingsViewModel AppSettings => MainPage.AppSettings;

        public delegate void DrivesWidgetInvokedEventHandler(object sender, DrivesWidgetInvokedEventArgs e);

        public event DrivesWidgetInvokedEventHandler DrivesWidgetInvoked;

        public static ObservableCollection<INavigationControlItem> ItemsAdded = new ObservableCollection<INavigationControlItem>();

        public DrivesWidget()
        {
            InitializeComponent();
        }

        private async void EjectDevice_Click(object sender, RoutedEventArgs e)
        {
            var item = ((MenuFlyoutItem)sender).DataContext as DriveItem;
            await Interaction.EjectDeviceAsync(item.Path);
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

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string NavigationPath = ""; // path to navigate
            string ClickedCard = (sender as Button).Tag.ToString();

            NavigationPath = ClickedCard;

            DrivesWidgetInvoked?.Invoke(this, new DrivesWidgetInvokedEventArgs()
            {
                Path = NavigationPath,
                LayoutType = AppSettings.GetLayoutType()
            });
        }

        public class DrivesWidgetInvokedEventArgs : EventArgs
        {
            public Type LayoutType { get; set; }
            public string Path { get; set; }
        }

        private void GridScaleUp(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            // Source for the scaling: https://github.com/windows-toolkit/WindowsCommunityToolkit/blob/master/Microsoft.Toolkit.Uwp.SampleApp/SamplePages/Implicit%20Animations/ImplicitAnimationsPage.xaml.cs
            // Search for "Scale Element".
            var element = sender as UIElement;
            var visual = ElementCompositionPreview.GetElementVisual(element);
            visual.Scale = new Vector3(1.03f, 1.03f, 1);
        }

        private void GridScaleNormal(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            var element = sender as UIElement;
            var visual = ElementCompositionPreview.GetElementVisual(element);
            visual.Scale = new Vector3(1);
        }
    }
}
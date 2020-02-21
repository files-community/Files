using Files.Filesystem;
using Files.Interacts;
using Files.UserControls.Ribbon;
using Microsoft.Toolkit.Uwp.UI.Controls;
using System;
using System.IO;
using System.Linq;
using Windows.Storage;
using Windows.System;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Animation;


namespace Files.Controls
{
    public sealed partial class RibbonArea : UserControl
    {
        public RibbonViewModel RibbonViewModel { get; } = new RibbonViewModel();
        public RibbonArea()
        {
            this.InitializeComponent();
            Window.Current.SizeChanged += Current_SizeChanged;
            Current_SizeChanged(null, null);
        }

        private void Current_SizeChanged(object sender, Windows.UI.Core.WindowSizeChangedEventArgs e)
        {
            
            

            // Ignore selected File Menu (index 0)
            if (RibbonTabView.SelectedIndex > 0 && RibbonTabView.IsLoaded)
            {
                double totalWidth = 0;
                var allItems = ((RibbonTabView.Items[RibbonTabView.SelectedIndex] as TabViewItem).Content as RibbonPage).PageContent;
                var RibbonItems = allItems.Where(x => x.DisplayMode != RibbonItemDisplayMode.Divider);
                foreach(IRibbonItem item in RibbonItems)
                {
                    totalWidth += item.EstimatedWidth;
                }
                double averageItemWidth = 145;
                int visibleItemCapacity = int.Parse(Math.Round(Window.Current.Bounds.Width / averageItemWidth).ToString());
                int totalRibbonCommandItemsCount = RibbonItems.Count();
                var itemsToShow = RibbonItems.Take(visibleItemCapacity);
                var itemsToMakeCompact = RibbonItems.TakeLast(totalRibbonCommandItemsCount - itemsToShow.Count());
                foreach (RibbonCommandItem item in itemsToShow)
                {
                    item.DisplayMode = RibbonItemDisplayMode.Tall; // TODO: Implement Tall Ribbon Items AppSetting
                }

                foreach (RibbonCommandItem itemToCollapse in itemsToMakeCompact)
                {
                    itemToCollapse.DisplayMode = RibbonItemDisplayMode.Compact;
                }
            }


            if (Window.Current.Bounds.Width >= 800)
            {
                if (App.CurrentInstance != null)
                    App.CurrentInstance.NavigationControl.IsSearchReigonVisible = true;

                RibbonViewModel.ShowAppBarSeparator();
            }
            else
            {
                if (App.CurrentInstance != null)
                    App.CurrentInstance.NavigationControl.IsSearchReigonVisible = false;

                RibbonViewModel.HideAppBarSeparator();
            }
        }

        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                (sender as ListView).SelectedItem = null;
            }
        }

        private void KeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            (args.Element as AutoSuggestBox).Focus(FocusState.Programmatic);
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;
            var instanceTabsView = rootFrame.Content as InstanceTabsView;
            instanceTabsView.AddNewTab(typeof(Settings), "Settings");
        }

        private async void AddItem_Click(object sender, RoutedEventArgs e)
        {
            await App.addItemDialog.ShowAsync();
        }

        private async void LayoutButton_Click(object sender, RoutedEventArgs e)
        {
            await App.layoutDialog.ShowAsync();
        }

        public async void ShowPropertiesButton_Click(object sender, RoutedEventArgs e)
        {
            App.propertiesDialog.propertiesFrame.Tag = App.propertiesDialog;
            App.propertiesDialog.propertiesFrame.Navigate(typeof(Properties), (App.CurrentInstance.ContentPage as BaseLayout).SelectedItem, new SuppressNavigationTransitionInfo());
            await App.propertiesDialog.ShowAsync(ContentDialogPlacement.Popup);
        }

        private async void NewWindowButton_Click(object sender, RoutedEventArgs e)
        {
            var filesUWPUri = new Uri("files-uwp:");
            var options = new LauncherOptions()
            {
                DisplayApplicationPicker = false
            };
            await Launcher.LaunchUriAsync(filesUWPUri);
        }

        private void TabViewItem_Tapped(object sender, TappedRoutedEventArgs e)
        {
            FlyoutBase.ShowAttachedFlyout(sender as TabViewItem);
            //((sender as TabViewItem).Resources["FileClickFlyout"] as FlyoutPresenter).ShowAt((sender as TabViewItem));
        }

        private void MenuFlyout_Closed(object sender, object e)
        {
            HomeRibbonItem.IsSelected = true;
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Exit();
        }

        private void RibbonItem_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            var itemTapped = sender as TabViewItem;
            if(RibbonTabView.SelectedItem != null)
            {
                RibbonTabView.SelectedItem = null;
            }
            else
            {
                itemTapped.IsSelected = true;
            }
        }
    }
}

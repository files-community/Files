using Microsoft.Toolkit.Uwp.UI.Controls;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Files
{
    /// <summary>
    /// The Instance Tabs Component for Project Mumbai
    /// </summary>
    public sealed partial class InstanceTabsView : Page
    {
        public static ObservableCollection<InstanceTabItem> instanceTabs { get; set; } = new ObservableCollection<InstanceTabItem>();

        public InstanceTabsView()
        {
            this.InitializeComponent();
            ApplicationView.PreferredLaunchViewSize = new Size(1080, 630);
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;
            var CoreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            CoreTitleBar.ExtendViewIntoTitleBar = true;
            CoreTitleBar.LayoutMetricsChanged += CoreTitleBar_LayoutMetricsChanged; ;
            CoreTitleBar_LayoutMetricsChanged(CoreTitleBar, null);
            TabStrip.Loaded += TabStrip_Loaded;


            var titleBar = ApplicationView.GetForCurrentView().TitleBar;
            titleBar.ButtonInactiveBackgroundColor = Color.FromArgb(0, 255, 255, 255);
            titleBar.ButtonHoverBackgroundColor = Color.FromArgb(75, 10, 10, 10);
            if (App.Current.RequestedTheme == ApplicationTheme.Dark)
            {
                titleBar.ButtonBackgroundColor = Color.FromArgb(0, 0, 0, 0);
                titleBar.ButtonForegroundColor = Colors.White;
                titleBar.ButtonHoverBackgroundColor = Color.FromArgb(75, 240, 240, 240);
                titleBar.BackgroundColor = Color.FromArgb(255, 25, 25, 25);
            }
            else if (App.Current.RequestedTheme == ApplicationTheme.Light)
            {
                titleBar.ButtonBackgroundColor = Color.FromArgb(0, 255, 255, 255);
                titleBar.ButtonForegroundColor = Colors.Black;
                titleBar.ButtonHoverBackgroundColor = Color.FromArgb(75, 155, 155, 155);
            }

            if (this.RequestedTheme == ElementTheme.Dark)
            {
                titleBar.ButtonForegroundColor = Colors.White;
                titleBar.ButtonHoverBackgroundColor = Color.FromArgb(75, 240, 240, 240);
                titleBar.BackgroundColor = Color.FromArgb(255, 25, 25, 25);
            }
            else if (this.RequestedTheme == ElementTheme.Light)
            {
                titleBar.ButtonForegroundColor = Colors.Black;
                titleBar.ButtonHoverBackgroundColor = Color.FromArgb(75, 155, 155, 155);
                titleBar.BackgroundColor = Colors.Transparent;
            }
            //instanceTabs.Clear();

        }

        private void CoreTitleBar_LayoutMetricsChanged(CoreApplicationViewTitleBar sender, object args)
        {
            LeftPaddingColumn.Width = new GridLength((TabStrip.Items.Count * 150) + 32);
            RightPaddingColumn.Width = new GridLength(sender.SystemOverlayRightInset);
        }

        public static Grid ContentPresGrid { get; set; } = new Grid();

        private void TabStrip_Loaded(object sender, RoutedEventArgs e)
        {
            instanceTabs.CollectionChanged += InstanceTabs_CollectionChanged;
            Frame FirstFrame = new Frame();
            FirstFrame.Tag = 0;
            FirstFrame.Navigate(typeof(ProHome));
            ContentPresGrid.Children.Add(FirstFrame);
            instanceTabs.Add(new InstanceTabItem() { HeaderText = "Favorites", SourcePage = "ProHome", index = 0, TabContent = FirstFrame });
        }

        private void InstanceTabs_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            var CoreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            LeftPaddingColumn.Width = new GridLength((TabStrip.Items.Count * 250) + 32);
            RightPaddingColumn.Width = new GridLength(CoreTitleBar.SystemOverlayRightInset);
            //if(e.NewItems.Count > 0)
            //{
            //    List<TabViewItem> tabsFound = new List<TabViewItem>();
            //    Interacts.Interaction.FindChildren<TabViewItem>(tabsFound, InstanceTabsPage.Content as DependencyObject);

            //    List<Frame> frames = new List<Frame>();
            //    Interacts.Interaction.FindChildren<Frame>(frames, InstanceTabsPage.Content as DependencyObject);
            //    frames[0].Navigate((e.NewItems as List<InstanceTabItem>)[0].SourcePage);
            //}

        }



        private void NewTabButton_Click(object sender, RoutedEventArgs e)
        {
            Frame FirstFrame = new Frame();
            FirstFrame.Tag = 0;
            FirstFrame.Navigate(typeof(ProHome));
            ContentPresGrid.Children.Add(FirstFrame);
            instanceTabs.Add(new InstanceTabItem() { HeaderText = "Favorites", SourcePage = "ProHome", index = TabStrip.Items.Count, TabContent = FirstFrame });
            TabStrip.SelectedItem = instanceTabs[instanceTabs.Count - 1];
        }

        private void DragArea_Loaded(object sender, RoutedEventArgs e)
        {
            Window.Current.SetTitleBar((Grid)sender);
        }

        private void TabStrip_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            List<TabViewItem> tabsFound = new List<TabViewItem>();
            Interacts.Interaction.FindChildren<TabViewItem>(tabsFound, InstanceTabsPage.Content as DependencyObject);
            if ((e.AddedItems[0] as InstanceTabItem).SourcePage == "ProHome")
            {
                foreach(TabViewItem tvi in tabsFound)
                {
                    if ((e.AddedItems[0] as InstanceTabItem).index == TabStrip.Items.IndexOf(tvi))
                    {
                        // Applies below with selected Tab's information
                        foreach(Frame instance in ContentPresGrid.Children)
                        {
                            // hide all other opened tab's content unless the instance belongs to the selected tab
                            if( ((int) instance.Tag) != (e.AddedItems[0] as InstanceTabItem).index)
                            {
                                instance.Visibility = Visibility.Collapsed;
                            }
                            else
                            {
                                instance.Visibility = Visibility.Visible;
                            }
                        }
                        return;
                    }
                }
            }
        }

        private void TabStrip_TabClosing(object sender, TabClosingEventArgs e)
        {
            foreach(Frame instance in ContentPresGrid.Children)
            {
                if(((int) instance.Tag) == (e.Item as InstanceTabItem).index)
                {
                    ContentPresGrid.Children.Remove(instance);
                    return;
                }
            }
        }
    }

    public class InstanceTabItem
    {
        public string HeaderText { get; set; }
        public string SourcePage { get; set; }
        public int index { get; set; }
        public Frame TabContent { get; set; }


    }
}

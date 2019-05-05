using Microsoft.Toolkit.Uwp.UI.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
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
        public static TabView tabView;
        public static List<Type> types = new List<Type>();
        public InstanceTabsView()
        {
            this.InitializeComponent();
            ApplicationView.PreferredLaunchViewSize = new Size(1080, 630);
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;
            var CoreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            CoreTitleBar.ExtendViewIntoTitleBar = true;
            CoreTitleBar.LayoutMetricsChanged += CoreTitleBar_LayoutMetricsChanged;
            CoreTitleBar_LayoutMetricsChanged(CoreTitleBar, null);
            tabView = TabStrip;
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
            AddNewTab(typeof(ProHome), null);
        }

        private void CoreTitleBar_LayoutMetricsChanged(CoreApplicationViewTitleBar sender, object args)
        {
            //LeftPaddingColumn.Width = new GridLength((TabStrip.Items.Count * 150) + 32);
            //RightPaddingColumn.Width = new GridLength(sender.SystemOverlayRightInset);
        }

        public void AddNewTab(Type t, string path)
        {
            Frame frame = new Frame();
            frame.Navigate(t, path);
            string TabLocationHeader;
            if (path != null)
            {
                TabLocationHeader = Path.GetDirectoryName(path);
            }
            else
            {
                TabLocationHeader = "Favorites";
            }
            Grid gr = new Grid();
            gr.Children.Add(frame);
            gr.HorizontalAlignment = HorizontalAlignment.Stretch;
            gr.VerticalAlignment = VerticalAlignment.Stretch;
            TabViewItem tvi = new TabViewItem()
            {
                Header = TabLocationHeader,
                Content = gr,
                Width = 200
            };
            tvi.Loaded += Tvi_Loaded;
            TabStrip.Items.Add(tvi);

        }

        private void Tvi_Loaded(object sender, RoutedEventArgs e)
        {
            //tabContentPresenter.Content = ((TabStrip.SelectedItem as TabViewItem));
            TabStrip.SelectionChanged += TabStrip_SelectionChanged;
        }

        private void NewTabButton_Click(object sender, RoutedEventArgs e)
        {
            AddNewTab(typeof(ProHome), null);
            TabStrip.SelectedItem = TabStrip.Items[TabStrip.Items.Count - 1];
        }

        private void DragArea_Loaded(object sender, RoutedEventArgs e)
        {
            Window.Current.SetTitleBar(sender as Grid);
        }

        private void TabStrip_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //var g = ((TabStrip.SelectedItem as TabViewItem)).Content as DependencyObject;
            //tabContentView.Content = g;
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

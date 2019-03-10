using Microsoft.Toolkit.Uwp.UI.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace Files
{
    /// <summary>
    /// This is not finished yet. This is the work that was started on having multiple Tabs
    /// </summary>
    public sealed partial class ProHome : Page
    {
        ObservableCollection<Tab> tabList = new ObservableCollection<Tab>();
        public static ObservableCollection<Tab> TabList { get; set; } = new ObservableCollection<Tab>();
        public ProHome()
        {
            this.InitializeComponent();
            TabList.Clear();
            TabList.Add(new Tab() { TabName = "Home", TabContent = "local:MainPage" });
        }

        private void TabView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedTab = e.AddedItems as TabViewItem;
            
        }
    }

    public class Tab
    {
        public string TabName { get; set; }
        public string TabContent { get; set; }
    }
}

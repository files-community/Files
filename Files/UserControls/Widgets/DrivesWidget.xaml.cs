using Files.Filesystem;
using Files.View_Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Files
{
    public sealed partial class DrivesWidget : UserControl
    {
        public SettingsViewModel AppSettings => App.AppSettings;
        public delegate void DrivesWidgetInvokedEventHandler(object sender, DrivesWidgetInvokedEventArgs e);

        public event DrivesWidgetInvokedEventHandler DrivesWidgetInvoked;
        public static ObservableCollection<INavigationControlItem> itemsAdded = new ObservableCollection<INavigationControlItem>();

        public DrivesWidget()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string NavigationPath = ""; // path to navigate
            string ClickedCard = (sender as Button).Tag.ToString();

            NavigationPath = ClickedCard;

            DrivesWidgetInvoked?.Invoke(this, new DrivesWidgetInvokedEventArgs() { Path = NavigationPath, LayoutType = AppSettings.GetLayoutType() });
        }

        public class DrivesWidgetInvokedEventArgs : EventArgs
        {
            public Type LayoutType { get; set; }
            public string Path { get; set; }
        }
    }
}
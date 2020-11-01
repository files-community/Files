using Files.Filesystem;
using Files.View_Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Files
{
    public sealed partial class DrivesWidget : UserControl
    {
        public SettingsViewModel AppSettings => App.AppSettings;
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

            App.CurrentInstance.ContentFrame.Navigate(AppSettings.GetLayoutType(), NavigationPath);

            App.CurrentInstance.InstanceViewModel.IsPageTypeNotHome = true; // show controls that were hidden on the home page
        }
    }
}
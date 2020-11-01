using Files.View_Models;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Files
{
    public sealed partial class DrivesWidget : UserControl
    {
        public SettingsViewModel AppSettings => App.AppSettings;
        public static List<DrivesLocationItem> itemsAdded = new List<DrivesLocationItem>();

        public DrivesWidget()
        {
            InitializeComponent();
            foreach (var item in itemsAdded) { item.AutomationProperties = item.Text; }
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

    public class DrivesLocationItem
    {
        public string Icon { get; set; }
        public string SpaceText { get; set; }
        public double DriveUsedSpaceDoubleValue {get;set;}
        public double DriveCapacityDoubleValue {get;set;}
        public string Text { get; set; }
        public string Tag { get; set; }
        public string AutomationProperties { get; set; }
    }
}
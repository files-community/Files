using Files.Filesystem;
using Files.View_Models.Properties;
using Microsoft.Toolkit.Uwp.Helpers;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Files
{
    public sealed partial class PropertiesDetails : PropertiesTab
    {
        public PropertiesDetails()
        {
            this.InitializeComponent();
        }

        protected override void Properties_Loaded(object sender, RoutedEventArgs e)
        {
            base.Properties_Loaded(sender, e);
            
            if (BaseProperties != null)
            {
                Stopwatch stopwatch = Stopwatch.StartNew();
                (BaseProperties as FileProperties).GetSystemFileProperties();
                stopwatch.Stop();
                Debug.WriteLine(string.Format("System file properties were obtained in {0} milliseconds", stopwatch.ElapsedMilliseconds));
            }
        }

        private void SetOverviewVisibilities()
        {
            var name = ViewModel.ItemName.Split(".");
            var extension = name[name.Length - 1].ToLower();

            if (extension.Contains("png") || extension.Contains("jpg") || extension.Contains("gif") || extension.Contains("jpeg"))
                OverviewImage.Visibility = Visibility.Visible;
        }

        private string GetStringArray(object array)
        {
            if (array == null || !(array is string[]))
                return "";

            var str = "";
            foreach (var i in array as string[])
                str += string.Format("{0}; ", i);

            return str;
        }

        private void SetStringArray(string val, string key)
        {
            ViewModel.SystemFileProperties_RW[key] = val.Split("; ");
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            SetOverviewVisibilities();
        }

        private async void OpenMaps_Click(object sender, RoutedEventArgs e)
        {
            await Windows.System.Launcher.LaunchUriAsync(ViewModel.Geopoint != null ? new Uri(String.Format(@"bingmaps:?where={0}", ViewModel.Geopoint.Address.FormattedAddress)) : new Uri(String.Format(@"bingmaps:?cp={0}~{1}", ViewModel.Latitude, ViewModel.Longitude)),
                new Windows.System.LauncherOptions() { TargetApplicationPackageFamilyName = "Microsoft.WindowsMaps_8wekyb3d8bbwe" });
        }

        public async Task SaveChanges(ListedItem item)
        {
            await CoreApplication.MainView.ExecuteOnUIThreadAsync(() => (BaseProperties as FileProperties).SyncPropertyChanges());
        }

        private async void ClearPersonalInformation_Click(object sender, RoutedEventArgs e)
        {
            ClearPersonalInformationFlyout.Hide();
            await (BaseProperties as FileProperties).ClearPersonalInformation();
        }
    }
}
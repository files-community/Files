using Files.Common;
using Files.Helpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using static Files.Views.PropertiesCustomization;

namespace Files.Views
{
    public sealed partial class CustomFolderIcons : Page
    {
        private string selectedFolderPath = null;
        private string iconResourceItemPath = null;

        public CustomFolderIcons()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            selectedFolderPath = (e.Parameter as IconSelectorInfo).SelectedDirectory;
            iconResourceItemPath = (e.Parameter as IconSelectorInfo).InitialPath;
            ItemDisplayedPath.Text = iconResourceItemPath;
            var iconInfoCollection = (e.Parameter as IconSelectorInfo).Icons as List<IconFileInfo>;
            foreach (IconFileInfo iFInfo in iconInfoCollection)
            {
                iFInfo.IconDataBytes = Convert.FromBase64String(iFInfo.IconData);
            }
            IconSelectionGrid.ItemsSource = iconInfoCollection;
        }

        private async void PickDllButton_Click(object sender, RoutedEventArgs e)
        {
            Windows.Storage.Pickers.FileOpenPicker picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.ComputerFolder;
            picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;
            picker.FileTypeFilter.Add(".dll");
            picker.FileTypeFilter.Add(".ico");
            var file = await picker.PickSingleFileAsync();
            iconResourceItemPath = file.Path;
            ItemDisplayedPath.Text = iconResourceItemPath;

            LoadIconsForPath(file.Path);
        }

        private async void LoadIconsForPath(string path)
        {
            var connection = await AppServiceConnectionHelper.Instance;
            if (connection != null)
            {
                var (status, response) = await connection.SendMessageForResponseAsync(new ValueSet()
                {
                    { "Arguments", "GetFolderIconsFromDLL" },
                    { "iconFile", path }
                });
                if (status == AppServiceResponseStatus.Success && response.ContainsKey("IconInfos"))
                {
                    var icons = JsonConvert.DeserializeObject<IList<IconFileInfo>>(response["IconInfos"] as string);
                    foreach (IconFileInfo iFInfo in icons)
                    {
                        iFInfo.IconDataBytes = Convert.FromBase64String(iFInfo.IconData);
                    }
                    IconSelectionGrid.ItemsSource = icons;
                }
            }
        }

        private async void IconSelectionGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedIconInfo = (sender as GridView).SelectedItem as IconFileInfo;
            var connection = await AppServiceConnectionHelper.Instance;
            if (connection != null)
            {
                _ = await connection.SendMessageForResponseAsync(new ValueSet()
                {
                    {"Arguments", "SetCustomFolderIcon" },
                    {"iconIndex", selectedIconInfo.Index },
                    {"folder", selectedFolderPath },
                    {"iconFile", iconResourceItemPath }
                });
            }
        }
    }
}

using Files.Common;
using Files.Extensions;
using Files.Helpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using static Files.Views.PropertiesCustomization;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Files.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class CustomFolderIcons : Page
    {
        private NamedPipeAsAppServiceConnection serviceConnection = null;

        public CustomFolderIcons()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            ItemDisplayedPath.Text = (e.Parameter as IconSelectorInfo).InitialPath;
            serviceConnection = (e.Parameter as IconSelectorInfo).Connection;
            var iconInfoCollection = (e.Parameter as IconSelectorInfo).Icons as List<IconFileInfo>;
            foreach (IconFileInfo iFInfo in iconInfoCollection)
            {
                iFInfo.LoadImageFromModelString();
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
            ItemDisplayedPath.Text = file.Path;

            LoadIconsForPath(file.Path);
        }

        private async void LoadIconsForPath(string path)
        {
            var response = await serviceConnection?.SendMessageForResponseAsync(new ValueSet()
            {
                { "Arguments", "GetFolderIconsFromDLL" },
                { "iconFile", path }
            });

            var icons = JsonConvert.DeserializeObject<IList<IconFileInfo>>(response.Data["IconInfos"] as string);
            foreach (IconFileInfo iFInfo in icons)
            {
                iFInfo.LoadImageFromModelString();
            }
            IconSelectionGrid.ItemsSource = icons;
        }
    }
}

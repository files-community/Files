using Files.Common;
using Files.Helpers;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Uwp;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Core;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using static Files.Views.PropertiesCustomization;

namespace Files.Views
{
    public sealed partial class CustomFolderIcons : Page
    {
        private string selectedItemPath;
        private string iconResourceItemPath;
        private IShellPage appInstance;

        public ICommand RestoreDefaultIconCommand { get; private set; }
        public bool IsShortcutItem { get; private set; }

        public CustomFolderIcons()
        {
            this.InitializeComponent();
            RestoreDefaultIconCommand = new AsyncRelayCommand(RestoreDefaultIcon);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is IconSelectorInfo selectorInfo)
            {
                selectedItemPath = selectorInfo.SelectedItem;
                IsShortcutItem = selectorInfo.IsShortcut;
                iconResourceItemPath = selectorInfo.InitialPath;
                appInstance = selectorInfo.AppInstance;
                ItemDisplayedPath.Text = iconResourceItemPath;

                LoadIconsForPath(iconResourceItemPath);
            }
        }

        private async void PickDllButton_Click(object sender, RoutedEventArgs e)
        {
            Windows.Storage.Pickers.FileOpenPicker picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.ComputerFolder;
            picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;
            picker.FileTypeFilter.Add(".dll");
            picker.FileTypeFilter.Add(".exe");
            picker.FileTypeFilter.Add(".ico");

            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                iconResourceItemPath = file.Path;
                ItemDisplayedPath.Text = iconResourceItemPath;
                LoadIconsForPath(file.Path);
            }
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
                    if (icons != null)
                    {
                        foreach (IconFileInfo iFInfo in icons)
                        {
                            iFInfo.IconDataBytes = Convert.FromBase64String(iFInfo.IconData);
                        }
                    }
                    IconSelectionGrid.ItemsSource = icons;
                }
            }
        }

        private async void IconSelectionGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedIconInfo = (sender as GridView).SelectedItem as IconFileInfo;
            if (selectedIconInfo == null)
            {
                return;
            }
            var setIconTask = IsShortcutItem ?
                SetCustomFileIcon(selectedItemPath, iconResourceItemPath, selectedIconInfo.Index) :
                SetCustomFolderIcon(selectedItemPath, iconResourceItemPath, selectedIconInfo.Index);
            if (await setIconTask)
            {
                await CoreApplication.MainView.DispatcherQueue.EnqueueAsync(() =>
                {
                    appInstance?.FilesystemViewModel?.RefreshItems(null);
                });
            }
        }

        private async Task RestoreDefaultIcon()
        {
            var setIconTask = IsShortcutItem ?
                SetCustomFileIcon(selectedItemPath, null) :
                SetCustomFolderIcon(selectedItemPath, null);
            if (await setIconTask)
            {
                await CoreApplication.MainView.DispatcherQueue.EnqueueAsync(() =>
                {
                    appInstance?.FilesystemViewModel?.RefreshItems(null);
                });
            }
        }

        private async Task<bool> SetCustomFolderIcon(string folderPath, string iconFile, int iconIndex = 0)
        {
            var connection = await AppServiceConnectionHelper.Instance;
            if (connection != null)
            {
                var (status, response) = await connection.SendMessageForResponseAsync(new ValueSet()
                {
                    {"Arguments", "SetCustomFolderIcon" },
                    {"iconIndex", iconIndex },
                    {"folder", folderPath },
                    {"iconFile", iconFile }
                });
                return status == AppServiceResponseStatus.Success && response.Get("Success", false);
            }
            return false;
        }

        private async Task<bool> SetCustomFileIcon(string filePath, string iconFile, int iconIndex = 0)
        {
            var connection = await AppServiceConnectionHelper.Instance;
            if (connection != null)
            {
                var (status, response) = await connection.SendMessageForResponseAsync(new ValueSet()
                {
                    {"Arguments", "FileOperation" },
                    {"fileop", "SetLinkIcon" },
                    {"iconIndex", iconIndex },
                    {"filepath", filePath },
                    {"iconFile", iconFile }
                });
                return status == AppServiceResponseStatus.Success && response.Get("Success", false);
            }
            return false;
        }
    }
}

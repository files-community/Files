using Files.Shared;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace Files.Uwp.Helpers
{
    public static class UIHelpers
    {
        public static async Task<ContentDialogResult> TryShowAsync(this ContentDialog dialog)
        {
            try
            {
                return await dialog.ShowAsync();
            }
            catch // A content dialog is already open
            {
                return ContentDialogResult.None;
            }
        }

        public static void CloseAllDialogs()
        {
            var openedDialogs = VisualTreeHelper.GetOpenPopups(Window.Current);

            foreach (var item in openedDialogs)
            {
                if (item.Child is ContentDialog dialog)
                {
                    dialog.Hide();
                }
            }
        }

        private static async Task<IList<IconFileInfo>> LoadSelectedIconsAsync(string filePath, IList<int> indexes, int iconSize = 48)
        {
            var connection = await AppServiceConnectionHelper.Instance;
            if (connection != null)
            {
                var value = new ValueSet
                {
                    { "Arguments", "GetSelectedIconsFromDLL" },
                    { "iconFile", filePath },
                    { "requestedIconSize", iconSize },
                    { "iconIndexes", JsonConvert.SerializeObject(indexes) }
                };
                var (status, response) = await connection.SendMessageForResponseAsync(value);
                if (status == AppServiceResponseStatus.Success)
                {
                    var icons = JsonConvert.DeserializeObject<IList<IconFileInfo>>((string)response["IconInfos"]);
                    if (icons != null)
                    {
                        foreach (IconFileInfo iFInfo in icons)
                        {
                            iFInfo.IconDataBytes = Convert.FromBase64String(iFInfo.IconData);
                        }
                    }

                    return icons;
                }
            }
            return null;
        }

        private static Task<IEnumerable<IconFileInfo>> IconResources = UIHelpers.LoadSidebarIconResources();

        public static async Task<IconFileInfo> GetIconResourceInfo(int index)
        {
            var icons = await UIHelpers.IconResources;
            if (icons != null)
            {
                return icons.FirstOrDefault(x => x.Index == index);
            }
            return null;
        }

        public static async Task<BitmapImage> GetIconResource(int index)
        {
            var iconInfo = await GetIconResourceInfo(index);
            if (iconInfo != null)
            {
                return await iconInfo.IconDataBytes.ToBitmapAsync();
            }
            return null;
        }

        private static async Task<IEnumerable<IconFileInfo>> LoadSidebarIconResources()
        {
            string imageres = Path.Combine(CommonPaths.SystemRootPath, "System32", "imageres.dll");
            var imageResList = await UIHelpers.LoadSelectedIconsAsync(imageres, new List<int>() {
                    Constants.ImageRes.RecycleBin,
                    Constants.ImageRes.NetworkDrives,
                    Constants.ImageRes.Libraries,
                    Constants.ImageRes.ThisPC,
                    Constants.ImageRes.CloudDrives,
                    Constants.ImageRes.Folder
                }, 32);

            return imageResList;
        }
    }
}
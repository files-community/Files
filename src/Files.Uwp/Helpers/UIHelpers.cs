using Files.Shared;
using Files.Shared.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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

        private static async Task<IList<IconFileInfo>> LoadSelectedIconsAsync(string filePath, int[] indexes, int iconSize = 48)
        {
            return await Task.Run(() => SafetyExtensions.IgnoreExceptions(() => NativeFileOperationsHelper.GetSelectedIconsFromDLL(filePath, iconSize, indexes), App.Logger));
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
            const string imageres = @"C:\Windows\SystemResources\imageres.dll.mun";
            var imageResList = await UIHelpers.LoadSelectedIconsAsync(imageres, new[] {
                    Constants.ImageRes.RecycleBin,
                    Constants.ImageRes.NetworkDrives,
                    Constants.ImageRes.Libraries,
                    Constants.ImageRes.ThisPC,
                    Constants.ImageRes.CloudDrives,
                    Constants.ImageRes.Folder
                }, 32);

            const string shell32 = @"C:\Windows\SystemResources\shell32.dll.mun";
            var shell32List = await UIHelpers.LoadSelectedIconsAsync(shell32, new[] {
                    Constants.Shell32.QuickAccess
                }, 32);

            if (shell32List != null && imageResList != null)
            {
                return imageResList.Concat(shell32List);
            }
            else if (shell32List != null && imageResList == null)
            {
                return shell32List;
            }
            else if (shell32List == null && imageResList != null)
            {
                return imageResList;
            }
            else
            {
                return null;
            }
        }
    }
}
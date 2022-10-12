using Files.Shared;
using Files.Shared.Extensions;
using Files.App.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;
using Windows.Storage;
using Vanara.PInvoke;
using Files.App.Filesystem;

namespace Files.App.Helpers
{
    public static class RecycleBinHelpers
    {
        #region Private Members

        private static readonly JsonElement defaultJson = JsonSerializer.SerializeToElement("{}");

        private static readonly Regex recycleBinPathRegex = new Regex(@"^[A-Z]:\\\$Recycle\.Bin\\", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        #endregion Private Members

        public static async Task<List<ShellFileItem>> EnumerateRecycleBin()
        {
            return (await Win32Shell.GetShellFolderAsync(CommonPaths.RecycleBinPath, "Enumerate", 0, int.MaxValue)).Enumerate;
        }

        public static async Task<bool> IsRecycleBinItem(IStorageItem item)
        {
            List<ShellFileItem> recycleBinItems = await EnumerateRecycleBin();
            return recycleBinItems.Any((shellItem) => shellItem.RecyclePath.Equals(item.Path));
        }

        public static async Task<bool> IsRecycleBinItem(string path)
        {
            List<ShellFileItem> recycleBinItems = await EnumerateRecycleBin();
            return recycleBinItems.Any((shellItem) => shellItem.RecyclePath.Equals(path));
        }

        public static bool IsPathUnderRecycleBin(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }
            return recycleBinPathRegex.IsMatch(path);
        }

        public static async Task EmptyRecycleBin()
        {
            var ConfirmEmptyBinDialog = new ContentDialog()
            {
                Title = "ConfirmEmptyBinDialogTitle".GetLocalizedResource(),
                Content = "ConfirmEmptyBinDialogContent".GetLocalizedResource(),
                PrimaryButtonText = "Yes".GetLocalizedResource(),
                SecondaryButtonText = "Cancel".GetLocalizedResource(),
                DefaultButton = ContentDialogButton.Primary
            };

            ContentDialogResult result = await UIHelpers.SetContentDialogRoot(ConfirmEmptyBinDialog).ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                Shell32.SHEmptyRecycleBin(IntPtr.Zero, null, Shell32.SHERB.SHERB_NOCONFIRMATION | Shell32.SHERB.SHERB_NOPROGRESSUI);
            }
        }

        public static async Task RestoreRecycleBin()
        {
            var ConfirmEmptyBinDialog = new ContentDialog()
            {
                Title = "ConfirmRestoreBinDialogTitle".GetLocalizedResource(),
                Content = "ConfirmRestoreBinDialogContent".GetLocalizedResource(),
                PrimaryButtonText = "Yes".GetLocalizedResource(),
                SecondaryButtonText = "Cancel".GetLocalizedResource(),
                DefaultButton = ContentDialogButton.Primary
            };

            ContentDialogResult result = await UIHelpers.SetContentDialogRoot(ConfirmEmptyBinDialog).ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                associatedInstance.SlimContentPage.ItemManipulationModel.SelectAllItems();
                await RestoreItemAsync();
            }
        }

        public static async Task RestoreSelectionRecycleBin()
        {
            var ConfirmEmptyBinDialog = new ContentDialog()
            {
                Title = "ConfirmRestoreSelectionBinDialogTitle".GetLocalizedResource(),
                Content = string.Format("ConfirmRestoreSelectionBinDialogContent".GetLocalizedResource(), associatedInstance.SlimContentPage.SelectedItems.Count),
                PrimaryButtonText = "Yes".GetLocalizedResource(),
                SecondaryButtonText = "Cancel".GetLocalizedResource(),
                DefaultButton = ContentDialogButton.Primary
            };

            ContentDialogResult result = await UIHelpers.SetContentDialogRoot(ConfirmEmptyBinDialog).ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                await RestoreItemAsync();
            }
        }

        public static async Task<bool> HasRecycleBin(string path)
        {
            if (string.IsNullOrEmpty(path) || path.StartsWith(@"\\?\", StringComparison.Ordinal))
            {
                return false;
            }
            var connection = await AppServiceConnectionHelper.Instance;
            if (connection != null)
            {
                var (status, response) = await connection.SendMessageForResponseAsync(new ValueSet()
                {
                    { "Arguments", "FileOperation" },
                    { "fileop", "TestRecycle" },
                    { "filepath", path }
                });
                var result = status == AppServiceResponseStatus.Success && response.Get("Success", defaultJson).GetBoolean();
                var shellOpResult = JsonSerializer.Deserialize<ShellOperationResult>(response.Get("Result", defaultJson).GetString());
                result &= shellOpResult != null && shellOpResult.Items.All(x => x.Succeeded);
                return result;
            }
            return false;
        }

        public static bool RecycleBinHasItems()
        {
            return Win32Shell.QueryRecycleBin().NumItems > 0;
        }

        private static Task RestoreItemAsync()
        {
            var items = associatedInstance.SlimContentPage.SelectedItems.ToList().Where(x => x is RecycleBinItem).Select((item) => new
            {
                Source = StorageHelpers.FromPathAndType(
                    item.ItemPath,
                    item.PrimaryItemAttribute == StorageItemTypes.File ? FilesystemItemType.File : FilesystemItemType.Directory),
                Dest = ((RecycleBinItem)item).ItemOriginalPath
            });
            return FilesystemHelpers.RestoreItemsFromTrashAsync(items.Select(x => x.Source), items.Select(x => x.Dest), true);
        }
        
        public static Task DeleteItemAsync()
        {
            var items = associatedInstance.SlimContentPage.SelectedItems.ToList().Select((item) => StorageHelpers.FromPathAndType(
                item.ItemPath,
                item.PrimaryItemAttribute == StorageItemTypes.File ? FilesystemItemType.File : FilesystemItemType.Directory));
            return FilesystemHelpers.DeleteItemsAsync(items, true, false, true);
        }
    }
}
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
using Microsoft.UI.Xaml.Controls;
using Files.App.Shell;
using Vanara.PInvoke;
using Files.App.Filesystem;

namespace Files.App.Helpers
{
    public static class RecycleBinHelpers
    {
        #region Private Members

        private static readonly JsonElement defaultJson = JsonSerializer.SerializeToElement("{}");

        private static readonly Regex recycleBinPathRegex = new Regex(@"^[A-Z]:\\\$Recycle\.Bin\\", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private static Task<NamedPipeAsAppServiceConnection> ServiceConnection => AppServiceConnectionHelper.Instance;

        #endregion Private Members

        public static event EventHandler? RecycleBinChanged;

        public static async Task<List<ShellFileItem>> EnumerateRecycleBin()
        {
            return (await Win32Shell.GetShellFolderAsync(CommonPaths.RecycleBinPath, "Enumerate", 0, int.MaxValue)).Enumerate;
        }

        public static async Task<bool> IsRecycleBinItem(IStorageItem item)
        {
            List<ShellFileItem> recycleBinItems = await EnumerateRecycleBin();
            return recycleBinItems.Any((shellItem) => shellItem.RecyclePath == item.Path);
        }

        public static async Task<bool> IsRecycleBinItem(string path)
        {
            List<ShellFileItem> recycleBinItems = await EnumerateRecycleBin();
            return recycleBinItems.Any((shellItem) => shellItem.RecyclePath == path);
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

            ContentDialogResult result = await SetContentDialogRoot(ConfirmEmptyBinDialog).ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                Shell32.SHEmptyRecycleBin(IntPtr.Zero, null, Shell32.SHERB.SHERB_NOCONFIRMATION | Shell32.SHERB.SHERB_NOPROGRESSUI);
                RaiseRecycleBinChangedEvent();
            }
        }

        public static async Task RestoreRecycleBin(IShellPage associatedInstance)
        {
            var ConfirmEmptyBinDialog = new ContentDialog()
            {
                Title = "ConfirmRestoreBinDialogTitle".GetLocalizedResource(),
                Content = "ConfirmRestoreBinDialogContent".GetLocalizedResource(),
                PrimaryButtonText = "Yes".GetLocalizedResource(),
                SecondaryButtonText = "Cancel".GetLocalizedResource(),
                DefaultButton = ContentDialogButton.Primary
            };

            ContentDialogResult result = await SetContentDialogRoot(ConfirmEmptyBinDialog).ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                associatedInstance.SlimContentPage.ItemManipulationModel.SelectAllItems();
                await RestoreItem(associatedInstance);
            }
        }

        public static async Task RestoreSelectionRecycleBin(IShellPage associatedInstance)
        {
            var ConfirmEmptyBinDialog = new ContentDialog()
            {
                Title = "ConfirmRestoreSelectionBinDialogTitle".GetLocalizedResource(),
                Content = string.Format("ConfirmRestoreSelectionBinDialogContent".GetLocalizedResource(), associatedInstance.SlimContentPage.SelectedItems.Count),
                PrimaryButtonText = "Yes".GetLocalizedResource(),
                SecondaryButtonText = "Cancel".GetLocalizedResource(),
                DefaultButton = ContentDialogButton.Primary
            };

            ContentDialogResult result = await SetContentDialogRoot(ConfirmEmptyBinDialog).ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                await RestoreItem(associatedInstance);
            }
        }

        //WINUI3
        private static ContentDialog SetContentDialogRoot(ContentDialog contentDialog)
        {
            if (Windows.Foundation.Metadata.ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
            {
                contentDialog.XamlRoot = App.Window.Content.XamlRoot;
            }
            return contentDialog;
        }

        public static async Task<bool> HasRecycleBin(string path)
        {
            if (string.IsNullOrEmpty(path) || path.StartsWith(@"\\?\", StringComparison.Ordinal))
                return false;

            var result = await FileOperationsHelpers.TestRecycleAsync(path.Split("|"));

            return result.Item1 &= result.Item2 != null && result.Item2.Items.All(x => x.Succeeded);
        }

        public static bool RecycleBinHasItems()
        {
            return Win32Shell.QueryRecycleBin().NumItems > 0;
        }

        public static async Task RestoreItem(IShellPage associatedInstance)
        {
            var items = associatedInstance.SlimContentPage.SelectedItems.ToList().Where(x => x is RecycleBinItem).Select((item) => new
            {
                Source = StorageHelpers.FromPathAndType(
                    item.ItemPath,
                    item.PrimaryItemAttribute == StorageItemTypes.File ? FilesystemItemType.File : FilesystemItemType.Directory),
                Dest = ((RecycleBinItem)item).ItemOriginalPath
            });
            await associatedInstance.FilesystemHelpers.RestoreItemsFromTrashAsync(items.Select(x => x.Source), items.Select(x => x.Dest), true);
        }

        public static async Task DeleteItem(IShellPage associatedInstance)
        {
            var items = associatedInstance.SlimContentPage.SelectedItems.ToList().Select((item) => StorageHelpers.FromPathAndType(
                item.ItemPath,
                item.PrimaryItemAttribute == StorageItemTypes.File ? FilesystemItemType.File : FilesystemItemType.Directory));
            await associatedInstance.FilesystemHelpers.DeleteItemsAsync(items, true, false, true);
        }

        public static void RaiseRecycleBinChangedEvent()
        {
			RecycleBinChanged?.Invoke(null, null);
		}
    }
}
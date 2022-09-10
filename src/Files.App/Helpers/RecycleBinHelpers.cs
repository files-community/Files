using Files.Shared;
using Files.Shared.Extensions;
using Files.App.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
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
    public class RecycleBinHelpers
    {
        #region Private Members

        private static readonly Regex recycleBinPathRegex = new Regex(@"^[A-Z]:\\\$Recycle\.Bin\\", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private Task<NamedPipeAsAppServiceConnection> ServiceConnection => AppServiceConnectionHelper.Instance;

        #endregion Private Members

        public async Task<List<ShellFileItem>> EnumerateRecycleBin()
        {
            return (await Win32Shell.GetShellFolderAsync(CommonPaths.RecycleBinPath, "Enumerate", 0, int.MaxValue)).Enumerate;
        }

        public async Task<bool> IsRecycleBinItem(IStorageItem item)
        {
            List<ShellFileItem> recycleBinItems = await EnumerateRecycleBin();
            return recycleBinItems.Any((shellItem) => shellItem.RecyclePath == item.Path);
        }

        public async Task<bool> IsRecycleBinItem(string path)
        {
            List<ShellFileItem> recycleBinItems = await EnumerateRecycleBin();
            return recycleBinItems.Any((shellItem) => shellItem.RecyclePath == path);
        }

        public bool IsPathUnderRecycleBin(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }
            return recycleBinPathRegex.IsMatch(path);
        }

        public static async Task S_EmptyRecycleBin()
        {
            await new RecycleBinHelpers().EmptyRecycleBin();
        }

        public async Task EmptyRecycleBin()
        {
            var ConfirmEmptyBinDialog = new ContentDialog()
            {
                Title = "ConfirmEmptyBinDialogTitle".GetLocalizedResource(),
                Content = "ConfirmEmptyBinDialogContent".GetLocalizedResource(),
                PrimaryButtonText = "Yes".GetLocalizedResource(),
                SecondaryButtonText = "Cancel".GetLocalizedResource(),
                DefaultButton = ContentDialogButton.Primary
            };

            ContentDialogResult result = await this.SetContentDialogRoot(ConfirmEmptyBinDialog).ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                Shell32.SHEmptyRecycleBin(IntPtr.Zero, null, Shell32.SHERB.SHERB_NOCONFIRMATION | Shell32.SHERB.SHERB_NOPROGRESSUI);
            }
        }

        //WINUI3
        private ContentDialog SetContentDialogRoot(ContentDialog contentDialog)
        {
            if (Windows.Foundation.Metadata.ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
            {
                contentDialog.XamlRoot = App.Window.Content.XamlRoot;
            }
            return contentDialog;
        }

        public async Task<bool> HasRecycleBin(string path)
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
                var result = status == AppServiceResponseStatus.Success && response.Get("Success", false);
                var shellOpResult = JsonConvert.DeserializeObject<ShellOperationResult>(response.Get("Result", ""));
                result &= shellOpResult != null && shellOpResult.Items.All(x => x.Succeeded);
                return result;
            }
            return false;
        }

        public bool RecycleBinHasItems()
        {
            return Win32Shell.QueryRecycleBin().NumItems > 0;
        }

        public static async Task S_RestoreItem(IShellPage associatedInstance)
        {
            await new RecycleBinHelpers().RestoreItem(associatedInstance);
        }

        private async Task RestoreItem(IShellPage associatedInstance)
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

        public static async Task S_DeleteItem(IShellPage associatedInstance)
        {
            await new RecycleBinHelpers().DeleteItem(associatedInstance);
        }

        public virtual async Task DeleteItem(IShellPage associatedInstance)
        {
            var items = associatedInstance.SlimContentPage.SelectedItems.ToList().Select((item) => StorageHelpers.FromPathAndType(
                item.ItemPath,
                item.PrimaryItemAttribute == StorageItemTypes.File ? FilesystemItemType.File : FilesystemItemType.Directory));
            await associatedInstance.FilesystemHelpers.DeleteItemsAsync(items, true, false, true);
        }
    }
}
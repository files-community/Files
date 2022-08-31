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
                var connection = await ServiceConnection;
                if (connection != null)
                {
                    var value = new ValueSet()
                    {
                        { "Arguments", "RecycleBin" },
                        { "action", "Empty" }
                    };

                    // Send request to fulltrust process to empty Recycle Bin
                    await connection.SendMessageAsync(value);
                }
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

        public async Task<bool> RecycleBinHasItems()
        {
            var recycleBinHasItems = false;
            var connection = await AppServiceConnectionHelper.Instance;
            if (connection != null)
            {
                var value = new ValueSet
                {
                    { "Arguments", "RecycleBin" },
                    { "action", "Query" }
                };
                var (status, response) = await connection.SendMessageForResponseAsync(value);
                if (status == AppServiceResponseStatus.Success && response.TryGetValue("NumItems", out var numItems))
                {
                    recycleBinHasItems = (long)numItems > 0;
                }
            }
            return recycleBinHasItems;
        }
    }
}
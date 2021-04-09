using Files.Common;
using Microsoft.Toolkit.Uwp;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml.Controls;

namespace Files.Helpers
{
    public class RecycleBinHelpers : IDisposable
    {
        #region Private Members

        private static readonly Regex recycleBinPathRegex = new Regex(@"\w:\\\$Recycle\.Bin\\.*", RegexOptions.IgnoreCase);

        private IShellPage associatedInstance;

        private NamedPipeAsAppServiceConnection Connection => associatedInstance?.ServiceConnection;

        #endregion Private Members

        public RecycleBinHelpers(IShellPage associatedInstance)
        {
            this.associatedInstance = associatedInstance;
        }

        public async Task<List<ShellFileItem>> EnumerateRecycleBin()
        {
            if (Connection != null)
            {
                ValueSet value = new ValueSet
                {
                    { "Arguments", "ShellFolder" },
                    { "action", "Enumerate" },
                    { "folder", App.AppSettings.RecycleBinPath }
                };
                var (status, response) = await Connection.SendMessageForResponseAsync(value);

                if (status == AppServiceResponseStatus.Success
                    && response.ContainsKey("Enumerate"))
                {
                    List<ShellFileItem> items = JsonConvert.DeserializeObject<List<ShellFileItem>>((string)response["Enumerate"]);
                    return items;
                }
            }

            return null;
        }

        public async Task<bool> IsRecycleBinItem(IStorageItem item)
        {
            List<ShellFileItem> recycleBinItems = await EnumerateRecycleBin();

            if (recycleBinItems == null)
            {
                return false;
            }

            return recycleBinItems.Any((shellItem) => shellItem.RecyclePath == item.Path);
        }

        public async Task<bool> IsRecycleBinItem(string path)
        {
            List<ShellFileItem> recycleBinItems = await EnumerateRecycleBin();

            if (recycleBinItems == null)
            {
                return false;
            }

            return recycleBinItems.Any((shellItem) => shellItem.RecyclePath == path);
        }

        public bool IsPathUnderRecycleBin(string path)
        {
            return recycleBinPathRegex.IsMatch(path);
        }

        public static void EmptyRecycleBin(IShellPage associatedInstance)
        {
            new RecycleBinHelpers(associatedInstance).EmptyRecycleBin();
        }

        public async void EmptyRecycleBin()
        {
            var ConfirmEmptyBinDialog = new ContentDialog()
            {
                Title = "ConfirmEmptyBinDialogTitle".GetLocalized(),
                Content = "ConfirmEmptyBinDialogContent".GetLocalized(),
                PrimaryButtonText = "ConfirmEmptyBinDialog/PrimaryButtonText".GetLocalized(),
                SecondaryButtonText = "ConfirmEmptyBinDialog/SecondaryButtonText".GetLocalized()
            };

            ContentDialogResult result = await ConfirmEmptyBinDialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                if (Connection != null)
                {
                    var value = new ValueSet();
                    value.Add("Arguments", "RecycleBin");
                    value.Add("action", "Empty");
                    // Send request to fulltrust process to empty recyclebin
                    await Connection.SendMessageAsync(value);
                }
            }
        }

        #region IDisposable

        public void Dispose()
        {
            associatedInstance = null;
        }

        #endregion IDisposable
    }
}
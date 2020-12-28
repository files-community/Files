using Files.Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;
using Windows.Storage;

namespace Files.Helpers
{
    public class RecycleBinHelpers : IDisposable
    {
        #region Private Members
        private static readonly Regex RECYCLE_BIN_PATH_REGEX = new Regex(@"\w:\\\$Recycle\.Bin\\.*");

        private IShellPage associatedInstance;

        private AppServiceConnection Connection => associatedInstance?.ServiceConnection;

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
                    { "Arguments", "RecycleBin" },
                    { "action", "Enumerate" }
                };
                AppServiceResponse response = await Connection.SendMessageAsync(value);

                if (response.Status == AppServiceResponseStatus.Success
                    && response.Message.ContainsKey("Enumerate"))
                {
                    List<ShellFileItem> items = JsonConvert.DeserializeObject<List<ShellFileItem>>((string)response.Message["Enumerate"]);
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
            return RECYCLE_BIN_PATH_REGEX.IsMatch(path);
        }

        #region IDisposable

        public void Dispose()
        {
            Connection?.Dispose();
            associatedInstance?.Dispose();

            associatedInstance = null;
        }

        #endregion IDisposable
    }
}
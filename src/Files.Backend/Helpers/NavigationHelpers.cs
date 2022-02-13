using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.System;

namespace Files.Backend.Helpers
{
    public static class NavigationHelpers
    {
        // TODO: Make this helper function rely on a collection of items rather than a tab instance
        //public static async void OpenSelectedItems(List<ListedItem>, bool openViaApplicationPicker = false)
        //{
        //    if (associatedInstance.FilesystemViewModel.WorkingDirectory.StartsWith(CommonPaths.RecycleBinPath, StringComparison.Ordinal))
        //    {
        //        // Do not open files and folders inside the recycle bin
        //        return;
        //    }
        //    if (associatedInstance.SlimContentPage == null)
        //    {
        //        return;
        //    }

        //    bool forceOpenInNewTab = false;
        //    var selectedItems = associatedInstance.SlimContentPage.SelectedItems.ToList();
        //    var opened = false;

        //    if (!openViaApplicationPicker &&
        //        selectedItems.Count > 1 &&
        //        selectedItems.All(x => x.PrimaryItemAttribute == StorageItemTypes.File && !x.IsExecutable && !x.IsShortcutItem))
        //    {
        //        // Multiple files are selected, open them together
        //        opened = await Win32Helpers.InvokeWin32ComponentAsync(string.Join('|', selectedItems.Select(x => x.ItemPath)), associatedInstance);
        //    }
        //    if (!opened)
        //    {
        //        foreach (ListedItem item in selectedItems)
        //        {
        //            var type = item.PrimaryItemAttribute == StorageItemTypes.Folder ?
        //                FilesystemItemType.Directory : FilesystemItemType.File;

        //            await OpenPath(item.ItemPath, associatedInstance, type, false, openViaApplicationPicker, forceOpenInNewTab: forceOpenInNewTab);

        //            if (type == FilesystemItemType.Directory)
        //            {
        //                forceOpenInNewTab = true;
        //            }
        //        }
        //    }
        //}
    }
}

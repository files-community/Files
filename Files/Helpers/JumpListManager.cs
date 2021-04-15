using Files.Common;
using Files.Filesystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.StartScreen;

namespace Files.Helpers
{
    public sealed class JumpListManager
    {
        private JumpList instance = null;
        private List<string> JumpListItemPaths { get; set; }

        public JumpListManager()
        {
            Initialize();
        }

        private async void Initialize()
        {
            try
            {
                if (JumpList.IsSupported())
                {
                    instance = await JumpList.LoadCurrentAsync();

                    // Disable automatic jumplist. It doesn't work with Files UWP.
                    instance.SystemGroupKind = JumpListSystemGroupKind.None;
                    JumpListItemPaths = instance.Items.Select(item => item.Arguments).ToList();
                }
            }
            catch (Exception ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Warn(ex, ex.Message);
                instance = null;
            }
        }

        public async void AddFolderToJumpList(string path)
        {
            // Saving to jumplist may fail randomly with error: ERROR_UNABLE_TO_REMOVE_REPLACED
            // In that case app should just catch the error and proceed as usual
            try
            {
                AddFolder(path);
                await instance.SaveAsync();
            }
            catch (COMException ex)
            {
                if (ex.ErrorCode != 80070497)
                {
                    throw ex;
                }
            }
        }

        private void AddFolder(string path)
        {
            if (instance != null && !JumpListItemPaths.Contains(path))
            {
                string displayName;
                if (path.Equals(App.AppSettings.DesktopPath, StringComparison.OrdinalIgnoreCase))
                {
                    displayName = "ms-resource:///Resources/SidebarDesktop";
                }
                else if (path.Equals(App.AppSettings.DownloadsPath, StringComparison.OrdinalIgnoreCase))
                {
                    displayName = "ms-resource:///Resources/SidebarDownloads";
                }
                else if (path.Equals(App.AppSettings.RecycleBinPath, StringComparison.OrdinalIgnoreCase))
                {
                    var localSettings = ApplicationData.Current.LocalSettings;
                    displayName = localSettings.Values.Get("RecycleBin_Title", "Recycle Bin");
                }
                else if (App.LibraryManager.TryGetLibrary(path, out LibraryLocationItem library))
                {
                    var libName = Path.GetFileNameWithoutExtension(library.Path);
                    displayName = libName switch
                    {
                        "Documents" or "Pictures" or "Music" or "Videos" => $"ms-resource:///Resources/Sidebar{libName}", // Use localized name
                        _ => library.Text, // Use original name
                    };
                }
                else
                {
                    displayName = Path.GetFileName(path);
                }

                var jumplistItem = JumpListItem.CreateWithArguments(path, displayName);
                jumplistItem.Description = path;
                jumplistItem.GroupName = Constants.JumpListConstants.RecentsGroupName;
                jumplistItem.Logo = Constants.JumpListConstants.RecentsItemIcon;
                instance.Items.Add(jumplistItem);
                if (!JumpListItemPaths.Contains(path))
                {
                    JumpListItemPaths.Add(path);
                }
            }
        }

        public async void RemoveFolder(string path)
        {
            // Updating the jumplist may fail randomly with error: FileLoadException: File in use
            // In that case app should just catch the error and proceed as usual
            try
            {
                if (JumpListItemPaths.Contains(path))
                {
                    JumpListItemPaths.Remove(path);
                    await UpdateAsync();
                }
            }
            catch (FileLoadException)
            {
            }
        }

        private async Task UpdateAsync()
        {
            if (instance != null)
            {
                // Clear all items to avoid localization issues
                instance?.Items.Clear();

                foreach (string path in JumpListItemPaths)
                {
                    AddFolder(path);
                }

                await instance.SaveAsync();
            }
        }
    }
}
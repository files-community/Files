using Files.Common;
using Files.Filesystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            JumpListItemPaths = new List<string>();
        }

        public async Task InitializeAsync()
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
                App.Logger.Warn(ex, ex.Message);
                instance = null;
            }
        }

        public async void AddFolderToJumpList(string path)
        {
            // Saving to jumplist may fail randomly with error: ERROR_UNABLE_TO_REMOVE_REPLACED
            // In that case app should just catch the error and proceed as usual
            try
            {
                if (instance != null)
                {
                    AddFolder(path);
                    await instance.SaveAsync();
                }
            }
            catch { }
        }

        private void AddFolder(string path)
        {
            if (instance != null)
            {
                string displayName = null;
                if (path.EndsWith("\\"))
                {
                    // Jumplist item argument can't end with a slash so append a character that can't exist in a directory name to support listing drives.
                    var drive = App.DrivesManager.Drives.Where(drive => drive.Path == path).FirstOrDefault();
                    if (drive == null)
                    {
                        return;
                    }

                    displayName = drive.Text;
                    path += '?';
                }

                if (displayName == null)
                {
                    if (path.Equals(CommonPaths.DesktopPath, StringComparison.OrdinalIgnoreCase))
                    {
                        displayName = "ms-resource:///Resources/SidebarDesktop";
                    }
                    else if (path.Equals(CommonPaths.DownloadsPath, StringComparison.OrdinalIgnoreCase))
                    {
                        displayName = "ms-resource:///Resources/SidebarDownloads";
                    }
                    else if (path.Equals(CommonPaths.RecycleBinPath, StringComparison.OrdinalIgnoreCase))
                    {
                        var localSettings = ApplicationData.Current.LocalSettings;
                        displayName = localSettings.Values.Get("RecycleBin_Title", "Recycle Bin");
                    }
                    else if (App.LibraryManager.TryGetLibrary(path, out LibraryLocationItem library))
                    {
                        var libName = Path.GetFileNameWithoutExtension(library.Path);
                        switch (libName)
                        {
                            case "Documents":
                            case "Pictures":
                            case "Music":
                            case "Videos":
                                // Use localized name
                                displayName = $"ms-resource:///Resources/Sidebar{libName}";
                                break;

                            default:
                                // Use original name
                                displayName = library.Text;
                                break;
                        }
                    }
                    else
                    {
                        displayName = Path.GetFileName(path);
                    }
                }

                var jumplistItem = JumpListItem.CreateWithArguments(path, displayName);
                jumplistItem.Description = jumplistItem.Arguments;
                jumplistItem.GroupName = "ms-resource:///Resources/JumpListRecentGroupHeader";
                jumplistItem.Logo = new Uri("ms-appx:///Assets/FolderIcon.png");

                // Keep newer items at the top.
                instance.Items.Remove(instance.Items.FirstOrDefault(x => x.Arguments.Equals(path, StringComparison.OrdinalIgnoreCase)));
                instance.Items.Insert(0, jumplistItem);
                JumpListItemPaths.Remove(JumpListItemPaths.FirstOrDefault(x => x.Equals(path, StringComparison.OrdinalIgnoreCase)));
                JumpListItemPaths.Add(path);
            }
        }

        public async void RemoveFolder(string path)
        {
            // Updating the jumplist may fail randomly with error: FileLoadException: File in use
            // In that case app should just catch the error and proceed as usual
            try
            {
                if (instance != null)
                {
                    if (JumpListItemPaths.Remove(path))
                    {
                        await instance.SaveAsync();
                    }
                }
            }
            catch { }
        }

        private async Task RefreshAsync()
        {
            if (instance != null)
            {
                // Clear all items to avoid localization issues
                instance.Items.Clear();

                foreach (string path in JumpListItemPaths)
                {
                    AddFolder(path);
                }

                await instance.SaveAsync();
            }
        }
    }
}

using Files.Common;
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
        private JumpList _instance = null;
        private List<string> JumpListItemPaths { get; set; }

        public JumpListManager()
        {
            Initialize();
        }

        private async void Initialize()
        {
            if (JumpList.IsSupported())
            {
                _instance = await JumpList.LoadCurrentAsync();

                // Disable automatic jumplist. It doesn't work with Files UWP.
                _instance.SystemGroupKind = JumpListSystemGroupKind.None;
                JumpListItemPaths = _instance.Items.Select(item => item.Arguments).ToList();
            }
        }

        public async void AddFolderToJumpList(string path)
        {
            // Saving to jumplist may fail randomly with error: ERROR_UNABLE_TO_REMOVE_REPLACED
            // In that case app should just catch the error and proceed as usual
            try
            {
                await AddFolder(path);
                await _instance?.SaveAsync();
            }
            catch { }
        }

        private Task AddFolder(string path)
        {
            if (_instance != null && !JumpListItemPaths.Contains(path))
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
                else if (path.Equals(App.AppSettings.DocumentsPath, StringComparison.OrdinalIgnoreCase))
                {
                    displayName = "ms-resource:///Resources/SidebarDocuments";
                }
                else if (path.Equals(App.AppSettings.PicturesPath, StringComparison.OrdinalIgnoreCase))
                {
                    displayName = "ms-resource:///Resources/SidebarPictures";
                }
                else if (path.Equals(App.AppSettings.MusicPath, StringComparison.OrdinalIgnoreCase))
                {
                    displayName = "ms-resource:///Resources/SidebarMusic";
                }
                else if (path.Equals(App.AppSettings.VideosPath, StringComparison.OrdinalIgnoreCase))
                {
                    displayName = "ms-resource:///Resources/SidebarVideos";
                }
                else if (path.Equals(App.AppSettings.RecycleBinPath, StringComparison.OrdinalIgnoreCase))
                {
                    var localSettings = ApplicationData.Current.LocalSettings;
                    displayName = localSettings.Values.Get("RecycleBin_Title", "Recycle Bin");
                }
                else
                {
                    displayName = Path.GetFileName(path);
                }

                var jumplistItem = JumpListItem.CreateWithArguments(path, displayName);
                jumplistItem.Description = jumplistItem.Arguments;
                jumplistItem.GroupName = "ms-resource:///Resources/JumpListRecentGroupHeader";
                jumplistItem.Logo = new Uri("ms-appx:///Assets/FolderIcon.png");
                _instance.Items.Add(jumplistItem);
                JumpListItemPaths.Add(path);
            }

            return Task.CompletedTask;
        }

        public async void RemoveFolder(string path)
        {
            if (JumpListItemPaths.Contains(path))
            {
                JumpListItemPaths.Remove(path);
                await UpdateAsync();
            }
        }

        private async Task UpdateAsync()
        {
            if (_instance != null)
            {
                // Clear all items to avoid localization issues
                _instance?.Items.Clear();

                foreach (string path in JumpListItemPaths)
                {
                    await AddFolder(path);
                }

                await _instance.SaveAsync();
            }
        }
    }
}
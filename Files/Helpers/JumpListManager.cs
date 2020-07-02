using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
            await AddFolder(path);
            await _instance?.SaveAsync();
        }

        private Task AddFolder(string path)
        {
            if (!JumpListItemPaths.Contains(path) && _instance != null)
            {
                var jumplistItem = JumpListItem.CreateWithArguments(path, Path.GetFileName(path));
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
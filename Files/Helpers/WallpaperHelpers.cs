using Files.Enums;
using Files.Filesystem;
using System;
using Windows.Storage;
using Windows.System.UserProfile;

namespace Files.Helpers
{
    public static class WallpaperHelpers
    {
        public static async void SetAsBackground(WallpaperType type, string filePath, IShellPage associatedInstance)
        {
            if (UserProfilePersonalizationSettings.IsSupported())
            {
                // Get the path of the selected file
                StorageFile sourceFile = await StorageItemHelpers.ToStorageItem<StorageFile>(filePath, associatedInstance);
                if (sourceFile == null)
                {
                    return;
                }

                // Get the app's local folder to use as the destination folder.
                StorageFolder localFolder = ApplicationData.Current.LocalFolder;

                // the file to the destination folder.
                // Generate unique name if the file already exists.
                // If the file you are trying to set as the wallpaper has the same name as the current wallpaper,
                // the system will ignore the request and no-op the operation
                StorageFile file = await FilesystemTasks.Wrap(() => sourceFile.CopyAsync(localFolder, sourceFile.Name, NameCollisionOption.GenerateUniqueName).AsTask());
                if (file == null)
                {
                    return;
                }

                UserProfilePersonalizationSettings profileSettings = UserProfilePersonalizationSettings.Current;
                if (type == WallpaperType.Desktop)
                {
                    // Set the desktop background
                    await profileSettings.TrySetWallpaperImageAsync(file);
                }
                else if (type == WallpaperType.LockScreen)
                {
                    // Set the lockscreen background
                    await profileSettings.TrySetLockScreenImageAsync(file);
                }
            }
        }
    }
}
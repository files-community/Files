using Files.Shared.Enums;
using Files.Uwp.Filesystem;
using Files.Uwp.Filesystem.StorageItems;
using System;
using Windows.Storage;
using Windows.System.UserProfile;

namespace Files.Uwp.Helpers
{
    public static class WallpaperHelpers
    {
        public static async void SetAsBackground(WallpaperType type, string filePath)
        {
            if (UserProfilePersonalizationSettings.IsSupported())
            {
                // Get the path of the selected file
                BaseStorageFile sourceFile = await StorageHelpers.ToStorageItem<BaseStorageFile>(filePath);
                if (sourceFile == null)
                {
                    return;
                }

                // Get the app's local folder to use as the destination folder.
                BaseStorageFolder localFolder = ApplicationData.Current.LocalFolder;

                // the file to the destination folder.
                // Generate unique name if the file already exists.
                // If the file you are trying to set as the wallpaper has the same name as the current wallpaper,
                // the system will ignore the request and no-op the operation
                BaseStorageFile file = await FilesystemTasks.Wrap(() => sourceFile.CopyAsync(localFolder, sourceFile.Name, NameCollisionOption.GenerateUniqueName).AsTask());
                if (file == null)
                {
                    return;
                }

                UserProfilePersonalizationSettings profileSettings = UserProfilePersonalizationSettings.Current;
                if (type == WallpaperType.Desktop)
                {
                    // Set the desktop background
                    await profileSettings.TrySetWallpaperImageAsync(await file.ToStorageFileAsync());
                }
                else if (type == WallpaperType.LockScreen)
                {
                    // Set the lockscreen background
                    await profileSettings.TrySetLockScreenImageAsync(await file.ToStorageFileAsync());
                }
            }
        }
    }
}
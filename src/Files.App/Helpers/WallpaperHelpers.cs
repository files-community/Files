using System;
using System.Linq;
using Files.App.Filesystem;
using Files.App.Filesystem.StorageItems;
using Files.Shared.Enums;
using Vanara.PInvoke;
using Windows.Storage;
using Windows.System.UserProfile;

namespace Files.App.Helpers
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

        public static void SetSlideshow(string[] filePaths)
        {
            if (filePaths is null || filePaths.Any())
            {
                return;
            }

            var idList = filePaths.Select(Shell32.IntILCreateFromPath).ToArray();
            Shell32.SHCreateShellItemArrayFromIDLists((uint)idList.Length, idList.ToArray(), out var shellItemArray);

            // Set SlideShow
            var wallpaper = (Shell32.IDesktopWallpaper)new Shell32.DesktopWallpaper();
            wallpaper.SetSlideshow(shellItemArray);

            // Set wallpaper to fill desktop.
            wallpaper.SetPosition(Shell32.DESKTOP_WALLPAPER_POSITION.DWPOS_FILL);

            // TODO: Should we handle multiple monitors?
            // var monitors = wallpaper.GetMonitorDevicePathCount();
            wallpaper.GetMonitorDevicePathAt(0, out var monitorId);
            // Advance the slideshow to reflect the change.
            wallpaper.AdvanceSlideshow(monitorId, Shell32.DESKTOP_SLIDESHOW_DIRECTION.DSD_FORWARD);
        }
    }
}

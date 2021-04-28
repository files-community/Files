using Files.ViewModels;
using System;
using System.IO;
using Windows.Storage;

namespace Files.Helpers
{
    internal static class GlyphHelper
    {
        public static SettingsViewModel AppSettings => App.AppSettings;

        /// <summary>
        /// Gets the icon for the items in the navigation sidebar
        /// </summary>
        /// <param name="path">The path in the sidebar</param>
        /// <returns>The icon code</returns>
        public static string GetItemIcon(string path, string fallback = "\uE8B7")
        {
            string iconCode = fallback;
            if (path != null)
            {
                // TODO: do library check based on the library file path?
                var udp = UserDataPaths.GetDefault();
                if (path.Equals(AppSettings.DesktopPath, StringComparison.OrdinalIgnoreCase))
                {
                    iconCode = "\uE8FC";
                }
                else if (path.Equals(AppSettings.DownloadsPath, StringComparison.OrdinalIgnoreCase))
                {
                    iconCode = "\uE896";
                }
                else if (path.Equals(udp.Documents, StringComparison.OrdinalIgnoreCase))
                {
                    iconCode = "\uE8A5";
                }
                else if (path.Equals(udp.Pictures, StringComparison.OrdinalIgnoreCase))
                {
                    iconCode = "\uEB9F";
                }
                else if (path.Equals(udp.Music, StringComparison.OrdinalIgnoreCase))
                {
                    iconCode = "\uEC4F";
                }
                else if (path.Equals(udp.Videos, StringComparison.OrdinalIgnoreCase))
                {
                    iconCode = "\uE8B2";
                }
                else if (path.Equals(AppSettings.NetworkFolderPath, StringComparison.OrdinalIgnoreCase))
                {
                    iconCode = "\uE8CE";
                }
                else if (Path.GetPathRoot(path).Equals(path, StringComparison.OrdinalIgnoreCase))
                {
                    iconCode = "\uEDA2";
                }
            }
            return iconCode;
        }

        public static Uri GetIconUri(string path)
        {
            Uri iconCode = new Uri("ms-appx:///Assets/FluentIcons/Folder.svg");
            if (path != null)
            {
                // TODO: do library check based on the library file path?
                var udp = UserDataPaths.GetDefault();
                if (path.Equals(AppSettings.DesktopPath, StringComparison.OrdinalIgnoreCase))
                {
                    iconCode = new Uri("ms-appx:///Assets/FluentIcons/Desktop.svg");
                }
                else if (path.Equals(AppSettings.DownloadsPath, StringComparison.OrdinalIgnoreCase))
                {
                    iconCode = new Uri("ms-appx:///Assets/FluentIcons/Downloads.svg");
                }
                else if (path.Equals(udp.Documents, StringComparison.OrdinalIgnoreCase))
                {
                    iconCode = new Uri("ms-appx:///Assets/FluentIcons/Documents.svg");
                }
                else if (path.Equals(udp.Pictures, StringComparison.OrdinalIgnoreCase))
                {
                    iconCode = new Uri("ms-appx:///Assets/FluentIcons/Pictures.svg");
                }
                else if (path.Equals(udp.Music, StringComparison.OrdinalIgnoreCase))
                {
                    iconCode = new Uri("ms-appx:///Assets/FluentIcons/Music.svg");
                }
                else if (path.Equals(udp.Videos, StringComparison.OrdinalIgnoreCase))
                {
                    iconCode = new Uri("ms-appx:///Assets/FluentIcons/Videos.svg");
                }
                else if (path.Equals(AppSettings.NetworkFolderPath, StringComparison.OrdinalIgnoreCase))
                {
                    iconCode = new Uri("ms-appx:///Assets/FluentIcons/Drive_Network.svg");
                }
                else if (Path.GetPathRoot(path).Equals(path, StringComparison.OrdinalIgnoreCase))
                {
                    iconCode = new Uri("ms-appx:///Assets/FluentIcons/Drive_USB.svg");
                }
            }
            return iconCode;
        }
    }
}
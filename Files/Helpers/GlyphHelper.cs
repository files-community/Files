using Files.ViewModels;
using System;
using System.IO;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media.Imaging;

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
        public static string GetItemIcon(string path)
        {
            string iconCode;

            if (path.Equals(AppSettings.DesktopPath, StringComparison.OrdinalIgnoreCase))
            {
                iconCode = "\uE8FC";
            }
            else if (path.Equals(AppSettings.DownloadsPath, StringComparison.OrdinalIgnoreCase))
            {
                iconCode = "\uE896";
            }
            else if (path.Equals(AppSettings.DocumentsPath, StringComparison.OrdinalIgnoreCase))
            {
                iconCode = "\uE8A5";
            }
            else if (path.Equals(AppSettings.PicturesPath, StringComparison.OrdinalIgnoreCase))
            {
                iconCode = "\uEB9F";
            }
            else if (path.Equals(AppSettings.MusicPath, StringComparison.OrdinalIgnoreCase))
            {
                iconCode = "\uEC4F";
            }
            else if (path.Equals(AppSettings.VideosPath, StringComparison.OrdinalIgnoreCase))
            {
                iconCode = "\uE8B2";
            }
            else if (Path.GetPathRoot(path).Equals(path, StringComparison.OrdinalIgnoreCase))
            {
                iconCode = "\uEDA2";
            }
            else
            {
                iconCode = "\uE8B7";
            }

            return iconCode;
        }
    }
}
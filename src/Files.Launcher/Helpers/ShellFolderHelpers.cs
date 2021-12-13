using Files.Common;
using System;
using System.IO;
using System.Linq;
using Vanara.PInvoke;
using Vanara.Windows.Shell;

namespace FilesFullTrust.Helpers
{
    public static class ShellFolderExtensions
    {
        public static ShellLibraryItem GetShellLibraryItem(ShellLibrary2 library, string filePath)
        {
            var libraryItem = new ShellLibraryItem
            {
                FullPath = filePath,
                AbsolutePath = library.GetDisplayName(ShellItemDisplayString.DesktopAbsoluteParsing),
                RelativePath = library.GetDisplayName(ShellItemDisplayString.ParentRelativeParsing),
                DisplayName = library.GetDisplayName(ShellItemDisplayString.NormalDisplay),
                IsPinned = library.PinnedToNavigationPane,
            };
            var folders = library.Folders;
            if (folders.Count > 0)
            {
                libraryItem.DefaultSaveFolder = Extensions.IgnoreExceptions(() => library.DefaultSaveFolder.FileSystemPath);
                libraryItem.Folders = folders.Select(f => f.FileSystemPath).ToArray();
            }
            return libraryItem;
        }

        public static ShellFileItem GetShellFileItem(ShellItem folderItem)
        {
            if (folderItem == null)
            {
                return null;
            }
            bool isFolder = folderItem.IsFolder && !folderItem.Attributes.HasFlag(ShellItemAttribute.Stream);
            if (folderItem.Properties == null)
            {
                return new ShellFileItem(isFolder, folderItem.FileSystemPath, Path.GetFileName(folderItem.Name), folderItem.Name, DateTime.Now, DateTime.Now, DateTime.Now, null, 0, null);
            }
            var parsingPath = folderItem.GetDisplayName(ShellItemDisplayString.DesktopAbsoluteParsing);
            parsingPath ??= folderItem.FileSystemPath; // True path on disk
            folderItem.Properties.TryGetValue<string>(
                Ole32.PROPERTYKEY.System.ItemNameDisplay, out var fileName);
            fileName ??= Path.GetFileName(folderItem.Name); // Original file name
            string filePath = Path.Combine(Path.GetDirectoryName(parsingPath), folderItem.Name); // In recycle bin "Name" contains original file path + name
            if (!isFolder && !string.IsNullOrEmpty(parsingPath) && Path.GetExtension(parsingPath) is string realExtension && !string.IsNullOrEmpty(realExtension))
            {
                if (!string.IsNullOrEmpty(fileName) && !fileName.EndsWith(realExtension, StringComparison.OrdinalIgnoreCase))
                {
                    fileName = $"{fileName}{realExtension}";
                }
                if (!string.IsNullOrEmpty(filePath) && !filePath.EndsWith(realExtension, StringComparison.OrdinalIgnoreCase))
                {
                    filePath = $"{filePath}{realExtension}";
                }
            }
            folderItem.Properties.TryGetValue<System.Runtime.InteropServices.ComTypes.FILETIME?>(
                Ole32.PROPERTYKEY.System.Recycle.DateDeleted, out var fileTime);
            var recycleDate = fileTime?.ToDateTime().ToLocalTime() ?? DateTime.Now; // This is LocalTime
            folderItem.Properties.TryGetValue<System.Runtime.InteropServices.ComTypes.FILETIME?>(
                Ole32.PROPERTYKEY.System.DateModified, out fileTime);
            var modifiedDate = fileTime?.ToDateTime().ToLocalTime() ?? DateTime.Now; // This is LocalTime
            folderItem.Properties.TryGetValue<System.Runtime.InteropServices.ComTypes.FILETIME?>(
                Ole32.PROPERTYKEY.System.DateCreated, out fileTime);
            var createdDate = fileTime?.ToDateTime().ToLocalTime() ?? DateTime.Now; // This is LocalTime
            string fileSize = folderItem.Properties.TryGetValue<ulong?>(
                Ole32.PROPERTYKEY.System.Size, out var fileSizeBytes) ?
                folderItem.Properties.GetPropertyString(Ole32.PROPERTYKEY.System.Size) : null;
            folderItem.Properties.TryGetValue<string>(
                Ole32.PROPERTYKEY.System.ItemTypeText, out var fileType);
            return new ShellFileItem(isFolder, parsingPath, fileName, filePath, recycleDate, modifiedDate, createdDate, fileSize, fileSizeBytes ?? 0, fileType);
        }

        public static ShellLinkItem GetShellLinkItem(ShellLink linkItem)
        {
            if (linkItem == null)
            {
                return null;
            }
            var baseItem = GetShellFileItem(linkItem);
            if (baseItem == null)
            {
                return null;
            }
            var link = new ShellLinkItem(baseItem);
            link.IsFolder = !string.IsNullOrEmpty(link.TargetPath) && linkItem.Target.IsFolder;
            link.RunAsAdmin = linkItem.RunAsAdministrator;
            link.Arguments = linkItem.Arguments;
            link.WorkingDirectory = linkItem.WorkingDirectory;
            link.TargetPath = linkItem.TargetPath;
            return link;
        }
    }
}

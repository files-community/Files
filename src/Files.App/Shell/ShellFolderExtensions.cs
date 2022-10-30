﻿using Files.Shared;
using Files.Shared.Extensions;
using System;
using System.IO;
using System.Linq;
using Vanara.PInvoke;
using Vanara.Windows.Shell;
using static Vanara.PInvoke.Shell32;

namespace Files.App.Shell
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
                libraryItem.DefaultSaveFolder = SafetyExtensions.IgnoreExceptions(() => library.DefaultSaveFolder.FileSystemPath);
                libraryItem.Folders = folders.Select(f => f.FileSystemPath).ToArray();
            }
            return libraryItem;
        }

        private static T TryGetProperty<T>(this ShellItemPropertyStore sip, Ole32.PROPERTYKEY key)
        {
            T value = default;
            SafetyExtensions.IgnoreExceptions(() => sip.TryGetValue<T>(key, out value));
            return value;
        }

        public static ShellFileItem GetShellFileItem(ShellItem folderItem)
        {
            if (folderItem is null)
            {
                return null;
            }
            bool isFolder = folderItem.IsFolder && !folderItem.Attributes.HasFlag(ShellItemAttribute.Stream);
            var parsingPath = folderItem.GetDisplayName(ShellItemDisplayString.DesktopAbsoluteParsing);
            parsingPath ??= folderItem.FileSystemPath; // True path on disk
            if (parsingPath is null || !Path.IsPathRooted(parsingPath))
            {
                parsingPath = parsingPath switch
                {
                    "::{645FF040-5081-101B-9F08-00AA002F954E}" => "Shell:RecycleBinFolder",
                    "::{F02C1A0D-BE21-4350-88B0-7367FC96EF3C}" => "Shell:NetworkPlacesFolder",
                    "::{208D2C60-3AEA-1069-A2D7-08002B30309D}" => "Shell:NetworkPlacesFolder",
                    // Use PIDL as path
                    _ => $@"\\SHELL\{string.Join("\\", folderItem.PIDL.Select(x => x.GetBytes()).Select(x => Convert.ToBase64String(x, 0, x.Length)))}"
                };
            }
            var fileName = folderItem.Properties.TryGetProperty<string>(Ole32.PROPERTYKEY.System.ItemNameDisplay);
            fileName ??= Path.GetFileName(folderItem.Name); // Original file name
            fileName ??= folderItem.GetDisplayName(ShellItemDisplayString.ParentRelativeParsing);
            var itemNameOrOriginalPath = folderItem.Name ?? fileName;
            string filePath = string.IsNullOrEmpty(Path.GetDirectoryName(parsingPath)) ? // Null if root
                parsingPath : Path.Combine(Path.GetDirectoryName(parsingPath), itemNameOrOriginalPath); // In recycle bin "Name" contains original file path + name
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
            var fileTime = folderItem.Properties.TryGetProperty<System.Runtime.InteropServices.ComTypes.FILETIME?>(
                Ole32.PROPERTYKEY.System.Recycle.DateDeleted);
            var recycleDate = fileTime?.ToDateTime().ToLocalTime() ?? DateTime.Now; // This is LocalTime
            fileTime = folderItem.Properties.TryGetProperty<System.Runtime.InteropServices.ComTypes.FILETIME?>(
                Ole32.PROPERTYKEY.System.DateModified);
            var modifiedDate = fileTime?.ToDateTime().ToLocalTime() ?? folderItem.FileInfo?.LastWriteTime ?? DateTime.Now; // This is LocalTime
            fileTime = folderItem.Properties.TryGetProperty<System.Runtime.InteropServices.ComTypes.FILETIME?>(
                Ole32.PROPERTYKEY.System.DateCreated);
            var createdDate = fileTime?.ToDateTime().ToLocalTime() ?? folderItem.FileInfo?.CreationTime ?? DateTime.Now; // This is LocalTime
            var fileSizeBytes = folderItem.Properties.TryGetProperty<ulong?>(Ole32.PROPERTYKEY.System.Size);
            string fileSize = fileSizeBytes is not null ? folderItem.Properties.GetPropertyString(Ole32.PROPERTYKEY.System.Size) : null;
            var fileType = folderItem.Properties.TryGetProperty<string>(Ole32.PROPERTYKEY.System.ItemTypeText);
            return new ShellFileItem(isFolder, parsingPath, fileName, filePath, recycleDate, modifiedDate, createdDate, fileSize, fileSizeBytes ?? 0, fileType);
        }

        public static ShellLinkItem GetShellLinkItem(ShellLink linkItem)
        {
            if (linkItem is null)
            {
                return null;
            }
            var baseItem = GetShellFileItem(linkItem);
            if (baseItem is null)
            {
                return null;
            }
            var link = new ShellLinkItem(baseItem);
            link.IsFolder = !string.IsNullOrEmpty(linkItem.TargetPath) && linkItem.Target.IsFolder;
            link.RunAsAdmin = linkItem.RunAsAdministrator;
            link.Arguments = linkItem.Arguments;
            link.WorkingDirectory = linkItem.WorkingDirectory;
            link.TargetPath = linkItem.TargetPath;
            return link;
        }

        public static string GetParsingPath(this ShellItem item)
        {
            if (item is null)
            {
                return null;
            }
            return item.IsFileSystem ? item.FileSystemPath : item.ParsingName;
        }

        public static bool GetStringAsPidl(string pathOrPidl, out Shell32.PIDL pidl)
        {
            if (pathOrPidl.StartsWith(@"\\SHELL\", StringComparison.Ordinal))
            {
                pidl = pathOrPidl.Replace(@"\\SHELL\", "", StringComparison.Ordinal)
                    .Split('\\', StringSplitOptions.RemoveEmptyEntries)
                    .Select(pathSegment => new Shell32.PIDL(pathSegment))
                    .Aggregate((x, y) => Shell32.PIDL.Combine(x, y));
                return true;
            }
            else
            {
                pidl = Shell32.PIDL.Null;
                return false;
            }
        }

        public static ShellItem GetShellItemFromPathOrPidl(string pathOrPidl)
        {
            if (GetStringAsPidl(pathOrPidl, out var pidl))
            {
                return ShellItem.Open(pidl);
            }
            return ShellItem.Open(pathOrPidl);
        }
    }
}

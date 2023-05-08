// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.IO;
using Vanara.PInvoke;
using Vanara.Windows.Shell;

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
				return null;

			// Zip archives are also shell folders, check for STREAM attribute
			// Do not use folderItem's Attributes property, throws unimplemented for some shell folders

			bool isFolder = folderItem.IsFolder && folderItem.IShellItem?.GetAttributes(Shell32.SFGAO.SFGAO_STREAM) is 0;
			var parsingPath = folderItem.GetDisplayName(ShellItemDisplayString.DesktopAbsoluteParsing);

			// True path on disk
			parsingPath ??= folderItem.FileSystemPath;

			if (parsingPath is null || !Path.IsPathRooted(parsingPath))
			{
				parsingPath = parsingPath switch
				{
					"::{645FF040-5081-101B-9F08-00AA002F954E}" => Constants.UserEnvironmentPaths.RecycleBinPath,
					"::{F02C1A0D-BE21-4350-88B0-7367FC96EF3C}" => Constants.UserEnvironmentPaths.NetworkFolderPath,
					"::{208D2C60-3AEA-1069-A2D7-08002B30309D}" => Constants.UserEnvironmentPaths.NetworkFolderPath,
					"::{20D04FE0-3AEA-1069-A2D8-08002B30309D}" => Constants.UserEnvironmentPaths.MyComputerPath,
					"::{031E4825-7B94-4DC3-B131-E946B44C8DD5}\\Documents.library-ms" => ShellHelpers.GetLibraryFullPathFromShell(parsingPath),
					"::{031E4825-7B94-4DC3-B131-E946B44C8DD5}\\Pictures.library-ms" => ShellHelpers.GetLibraryFullPathFromShell(parsingPath),
					"::{031E4825-7B94-4DC3-B131-E946B44C8DD5}\\Music.library-ms" => ShellHelpers.GetLibraryFullPathFromShell(parsingPath),
					"::{031E4825-7B94-4DC3-B131-E946B44C8DD5}\\Videos.library-ms" => ShellHelpers.GetLibraryFullPathFromShell(parsingPath),
					// Use PIDL as path
					// Replace "/" with "_" to avoid confusion with path separator
					_ => $@"\\SHELL\{string.Join("\\", folderItem.PIDL.Select(x => x.GetBytes()).Select(x => Convert.ToBase64String(x, 0, x.Length).Replace("/", "_")))}"
				};
			}

			var fileName = folderItem.Properties.TryGetProperty<string>(Ole32.PROPERTYKEY.System.ItemNameDisplay);
			fileName ??= Path.GetFileName(folderItem.Name); // Original file name
			fileName ??= folderItem.GetDisplayName(ShellItemDisplayString.ParentRelativeParsing);

			var itemNameOrOriginalPath = folderItem.Name ?? fileName;

			// In recycle bin "Name" contains original file path + name
			string filePath = Path.IsPathRooted(itemNameOrOriginalPath) ?
				itemNameOrOriginalPath : parsingPath;

			if (!isFolder && !string.IsNullOrEmpty(parsingPath) && Path.GetExtension(parsingPath) is string realExtension && !string.IsNullOrEmpty(realExtension))
			{
				if (!string.IsNullOrEmpty(fileName) && !fileName.EndsWith(realExtension, StringComparison.OrdinalIgnoreCase))
					fileName = $"{fileName}{realExtension}";

				if (!string.IsNullOrEmpty(filePath) && !filePath.EndsWith(realExtension, StringComparison.OrdinalIgnoreCase))
					filePath = $"{filePath}{realExtension}";
			}

			var fileTime = folderItem.Properties.TryGetProperty<System.Runtime.InteropServices.ComTypes.FILETIME?>(
				Ole32.PROPERTYKEY.System.Recycle.DateDeleted);

			var recycleDate = fileTime?.ToDateTime().ToLocalTime() ?? DateTime.Now; // This is LocalTime
			fileTime = folderItem.Properties.TryGetProperty<System.Runtime.InteropServices.ComTypes.FILETIME?>(
				Ole32.PROPERTYKEY.System.DateModified);

			var modifiedDate = fileTime?.ToDateTime().ToLocalTime() ?? SafetyExtensions.IgnoreExceptions(() => folderItem.FileInfo?.LastWriteTime) ?? DateTime.Now; // This is LocalTime
			fileTime = folderItem.Properties.TryGetProperty<System.Runtime.InteropServices.ComTypes.FILETIME?>(
				Ole32.PROPERTYKEY.System.DateCreated);

			var createdDate = fileTime?.ToDateTime().ToLocalTime() ?? SafetyExtensions.IgnoreExceptions(() => folderItem.FileInfo?.CreationTime) ?? DateTime.Now; // This is LocalTime
			var fileSizeBytes = folderItem.Properties.TryGetProperty<ulong?>(Ole32.PROPERTYKEY.System.Size);
			string fileSize = fileSizeBytes is not null ? folderItem.Properties.GetPropertyString(Ole32.PROPERTYKEY.System.Size) : null;
			var fileType = folderItem.Properties.TryGetProperty<string>(Ole32.PROPERTYKEY.System.ItemTypeText);

			return new ShellFileItem(isFolder, parsingPath, fileName, filePath, recycleDate, modifiedDate, createdDate, fileSize, fileSizeBytes ?? 0, fileType, folderItem.PIDL.GetBytes());
		}

		public static ShellLinkItem GetShellLinkItem(ShellLink linkItem)
		{
			if (linkItem is null)
				return null;

			var baseItem = GetShellFileItem(linkItem);
			if (baseItem is null)
				return null;

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
				return null;

			return item.IsFileSystem ? item.FileSystemPath : item.ParsingName;
		}

		public static bool GetStringAsPidl(string pathOrPidl, out Shell32.PIDL pidl)
		{
			if (pathOrPidl.StartsWith(@"\\SHELL\", StringComparison.Ordinal))
			{
				pidl = pathOrPidl.Replace(@"\\SHELL\", "", StringComparison.Ordinal)
					// Avoid confusion with path separator
					.Replace("_", "/")
					.Split('\\', StringSplitOptions.RemoveEmptyEntries)
					.Select(pathSegment => new Shell32.PIDL(Convert.FromBase64String(pathSegment)))
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
			return GetStringAsPidl(pathOrPidl, out var pidl) ? ShellItem.Open(pidl) : ShellItem.Open(pathOrPidl);
		}
	}
}

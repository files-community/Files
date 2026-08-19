// Copyright (c) Files Community
// Licensed under the MIT License.

using System.IO;
using System.Runtime.InteropServices.ComTypes;
using Windows.Win32.System.SystemServices;
using Windows.Win32.UI.Shell;

namespace Files.App.Utils.Shell
{
	/// <summary>
	/// Provides static extension for shell folders.
	/// </summary>
	public static class ShellFolderExtensions
	{
		public static ShellLibraryItem GetShellLibraryItem(ShellLibraryEx library, string filePath)
		{
			var libraryItem = new ShellLibraryItem
			{
				FullPath = filePath,
				AbsolutePath = library.GetDisplayName(SIGDN.SIGDN_DESKTOPABSOLUTEPARSING) ?? string.Empty,
				RelativePath = library.GetDisplayName(SIGDN.SIGDN_PARENTRELATIVEPARSING) ?? string.Empty,
				DisplayName = library.GetDisplayName(SIGDN.SIGDN_NORMALDISPLAY) ?? string.Empty,
				IsPinned = library.PinnedToNavigationPane,
			};

			var folders = library.Folders;
			if (folders.Count > 0)
			{
				libraryItem.DefaultSaveFolder = SafetyExtensions.IgnoreExceptions(() => library.DefaultSaveFolder.FileSystemPath) ?? string.Empty;
				libraryItem.Folders = folders.Select(f => f.FileSystemPath).OfType<string>().ToArray();
			}

			return libraryItem;
		}

		private static T? TryGetProperty<T>(this ShellItemPropertyStore propertyStore, string propertyName)
		{
			T? value = default;
			SafetyExtensions.IgnoreExceptions(() => propertyStore.TryGetValue(propertyName, out value));
			return value;
		}

		public static ShellFileItem? GetShellFileItem(ShellItem? folderItem)
		{
			if (folderItem is null)
				return null;

			// NOTE: Query only the required attributes because some shell folders do not implement the full attribute set

			// Zip archives are also shell folders, check for STREAM attribute

			bool isFolder = folderItem.IsFolder && !folderItem.IsStream;
			var parsingPath = folderItem.GetDisplayName(SIGDN.SIGDN_DESKTOPABSOLUTEPARSING);

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
					_ => $@"\\SHELL\{string.Join("\\", folderItem.PIDL.Select(x => x.GetBytes()).Select(x => Convert.ToBase64String(x, 0, x.Length).Replace('/', '_')))}"
				};
			}

			var fileName = folderItem.Properties.TryGetProperty<string>("System.ItemNameDisplay");
			fileName ??= Path.GetFileName(folderItem.Name); // Original file name
			fileName ??= folderItem.GetDisplayName(SIGDN.SIGDN_PARENTRELATIVEPARSING);

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

			var fileTime = folderItem.Properties.TryGetProperty<FILETIME?>("System.Recycle.DateDeleted");

			var recycleDate = fileTime is { } deleted ? ToDateTime(deleted).ToLocalTime() : DateTime.Now; // This is LocalTime
			fileTime = folderItem.Properties.TryGetProperty<FILETIME?>("System.DateModified");

			var modifiedDate = fileTime is { } modified ? ToDateTime(modified).ToLocalTime() : SafetyExtensions.IgnoreExceptions(() => folderItem.FileInfo?.LastWriteTime) ?? DateTime.Now; // This is LocalTime
			fileTime = folderItem.Properties.TryGetProperty<FILETIME?>("System.DateCreated");

			var createdDate = fileTime is { } created ? ToDateTime(created).ToLocalTime() : SafetyExtensions.IgnoreExceptions(() => folderItem.FileInfo?.CreationTime) ?? DateTime.Now; // This is LocalTime
			var fileSizeBytes = folderItem.Properties.TryGetProperty<ulong?>("System.Size");
			string? fileSize = fileSizeBytes is not null ? folderItem.Properties.GetPropertyString("System.Size") : null;
			var fileType = folderItem.Properties.TryGetProperty<string>("System.ItemTypeText");

			return new(isFolder, parsingPath, fileName, filePath, recycleDate, modifiedDate, createdDate, fileSize, fileSizeBytes ?? 0, fileType, folderItem.PIDL.GetBytes());
		}

		public static ShellLinkItem? GetShellLinkItem(ShellLink? linkItem)
		{
			if (linkItem is null)
				return null;

			var baseItem = GetShellFileItem(linkItem);
			if (baseItem is null)
				return null;

			string targetPath = Environment.ExpandEnvironmentVariables(linkItem.TargetPath);
			var link = new ShellLinkItem(baseItem)
			{
				// The attributes persisted in the link file avoid opening the target,
				// which can block on unreachable network locations
				IsFolder = linkItem.StoredTargetIsFolder() ?? linkItem.IsTargetFolder(targetPath),
				RunAsAdmin = linkItem.RunAsAdministrator,
				ShowWindowCommand = (Windows.Win32.UI.WindowsAndMessaging.SHOW_WINDOW_CMD)linkItem.ShowState,
				Arguments = linkItem.Arguments,
				WorkingDirectory = Environment.ExpandEnvironmentVariables(linkItem.WorkingDirectory),
				TargetPath = targetPath
			};

			return link;
		}

		public static string? GetParsingPath(this ShellItem? item)
		{
			if (item is null)
				return null;

			return item.IsFileSystem ? item.FileSystemPath : item.ParsingName;
		}

		public static bool GetStringAsPIDL(string pathOrPIDL, out ShellPidl? pPIDL)
		{
			if (pathOrPIDL.StartsWith(@"\\SHELL\", StringComparison.Ordinal))
			{
				var segments = pathOrPIDL.Replace(@"\\SHELL\", "", StringComparison.Ordinal)
					// Avoid confusion with path separator
					.Replace('_', '/')
					.Split('\\', StringSplitOptions.RemoveEmptyEntries)
					.Select(Convert.FromBase64String);
				pPIDL = ShellPidl.FromSegments(segments);

				return true;
			}
			else
			{
				pPIDL = null;

				return false;
			}
		}

		public static ShellItem GetShellItemFromPathOrPIDL(string pathOrPIDL)
		{
			return GetStringAsPIDL(pathOrPIDL, out var pPIDL) ? ShellItem.Open(pPIDL!) : ShellItem.Open(pathOrPIDL);
		}

		private static DateTime ToDateTime(FILETIME value)
			=> DateTime.FromFileTimeUtc(((long)value.dwHighDateTime << 32) | (uint)value.dwLowDateTime);

	}
}

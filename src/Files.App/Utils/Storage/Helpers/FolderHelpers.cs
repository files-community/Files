// Copyright (c) Files Community
// Licensed under the MIT License.

using System.IO;

namespace Files.App.Utils.Storage
{
	public readonly record struct SubfolderEntry(string Path, string Name, bool HasSubfolders, bool IsHidden);

	public static class FolderHelpers
	{
		public static bool CheckFolderAccessWithWin32(string path)
		{
			IntPtr hFileTsk = Win32PInvoke.FindFirstFileExFromApp($"{path}{Path.DirectorySeparatorChar}*.*", Win32PInvoke.FINDEX_INFO_LEVELS.FindExInfoBasic,
				out Win32PInvoke.WIN32_FIND_DATA _, Win32PInvoke.FINDEX_SEARCH_OPS.FindExSearchNameMatch, IntPtr.Zero, Win32PInvoke.FIND_FIRST_EX_LARGE_FETCH);
			if (hFileTsk.ToInt64() != -1)
			{
				Win32PInvoke.FindClose(hFileTsk);
				return true;
			}
			return false;
		}

		public static async Task<bool> CheckBitlockerStatusAsync(BaseStorageFolder rootFolder, string path)
		{
			if (rootFolder?.Properties is null)
			{
				return false;
			}
			if (Path.IsPathRooted(path) && Path.GetPathRoot(path) == path)
			{
				IDictionary<string, object> extraProperties =
					await rootFolder.Properties.RetrievePropertiesAsync(["System.Volume.BitLockerProtection"]);
				return (int?)extraProperties["System.Volume.BitLockerProtection"] == 6; // Drive is bitlocker protected and locked
			}
			return false;
		}

		/// <summary>
		/// This function is used to determine whether or not a folder has any contents.
		/// </summary>
		/// <param name="targetPath">The path to the target folder</param>
		///
		public static bool CheckForFilesFolders(string targetPath)
		{
			IntPtr hFile = Win32PInvoke.FindFirstFileExFromApp($"{targetPath}{Path.DirectorySeparatorChar}*.*", Win32PInvoke.FINDEX_INFO_LEVELS.FindExInfoBasic,
				out Win32PInvoke.WIN32_FIND_DATA _, Win32PInvoke.FINDEX_SEARCH_OPS.FindExSearchNameMatch, IntPtr.Zero, Win32PInvoke.FIND_FIRST_EX_LARGE_FETCH);
			Win32PInvoke.FindNextFile(hFile, out _);
			var result = Win32PInvoke.FindNextFile(hFile, out _);
			Win32PInvoke.FindClose(hFile);
			return result;
		}

		public static List<SubfolderEntry> EnumerateSubfolders(string path, bool showHidden, bool showProtected, bool showDot, int limit = 1000)
		{
			var results = new List<SubfolderEntry>();
			IntPtr hFind = OpenChildSearch(path, out var findData);
			if (hFind.ToInt64() == -1)
				return results;

			try
			{
				do
				{
					if (findData.cFileName is "." or "..")
						continue;
					var attrs = (FileAttributes)findData.dwFileAttributes;
					if ((attrs & FileAttributes.Directory) != FileAttributes.Directory)
						continue;

					var isHidden = (attrs & FileAttributes.Hidden) == FileAttributes.Hidden;
					var isSystem = (attrs & FileAttributes.System) == FileAttributes.System;

					if (!showDot && findData.cFileName.StartsWith('.'))
						continue;
					if (isHidden && !showHidden)
						continue;
					if (isHidden && isSystem && !showProtected)
						continue;

					var subPath = Path.Combine(path, findData.cFileName);
					results.Add(new SubfolderEntry(subPath, findData.cFileName, HasSubfolders(subPath), isHidden));

					if (results.Count == limit)
						break;
				}
				while (Win32PInvoke.FindNextFile(hFind, out findData));
			}
			finally
			{
				Win32PInvoke.FindClose(hFind);
			}

			results.Sort((a, b) => StringComparer.OrdinalIgnoreCase.Compare(a.Name, b.Name));
			return results;
		}

		public static bool HasSubfolders(string path)
		{
			IntPtr hFind = OpenChildSearch(path, out var findData);
			if (hFind.ToInt64() == -1)
				return false;

			try
			{
				do
				{
					if (findData.cFileName is "." or "..")
						continue;
					if (((FileAttributes)findData.dwFileAttributes & FileAttributes.Directory) == FileAttributes.Directory)
						return true;
				}
				while (Win32PInvoke.FindNextFile(hFind, out findData));
				return false;
			}
			finally
			{
				Win32PInvoke.FindClose(hFind);
			}
		}

		private static IntPtr OpenChildSearch(string path, out Win32PInvoke.WIN32_FIND_DATA findData)
			=> Win32PInvoke.FindFirstFileExFromApp(
				path + "\\*.*",
				Win32PInvoke.FINDEX_INFO_LEVELS.FindExInfoBasic,
				out findData,
				Win32PInvoke.FINDEX_SEARCH_OPS.FindExSearchNameMatch,
				IntPtr.Zero,
				Win32PInvoke.FIND_FIRST_EX_LARGE_FETCH);
	}
}

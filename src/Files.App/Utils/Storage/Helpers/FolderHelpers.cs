// Copyright (c) Files Community
// Licensed under the MIT License.

using System.IO;
using Windows.Win32;
using Windows.Win32.Storage.FileSystem;

namespace Files.App.Utils.Storage
{
	public readonly record struct SubfolderEntry(string Path, string Name, bool HasSubfolders, bool IsHidden);

	public static class FolderHelpers
	{
		public static unsafe bool CheckFolderAccessWithWin32(string path)
		{
			WIN32_FIND_DATAW findData = default;
			using FindCloseSafeHandle hFile = PInvoke.FindFirstFileEx($"{path}{Path.DirectorySeparatorChar}*.*", FINDEX_INFO_LEVELS.FindExInfoBasic,
				&findData, FINDEX_SEARCH_OPS.FindExSearchNameMatch, FIND_FIRST_EX_FLAGS.FIND_FIRST_EX_LARGE_FETCH);
			return !hFile.IsInvalid;
		}

		public static async Task<bool> CheckBitlockerStatusAsync(BaseStorageFolder? rootFolder, string path)
		{
			if (rootFolder?.Properties is null)
			{
				return false;
			}
			if (Path.IsPathRooted(path) && Path.GetPathRoot(path) == path)
			{
				IDictionary<string, object> extraProperties =
					await rootFolder.Properties.RetrievePropertiesAsync((string[])["System.Volume.BitLockerProtection"]);
				return (int?)extraProperties["System.Volume.BitLockerProtection"] == 6; // Drive is bitlocker protected and locked
			}
			return false;
		}

		/// <summary>
		/// This function is used to determine whether or not a folder has any contents.
		/// </summary>
		/// <param name="targetPath">The path to the target folder</param>
		///
		public static unsafe bool CheckForFilesFolders(string targetPath)
		{
			WIN32_FIND_DATAW findData = default;
			using FindCloseSafeHandle hFile = PInvoke.FindFirstFileEx($"{targetPath}{Path.DirectorySeparatorChar}*.*", FINDEX_INFO_LEVELS.FindExInfoBasic,
				&findData, FINDEX_SEARCH_OPS.FindExSearchNameMatch, FIND_FIRST_EX_FLAGS.FIND_FIRST_EX_LARGE_FETCH);
			if (hFile.IsInvalid)
				return false;

			do
			{
				string fileName = findData.cFileName.ToString();
				if (fileName is not "." and not "..")
					return true;
			}
			while (PInvoke.FindNextFile(hFile, out findData));

			return false;
		}

		public static unsafe List<SubfolderEntry> EnumerateSubfolders(string path, bool showHidden, bool showProtected, bool showDot, int limit = 1000)
		{
			var results = new List<SubfolderEntry>();
			WIN32_FIND_DATAW findData = default;
			using FindCloseSafeHandle hFind = PInvoke.FindFirstFileEx(
				path + "\\*.*",
				FINDEX_INFO_LEVELS.FindExInfoBasic,
				&findData,
				FINDEX_SEARCH_OPS.FindExSearchNameMatch,
				FIND_FIRST_EX_FLAGS.FIND_FIRST_EX_LARGE_FETCH);
			if (hFind.IsInvalid)
				return results;

			do
			{
				var attrs = (FileAttributes)findData.dwFileAttributes;
				if ((attrs & FileAttributes.Directory) != FileAttributes.Directory)
					continue;

				string fileName = findData.cFileName.ToString();
				if (fileName is "." or "..")
					continue;

				var isHidden = (attrs & FileAttributes.Hidden) == FileAttributes.Hidden;
				var isSystem = (attrs & FileAttributes.System) == FileAttributes.System;

				if (!showDot && fileName.StartsWith('.'))
					continue;
				if (isHidden && !showHidden)
					continue;
				if (isHidden && isSystem && !showProtected)
					continue;

				var subPath = Path.Combine(path, fileName);
				results.Add(new SubfolderEntry(subPath, fileName, HasSubfolders(subPath), isHidden));

				if (results.Count == limit)
					break;
			}
			while (PInvoke.FindNextFile(hFind, out findData));

			var naturalComparer = NaturalStringComparer.GetForProcessor();
			results.Sort((a, b) => naturalComparer.Compare(a.Name, b.Name));
			return results;
		}

		public static unsafe bool HasSubfolders(string path)
		{
			WIN32_FIND_DATAW findData = default;
			using FindCloseSafeHandle hFind = PInvoke.FindFirstFileEx(
				path + "\\*.*",
				FINDEX_INFO_LEVELS.FindExInfoBasic,
				&findData,
				FINDEX_SEARCH_OPS.FindExSearchNameMatch,
				FIND_FIRST_EX_FLAGS.FIND_FIRST_EX_LARGE_FETCH);
			if (hFind.IsInvalid)
				return false;

			do
			{
				string fileName = findData.cFileName.ToString();
				if (fileName is "." or "..")
					continue;
				if (((FileAttributes)findData.dwFileAttributes & FileAttributes.Directory) == FileAttributes.Directory)
					return true;
			}
			while (PInvoke.FindNextFile(hFind, out findData));
			return false;
		}
	}
}

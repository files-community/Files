// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Filesystem.StorageItems;
using System.IO;
using static Files.Backend.Helpers.NativeFindStorageItemHelper;

namespace Files.App.Filesystem
{
	public static class FolderHelpers
	{
		public static bool CheckFolderAccessWithWin32(string path)
		{
			IntPtr hFileTsk = FindFirstFileExFromApp($"{path}{Path.DirectorySeparatorChar}*.*", FINDEX_INFO_LEVELS.FindExInfoBasic,
				out WIN32_FIND_DATA _, FINDEX_SEARCH_OPS.FindExSearchNameMatch, IntPtr.Zero, FIND_FIRST_EX_LARGE_FETCH);
			if (hFileTsk.ToInt64() != -1)
			{
				FindClose(hFileTsk);
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
					await rootFolder.Properties.RetrievePropertiesAsync(new string[] { "System.Volume.BitLockerProtection" });
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
			IntPtr hFile = FindFirstFileExFromApp($"{targetPath}{Path.DirectorySeparatorChar}*.*", FINDEX_INFO_LEVELS.FindExInfoBasic,
				out WIN32_FIND_DATA _, FINDEX_SEARCH_OPS.FindExSearchNameMatch, IntPtr.Zero, FIND_FIRST_EX_LARGE_FETCH);
			FindNextFile(hFile, out _);
			var result = FindNextFile(hFile, out _);
			FindClose(hFile);
			return result;
		}
	}
}

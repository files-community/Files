// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Filesystem.StorageItems;
using Files.Backend.Helpers;
using System.IO;

namespace Files.App.Filesystem
{
	/// <summary>
	/// Provides static helper for storage folders.
	/// </summary>
	public static class FolderHelpers
	{
		public static bool CheckFolderAccessWithWin32(string path)
		{
			IntPtr hFileTsk = NativeFindStorageItemHelper.FindFirstFileExFromApp(
				$"{path}{Path.DirectorySeparatorChar}*.*",
				NativeFindStorageItemHelper.FINDEX_INFO_LEVELS.FindExInfoBasic,
				out NativeFindStorageItemHelper.WIN32_FIND_DATA _,
				NativeFindStorageItemHelper.FINDEX_SEARCH_OPS.FindExSearchNameMatch,
				IntPtr.Zero,
				NativeFindStorageItemHelper.FIND_FIRST_EX_LARGE_FETCH);

			if (hFileTsk.ToInt64() != -1)
			{
				NativeFindStorageItemHelper.FindClose(hFileTsk);

				return true;
			}

			return false;
		}

		public static async Task<bool> CheckBitlockerStatusAsync(BaseStorageFolder rootFolder, string path)
		{
			if (rootFolder?.Properties is null)
				return false;

			if (Path.IsPathRooted(path) && Path.GetPathRoot(path) == path)
			{
				IDictionary<string, object> extraProperties =
					await rootFolder.Properties.RetrievePropertiesAsync(new string[] { "System.Volume.BitLockerProtection" });

				return (int?)extraProperties["System.Volume.BitLockerProtection"] == 6; // Drive is BitLocker protected and locked
			}

			return false;
		}

		/// <summary>
		/// This function is used to determine whether or not a folder has any contents.
		/// </summary>
		/// <param name="targetPath">The path to the target folder</param>
		public static bool CheckForFilesFolders(string targetPath)
		{
			IntPtr hFile = NativeFindStorageItemHelper.FindFirstFileExFromApp(
				$"{targetPath}{Path.DirectorySeparatorChar}*.*",
				NativeFindStorageItemHelper.FINDEX_INFO_LEVELS.FindExInfoBasic,
				out NativeFindStorageItemHelper.WIN32_FIND_DATA _,
				NativeFindStorageItemHelper.FINDEX_SEARCH_OPS.FindExSearchNameMatch,
				IntPtr.Zero,
				NativeFindStorageItemHelper.FIND_FIRST_EX_LARGE_FETCH);

			NativeFindStorageItemHelper.FindNextFile(hFile, out _);

			var result = NativeFindStorageItemHelper.FindNextFile(hFile, out _);

			NativeFindStorageItemHelper.FindClose(hFile);

			return result;
		}
	}
}

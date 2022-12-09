using Common;
using Files.App.Filesystem.StorageItems;
using Files.App.Helpers;
using Files.App.Shell;
using Files.Shared.Extensions;
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.FileProperties;
using IO = System.IO;

namespace Files.App.Filesystem
{
	public static class FileTagsHelper
	{
		public static string FileTagsDbPath
			=> IO.Path.Combine(ApplicationData.Current.LocalFolder.Path, "filetags.db");

		private static readonly Lazy<FileTagsDb> dbInstance = new(() => new FileTagsDb(FileTagsDbPath, true));

		public static FileTagsDb GetDbInstance()
			=> dbInstance.Value;

		public static string[] ReadFileTag(string filePath)
		{
			var tagString = NativeFileOperationsHelper.ReadStringFromFile($"{filePath}:files");
			return tagString?.Split(',');
		}

		public static void WriteFileTag(string filePath, string[] tag)
		{
			// Backup date modified
			var isDateOk = NativeFileOperationsHelper.GetFileDateModified(filePath, out var dateModified);
			var isReadOnly = NativeFileOperationsHelper.HasFileAttribute(filePath, IO.FileAttributes.ReadOnly);

            // Unset read-only attribute https://github.com/files-community/Files/issues/7534
            if (isReadOnly)
			{
				NativeFileOperationsHelper.UnsetFileAttribute(filePath, IO.FileAttributes.ReadOnly);
			}
			if (tag is null || !tag.Any())
			{
				NativeFileOperationsHelper.DeleteFileFromApp($"{filePath}:files");
			}
			else if (ReadFileTag(filePath) is not string[] arr || !tag.SequenceEqual(arr))
			{
				NativeFileOperationsHelper.WriteStringToFile($"{filePath}:files", string.Join(',', tag));
			}

            // Restore read-only attribute https://github.com/files-community/Files/issues/7534
            if (isReadOnly)
			{
				NativeFileOperationsHelper.SetFileAttribute(filePath, IO.FileAttributes.ReadOnly);
			}

			if (isDateOk)
			{
				// Restore date modified
				NativeFileOperationsHelper.SetFileDateModified(filePath, dateModified);
			}
		}

		public static void UpdateTagsDb()
		{
			var dbInstance = GetDbInstance();
			foreach (var file in dbInstance.GetAll())
			{
				var pathFromFrn = Win32API.PathFromFileId(file.Frn ?? 0, file.FilePath);
				if (pathFromFrn is not null)
				{
					// Frn is valid, update file path
					var tag = ReadFileTag(pathFromFrn.Replace(@"\\?\", "", StringComparison.Ordinal));
					if (tag is not null && tag.Any())
					{
						dbInstance.UpdateTag(file.Frn ?? 0, null, pathFromFrn.Replace(@"\\?\", "", StringComparison.Ordinal));
						dbInstance.SetTags(pathFromFrn.Replace(@"\\?\", "", StringComparison.Ordinal), file.Frn, tag);
					}
					else
					{
						dbInstance.SetTags(null, file.Frn, null);
					}
				}
				else
				{
					var tag = ReadFileTag(file.FilePath);
					if (tag is not null && tag.Any())
					{
						if (!SafetyExtensions.IgnoreExceptions(() =>
						{
							var frn = GetFileFRN(file.FilePath);
							dbInstance.UpdateTag(file.FilePath, frn, null);
							dbInstance.SetTags(file.FilePath, (ulong?)frn, tag);
						},
						App.Logger))
						{
							dbInstance.SetTags(file.FilePath, null, null);
						}
					}
					else
					{
						dbInstance.SetTags(file.FilePath, null, null);
					}
				}
			}
		}

		public static ulong? GetFileFRN(string filePath)
			=> NativeFileOperationsHelper.GetFileFRN(filePath);

		public static Task<ulong?> GetFileFRN(IStorageItem item)
		{
			return item switch
			{
				BaseStorageFolder { Properties: not null } folder => GetFileFRN(folder.Properties),
				BaseStorageFile { Properties: not null } file => GetFileFRN(file.Properties),

				_ => Task.FromResult<ulong?>(null),
			};

			static async Task<ulong?> GetFileFRN(IStorageItemExtraProperties properties)
			{
				var extra = await properties.RetrievePropertiesAsync(new string[] { "System.FileFRN" });

				return (ulong?)extra["System.FileFRN"];
			}
		}
	}
}

// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.Win32;
using System.Runtime.CompilerServices;
using System.Security;
using Windows.ApplicationModel;
using Files.Shared.Helpers;
using static Files.App.Helpers.RegistryHelpers;
using static Files.App.Utils.FileTags.TaggedFileRegistry;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Files.App.Utils.FileTags
{
	public sealed class FileTagsDatabase
	{
		private static string? _FileTagsKey;
		private string? FileTagsKey => _FileTagsKey ??= SafetyExtensions.IgnoreExceptions(() => @$"Software\Files Community\{Package.Current.Id.Name}\v1\FileTags");
		private readonly static string MigrationMarkerKey = "MigrationCompleted";

		public void SetTags(string filePath, ulong? frn, string[] tags)
		{
			if (FileTagsKey is null)
				return;

			using var filePathKey = Registry.CurrentUser.CreateSubKey(CombineKeys(FileTagsKey, ChecksumHelpers.CreateSHA256(filePath)));

			if (tags is [])
			{
				SaveValues(filePathKey, null);
				if (frn is not null)
				{
					using var frnKey = Registry.CurrentUser.CreateSubKey(CombineKeys(FileTagsKey, "FRN", frn.Value.ToString()));
					SaveValues(frnKey, null);
				}

				return;
			}

			var newTag = new TaggedFile()
			{
				FilePath = filePath,
				Frn = frn,
				Tags = tags
			};
			SaveValues(filePathKey, newTag);

			if (frn is not null)
			{
				using var frnKey = Registry.CurrentUser.CreateSubKey(CombineKeys(FileTagsKey, "FRN", frn.Value.ToString()));
				SaveValues(frnKey, newTag);
			}
		}

		private TaggedFile? FindTag(string? filePath, ulong? frn)
		{
			if (FileTagsKey is null)
				return null;

			if (filePath is not null)
			{
				using var filePathKey = Registry.CurrentUser.CreateSubKey(CombineKeys(FileTagsKey, ChecksumHelpers.CreateSHA256(filePath)));
				if (filePathKey.ValueCount > 0)
				{
					var tag = new TaggedFile();
					BindValues(filePathKey, tag);
					if (frn is not null)
					{
						// Keep entry updated
						tag.Frn = frn;
						var value = frn.Value;
						filePathKey.SetValue(nameof(TaggedFile.Frn), Unsafe.As<ulong, long>(ref value), RegistryValueKind.QWord);
					}
					return tag;
				}
			}

			if (frn is not null)
			{
				using var frnKey = Registry.CurrentUser.CreateSubKey(CombineKeys(FileTagsKey, "FRN", frn.Value.ToString()));
				if (frnKey.ValueCount > 0)
				{
					var tag = new TaggedFile();
					BindValues(frnKey, tag);
					if (filePath is not null)
					{
						// Keep entry updated
						tag.FilePath = filePath;
						frnKey.SetValue(nameof(TaggedFile.FilePath), filePath, RegistryValueKind.String);
					}
					return tag;
				}
			}

			return null;
		}

		public void UpdateTag(string oldFilePath, ulong? frn, string? newFilePath)
		{
			if (FileTagsKey is null)
				return;

			var tag = FindTag(oldFilePath, null);
			using var filePathKey = Registry.CurrentUser.CreateSubKey(CombineKeys(FileTagsKey, ChecksumHelpers.CreateSHA256(oldFilePath)));
			SaveValues(filePathKey, null);

			if (tag is not null)
			{
				tag.Frn = frn ?? tag.Frn;
				tag.FilePath = newFilePath ?? tag.FilePath;

				if (frn is not null)
				{
					using var newFrnKey = Registry.CurrentUser.CreateSubKey(CombineKeys(FileTagsKey, "FRN", frn.Value.ToString()));
					SaveValues(newFrnKey, tag);
				}

				if (newFilePath is not null)
				{
					using var newFilePathKey = Registry.CurrentUser.CreateSubKey(CombineKeys(FileTagsKey, ChecksumHelpers.CreateSHA256(newFilePath)));
					SaveValues(newFilePathKey, tag);
				}
			}
		}

		public void UpdateTag(ulong oldFrn, ulong? frn, string? newFilePath)
		{
			if (FileTagsKey is null)
				return;

			var tag = FindTag(null, oldFrn);
			using var frnKey = Registry.CurrentUser.CreateSubKey(CombineKeys(FileTagsKey, "FRN", oldFrn.ToString()));
			SaveValues(frnKey, null);

			if (tag is not null)
			{
				tag.Frn = frn ?? tag.Frn;
				tag.FilePath = newFilePath ?? tag.FilePath;

				if (frn is not null)
				{
					using var newFrnKey = Registry.CurrentUser.CreateSubKey(CombineKeys(FileTagsKey, "FRN", frn.Value.ToString()));
					SaveValues(newFrnKey, tag);
				}

				if (newFilePath is not null)
				{
					using var newFilePathKey = Registry.CurrentUser.CreateSubKey(CombineKeys(FileTagsKey, ChecksumHelpers.CreateSHA256(newFilePath)));
					SaveValues(newFilePathKey, tag);
				}
			}
		}

		public string[] GetTags(string? filePath, ulong? frn)
		{
			MigrateExistingKeys();
			return FindTag(filePath, frn)?.Tags ?? [];
		}

		public IEnumerable<TaggedFile> GetAll()
		{
			var list = new List<TaggedFile>();

			if (FileTagsKey is not null)
			{
				try
				{
					IterateKeys(list, FileTagsKey, 0);
				}
				catch (SecurityException)
				{
					// Handle edge case where IterateKeys results in SecurityException
				}
			}

			return list;
		}

		public IEnumerable<TaggedFile> GetAllUnderPath(string folderPath)
		{
			folderPath = folderPath.Replace('/', '\\').TrimStart('\\');
			var list = new List<TaggedFile>();

			if (FileTagsKey is not null)
			{
				try
				{
					IterateKeys(list, CombineKeys(FileTagsKey, ChecksumHelpers.CreateSHA256(folderPath)), 0);
				}
				catch (SecurityException)
				{
					// Handle edge case where IterateKeys results in SecurityException
				}
			}

			return list;
		}

		public void Import(string json)
		{
			if (FileTagsKey is null)
				return;

			var tags = JsonSerializer.Deserialize<TaggedFile[]>(json);

			Registry.CurrentUser.DeleteSubKeyTree(FileTagsKey, false);
			if (tags is null)
			{
				return;
			}
			foreach (var tag in tags)
			{
				using var filePathKey = Registry.CurrentUser.CreateSubKey(CombineKeys(FileTagsKey, ChecksumHelpers.CreateSHA256(tag.FilePath)));
				SaveValues(filePathKey, tag);
				if (tag.Frn is not null)
				{
					using var frnKey = Registry.CurrentUser.CreateSubKey(CombineKeys(FileTagsKey, "FRN", tag.Frn.Value.ToString()));
					SaveValues(frnKey, tag);
				}
			}
		}

		public string Export()
		{
			var list = new List<TaggedFile>();

			if (FileTagsKey is not null)
				IterateKeys(list, FileTagsKey, 0);

			return JsonSerializer.Serialize(list);
		}

		private void IterateKeys(List<TaggedFile> list, string path, int depth)
		{
			using var key = Registry.CurrentUser.OpenSubKey(path);
			if (key is null)
				return;

			if (key.ValueCount > 0)
			{
				var tag = new TaggedFile();
				BindValues(key, tag);
				list.Add(tag);
			}

			foreach (var subKey in key.GetSubKeyNames())
			{
				// Skip FRN key
				if (depth == 0 && subKey == "FRN")
					continue;

				IterateKeys(list, CombineKeys(path, subKey), depth + 1);
			}
		}

		private void MigrateExistingKeys()
		{
			if (FileTagsKey is null)
				return;

			using var baseKey = Registry.CurrentUser.OpenSubKey(FileTagsKey);
			if (baseKey is null)
				return;

			// Check if migration is already completed
			if (baseKey.GetValue(MigrationMarkerKey) is not null)
				return;

			var keysToMigrate = new List<(string oldKey, TaggedFile tag)>();
			
			// Collect all keys that need migration (excluding FRN and migration marker)
			foreach (var subKeyName in baseKey.GetSubKeyNames())
			{
				if (subKeyName == "FRN" || subKeyName == MigrationMarkerKey)
					continue;

				// Check if this is a hash key (64 characters hex)
				if (subKeyName.Length == 64 && IsHexString(subKeyName))
					continue; // Already migrated

				using var subKey = baseKey.OpenSubKey(subKeyName);
				if (subKey?.ValueCount > 0)
				{
					var tag = new TaggedFile();
					BindValues(subKey, tag);
					keysToMigrate.Add((subKeyName, tag));
				}
			}

			// Migrate collected keys
			using var writerKey = Registry.CurrentUser.CreateSubKey(FileTagsKey);
			foreach (var (oldKey, tag) in keysToMigrate)
			{
				if (!string.IsNullOrEmpty(tag.FilePath))
				{
					// Create new hashed key
					using var newKey = Registry.CurrentUser.CreateSubKey(CombineKeys(FileTagsKey, ChecksumHelpers.CreateSHA256(tag.FilePath)));
					SaveValues(newKey, tag);
				}

				// Delete old key
				try
				{
					writerKey.DeleteSubKeyTree(oldKey);
				}
				catch
				{
					// Ignore deletion errors
				}
			}

			// Mark migration as completed
			writerKey.SetValue(MigrationMarkerKey, "1", RegistryValueKind.String);
		}

		private static bool IsHexString(string value)
		{
			return value.All(c => c >= '0' && c <= '9' || c >= 'a' && c <= 'f' || c >= 'A' && c <= 'F');
		}
	}
}
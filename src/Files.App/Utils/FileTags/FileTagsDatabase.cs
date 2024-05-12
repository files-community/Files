// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using LiteDB;
using Microsoft.Win32;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using Windows.ApplicationModel;
using Windows.Storage;
using JsonSerializer = System.Text.Json.JsonSerializer;
using static Files.App.Helpers.RegistryHelpers;
using static Files.App.Utils.FileTags.TaggedFileRegistry;

namespace Files.App.Utils.FileTags
{
	public sealed class FileTagsDatabase
	{
		private readonly static string FileTagsKey = @$"Software\Files Community\{Package.Current.Id.FullName}\v1\FileTags";

		private readonly static string FileTagsDbPath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "filetags.db");
		private const string FileTagsCollectionName = "taggedfiles";

		static FileTagsDatabase()
		{
			if (File.Exists(FileTagsDbPath))
			{
				SafetyExtensions.IgnoreExceptions(() => CheckDbVersion(FileTagsDbPath));

				using (var database = new LiteDatabase(new ConnectionString(FileTagsDbPath)
				{
					Connection = ConnectionType.Direct,
					Upgrade = true
				}))
				{
					UpdateDb(database);
					ImportCore(database.GetCollection<TaggedFile>(FileTagsCollectionName).FindAll().ToArray());
				}

				File.Delete(FileTagsDbPath);
			}
		}

		private static void UpdateDb(LiteDatabase database)
		{
			if (database.UserVersion == 0)
			{
				var col = database.GetCollection(FileTagsCollectionName);
				foreach (var doc in col.FindAll())
				{
					doc["Tags"] = new BsonValue(new[] { doc["Tag"].AsString });
					doc.Remove("Tags");
					col.Update(doc);
				}
				database.UserVersion = 1;
			}
		}

		// https://github.com/mbdavid/LiteDB/blob/master/LiteDB/Engine/Engine/Upgrade.cs
		private static void CheckDbVersion(string filename)
		{
			var buffer = new byte[8192 * 2];
			using (var stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
			{
				// read first 16k
				stream.Read(buffer, 0, buffer.Length);

				// checks if v7 (plain or encrypted)
				if (Encoding.UTF8.GetString(buffer, 25, "** This is a LiteDB file **".Length) == "** This is a LiteDB file **" &&
					buffer[52] == 7)
				{
					return; // version 4.1.4
				}
			}
			File.Delete(filename); // recreate DB with correct version
		}

		public void SetTags(string filePath, ulong? frn, string[] tags)
		{
			using var filePathKey = Registry.CurrentUser.CreateSubKey(CombineKeys(FileTagsKey, filePath));

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
			if (filePath is not null)
			{
				using var filePathKey = Registry.CurrentUser.CreateSubKey(CombineKeys(FileTagsKey, filePath));
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
			var tag = FindTag(oldFilePath, null);
			using var filePathKey = Registry.CurrentUser.CreateSubKey(CombineKeys(FileTagsKey, oldFilePath));
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
					using var newFilePathKey = Registry.CurrentUser.CreateSubKey(CombineKeys(FileTagsKey, newFilePath));
					SaveValues(newFilePathKey, tag);
				}
			}
		}

		public void UpdateTag(ulong oldFrn, ulong? frn, string? newFilePath)
		{
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
					using var newFilePathKey = Registry.CurrentUser.CreateSubKey(CombineKeys(FileTagsKey, newFilePath));
					SaveValues(newFilePathKey, tag);
				}
			}
		}

		public string[] GetTags(string? filePath, ulong? frn)
		{
			return FindTag(filePath, frn)?.Tags ?? [];
		}

		public IEnumerable<TaggedFile> GetAll()
		{
			var list = new List<TaggedFile>();
			IterateKeys(list, FileTagsKey, 0);
			return list;
		}

		public IEnumerable<TaggedFile> GetAllUnderPath(string folderPath)
		{
			folderPath = folderPath.Replace('/', '\\').TrimStart('\\');
			var list = new List<TaggedFile>();
			IterateKeys(list, CombineKeys(FileTagsKey, folderPath), 0);
			return list;
		}

		public void Import(string json)
		{
			var tags = JsonSerializer.Deserialize<TaggedFile[]>(json);
			ImportCore(tags);
		}

		private static void ImportCore(TaggedFile[]? tags)
		{
			Registry.CurrentUser.DeleteSubKeyTree(FileTagsKey, false);
			if (tags is null)
			{
				return;
			}
			foreach (var tag in tags)
			{
				using var filePathKey = Registry.CurrentUser.CreateSubKey(CombineKeys(FileTagsKey, tag.FilePath));
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
			IterateKeys(list, FileTagsKey, 0);
			return JsonSerializer.Serialize(list);
		}

		private void IterateKeys(List<TaggedFile> list, string path, int depth)
		{
			using var key = Registry.CurrentUser.OpenSubKey(path);
			if (key is null)
			{
				return;
			}

			if (key.ValueCount > 0)
			{
				var tag = new TaggedFile();
				BindValues(key, tag);
				list.Add(tag);
			}

			foreach (var subKey in key.GetSubKeyNames())
			{
				if (depth == 0 && subKey == "FRN")
				{
					// Skip FRN key
					continue;
				}

				IterateKeys(list, CombineKeys(path, subKey), depth + 1);
			}
		}
	}
}
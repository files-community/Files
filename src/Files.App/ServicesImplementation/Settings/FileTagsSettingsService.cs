using Files.App.Extensions;
using Files.App.Filesystem;
using Files.App.Serialization;
using Files.App.Serialization.Implementation;
using Files.Backend.Services.Settings;
using Files.Backend.ViewModels.FileTags;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.Storage;

namespace Files.App.ServicesImplementation.Settings
{
	internal sealed class FileTagsSettingsService : BaseJsonSettings, IFileTagsSettingsService
	{
		public event EventHandler OnSettingImportedEvent;

		public event EventHandler OnTagsUpdated;

		private static readonly List<TagViewModel> DefaultFileTags = new List<TagViewModel>()
		{
			new("Home", "#0072BD", "f7e0e137-2eb5-4fa4-a50d-ddd65df17c34"),
			new("Work", "#D95319", "c84a8131-c4de-47d9-9440-26e859d14b3d"),
			new("Photos", "#EDB120", "d4b8d4bd-ceaf-4e58-ac61-a185fcf96c5d"),
			new("Important", "#77AC30", "79376daf-c44a-4fe4-aa3b-8b30baea453e")
		};

		public FileTagsSettingsService()
		{
			SettingsSerializer = new DefaultSettingsSerializer();
			JsonSettingsSerializer = new DefaultJsonSettingsSerializer();
			JsonSettingsDatabase = new CachingJsonSettingsDatabase(SettingsSerializer, JsonSettingsSerializer);

			Initialize(Path.Combine(ApplicationData.Current.LocalFolder.Path,
				Constants.LocalSettings.SettingsFolderName, Constants.LocalSettings.FileTagSettingsFileName));
		}

		public IList<TagViewModel> FileTagList
		{
			get => Get<List<TagViewModel>>(DefaultFileTags);
			set
			{
				Set(value);
				OnTagsUpdated.Invoke(this, EventArgs.Empty);
			}
		}

		public TagViewModel GetTagById(string uid)
		{
			if (FileTagList.Any(x => x.Uid is null))
			{
				App.Logger.Warn("Tags file is invalid, regenerate");
				FileTagList = DefaultFileTags;
			}

			var tag = FileTagList.SingleOrDefault(x => x.Uid == uid);

			if (!string.IsNullOrEmpty(uid) && tag is null)
			{
				tag = new TagViewModel("Unknown".GetLocalizedResource(), "#9ea3a1", uid);
				FileTagList = FileTagList.Append(tag).ToList();
			}

			return tag;
		}

		public IList<TagViewModel> GetTagsByIds(string[] uids)
		{
			return uids?.Select(x => GetTagById(x)).ToList();
		}

		public IEnumerable<TagViewModel> GetTagsByName(string tagName)
		{
			return FileTagList.Where(x => x.Name.Equals(tagName, StringComparison.OrdinalIgnoreCase));
		}

		public IEnumerable<TagViewModel> SearchTagsByName(string tagName)
		{
			return FileTagList.Where(x => x.Name.StartsWith(tagName, StringComparison.OrdinalIgnoreCase));
		}

		public void CreateNewTag(string newTagName, string color)
		{
			var newTag = new TagViewModel(
				newTagName,
				color,
				Guid.NewGuid().ToString());

			var oldTags = FileTagList.ToList();
			oldTags.Add(newTag);
			FileTagList = oldTags;
		}

		public void EditTag(string uid, string name, string color)
		{
			var (tag, index) = GetTagAndIndex(uid);
			if (tag is null)
				return;

			tag.Name = name;
			tag.Color = color;

			var oldTags = FileTagList.ToList();
			oldTags.RemoveAt(index);
			oldTags.Insert(index, tag);
			FileTagList = oldTags;
		}

		public void DeleteTag(string uid)
		{
			var (_, index) = GetTagAndIndex(uid);
			if (index == -1)
				return;

			var oldTags = FileTagList.ToList();
			oldTags.RemoveAt(index);
			FileTagList = oldTags;
			UntagAllFiles(uid);
		}

		public override bool ImportSettings(object import)
		{
			if (import is string importString)
			{
				FileTagList = JsonSettingsSerializer.DeserializeFromJson<List<TagViewModel>>(importString);
			}
			else if (import is List<TagViewModel> importList)
			{
				FileTagList = importList;
			}

			FileTagList ??= DefaultFileTags;

			if (FileTagList is not null)
			{
				FlushSettings();
				OnSettingImportedEvent?.Invoke(this, null);
				return true;
			}

			return false;
		}

		public override object ExportSettings()
		{
			// Return string in Json format
			return JsonSettingsSerializer.SerializeToJson(FileTagList);
		}

		private (TagViewModel?, int) GetTagAndIndex(string uid)
		{
			TagViewModel? tag = null;
			int index = -1;

			for (int i = 0; i < FileTagList.Count; i++)
			{
				if (FileTagList[i].Uid == uid)
				{
					tag = FileTagList[i];
					index = i;
					break;
				}
			}

			return (tag, index);
		}

		private void UntagAllFiles(string uid)
		{
			var tagDoDelete = new string [] { uid };

			foreach (var item in FileTagsHelper.GetDbInstance().GetAll())
			{
				if (item.Tags.Contains(uid))
				{ 
					FileTagsHelper.WriteFileTag(
						item.FilePath, 
						item.Tags.Except(tagDoDelete).ToArray());
				}
			}
		}
	}
}

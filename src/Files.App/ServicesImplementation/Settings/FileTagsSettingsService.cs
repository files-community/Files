using Files.App.Extensions;
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

		private static readonly List<TagViewModel> DefaultFileTags = new List<TagViewModel>()
		{
			new("Home", "#0072BD"),
			new("Work", "#D95319"),
			new("Photos", "#EDB120"),
			new("Important", "#77AC30")
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
			set => Set(value);
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
				tag = new TagViewModel("FileTagUnknown".GetLocalizedResource(), "#9ea3a1", uid);
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
	}
}
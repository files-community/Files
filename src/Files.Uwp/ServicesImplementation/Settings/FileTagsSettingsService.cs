using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Files.Backend.Services.Settings;
using Files.Backend.ViewModels.FileTags;
using Files.Uwp.Serialization;
using Files.Uwp.Serialization.Implementation;
using Microsoft.Toolkit.Uwp;
using Windows.Storage;

namespace Files.Uwp.ServicesImplementation.Settings
{
	internal sealed class FileTagsSettingsService : BaseJsonSettings, IFileTagsSettingsService
	{
		public event EventHandler OnSettingImportedEvent;

		private static readonly List<FileTagViewModel> DefaultFileTags = new List<FileTagViewModel>()
		{
			new FileTagViewModel("Blue", "#0072BD"),
			new FileTagViewModel("Orange", "#D95319"),
			new FileTagViewModel("Yellow", "#EDB120"),
			new FileTagViewModel("Green", "#77AC30"),
			new FileTagViewModel("Azure", "#4DBEEE")
		};

		public FileTagsSettingsService()
		{
			SettingsSerializer = new DefaultSettingsSerializer();
			JsonSettingsSerializer = new DefaultJsonSettingsSerializer();
			JsonSettingsDatabase = new CachingJsonSettingsDatabase(SettingsSerializer, JsonSettingsSerializer);

			Initialize(Path.Combine(ApplicationData.Current.LocalFolder.Path, Constants.LocalSettings.SettingsFolderName, Constants.LocalSettings.FileTagSettingsFileName));
		}

		public IList<FileTagViewModel> FileTagList
		{
			get => Get<List<FileTagViewModel>>(DefaultFileTags);
			set => Set(value);
		}

		public FileTagViewModel GetTagById(string uid)
		{
			if (FileTagList.Any(x => x.Uid == null))
			{
				App.Logger.Warn("Tags file is invalid, regenerate");
				FileTagList = DefaultFileTags;
			}
			var tag = FileTagList.SingleOrDefault(x => x.Uid == uid);
			if (!string.IsNullOrEmpty(uid) && tag == null)
			{
				tag = new FileTagViewModel("FileTagUnknown".GetLocalized(), "#9ea3a1", uid);
				FileTagList = FileTagList.Append(tag).ToList();
			}
			return tag;
		}

		public IEnumerable<FileTagViewModel> GetTagsByName(string tagName)
		{
			return FileTagList.Where(x => x.TagName.StartsWith(tagName, StringComparison.OrdinalIgnoreCase));
		}

		public override bool ImportSettings(object import)
		{
			if (import is string importString)
			{
				FileTagList = JsonSettingsSerializer.DeserializeFromJson<List<FileTagViewModel>>(importString);
			}
			else if (import is List<FileTagViewModel> importList)
			{
				FileTagList = importList;
			}

			FileTagList ??= DefaultFileTags;

			if (FileTagList != null)
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

		public void ReportToAppCenter()
		{
		}
	}
}

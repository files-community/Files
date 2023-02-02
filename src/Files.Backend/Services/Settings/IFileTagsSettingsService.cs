using Files.Backend.ViewModels.FileTags;
using System;
using System.Collections.Generic;

namespace Files.Backend.Services.Settings
{
	public interface IFileTagsSettingsService : IBaseSettingsService
	{
		event EventHandler OnSettingImportedEvent;

		IList<TagViewModel> FileTagList { get; set; }

		TagViewModel GetTagById(string uid);

		IList<TagViewModel> GetTagsByIds(string[] uids);

		IEnumerable<TagViewModel> GetTagsByName(string tagName);

		IEnumerable<TagViewModel> SearchTagsByName(string tagName);

		object ExportSettings();

		bool ImportSettings(object import);
	}
}
// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Core.ViewModels.FileTags;

namespace Files.Core.Services.Settings
{
	public interface IFileTagsSettingsService : IBaseSettingsService
	{
		event EventHandler OnSettingImportedEvent;

		event EventHandler OnTagsUpdated;

		IList<TagViewModel> FileTagList { get; set; }

		TagViewModel GetTagById(string uid);

		IList<TagViewModel>? GetTagsByIds(string[] uids);

		IEnumerable<TagViewModel> GetTagsByName(string tagName);

		void CreateNewTag(string newTagName, string color);

		void EditTag(string uid, string name, string color);

		void DeleteTag(string uid);

		object ExportSettings();

		bool ImportSettings(object import);
	}
}
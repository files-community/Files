// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using System.IO;

namespace Files.App.Helpers
{
	internal static class AppSettingsImportHelper
	{
		public static async Task ImportFromZipAsync(string filePath)
		{
			var file = await StorageHelpers.ToStorageItem<BaseStorageFile>(filePath);
			if (file is null)
				throw new IOException($"The settings file '{filePath}' could not be read.");

			if (await ZipStorageFolder.FromStorageFileAsync(file) is not ZipStorageFolder zipFolder)
				return;

			// Import user settings
			var userSettingsFile = await zipFolder.GetFileAsync(Constants.LocalSettings.UserSettingsFileName);
			string importSettings = await userSettingsFile.ReadTextAsync();
			Ioc.Default.GetRequiredService<IUserSettingsService>().ImportSettings(importSettings);

			// Import file tags list and DB
			var fileTagsList = await zipFolder.GetFileAsync(Constants.LocalSettings.FileTagSettingsFileName);
			string importTags = await fileTagsList.ReadTextAsync();
			Ioc.Default.GetRequiredService<IFileTagsSettingsService>().ImportSettings(importTags);
			var fileTagsDB = await zipFolder.GetFileAsync(Constants.LocalSettings.FileTagSettingsDatabaseFileName);
			string importTagsDB = await fileTagsDB.ReadTextAsync();
			var tagDbInstance = FileTagsHelper.GetDbInstance();
			tagDbInstance.Import(importTagsDB);

			// Import layout preferences and DB
			var layoutPrefsDB = await zipFolder.GetFileAsync(Constants.LocalSettings.UserSettingsDatabaseFileName);
			string importPrefsDB = await layoutPrefsDB.ReadTextAsync();
			var layoutDbInstance = LayoutPreferencesManager.GetDatabaseManagerInstance();
			layoutDbInstance.Import(importPrefsDB);
		}
	}
}

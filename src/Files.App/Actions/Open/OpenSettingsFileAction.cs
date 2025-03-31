// Copyright (c) Files Community
// Licensed under the MIT License.

using Windows.Storage;
using Windows.System;

namespace Files.App.Actions
{
	internal sealed partial class OpenSettingsFileAction :  IAction
	{
		public string Label
			=> Strings.EditSettingsFile.GetLocalizedResource();

		public string Description
			=> Strings.EditSettingsFileDescription.GetLocalizedResource();

		public HotKey HotKey
			=> new(Keys.OemComma, KeyModifiers.CtrlShift);

		public RichGlyph Glyph
			=> new("\uE8DA");

		public async Task ExecuteAsync(object? parameter = null)
		{
			await SafetyExtensions.IgnoreExceptions(async () =>
			{
				var settingsJsonFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appdata:///local/settings/user_settings.json"));
				if (!await Launcher.LaunchFileAsync(settingsJsonFile))
					await ContextMenu.InvokeVerb("open", settingsJsonFile.Path);
			});
		}
	}
}

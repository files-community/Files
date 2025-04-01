// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation.Metadata;
using Windows.Storage;
using Windows.System;

namespace Files.App.Actions
{
	internal sealed partial class OpenSettingsFileAction : IAction
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
			try
			{
				var settingsJsonFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri($"ms-appdata:///local/{Constants.LocalSettings.SettingsFolderName}/{Constants.LocalSettings.UserSettingsFileName}"));
				if (!await Launcher.LaunchFileAsync(settingsJsonFile))
					await ContextMenu.InvokeVerb("open", settingsJsonFile.Path);
			}
			catch (Exception ex)
			{
				// Only show the error dialog if no other popups are open
				if (!VisualTreeHelper.GetOpenPopupsForXamlRoot(MainWindow.Instance.Content.XamlRoot).Any())
				{
					var errorDialog = new ContentDialog()
					{
						Title = Strings.FailedToOpenSettingsFile.GetLocalizedResource(),
						Content = ex.Message,
						PrimaryButtonText = Strings.OK.GetLocalizedResource(),
					};

					if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
						errorDialog.XamlRoot = MainWindow.Instance.Content.XamlRoot;

					await errorDialog.TryShowAsync();
				}
			}
		}
	}
}
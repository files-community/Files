// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation.Metadata;
using Windows.Storage;
using Windows.System;

namespace Files.App.Actions
{
	[GeneratedRichCommand]
	internal sealed partial class OpenLogFileAction : IAction
	{
		public string Label
			=> Strings.OpenLogFile.GetLocalizedResource();

		public string Description
			=> Strings.OpenLogFileDescription.GetLocalizedResource();

		public HotKey HotKey
			=> new(Keys.OemPeriod, KeyModifiers.Ctrl);

		public async Task ExecuteAsync(object? parameter = null)
		{
			try
			{
				var debugFile = await ApplicationData.Current.LocalFolder.TryGetItemAsync("debug.log") as StorageFile;

				if (debugFile != null && !await Launcher.LaunchFileAsync(debugFile))
				{
					// Fallback to Process.Start if Launcher fails
					using var process = Process.Start(new ProcessStartInfo(debugFile.Path)
					{
						UseShellExecute = true,
						Verb = "open"
					});
				}
			}
			catch (Exception ex)
			{
				// Only show the error dialog if no other popups are open
				if (!VisualTreeHelper.GetOpenPopupsForXamlRoot(MainWindow.Instance.Content.XamlRoot).Any())
				{
					var errorDialog = new ContentDialog()
					{
						Title = Strings.FailedToOpenLogFile.GetLocalizedResource(),
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
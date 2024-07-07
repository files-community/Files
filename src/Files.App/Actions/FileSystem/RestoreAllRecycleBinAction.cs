// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Controls;
using Windows.Foundation.Metadata;

namespace Files.App.Actions
{
	internal sealed class RestoreAllRecycleBinAction : BaseUIAction, IAction
	{
		private readonly IWindowsRecycleBinService WindowsRecycleBinService = Ioc.Default.GetRequiredService<IWindowsRecycleBinService>();

		public string Label
			=> "RestoreAllItems".GetLocalizedResource();

		public string Description
			=> "RestoreAllRecycleBinDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new(themedIconStyle: "App.ThemedIcons.RestoreDeleted");

		public override bool IsExecutable =>
			UIHelpers.CanShowDialog &&
			WindowsRecycleBinService.HasItems();

		public async Task ExecuteAsync(object? parameter = null)
		{
			// TODO: Use AppDialogService
			var confirmationDialog = new ContentDialog()
			{
				Title = "ConfirmRestoreBinDialogTitle".GetLocalizedResource(),
				Content = "ConfirmRestoreBinDialogContent".GetLocalizedResource(),
				PrimaryButtonText = "Yes".GetLocalizedResource(),
				SecondaryButtonText = "Cancel".GetLocalizedResource(),
				DefaultButton = ContentDialogButton.Primary
			};

			if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
				confirmationDialog.XamlRoot = MainWindow.Instance.Content.XamlRoot;

			if (await confirmationDialog.TryShowAsync() is not ContentDialogResult.Primary)
				return;

			bool result = await Task.Run(WindowsRecycleBinService.RestoreAllAsync);

			// Show error dialog when failed
			if (!result)
			{
				var errorDialog = new ContentDialog()
				{
					Title = "FailedToRestore".GetLocalizedResource(),
					PrimaryButtonText = "OK".GetLocalizedResource(),
				};

				if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
					errorDialog.XamlRoot = MainWindow.Instance.Content.XamlRoot;

				await errorDialog.TryShowAsync();
			}
		}
	}
}

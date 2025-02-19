// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Controls;
using Windows.Foundation.Metadata;

namespace Files.App.Actions
{
	internal sealed partial class RestoreAllRecycleBinAction : BaseUIAction, IAction
	{
		private readonly IStorageTrashBinService StorageTrashBinService = Ioc.Default.GetRequiredService<IStorageTrashBinService>();

		public string Label
			=> Strings.RestoreAllItems.GetLocalizedResource();

		public string Description
			=> Strings.RestoreAllRecycleBinDescription.GetLocalizedResource();

		public RichGlyph Glyph
			=> new(themedIconStyle: "App.ThemedIcons.RestoreDeleted");

		public override bool IsExecutable =>
			UIHelpers.CanShowDialog &&
			StorageTrashBinService.HasItems();

		public async Task ExecuteAsync(object? parameter = null)
		{
			// TODO: Use AppDialogService
			var confirmationDialog = new ContentDialog()
			{
				Title = Strings.ConfirmRestoreBinDialogTitle.GetLocalizedResource(),
				Content = Strings.ConfirmRestoreBinDialogContent.GetLocalizedResource(),
				PrimaryButtonText = Strings.Yes.GetLocalizedResource(),
				SecondaryButtonText = Strings.Cancel.GetLocalizedResource(),
				DefaultButton = ContentDialogButton.Primary
			};

			if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
				confirmationDialog.XamlRoot = MainWindow.Instance.Content.XamlRoot;

			if (await confirmationDialog.TryShowAsync() is not ContentDialogResult.Primary)
				return;

			bool result = await StorageTrashBinService.RestoreAllTrashesAsync();

			// Show error dialog when failed
			if (!result)
			{
				var errorDialog = new ContentDialog()
				{
					Title = Strings.FailedToRestore.GetLocalizedResource(),
					PrimaryButtonText = Strings.OK.GetLocalizedResource(),
				};

				if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
					errorDialog.XamlRoot = MainWindow.Instance.Content.XamlRoot;

				await errorDialog.TryShowAsync();
			}
		}
	}
}

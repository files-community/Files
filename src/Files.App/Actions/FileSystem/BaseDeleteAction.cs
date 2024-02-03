// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Windows.Storage;

namespace Files.App.Actions
{
	internal abstract class BaseDeleteAction : BaseUIAction
	{
		private IContentPageContext ContentPageContext { get; } = Ioc.Default.GetRequiredService<IContentPageContext>();
		private IFoldersSettingsService FoldersSettingsService { get; } = Ioc.Default.GetRequiredService<IFoldersSettingsService>();

		public override bool IsExecutable =>
			ContentPageContext.HasSelection &&
			(!ContentPageContext.ShellPage?.SlimContentPage?.IsRenamingItem ?? false) &&
			UIHelpers.CanShowDialog;

		public BaseDeleteAction()
		{
			ContentPageContext.PropertyChanged += Context_PropertyChanged;
		}

		protected async Task DeleteItemsAsync(bool permanently)
		{
			var items =
				ContentPageContext.SelectedItems.Select(item =>
					StorageHelpers.FromPathAndType(
						item.ItemPath,
						item.PrimaryItemAttribute is StorageItemTypes.File
							? FilesystemItemType.File
							: FilesystemItemType.Directory));

			await ContentPageContext.ShellPage!.FilesystemHelpers.DeleteItemsAsync(items, FoldersSettingsService.DeleteConfirmationPolicy, permanently, true);

			await ContentPageContext.ShellPage.FilesystemViewModel.ApplyFilesAndFoldersChangesAsync();
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IContentPageContext.HasSelection))
				OnPropertyChanged(nameof(IsExecutable));
		}
	}
}

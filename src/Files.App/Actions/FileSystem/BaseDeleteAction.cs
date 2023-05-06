// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Contexts;
using Windows.Storage;

namespace Files.App.Actions
{
	internal abstract class BaseDeleteAction : BaseUIAction
	{
		private readonly IFoldersSettingsService settings = Ioc.Default.GetRequiredService<IFoldersSettingsService>();

		protected readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		public override bool IsExecutable =>
			context.HasSelection &&
			(!context.ShellPage?.SlimContentPage?.IsRenamingItem ?? false) &&
			UIHelpers.CanShowDialog;

		public BaseDeleteAction()
		{
			context.PropertyChanged += Context_PropertyChanged;
		}

		protected async Task DeleteItems(bool permanently)
		{
			var items = context.SelectedItems.Select(item => StorageHelpers.FromPathAndType(item.ItemPath,
					item.PrimaryItemAttribute is StorageItemTypes.File ? FilesystemItemType.File : FilesystemItemType.Directory));

			await context.ShellPage!.FilesystemHelpers.DeleteItemsAsync(items, settings.DeleteConfirmationPolicy, permanently, true);
			await context.ShellPage.FilesystemViewModel.ApplyFilesAndFoldersChangesAsync();
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IContentPageContext.HasSelection))
				OnPropertyChanged(nameof(IsExecutable));
		}
	}
}

// Copyright (c) Files Community
// Licensed under the MIT License.

using Windows.Storage;

namespace Files.App.Actions
{
	internal abstract class BaseDeleteAction : BaseUIAction
	{
		private readonly IFoldersSettingsService settings;

		protected readonly IContentPageContext context;

		public virtual ActionCategory Category
			=> ActionCategory.FileSystem;

		public override bool IsExecutable =>
			context.HasSelection &&
			(!context.ShellPage?.SlimContentPage?.IsRenamingItem ?? false) &&
			UIHelpers.CanShowDialog;

		public BaseDeleteAction()
		{
			settings = Ioc.Default.GetRequiredService<IFoldersSettingsService>();
			context = Ioc.Default.GetRequiredService<IContentPageContext>();

			context.PropertyChanged += Context_PropertyChanged;
		}

		protected async Task DeleteItemsAsync(bool permanently)
		{
			var items =
				context.SelectedItems.Select(item =>
					StorageHelpers.FromPathAndType(
						item.ItemPath ?? throw new InvalidOperationException("The selected item does not have a path."),
						item.PrimaryItemAttribute is StorageItemTypes.File
							? FilesystemItemType.File
							: FilesystemItemType.Directory));

			if (context.ShellPage is { } shellPage)
			{
				await shellPage.FilesystemHelpers.DeleteItemsAsync(items, settings.DeleteConfirmationPolicy, permanently, true);
				var shellViewModel = shellPage.ShellViewModel ?? throw new InvalidOperationException("The active shell page has no shell view model.");
				await shellViewModel.ApplyFilesAndFoldersChangesAsync();
			}
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IContentPageContext.HasSelection))
				OnPropertyChanged(nameof(IsExecutable));
		}
	}
}

// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Actions
{
	[GeneratedRichCommand]
	internal sealed partial class DeleteItemFromShelfAction : ObservableObject, IAction
	{
		private readonly IShelfContext shelfContext;
		private readonly IContentPageContext contentPageContext;
		private readonly IFoldersSettingsService foldersSettingsService;

		public string Label
			=> Strings.Delete.GetLocalizedResource();

		public string Description
			=> Strings.DeleteItemDescription.GetLocalizedFormatResource(shelfContext.SelectedItems.Count);

		public ActionCategory Category
			=> ActionCategory.FileSystem;

		public RichGlyph Glyph
			=> new(themedIconStyle: "App.ThemedIcons.Delete");

		public bool IsExecutable
			=> shelfContext.HasSelection && contentPageContext.ShellPage is not null;

		public bool IsAccessibleGlobally
			=> false;

		public DeleteItemFromShelfAction()
		{
			shelfContext = Ioc.Default.GetRequiredService<IShelfContext>();
			contentPageContext = Ioc.Default.GetRequiredService<IContentPageContext>();
			foldersSettingsService = Ioc.Default.GetRequiredService<IFoldersSettingsService>();

			shelfContext.PropertyChanged += ShelfContext_PropertyChanged;
		}

		public async Task ExecuteAsync(object? parameter = null)
		{
			if (contentPageContext.ShellPage is not { } shellPage)
				return;

			var itemsToDelete = shelfContext.SelectedItems.Select(x => StorageHelpers.FromPathAndType(x.Inner.Id, x.Inner switch
			{
				IFile => FilesystemItemType.File,
				IFolder => FilesystemItemType.Directory,
				_ => throw new ArgumentOutOfRangeException(nameof(parameter))
			}));

			await shellPage.FilesystemHelpers.DeleteItemsAsync(itemsToDelete, foldersSettingsService.DeleteConfirmationPolicy, false, true);
			await shellPage.ShellViewModel.ApplyFilesAndFoldersChangesAsync();
		}

		private void ShelfContext_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IShelfContext.HasSelection))
				OnPropertyChanged(nameof(IsExecutable));
		}
	}
}

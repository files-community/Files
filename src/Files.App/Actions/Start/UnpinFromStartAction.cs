// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Actions
{
	[GeneratedRichCommand]
	internal sealed class UnpinFromStartAction : IAction
	{
		private IStorageService StorageService { get; } = Ioc.Default.GetRequiredService<IStorageService>();

		private IStartMenuService StartMenuService { get; } = Ioc.Default.GetRequiredService<IStartMenuService>();

		public IContentPageContext context;

		public string Label
			=> Strings.UnpinItemFromStartText.GetLocalizedResource();

		public string Description
			=> Strings.UnpinFromStartDescription.GetLocalizedFormatResource(context.HasSelection ? context.SelectedItems.Count : 1);

		public RichGlyph Glyph
			=> new(themedIconStyle: "App.ThemedIcons.FavoritePinRemove");

		public ActionCategory Category
			=> ActionCategory.Start;

		public UnpinFromStartAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();
		}

		public async Task ExecuteAsync(object? parameter = null)
		{
			if (context.SelectedItems.Count > 0)
			{
				var selectedItems = context.ShellPage?.SlimContentPage?.SelectedItems
					?? throw new InvalidOperationException("The active file-list selection is not available.");
				foreach (ListedItem listedItem in selectedItems)
				{
					await SafetyExtensions.IgnoreExceptions(async () =>
					{
						var itemPath = listedItem.GetRequiredPath();
						IStorable storable = listedItem switch
						{
							// Archives are marked as folders when browsable in-app but are files on disk
							{ IsFolder: true, IsArchive: false } => await StorageService.GetFolderAsync(itemPath),
							_ => await StorageService.GetFileAsync((listedItem as IShortcutItem)?.TargetPath is { Length: > 0 } targetPath
								? targetPath
								: itemPath)
						};
						await StartMenuService.UnpinAsync(storable);
					});
				}
			}
			else
			{
				await SafetyExtensions.IgnoreExceptions(async () =>
				{
					var currentFolder = context.ShellPage?.ShellViewModel?.CurrentFolder
						?? throw new InvalidOperationException("The current folder is not available.");
					var currentFolderPath = currentFolder.GetRequiredPath();
					IStorable storable = context.PageType is ContentPageTypes.ZipFolder
						? await StorageService.GetFileAsync(currentFolderPath)
						: await StorageService.GetFolderAsync(currentFolderPath);

					await StartMenuService.UnpinAsync(storable);
				});
			}
		}
	}
}

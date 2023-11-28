// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Core.Storage;

namespace Files.App.Actions
{
	internal class UnpinFromStartAction : IAction
	{
		private IStorageService StorageService { get; } = Ioc.Default.GetRequiredService<IStorageService>();

		private IStartMenuService StartMenuService { get; } = Ioc.Default.GetRequiredService<IStartMenuService>();

		public IContentPageContext context;

		public string Label
			=> "UnpinItemFromStart/Text".GetLocalizedResource();

		public string Description
			=> "UnpinFromStartDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new(opacityStyle: "ColorIconUnpinFromFavorites");

		public UnpinFromStartAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();
		}

		public async Task ExecuteAsync()
		{
			if (context.SelectedItems.Count > 0)
			{
				foreach (ListedItem listedItem in context.ShellPage?.SlimContentPage.SelectedItems)
				{
					var folder = await StorageService.GetFolderAsync(listedItem.ItemPath);
					await StartMenuService.UnpinAsync(folder);
				}
			}
			else
			{
				var currentFolder = context.ShellPage.FilesystemViewModel.CurrentFolder;
				var folder = await StorageService.GetFolderAsync(currentFolder.ItemPath);

				await StartMenuService.UnpinAsync(folder);
			}
		}
	}
}

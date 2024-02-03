// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Core.Storage;

namespace Files.App.Actions
{
	internal class UnpinFromStartAction : IAction
	{
		private IContentPageContext ContentPageContext { get; } = Ioc.Default.GetRequiredService<IContentPageContext>();
		private IStartMenuService StartMenuService { get; } = Ioc.Default.GetRequiredService<IStartMenuService>();
		private IStorageService StorageService { get; } = Ioc.Default.GetRequiredService<IStorageService>();

		public string Label
			=> "UnpinItemFromStart/Text".GetLocalizedResource();

		public string Description
			=> "UnpinFromStartDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new(opacityStyle: "ColorIconUnpinFromFavorites");

		public UnpinFromStartAction()
		{
		}

		public async Task ExecuteAsync()
		{
			if (ContentPageContext.SelectedItems.Count > 0)
			{
				foreach (ListedItem listedItem in ContentPageContext.ShellPage?.SlimContentPage.SelectedItems)
				{
					IStorable storable = listedItem.IsFolder switch
					{
						true => await StorageService.GetFolderAsync(listedItem.ItemPath),
						_ => await StorageService.GetFileAsync(listedItem.ItemPath)
					};
					await StartMenuService.UnpinAsync(storable);
				}
			}
			else
			{
				var currentFolder = ContentPageContext.ShellPage.FilesystemViewModel.CurrentFolder;
				var folder = await StorageService.GetFolderAsync(currentFolder.ItemPath);

				await StartMenuService.UnpinAsync(folder);
			}
		}
	}
}

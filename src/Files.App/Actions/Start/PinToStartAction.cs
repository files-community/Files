// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Core.Storage;

namespace Files.App.Actions
{
	internal class PinToStartAction : IAction
	{
		private IContentPageContext ContentPageContext { get; } = Ioc.Default.GetRequiredService<IContentPageContext>();
		private IStartMenuService StartMenuService { get; } = Ioc.Default.GetRequiredService<IStartMenuService>();
		private IStorageService StorageService { get; } = Ioc.Default.GetRequiredService<IStorageService>();

		public string Label
			=> "PinItemToStart/Text".GetLocalizedResource();

		public string Description
			=> "PinToStartDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new(opacityStyle: "ColorIconPinToFavorites");

		public bool IsExecutable =>
			ContentPageContext.ShellPage is not null;

		public PinToStartAction()
		{
		}

		public async Task ExecuteAsync()
		{
			if (ContentPageContext.SelectedItems.Count > 0 && ContentPageContext.ShellPage?.SlimContentPage?.SelectedItems is not null)
			{
				foreach (ListedItem listedItem in ContentPageContext.ShellPage.SlimContentPage.SelectedItems)
				{
					IStorable storable = listedItem.IsFolder switch
					{
						true => await StorageService.GetFolderAsync(listedItem.ItemPath),
						_ => await StorageService.GetFileAsync(listedItem.ItemPath)
					};

					await StartMenuService.PinAsync(storable, listedItem.Name);
				}
			}
			else if (ContentPageContext.ShellPage?.FilesystemViewModel?.CurrentFolder is not null)
			{
				var currentFolder = ContentPageContext.ShellPage.FilesystemViewModel.CurrentFolder;
				var folder = await StorageService.GetFolderAsync(currentFolder.ItemPath);

				await StartMenuService.PinAsync(folder, currentFolder.Name);
			}
		}
	}
}

// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Core.Storage;

namespace Files.App.Actions
{
	internal sealed class UnpinFromStartAction : IAction
	{
		private IStorageService StorageService { get; } = Ioc.Default.GetRequiredService<IStorageService>();

		private IStartMenuService StartMenuService { get; } = Ioc.Default.GetRequiredService<IStartMenuService>();

		public IContentPageContext context;

		public string Label
			=> "UnpinItemFromStart/Text".GetLocalizedResource();

		public string Description
			=> "UnpinFromStartDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new(opacityStyle: "Icons.Unpin.16x16");

		public UnpinFromStartAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();
		}

		public async Task ExecuteAsync(object? parameter = null)
		{
			if (context.SelectedItems.Count > 0)
			{
				foreach (ListedItem listedItem in context.ShellPage?.SlimContentPage.SelectedItems)
				{
					IStorable storable = listedItem.IsFolder switch
					{
						true => await StorageService.GetFolderAsync(listedItem.ItemPath),
						_ => await StorageService.GetFileAsync((listedItem as ShortcutItem)?.TargetPath ?? listedItem.ItemPath)
					};
					await StartMenuService.UnpinAsync(storable);
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

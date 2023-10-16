// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Core.Storage;

namespace Files.App.Actions
{
	internal class PinToStartAction : IAction
	{
		public IContentPageContext context;

		private IStartMenuService SystemPinService { get; } = Ioc.Default.GetRequiredService<IStartMenuService>();

		private IStorageService StorageService { get; } = Ioc.Default.GetRequiredService<IStorageService>();


		public string Label
			=> "PinItemToStart/Text".GetLocalizedResource();

		public string Description
			=> "PinToStartDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new(opacityStyle: "ColorIconPinToFavorites");

		public PinToStartAction()
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
					await SystemPinService.PinAsync(folder);
				}
			}
			else
			{
				var folder = await StorageService.GetFolderAsync(context.ShellPage?.FilesystemViewModel.CurrentFolder.ItemPath);
				await SystemPinService.PinAsync(folder);
			}
		}
	}
}

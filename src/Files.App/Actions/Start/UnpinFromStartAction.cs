// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Core.Storage;

namespace Files.App.Actions
{
	internal class UnpinFromStartAction : IAction
	{
		public IContentPageContext context;

		private ISystemPinService SystemPinService { get; } = Ioc.Default.GetRequiredService<ISystemPinService>();

		private IStorageService StorageService { get; } = Ioc.Default.GetRequiredService<IStorageService>();

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
					await SystemPinService.UnpinAsync(folder);
				}
			}
			else
			{
				var folder = await StorageService.GetFolderAsync(context.ShellPage?.FilesystemViewModel.CurrentFolder.ItemPath);
				await SystemPinService.UnpinAsync(folder);
			}
		}
	}
}

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

		public bool IsExecutable =>
			context.ShellPage is not null;

		public PinToStartAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();
		}

		public async Task ExecuteAsync()
		{
			if (context.SelectedItems.Count > 0 && context.ShellPage?.SlimContentPage?.SelectedItems is not null)
			{
				foreach (ListedItem listedItem in context.ShellPage.SlimContentPage.SelectedItems)
					await App.SecondaryTileHelper.TryPinFolderAsync(listedItem.ItemPath, listedItem.Name);
			}
			else if (context.ShellPage?.FilesystemViewModel?.CurrentFolder is not null)
			{
				await App.SecondaryTileHelper.TryPinFolderAsync(context.ShellPage.FilesystemViewModel.CurrentFolder.ItemPath, context.ShellPage.FilesystemViewModel.CurrentFolder.Name);
			}
		}
	}
}

// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal class UnpinFromStartAction : IAction
	{
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
					await App.SecondaryTileHelper.UnpinFromStartAsync(listedItem.ItemPath);
			}
			else
			{
				await App.SecondaryTileHelper.UnpinFromStartAsync(context.ShellPage?.FilesystemViewModel.CurrentFolder.ItemPath);
			}
		}
	}
}

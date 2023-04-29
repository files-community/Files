﻿// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Commands;
using Files.App.Contexts;

namespace Files.App.Actions
{
	internal class UnpinFromStartAction : IAction
	{
		public IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label { get; } = "UnpinItemFromStart/Text".GetLocalizedResource();

		public string Description => "UnpinFromStartDescription".GetLocalizedResource();

		public RichGlyph Glyph { get; } = new RichGlyph(opacityStyle: "ColorIconUnpinFromFavorites");

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

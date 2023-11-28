// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.Mvvm.DependencyInjection;
using Files.Core.Storage;

namespace Files.App.Actions
{
	internal class PinToStartAction : IAction
	{
		private IStorageService StorageService { get; } = Ioc.Default.GetRequiredService<IStorageService>();

		private IStartMenuService StartMenuService { get; } = Ioc.Default.GetRequiredService<IStartMenuService>();

		public IContentPageContext context;

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
				{
					var folder = await StorageService.GetFolderAsync(listedItem.ItemPath);
					await StartMenuService.PinAsync(folder, listedItem.Name);
				}
			}
			else if (context.ShellPage?.FilesystemViewModel?.CurrentFolder is not null)
			{
				var currentFolder = context.ShellPage.FilesystemViewModel.CurrentFolder;
				var folder = await StorageService.GetFolderAsync(currentFolder.ItemPath);

				await StartMenuService.PinAsync(folder, currentFolder.Name);
			}
		}
	}
}

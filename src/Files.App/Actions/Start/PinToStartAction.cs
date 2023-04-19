using Files.App.Commands;
using Files.App.Contexts;

namespace Files.App.Actions
{
	internal class PinToStartAction : IAction
	{
		public IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label { get; } = "PinItemToStart/Text".GetLocalizedResource();

		public string Description => "PinToStartDescription".GetLocalizedResource();

		public RichGlyph Glyph { get; } = new RichGlyph(opacityStyle: "ColorIconPinToFavorites");

		public async Task ExecuteAsync()
		{
			if (context.SelectedItems.Count > 0)
			{
				foreach (ListedItem listedItem in context.ShellPage?.SlimContentPage.SelectedItems)
					await App.SecondaryTileHelper.TryPinFolderAsync(listedItem.ItemPath, listedItem.Name);
			}
			else
			{
				await App.SecondaryTileHelper.TryPinFolderAsync(context.ShellPage?.FilesystemViewModel.CurrentFolder.ItemPath, context.ShellPage?.FilesystemViewModel.CurrentFolder.Name);
			}
		}
	}
}

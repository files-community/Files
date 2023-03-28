using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Commands;
using Files.App.Contexts;
using Files.App.Extensions;
using Files.App.Filesystem;
using Microsoft.UI.Xaml.Input;
using System.Threading.Tasks;

namespace Files.App.Actions
{
	internal class UnpinFromStartAction : XamlUICommand
	{
		public IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label { get; } = "UnpinItemFromStart/Text".GetLocalizedResource();

		public string Description => "TODO: Need to be described.";

		public RichGlyph Glyph { get; } = new RichGlyph(opacityStyle: "ColorIconUnpinFromFavorites");

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
